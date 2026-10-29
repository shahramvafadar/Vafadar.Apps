using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original bulk consent must not cross a replacement of its actual source publication.</summary>
public sealed class TransactionsOriginalBulkOwnershipTests
{
    [Fact]
    public async Task A_confirmation_from_the_previous_publication_cannot_delete_original_rows()
    {
        var ct = TestContext.Current.CancellationToken;
        using var f = new FlowFixture();
        var account = new Account { Name = "Original bulk ownership", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(account, ct);
        var row = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1000,
            Date = new(2026, 10, 29), Title = "Original reviewed row" };
        Assert.True((await f.Store.SaveEntryAsync(row, ct)).Succeeded);
        var undo = new UndoService(f.Store, f.Time);
        var vm = new BulkTransactionsViewModel(f.Store, f.Translator, f.Platform, undo, () => Task.CompletedTask);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: ct);
        vm.Load(rows, [], _ => string.Empty); vm.Start(); vm.Toggle(row.Id);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.PendingConfirmation = () =>
        {
            vm.Load(rows, [], _ => string.Empty);
            return Task.FromResult(true);
        };
        await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(undo.CanUndo); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: ct));
    }
}
