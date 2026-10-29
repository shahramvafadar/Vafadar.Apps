using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original file-bound Undo keeps its deadline and acceptance across page retirement and subscriber failure.</summary>
public sealed class TransactionsOriginalUndoOwnershipTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<UndoService> PrepareAsync(FlowFixture f)
    {
        var account = new Account { Name = "Original Undo ownership", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1000, Date = new(2026, 10, 29) };
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded);
        var undo = new UndoService(f.Store, f.Time); undo.Offer(await f.Store.DeleteEntryAsync(entry.Id, Ct)); return undo;
    }

    [Fact]
    public async Task A_hidden_original_page_cannot_consume_an_existing_Undo_offer()
    {
        using var f = new FlowFixture(); var undo = await PrepareAsync(f); var refreshed = 0;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var command = new TransactionUndoViewModel(undo, f.Platform, () => { refreshed++; return Task.CompletedTask; }, () => false, f.Translator);
        await command.UndoDeleteCommand.ExecuteAsync(null);
        Assert.True(undo.CanUndo); Assert.Equal(TimeSpan.FromSeconds(8), undo.Remaining); Assert.Equal(0, refreshed);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Retirement_during_the_original_restore_writer_rolls_back_without_renewing_its_deadline()
    {
        using var f = new FlowFixture(); var undo = await PrepareAsync(f); var active = true;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Time.BeforeRead = () => active = false;
        try { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => undo.UndoAsync(() => active)); }
        finally { f.Time.BeforeRead = null; }
        Assert.True(undo.CanUndo); Assert.False(undo.LastAccepted); Assert.Equal(TimeSpan.FromSeconds(8), undo.Remaining);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_committed_restore_is_consumed_before_a_failed_original_notification()
    {
        using var f = new FlowFixture(); var undo = await PrepareAsync(f); var refreshed = 0;
        f.Store.Changed += (_, _) => throw new IOException("Fictitious original Undo subscriber failure.");
        var command = new TransactionUndoViewModel(undo, f.Platform, () => { refreshed++; return Task.CompletedTask; }, () => true, f.Translator);
        await command.UndoDeleteCommand.ExecuteAsync(null);
        Assert.True(undo.LastAccepted); Assert.False(undo.CanUndo); Assert.Equal(0, refreshed);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(f.Platform.Failures);
        Assert.Contains(f.Platform.Alerts, alert => alert.Message == f.Translator["Occurrence_SavedRefreshFailed"]);
    }
}
