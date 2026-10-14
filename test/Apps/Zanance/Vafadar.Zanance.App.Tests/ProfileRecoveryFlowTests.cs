using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual application recovery captures all device profiles independently of commercial selection.</summary>
public sealed class ProfileRecoveryFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-242")]
    public async Task Every_original_is_verified_before_first_native_import_without_catalogue_or_endpoint_publication()
    {
        using var f = new Fixture(); await f.InitializeAsync(); var verified = 0;
        var recovery = f.Service((_, _, _, _, _) => { verified++; return Task.CompletedTask; });
        f.Native.BeforeImport = () => Assert.True(verified >= 2);
        var before = f.Profiles.CaptureStorage(); var endpoint = f.Location.Path;
        using var owner = await f.Location.Access.AcquireExclusiveAsync(Ct);
        await recovery.RecoverAsync(new byte[32], owner, Ct);
        Assert.Equal(2, f.Native.Imported.Count); Assert.Equal(endpoint, f.Location.Path); before.Validate();
        Assert.Contains(f.Main.InitialCandidateKey, f.Native.Imported); Assert.Contains(f.Work.InitialCandidateKey, f.Native.Imported);
    }

    [Fact, Trait("AT", "AT-242")]
    public async Task Invalid_second_profile_stops_all_native_writes_after_complete_preflight_failure()
    {
        using var f = new Fixture(); await f.InitializeAsync(); var checkedProfiles = 0;
        var recovery = f.Service((_, _, _, _, _) => { if (++checkedProfiles == 2) throw new IOException("Fictitious damaged profile."); return Task.CompletedTask; });
        using var owner = await f.Location.Access.AcquireExclusiveAsync(Ct);
        await Assert.ThrowsAsync<IOException>(() => recovery.RecoverAsync(new byte[32], owner, Ct));
        Assert.Empty(f.Native.Imported); Assert.False(f.Location.IsProtected);
    }

    [Fact, Trait("AT", "AT-242")]
    public async Task Missing_profile_mapping_refuses_unlock_and_never_infers_empty_or_fresh_profile()
    {
        using var f = new Fixture(); await f.InitializeAsync(includeWork: false); var unlocks = 0;
        var recovery = f.Service((_, _, _, _, _) => Task.CompletedTask, () => unlocks++);
        using var owner = await f.Location.Access.AcquireExclusiveAsync(Ct);
        await Assert.ThrowsAsync<IOException>(() => recovery.RecoverAsync(new byte[32], owner, Ct));
        Assert.Equal(0, unlocks); Assert.Empty(f.Native.Imported); Assert.Equal(2, f.Profiles.All.Count);
    }

    [Fact, Trait("AT", "AT-242")]
    public async Task Catalogue_change_during_verification_prevents_native_import_instead_of_overwriting_names()
    {
        using var f = new Fixture(); await f.InitializeAsync();
        var recovery = f.Service((_, _, _, _, _) => { f.Profiles.Rename(f.Profiles.Current, "Later original name"); return Task.CompletedTask; });
        using var owner = await f.Location.Access.AcquireExclusiveAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => recovery.RecoverAsync(new byte[32], owner, Ct));
        Assert.Empty(f.Native.Imported); Assert.Equal("Later original name", f.Profiles.Current.Name);
    }
    private sealed class Fixture : IDisposable
    {
        private readonly FlowFixture flow = new();
        internal readonly Native Native = new();
        internal LocalDatabaseLocation<ZananceDbContext> Location => flow.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        internal ProfileService Profiles = null!; internal DatabaseProfileIdentity Main = null!, Work = null!;
        private DatabaseRecoveryMaterial material = null!;
        private SqliteRecoveryEnvelopeCatalogue catalogue = null!;
        private string IdentityPath => flow.Directory.Combine("identities.sqlite");
        internal async Task InitializeAsync(bool includeWork = true)
        {
            Profiles = new(Location, flow.Store, flow.Preferences, new Runtime(), flow.Translator);
            var work = Profiles.Create("Fictitious retained work");
            var identities = new SqliteDatabaseIdentityCatalogue(IdentityPath, () => { }); await identities.CreateNewAsync(["", work.Id], Ct);
            var rows = await identities.ReadAsync(Ct); Main = rows.Single(row => row.LegacyProfileId.Length == 0); Work = rows.Single(row => row.LegacyProfileId == work.Id);
            material = DatabaseRecoveryMaterial.Create(Main.InstallationId);
            catalogue = new(flow.Directory.Combine("recovery.sqlite"), material.Identity, new Durability(), () => { }); await catalogue.CreateNewAsync(Ct);
            foreach (var binding in includeWork ? new[] { Main.InitialCandidateKey, Work.InitialCandidateKey } : [Main.InitialCandidateKey])
            {
                var key = RandomNumberGenerator.GetBytes(32);
                try { await catalogue.CaptureAsync(binding, key, Ct); } finally { CryptographicOperations.ZeroMemory(key); }
            }
        }
        internal ProtectedProfileRecovery Service(Func<DatabaseKeyBinding, string, ReadOnlyMemory<byte>, DatabaseAccessGate.ExclusiveLease, CancellationToken, Task> verify, Action? unlocked = null) =>
            new(Location, Profiles, IdentityPath, (_, _) => { unlocked?.Invoke(); return Task.FromResult(material); }, catalogue, Native, verify);
        public void Dispose() { material?.Dispose(); flow.Dispose(); }
    }
    private sealed class Native : IDatabaseGenerationKeyRecovery
    {
        internal readonly List<DatabaseKeyBinding> Imported = []; internal Action? BeforeImport;
        public async Task ImportRecoveredAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> key,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> verify, CancellationToken ct)
        { BeforeImport?.Invoke(); await verify(key, ct); Imported.Add(binding); }
    }
    private sealed class Durability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => Task.CompletedTask;
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class Runtime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => action();
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new NotSupportedException();
        public Task<bool> AuthenticateAsync(string reason) => throw new NotSupportedException();
        public Task ReloadLockAsync() => throw new NotSupportedException();
        public void DismissUndo() { }
    }
}
