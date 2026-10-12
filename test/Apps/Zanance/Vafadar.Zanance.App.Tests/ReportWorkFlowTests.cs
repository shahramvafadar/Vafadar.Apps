using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual report packets, original financial calculations and the application export effect boundary.</summary>
[Trait("AT", "AT-173")]
public sealed class ReportWorkFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid Scope = Guid.Parse("71727374-6262-5353-4444-353535353535");
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Current.CreateDbContextAsync(cancellationToken);
    }
    private sealed class Native : IReportExportRuntime
    {
        internal int Confirms, Writes, Shares, Removes;
        internal bool Confirm = true;
        internal Func<Task>? DuringConfirm, DuringWrite;
        public async Task<bool> ConfirmAsync() { Confirms++; if (DuringConfirm is not null) await DuringConfirm(); return Confirm; }
        public async Task<string> WriteAsync(byte[] document) { Writes++; Assert.NotEmpty(document); if (DuringWrite is not null) await DuringWrite(); return "owned-report.pdf"; }
        public Task ShareAsync(string path) { Shares++; Assert.Equal("owned-report.pdf", path); return Task.CompletedTask; }
        public Task RemoveAsync(string path) { Removes++; Assert.Equal("owned-report.pdf", path); return Task.CompletedTask; }
    }
    private static ServiceProvider Provider(FlowFixture f, Access access) => new ServiceCollection().AddSingleton<TimeProvider>(f.Time)
        .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(f.Directory.Combine("zanance.db")).BuildServiceProvider();
    private static void Enable(FlowFixture f, Access access, ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
        access.Current = new(f.Directory.Combine("zanance.db"), new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, Scope),
            shared ? new(Scope, member, host) : null));
    private static async Task SeedAsync(FlowFixture f)
    {
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        await f.Store.SaveAccountAsync(new Account { Name = "Original financial history", CurrencyCode = "EUR", OpeningBalance = 12345, OpeningDate = new(2026, 10, 1), IsArchived = true }, Ct);
        await f.Store.SaveAccountAsync(new Account { Name = "Separate currency", CurrencyCode = "USD", OpeningBalance = 9876, OpeningDate = new(2026, 10, 1) }, Ct);
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Free_retains_original_financial_history_data_status_and_owned_pdf_without_creating_settings(string language)
    {
        using var f = new FlowFixture(); await SeedAsync(f); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var access = new Access(); using var p = Provider(f, access); Enable(f, access); var store = p.GetRequiredService<ReportWorkStore>();
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var review = await store.ReadAsync(Ct);
        Assert.Null(review.Inputs.Settings); Assert.Equal(2, review.Inputs.Accounts.Count);
        var movements = review.Project(0, inputs => ReportCalculator.AccountMovements(inputs.Accounts, inputs.Entries, new(2026, 10, 1), new(2026, 10, 31)));
        Assert.Equal(12345, Assert.Single(movements, item => item.CurrencyCode == "EUR").Closing);
        Assert.Equal(9876, Assert.Single(movements, item => item.CurrencyCode == "USD").Closing);
        Assert.True(review.Project(5, inputs => inputs.Entries.Count == 0));
        var native = new Native(); var session = new ReportExportSession(store);
        await session.ExportAsync(review, false, () => { }, () => Task.FromResult(new byte[] { 1 }), native, Ct);
        Assert.Equal((1, 1, 1, 0), (native.Confirms, native.Writes, native.Shares, native.Removes)); Assert.False(session.IsBusy);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.NotEqual("Report_ReadFailed", f.Translator["Report_ReadFailed"]); Assert.NotEqual("Report_Refresh", f.Translator["Report_Refresh"]);
    }
    [Theory]
    [InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    public async Task Free_denies_original_paid_package_before_any_projection_or_authentication(int package)
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); Enable(f, access);
        var store = p.GetRequiredService<ReportWorkStore>(); var review = await store.ReadAsync(Ct); var projections = 0;
        Assert.Throws<CommercialWriteRejectedException>(() => review.Project(package, _ => ++projections)); Assert.Equal(0, projections);
        var native = new Native(); var session = new ReportExportSession(store);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => session.ExportAsync(review, true, () => { }, () => Task.FromResult(new byte[] { 1 }), native, Ct));
        Assert.Equal(0, native.Confirms); Assert.Equal(0, native.Writes); Assert.False(session.IsBusy);
    }
    [Theory]
    [InlineData(-1)][InlineData(1)][InlineData(2)]
    public async Task Inactive_Plus_and_Pro_use_original_package_calculators_and_advanced_export(int plan)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); if (plan >= 0) Enable(f, access, (ProductPlan)plan);
        var store = p.GetRequiredService<ReportWorkStore>(); var review = await store.ReadAsync(Ct);
        Assert.Equal(2, review.Project(3, inputs => ReportCalculator.AccountMovements(inputs.Accounts, inputs.Entries, new(2026, 10, 1), new(2026, 10, 31))).Count);
        var native = new Native(); await new ReportExportSession(store).ExportAsync(review, true, () => { }, () => Task.FromResult(new byte[] { 1 }), native, Ct);
        Assert.Equal(1, native.Shares);
    }
    [Theory]
    [InlineData("account")][InlineData("settings")][InlineData("rights")][InlineData("activation")][InlineData("day")][InlineData("form")][InlineData("caller")]
    public async Task Changes_during_native_authentication_reject_export_before_rendering_or_cache_write(string change)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access);
        if (change != "activation") Enable(f, access, ProductPlan.Plus);
        var store = p.GetRequiredService<ReportWorkStore>(); var review = await store.ReadAsync(Ct); var scope = true; var renders = 0;
        var native = new Native { DuringConfirm = async () =>
        {
            switch (change)
            {
                case "account": await using (var db = await p.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
                    { var row = await db.Accounts.FirstAsync(Ct); row.OpeningBalance++; await db.SaveChangesAsync(Ct); } break;
                case "settings": await f.Store.GetSettingsAsync(Ct); break;
                case "rights": Enable(f, access); break;
                case "activation": Enable(f, access, ProductPlan.Plus); break;
                case "day": f.Time.Now = f.Time.Now.AddDays(1); break;
                case "form": scope = false; break;
                case "caller": review.Inputs.Accounts[0].Name += "changed"; break;
            }
        } };
        var session = new ReportExportSession(store);
        await Assert.ThrowsAnyAsync<Exception>(() => session.ExportAsync(review, true, () => { if (!scope) throw new InvalidOperationException("Changed explicit scope."); },
            () => { renders++; return Task.FromResult(new byte[] { 1 }); }, native, Ct));
        Assert.Equal(1, native.Confirms); Assert.Equal(0, renders); Assert.Equal(0, native.Writes); Assert.Equal(0, native.Shares); Assert.False(session.IsBusy);
    }
    [Fact]
    public async Task A_changed_actual_profile_during_authentication_cannot_export_the_previous_file()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var access = new Access(); var store = new ReportWorkStore(factory, access, f.Time); var review = await store.ReadAsync(Ct);
        var native = new Native { DuringConfirm = () => { factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); return Task.CompletedTask; } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReportExportSession(store).ExportAsync(review, false, () => { }, () => Task.FromResult(new byte[] { 1 }), native, Ct));
        Assert.Equal(0, native.Writes); Assert.Equal(0, native.Shares);
    }
    [Fact]
    public async Task A_change_after_cache_write_removes_only_the_unpublished_owned_file()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus);
        var store = p.GetRequiredService<ReportWorkStore>(); var review = await store.ReadAsync(Ct);
        var native = new Native { DuringWrite = () => { Enable(f, access); return Task.CompletedTask; } };
        var session = new ReportExportSession(store);
        await Assert.ThrowsAnyAsync<Exception>(() => session.ExportAsync(review, true, () => { }, () => Task.FromResult(new byte[] { 1 }), native, Ct));
        Assert.Equal((1, 0, 1), (native.Writes, native.Shares, native.Removes)); Assert.False(session.IsBusy);
    }
    [Fact]
    public async Task Cancelled_authentication_does_not_render_or_write_and_single_flight_starts_before_authentication()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); var store = p.GetRequiredService<ReportWorkStore>();
        var review = await store.ReadAsync(Ct); var session = new ReportExportSession(store); var reached = new TaskCompletionSource(); var release = new TaskCompletionSource();
        var native = new Native { Confirm = false, DuringConfirm = async () => { reached.SetResult(); await release.Task; } };
        var first = session.ExportAsync(review, false, () => { }, () => throw new InvalidOperationException("Cancelled render."), native, Ct);
        await reached.Task; Assert.True(session.IsBusy);
        await session.ExportAsync(review, false, () => { }, () => throw new InvalidOperationException("Repeated render."), native, Ct);
        Assert.Equal(1, native.Confirms); release.SetResult(); await first; Assert.False(session.IsBusy); Assert.Equal(0, native.Writes);
    }
    [Theory]
    [InlineData(true,false,true)][InlineData(true,true,false)][InlineData(true,true,true)]
    public async Task Exact_shared_membership_and_host_facts_remain_separate_from_personal_paid_access(bool shared, bool member, bool host)
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus, shared, member, host);
        var store = p.GetRequiredService<ReportWorkStore>();
        if (!member) { await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.ReadAsync(Ct)); return; }
        var review = await store.ReadAsync(Ct);
        if (!host) { Assert.Throws<CommercialWriteRejectedException>(() => review.DemandPdf(true)); review.DemandPdf(false); }
        else { review.DemandPdf(true); Assert.Equal(FeaturePermission.Allowed, review.Permission(CommercialFeature.AdvancedReports)); }
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Original_contribution_writer_rechecks_report_sources_and_distinguishes_post_commit_notification(bool stale)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access);
        var goals = p.GetRequiredService<GoalStore>();
        var goal = new Core.Goals.Goal { Name = "Original report goal", CurrencyCode = "EUR", TargetAmount = 50000 };
        await goals.SaveGoalAsync(goal, Ct); Enable(f, access, ProductPlan.Plus);
        var reader = p.GetRequiredService<ReportWorkStore>(); var review = await reader.ReadAsync(Ct);
        if (stale)
        {
            await using var db = await p.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            var account = await db.Accounts.FirstAsync(Ct); account.OpeningBalance++; await db.SaveChangesAsync(Ct);
        }
        else goals.Changed += (_, _) => throw new InvalidOperationException("Fictitious report observer failure.");
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var plan = new Core.Goals.ContributionPlan { Method = Core.Goals.ContributionMethod.FixedAmount, Amount = 2500,
            Rule = new Core.Plans.RecurrenceRule { Frequency = Core.Plans.Frequency.Monthly, Start = new(2026, 11, 1) } };
        if (stale)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => goals.SaveContributionPlanAsync(goal.Id, plan, Ct, reportReview: review));
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(await goals.GetContributionPlansAsync(Ct));
        }
        else
        {
            var committed = await Assert.ThrowsAsync<CommittedWriteNotificationException<Guid>>(() => goals.SaveContributionPlanAsync(goal.Id, plan, Ct, reportReview: review));
            Assert.Equal(goal.Id, committed.Result); Assert.Equal(2500, Assert.Single(await goals.GetContributionPlansAsync(Ct)).Amount);
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
            await Assert.ThrowsAsync<InvalidOperationException>(() => goals.SaveContributionPlanAsync(goal.Id, plan, Ct, reportReview: review));
            Assert.Single(await goals.GetContributionPlansAsync(Ct));
        }
    }
}
