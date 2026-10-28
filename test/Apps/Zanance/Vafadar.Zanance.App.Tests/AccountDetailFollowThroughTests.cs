using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original goal input order and accepted reconciliation follow-through remain bound to their actual owner.</summary>
public sealed class AccountDetailFollowThroughTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Complete_detail_inputs_keep_original_goal_priority_and_allocation_date_order()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var high = new Goal { Name = "High active", CurrencyCode = "EUR", TargetAmount = 5000, Priority = GoalPriority.High };
        var low = new Goal { Name = "Low active", CurrencyCode = "EUR", TargetAmount = 5000, Priority = GoalPriority.Low };
        var complete = new Goal { Name = "Completed", CurrencyCode = "EUR", TargetAmount = 5000, State = GoalState.Completed };
        var oldest = new GoalAllocation { GoalId = high.Id, AccountId = account.Id, Amount = 500, Date = new(2026, 10, 1) };
        var newest = new GoalAllocation { GoalId = low.Id, AccountId = account.Id, Amount = 700, Date = new(2026, 10, 3) };
        var middle = new GoalAllocation { GoalId = high.Id, AccountId = account.Id, Amount = -100, Date = new(2026, 10, 2) };
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        { db.Goals.AddRange(low, complete, high); db.GoalAllocations.AddRange(oldest, newest, middle); await db.SaveChangesAsync(Ct); }
        var snapshot = await f.Services.GetRequiredService<LoanWorkStore>().ReadAccountDetailAsync(Ct);
        Assert.Equal([high.Id, low.Id, complete.Id], snapshot.Inputs.Goals.Select(item => item.Id));
        Assert.Equal([newest.Id, middle.Id, oldest.Id], snapshot.Inputs.Allocations.Select(item => item.Id));
        var original = f.Services.GetRequiredService<GoalStore>();
        Assert.Equal(snapshot.Inputs.Goals.Select(item => item.Id), (await original.GetGoalsAsync(Ct)).Select(item => item.Id));
        Assert.Equal(snapshot.Inputs.Allocations.Select(item => item.Id), (await original.GetAllocationsAsync(cancellationToken: Ct)).Select(item => item.Id));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Accepted_adjustment_publication_cannot_erase_the_new_accounts_draft_after_the_first_property_retires()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f); var next = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var vm = AccountDetailOriginalBoundaryTests.Create(f); AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        vm.ObservedText = "90.00"; vm.Reason = "Original explicit adjustment"; await vm.CompareCommand.ExecuteAsync(null); Assert.True(vm.CanAdjust);
        var switched = false; vm.PropertyChanged += (_, e) =>
        {
            if (switched || e.PropertyName != nameof(vm.ObservedText) || vm.ObservedText != string.Empty) return;
            switched = true; vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = next.Id });
            vm.ObservedText = "777"; vm.Reason = "New account untouched draft"; vm.ReconcileDate = new(2026, 10, 4);
        };
        f.Platform.Confirmations.Enqueue(true); await vm.AdjustCommand.ExecuteAsync(null);
        Assert.True(switched); Assert.Equal("777", vm.ObservedText); Assert.Equal("New account untouched draft", vm.Reason); Assert.Equal(new DateOnly(2026, 10, 4), vm.ReconcileDate);
        Assert.False(vm.Loading.IsReady); Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.Equal(account.Id, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).AccountId);
    }
}
