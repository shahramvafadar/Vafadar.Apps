using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-170: original forecast computations and actual atomic snapshot writers preserve drafts/history under approved access.</summary>
[Trait("AT", "AT-170")]
public sealed class ForecastWorkFlowTests
{
    private static readonly Guid Scope = Guid.Parse("abababab-3434-5656-7878-909090909090");
    private static readonly DateOnly Day = new(2026, 10, 9);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal Action? OnCapture;
        internal bool Retire;
        internal int Retired;
        public CommercialWriteAccess Capture(string databasePath) { OnCapture?.Invoke(); return Current; }
    }
    private static void Enable(FlowFixture f, Access access, ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
        access.Current = new(f.Directory.Combine("zanance.db"), new(plan,
            new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, Scope), shared ? new(Scope, member, host) : null));
    private static ServiceProvider Provider(FlowFixture f, Access access, bool trigger = false)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(f.Time).AddSingleton<ICommercialWriteAccessSource>(access)
            .AddZananceData(f.Directory.Combine("zanance.db"));
        if (trigger)
        {
            var original = services.Single(item => item.ServiceType == typeof(IDbContextFactory<ZananceDbContext>)); services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new TriggerFactory(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, original.ImplementationType!), access));
        }
        return services.BuildServiceProvider();
    }
    private sealed class TriggerFactory(IDbContextFactory<ZananceDbContext> inner, Access access) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext(); ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireForecastAccess", () =>
            { if (access.Retire) { access.Current = new(access.Current.DatabasePath!, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Scope))); access.Retire = false; access.Retired++; } return 1; }); return db;
        }
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
    private static ForecastWorkspace Workspace(FlowFixture f, ServiceProvider p) => new(p.GetRequiredService<ForecastWorkStore>(), p.GetRequiredService<ZananceStore>(), f.Translator);
    private static async Task SeedAsync(FlowFixture f, bool multiple = false)
    {
        await f.Store.SaveAccountAsync(new Account { Name = "Original forecast scope", CurrencyCode = "EUR", OpeningDate = Day.AddMonths(-1), OpeningBalance = 12345 }, Ct);
        if (multiple) await f.Store.SaveAccountAsync(new Account { Name = "Original second currency", CurrencyCode = "USD", OpeningDate = Day.AddMonths(-1), OpeningBalance = 9876 }, Ct);
        var settings = await f.Store.GetSettingsAsync(Ct); settings.MonthStartDay = 17; await f.Store.SaveSettingsAsync(settings, Ct);
    }
    private static async Task SqlAsync(ServiceProvider p, string sql)
    { await using var db = await p.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct); await db.Database.ExecuteSqlRawAsync(sql, Ct); }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Free_refused_horizon_and_scenario_keep_original_draft_and_basic_financial_month(string language)
    {
        using var f = new FlowFixture(); await SeedAsync(f); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var access = new Access(); using var p = Provider(f, access); var vm = Workspace(f, p); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        vm.SnapshotName = "  Original raw name  "; vm.Scenario.SetAmount(Guid.NewGuid(), Day, 34567); Enable(f, access);
        await vm.LoadAsync(2, PeriodCalendar.Gregorian, Day);
        Assert.True(vm.Loading.IsReady); Assert.Empty(vm.Forecasts); Assert.Equal(2, vm.HorizonIndex); Assert.False(vm.Scenario.IsEmpty);
        Assert.Equal("  Original raw name  ", vm.SnapshotName); Assert.Contains(f.Translator["Commercial_RequiresPlus"], vm.Error); Assert.False(vm.CanSave);
        vm.ResetScenario(); await vm.LoadAsync(0, PeriodCalendar.Gregorian, Day);
        var forecast = Assert.Single(vm.Forecasts); var (year, month) = PeriodMath.MonthOf(Day, PeriodCalendar.Gregorian, 17);
        Assert.Equal(PeriodMath.MonthRange(year, month, PeriodCalendar.Gregorian, 17).Last, forecast.Path[^1].Date);
        Assert.Equal(12345, forecast.StartBalance); Assert.Equal(12345, forecast.EndBalance); Assert.Null(vm.Error);
        Assert.False(vm.CanSave); Assert.False(vm.CanAnalyze); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData(PeriodCalendar.Gregorian)][InlineData(PeriodCalendar.Persian)][InlineData(PeriodCalendar.Hijri)]
    public async Task Free_month_end_uses_original_named_calendar_and_month_start_without_settings_writes(PeriodCalendar calendar)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); Enable(f, access);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var reviewed = await p.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct);
        var actual = reviewed.Compute(0, new(), calendar, Day); var (year, month) = PeriodMath.MonthOf(Day, calendar, 17);
        var expected = ForecastCalculator.Compute(reviewed.Accounts, reviewed.Entries, reviewed.Schedules, reviewed.States, Day, PeriodMath.MonthRange(year, month, calendar, 17).Last);
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual)); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Inactive_or_plus_keeps_all_horizons_original_calculations_and_atomic_currency_save(bool enabled)
    {
        using var f = new FlowFixture(); await SeedAsync(f, true); var access = new Access(); using var p = Provider(f, access); if (enabled) Enable(f, access, ProductPlan.Plus);
        var vm = Workspace(f, p); var before = await GoalChooserFlowTests.SnapshotAsync(f); vm.Scenario.SetExcluded(Guid.NewGuid(), Day, true);
        await vm.LoadAsync(2, PeriodCalendar.Persian, Day); var review = vm.Review!;
        Assert.Equal(JsonSerializer.Serialize(ForecastCalculator.Compute(review.Accounts, review.Entries, review.Schedules, review.States, Day, Day.AddDays(90), scenario: vm.Scenario)), JsonSerializer.Serialize(vm.Forecasts));
        Assert.True(vm.CanSave); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.SaveAsync("  Original batch  ", "Original fictitious scenario", "0.1.0");
        Assert.Equal(f.Translator["Forecast_Saved"], vm.Result); Assert.Null(vm.Error); Assert.False(vm.CanSave); Assert.Equal("  Original batch  ", vm.SnapshotName);
        var saved = await f.Store.GetForecastSnapshotsAsync(Ct); Assert.Equal(2, saved.Count); Assert.Equal(new[] { "Original batch (EUR)", "Original batch (USD)" }, saved.OrderBy(item => item.CurrencyCode).Select(item => item.Name));
        Assert.All(saved, item =>
        {
            Assert.Equal(Day.AddDays(90), item.Horizon); Assert.Equal("Original fictitious scenario", item.Assumptions); Assert.Equal("0.1.0", item.AppVersion);
            Assert.Equal(review.Accounts.Where(account => account.CurrencyCode == item.CurrencyCode).Select(account => account.Id), item.Scope());
        });
        await vm.SaveAsync("Unaccepted repeat", null, "0.1.0"); Assert.Equal(2, (await f.Store.GetForecastSnapshotsAsync(Ct)).Count);
    }
    [Theory]
    [InlineData(ProductPlan.Free)][InlineData(ProductPlan.Plus)][InlineData(ProductPlan.Pro)]
    public async Task Original_shared_membership_and_host_access_are_required_independently_of_personal_tier(ProductPlan plan)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access);
        Enable(f, access, plan, shared: true, member: false);
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => p.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct)); Assert.Equal(FeaturePermission.RequiresMembership, error.Permission);
        Enable(f, access, plan, shared: true); var reviewed = await p.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct); Assert.Single(reviewed.Compute(2, new(), PeriodCalendar.Gregorian, Day));
        Enable(f, access, plan, shared: true, host: false); reviewed = await p.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct);
        error = Assert.Throws<CommercialWriteRejectedException>(() => reviewed.Compute(0, new(), PeriodCalendar.Gregorian, Day)); Assert.Equal(FeaturePermission.RequiresPro, error.Permission);
        Assert.Single(reviewed.Accounts); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
    }
    [Fact]
    public async Task Retired_original_review_blocks_computation_scenario_and_save_without_losing_raw_name()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus);
        var vm = Workspace(f, p); await vm.LoadAsync(1, PeriodCalendar.Gregorian, Day); var before = await GoalChooserFlowTests.SnapshotAsync(f); Enable(f, access);
        Assert.Throws<InvalidOperationException>(() => vm.EditScenario(s => s.SetAmount(Guid.NewGuid(), Day, 777)));
        await vm.SaveAsync("  Keep refused name  ", null, "0.1.0"); Assert.Equal("  Keep refused name  ", vm.SnapshotName); Assert.Null(vm.Result); Assert.NotNull(vm.Error); Assert.True(vm.Scenario.IsEmpty);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.LoadAsync(1, PeriodCalendar.Gregorian, Day); Assert.Empty(vm.Forecasts); Assert.Equal(1, vm.HorizonIndex); Assert.Equal("  Keep refused name  ", vm.SnapshotName);
    }
    [Fact]
    public async Task Real_second_currency_sql_failure_rolls_back_the_entire_snapshot_batch_and_preserves_scenario_name()
    {
        using var f = new FlowFixture(); await SeedAsync(f, true); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus);
        var vm = Workspace(f, p); await vm.LoadAsync(2, PeriodCalendar.Gregorian, Day); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await SqlAsync(p, "CREATE TRIGGER RejectSecondForecast BEFORE INSERT ON ForecastSnapshots WHEN NEW.CurrencyCode='USD' BEGIN SELECT RAISE(ABORT,'Owned forecast batch failure'); END;");
        var events = 0; p.GetRequiredService<ZananceStore>().Changed += (_, _) => events++;
        await vm.SaveAsync("  Raw batch draft  ", null, "0.1.0"); Assert.NotNull(vm.Error); Assert.Null(vm.Result); Assert.False(vm.CanSave); Assert.Equal("  Raw batch draft  ", vm.SnapshotName); Assert.Equal(0, events);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await SqlAsync(p, "DROP TRIGGER RejectSecondForecast"); await vm.LoadAsync(2, PeriodCalendar.Gregorian, Day);
        await vm.SaveAsync(vm.SnapshotName, null, "0.1.0"); Assert.Equal(2, (await f.Store.GetForecastSnapshotsAsync(Ct)).Count); Assert.Equal(1, events);
    }
    [Fact]
    public async Task Real_trigger_retired_access_rolls_back_saved_rows_before_notification()
    {
        using var f = new FlowFixture(); await SeedAsync(f, true); var access = new Access(); using var p = Provider(f, access, trigger: true); Enable(f, access, ProductPlan.Plus);
        var vm = Workspace(f, p); await vm.LoadAsync(1, PeriodCalendar.Gregorian, Day); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await SqlAsync(p, "CREATE TRIGGER RetireForecast AFTER INSERT ON ForecastSnapshots BEGIN SELECT RetireForecastAccess(); END;"); access.Retire = true;
        await vm.SaveAsync("Original trigger draft", null, "0.1.0"); Assert.Equal(1, access.Retired); Assert.Null(vm.Result); Assert.NotNull(vm.Error); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Accepted_original_subscriber_failure_reports_saved_result_and_cannot_repeat_old_payload(string language)
    {
        using var f = new FlowFixture(); await SeedAsync(f, true); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus); var vm = Workspace(f, p); await vm.LoadAsync(2, PeriodCalendar.Gregorian, Day);
        p.GetRequiredService<ZananceStore>().Changed += (_, _) => throw new IOException("Fictitious original notification failure");
        await vm.SaveAsync("Accepted original", null, "0.1.0"); Assert.Equal(f.Translator["Forecast_Saved"], vm.Result); Assert.Equal(f.Translator["Forecast_SavedFollowUpFailed"], vm.Error); Assert.False(vm.CanSave);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.LoadAsync(2, PeriodCalendar.Gregorian, Day); Assert.False(vm.CanSave); await vm.SaveAsync("No repeat", null, "0.1.0");
        Assert.Equal(2, (await f.Store.GetForecastSnapshotsAsync(Ct)).Count); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Free_keeps_every_original_saved_value_but_refuses_new_comparison_and_still_deletes_owned_data()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access);
        var vm = Workspace(f, p); await vm.LoadAsync(2, PeriodCalendar.Gregorian, Day); await vm.SaveAsync("Existing original", "Saved assumptions", "0.1.0"); var saved = Assert.Single(await f.Store.GetForecastSnapshotsAsync(Ct));
        Enable(f, access); var reviewed = await p.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct); var original = Assert.Single(reviewed.Snapshots);
        Assert.Equal(JsonSerializer.Serialize(saved), JsonSerializer.Serialize(original)); Assert.NotEmpty(original.Points()); Assert.Equal("Saved assumptions", original.Assumptions);
        Assert.Throws<CommercialWriteRejectedException>(() => reviewed.Compare(original, Day));
        await p.GetRequiredService<ZananceStore>().DeleteReviewedForecastSnapshotAsync(saved.Id, reviewed, Ct); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
    }
    [Fact]
    public async Task Wrong_file_rights_and_retired_inactive_facts_never_publish_or_write_forecasts()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); var reader = p.GetRequiredService<ForecastWorkStore>();
        var reviewed = await reader.ReadAsync(Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f); Enable(f, access, ProductPlan.Plus);
        Assert.Throws<InvalidOperationException>(reviewed.EnsureCurrent); access.Current = new(f.Directory.Combine("foreign.db"), new(ProductPlan.Pro, new(EntitlementScopeKind.PersonalProfile, Scope)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadAsync(Ct)); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Original_read_without_settings_returns_unsaved_defaults_and_changes_no_table()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var review = await p.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct); Assert.Equal(1, review.Settings.MonthStartDay); Assert.Empty(review.Compute(0, new(), PeriodCalendar.Gregorian, Day));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Retired_access_during_original_read_keeps_form_covered_and_raw_draft_until_explicit_refresh()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus);
        var vm = Workspace(f, p); await vm.LoadAsync(2, PeriodCalendar.Gregorian, Day); vm.SnapshotName = "Original covered draft";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var captures = 0;
        access.OnCapture = () => { if (++captures == 2) Enable(f, access); };
        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.LoadAsync(2, PeriodCalendar.Gregorian, Day));
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady); Assert.False(vm.CanSave); Assert.Equal("Original covered draft", vm.SnapshotName);
        access.OnCapture = null; await vm.LoadAsync(2, PeriodCalendar.Gregorian, Day); Assert.True(vm.Loading.IsReady); Assert.Empty(vm.Forecasts);
        Assert.Equal(2, vm.HorizonIndex); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Direct_original_snapshot_writer_refuses_Free_before_audit_mutation_or_notification()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); Enable(f, access);
        var review = await p.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct); var forecast = Assert.Single(review.Compute(0, new(), PeriodCalendar.Gregorian, Day));
        var snapshot = ForecastSnapshot.From("  Original direct draft  ", forecast, review.Accounts.Select(item => item.Id), Day, null, "0.1.0");
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var events = 0; var store = p.GetRequiredService<ZananceStore>(); store.Changed += (_, _) => events++;
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveForecastSnapshotAsync(snapshot, Ct));
        Assert.Equal(CommercialFeature.AdvancedForecast, rejected.Feature); Assert.Equal(FeaturePermission.RequiresPlus, rejected.Permission);
        Assert.Equal("  Original direct draft  ", snapshot.Name); Assert.Equal(default, snapshot.CreatedAt); Assert.Equal(default, snapshot.UpdatedAt);
        Assert.Equal(0, events); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Legacy_saved_scope_with_another_currency_compares_only_its_own_money_without_rewriting_saved_data()
    {
        using var f = new FlowFixture(); await SeedAsync(f, true); var access = new Access(); using var p = Provider(f, access);
        var reviewed = await p.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct); var forecast = reviewed.Compute(0, new(), PeriodCalendar.Gregorian, Day).Single(item => item.CurrencyCode == "EUR");
        var legacy = ForecastSnapshot.From("Legacy original mixed identity scope", forecast, reviewed.Accounts.Select(item => item.Id), Day, null, "0.1.0");
        await f.Store.SaveForecastSnapshotAsync(legacy, Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        reviewed = await p.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct); var comparison = reviewed.Compare(Assert.Single(reviewed.Snapshots), Day);
        var actual = Assert.Single(comparison.Days); Assert.Equal(12345, actual.Saved); Assert.Equal(12345, actual.Actual);
        Assert.Equal(0, comparison.Difference); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory, Trait("AT", "AT-43")]
    [InlineData(false)][InlineData(true)]
    public async Task Legacy_missing_destination_stays_incomplete_through_original_read_and_atomic_snapshot_save(bool assumeSource)
    {
        using var f = new FlowFixture(); await SeedAsync(f, true); var access = new Access(); using var p = Provider(f, access);
        var accounts = await f.Store.GetAccountsAsync(cancellationToken:Ct);
        var eur = accounts.Single(account => account.CurrencyCode == "EUR");
        var usd = accounts.Single(account => account.CurrencyCode == "USD");
        var plan = new Core.Plans.Schedule { Name="Legacy destination unavailable", AccountId=eur.Id, ToAccountId=usd.Id,
            Kind=Core.Ledger.EntryKind.Transfer, Amount=1234, ToAmount=null,
            Rule=new() { Frequency=Core.Plans.Frequency.Once, Start=Day.AddDays(1) } };
        // Represent an original legacy row; the current form correctly refuses creating an incomplete transfer.
        await using (var db = await p.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { db.Schedules.Add(plan); await db.SaveChangesAsync(Ct); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var vm = Workspace(f, p);
        if (assumeSource) vm.Scenario.SetAmount(plan.Id, plan.Rule.Start, 2468);
        await vm.LoadAsync(2, PeriodCalendar.Gregorian, Day);
        Assert.True(vm.Loading.IsReady); Assert.Null(vm.Error); Assert.True(vm.CanSave);
        Assert.Equal(assumeSource ? 9877 : 11111, vm.Forecasts.Single(item => item.CurrencyCode == "EUR").EndBalance);
        var destination = vm.Forecasts.Single(item => item.CurrencyCode == "USD");
        Assert.Equal(9876, destination.EndBalance); Assert.Null(Assert.Single(destination.Items).Effect); Assert.True(destination.IsIncomplete);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.SaveAsync("Original missing-rate batch", null, "0.1.0"); Assert.Null(vm.Error); Assert.False(vm.CanSave);
        var snapshots = await f.Store.GetForecastSnapshotsAsync(Ct); Assert.Equal(2, snapshots.Count);
        var saved = snapshots.Single(item => item.CurrencyCode == "USD");
        Assert.Equal(1, saved.UnknownCount); Assert.Equal(new[] { usd.Id }, saved.Scope());
        Assert.All(saved.Points(), point => Assert.Equal(9876, point.Balance));
        Assert.Equal(0, snapshots.Single(item => item.CurrencyCode == "EUR").UnknownCount);
        var stored = Assert.Single(await p.GetRequiredService<PlanStore>().GetSchedulesAsync(Ct));
        Assert.Equal(1234, stored.Amount); Assert.Null(stored.ToAmount); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken:Ct));
    }
}
