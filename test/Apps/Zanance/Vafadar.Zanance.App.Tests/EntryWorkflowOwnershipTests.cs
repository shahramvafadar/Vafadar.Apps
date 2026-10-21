using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-428: original nested account, individual aggregate decisions, atomic adjustment and exact short Undo.</summary>
[Trait("AT", "AT-428")]
public sealed class EntryWorkflowOwnershipTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Today = new(2026, 10, 9);
    private static async Task<(LedgerEntry Aggregate, LedgerEntry Detail)> SeedAsync(FlowFixture f)
    {
        var account = new Account { Name = "Fictitious cash", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var aggregate = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 10000,
            Date = new(2020, 1, 1), IsAggregated = true, AggregatedFrom = new(2020, 1, 1), AggregatedTo = new(2030, 12, 31), Title = "Original broad aggregate" };
        Assert.True((await f.Store.SaveEntryAsync(aggregate, Ct)).Succeeded);
        return (aggregate, new() { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 2000, Date = Today, Title = "Explicit detail" });
    }
    private static async Task<(EntryEditorReadSnapshot Source, EntryAggregateReview Preview, EntryEditorSaveSession Save)> ReviewAsync(
        FlowFixture f, IReadOnlyList<LedgerEntry> proposal, Guid? editId = null)
    {
        var source = await f.Store.ReadEntryEditorSnapshotAsync(new(Today, EditId: editId), ct: Ct);
        return (source, await f.Store.ReadEditorAggregatesAsync(source, proposal, [], Ct), new(f.Store, source.Inputs.Resources, source));
    }
    private static IReadOnlyList<ImportAggregateChoice> Link(EntryAggregateReview review) => [.. review.Overlaps.Select(o => new ImportAggregateChoice(o, true))];
    private static LedgerEntry NewAggregate(LedgerEntry original)
    {
        var next = EntryActions.Duplicate(original, original.Date);
        next.IsAggregated = true; next.AggregatedFrom = original.AggregatedFrom; next.AggregatedTo = original.AggregatedTo;
        return next;
    }

    [Fact]
    public async Task Wide_range_link_and_short_undo_keep_manual_source_and_exact_detail()
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f);
        var (_, preview, save) = await ReviewAsync(f, [detail]);
        Assert.Equal(aggregate.Id, Assert.Single(preview.Overlaps).Aggregate.Id);
        Assert.True((await save.SaveAsync([detail], [], detail.Id, null, () => true, preview, Link(preview))).Succeeded);
        Assert.Equal(8000, (await f.Store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        var saved = (await f.Store.GetEntryAsync(detail.Id, Ct))!;
        Assert.Equal(detail.Source, saved.Source); Assert.Null(saved.ImportBatchId); Assert.Empty(await f.Store.GetImportBatchesAsync(Ct));
        await f.Store.UndoEditorAggregateAsync(Assert.IsType<EntryAggregateUndo>(save.AggregateUndo), Ct);
        Assert.Equal(10000, (await f.Store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        Assert.Equal(2000, (await f.Store.GetEntryAsync(detail.Id, Ct))!.Amount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.UndoEditorAggregateAsync(save.AggregateUndo!, Ct));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Ordinary_detail_or_aggregate_edit_does_not_subtract_existing_coverage_again(bool editAggregate)
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f);
        var (_, first, save) = await ReviewAsync(f, [detail]);
        await save.SaveAsync([detail], [], detail.Id, null, () => true, first, Link(first));
        var edit = (await f.Store.GetEntryAsync(editAggregate ? aggregate.Id : detail.Id, Ct))!;
        edit.Amount += 100; edit.Note = "Explicit later correction";
        var (_, next, correction) = await ReviewAsync(f, [edit], edit.Id); Assert.Empty(next.Overlaps);
        Assert.True((await correction.SaveAsync([edit], [], edit.Id, null, () => true, next, [])).Succeeded);
        Assert.Equal(editAggregate ? 8100 : 8000, (await f.Store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.UndoEditorAggregateAsync(save.AggregateUndo!, Ct));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Changed_or_new_overlap_after_preview_rolls_back_the_entire_editor(bool initiallyEmpty)
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f);
        if (initiallyEmpty) { aggregate.AggregatedTo = new(2025, 1, 1); await f.Store.SaveEntryAsync(aggregate, Ct); }
        var (_, preview, save) = await ReviewAsync(f, [detail]);
        aggregate.Amount += 10; aggregate.AggregatedTo = new(2030, 12, 31); await f.Store.SaveEntryAsync(aggregate, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<EntryEditorSourceChangedException>(() => save.SaveAsync([detail], [], detail.Id, null, () => true, preview, Link(preview)));
        Assert.False(save.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Every_overlap_has_its_own_choice_and_one_detail_cannot_reduce_two()
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f);
        var other = NewAggregate(aggregate); other.Title = "Other reviewed aggregate";
        await f.Store.SaveEntryAsync(other, Ct);
        var (_, preview, save) = await ReviewAsync(f, [detail]); Assert.Equal(2, preview.Overlaps.Count);
        await Assert.ThrowsAsync<EntryEditorSourceChangedException>(() => save.SaveAsync([detail], [], detail.Id, null, () => true, preview, Link(preview)));
        Assert.False(save.Accepted); Assert.Null(await f.Store.GetEntryAsync(detail.Id, Ct));
        var choices = preview.Overlaps.Select(o => new ImportAggregateChoice(o, o.Aggregate.Id == aggregate.Id)).ToArray();
        Assert.True((await save.SaveAsync([detail], [], detail.Id, null, () => true, preview, choices)).Succeeded);
        Assert.Equal(8000, (await f.Store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        Assert.Equal(10000, (await f.Store.GetEntryAsync(other.Id, Ct))!.Amount);
    }

    [Fact]
    public async Task Earlier_editor_link_also_refuses_a_second_import_link()
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f);
        var (_, preview, save) = await ReviewAsync(f, [detail]);
        await save.SaveAsync([detail], [], detail.Id, null, () => true, preview, Link(preview));
        var incoming = NewAggregate(aggregate);
        var overlaps = AggregatedEntries.FindForImport([incoming], await f.Store.GetEntriesAsync(cancellationToken: Ct));
        var result = await f.Store.ImportAsync([incoming], [.. overlaps.Select(o => new ImportAggregateChoice(o, true))], Ct);
        Assert.Equal(ImportOverlapConflict.SharedDetail, result.Conflict);
        Assert.Null(await f.Store.GetEntryAsync(incoming.Id, Ct));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Deadline_or_departed_financial_owner_cannot_write(bool expired)
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f);
        var (_, preview, save) = await ReviewAsync(f, [detail]);
        if (!expired)
        {
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            await Assert.ThrowsAsync<InvalidOperationException>(() => save.SaveAsync([detail], [], detail.Id, null, () => false, preview, Link(preview)));
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); return;
        }
        await save.SaveAsync([detail], [], detail.Id, null, () => true, preview, Link(preview));
        f.Time.Now += TimeSpan.FromSeconds(8);
        var undo = Assert.IsType<EntryAggregateUndo>(save.AggregateUndo); var service = new UndoService(f.Store, f.Time);
        service.Offer(() => f.Store.UndoEditorAggregateAsync(undo, Ct), undo.CreatedAt); Assert.False(service.CanUndo);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.UndoEditorAggregateAsync(undo, Ct));
        Assert.Equal(8000, (await f.Store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
    }

    [Fact]
    public async Task Notification_failure_consumes_finance_and_undo_before_publication()
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f);
        var (_, preview, save) = await ReviewAsync(f, [detail]);
        f.Store.Changed += (_, _) => throw new InvalidOperationException("Deliberate postcommit observer failure.");
        Assert.True((await save.SaveAsync([detail], [], detail.Id, null, () => true, preview, Link(preview))).Succeeded);
        Assert.True(save.Accepted); Assert.True(save.NotificationFailed);
        var undo = Assert.IsType<EntryAggregateUndo>(save.AggregateUndo); await f.Store.UndoEditorAggregateAsync(undo, Ct);
        Assert.True(undo.Accepted); Assert.Equal(10000, (await f.Store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.UndoEditorAggregateAsync(undo, Ct));
    }

    [Fact]
    public async Task New_fully_covered_aggregate_has_explicit_undo_without_deleting_its_details()
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f);
        await f.Store.SaveEntryAsync(detail, Ct);
        var created = NewAggregate(aggregate); created.Amount = detail.Amount;
        var (_, preview, save) = await ReviewAsync(f, [created]);
        Assert.True((await save.SaveAsync([created], [], created.Id, null, () => true, preview, Link(preview))).Succeeded);
        Assert.Null(await f.Store.GetEntryAsync(created.Id, Ct));
        await f.Store.UndoEditorAggregateAsync(save.AggregateUndo!, Ct);
        Assert.Equal(2000, (await f.Store.GetEntryAsync(created.Id, Ct))!.Amount);
        Assert.Equal(2000, (await f.Store.GetEntryAsync(detail.Id, Ct))!.Amount);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Consumed_aggregate_document_or_refund_keeps_link_unavailable(bool refund)
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f); detail.Amount = aggregate.Amount;
        if (refund) await f.Store.SaveEntryAsync(new() { AccountId = aggregate.AccountId, Kind = EntryKind.Refund, Date = Today, RefundOfId = aggregate.Id, Amount = 100 }, Ct);
        else await f.Store.AddAttachmentAsync(new() { EntryId = aggregate.Id, FileName = "Original.pdf", ContentType = "application/pdf", Data = [1, 2] }, Ct);
        var (_, preview, save) = await ReviewAsync(f, [detail]); Assert.False(preview.CanLink(aggregate.Id));
        await Assert.ThrowsAsync<EntryEditorSourceChangedException>(() => save.SaveAsync([detail], [], detail.Id, null, () => true, preview, Link(preview)));
        Assert.False(save.Accepted);
        Assert.True((await save.SaveAsync([detail], [], detail.Id, null, () => true, preview,
            [.. preview.Overlaps.Select(o => new ImportAggregateChoice(o, false))])).Succeeded);
        Assert.Null(save.AggregateUndo);
        Assert.Equal(aggregate.Amount, (await f.Store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        Assert.Equal(detail.Amount, (await f.Store.GetEntryAsync(detail.Id, Ct))!.Amount);
        Assert.Equal(refund ? 0 : 1, (await f.Store.GetAttachmentsAsync(aggregate.Id, Ct)).Count);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Nested_account_returns_only_its_known_atomic_reference_commit(bool makeDefault)
    {
        using var f = new FlowFixture(); var load = new EntryEditorLoadSession(f.Store, new(Today));
        var original = await load.ReadAsync(); load.Publish(original); load.Retire();
        var handoff = new EntryAccountHandoff(original, () => true);
        var account = new Account { Name = "Explicit child account", CurrencyCode = "EUR", OpeningDate = Today };
        f.Store.Changed += (_, _) => throw new InvalidOperationException("Deliberate metadata publication failure.");
        await handoff.SaveAsync(f.Store, account, makeDefault); Assert.NotNull(handoff.Accepted);
        load.AcceptAccountReturn(handoff); var ready = await load.ReadAsync(); load.Publish(ready);
        Assert.Equal(account.Id, Assert.Single(ready.Inputs.Resources.Accounts).Id);
        Assert.Equal(makeDefault ? account.Id : (Guid?)null, ready.Inputs.Resources.Settings?.DefaultAccountId);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => handoff.SaveAsync(f.Store, account, makeDefault));
        Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Retired_or_changed_parent_refuses_nested_account_without_mutation(bool changed)
    {
        using var f = new FlowFixture(); var source = await f.Store.ReadEntryEditorSnapshotAsync(new(Today), ct: Ct);
        var current = true; var handoff = new EntryAccountHandoff(source, () => current);
        if (changed) await f.Store.SaveAccountAsync(new() { Name = "Other explicit account", CurrencyCode = "EUR", OpeningDate = Today }, Ct);
        else current = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => handoff.SaveAsync(f.Store,
            new() { Name = "Retired child", CurrencyCode = "EUR", OpeningDate = Today }, true));
        Assert.Null(handoff.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Trigger_changed_aggregate_rolls_back_before_journal_or_acceptance()
    {
        using var f = new FlowFixture(); var (aggregate, detail) = await SeedAsync(f);
        var (_, preview, save) = await ReviewAsync(f, [detail]);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER AlterAggregate AFTER UPDATE ON Entries WHEN NEW.IsAggregated = 1 BEGIN UPDATE Entries SET Amount = Amount + 1 WHERE Id = NEW.Id; END", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<EntryEditorSourceChangedException>(() => save.SaveAsync([detail], [], detail.Id, null, () => true, preview, Link(preview)));
        Assert.False(save.Accepted); Assert.Null(save.AggregateUndo); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Actual_account_child_keeps_known_commit_locked_until_explicit_close_after_observer_failure()
    {
        using var f = new FlowFixture(); var source = await f.Store.ReadEntryEditorSnapshotAsync(new(Today), ct: Ct);
        var handoff = new EntryAccountHandoff(source, () => true);
        var child = new Features.Accounts.AccountEditorViewModel(f.Store, f.Services.GetRequiredService<PlanStore>(),
            f.Translator, f.Localization, f.Time, f.Platform);
        await child.ApplyQueryAsync(new Dictionary<string, object> { ["entryAccountHandoff"] = handoff });
        child.Form.Name = "Explicit real application child"; child.IsDefault = true;
        f.Store.Changed += (_, _) => throw new InvalidOperationException("Deliberate metadata observer failure.");
        await child.SaveCommand.ExecuteAsync(null);
        Assert.True(handoff.NotificationFailed); Assert.NotNull(handoff.Accepted); Assert.False(child.CanSave);
        Assert.NotNull(child.SaveError); Assert.Empty(f.Platform.Routes);
        await child.SaveCommand.ExecuteAsync(null); Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        await child.CancelCommand.ExecuteAsync(null); Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
    }

    [Fact]
    public async Task Old_nested_discard_answer_cannot_close_a_replaced_parent()
    {
        using var f = new FlowFixture(); var source = await f.Store.ReadEntryEditorSnapshotAsync(new(Today), ct: Ct);
        var current = true; var handoff = new EntryAccountHandoff(source, () => current);
        var child = new Features.Accounts.AccountEditorViewModel(f.Store, f.Services.GetRequiredService<PlanStore>(),
            f.Translator, f.Localization, f.Time, f.Platform);
        await child.ApplyQueryAsync(new Dictionary<string, object> { ["entryAccountHandoff"] = handoff }); child.Form.Name = "Uncommitted child";
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => answer.Task;
        var pending = child.CancelCommand.ExecuteAsync(null); Assert.False(pending.IsCompleted);
        current = false; answer.SetResult(true); await pending;
        Assert.Empty(f.Platform.Routes); Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Null(handoff.Accepted);
    }

    [Fact]
    public void Delayed_original_offer_keeps_its_meaning_after_publication_failure_and_never_schedules_a_negative_expiry()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var original = f.Time.Now; var invoked = 0;
        f.Time.Now += TimeSpan.FromSeconds(7);
        void Failed(object? sender, EventArgs args) => throw new InvalidOperationException("Deliberate offer observer failure.");
        undo.Changed += Failed;
        Assert.Throws<InvalidOperationException>(() => undo.Offer(() => { invoked++; return Task.CompletedTask; }, original, UndoNoticeKind.AggregateAdjusted));
        undo.Changed -= Failed; Assert.Equal(UndoNoticeKind.AggregateAdjusted, undo.NoticeKind);
        var reads = 0;
        f.Time.BeforeRead = () => { if (++reads > 1) f.Time.Now = original + TimeSpan.FromSeconds(9); };
        Assert.InRange(undo.Remaining, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        Assert.False(undo.CanUndo); Assert.Equal(TimeSpan.Zero, undo.Remaining); Assert.Equal(0, invoked);
        f.Time.BeforeRead = null; undo.Offer([new LedgerEntry()]); Assert.Equal(UndoNoticeKind.Deleted, undo.NoticeKind);
    }

    [Fact]
    public async Task Departed_actual_child_rolls_back_its_pending_metadata_and_only_explicit_new_presentation_can_retry()
    {
        using var f = new FlowFixture(); var source = await f.Store.ReadEntryEditorSnapshotAsync(new(Today), ct: Ct);
        var handoff = new EntryAccountHandoff(source, () => true);
        var child = new Features.Accounts.AccountEditorViewModel(f.Store, f.Services.GetRequiredService<PlanStore>(),
            f.Translator, f.Localization, f.Time, f.Platform);
        await child.ApplyQueryAsync(new Dictionary<string, object> { ["entryAccountHandoff"] = handoff }); child.Form.Name = "Retained child raw name";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Time.BeforeRead = () => child.SetPresentationActive(false);
        await child.SaveCommand.ExecuteAsync(null); f.Time.BeforeRead = null;
        Assert.Null(handoff.Accepted); Assert.False(child.CanSave); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal("Retained child raw name", child.Form.Name);
        child.SetPresentationActive(true); Assert.True(child.CanSave); await child.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(handoff.Accepted); Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
    }
}
