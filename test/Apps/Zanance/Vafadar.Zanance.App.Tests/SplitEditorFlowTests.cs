using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-407: actual original split session owns drafts, retirement and known committed outcomes.</summary>
[Trait("AT", "AT-407")]
public sealed class SplitEditorFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<LedgerEntry> SeedAsync(FlowFixture f)
    {
        var account = new Account { Name = "Fictitious original split", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Expense,
            Amount = 10000, Title = "Fictitious reviewed purchase", Note = "Original note" };
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded); return entry;
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Changed_original_or_pending_input_keeps_original_rows_without_acceptance(bool source)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new SplitEditorSession(f.Store);
        Assert.NotNull(await session.LoadAsync(entry.Id));
        if (source) { entry.Note = "Newer original note"; await f.Store.SaveEntryAsync(entry, Ct); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<SplitReviewChangedException>(() => session.SaveAsync([(null, 6000), (null, 4000)], false, () => source));
        Assert.False(session.Committed);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        session.Retire(); Assert.Null(session.Review);
        Assert.Null(await session.SaveAsync([(null, 6000), (null, 4000)], false, () => true));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_committed_split_with_failed_notification_cannot_be_replayed_or_reloaded_as_a_new_operation()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new SplitEditorSession(f.Store);
        await session.LoadAsync(entry.Id);
        f.Store.Changed += (_, _) => throw new InvalidOperationException("Deliberate original post-commit notification failure.");
        await Assert.ThrowsAsync<CommittedWriteNotificationException<SaveResult>>(() => session.SaveAsync([(null, 6000), (null, 4000)], false, () => true));
        Assert.True(session.Committed); Assert.Null(session.Review);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.Null(await session.SaveAsync([(null, 6000), (null, 4000)], false, () => true));
        Assert.Null(await session.LoadAsync(entry.Id));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        var saved = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(2, saved.Count); Assert.Equal(10000, saved.Sum(e => e.Amount));
    }
}
