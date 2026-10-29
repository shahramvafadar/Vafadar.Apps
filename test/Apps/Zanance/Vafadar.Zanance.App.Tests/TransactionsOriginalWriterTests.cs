using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original bulk writer rollback, persisted-source and accepted-notification boundaries.</summary>
public sealed class TransactionsOriginalWriterTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<TransactionsReadSnapshot> PrepareAsync(FlowFixture f)
    {
        await f.Store.GetSettingsAsync(Ct);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var account = new Account { Name = "Original reviewed bulk writer", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        db.Accounts.Add(account);
        db.Entries.Add(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1000,
            Date = new(2026, 10, 29), Title = "Original complete row", Review = ReviewState.Unreviewed, Tags = ["original"] });
        await db.SaveChangesAsync(Ct);
        return await f.Store.ReadTransactionsAsync(cancellationToken: Ct);
    }

    private static async Task WriteAsync(FlowFixture f, TransactionsReadSnapshot source, bool deleting,
        Func<bool> current, Action accepted)
    {
        if (deleting) await f.Store.DeleteTransactionsAsync(source, [source.Entries[0].Id], current, Ct, accepted);
        else
        {
            var changed = source.Entries[0].Copy(); changed.Review = ReviewState.Confirmed;
            Assert.True((await f.Store.SaveTransactionsAsync(source, [changed], current, Ct, accepted)).Succeeded);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_complete_source_checks_and_actual_SaveChanges_leave_the_callers_context(bool deleting)
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f);
        var caller = new SynchronizationContext(); var checks = 0; var stamps = 0; var accepted = 0;
        f.Time.BeforeRead = () => { Assert.NotSame(caller, SynchronizationContext.Current); stamps++; };
        var previous = SynchronizationContext.Current; Task pending;
        try
        {
            SynchronizationContext.SetSynchronizationContext(caller);
            pending = WriteAsync(f, source, deleting,
                () => { Assert.NotSame(caller, SynchronizationContext.Current); checks++; return true; },
                () => { Assert.NotSame(caller, SynchronizationContext.Current); accepted++; });
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        try { await pending; }
        finally { f.Time.BeforeRead = null; }
        Assert.True(checks >= 3); Assert.True(stamps > 0); Assert.Equal(1, accepted);
        Assert.Equal(deleting ? 0 : 1, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retirement_at_actual_SaveChanges_rolls_back_the_entire_original_writer(bool deleting)
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var active = true; var accepted = false;
        f.Time.BeforeRead = () => active = false;
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WriteAsync(f, source, deleting, () => active, () => accepted = true));
        }
        finally { f.Time.BeforeRead = null; }
        Assert.False(accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_persisted_edit_after_review_is_preserved_instead_of_overwritten_or_deleted(bool deleting)
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Entries.ExecuteUpdateAsync(set => set.SetProperty(entry => entry.Title, "New persisted original title"), Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var accepted = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => WriteAsync(f, source, deleting, () => true, () => accepted = true));
        Assert.False(accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_failed_postcommit_notification_retains_the_original_accepted_result(bool deleting)
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f); var accepted = false; var notifications = 0;
        f.Store.Changed += (_, _) => { notifications++; throw new IOException("Fictitious original bulk notification failure."); };
        var failure = await Record.ExceptionAsync(() => WriteAsync(f, source, deleting, () => true, () => accepted = true));
        Assert.True(accepted); Assert.Equal(1, notifications);
        if (deleting)
        {
            var committed = Assert.IsType<CommittedWriteNotificationException<IReadOnlyList<LedgerEntry>>>(failure);
            Assert.Equal(source.Entries[0].Id, Assert.Single(committed.Result).Id);
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        else
        {
            Assert.True(Assert.IsType<CommittedWriteNotificationException<SaveResult>>(failure).Result.Succeeded);
            var row = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.Equal(ReviewState.Confirmed, row.Review); Assert.Equal(1000, row.Amount); Assert.Equal(["original"], row.Tags);
        }
    }
}
