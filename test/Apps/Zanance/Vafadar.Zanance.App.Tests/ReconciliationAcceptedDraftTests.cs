using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Known acceptance of one original adjustment must not erase newer same-page inputs from its post-commit callback.</summary>
public sealed class ReconciliationAcceptedDraftTests
{
    [Fact]
    public async Task An_accepted_adjustment_keeps_a_newer_same_page_balance_reason_and_date()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var vm = AccountDetailOriginalBoundaryTests.Create(f);
        AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync(); Assert.True(vm.Loading.IsReady);
        var acceptedDate = vm.ReconcileDate; var newerDate = acceptedDate.AddDays(-1);
        vm.ObservedText = "102.34"; vm.Reason = "New accepted-receipt control";
        await vm.CompareCommand.ExecuteAsync(null); Assert.True(vm.CanAdjust);
        var notifications = 0;
        f.Store.Changed += (_, _) =>
        {
            notifications++; vm.ObservedText = "999.9900"; vm.Reason = "Newer same-page reason"; vm.ReconcileDate = newerDate;
            throw new IOException("Distinct accepted adjustment follow-through failure");
        };
        f.Platform.Confirmations.Enqueue(true); await vm.AdjustCommand.ExecuteAsync(null);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(ct);
        var accepted = Assert.Single(await db.Entries.AsNoTracking().ToListAsync(ct));
        Assert.Equal(EntryKind.Adjustment, accepted.Kind); Assert.Equal(AdjustmentDirection.Increase, accepted.Direction);
        Assert.Equal(234, accepted.Amount); Assert.Equal(account.Id, accepted.AccountId); Assert.Equal(acceptedDate, accepted.Date);
        Assert.Equal("New accepted-receipt control", accepted.Note);
        var savedAccount = await db.Accounts.AsNoTracking().SingleAsync(ct);
        Assert.Equal(acceptedDate, savedAccount.LastReconciledOn); Assert.Equal(account.Name, savedAccount.Name);
        Assert.Equal(account.CurrencyCode, savedAccount.CurrencyCode); Assert.Equal(account.OpeningBalance, savedAccount.OpeningBalance);
        Assert.Equal(1, notifications); Assert.Single(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
        Assert.Equal("999.9900", vm.ObservedText); Assert.Equal("Newer same-page reason", vm.Reason); Assert.Equal(newerDate, vm.ReconcileDate);
        Assert.False(vm.CanAdjust); Assert.Equal(f.Translator["Reconcile_Done"], vm.ResultText);
        Assert.True(vm.Loading.IsReady);
        // The accepted fixture is terminal: do not press Compare/Adjust/Undo again.
    }
}
