using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-414: original holding deletion/Undo preserves file ownership, refunds and quantity history.</summary>
[Trait("AT", "AT-414")]
public sealed class HoldingHistoryUndoFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 1);
    private sealed record FixtureRows(HoldingStore Holdings, Account Cash, AssetType Type, AssetLocation Place,
        AssetEvent Purchase, LedgerEntry Fee, LedgerEntry Refund);
    private static async Task<FixtureRows> PrepareAsync(FlowFixture f)
    {
        await f.Store.EnsureDefaultCategoriesAsync(Ct); var holdings = f.Services.GetRequiredService<HoldingStore>();
        var cash = new Account { Name = "D414 original cash", CurrencyCode = "EUR", OpeningDate = Day, OpeningBalance = 90000 };
        await f.Store.SaveAccountAsync(cash, Ct);
        var type = new AssetType { Name = "D414 original holding", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await holdings.SaveTypeAsync(type, Ct); var place = await holdings.EnsureDefaultLocationAsync("D414 original place", Ct);
        var purchase = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Date = Day,
            Kind = AssetEventKind.Purchase, Quantity = 2000, BasisAmount = 5000, Note = "Original source metadata" };
        var payment = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Date = Day, Amount = 5000 };
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Date = Day, Amount = 200,
            CategoryId = await holdings.FeesCategoryAsync(Ct), Note = "Original fee metadata", Tags = ["original-fee"] };
        Assert.True((await holdings.SaveEventAsync(purchase, [payment, fee], true, Ct)).Succeeded);
        var refund = EntryActions.CreateRefund(fee, 150, cash.Id, Day.AddDays(1)); refund.Note = "Original linked refund";
        Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        return new(holdings, cash, type, place, purchase, fee, refund);
    }
    private static async Task<HoldingHistoryEditor> EditorAsync(FlowFixture f, FixtureRows r)
    {
        var editor = new HoldingHistoryEditor(r.Holdings, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Platform);
        editor.PublishDisplayed(await r.Holdings.ReadPriceReviewAsync(r.Type.Id, Ct));
        editor.RefreshView = async () => await r.Holdings.ReadPriceReviewAsync(r.Type.Id, Ct);
        return editor;
    }
    private static void ChooseDelete(FlowFixture f)
    { f.Platform.Choices.Enqueue(f.Translator["Common_Delete"]); f.Platform.Confirmations.Enqueue(true); }
    private static async Task SqlAsync(FlowFixture f, string sql)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }
    [Fact]
    public async Task Original_fee_refund_link_and_receipt_return_with_the_same_deleted_holding_group()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f);
        var receipt = new EntryAttachment { EntryId = r.Fee.Id, FileName = "original-fictitious.bin",
            ContentType = "application/octet-stream", Data = [1, 2, 3] };
        await f.Store.AddAttachmentAsync(receipt, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var result = await r.Holdings.DeleteEventAsync(r.Purchase.Id, Ct);
        Assert.NotNull(result.Removed); Assert.Null(result.Conflict);
        var kept = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(r.Refund.Id, kept.Id); Assert.Null(kept.RefundOfId); Assert.Equal(150, kept.Amount);
        await r.Holdings.RestoreAsync(result.Removed!, Ct);
        Assert.Equal(r.Fee.Id, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Single(e => e.Id == r.Refund.Id).RefundOfId);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Retained_short_undo_cannot_be_inserted_into_another_file_with_the_same_original_ids()
    {
        using var source = new FlowFixture(); var r = await PrepareAsync(source);
        var removed = (await r.Holdings.DeleteEventAsync(r.Purchase.Id, Ct)).Removed!;
        using var target = new FlowFixture();
        await target.Store.EnsureDefaultCategoriesAsync(Ct);
        if (r.Fee.CategoryId is { } categoryId && !(await target.Store.GetCategoriesAsync(Ct)).Any(c => c.Id == categoryId))
            await target.Store.SaveCategoryAsync(new Category(categoryId) { Name = "D414 existing category", Kind = CategoryKind.Expense }, Ct);
        await target.Store.SaveAccountAsync(new Account(r.Cash.Id) { Name = r.Cash.Name, CurrencyCode = "EUR", OpeningDate = Day }, Ct);
        var holdings = target.Services.GetRequiredService<HoldingStore>();
        await holdings.SaveTypeAsync(new AssetType(r.Type.Id) { Name = r.Type.Name, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" }, Ct);
        await holdings.SaveLocationAsync(new AssetLocation(r.Place.Id) { Name = r.Place.Name }, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(target);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => holdings.RestoreAsync(removed, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(target));
    }
    [Fact]
    public async Task Restoring_a_deleted_outflow_cannot_make_the_changed_remaining_history_negative()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f);
        var outflow = new AssetEvent { AssetTypeId = r.Type.Id, LocationId = r.Place.Id, Date = Day.AddDays(2),
            Kind = AssetEventKind.Outflow, Quantity = 2000 };
        Assert.True((await r.Holdings.SaveEventAsync(outflow, [], Ct)).Succeeded);
        var removed = (await r.Holdings.DeleteEventAsync(outflow.Id, Ct)).Removed!;
        Assert.True((await r.Holdings.SaveEventAsync(new AssetEvent { AssetTypeId = r.Type.Id, LocationId = r.Place.Id,
            Date = Day.AddDays(3), Kind = AssetEventKind.Outflow, Quantity = 1000 }, [], Ct)).Succeeded);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => r.Holdings.RestoreAsync(removed, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Mutating_a_deleted_group_cannot_turn_undo_into_an_unreviewed_quantity_write()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f);
        var removed = (await r.Holdings.DeleteEventAsync(r.Purchase.Id, Ct)).Removed!;
        removed.Events[0].Quantity = 999000;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => r.Holdings.RestoreAsync(removed, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancelled_original_history_menu_or_delete_confirmation_keeps_every_original_row(bool confirm)
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f); var editor = await EditorAsync(f, r);
        f.Platform.Choices.Enqueue(confirm ? f.Translator["Common_Delete"] : null);
        f.Platform.Confirmations.Enqueue(false); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.OpenCommand.ExecuteAsync(r.Purchase.Id);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(editor.HasUndo);
        Assert.False(editor.DeleteCommitted); Assert.True(editor.CanOpen); Assert.Null(editor.Error);
    }
    [Fact]
    public async Task Changed_displayed_quantity_refuses_before_offering_an_unreviewed_history_menu()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f); var editor = await EditorAsync(f, r);
        Assert.True((await r.Holdings.SaveEventAsync(new AssetEvent { AssetTypeId = r.Type.Id, LocationId = r.Place.Id,
            Kind = AssetEventKind.Opening, Date = Day.AddDays(2), Quantity = 1000 }, [], Ct)).Succeeded);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await editor.OpenCommand.ExecuteAsync(r.Purchase.Id);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.OfferedActions);
        Assert.False(editor.CanOpen); Assert.True(editor.NeedsRefresh);
        Assert.Equal(f.Translator["Holding_HistorySourceChanged"], editor.Error);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_pending_delete_confirmation_rejects_changed_fee_or_departed_owner(bool depart)
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f); var editor = await EditorAsync(f, r);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.Choices.Enqueue(f.Translator["Common_Delete"]);
        f.Platform.PendingConfirmation = () => { entered.SetResult(); return pending.Task; };
        var deleting = editor.OpenCommand.ExecuteAsync(r.Purchase.Id); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        if (depart) editor.Retire();
        else
        {
            var source = (await r.Holdings.GetEventsAsync(r.Type.Id, Ct)).Single();
            var entries = (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Where(e => e.GroupId == source.GroupId).ToList();
            entries.Single(e => e.Id == r.Fee.Id).Note = "Original fee changed while confirmation was pending";
            Assert.True((await r.Holdings.SaveEventAsync(source, entries, true, Ct)).Succeeded);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); pending.SetResult(true); await deleting;
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(editor.HasUndo);
        Assert.False(editor.DeleteCommitted); Assert.True(editor.NeedsRefresh); Assert.False(editor.CanOpen);
    }
    [Fact]
    public async Task Known_delete_and_undo_commits_keep_their_exact_outcome_when_both_notifications_fail()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f); var editor = await EditorAsync(f, r);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); ChooseDelete(f);
        void Fail(object? sender, EventArgs args) => throw new IOException("Original notification failure");
        r.Holdings.Changed += Fail;
        await editor.OpenCommand.ExecuteAsync(r.Purchase.Id);
        Assert.True(editor.DeleteCommitted); Assert.True(editor.CanUndo); Assert.True(editor.HasUndo);
        Assert.Equal(f.Translator["Holding_HistoryDeletedRefresh"], editor.Error); Assert.False(editor.CanOpen);
        var deleted = await GoalChooserFlowTests.SnapshotAsync(f); await editor.OpenCommand.ExecuteAsync(r.Purchase.Id);
        Assert.Equal(deleted, await GoalChooserFlowTests.SnapshotAsync(f));
        await editor.UndoCommand.ExecuteAsync(null); r.Holdings.Changed -= Fail;
        Assert.True(editor.UndoCommitted); Assert.False(editor.HasUndo); Assert.False(editor.CanUndo);
        Assert.Equal(f.Translator["Holding_HistoryRestoredRefresh"], editor.Error);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); await editor.UndoCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await editor.RefreshCommand.ExecuteAsync(null); Assert.True(editor.CanOpen); Assert.Null(editor.Error);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Failed_undo_persistence_keeps_the_original_offer_and_retries_only_the_uncommitted_write()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f); var editor = await EditorAsync(f, r);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); ChooseDelete(f); await editor.OpenCommand.ExecuteAsync(r.Purchase.Id);
        Assert.True(editor.CanUndo);
        await SqlAsync(f, "CREATE TRIGGER RejectHoldingUndo BEFORE INSERT ON AssetEvents BEGIN SELECT RAISE(ABORT, 'Original undo persistence failure'); END;");
        var deleted = await GoalChooserFlowTests.SnapshotAsync(f); await editor.UndoCommand.ExecuteAsync(null);
        Assert.Equal(deleted, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(editor.HasUndo); Assert.True(editor.CanUndo);
        Assert.False(editor.UndoCommitted); Assert.Equal(f.Translator["Holding_HistoryUndoFailed"], editor.Error);
        await SqlAsync(f, "DROP TRIGGER RejectHoldingUndo;"); await editor.UndoCommand.ExecuteAsync(null);
        Assert.True(editor.UndoCommitted); Assert.False(editor.HasUndo); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Known_delete_and_undo_remain_accepted_when_only_their_following_readback_fails()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f); var editor = await EditorAsync(f, r);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        editor.RefreshView = () => throw new IOException("Original readback failure"); ChooseDelete(f);
        await editor.OpenCommand.ExecuteAsync(r.Purchase.Id);
        Assert.True(editor.DeleteCommitted); Assert.True(editor.CanUndo); Assert.Equal(f.Translator["Holding_HistoryDeletedRefresh"], editor.Error);
        await editor.UndoCommand.ExecuteAsync(null);
        Assert.True(editor.UndoCommitted); Assert.False(editor.CanUndo); Assert.Equal(f.Translator["Holding_HistoryRestoredRefresh"], editor.Error);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        editor.RefreshView = async () => await r.Holdings.ReadPriceReviewAsync(r.Type.Id, Ct);
        await editor.RefreshCommand.ExecuteAsync(null);
        Assert.Null(editor.Error); Assert.True(editor.CanOpen); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData("refund")]
    [InlineData("attachment")]
    [InlineData("purge")]
    public async Task Changed_retained_refund_or_orphan_receipt_disables_unsafe_undo_without_partial_money(string change)
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f);
        var receipt = new EntryAttachment { EntryId = r.Fee.Id, FileName = "original-fictitious.bin",
            ContentType = "application/octet-stream", Data = [1, 2, 3] }; await f.Store.AddAttachmentAsync(receipt, Ct);
        var editor = await EditorAsync(f, r); ChooseDelete(f); await editor.OpenCommand.ExecuteAsync(r.Purchase.Id);
        if (change == "refund")
        {
            var refund = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)); refund.Note = "Explicit later retained-refund edit";
            Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        }
        else if (change == "purge") Assert.Equal(1, await f.Store.PurgeOrphanAttachmentsAsync(Ct));
        else
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            await db.Attachments.Where(a => a.Id == receipt.Id).ExecuteUpdateAsync(set => set.SetProperty(a => a.Data, new byte[] { 9, 8, 7 }), Ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await editor.UndoCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(editor.HasUndo); Assert.False(editor.CanUndo);
        Assert.Equal(f.Translator["Holding_HistoryUndoChanged"], editor.Error); Assert.False(editor.UndoCommitted);
        await editor.RefreshCommand.ExecuteAsync(null); Assert.False(editor.CanUndo);
        await editor.UndoCommand.ExecuteAsync(null); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task A_shared_legacy_money_group_cannot_be_deleted_by_guessing_which_holding_owns_it()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            // Simulate an already stored legacy ambiguity; the new original action must not choose an owner.
            db.AssetEvents.Add(new AssetEvent { AssetTypeId = r.Type.Id, LocationId = r.Place.Id, Date = Day,
                Kind = AssetEventKind.Opening, Quantity = 1000, GroupId = r.Purchase.GroupId });
            await db.SaveChangesAsync(Ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => r.Holdings.DeleteEventAsync(r.Purchase.Id, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Departed_uncommitted_undo_keeps_its_original_offer_for_explicit_return_and_retry()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f); var editor = await EditorAsync(f, r);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); ChooseDelete(f); await editor.OpenCommand.ExecuteAsync(r.Purchase.Id);
        var deleted = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Time.BeforeRead = () => { f.Time.BeforeRead = null; editor.Retire(); };
        await editor.UndoCommand.ExecuteAsync(null);
        Assert.Equal(deleted, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(editor.HasUndo); Assert.True(editor.CanUndo);
        Assert.False(editor.UndoCommitted); Assert.Equal(f.Translator["Holding_HistoryUndoFailed"], editor.Error);
        await editor.RefreshCommand.ExecuteAsync(null); await editor.UndoCommand.ExecuteAsync(null);
        Assert.True(editor.UndoCommitted); Assert.False(editor.HasUndo); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Consumed_receipt_never_rearms_after_repeated_restore_attempts()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f);
        var removed = (await r.Holdings.DeleteEventAsync(r.Purchase.Id, Ct)).Removed!;
        await r.Holdings.RestoreAsync(removed, Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        for (var attempt = 0; attempt < 2; attempt++)
            await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => r.Holdings.RestoreAsync(removed, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Original_undo_retired_after_sql_save_rolls_back_and_keeps_its_uncommitted_receipt()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var removed = (await r.Holdings.DeleteEventAsync(r.Purchase.Id, Ct)).Removed!; var deleted = await GoalChooserFlowTests.SnapshotAsync(f);
        var calls = 0;
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => r.Holdings.RestoreReviewedHoldingAsync(removed, () => ++calls == 1, Ct));
        Assert.Equal(2, calls); Assert.Equal(deleted, await GoalChooserFlowTests.SnapshotAsync(f));
        await r.Holdings.RestoreReviewedHoldingAsync(removed, () => true, Ct);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Real_advancing_audit_time_preserves_original_creation_money_and_refund_metadata()
    {
        using var f = new FlowFixture(); var r = await PrepareAsync(f);
        var original = (await f.Store.GetEntriesAsync(cancellationToken: Ct)).ToDictionary(e => e.Id);
        var removed = (await r.Holdings.DeleteEventAsync(r.Purchase.Id, Ct)).Removed!;
        f.Time.Now += TimeSpan.FromHours(1); await r.Holdings.RestoreAsync(removed, Ct);
        var restored = (await f.Store.GetEntriesAsync(cancellationToken: Ct)).ToDictionary(e => e.Id);
        Assert.Equal(original.Count, restored.Count);
        foreach (var item in original.Values)
        {
            Assert.Equal(item.CreatedAt, restored[item.Id].CreatedAt); Assert.Equal(item.Amount, restored[item.Id].Amount);
            Assert.Equal(item.Note, restored[item.Id].Note); Assert.Equal(item.Tags, restored[item.Id].Tags);
        }
        Assert.Equal(r.Fee.Id, restored[r.Refund.Id].RefundOfId);
        Assert.Equal(original[r.Refund.Id].UpdatedAt, restored[r.Refund.Id].UpdatedAt);
        Assert.True(restored[r.Fee.Id].UpdatedAt > original[r.Fee.Id].UpdatedAt);
    }
}
