using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-153: original budget returning-data resolves after unlock in the actual file without financial or preference writes.</summary>
[Trait("AT", "AT-153")]
public sealed class BudgetReminderFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Fail;
        public CommercialWriteAccess Capture(string databasePath) => Fail ? throw new InvalidOperationException("Owned read failed.") : Current;
    }
    private static AppLinkRouter Router(FlowFixture f, Security.AppLockService gate, ZananceStore? store = null) => new(gate,
        f.Platform, f.Services.GetRequiredService<PlanStore>(), f.Services.GetRequiredService<GoalStore>(), store ?? f.Store);
    private static async Task<(Budget Budget, string Link)> SeedAsync(FlowFixture f, BudgetPeriod period, PeriodCalendar calendar)
    {
        var (year, month) = PeriodMath.MonthOf(new(2026, 10, 9), calendar, 1);
        var budget = new Budget { CurrencyCode = "USD", Calendar = calendar, Year = year, Month = month, Period = period,
            PeriodStart = period == BudgetPeriod.Month ? default : new(2026, 10, 3), TotalLimit = 10000 };
        await f.Store.SaveBudgetAsync(budget, Ct);
        var snapshot = await f.Services.GetRequiredService<BudgetChoiceStore>().ReadAsync(Ct);
        return (budget, BudgetReminderLink.Format(snapshot.ReminderScope, budget.Id));
    }

    [Theory]
    [InlineData(BudgetPeriod.Month, PeriodCalendar.Gregorian)]
    [InlineData(BudgetPeriod.Month, PeriodCalendar.Persian)]
    [InlineData(BudgetPeriod.Month, PeriodCalendar.Hijri)]
    [InlineData(BudgetPeriod.Week, PeriodCalendar.Gregorian)]
    [InlineData(BudgetPeriod.TwoWeeks, PeriodCalendar.Persian)]
    public async Task Original_currency_calendar_and_exact_period_survive_unlock_and_a_later_default_change(BudgetPeriod period, PeriodCalendar calendar)
    {
        using var f = new FlowFixture(); var saved = await SeedAsync(f, period, calendar);
        await AppLockTests.EnableDeviceAsync(f); var gate = AppLockTests.Create(f); await gate.Lock.StartAsync();
        var router = Router(f, gate.Lock); await router.OpenAsync(saved.Link); Assert.Empty(f.Platform.Routes);
        await f.Store.UpdateSettingsAsync(settings => { settings.DefaultCurrencyCode = "EUR"; settings.BudgetCalendar = PeriodCalendar.Gregorian; }, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await gate.Lock.UnlockedAsync();
        var navigation = Assert.Single(f.Platform.Routes); Assert.Equal(AppRoutes.BudgetRoute, navigation.Route);
        var link = Assert.IsType<BudgetReminderLink>(navigation.Parameters!["budgetReminder"]);
        var target = await f.Store.ResolveBudgetReminderAsync(link, Ct); Assert.NotNull(target);
        Assert.Equal(saved.Budget.Id, target.Id); Assert.Equal("USD", target.CurrencyCode); Assert.Equal(calendar, target.Calendar);
        Assert.Equal(period, target.Period); Assert.Equal(saved.Budget.PeriodStart, target.PeriodStart);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_notification_from_another_actual_file_never_opens_a_restored_budget_with_the_same_guid()
    {
        using var first = new FlowFixture(); var saved = await SeedAsync(first, BudgetPeriod.Month, PeriodCalendar.Gregorian);
        using var other = new FlowFixture(); var restored = await SeedAsync(other, BudgetPeriod.Month, PeriodCalendar.Gregorian);
        await using (var db = await other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Budgets SET Id={saved.Budget.Id} WHERE Id={restored.Budget.Id}", Ct);
        var gate = AppLockTests.Create(other); await gate.Lock.StartAsync(); var before = await GoalChooserFlowTests.SnapshotAsync(other);
        Assert.Null(await other.Store.ResolveBudgetReminderAsync(BudgetReminderLink.Parse(saved.Link)!, Ct));
        await Router(other, gate.Lock).OpenAsync(saved.Link); Assert.Empty(other.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(other));
    }

    [Fact]
    public async Task Deletion_while_locked_rejects_the_original_destination_instead_of_opening_another_budget()
    {
        using var f = new FlowFixture(); var saved = await SeedAsync(f, BudgetPeriod.Month, PeriodCalendar.Gregorian);
        await AppLockTests.EnableDeviceAsync(f); var gate = AppLockTests.Create(f); await gate.Lock.StartAsync();
        await Router(f, gate.Lock).OpenAsync(saved.Link); await f.Store.DeleteBudgetAsync(saved.Budget.Id, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await gate.Lock.UnlockedAsync();
        Assert.Empty(f.Platform.Routes); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task An_expired_unselected_paid_method_stays_viewable_without_unlocking_new_work_or_writing_preferences()
    {
        using var f = new FlowFixture(); var saved = await SeedAsync(f, BudgetPeriod.Month, PeriodCalendar.Gregorian);
        saved.Budget.Method = BudgetMethod.Flex; await f.Store.SaveBudgetAsync(saved.Budget, Ct);
        f.Time.Now = new(2026, 12, 15, 9, 0, 0, TimeSpan.Zero);
        var scope = new QuotaScope(QuotaScopeKind.PersonalProfile, Guid.NewGuid()); var source = new AccessSource
        { Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, scope.Id)),
            budgetSelection: new(scope, [])) };
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var store = new ZananceStore(factory, f.Time, source);
        var gate = AppLockTests.Create(f); await gate.Lock.StartAsync(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Router(f, gate.Lock, store).OpenAsync(saved.Link); var navigation = Assert.Single(f.Platform.Routes);
        var target = await store.ResolveBudgetReminderAsync(Assert.IsType<BudgetReminderLink>(navigation.Parameters!["budgetReminder"]), Ct);
        Assert.NotNull(target); Assert.Equal(BudgetMethod.Flex, target.Method);
        Assert.False((await new BudgetChoiceStore(factory, source, f.Time).ReadAsync(Ct)).Work.CanUse(target.Id));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_failed_actual_file_read_never_falls_back_to_general_budget_navigation()
    {
        using var f = new FlowFixture(); var saved = await SeedAsync(f, BudgetPeriod.Month, PeriodCalendar.Gregorian);
        var source = new AccessSource { Fail = true }; var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, source);
        var gate = AppLockTests.Create(f); await gate.Lock.StartAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Router(f, gate.Lock, store).OpenAsync(saved.Link));
        Assert.Empty(f.Platform.Routes);
    }

    [Theory]
    [InlineData("budget-reminder-v1")]
    [InlineData("budget-reminder-v1|bad|bad")]
    [InlineData("budget-reminder-v1|0000000000000000000000000000000000000000000000000000000000000000|00000000000000000000000000000000")]
    [InlineData("budget-reminder-v1|AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA|11111111111111111111111111111111")]
    public async Task Malformed_native_budget_metadata_is_rejected_without_queuing_or_navigation(string input)
    {
        using var f = new FlowFixture(); Assert.Null(BudgetReminderLink.Parse(input)); var gate = AppLockTests.Create(f);
        await Router(f, gate.Lock).OpenAsync(input); await gate.Lock.StartAsync(); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public void Original_budget_payload_roundtrips_and_rejects_extra_members_and_oversized_identity()
    {
        var scope = new string('a', 64); var id = Guid.NewGuid(); var formatted = BudgetReminderLink.Format(scope, id);
        var parsed = BudgetReminderLink.Parse(formatted); Assert.NotNull(parsed); Assert.Equal(scope, parsed.Scope); Assert.Equal(id, parsed.BudgetId);
        Assert.Null(BudgetReminderLink.Parse(formatted + "|extra")); Assert.Null(BudgetReminderLink.Parse(formatted + new string('a', 1500)));
        Assert.Throws<ArgumentException>(() => BudgetReminderLink.Format("bad", id));
        Assert.Throws<ArgumentException>(() => BudgetReminderLink.Format(scope, Guid.Empty));
    }
}
