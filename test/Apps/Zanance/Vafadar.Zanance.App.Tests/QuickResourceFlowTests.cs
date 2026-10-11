using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-156: actual quick-resource commands and durable choices against complete isolated SQLite rows.</summary>
[Trait("AT", "AT-156")]
public sealed class QuickResourceFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid Scope = Guid.Parse("56565656-3434-4545-6767-898989898989");
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed record Setup(FlowFixture F, AccessSource Source, ZananceStore Store, ResourceChoiceStore Choices,
        QuickResourceActions Actions, IDbContextFactory<ZananceDbContext> Factory, LedgerEntry Entry);
    private static async Task<Setup> CreateAsync(bool bounded = true)
    {
        var f = new FlowFixture(); var a = new Account { Name = "Fictitious original", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(a, Ct);
        var e = new LedgerEntry { AccountId = a.Id, Date = new(2026, 10, 9), Kind = EntryKind.Expense, Amount = 12345,
            Title = "Complete source", Payee = "Original payee", Note = "Never copied", Tags = ["original"] };
        Assert.True((await f.Store.SaveEntryAsync(e, cancellationToken: Ct)).Succeeded);
        for (var i = 0; i < 5; i++) await f.Store.SaveTemplateAsync(EntryTemplate.From(e, "Original template " + i, true), Ct);
        for (var i = 0; i < 3; i++) await f.Store.SaveSavedFilterAsync(new SavedFilter { Name = "Original filter " + i, Period = 3,
            From = new(2026, 7, 1), To = new(2026, 7, 20), Kind = 1, AccountId = a.Id, CategoryIds = [Guid.NewGuid()],
            CategoryName = "Complete original category query", Search = "#original", InTotalsOnly = true, UnreviewedOnly = true }, Ct);
        var source = new AccessSource();
        if (bounded) source.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Scope)));
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var store = new ZananceStore(factory, f.Time, source); var choices = new ResourceChoiceStore(factory, source);
        var actions = new QuickResourceActions(store, f.Translator, f.Platform); await actions.LoadAsync(e.Id);
        return new(f, source, store, choices, actions, factory, e);
    }
    private static QuotaKind Kind(bool filter) => filter ? QuotaKind.SavedFilters : QuotaKind.QuickTemplates;
    private static Task SaveAsync(Setup s, bool filter, QuickResourceReview? review = null) => filter
        ? s.Store.SaveSavedFilterAsync(new SavedFilter { Name = "New query", Search = "complete draft" }, Ct, review)
        : s.Store.SaveTemplateAsync(EntryTemplate.From(s.Entry, "New template", true), Ct, review);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Opening_preserves_every_original_and_raw_preferences_without_guessing_a_subset(bool bounded)
    {
        var s = await CreateAsync(bounded); using var f = s.F; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await s.Actions.LoadAsync(s.Entry.Id); var r = s.Actions.Review!;
        Assert.Equal(5, r.Templates.Count); Assert.Equal(3, r.Filters.Count); Assert.Null(r.Settings);
        Assert.Equal(!bounded, r.Work.CanCreateTemplate); Assert.Equal(!bounded, r.Work.CanCreateFilter);
        Assert.All(r.Templates, t => Assert.Equal(!bounded, r.Work.CanApplyTemplate(t.Id)));
        Assert.All(r.Filters, t => Assert.True(r.Work.CanViewFilter(t.Id)));
        Assert.True(r.Work.CanCorrect); Assert.True(r.Work.CanDelete);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Explicit_choice_persists_across_new_store_without_changing_complete_financial_or_resource_rows(bool filter)
    {
        var s = await CreateAsync(); using var f = s.F; var before = await GoalChooserFlowTests.SnapshotAsync(f, false);
        var first = await s.Choices.ReadAsync(Kind(filter), Ct); var ids = first.Items.Take(filter ? 1 : 3).Select(i => i.Id).ToArray();
        Assert.True(first.RequiresSelection); Assert.Empty(first.SelectedIds);
        await s.Choices.SaveAsync(first, ids, Ct);
        var restarted = new ResourceChoiceStore(s.Factory, s.Source); var read = await restarted.ReadAsync(Kind(filter), Ct);
        Assert.Equal(ids.Order(), read.SelectedIds.Order()); Assert.False(read.RequiresSelection);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f, false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Empty_explicit_choice_allows_a_new_definition_and_atomically_selects_only_that_identity(bool filter)
    {
        var s = await CreateAsync(); using var f = s.F;
        await s.Choices.SaveAsync(await s.Choices.ReadAsync(Kind(filter), Ct), [], Ct);
        var review = await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        await SaveAsync(s, filter, review);
        var choice = await s.Choices.ReadAsync(Kind(filter), Ct); Assert.Single(choice.SelectedIds);
        Assert.Equal(filter ? 4 : 6, choice.Items.Count); Assert.False(choice.RequiresSelection);
        var next = await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        Assert.Equal(!filter, filter ? next.Work.CanCreateFilter : next.Work.CanCreateTemplate);
        Assert.Single(await s.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Null(next.Settings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Full_chosen_capacity_rejects_new_work_but_preserves_unselected_correction_and_delete(bool filter)
    {
        var s = await CreateAsync(); using var f = s.F;
        var snapshot = await s.Choices.ReadAsync(Kind(filter), Ct); var selected = snapshot.Items.Take(filter ? 1 : 3).Select(i => i.Id).ToArray();
        await s.Choices.SaveAsync(snapshot, selected, Ct);
        var review = await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => SaveAsync(s, filter, review));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        if (filter)
        {
            var original = review.Filters.First(x => !selected.Contains(x.Id)); var created = original.CreatedAt;
            original.Search = "Explicit retained correction"; await s.Store.SaveSavedFilterAsync(original, Ct);
            Assert.Equal(created, (await s.Store.GetSavedFiltersAsync(Ct)).Single(x => x.Id == original.Id).CreatedAt);
            await s.Store.DeleteSavedFilterAsync(original.Id, Ct);
        }
        else
        {
            var original = review.Templates.First(x => !selected.Contains(x.Id)); var created = original.CreatedAt;
            original.Amount = 67890; await s.Store.SaveTemplateAsync(original, Ct);
            Assert.Equal(created, (await s.Store.GetTemplatesAsync(Ct)).Single(x => x.Id == original.Id).CreatedAt);
            await s.Store.DeleteTemplateAsync(original.Id, Ct);
        }
        Assert.Equal(selected.Order(), (await s.Choices.ReadAsync(Kind(filter), Ct)).SelectedIds.Order());
        Assert.Single(await s.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Same_timestamp_original_packet_change_rejects_chooser_save_and_reviewed_writer(bool filter)
    {
        var s = await CreateAsync(); using var f = s.F; var first = await s.Choices.ReadAsync(Kind(filter), Ct); var review = s.Actions.Review!;
        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
        {
            if (filter) await db.SavedFilters.Where(x => x.Id == first.Items[0].Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Search, "Changed same timestamp"), Ct);
            else await db.Templates.Where(x => x.Id == first.Items[0].Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Amount, 99999), Ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Choices.SaveAsync(first, [], Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => SaveAsync(s, filter, review));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Choice_value_change_at_unchanged_revision_retires_reviewed_writers(bool filter)
    {
        var s = await CreateAsync(); using var f = s.F; await s.Choices.SaveAsync(await s.Choices.ReadAsync(Kind(filter), Ct), [], Ct);
        var review = await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct); var snap = filter ? review.FilterChoice : review.TemplateChoice;
        await using (var db = await s.Factory.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ResourceSelections SET IdentitySet = {snap.Items[0].Id.ToString("N")} WHERE Kind = {(int)Kind(filter)}", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => SaveAsync(s, filter, review));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_chooser_publishes_new_kind_captions_and_saves_only_an_explicit_original_subset(bool filter)
    {
        var s = await CreateAsync(); using var f = s.F; var before = await GoalChooserFlowTests.SnapshotAsync(f, false);
        var vm = new ActiveItemsViewModel(s.Choices, f.Translator, f.Platform); await vm.LoadAsync();
        vm.KindIndex = filter ? 5 : 4; await vm.PendingKindChange;
        Assert.Equal(f.Translator[filter ? "Filter_Title" : "Templates_Title"], vm.KindNames[vm.KindIndex]);
        Assert.Equal(filter ? 3 : 5, vm.Items.Count); Assert.True(vm.CanChoose); Assert.DoesNotContain(vm.Items, i => i.IsSelected);
        vm.ToggleCommand.Execute(vm.Items[0]); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Single((await s.Choices.ReadAsync(Kind(filter), Ct)).SelectedIds); Assert.Single(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f, false));
    }

    [Fact]
    public async Task Confirmed_same_name_filter_replacement_moves_only_the_original_selected_slot_and_keeps_original_queries()
    {
        var s = await CreateAsync(); using var f = s.F; var snap = await s.Choices.ReadAsync(QuotaKind.SavedFilters, Ct);
        await s.Choices.SaveAsync(snap, [snap.Items[0].Id], Ct); await s.Actions.LoadAsync();
        var original = s.Actions.Review!.Filters.Single(x => x.Id == snap.Items[0].Id);
        f.Platform.Choices.Enqueue(f.Translator["Filter_SaveCurrent"]); f.Platform.Inputs.Enqueue(original.Name); f.Platform.Confirmations.Enqueue(true);
        var query = new SavedFilter { Name = "Unused mutable name", From = new(2026, 1, 2), To = new(2026, 2, 3), Kind = 2,
            CategoryIds = [Guid.NewGuid()], Search = "#retained complete draft", InTotalsOnly = true, UnreviewedOnly = true };
        await s.Actions.FilterMenuAsync(query, () => s.Actions.LoadAsync());
        Assert.Empty(f.Platform.Failures); var saved = (await s.Store.GetSavedFiltersAsync(Ct)).Single(x => x.Name == original.Name);
        Assert.NotEqual(original.Id, saved.Id); Assert.Equal(query.CategoryIds, saved.CategoryIds); Assert.Equal(query.From, saved.From);
        Assert.Equal(query.Search, saved.Search); Assert.Equal(query.To, saved.To);
        Assert.Equal(saved.Id, Assert.Single((await s.Choices.ReadAsync(QuotaKind.SavedFilters, Ct)).SelectedIds));
        Assert.Equal(3, (await s.Store.GetSavedFiltersAsync(Ct)).Count); Assert.Single(await s.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Pending_name_dialog_keeps_original_query_and_busy_guard_without_automatic_resubmission()
    {
        var s = await CreateAsync(false); using var f = s.F; var pending = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.Choices.Enqueue(f.Translator["Filter_SaveCurrent"]); f.Platform.PendingInput = () => pending.Task;
        var query = new SavedFilter { Name = "Unused", Search = "Original query", CategoryIds = [Guid.NewGuid()], Period = 3 };
        var originalCategory = query.CategoryIds[0]; var task = s.Actions.FilterMenuAsync(query, () => s.Actions.LoadAsync());
        Assert.True(s.Actions.IsBusy); query.Search = "Later typing"; query.CategoryIds.Clear(); query.Period = 0;
        await s.Actions.FilterMenuAsync(query, () => s.Actions.LoadAsync()); Assert.Single(f.Platform.OfferedActions);
        pending.SetResult("Captured query"); await task;
        var saved = (await s.Store.GetSavedFiltersAsync(Ct)).Single(x => x.Name == "Captured query");
        Assert.Equal("Original query", saved.Search); Assert.Equal(originalCategory, Assert.Single(saved.CategoryIds)); Assert.Equal(3, saved.Period);
    }

    [Fact]
    public async Task Changed_source_during_amount_confirmation_keeps_the_unsaved_name_and_never_creates_a_template()
    {
        var s = await CreateAsync(false); using var f = s.F; var count = (await s.Store.GetTemplatesAsync(Ct)).Count;
        f.Platform.Inputs.Enqueue("Explicit unsaved name"); f.Platform.PendingConfirmation = async () =>
        {
            await using var db = await s.Factory.CreateDbContextAsync(Ct);
            await db.Entries.Where(x => x.Id == s.Entry.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Amount, 22222), Ct); return true;
        };
        await s.Actions.CreateTemplateAsync(s.Entry, "Heading", "EUR 123.45");
        Assert.Single(f.Platform.Failures); Assert.Equal("Explicit unsaved name", s.Actions.TemplateNameDraft);
        Assert.Null(s.Actions.Review); Assert.Equal(count, (await s.Store.GetTemplatesAsync(Ct)).Count);
    }

    [Fact]
    public async Task Committed_delete_with_failed_refresh_retires_the_operation_and_never_repeats_the_writer()
    {
        var s = await CreateAsync(false); using var f = s.F; var id = s.Actions.Review!.Templates[0].Id; var writes = 0;
        s.Store.Changed += (_, _) => writes++; f.Platform.Confirmations.Enqueue(true);
        await s.Actions.DeleteTemplateAsync(id, () => throw new IOException("Fictitious refresh failure"));
        Assert.Equal(1, writes); Assert.Null(s.Actions.Review); Assert.Single(f.Platform.Alerts);
        await s.Actions.DeleteTemplateAsync(id, () => s.Actions.LoadAsync()); Assert.Equal(1, writes);
        Assert.Equal(4, (await s.Store.GetTemplatesAsync(Ct)).Count);
    }

    [Fact]
    public async Task Applying_selected_template_only_fills_a_detached_draft_and_leaves_complete_stored_data_unchanged()
    {
        var s = await CreateAsync(); using var f = s.F; var snap = await s.Choices.ReadAsync(QuotaKind.QuickTemplates, Ct);
        await s.Choices.SaveAsync(snap, [snap.Items[0].Id], Ct); await s.Actions.LoadAsync();
        var before = await GoalChooserFlowTests.SnapshotAsync(f); LedgerEntry? draft = null;
        await s.Actions.ApplyTemplateAsync(snap.Items[0].Id, t => draft = t.CreateEntry(new(2026, 10, 15)));
        Assert.NotNull(draft); Assert.NotEqual(s.Entry.Id, draft.Id); Assert.Equal(s.Entry.Amount, draft.Amount);
        Assert.Null(draft.Note); Assert.Null(draft.ScheduleId); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await s.Actions.ApplyTemplateAsync(snap.Items[1].Id, _ => throw new InvalidOperationException("Unavailable template ran"));
        Assert.Single(f.Platform.Failures); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancelled_delete_preserves_complete_rows_and_original_choice(bool filter)
    {
        var s = await CreateAsync(false); using var f = s.F; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        if (filter)
        {
            f.Platform.Choices.Enqueue(f.Translator["Filter_Remove"]); var original = s.Actions.Review!.Filters.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).First();
            f.Platform.Choices.Enqueue("1. " + original.Name);
            await s.Actions.FilterMenuAsync(new SavedFilter { Name = "Unused" }, () => s.Actions.LoadAsync());
        }
        else await s.Actions.DeleteTemplateAsync(s.Actions.Review!.Templates[0].Id, () => s.Actions.LoadAsync());
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.NotNull(s.Actions.Review);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Malformed_selection_keeps_original_history_and_corrections_but_requires_explicit_repair_for_new_work(bool filter)
    {
        var s = await CreateAsync(); using var f = s.F; await s.Choices.SaveAsync(await s.Choices.ReadAsync(Kind(filter), Ct), [], Ct);
        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ResourceSelections SET IdentitySet = {"malformed original"} WHERE Kind = {(int)Kind(filter)}", Ct);
        var review = await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        Assert.True(filter ? review.FilterChoice.RequiresSelection : review.TemplateChoice.RequiresSelection);
        Assert.All(review.Filters, x => Assert.True(review.Work.CanViewFilter(x.Id)));
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => SaveAsync(s, filter, review));
        if (filter) await s.Store.DeleteSavedFilterAsync(review.Filters[0].Id, Ct, review);
        else await s.Store.DeleteTemplateAsync(review.Templates[0].Id, Ct, review);
        var current = await s.Choices.ReadAsync(Kind(filter), Ct); Assert.True(current.RequiresSelection);
        await s.Choices.SaveAsync(current, [], Ct); Assert.False((await s.Choices.ReadAsync(Kind(filter), Ct)).RequiresSelection);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sql_failure_rolls_back_new_resource_and_its_selected_slot_together(bool filter)
    {
        var s = await CreateAsync(); using var f = s.F; await s.Choices.SaveAsync(await s.Choices.ReadAsync(Kind(filter), Ct), [], Ct);
        var review = await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        await using (var db = await s.Factory.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync(filter
            ? "CREATE TRIGGER fail_quick BEFORE INSERT ON SavedFilters BEGIN SELECT RAISE(ABORT, 'owned failure'); END;"
            : "CREATE TRIGGER fail_quick BEFORE INSERT ON Templates BEGIN SELECT RAISE(ABORT, 'owned failure'); END;", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(s, filter, review));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty((await s.Choices.ReadAsync(Kind(filter), Ct)).SelectedIds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retired_file_or_rights_rejects_the_original_review_before_a_write(bool foreignFile)
    {
        var s = await CreateAsync(); using var f = s.F; var review = s.Actions.Review!; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Source.Current = foreignFile ? new(f.Directory.Combine("foreign.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Scope)))
            : new(f.Directory.Combine("zanance.db"), new(ProductPlan.Plus, new(EntitlementScopeKind.PersonalProfile, Scope)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Store.DeleteTemplateAsync(review.Templates[0].Id, Ct, review));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Actual_delete_rechecks_complete_same_timestamp_template_after_confirmation_and_retires_failed_packet()
    {
        var s = await CreateAsync(false); using var f = s.F; var id = s.Actions.Review!.Templates[0].Id;
        f.Platform.PendingConfirmation = async () =>
        {
            await using var db = await s.Factory.CreateDbContextAsync(Ct);
            await db.Templates.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Payee, "Changed original"), Ct); return true;
        };
        await s.Actions.DeleteTemplateAsync(id, () => s.Actions.LoadAsync());
        Assert.Single(f.Platform.Failures); Assert.Null(s.Actions.Review); Assert.Equal(5, (await s.Store.GetTemplatesAsync(Ct)).Count);
        Assert.Equal("Changed original", (await s.Store.GetTemplatesAsync(Ct)).Single(x => x.Id == id).Payee);
    }
    [Fact]
    public async Task Duplicate_restored_filter_names_delete_the_explicit_second_identity_and_preserve_the_first_complete_query()
    {
        var s = await CreateAsync(false); using var f = s.F;
        var originals = (await s.Store.GetSavedFiltersAsync(Ct)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToArray();
        await using (var db = await s.Factory.CreateDbContextAsync(Ct)) await db.SavedFilters.Where(x => x.Id == originals[0].Id || x.Id == originals[1].Id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.Name, "Same retained name"), Ct);
        await s.Actions.LoadAsync(); var first = JsonSerializer.Serialize(s.Actions.Review!.Filters.Single(x => x.Id == originals[0].Id));
        var ledger = JsonSerializer.Serialize(await s.Store.GetEntriesAsync(cancellationToken: Ct));
        f.Platform.Choices.Enqueue(f.Translator["Filter_Remove"]); f.Platform.Choices.Enqueue("2. Same retained name");
        f.Platform.Confirmations.Enqueue(true);
        await s.Actions.FilterMenuAsync(new SavedFilter { Name = "Unused" }, () => s.Actions.LoadAsync());
        Assert.Empty(f.Platform.Failures); Assert.Equal(2, (await s.Store.GetSavedFiltersAsync(Ct)).Count);
        Assert.DoesNotContain(await s.Store.GetSavedFiltersAsync(Ct), x => x.Id == originals[1].Id);
        Assert.Equal(first, JsonSerializer.Serialize((await s.Store.GetSavedFiltersAsync(Ct)).Single(x => x.Id == originals[0].Id)));
        Assert.Equal(ledger, JsonSerializer.Serialize(await s.Store.GetEntriesAsync(cancellationToken: Ct)));
        Assert.Equal(3, f.Platform.OfferedActions[1].Distinct().Count());
    }
}
