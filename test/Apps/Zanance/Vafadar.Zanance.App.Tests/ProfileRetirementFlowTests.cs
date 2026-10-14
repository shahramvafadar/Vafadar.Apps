using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual profile catalogue and native-file retirement flows; settings persistence failures are explicit test effects.</summary>
public sealed class ProfileRetirementFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-236")]
    public async Task Reviewed_inactive_target_uses_real_native_host_and_preserves_main_profile()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        var events = 0; fixture.Profiles.Changed += (_, _) => events++;
        await fixture.Profiles.DeleteAsync(fixture.Work, Ct);
        Assert.True(Assert.Single(fixture.Profiles.All).IsMain); Assert.True(fixture.Profiles.Current.IsMain);
        Assert.Equal(1, fixture.Runtime.ExclusiveRuns); Assert.Equal(1, fixture.Persistence.Writes); Assert.Equal(1, events);
        Assert.False(File.Exists(fixture.Candidate)); Assert.False(File.Exists(fixture.TargetKey));
        Assert.DoesNotContain(await fixture.Identities.ReadAsync(Ct), row => row.ProfileId == fixture.Identity.ProfileId);
        Assert.True(File.Exists(fixture.MainPath));
    }

    [Fact, Trait("AT", "AT-236")]
    public async Task Failed_settings_persistence_retains_files_and_same_request_resumes_after_catalogue_publication()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync(); fixture.Persistence.Fail = true;
        await Assert.ThrowsAsync<IOException>(() => fixture.Profiles.DeleteAsync(fixture.Work, Ct));
        Assert.Single(fixture.Profiles.All); Assert.True(File.Exists(fixture.Candidate)); Assert.True(File.Exists(fixture.TargetKey));
        Assert.Contains(await fixture.Identities.ReadAsync(Ct), row => row.ProfileId == fixture.Identity.ProfileId);
        fixture.Persistence.Fail = false;
        using (var owner = await fixture.Location.Access.AcquireExclusiveAsync(Ct))
            await fixture.Host.ResumePendingAsync(fixture.Profiles, fixture.Location, owner, Ct);
        Assert.False(File.Exists(fixture.Candidate)); Assert.False(File.Exists(fixture.TargetKey));
        Assert.Equal(2, fixture.Persistence.Writes); Assert.Single(fixture.Profiles.All);
        using (var owner = await fixture.Location.Access.AcquireExclusiveAsync(Ct))
            await fixture.Host.ResumePendingAsync(fixture.Profiles, fixture.Location, owner, Ct);
        Assert.Equal(2, fixture.Persistence.Writes);
    }

    [Fact, Trait("AT", "AT-236")]
    public async Task Unrelated_catalogue_change_after_failed_publication_prevents_cleanup()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync(); fixture.Persistence.Fail = true;
        await Assert.ThrowsAsync<IOException>(() => fixture.Profiles.DeleteAsync(fixture.Work, Ct));
        fixture.Profiles.Rename(fixture.Profiles.Current, "Changed main after pending retirement");
        fixture.Persistence.Fail = false;
        using var owner = await fixture.Location.Access.AcquireExclusiveAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Host.ResumePendingAsync(fixture.Profiles, fixture.Location, owner, Ct));
        Assert.True(File.Exists(fixture.Candidate)); Assert.True(File.Exists(fixture.TargetKey));
    }

    [Fact, Trait("AT", "AT-236")]
    public async Task Incomplete_inventory_retries_the_existing_request_instead_of_creating_a_second_operation()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        var unknown = fixture.Candidate + ".future";
        await File.WriteAllTextAsync(unknown, "Retain unknown reference.", Ct);
        await Assert.ThrowsAsync<IOException>(() => fixture.Profiles.DeleteAsync(fixture.Work, Ct));
        Assert.Equal(2, fixture.Profiles.All.Count); Assert.Equal(0, fixture.Persistence.Writes);
        var directory = Path.GetDirectoryName(fixture.MainPath)!;
        Assert.Single(Directory.EnumerateFiles(directory, "profile-retirement-*.sqlite"));
        File.Delete(unknown);
        await fixture.Profiles.DeleteAsync(fixture.Work, Ct);
        Assert.Single(Directory.EnumerateFiles(directory, "profile-retirement-*.sqlite"));
        Assert.Single(fixture.Profiles.All); Assert.False(File.Exists(fixture.Candidate));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly FlowFixture flow = new();
        internal LocalDatabaseLocation<ZananceDbContext> Location => flow.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        internal string MainPath => flow.Directory.Combine("zanance.db");
        internal ProfileService Profiles = null!;
        internal LocalProfile Work = null!;
        internal DatabaseProfileIdentity Identity = null!;
        internal SqliteDatabaseIdentityCatalogue Identities = null!;
        internal ProtectedProfileRetirementHost Host = null!;
        internal string Candidate = null!, TargetKey = null!;
        internal readonly Runtime Runtime = new();
        internal readonly Persistence Persistence = new();
        internal async Task InitializeAsync()
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
            var roots = new Dictionary<string, string> { ["database"] = flow.Directory.Path };
            foreach (var root in new[] { "keys", "portable", "safety" })
            { roots.Add(root, flow.Directory.Combine(root)); Directory.CreateDirectory(roots[root]); }
            var files = new WindowsProfileRetirementFiles(roots, () => { }); var native = new WindowsGenerationFileDurability(flow.Directory.Path);
            var keys = new WindowsGenerationKeyStore(roots["keys"], () => { });
            Host = new(flow.Directory.Path, flow.Directory.Combine("identities.sqlite"), Persistence, keys, native, files,
                (state, journal, ct) => { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException(); return files.RemoveAsync(state, journal, native, ct); },
                (journal, ct) => { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException(); return files.VerifyRemovedAsync(journal, ct); });
            Profiles = new(Location, flow.Store, flow.Preferences, Runtime, flow.Translator, profileRetirement: Host);
            Work = Profiles.Create("Fictitious reviewed work");
            Identities = new(flow.Directory.Combine("identities.sqlite"), () => { }); await Identities.CreateNewAsync(["", Work.Id], native, Ct);
            Identity = (await Identities.ReadAsync(Ct)).Single(row => row.LegacyProfileId == Work.Id);
            var original = flow.Directory.Combine("zanance-" + Work.Id + ".db");
            await new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", Identity.InitialCandidateKey, () => { }).CreateNewAsync(native, Ct);
            await keys.PrepareAsync(Identity.InitialCandidateKey, Ct);
            Candidate = original + "." + Identity.CandidateGeneration.ToString("N") + ".cipher.db";
            TargetKey = Path.Combine(roots["keys"], $"{Identity.ProfileId:N}-{Identity.CandidateGeneration:N}.key");
            await File.WriteAllTextAsync(Candidate, "Fictitious owned candidate bytes.", Ct);
        }
        public void Dispose() => flow.Dispose();
    }
    private sealed class Persistence : ISettingsPersistence
    {
        internal bool Fail; internal int Writes;
        public Task PersistAsync(CancellationToken cancellationToken = default)
        { Writes++; if (Fail) throw new IOException("Injected native settings persistence failure."); return Task.CompletedTask; }
    }
    private sealed class Runtime : IProfileRuntime
    {
        internal int ExclusiveRuns;
        public async Task RunExclusiveAsync(Func<Task> action) { ExclusiveRuns++; await action(); }
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new NotSupportedException();
        public Task<bool> AuthenticateAsync(string reason) => throw new NotSupportedException();
        public Task ReloadLockAsync() => throw new NotSupportedException();
        public void DismissUndo() { }
    }
}
