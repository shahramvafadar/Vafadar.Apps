using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks existing metadata/ownership/journal guards before native key work.</summary>
public sealed class ProfileGenerationBootstrapTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-290")]
    public async Task Selected_publisher_cannot_claim_success_without_a_verified_retained_generation()
    {
        using var db = new TestDatabase(); var location = Location(db);
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var identityPath = db.PathOf("data/identities.sqlite");
        var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
        await identities.CreateNewAsync([""], Ct); var identity = Assert.Single(await identities.ReadAsync(Ct));
        var journal = new SqliteDatabaseGenerationJournal(location.Path + ".generation.sqlite", identity.InitialMigration);
        await journal.CreateNewAsync(Ct);
        await new SqliteProfileRestoreHistory(location.Path + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate)
            .CreateInitialIntentAsync(identity.InitialMigration, journal, Ct);
        var invoked = false;
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SqlCipherProfileBootstrap.PrepareAsync(location, owner,
            identityPath, new Dictionary<string, string> { [""] = location.Path }, false, new NoKeys(), new NoDurability(),
            (_, _) => throw new InvalidOperationException("Unexpected receiver."), () => { }, 1024 * 1024, "4.19.0", Ct,
            selectedOriginalPath: location.Path, publishSelected: () => { invoked = true; return Task.CompletedTask; }));
        Assert.True(invoked); Assert.Equal("Selected publisher did not retain the verified captured profile.", error.Message);
        Assert.False(location.IsProtected); Assert.True(File.Exists(location.Path));
    }

    [Theory, InlineData("missing-publisher"), InlineData("missing-selected"), InlineData("foreign-selected"), Trait("AT", "AT-290")]
    public async Task Selected_publication_requires_the_captured_current_original_and_publisher_pair(string scenario)
    {
        using var db = new TestDatabase(); var location = Location(db);
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var identityPath = db.PathOf("data/identities.sqlite");
        var selected = scenario == "missing-selected" ? null : scenario == "foreign-selected" ? db.PathOf("data/foreign.db") : location.Path;
        Func<Task>? publish = scenario == "missing-publisher" ? null : () => throw new InvalidOperationException("Unexpected publication.");
        await Assert.ThrowsAsync<ArgumentException>(() => SqlCipherProfileBootstrap.PrepareAsync(location, owner, identityPath,
            new Dictionary<string, string> { [""] = location.Path }, false, new NoKeys(), new NoDurability(),
            (_, _) => throw new InvalidOperationException("Unexpected receiver."), () => { }, 1024 * 1024, "4.19.0", Ct,
            selectedOriginalPath: selected, publishSelected: publish));
        Assert.False(File.Exists(identityPath)); Assert.True(File.Exists(location.Path));
    }

    [Fact, Trait("AT", "AT-227")]
    public async Task All_profile_preparation_retires_only_the_captured_original_pool_before_native_work()
    {
        using var db = new TestDatabase(); using var other = new TestDatabase();
        var location = Location(db); var foreign = Location(other);
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var path = db.PathOf("data/identities.sqlite");
        var identities = new SqliteDatabaseIdentityCatalogue(path, owner.Validate); await identities.CreateNewAsync([""], Ct);
        var target = new SqliteConnectionStringBuilder { DataSource = location.Path }.ToString();
        var unrelated = new SqliteConnectionStringBuilder { DataSource = foreign.Path }.ToString();
        SqlitePoolProbe.Mark(target); SqlitePoolProbe.Mark(unrelated);
        await Assert.ThrowsAsync<InvalidOperationException>(() => SqlCipherProfileBootstrap.PrepareAsync(location, owner, path,
            new Dictionary<string, string> { [""] = location.Path }, false, new NoKeys(), new NoDurability(),
            (_, _) => throw new InvalidOperationException("Unexpected receiver."), () => { }, 1024 * 1024, "4.19.0", Ct));
        Assert.Null(SqlitePoolProbe.Read(target));
        Assert.Equal("retained", SqlitePoolProbe.Read(unrelated));
        Assert.True(File.Exists(location.Path));
    }

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

    [Theory, InlineData("existing-original"), InlineData("missing-intent"), InlineData("unknown-ciphertext"), Trait("AT", "AT-219")]
    public async Task Empty_profile_creation_never_replaces_existing_data_or_guesses_missing_intent(string scenario)
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var identities = new SqliteDatabaseIdentityCatalogue(db.PathOf("identities.sqlite"), owner.Validate);
        await identities.CreateNewAsync([""], Ct); var identity = Assert.Single(await identities.ReadAsync(Ct));
        var original = scenario == "existing-original" ? location.Path : Path.Combine(Path.GetDirectoryName(location.Path)!, "new.db");
        var candidate = original + "." + identity.CandidateGeneration.ToString("N") + ".cipher.db";
        if (scenario == "unknown-ciphertext") await File.WriteAllBytesAsync(candidate, [1, 2, 3], Ct);
        await Assert.ThrowsAsync<IOException>(async () => _ = await SqlCipherNewProfile.OpenAsync(location, identities, "", original,
            scenario != "missing-intent", new NoKeys(), new NoDurability(), (_, _) => throw new InvalidOperationException("Unexpected schema construction."),
            owner, () => { }, 1024 * 1024, "4.19.0", Ct));
        Assert.True(File.Exists(location.Path)); Assert.False(File.Exists(original + ".new-profile.sqlite"));
        Assert.False(File.Exists(original + ".generation.sqlite"));
        if (scenario == "unknown-ciphertext") Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(candidate, Ct));
    }

    [Fact, Trait("AT", "AT-217")]
    public async Task Missing_installation_with_existing_migration_state_cannot_generate_replacement_bindings()
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var metadata = Path.Combine(Path.GetDirectoryName(location.Path)!, "missing-identities.sqlite");
        await File.WriteAllBytesAsync(location.Path + ".generation.sqlite", [1, 2, 3], Ct);
        await Assert.ThrowsAsync<IOException>(() => SqlCipherProfileBootstrap.PrepareAsync(location, owner, metadata,
            new Dictionary<string, string> { [""] = location.Path }, true, new NoKeys(), new NoDurability(),
            (_, _) => throw new InvalidOperationException("Unexpected receiver."), () => { }, 1024 * 1024, "4.19.0", Ct));
        Assert.False(File.Exists(metadata)); Assert.True(File.Exists(location.Path));
    }

    [Fact, Trait("AT", "AT-217")]
    public async Task All_profile_bootstrap_never_creates_missing_installation_when_initialization_is_not_authorized()
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var metadata = Path.Combine(Path.GetDirectoryName(location.Path)!, "missing-identities.sqlite");
        await Assert.ThrowsAsync<IOException>(() => SqlCipherProfileBootstrap.PrepareAsync(location, owner, metadata,
            new Dictionary<string, string> { [""] = location.Path }, false, new NoKeys(), new NoDurability(),
            (_, _) => throw new InvalidOperationException("Unexpected receiver."), () => { }, 1024 * 1024, "4.19.0", Ct));
        Assert.False(File.Exists(metadata)); Assert.True(File.Exists(location.Path));
    }

    [Fact, Trait("AT", "AT-217")]
    public async Task All_profile_bootstrap_rejects_two_identities_for_the_same_original_before_any_metadata_or_key_work()
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var metadata = Path.Combine(Path.GetDirectoryName(location.Path)!, "missing-identities.sqlite");
        await Assert.ThrowsAsync<ArgumentException>(() => SqlCipherProfileBootstrap.PrepareAsync(location, owner, metadata,
            new Dictionary<string, string> { [""] = location.Path, [Guid.NewGuid().ToString("N")] = location.Path }, true,
            new NoKeys(), new NoDurability(), (_, _) => throw new InvalidOperationException("Unexpected receiver."),
            () => { }, 1024 * 1024, "4.19.0", Ct));
        Assert.False(File.Exists(metadata)); Assert.True(File.Exists(location.Path));
    }

    [Fact, Trait("AT", "AT-217")]
    public async Task Changed_captured_profile_set_fails_before_migration_and_preserves_original_data()
    {
        using var db = new TestDatabase(); var location = Location(db); using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var metadata = Path.Combine(Path.GetDirectoryName(location.Path)!, "identities.sqlite");
        await new SqliteDatabaseIdentityCatalogue(metadata, owner.Validate).CreateNewAsync(["", Guid.NewGuid().ToString("N")], Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => SqlCipherProfileBootstrap.PrepareAsync(location, owner, metadata,
            new Dictionary<string, string> { [""] = location.Path }, false, new NoKeys(), new NoDurability(),
            (_, _) => throw new InvalidOperationException("Unexpected receiver."), () => { }, 1024 * 1024, "4.19.0", Ct));
        Assert.True(File.Exists(location.Path)); Assert.False(File.Exists(location.Path + ".generation.sqlite"));
    }

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
