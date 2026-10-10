using Vafadar.Zanance.App.Presentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-129: real application Undo offers survive failure without extending their original deadline.</summary>
[Trait("AT", "AT-129")]
public sealed class UndoRetryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task Failed_action_keeps_the_same_offer_and_deadline_for_retry()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var fail = true; var calls = 0;
        undo.Offer(() => { calls++; if (fail) throw new InvalidOperationException("Owned action failure"); return Task.CompletedTask; });
        f.Time.Now = f.Time.Now.AddSeconds(3);
        await Assert.ThrowsAsync<InvalidOperationException>(() => undo.UndoAsync());
        Assert.True(undo.CanUndo); Assert.Equal(TimeSpan.FromSeconds(5), undo.Remaining);
        fail = false; await undo.UndoAsync(); await undo.UndoAsync(); Assert.Equal(2, calls); Assert.False(undo.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Older_completion_or_failure_never_dismisses_a_newer_offer(bool fail)
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldCalls = 0; var newCalls = 0;
        undo.Offer(async () => { oldCalls++; await pending.Task.WaitAsync(Ct); if (fail) throw new InvalidOperationException("Owned old action failure"); });
        var operation = undo.UndoAsync(); Assert.False(undo.CanUndo);
        f.Time.Now = f.Time.Now.AddSeconds(2); undo.Offer(() => { newCalls++; return Task.CompletedTask; });
        Assert.False(undo.CanUndo); await undo.UndoAsync(); Assert.Equal(0, newCalls);
        pending.SetResult(); if (fail) await Assert.ThrowsAsync<InvalidOperationException>(() => operation); else await operation;
        Assert.True(undo.CanUndo); Assert.Equal(TimeSpan.FromSeconds(8), undo.Remaining);
        await undo.UndoAsync(); Assert.Equal(1, oldCalls); Assert.Equal(1, newCalls); Assert.False(undo.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dismissal_during_an_older_Undo_never_revives_its_offer(bool fail)
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        undo.Offer(async () => { calls++; await pending.Task.WaitAsync(Ct); if (fail) throw new InvalidOperationException("Owned dismissed action failure"); });
        var operation = undo.UndoAsync(); undo.Dismiss(); pending.SetResult();
        if (fail) await Assert.ThrowsAsync<InvalidOperationException>(() => operation); else await operation;
        Assert.False(undo.CanUndo); await undo.UndoAsync(); Assert.Equal(1, calls);
    }

    [Fact]
    public async Task A_second_tap_during_pending_Undo_never_executes_the_action_twice()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        undo.Offer(async () => { calls++; await pending.Task.WaitAsync(Ct); });
        var operation = undo.UndoAsync(); await undo.UndoAsync(); Assert.Equal(1, calls); pending.SetResult(); await operation;
        Assert.False(undo.CanUndo); await undo.UndoAsync(); Assert.Equal(1, calls);
    }

    [Fact]
    public async Task A_failed_operation_never_renews_an_expired_offer()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var calls = 0;
        undo.Offer(() => { calls++; f.Time.Now = f.Time.Now.AddSeconds(8); throw new InvalidOperationException("Owned late failure"); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => undo.UndoAsync());
        Assert.False(undo.CanUndo); Assert.Equal(TimeSpan.Zero, undo.Remaining); await undo.UndoAsync(); Assert.Equal(1, calls);
    }

    [Fact]
    public async Task A_throwing_offer_observer_never_leaves_Undo_stuck_in_progress()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var calls = 0;
        undo.Offer(() => { calls++; return Task.CompletedTask; });
        void Reject(object? sender, EventArgs args) => throw new InvalidOperationException("Owned observer failure");
        undo.Changed += Reject; await Assert.ThrowsAsync<InvalidOperationException>(() => undo.UndoAsync()); undo.Changed -= Reject;
        Assert.True(undo.CanUndo); Assert.Equal(0, calls); await undo.UndoAsync(); Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Failed_real_ledger_Undo_keeps_its_deletion_batch_and_refunds_for_a_successful_retry()
    {
        using var f = new FlowFixture(); var account = new Account { Name = "Owned cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 9) };
        await f.Store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Date = account.OpeningDate, Kind = EntryKind.Expense, Amount = 1000, Title = "Retained financial identity", Note = "Complete original note", Tags = ["retained"] };
        await f.Store.SaveEntryAsync(entry, Ct); var refund = EntryActions.CreateRefund(entry, 300, account.Id, account.OpeningDate);
        await f.Store.SaveEntryAsync(refund, Ct); var deleted = await f.Store.DeleteEntryAsync(entry.Id, Ct);
        var before = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        var undo = new UndoService(f.Store, f.Time); undo.Offer(deleted); f.Time.Now = f.Time.Now.AddSeconds(2);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectActualUndo BEFORE INSERT ON Entries BEGIN SELECT RAISE(ABORT,'Owned application Undo failure'); END;", Ct);
        await Assert.ThrowsAsync<DbUpdateException>(() => undo.UndoAsync()); Assert.True(undo.CanUndo); Assert.Equal(TimeSpan.FromSeconds(6), undo.Remaining);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)));
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectActualUndo", Ct); await undo.UndoAsync(); Assert.False(undo.CanUndo);
        var restored = await f.Store.GetEntryAsync(entry.Id, Ct); Assert.Equal(entry.CreatedAt, restored!.CreatedAt); Assert.Equal(entry.Note, restored.Note); Assert.Equal(entry.Tags, restored.Tags);
        Assert.Equal(refund.Id, Assert.Single(await f.Store.GetRefundsAsync(entry.Id, Ct)).Id);
    }
}
