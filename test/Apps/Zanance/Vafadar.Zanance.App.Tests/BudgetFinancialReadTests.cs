using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual complete budget reader and financial worker, with original SQLite and no native-control substitutes.</summary>
[Trait("AT", "AT-155")]
public sealed class BudgetFinancialReadTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Today = new(2026, 10, 9);
    private static IDbContextFactory<ZananceDbContext> Factory(FlowFixture fixture) => fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();

    private static async Task SeedAsync(FlowFixture fixture, BudgetMethod method = BudgetMethod.Limits)
    {
        await using var db = await Factory(fixture).CreateDbContextAsync(Ct);
        var account = new Account { Name = "Original budget cash", OpeningBalance = 10000, OpeningDate = new(2026, 9, 1), CurrencyCode = "EUR" };
        var food = new Category { Name = "Original food", Kind = CategoryKind.Expense };
        var goal = new Goal { Name = "Original earmark", TargetAmount = 3000, AccountId = account.Id, CurrencyCode = "EUR" };
        db.Accounts.Add(account); db.Categories.Add(food); db.Goals.Add(goal);
        db.GoalAllocations.Add(new GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = 2500, Date = Today });
        db.Budgets.Add(new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", Method = method, TotalLimit = 2000,
            CategoryLimits = [new() { CategoryId = food.Id, Limit = 1000 }] });
        db.Entries.AddRange(new LedgerEntry { AccountId = account.Id, CategoryId = food.Id, Kind = EntryKind.Expense, Amount = 1000, Date = Today, Review = ReviewState.Confirmed },
            new LedgerEntry { AccountId = account.Id, CategoryId = food.Id, Kind = EntryKind.Refund, Amount = 400, Date = Today, Review = ReviewState.Confirmed },
            new LedgerEntry { AccountId = account.Id, CategoryId = food.Id, Kind = EntryKind.Expense, Amount = 200, Date = Today, Review = ReviewState.Unreviewed },
            new LedgerEntry { AccountId = account.Id, CategoryId = food.Id, Kind = EntryKind.Expense, Amount = 100, Date = Today.AddMonths(-1), Review = ReviewState.Confirmed },
            new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Income, Amount = 99999, Date = Today.AddMonths(1) });
        db.Schedules.Add(new Schedule { Name = "Original monthly bill", Kind = EntryKind.Expense, AccountId = account.Id,
            Amount = 2000, Rule = new() { Frequency = Frequency.Monthly, Start = Today } });
        await db.SaveChangesAsync(Ct);
    }

    [Theory]
    [InlineData(BudgetMethod.Limits, false), InlineData(BudgetMethod.Limits, true)]
    [InlineData(BudgetMethod.Envelopes, false), InlineData(BudgetMethod.Envelopes, true)]
    [InlineData(BudgetMethod.Flex, false), InlineData(BudgetMethod.Flex, true)]
    public async Task Original_methods_keep_refunds_review_filter_and_envelope_cash_independent(BudgetMethod method, bool confirmed)
    {
        using var fixture = new FlowFixture(); await SeedAsync(fixture, method);
        var packet = await fixture.Store.GetBudgetReadSnapshotAsync(Ct);
        var budget = Assert.Single(packet.Review.Choices.Items).ToBudget();
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var result = await BudgetFinancialProjection.PrepareAsync(packet, budget, new(2026, 10, 1), new(2026, 10, 31), "EUR", Today, confirmed, Ct);
        Assert.Equal(confirmed ? 600 : 800, result.Total!.Spent); Assert.Equal(2000, result.Total.Limit);
        Assert.Equal(1, result.Unreviewed); Assert.Equal(2000, result.Planned.Total); Assert.Equal(1, result.Planned.Count);
        Assert.Equal(method == BudgetMethod.Flex, result.Flex is not null);
        if (method == BudgetMethod.Flex) Assert.Empty(result.Categories);
        else Assert.Equal(confirmed ? 400 : 200, Assert.Single(result.Categories).Status.Remaining);
        if (method == BudgetMethod.Envelopes)
        {
            Assert.Equal(9100, result.Envelope!.Available); Assert.Equal(2500, result.Envelope.Earmarked);
            Assert.Equal(confirmed ? 6200 : 6400, result.Envelope.Unassigned);
        }
        else Assert.Null(result.Envelope);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Theory, InlineData(BudgetPeriod.Week), InlineData(BudgetPeriod.TwoWeeks)]
    public async Task Existing_short_period_uses_limits_even_with_a_monthly_method(BudgetPeriod period)
    {
        using var fixture = new FlowFixture(); await SeedAsync(fixture, BudgetMethod.Flex);
        var packet = await fixture.Store.GetBudgetReadSnapshotAsync(Ct); var budget = Assert.Single(packet.Review.Choices.Items).ToBudget();
        budget.Period = period; budget.PeriodStart = Today;
        var result = await BudgetFinancialProjection.PrepareAsync(packet, budget, Today, Today.AddDays(period == BudgetPeriod.Week ? 6 : 13), "EUR", Today, false, Ct);
        Assert.Null(result.Flex); Assert.Null(result.Envelope); Assert.Equal(800, result.Total!.Spent);
        Assert.Single(result.Categories);
    }

    [Fact]
    public async Task Previous_envelope_period_keeps_its_spending_without_present_cash_summary()
    {
        using var fixture = new FlowFixture(); await SeedAsync(fixture, BudgetMethod.Envelopes);
        var packet = await fixture.Store.GetBudgetReadSnapshotAsync(Ct); var budget = Assert.Single(packet.Review.Choices.Items).ToBudget();
        var result = await BudgetFinancialProjection.PrepareAsync(packet, budget, new(2026, 9, 1), new(2026, 9, 30), "EUR", Today, false, Ct);
        Assert.True(result.EnvelopeOutsideCurrentPeriod); Assert.Null(result.Envelope); Assert.Equal(100, result.Total!.Spent);
    }

    [Fact]
    public async Task No_budget_is_absence_not_zero_and_keeps_original_planned_summary()
    {
        using var fixture = new FlowFixture(); await SeedAsync(fixture);
        var packet = await fixture.Store.GetBudgetReadSnapshotAsync(Ct);
        var result = await BudgetFinancialProjection.PrepareAsync(packet, null, new(2026, 10, 1), new(2026, 10, 31), "EUR", Today, false, Ct);
        Assert.Null(result.Total); Assert.Empty(result.Categories); Assert.Null(result.Envelope); Assert.Null(result.Flex);
        Assert.Equal(2000, result.Planned.Total);
    }

    [Fact]
    public async Task Carry_uses_complete_prior_history_including_unreviewed_rows()
    {
        using var fixture = new FlowFixture(); await SeedAsync(fixture);
        await using (var db = await Factory(fixture).CreateDbContextAsync(Ct))
        {
            var current = await db.Budgets.SingleAsync(Ct); current.Rollover = BudgetRollover.Surplus;
            db.Budgets.Add(new Budget { Year = 2026, Month = 9, TotalLimit = 500, CurrencyCode = "EUR", Rollover = BudgetRollover.Surplus });
            await db.SaveChangesAsync(Ct);
        }
        var packet = await fixture.Store.GetBudgetReadSnapshotAsync(Ct); var budget = packet.Review.Choices.Items.Single(item => item.Month == 10).ToBudget();
        var result = await BudgetFinancialProjection.PrepareAsync(packet, budget, new(2026, 10, 1), new(2026, 10, 31), "EUR", Today, true, Ct);
        Assert.Equal(400, result.Carry.Total); Assert.Equal(2400, result.Total!.Limit); Assert.Equal(600, result.Total.Spent);
    }

    [Fact]
    public async Task Empty_read_and_cancelled_read_never_create_defaults_or_hold_original_lease()
    {
        using var fixture = new FlowFixture();
        var packet = await fixture.Store.GetBudgetReadSnapshotAsync(Ct);
        Assert.Null(packet.Review.Settings); Assert.Empty(packet.Entries); Assert.Empty(packet.Review.Choices.Items);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Store.GetBudgetReadSnapshotAsync(cancelled.Token));
        await using (var db = await Factory(fixture).CreateDbContextAsync(Ct))
        {
            Assert.False(await db.Settings.AnyAsync(Ct));
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ResourceSelections").SingleAsync(Ct));
        }
        var location = fixture.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct); timeout.CancelAfter(TimeSpan.FromSeconds(5));
        using var owner = await location.Access.AcquireExclusiveAsync(timeout.Token);
        location.Access.RequireDrainedOwner(owner);
    }

    [Fact]
    public async Task Goal_allocation_history_keeps_original_date_and_timestamp_order()
    {
        using var fixture = new FlowFixture(); await SeedAsync(fixture, BudgetMethod.Envelopes);
        await using (var db = await Factory(fixture).CreateDbContextAsync(Ct))
        {
            var original = await db.GoalAllocations.SingleAsync(Ct);
            db.GoalAllocations.AddRange(
                new GoalAllocation { GoalId = original.GoalId, AccountId = original.AccountId, Date = Today.AddDays(-1), Amount = -100 },
                new GoalAllocation { GoalId = original.GoalId, AccountId = original.AccountId, Date = Today.AddDays(1), Amount = 100 });
            await db.SaveChangesAsync(Ct);
        }
        var expected = await fixture.Services.GetRequiredService<GoalStore>().GetAllocationsAsync(cancellationToken: Ct);
        var packet = await fixture.Store.GetBudgetReadSnapshotAsync(Ct);
        Assert.Equal(expected.Select(item => item.Id), packet.Allocations.Select(item => item.Id));
        var result = await BudgetFinancialProjection.PrepareAsync(packet, Assert.Single(packet.Review.Choices.Items).ToBudget(),
            new(2026, 10, 1), new(2026, 10, 31), "EUR", Today, false, Ct);
        Assert.Equal(2500, result.Envelope!.Earmarked); Assert.Equal(6400, result.Envelope.Unassigned);
    }

    [Fact]
    public async Task Concurrent_writer_cannot_mix_original_budget_accounts_ledger_plans_and_goal_earmarks()
    {
        using var fixture = new FlowFixture(); await SeedAsync(fixture);
        await using (var setup = await Factory(fixture).CreateDbContextAsync(Ct))
            await setup.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        var path = fixture.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path;
        var pause = new PausedRead();
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite($"Data Source={path};Pooling=False")
            .UseModel(ZananceDbContextModel.Instance).AddInterceptors(pause).Options;
        var store = new ZananceStore(new DirectFactory(options), fixture.Time, fixture.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var pending = store.GetBudgetReadSnapshotAsync(Ct);
        try
        {
            await pause.Captured.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
            await using var writer = await Factory(fixture).CreateDbContextAsync(Ct);
            (await writer.Accounts.SingleAsync(Ct)).OpeningBalance = 99000;
            (await writer.Budgets.SingleAsync(Ct)).TotalLimit = 9999;
            (await writer.Entries.FirstAsync(item => item.Amount == 1000, Ct)).Amount = 8800;
            (await writer.Schedules.SingleAsync(Ct)).Amount = 777;
            (await writer.GoalAllocations.SingleAsync(Ct)).Amount = 666;
            await writer.SaveChangesAsync(Ct);
        }
        finally { pause.Release.TrySetResult(); }
        var packet = await pending;
        Assert.Equal(10000, Assert.Single(packet.Review.Accounts).OpeningBalance);
        Assert.Equal(2000, Assert.Single(packet.Review.Choices.Items).TotalLimit);
        Assert.Contains(packet.Entries, item => item.Amount == 1000); Assert.DoesNotContain(packet.Entries, item => item.Amount == 8800);
        Assert.Equal(2000, Assert.Single(packet.Schedules).Amount); Assert.Equal(2500, Assert.Single(packet.Allocations).Amount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ValidateBudgetEditorReviewAsync(packet.Review, Ct));
    }

    /// <summary>A real SQLite context factory with a controlled observation boundary.</summary>
    private sealed class DirectFactory(DbContextOptions<ZananceDbContext> options) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => new(options);
    }

    /// <summary>Pauses one completed actual SQLite read while an independent writer commits.</summary>
    private sealed class PausedRead : DbCommandInterceptor
    {
        private int _captured;
        internal TaskCompletionSource Captured { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _captured, 1) == 0)
            { Captured.TrySetResult(); await Release.Task.WaitAsync(cancellationToken); }
            return result;
        }
    }
}
