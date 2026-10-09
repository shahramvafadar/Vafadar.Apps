using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual snapshot publication/interaction boundary, including asynchronous, failed and repeated loads.</summary>
public sealed class SnapshotLoadTests
{
    [Fact, Trait("AT", "AT-83")]
    public void First_frame_cannot_show_results_or_accept_filter_input()
    {
        var load = new SnapshotLoadState();
        Assert.True(load.IsLoading); Assert.False(load.HasLoaded); Assert.False(load.HasFailed); Assert.False(load.IsReady);
    }

    [Fact, Trait("AT", "AT-83")]
    public async Task Repeated_loads_share_one_pending_read_and_one_publication()
    {
        var load = new SnapshotLoadState(); var read = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0; var publications = 0;
        var first = load.RunAsync(() => { calls++; return read.Task; }, () => publications++);
        var second = load.RunAsync(() => { calls++; return Task.CompletedTask; }, () => publications++);
        Assert.Same(first, second); Assert.Equal(1, calls); Assert.Equal(0, publications); Assert.False(load.IsReady);
        read.SetResult(); await first; await second;
        Assert.Equal(1, publications); Assert.True(load.IsReady);
    }

    [Fact, Trait("AT", "AT-83")]
    public async Task Results_are_published_before_any_ready_notification()
    {
        var load = new SnapshotLoadState(); var published = false; var notifications = 0;
        load.PropertyChanged += (_, change) =>
        {
            if (change.PropertyName == nameof(load.IsReady) && load.IsReady) { Assert.True(published); notifications++; }
        };
        await load.RunAsync(() => Task.CompletedTask, () =>
        {
            Assert.True(load.IsLoading); Assert.False(load.IsReady); published = true;
        });
        Assert.True(load.HasLoaded); Assert.True(notifications > 0);
    }

    [Fact, Trait("AT", "AT-83")]
    public async Task Synchronously_completed_reads_do_not_suppress_later_reloads()
    {
        var load = new SnapshotLoadState(); var publications = 0;
        await load.RunAsync(() => Task.CompletedTask, () => publications++);
        await load.RunAsync(() => Task.CompletedTask, () => publications++);
        Assert.Equal(2, publications); Assert.True(load.IsReady);
    }

    [Fact, Trait("AT", "AT-83")]
    public async Task A_failed_first_read_never_presents_partial_results_and_can_retry()
    {
        var load = new SnapshotLoadState(); var publications = 0;
        await Assert.ThrowsAsync<IOException>(() => load.RunAsync(() => Task.FromException(new IOException("Fictitious read failure")), () => publications++));
        Assert.Equal(0, publications); Assert.True(load.HasFailed); Assert.False(load.HasLoaded); Assert.False(load.IsReady); Assert.False(load.IsLoading);
        await load.RunAsync(() => Task.CompletedTask, () => publications++);
        Assert.Equal(1, publications); Assert.False(load.HasFailed); Assert.True(load.IsReady);
    }

    [Fact, Trait("AT", "AT-83")]
    public async Task A_failed_reload_keeps_prior_publication_but_never_exposes_stale_actions()
    {
        var load = new SnapshotLoadState();
        await load.RunAsync(() => Task.CompletedTask, () => { });
        await Assert.ThrowsAsync<IOException>(() => load.RunAsync(() => Task.FromException(new IOException("Fictitious reload failure")), () => Assert.Fail("Failed reads cannot publish")));
        Assert.True(load.HasLoaded); Assert.True(load.HasFailed); Assert.False(load.IsReady);
        await load.RunAsync(() => Task.CompletedTask, () => { });
        Assert.True(load.IsReady);
    }

    [Fact, Trait("AT", "AT-83")]
    public async Task Retrying_a_failed_reload_never_briefly_exposes_the_retained_snapshot()
    {
        var load = new SnapshotLoadState();
        await load.RunAsync(() => Task.CompletedTask, () => { });
        await Assert.ThrowsAsync<IOException>(() => load.RunAsync(() => Task.FromException(new IOException("Fictitious reload failure")), () => { }));
        var published = false;
        load.PropertyChanged += (_, _) => { if (load.IsReady) { Assert.True(published); } };
        var read = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var retry = load.RunAsync(() => read.Task, () => published = true);
        Assert.False(load.HasFailed); Assert.True(load.IsLoading); Assert.False(load.IsReady);
        read.SetResult(); await retry; Assert.True(load.IsReady);
    }

    [Fact, Trait("AT", "AT-83")]
    public async Task Presentation_failure_is_propagated_and_keeps_the_page_covered()
    {
        var load = new SnapshotLoadState();
        await Assert.ThrowsAsync<InvalidOperationException>(() => load.RunAsync(() => Task.CompletedTask, () => throw new InvalidOperationException("Fictitious presenter failure")));
        Assert.True(load.HasFailed); Assert.False(load.HasLoaded); Assert.False(load.IsReady);
        await load.RunAsync(() => Task.CompletedTask, () => { }); Assert.True(load.IsReady);
    }

    [Fact, Trait("AT", "AT-83")]
    public async Task Cancellation_releases_the_pending_load_without_presenting_and_allows_retry()
    {
        var load = new SnapshotLoadState(); using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var operation = load.RunAsync(() => Task.FromCanceled(cancelled.Token), () => Assert.Fail("Cancelled reads cannot publish"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.True(operation.IsCanceled); Assert.False(load.IsLoading); Assert.False(load.IsReady);
        await load.RunAsync(() => Task.CompletedTask, () => { }); Assert.True(load.IsReady);
    }

    [Theory, InlineData(true), InlineData(false), Trait("AT", "AT-83")]
    public void Missing_callbacks_are_rejected_without_starting_a_read(bool missingRead)
    {
        var load = new SnapshotLoadState();
        Assert.Throws<ArgumentNullException>(() => { _ = load.RunAsync(missingRead ? null! : () => Task.CompletedTask, missingRead ? () => { } : null!); });
        Assert.False(load.HasLoaded); Assert.False(load.HasFailed);
    }

    [Fact, Trait("AT", "AT-83")]
    public async Task Publishing_a_real_store_snapshot_preserves_entry_identity_and_transfer_totals()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken; var day = new DateOnly(2026, 10, 9);
        var source = new Account { Name = "Fictitious source", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = day };
        var destination = new Account { Name = "Fictitious destination", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = day };
        await f.Store.SaveAccountAsync(source, ct); await f.Store.SaveAccountAsync(destination, ct);
        var transfer = new LedgerEntry { AccountId = source.Id, ToAccountId = destination.Id, Amount = 1234, ToAmount = 1234, Kind = EntryKind.Transfer, Date = day };
        Assert.True((await f.Store.SaveEntryAsync(transfer, ct)).Succeeded);
        List<LedgerEntry> snapshot = []; List<LedgerEntry> presented = []; var load = new SnapshotLoadState();
        await load.RunAsync(async () => snapshot = await f.Store.GetEntriesAsync(cancellationToken: ct), () => presented = snapshot);
        Assert.True(load.IsReady); Assert.Same(snapshot, presented); Assert.Equal(transfer.Id, Assert.Single(presented).Id);
        var after = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: ct));
        Assert.Equal((transfer.Id, transfer.Kind, transfer.Amount, transfer.ToAccountId), (after.Id, after.Kind, after.Amount, after.ToAccountId));
        var accounts = await f.Store.GetAccountsAsync(cancellationToken: ct);
        Assert.Equal(10000, LedgerCalculator.TotalBalances(accounts, presented, day)["EUR"]);
        Assert.Empty(LedgerCalculator.Totals(accounts, presented, new LedgerFilter(day, day)));
    }
}
