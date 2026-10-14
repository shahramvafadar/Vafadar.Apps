using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Checks host authority boundaries through original app sources, without pretending ordinary SQLite is a cipher.</summary>
public sealed class ProtectedProfileStorageTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-254")]
    public void Construction_is_lazy_and_refuses_registration_that_could_open_plaintext()
    {
        using var f = new FlowFixture();
        var unprotected = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var resolved = 0;
        Assert.Throws<InvalidOperationException>(() => Create(f, unprotected, () => { resolved++; throw new IOException("Unexpected profile resolution."); }));
        var location = new LocalDatabaseLocation<ZananceDbContext>(unprotected.Path, protectionRequired: true);
        _ = Create(f, location, () => { resolved++; throw new IOException("Unexpected profile resolution."); });
        Assert.Equal(0, resolved);
        Assert.False(File.Exists(f.Directory.Combine("identities.sqlite")));
    }

    [Fact, Trait("AT", "AT-254")]
    public async Task New_profile_without_reviewed_durable_intent_cannot_register_an_identity_or_prepare_a_key()
    {
        using var f = new FlowFixture();
        var location = new LocalDatabaseLocation<ZananceDbContext>(f.Directory.Combine("zanance.db"), protectionRequired: true);
        var profiles = new ProfileService(location, f.Store, f.Preferences, new Runtime(), f.Translator);
        var host = Create(f, location, () => profiles);
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var id = Guid.NewGuid().ToString("N");
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.PrepareNewAsync(id, f.Directory.Combine("zanance-" + id + ".db"), owner, Ct));
        Assert.Single(profiles.All);
        Assert.False(File.Exists(f.Directory.Combine("identities.sqlite")));
        Assert.False(location.IsProtected);
    }

    [Theory, InlineData(true), InlineData(false), Trait("AT", "AT-254")]
    public async Task Reviewed_creation_refuses_a_different_identifier_or_original_before_native_effects(bool changeIdentifier)
    {
        using var f = new FlowFixture();
        var location = new LocalDatabaseLocation<ZananceDbContext>(f.Directory.Combine("zanance.db"), protectionRequired: true);
        var profiles = new ProfileService(location, f.Store, f.Preferences, new Runtime(), f.Translator);
        var host = Create(f, location, () => profiles);
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var id = Guid.NewGuid().ToString("N");
        var request = profiles.CaptureCreation(profiles.ReadWorkReview(), new(id, "Fictitious work"), owner);
        var actualId = changeIdentifier ? Guid.NewGuid().ToString("N") : id;
        var original = changeIdentifier ? f.Directory.Combine("zanance-" + actualId + ".db") : f.Directory.Combine("different.db");
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.PrepareNewAsync(actualId, original, owner, Ct));
        Assert.Equal(request, profiles.ReadCreation(owner));
        Assert.Single(profiles.All);
        Assert.False(File.Exists(f.Directory.Combine("identities.sqlite")));
    }

    [Fact, Trait("AT", "AT-256")]
    public async Task Backup_adapter_refuses_unstarted_storage_and_releases_its_exclusive_lease_without_metadata()
    {
        using var f = new FlowFixture();
        var location = new LocalDatabaseLocation<ZananceDbContext>(f.Directory.Combine("zanance.db"), protectionRequired: true);
        var profiles = new ProfileService(location, f.Store, f.Preferences, new Runtime(), f.Translator);
        var host = Create(f, location, () => profiles);
        var adapter = new Vafadar.Zanance.App.Features.Backup.ProtectedBackupRestoreReceiver(location, host);
        var manifest = new Vafadar.Backup.BackupManifest { FormatVersion = 1, AppId = "pro.vafadar.zanance",
            AppVersion = "0.1", CreatedAt = DateTimeOffset.UnixEpoch, Entries = [] };
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.RestoreAsync(manifest,
            new Dictionary<string, ReadOnlyMemory<byte>>(), Ct));
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        location.Access.RequireDrainedOwner(owner);
        Assert.False(File.Exists(f.Directory.Combine("identities.sqlite")));
        Assert.False(location.IsProtected);
    }

    private static ProtectedProfileStorage Create(FlowFixture f, LocalDatabaseLocation<ZananceDbContext> location, Func<ProfileService> profiles) =>
        new(location, profiles, f.Directory.Combine("identities.sqlite"), new Keys(), new Durability(), () => { },
            (_, _, _) => throw new IOException("Unexpected restore."), 16 * 1024 * 1024, "4.19.0");

    private sealed class Keys : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Unexpected key preparation.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Unexpected key read.");
    }
    private sealed class Durability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => throw new IOException("Unexpected storage effect.");
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => throw new IOException("Unexpected storage effect.");
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => throw new IOException("Unexpected storage effect.");
    }
    private sealed class Runtime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => action();
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new IOException("Unexpected migration.");
        public Task<bool> AuthenticateAsync(string reason) => throw new IOException("Unexpected authentication.");
        public Task ReloadLockAsync() => throw new IOException("Unexpected lock reload.");
        public void DismissUndo() => throw new IOException("Unexpected Undo effect.");
    }
}
