using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks actual identity deletion against existing retirement authority and retained SQLite bindings.</summary>
public sealed class ProfileIdentityRetirementTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-232")]
    public async Task Exact_retirement_and_repeated_resume_preserve_every_other_identity()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        await fixture.ReadyAsync(); var before = await fixture.Catalogue.ReadAsync(Ct);
        await fixture.RetireAsync(); await fixture.RetireAsync();
        Assert.Equal(before.Where(row => row.ProfileId != fixture.Request.ProfileId), await fixture.Catalogue.ReadAsync(Ct));
        Assert.Equal(ProfileRetirementPhase.ArtifactsRemoved, (await fixture.Journal.ReadAsync(Ct)).Phase);
        Assert.Equal(2, fixture.ReferenceChecks);
    }

    [Fact, Trait("AT", "AT-232")]
    public async Task Missing_or_early_retirement_cannot_remove_a_registered_identity()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync(createJournal: false);
        var before = await fixture.Catalogue.ReadAsync(Ct);
        await Assert.ThrowsAsync<IOException>(() => fixture.RetireAsync()); Assert.Equal(0, fixture.ReferenceChecks);
        await fixture.Journal.CreateNewAsync(fixture.Durability, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.RetireAsync());
        Assert.Equal(before, await fixture.Catalogue.ReadAsync(Ct)); Assert.Equal(0, fixture.ReferenceChecks);
    }

    [Theory, InlineData(true), InlineData(false), Trait("AT", "AT-232")]
    public async Task Foreign_installation_or_replaced_profile_binding_is_refused(bool installation)
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync(createJournal: false);
        var before = await fixture.Catalogue.ReadAsync(Ct); var original = fixture.Request;
        fixture.Request = new(original.LegacyProfileId, installation ? original.ProfileId : Guid.NewGuid(),
            installation ? Guid.NewGuid() : original.InstallationId, original.OperationId);
        await fixture.Journal.CreateNewAsync(fixture.Durability, Ct); await fixture.ReadyAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.RetireAsync());
        Assert.Equal(before, await fixture.Catalogue.ReadAsync(Ct)); Assert.Equal(0, fixture.ReferenceChecks);
    }

    [Fact, Trait("AT", "AT-232")]
    public async Task Retained_artifact_references_prevent_identity_removal()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync(); await fixture.ReadyAsync();
        var before = await fixture.Catalogue.ReadAsync(Ct); fixture.ReferencesRemain = true;
        await Assert.ThrowsAsync<IOException>(() => fixture.RetireAsync());
        Assert.Equal(before, await fixture.Catalogue.ReadAsync(Ct)); Assert.Equal(1, fixture.ReferenceChecks);
    }

    [Fact, Trait("AT", "AT-232")]
    public async Task Observable_identity_commit_after_failed_flush_resumes_without_replacing_other_rows()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync(); await fixture.ReadyAsync();
        var before = await fixture.Catalogue.ReadAsync(Ct); fixture.Durability.FailCatalogue = true;
        await Assert.ThrowsAsync<IOException>(() => fixture.RetireAsync());
        var retained = await fixture.Catalogue.ReadAsync(Ct);
        Assert.Equal(before.Where(row => row.ProfileId != fixture.Request.ProfileId), retained);
        await Assert.ThrowsAsync<IOException>(() => fixture.RetireAsync());
        fixture.Durability.FailCatalogue = false; await fixture.RetireAsync();
        Assert.Equal(retained, await fixture.Catalogue.ReadAsync(Ct)); Assert.Equal(3, fixture.ReferenceChecks);
    }

    [Fact, Trait("AT", "AT-232")]
    public async Task A_later_same_legacy_registration_cannot_be_deleted_by_the_old_retirement()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync(); await fixture.ReadyAsync(); await fixture.RetireAsync();
        var replacement = await fixture.Catalogue.RegisterAsync(fixture.Request.LegacyProfileId, Ct);
        Assert.NotEqual(fixture.Request.ProfileId, replacement.ProfileId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.RetireAsync());
        Assert.Contains(replacement, await fixture.Catalogue.ReadAsync(Ct));
    }

    [Fact, Trait("AT", "AT-232")]
    public async Task Changed_catalogue_during_reference_check_refuses_deletion()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync(); await fixture.ReadyAsync();
        fixture.OnReferenceCheck = async token => { await fixture.Catalogue.RegisterAsync(Guid.NewGuid().ToString("N"), token); };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.RetireAsync());
        var rows = await fixture.Catalogue.ReadAsync(Ct);
        Assert.Equal(4, rows.Count); Assert.Contains(rows, row => row.ProfileId == fixture.Request.ProfileId);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TemporaryDirectory folder = new();
        internal SqliteDatabaseIdentityCatalogue Catalogue => new(folder.Combine("identities.sqlite"), () => { });
        internal ProfileRetirementCheckpoint Request = null!;
        internal SqliteProfileRetirementJournal Journal => new(folder.Combine("retirement.sqlite"), Request, () => { });
        internal readonly Effects Durability = new();
        internal bool ReferencesRemain;
        internal int ReferenceChecks;
        internal Func<CancellationToken, Task>? OnReferenceCheck;
        internal async Task InitializeAsync(bool createJournal = true)
        {
            var legacy = Guid.NewGuid().ToString("N"); await Catalogue.CreateNewAsync(["", legacy, Guid.NewGuid().ToString("N")], Ct);
            var target = (await Catalogue.ReadAsync(Ct)).Single(row => row.LegacyProfileId == legacy);
            Request = new(legacy, target.ProfileId, target.InstallationId, Guid.NewGuid());
            if (createJournal) await Journal.CreateNewAsync(Durability, Ct);
        }
        internal async Task ReadyAsync()
        {
            await Journal.CompareAndAdvanceAsync(Request, Durability, Ct);
            await Journal.CompareAndAdvanceAsync(Request.Advance(), Durability, Ct);
        }
        internal Task RetireAsync() => Catalogue.RetireAsync(Request.Advance().Advance(), Journal, Durability,
            async ct => { ReferenceChecks++; if (ReferencesRemain) throw new IOException("Captured references remain.");
                if (OnReferenceCheck is not null) await OnReferenceCheck(ct); }, Ct);
        public void Dispose() => folder.Dispose();
    }
    private sealed class Effects : IDatabaseGenerationFileDurability
    {
        internal bool FailCatalogue;
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct)
        { if (FailCatalogue && Path.GetFileName(path) == "identities.sqlite") throw new IOException("Injected catalogue flush failure."); return Task.CompletedTask; }
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
