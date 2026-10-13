using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Checks explicit installation authority through actual application startup without substituting native encryption.</summary>
public sealed class ProtectedStartupTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-225")]
    public async Task New_installation_authority_cannot_replace_an_existing_plaintext_original()
    {
        using var f = new FlowFixture(); var location = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        // The fixture's completed SQLite migration keeps a pooled Windows file handle; close only this owned pool.
        Vafadar.Testing.SqliteTestPools.Clear(f.Directory);
        var original = await File.ReadAllBytesAsync(location.Path, Ct);
        var identity = f.Directory.Combine("identities.sqlite");
        var startup = Create(f, identity, ProtectedStorageInitialization.NewInstallation, (_, _) => throw new IOException("Unexpected schema construction."));
        await Assert.ThrowsAsync<IOException>(() => startup.PrepareAsync(Ct));
        Assert.False(File.Exists(identity));
        Assert.Equal(original, await File.ReadAllBytesAsync(location.Path, Ct));
        Assert.False(location.IsProtected);
    }

    [Fact, Trait("AT", "AT-225")]
    public async Task New_installation_requires_a_real_schema_builder_before_creating_metadata()
    {
        using var f = new FlowFixture(); var identity = f.Directory.Combine("identities.sqlite");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(f, identity, ProtectedStorageInitialization.NewInstallation).PrepareAsync(Ct));
        Assert.False(File.Exists(identity));
    }

    [Fact, Trait("AT", "AT-225")]
    public async Task Existing_installation_does_not_infer_new_authority_from_missing_metadata()
    {
        using var f = new FlowFixture(); var identity = f.Directory.Combine("identities.sqlite");
        await Assert.ThrowsAsync<IOException>(() => Create(f, identity, ProtectedStorageInitialization.ExistingInstallation).PrepareAsync(Ct));
        Assert.False(File.Exists(identity));
    }

    [Fact, Trait("AT", "AT-228")]
    public async Task Already_protected_endpoint_is_not_accepted_without_this_startups_prepared_capture()
    {
        using var f = new FlowFixture(); var location = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        location.MoveToProtected(location.Path, () => throw new IOException("Foreign publication must not be opened."), "4.19.0");
        var identity = f.Directory.Combine("identities.sqlite");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(f, identity, ProtectedStorageInitialization.ExistingInstallation).PrepareAsync(Ct));
        Assert.True(location.IsProtected); Assert.False(File.Exists(identity));
    }

    private static ProtectedDatabaseStartup Create(FlowFixture f, string identity, ProtectedStorageInitialization mode,
        Func<Microsoft.Data.Sqlite.SqliteConnection, CancellationToken, Task>? schema = null)
    {
        var location = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var profiles = new ProfileService(location, f.Store, f.Preferences, new Runtime(), f.Translator);
        var session = new SqlCipherProfileSession<ZananceDbContext>(location, (_, _, _) => throw new IOException("Unexpected native publication."));
        return new(location, profiles, session, identity, mode, new Keys(), new Durability(),
            (_, _) => throw new IOException("Unexpected restore."), 16 * 1024 * 1024, "4.19.0", schema);
    }

    private sealed class Keys : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new InvalidOperationException("Unexpected key preparation.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new InvalidOperationException("Unexpected key read.");
    }
    private sealed class Durability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => throw new InvalidOperationException("Unexpected file effects.");
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => throw new InvalidOperationException("Unexpected file effects.");
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => throw new InvalidOperationException("Unexpected file effects.");
    }
    private sealed class Runtime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new IOException("Unexpected profile action.");
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new IOException("Unexpected migration shortcut.");
        public Task<bool> AuthenticateAsync(string reason) => throw new IOException("Unexpected authentication.");
        public Task ReloadLockAsync() => throw new IOException("Unexpected lock reload.");
        public void DismissUndo() => throw new IOException("Unexpected Undo dismissal.");
    }
}
