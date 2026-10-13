using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Vafadar.Data.Tests;

/// <summary>Original session refusal and authorization guards; native conversion is checked in the independent Android package.</summary>
public sealed class SqlCipherGenerationMigrationSessionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vafadar-migration-session-" + Guid.NewGuid().ToString("N"));
    private readonly DatabaseGenerationCheckpoint initial = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    private readonly Keys keys = new();
    private readonly Durability durability = new();
    private bool retired;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private string Original => Path.Combine(root, "profile.db");
    private SqliteDatabaseGenerationJournal Journal => new(Path.Combine(root, "generation.sqlite"), initial);
    private static DatabaseGenerationCheckpoint Next(DatabaseGenerationCheckpoint state) => state.Advance(state.ProfileId, state.CandidateGeneration, state.Phase + 1);
    private SqlCipherGenerationMigrationSession Session() => new(initial, Guid.NewGuid(), Original, Journal, keys,
        durability, () => { if (retired) throw new InvalidOperationException("Retired fixture lease."); }, 1024 * 1024, "4.19.0");

    /// <summary>Creates only an independently owned fixture directory.</summary>
    public SqlCipherGenerationMigrationSessionTests() => Directory.CreateDirectory(root);

    private async Task<DatabaseGenerationCheckpoint> InitializeAsync(int advances = 0)
    {
        await Journal.CreateNewAsync(Ct);
        var state = initial;
        for (var i = 0; i < advances; i++) { var next = Next(state); await Journal.CompareAndCommitAsync(state, next, Ct); state = next; }
        await using var original = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Original, Pooling = false }.ToString());
        await original.OpenAsync(Ct);
        using var command = original.CreateCommand(); command.CommandText = "CREATE TABLE packet(amount INTEGER); INSERT INTO packet VALUES(123)";
        await command.ExecuteNonQueryAsync(Ct);
        return state;
    }

    [Fact, Trait("AT", "AT-195")]
    public async Task Candidate_before_key_preparation_never_generates_a_replacement_key()
    {
        await InitializeAsync(); var session = Session(); File.WriteAllBytes(session.CandidatePath, [1]);
        await Assert.ThrowsAsync<IOException>(() => session.PrepareKeyAsync(initial, Ct));
        Assert.Equal(0, keys.Prepared); Assert.Equal(initial, await Journal.ReadAsync(Ct)); Assert.True(File.Exists(Original));
    }

    [Fact]
    public async Task Retired_lease_after_key_await_does_not_read_key_or_record_readiness()
    {
        await InitializeAsync(); var session = Session(); keys.AfterPrepare = () => retired = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.PrepareKeyAsync(initial, Ct));
        Assert.Equal(0, keys.Reads); Assert.Equal(initial, await Journal.ReadAsync(Ct));
    }

    [Fact]
    public async Task Invalid_returned_key_is_cleared_before_preparation_fails()
    {
        await InitializeAsync(); keys.Buffer = Enumerable.Repeat((byte)7, 31).ToArray();
        await Assert.ThrowsAsync<CryptographicException>(() => Session().PrepareKeyAsync(initial, Ct));
        Assert.All(keys.Buffer, value => Assert.Equal((byte)0, value)); Assert.Equal(initial, await Journal.ReadAsync(Ct));
    }

    [Fact]
    public async Task Cancelled_key_preparation_does_not_call_the_platform_store()
    {
        await InitializeAsync(); using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Session().PrepareKeyAsync(initial, cancelled.Token));
        Assert.Equal(0, keys.Prepared);
    }

    [Fact]
    public async Task Low_disk_stops_before_key_read_export_or_original_cleanup()
    {
        var state = await InitializeAsync(1); durability.AvailableBytes = 0; var session = Session();
        await Assert.ThrowsAsync<IOException>(() => session.CreateAndVerifyCandidateAsync(state, Ct));
        Assert.Equal(0, keys.Reads); Assert.False(File.Exists(session.CandidatePath)); Assert.True(File.Exists(Original));
    }

    [Fact, Trait("AT", "AT-195")]
    public async Task Default_plain_SQLite_provider_cannot_silently_export_an_encrypted_generation()
    {
        var state = await InitializeAsync(1); var session = Session();
        await Assert.ThrowsAsync<InvalidDataException>(() => session.CreateAndVerifyCandidateAsync(state, Ct));
        Assert.Equal(0, keys.Reads); Assert.False(File.Exists(session.CandidatePath)); Assert.Equal(state, await Journal.ReadAsync(Ct));
    }

    [Fact]
    public async Task Missing_selected_key_does_not_prepare_a_key_or_open_original_as_fallback()
    {
        var selected = await InitializeAsync(3); var session = Session(); keys.Missing = true;
        await Assert.ThrowsAsync<CryptographicException>(() => session.ReopenCandidateAsync(selected, Ct));
        Assert.Equal(0, keys.Prepared); Assert.True(File.Exists(Original)); Assert.Equal(selected, await Journal.ReadAsync(Ct));
    }

    [Fact]
    public async Task Plaintext_selected_candidate_is_refused_before_original_cleanup()
    {
        var selected = await InitializeAsync(3); var session = Session(); File.Copy(Original, session.CandidatePath);
        await Assert.ThrowsAsync<InvalidDataException>(() => session.CleanupOriginalAsync(selected, Ct));
        Assert.True(File.Exists(Original)); Assert.Equal(0, durability.Directories); Assert.Equal(selected, await Journal.ReadAsync(Ct));
        Assert.All(keys.Buffer, value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public async Task Cleanup_before_durable_selection_does_not_touch_any_owned_file()
    {
        var state = await InitializeAsync(2); var session = Session();
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.CleanupOriginalAsync(state, Ct));
        Assert.True(File.Exists(Original)); Assert.Equal(0, keys.Reads);
    }

    [Fact]
    public async Task Foreign_effect_identity_is_rejected_before_platform_key_access()
    {
        await InitializeAsync(); var foreign = new DatabaseGenerationCheckpoint(Guid.NewGuid(), initial.OriginalGeneration, initial.CandidateGeneration);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Session().PrepareKeyAsync(foreign, Ct));
        Assert.Equal(0, keys.Prepared);
    }

    /// <summary>Deletes only the owned fixture tree, with no pooled connections.</summary>
    public void Dispose() => Directory.Delete(root, recursive: true);

    private sealed class Keys : IDatabaseGenerationKeyStore
    {
        internal byte[] Buffer = new byte[32];
        internal int Prepared, Reads;
        internal bool Missing;
        internal Action? AfterPrepare;
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken)
        { Prepared++; AfterPrepare?.Invoke(); return Task.CompletedTask; }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken)
        { Reads++; if (Missing) throw new CryptographicException("Missing fictitious key."); return Task.FromResult(Buffer); }
    }

    private sealed class Durability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes { get; set; } = 1024 * 1024 * 1024;
        internal int Directories;
        public Task SynchronizeFileAsync(string path, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SynchronizeDirectoryAsync(CancellationToken cancellationToken) { Directories++; return Task.CompletedTask; }
    }
}
