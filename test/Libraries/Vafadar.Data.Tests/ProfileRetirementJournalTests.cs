using Microsoft.Data.Sqlite;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks actual SQLite retirement barriers without pretending file deletion or catalogue publication occurred.</summary>
public sealed class ProfileRetirementJournalTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static ProfileRetirementCheckpoint Identity() => new(Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Windows_long_identity_path_commits_and_reopens_with_transaction_sidecars()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var folder = new TemporaryDirectory(); var captured = Identity();
        var nested = folder.Combine(new string('a', 90)); Directory.CreateDirectory(nested);
        var path = Path.Combine(nested, $"profile-retirement-{captured.LegacyProfileId}-{captured.ProfileId:N}-{captured.InstallationId:N}-{captured.OperationId:N}.sqlite");
        Assert.True(path.Length > 260);
        var journal = new SqliteProfileRetirementJournal(path, captured, () => { });
        await journal.CreateNewAsync(new Effects(), Ct);
        Assert.Equal(captured, await journal.ReadAsync(Ct));
    }

    [Fact]
    public async Task Exact_request_reopens_and_advances_each_actual_barrier_once()
    {
        using var folder = new TemporaryDirectory(); var captured = Identity(); var effects = new Effects();
        var path = folder.Combine("retirement.sqlite"); var journal = new SqliteProfileRetirementJournal(path, captured, () => { });
        await journal.CreateNewAsync(effects, Ct);
        var state = await journal.ReadAsync(Ct); Assert.Equal(captured, state);
        for (var index = 0; index < 3; index++)
        {
            await journal.CompareAndAdvanceAsync(state, effects, Ct); state = state.Advance();
            Assert.Equal(state, await new SqliteProfileRetirementJournal(path, captured, () => { }).ReadAsync(Ct));
        }
        Assert.Equal(ProfileRetirementPhase.Completed, state.Phase);
        await Assert.ThrowsAsync<InvalidOperationException>(() => journal.CompareAndAdvanceAsync(state, effects, Ct));
        Assert.True(effects.Files >= 4); Assert.True(effects.Directories >= 4);
    }

    [Fact]
    public async Task Stale_or_foreign_request_cannot_advance_committed_state()
    {
        using var folder = new TemporaryDirectory(); var captured = Identity(); var effects = new Effects();
        var journal = new SqliteProfileRetirementJournal(folder.Combine("retirement.sqlite"), captured, () => { });
        await journal.CreateNewAsync(effects, Ct); await journal.CompareAndAdvanceAsync(captured, effects, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => journal.CompareAndAdvanceAsync(captured, effects, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => journal.CompareAndAdvanceAsync(Identity(), effects, Ct));
        Assert.Equal(captured.Advance(), await journal.ReadAsync(Ct));
    }

    [Fact]
    public async Task Missing_or_other_profile_state_never_manufactures_retirement_authority()
    {
        using var folder = new TemporaryDirectory(); var captured = Identity(); var path = folder.Combine("retirement.sqlite");
        var journal = new SqliteProfileRetirementJournal(path, captured, () => { });
        await Assert.ThrowsAsync<IOException>(() => journal.ReadAsync(Ct)); Assert.False(File.Exists(path));
        await journal.CreateNewAsync(new Effects(), Ct);
        await Assert.ThrowsAsync<IOException>(() => new SqliteProfileRetirementJournal(path, Identity(), () => { }).ReadAsync(Ct));
        await Assert.ThrowsAsync<IOException>(() => journal.CreateNewAsync(new Effects(), Ct));
        Assert.Equal(captured, await journal.ReadAsync(Ct));
    }

    [Fact]
    public async Task Extra_schema_and_retired_access_are_rejected()
    {
        using var folder = new TemporaryDirectory(); var captured = Identity(); var path = folder.Combine("retirement.sqlite"); var retired = false;
        var journal = new SqliteProfileRetirementJournal(path, captured, () => { if (retired) throw new InvalidOperationException("Retired fixture."); });
        await journal.CreateNewAsync(new Effects(), Ct);
        retired = true; await Assert.ThrowsAsync<InvalidOperationException>(() => journal.ReadAsync(Ct)); retired = false;
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
        { connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "CREATE TABLE sqliteAlias(value INTEGER)"; command.ExecuteNonQuery(); }
        await Assert.ThrowsAsync<IOException>(() => journal.ReadAsync(Ct));
    }

    [Theory, InlineData(""), InlineData("../outside"), InlineData("00000000000000000000000000000000")]
    public void Main_or_noncanonical_profile_cannot_be_a_retirement_request(string legacy) =>
        Assert.Throws<ArgumentException>(() => new ProfileRetirementCheckpoint(legacy, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

    [Fact, Trait("AT", "AT-236")]
    public async Task Reviewed_catalogue_digests_are_atomic_with_request_and_staged_retry_never_replaces_them()
    {
        using var folder = new TemporaryDirectory(); var identity = Identity(); var path = folder.Combine("retirement.sqlite");
        var journal = new SqliteProfileRetirementJournal(path, identity, () => { });
        var digest = new ProfileRetirementCatalogueDigest(new string('a', 64), new string('b', 64));
        await Assert.ThrowsAsync<IOException>(() => journal.CreateNewAsync(digest, new AlwaysFailSync(), Ct));
        Assert.False(File.Exists(path));
        Assert.Equal(digest, await new SqliteProfileRetirementJournal(path + ".creating", identity, () => { }).ReadCatalogueAsync(Ct));
        await Assert.ThrowsAsync<IOException>(() => journal.CreateNewAsync(new(new string('c', 64), new string('d', 64)), new Effects(), Ct));
        await journal.CreateNewAsync(digest, new Effects(), Ct);
        Assert.Equal(identity, await journal.ReadAsync(Ct)); Assert.Equal(digest, await journal.ReadCatalogueAsync(Ct));
        Assert.False(await journal.HasArtifactInventoryAsync(Ct));
    }

    private sealed class AlwaysFailSync : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => throw new IOException("Injected staged publication interruption.");
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => throw new IOException("Unexpected final publication.");
    }

    private sealed class Effects : IDatabaseGenerationFileDurability
    {
        internal int Files, Directories;
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) { Files++; Assert.True(File.Exists(path)); return Task.CompletedTask; }
        public Task SynchronizeDirectoryAsync(CancellationToken ct) { Directories++; return Task.CompletedTask; }
    }

    [Fact]
    public async Task Failed_native_barrier_never_authorizes_cleanup_until_requalified()
    {
        using var folder = new TemporaryDirectory(); var identity = Identity();
        var durability = new FaultDurability(); var host = new RetirementEffects(durability);
        var journal = new SqliteProfileRetirementJournal(folder.Combine("retirement.sqlite"), identity, () => { });
        await journal.CreateNewAsync(durability, Ct);
        await journal.CaptureArtifactsAsync([], durability, Ct);
        var coordinator = new ProfileRetirementCoordinator(journal, durability, host);
        await Assert.ThrowsAsync<IOException>(() => coordinator.ResumeAsync(Ct));
        Assert.Equal(ProfileRetirementPhase.CatalogueRemoved, (await journal.ReadAsync(Ct)).Phase);
        Assert.Equal(0, host.ArtifactRemovals); Assert.Equal(0, host.IdentityRetirements);
        // The committed phase is observable, but a second failed native flush still blocks cleanup.
        await Assert.ThrowsAsync<IOException>(() => coordinator.ResumeAsync(Ct));
        Assert.Equal(0, host.ArtifactRemovals);
        durability.Fail = false; host.FailAfterCatalogue = false;
        await coordinator.ResumeAsync(Ct); await coordinator.ResumeAsync(Ct);
        Assert.Equal(1, host.CatalogueRemovals); Assert.Equal(1, host.ArtifactRemovals);
        Assert.Equal(1, host.IdentityRetirements);
        Assert.Equal(ProfileRetirementPhase.Completed, (await journal.ReadAsync(Ct)).Phase);
    }

    [Fact]
    public async Task Missing_intent_and_changed_host_state_never_start_native_effects()
    {
        using var folder = new TemporaryDirectory(); var identity = Identity(); var durability = new FaultDurability();
        var host = new RetirementEffects(durability) { FailAfterCatalogue = false };
        var journal = new SqliteProfileRetirementJournal(folder.Combine("retirement.sqlite"), identity, () => { });
        var coordinator = new ProfileRetirementCoordinator(journal, durability, host);
        await Assert.ThrowsAsync<IOException>(() => coordinator.ResumeAsync(Ct));
        Assert.Equal(0, host.CatalogueRemovals);
        await journal.CreateNewAsync(durability, Ct); await journal.CaptureArtifactsAsync([], durability, Ct); host.Reject = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ResumeAsync(Ct));
        Assert.Equal(0, host.CatalogueRemovals); Assert.Equal(identity, await journal.ReadAsync(Ct));
    }

    private sealed class FaultDurability : IDatabaseGenerationFileDurability
    {
        internal bool Fail;
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct)
        { if (Fail) throw new IOException("Injected native synchronization failure."); return Task.CompletedTask; }
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class RetirementEffects(FaultDurability durability) : IProfileRetirementEffects
    {
        internal bool FailAfterCatalogue = true, Reject;
        internal int CatalogueRemovals, ArtifactRemovals, IdentityRetirements;
        public Task ValidateAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        {
            if (Reject) throw new InvalidOperationException("Captured ownership changed.");
            if (checkpoint.Phase >= ProfileRetirementPhase.CatalogueRemoved) Assert.Equal(1, CatalogueRemovals);
            if (checkpoint.Phase >= ProfileRetirementPhase.ArtifactsRemoved) Assert.Equal(1, ArtifactRemovals);
            if (checkpoint.Phase == ProfileRetirementPhase.Completed) Assert.Equal(1, IdentityRetirements);
            return Task.CompletedTask;
        }
        public Task RemoveCatalogueAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        { CatalogueRemovals = 1; durability.Fail = FailAfterCatalogue; return Task.CompletedTask; }
        public Task RemoveArtifactsAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        { ArtifactRemovals = 1; return Task.CompletedTask; }
        public Task RetireIdentityAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        { IdentityRetirements = 1; return Task.CompletedTask; }
    }
}
