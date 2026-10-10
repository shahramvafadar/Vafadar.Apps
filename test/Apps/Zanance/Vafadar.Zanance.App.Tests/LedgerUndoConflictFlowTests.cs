using System.Text.Json;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-131: the actual command reports an obsolete financial Undo while retaining every newer edit.</summary>
[Trait("AT", "AT-131")]
public sealed class LedgerUndoConflictFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Stale_refund_Undo_reports_through_the_actual_command_without_writes_or_deadline_extension()
    {
        using var f = new FlowFixture(); var account = new Account { Name = "Owned EUR", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 10) };
        await f.Store.SaveAccountAsync(account, Ct);
        var purchase = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Date = account.OpeningDate, Amount = 1000 };
        Assert.True((await f.Store.SaveEntryAsync(purchase, Ct)).Succeeded);
        var refund = EntryActions.CreateRefund(purchase, 300, account.Id, account.OpeningDate); await f.Store.SaveEntryAsync(refund, Ct);
        var undo = new UndoService(f.Store, f.Time); undo.Offer(await f.Store.DeleteEntryAsync(purchase.Id, Ct));
        var current = (await f.Store.GetEntryAsync(refund.Id, Ct))!; current.Amount = 2000; current.Note = "Later explicit edit";
        Assert.True((await f.Store.SaveEntryAsync(current, Ct)).Succeeded);
        var before = JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)); var refreshed = 0;
        var action = new TransactionUndoViewModel(undo, f.Platform, () => { refreshed++; return Task.CompletedTask; });
        f.Time.Now = f.Time.Now.AddSeconds(2); await action.UndoDeleteCommand.ExecuteAsync(null);
        Assert.IsType<InvalidOperationException>(Assert.Single(f.Platform.Failures)); Assert.Equal(0, refreshed);
        Assert.Equal(before, JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)));
        Assert.True(undo.CanUndo); Assert.Equal(TimeSpan.FromSeconds(6), undo.Remaining); Assert.Null(await f.Store.GetEntryAsync(purchase.Id, Ct));
        f.Time.Now = f.Time.Now.AddSeconds(6); await action.UndoDeleteCommand.ExecuteAsync(null);
        Assert.False(undo.CanUndo); Assert.Single(f.Platform.Failures); Assert.Equal(1, refreshed);
        Assert.Equal(before, JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)));
    }
}
