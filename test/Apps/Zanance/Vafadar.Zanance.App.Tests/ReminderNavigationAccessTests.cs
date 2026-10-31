using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original notification resolution cannot outlive the access under which its asynchronous read began.</summary>
public sealed class ReminderNavigationAccessTests
{
    [Theory]
    [InlineData("budget", false, false)]
    [InlineData("goal", false, false)]
    [InlineData("plan", false, false)]
    [InlineData("budget", true, false)]
    [InlineData("goal", true, false)]
    [InlineData("plan", true, false)]
    [InlineData("budget", true, true)]
    [InlineData("goal", true, true)]
    [InlineData("plan", true, true)]
    public async Task Cover_during_actual_reminder_read_keeps_navigation_pending_until_unlock(string kind,
        bool laterArrival, bool deletedBeforeUnlock)
    {
        using var fixture = new FlowFixture();
        var originalFactory = fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var access = fixture.Services.GetRequiredService<ICommercialWriteAccessSource>();
        var originalGoals = fixture.Services.GetRequiredService<GoalStore>();
        var originalPlans = fixture.Services.GetRequiredService<PlanStore>();
        string link;
        string expectedRoute;
        Func<Task> retire;
        if (kind == "budget")
        {
            var budget = new Budget { CurrencyCode = "EUR", Year = 2026, Month = 10, TotalLimit = 10000 };
            await fixture.Store.SaveBudgetAsync(budget, TestContext.Current.CancellationToken);
            var snapshot = await fixture.Services.GetRequiredService<BudgetChoiceStore>().ReadAsync(TestContext.Current.CancellationToken);
            link = BudgetReminderLink.Format(snapshot.ReminderScope, budget.Id);
            expectedRoute = AppRoutes.BudgetRoute;
            retire = () => fixture.Store.DeleteBudgetAsync(budget.Id, TestContext.Current.CancellationToken);
        }
        else if (kind == "goal")
        {
            var goal = new Goal { Name = "Original reminder goal", Type = GoalType.Earmark, CurrencyCode = "EUR" };
            await originalGoals.SaveGoalAsync(goal, TestContext.Current.CancellationToken);
            var snapshot = await originalGoals.GetWorkSnapshotAsync(TestContext.Current.CancellationToken);
            link = GoalReminderLink.Format(snapshot.ReminderScope, [new(goal.Id, new(2026, 10, 31))]);
            expectedRoute = AppRoutes.GoalDetailRoute;
            retire = () => originalGoals.DeleteGoalAsync(goal.Id, TestContext.Current.CancellationToken);
        }
        else
        {
            var account = new Account { Name = "Original reminder account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            await fixture.Store.SaveAccountAsync(account, TestContext.Current.CancellationToken);
            var plan = new Schedule { Name = "Original reminder plan", AccountId = account.Id, Amount = 100,
                Rule = new() { Start = new(2026, 10, 31), Frequency = Frequency.Once } };
            await originalPlans.SaveScheduleAsync(plan, TestContext.Current.CancellationToken);
            var snapshot = await originalPlans.GetWorkSnapshotAsync(TestContext.Current.CancellationToken);
            link = PlanReminderLink.Format(snapshot.ReminderScope, [new(plan.Id, new(2026, 10, 31))]);
            expectedRoute = AppRoutes.OccurrenceRoute;
            retire = async () => Assert.True(await originalPlans.DeleteScheduleAsync(plan.Id, TestContext.Current.CancellationToken));
        }

        await AppLockTests.EnableDeviceAsync(fixture);
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        await setup.Lock.UnlockedAsync();
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var factory = new CoverDuringReadFactory(originalFactory, async () =>
        {
            await setup.Lock.SleepAsync();
            if (laterArrival)
                await setup.Lock.RunWhenUnlockedAsync(() => fixture.Platform.NavigateAsync(AppRoutes.ReviewRoute));
        });
        var store = new ZananceStore(factory, fixture.Time, access);
        var router = new AppLinkRouter(setup.Lock, fixture.Platform, new PlanStore(factory, store, access),
            new GoalStore(factory, access), store);

        await router.OpenAsync(link);
        Assert.True(factory.CoverIssued);
        Assert.True(setup.Lock.IsLocked);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
        Assert.Empty(fixture.Platform.Routes);
        if (deletedBeforeUnlock) await retire();
        var beforeUnlock = await GoalChooserFlowTests.SnapshotAsync(fixture);
        await setup.Lock.UnlockedAsync();
        var expected = new List<string>();
        if (!deletedBeforeUnlock) expected.Add(expectedRoute);
        if (laterArrival) expected.Add(AppRoutes.ReviewRoute);
        Assert.Equal(expected, fixture.Platform.Routes.Select(route => route.Route));
        Assert.Equal(beforeUnlock, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    /// <summary>Acquires real original SQLite contexts after one actual access-gate cover transition.</summary>
    private sealed class CoverDuringReadFactory(IDbContextFactory<ZananceDbContext> original, Func<Task> cover)
        : IDbContextFactory<ZananceDbContext>
    {
        internal bool CoverIssued { get; private set; }

        /// <inheritdoc />
        public ZananceDbContext CreateDbContext() => original.CreateDbContext();

        /// <inheritdoc />
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            if (!CoverIssued)
            {
                CoverIssued = true;
                await cover();
            }
            return await original.CreateDbContextAsync(cancellationToken);
        }
    }
}
