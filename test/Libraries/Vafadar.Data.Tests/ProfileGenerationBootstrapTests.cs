using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Data.Tests;

/// <summary>Checks existing metadata/ownership/journal guards before native key work.</summary>
public sealed class ProfileGenerationBootstrapTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-206")]
    public async Task Journal_initialization_reuses_actual_metadata_and_cannot_replace_existing_state()
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var identities = new SqliteDatabaseIdentityCatalogue(db.PathOf("identities.sqlite"), owner.Validate);
        await identities.CreateNewAsync([""], Ct); var initial = Assert.Single(await identities.ReadAsync(Ct));
        await SqlCipherProfileGeneration.InitializeJournalAsync(location, identities, "", location.Path, owner, () => { }, Ct);
        Assert.Equal(initial.InitialMigration, await new SqliteDatabaseGenerationJournal(location.Path + ".generation.sqlite", initial.InitialMigration).ReadAsync(Ct));
        await Assert.ThrowsAsync<IOException>(() => SqlCipherProfileGeneration.InitializeJournalAsync(location, identities, "", location.Path, owner, () => { }, Ct));
    }

    [Fact, Trait("AT", "AT-206")]
    public async Task Missing_journal_cannot_prepare_keys_or_generate_a_replacement()
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var identities = new SqliteDatabaseIdentityCatalogue(db.PathOf("identities.sqlite"), owner.Validate);
        await identities.CreateNewAsync([""], Ct);
        await Assert.ThrowsAsync<IOException>(() => SqlCipherProfileGeneration.OpenInitialAsync(location, identities, "", location.Path,
            new NoKeys(), new NoDurability(), owner, () => { }, 1024 * 1024, "4.19.0", Ct));
        Assert.False(File.Exists(location.Path + ".generation.sqlite")); Assert.True(File.Exists(location.Path));
    }

    [Fact, Trait("AT", "AT-206")]
    public async Task Foreign_maintenance_owner_fails_before_reading_or_creating_metadata()
    {
        using var db = new TestDatabase(); using var owner = await new DatabaseAccessGate().AcquireExclusiveAsync(Ct);
        var identities = new SqliteDatabaseIdentityCatalogue(db.PathOf("missing.sqlite"), () => { });
        await Assert.ThrowsAsync<InvalidOperationException>(() => SqlCipherProfileGeneration.InitializeJournalAsync(Location(db), identities, "", Location(db).Path, owner, () => { }, Ct));
        Assert.False(File.Exists(db.PathOf("missing.sqlite")));
    }

    [Fact, Trait("AT", "AT-206")]
    public async Task Existing_ciphertext_or_another_endpoint_cannot_be_treated_as_fresh_plaintext()
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var identities = new SqliteDatabaseIdentityCatalogue(db.PathOf("identities.sqlite"), owner.Validate); await identities.CreateNewAsync([""], Ct);
        var identity = Assert.Single(await identities.ReadAsync(Ct));
        var candidate = location.Path + "." + identity.CandidateGeneration.ToString("N") + ".cipher.db";
        await File.WriteAllBytesAsync(candidate, new byte[16], Ct);
        await Assert.ThrowsAsync<IOException>(() => SqlCipherProfileGeneration.InitializeJournalAsync(location, identities, "", location.Path, owner, () => { }, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => SqlCipherProfileGeneration.InitializeJournalAsync(location, identities, "", db.PathOf("foreign.db"), owner, () => { }, Ct));
        Assert.False(File.Exists(location.Path + ".generation.sqlite"));
    }

    private static LocalDatabaseLocation<TestDbContext> Location(TestDatabase db) => db.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-211")]
    public async Task Current_resolution_never_treats_missing_or_foreign_history_as_initial_selection(bool foreignInitial)
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var identities = new SqliteDatabaseIdentityCatalogue(db.PathOf("identities.sqlite"), owner.Validate);
        await identities.CreateNewAsync([""], Ct); var identity = Assert.Single(await identities.ReadAsync(Ct));
        var initial = foreignInitial ? new DatabaseKeyBinding(identity.ProfileId, identity.InstallationId, Guid.NewGuid()) : identity.InitialCandidateKey;
        var history = new SqliteProfileRestoreHistory(db.PathOf("restore-history.sqlite"), initial, owner.Validate);
        if (foreignInitial) await history.CreateNewAsync(Ct);
        async Task Open() => _ = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", location.Path, history,
            _ => throw new InvalidOperationException("Unexpected receiver construction."), new NoKeys(), new NoDurability(), owner, () => { },
            1024 * 1024, "4.19.0", Ct);
        if (foreignInitial) await Assert.ThrowsAsync<InvalidOperationException>(Open);
        else await Assert.ThrowsAsync<IOException>(Open);
        Assert.True(File.Exists(location.Path)); Assert.False(File.Exists(location.Path + ".generation.sqlite"));
    }

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-210")]
    public async Task Restored_owner_rejects_foreign_history_or_installation_before_native_effects(bool foreignInstallation)
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var initial = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var history = new SqliteProfileRestoreHistory(db.PathOf("restore-history.sqlite"), initial, owner.Validate);
        await history.CreateNewAsync(Ct);
        var captured = new DatabaseGenerationCheckpoint(initial.ProfileId, initial.GenerationId, Guid.NewGuid());
        await history.ReserveAsync(captured, Ct);
        var binding = new DatabaseKeyBinding(initial.ProfileId, foreignInstallation ? Guid.NewGuid() : initial.InstallationId, captured.CandidateGeneration);
        var receiverHistory = foreignInstallation ? history : new SqliteProfileRestoreHistory(db.PathOf("other-history.sqlite"), initial, owner.Validate);
        var receiver = new SqlCipherPackageRestoreReceiver(captured,
            new SqliteDatabaseGenerationJournal(db.PathOf("restore-journal.sqlite"), captured), db.PathOf("candidate.db"),
            binding, new NoKeys(), new NoDurability(), new Vafadar.Core.Settings.InMemorySettingsStore(), ["language"],
            _ => throw new InvalidOperationException("Unexpected current open."),
            (_, _) => throw new InvalidOperationException("Unexpected upgrade."),
            _ => throw new InvalidOperationException("Unexpected publication."), owner.Validate, 1024 * 1024, "4.19.0",
            _ => throw new InvalidOperationException("Unexpected persistence."), receiverHistory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => SqlCipherProfileGeneration.OpenRestoredAsync(location, history,
            receiver, new NoKeys(), owner, () => { }, "4.19.0", Ct));
        Assert.True(File.Exists(location.Path)); Assert.False(File.Exists(db.PathOf("candidate.db")));
        Assert.False(File.Exists(db.PathOf("restore-journal.sqlite")));
        Assert.False((await history.ReadAsync(Ct))[^1].Selected);
    }
    private sealed class NoKeys : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new InvalidOperationException("Unexpected key preparation.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new InvalidOperationException("Unexpected key read.");
    }
    private sealed class NoDurability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => throw new InvalidOperationException("Unexpected storage effect.");
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => throw new InvalidOperationException("Unexpected synchronization.");
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => throw new InvalidOperationException("Unexpected synchronization.");
    }
}
