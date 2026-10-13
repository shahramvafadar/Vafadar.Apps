using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Vafadar.Data.Tests;

/// <summary>Original selection refusal guards; actual rollback/selection uses native Android proof.</summary>
public sealed class SqlCipherRestoreSelectionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vafadar-restore-select-" + Guid.NewGuid().ToString("N"));
    private readonly DatabaseGenerationCheckpoint initial = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    private readonly Guid installation = Guid.NewGuid();
    private readonly Keys keys = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private SqliteDatabaseGenerationJournal Journal => new(Path.Combine(root, "restore.sqlite"), initial);
    /// <summary>Creates only the owned fixture directory.</summary>
    public SqlCipherRestoreSelectionTests() => Directory.CreateDirectory(root);
    private async Task<DatabaseGenerationCheckpoint> Select(DatabaseKeyBinding? binding = null, Action? access = null, CancellationToken? ct = null)
    {
        using var current = new SqliteConnection();
        return await SqlCipherRestoreSelection.SelectAsync(initial, Journal, Path.Combine(root, "candidate.db"),
            binding ?? new(initial.ProfileId, installation, initial.CandidateGeneration), current, keys, new Durability(),
            access ?? (() => { }), 1024 * 1024, "4.19.0", ct ?? Ct);
    }
    [Fact, Trait("AT", "AT-199")]
    public async Task Foreign_binding_stops_before_journal_or_key_access()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Select(new(Guid.NewGuid(), installation, initial.CandidateGeneration)));
        Assert.Equal(0, keys.Reads); Assert.False(File.Exists(Path.Combine(root, "restore.sqlite")));
    }
    [Fact]
    public async Task Missing_journal_never_becomes_a_new_restore_selection()
    { await Assert.ThrowsAsync<IOException>(() => Select()); Assert.Equal(0, keys.Reads); }
    [Fact]
    public async Task Retired_access_refuses_before_key_or_disk_effects()
    { await Assert.ThrowsAsync<InvalidOperationException>(() => Select(access: () => throw new InvalidOperationException("Retired fixture lease."))); Assert.Equal(0, keys.Reads); }
    [Fact]
    public async Task Missing_existing_restore_key_does_not_prepare_a_replacement()
    {
        await Journal.CreateNewAsync(Ct); keys.Missing = true;
        await Assert.ThrowsAsync<CryptographicException>(() => Select()); Assert.Equal(0, keys.Prepares); Assert.Equal(initial, await Journal.ReadAsync(Ct));
    }
    [Fact]
    public async Task Invalid_key_is_cleared_before_candidate_or_selector_changes()
    {
        await Journal.CreateNewAsync(Ct); keys.Buffer = Enumerable.Repeat((byte)7, 31).ToArray();
        await Assert.ThrowsAsync<CryptographicException>(() => Select()); Assert.All(keys.Buffer, value => Assert.Equal((byte)0, value));
        Assert.Equal(initial, await Journal.ReadAsync(Ct));
    }
    [Fact]
    public async Task Cancelled_selection_does_not_read_keys()
    {
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Select(ct: cancelled.Token)); Assert.Equal(0, keys.Reads);
    }
    /// <summary>Removes only fixture-owned files.</summary>
    public void Dispose() => Directory.Delete(root, recursive: true);
    private sealed class Keys : IDatabaseGenerationKeyStore
    {
        internal int Prepares, Reads; internal bool Missing; internal byte[] Buffer = new byte[32];
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) { Prepares++; return Task.CompletedTask; }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct)
        { Reads++; if (Missing) throw new CryptographicException("Missing fictitious key."); return Task.FromResult(Buffer); }
    }
    private sealed class Durability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => 1024L * 1024 * 1024;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => Task.CompletedTask;
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
