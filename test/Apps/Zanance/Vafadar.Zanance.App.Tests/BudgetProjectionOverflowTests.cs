using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>The original budget worker refuses incomplete monetary presentation before publishing rows.</summary>
public sealed class BudgetProjectionOverflowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Today = new(2026, 10, 9);

    private static async Task<(BudgetReadSnapshot Packet, Budget Budget)> PrepareAsync(FlowFixture fixture,
        BudgetMethod method, int shape, bool representable = false)
    {
        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var account = new Account { Name = "Projection boundary", CurrencyCode = "EUR", OpeningDate = new(2026, 8, 1) };
        var category = new Category { Name = "Projection boundary", Kind = CategoryKind.Expense, SpendingType = SpendingType.Fixed };
        var current = new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", Method = method, Rollover = BudgetRollover.SurplusAndDeficit };
        db.Accounts.Add(account); db.Categories.Add(category); db.Budgets.Add(current);
        LedgerEntry Entry(EntryKind kind, long amount, DateOnly day) => new() { AccountId = account.Id, CategoryId = category.Id, Kind = kind, Amount = amount, Date = day };
        if (shape is 0 or 1)
        {
            var previous = new Budget { Year = 2026, Month = 9, CurrencyCode = "EUR", Rollover = BudgetRollover.SurplusAndDeficit };
            if (shape == 0) { current.TotalLimit = representable ? long.MaxValue - 1 : long.MaxValue; previous.TotalLimit = 1; }
            else { current.CategoryLimits = [new() { CategoryId = category.Id, Limit = representable ? long.MaxValue - 1 : long.MaxValue }]; previous.CategoryLimits = [new() { CategoryId = category.Id, Limit = 1 }]; }
            db.Budgets.Add(previous);
        }
        else if (shape == 2)
        {
            current.TotalLimit = long.MaxValue;
            var refund = Entry(EntryKind.Refund, 1, Today);
            if (method == BudgetMethod.Flex && !representable) refund.CategoryId = null;
            db.Entries.Add(refund);
        }
        else if (shape == 3)
        {
            account.OpeningBalance = long.MinValue; current.CategoryLimits = [new() { CategoryId = category.Id, Limit = 1 }];
        }
        else if (shape is 4 or 5)
        {
            current.TotalLimit = shape == 5 ? 1 : 0;
            db.Schedules.Add(new Schedule { Name = "Fixed boundary", AccountId = account.Id, CategoryId = category.Id, Amount = long.MaxValue, Rule = new() { Frequency = Frequency.Once, Start = Today } });
            if (shape == 4) db.Entries.Add(Entry(EntryKind.Refund, 1, Today));
        }
        else if (shape == 6)
        {
            account.OpeningBalance = long.MaxValue; current.TotalLimit = 0;
            db.Budgets.Add(new Budget { Year = 2026, Month = 9, CurrencyCode = "EUR", TotalLimit = 0, Rollover = BudgetRollover.SurplusAndDeficit });
            db.Entries.AddRange(Entry(EntryKind.Expense, long.MaxValue, new(2026, 9, 1)), Entry(EntryKind.Expense, 1, Today));
        }
        else if (shape == 7)
        {
            account.OpeningBalance = long.MaxValue; current.TotalLimit = long.MaxValue;
            db.Budgets.AddRange(new Budget { Year = 2026, Month = 8, CurrencyCode = "EUR", TotalLimit = 0, Rollover = BudgetRollover.SurplusAndDeficit },
                new Budget { Year = 2026, Month = 9, CurrencyCode = "EUR", TotalLimit = 0, Rollover = BudgetRollover.SurplusAndDeficit });
            db.Entries.AddRange(Entry(EntryKind.Expense, 1, new(2026, 8, 1)), Entry(EntryKind.Expense, long.MaxValue, new(2026, 9, 1)));
        }
        else if (shape == 8)
        {
            account.OpeningBalance = long.MinValue; current.TotalLimit = 0;
            db.Entries.AddRange(Entry(EntryKind.Refund, long.MaxValue, Today), Entry(EntryKind.Refund, 1, Today));
        }
        else throw new ArgumentOutOfRangeException(nameof(shape));
        await db.SaveChangesAsync(Ct);
        var packet = await fixture.Store.GetBudgetReadSnapshotAsync(Ct);
        return (packet, packet.Review.Choices.Items.Single(item => item.Month == 10).ToBudget());
    }

    [Theory]
    [InlineData(BudgetMethod.Limits, 0)] [InlineData(BudgetMethod.Flex, 0)] [InlineData(BudgetMethod.Envelopes, 0)]
    [InlineData(BudgetMethod.Limits, 1)] [InlineData(BudgetMethod.Envelopes, 1)]
    [InlineData(BudgetMethod.Limits, 2)]
    [InlineData(BudgetMethod.Envelopes, 3)]
    [InlineData(BudgetMethod.Flex, 4)] [InlineData(BudgetMethod.Flex, 5)]
    [InlineData(BudgetMethod.Limits, 6)] [InlineData(BudgetMethod.Limits, 7)]
    public async Task Complete_original_projection_refuses_unrepresentable_display(BudgetMethod method, int shape)
    {
        using var fixture = new FlowFixture(); var prepared = await PrepareAsync(fixture, method, shape);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        await Assert.ThrowsAsync<OverflowException>(() => BudgetFinancialProjection.PrepareAsync(prepared.Packet, prepared.Budget,
            new(2026, 10, 1), new(2026, 10, 31), "EUR", Today, false, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Theory]
    [InlineData(BudgetMethod.Limits, 0)] [InlineData(BudgetMethod.Flex, 0)] [InlineData(BudgetMethod.Envelopes, 0)]
    [InlineData(BudgetMethod.Limits, 1)] [InlineData(BudgetMethod.Envelopes, 1)]
    public async Task Representable_effective_limit_preserves_original_carry_and_source(BudgetMethod method, int shape)
    {
        using var fixture = new FlowFixture(); var prepared = await PrepareAsync(fixture, method, shape, true);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var result = await BudgetFinancialProjection.PrepareAsync(prepared.Packet, prepared.Budget, new(2026, 10, 1), new(2026, 10, 31), "EUR", Today, false, Ct);
        var status = shape == 0 ? result.Total! : Assert.Single(result.Categories).Status;
        Assert.Equal(long.MaxValue, status.Limit); Assert.Equal(long.MaxValue, status.Remaining);
        Assert.Equal(1, shape == 0 ? result.Carry.Total : result.Carry.For(Assert.Single(result.Categories).CategoryId));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Fact]
    public async Task Flexible_refund_cannot_publish_an_unrepresentable_remaining()
    {
        using var fixture = new FlowFixture(); var prepared = await PrepareAsync(fixture, BudgetMethod.Flex, 2);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        await Assert.ThrowsAsync<OverflowException>(() => BudgetFinancialProjection.PrepareAsync(prepared.Packet, prepared.Budget,
            new(2026, 10, 1), new(2026, 10, 31), "EUR", Today, false, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Fact]
    public async Task A_fixed_refund_does_not_inflate_flexible_remaining()
    {
        using var fixture = new FlowFixture(); var prepared = await PrepareAsync(fixture, BudgetMethod.Flex, 2, true);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var result = await BudgetFinancialProjection.PrepareAsync(prepared.Packet, prepared.Budget,
            new(2026, 10, 1), new(2026, 10, 31), "EUR", Today, false, Ct);
        Assert.Equal(-1, result.Flex!.Fixed.Spent); Assert.Equal(0, result.Flex.Flexible.Spent);
        Assert.Equal(long.MaxValue, result.Flex.Flexible.Remaining);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Theory]
    [InlineData(BudgetMethod.Limits)] [InlineData(BudgetMethod.Envelopes)]
    public async Task Zero_limit_does_not_validate_an_undisplayed_remaining(BudgetMethod method)
    {
        using var fixture = new FlowFixture(); var prepared = await PrepareAsync(fixture, method, 8);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var result = await BudgetFinancialProjection.PrepareAsync(prepared.Packet, prepared.Budget,
            new(2026, 10, 1), new(2026, 10, 31), "EUR", Today, false, Ct);
        Assert.Equal(0, result.Total!.Limit); Assert.Equal(long.MinValue, result.Total.Spent);
        Assert.Null(result.Total.UsagePercent); Assert.False(result.Total.IsOver);
        if (method == BudgetMethod.Envelopes) Assert.Equal(0, result.Envelope!.Unassigned);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }
}
