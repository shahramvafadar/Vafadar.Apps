using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual bulk view-model commands over a real SQLite ledger, without list-control doubles.</summary>
public sealed class BulkTransactionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-80")]
    public async Task Select_all_only_selects_visible_known_rows_and_cancel_changes_no_money()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        vm.Start(); vm.SelectAll([setup.Rows[0].Id, Guid.NewGuid()]);
        Assert.True(vm.IsSelected(setup.Rows[0].Id)); Assert.False(vm.IsSelected(setup.Rows[1].Id));
        vm.Toggle(setup.Rows[0].Id); Assert.False(vm.HasSelection); vm.SelectAll(setup.Rows.Select(e => e.Id));
        vm.Stop(); Assert.False(vm.IsSelecting); Assert.False(vm.HasSelection);
        Assert.All(await f.Store.GetEntriesAsync(cancellationToken: Ct), e => Assert.Equal(ReviewState.Unreviewed, e.Review));
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Review_changes_only_selected_rows_and_does_not_mutate_the_loaded_snapshots()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        vm.Start(); vm.Toggle(setup.Rows[0].Id); await vm.ReviewedCommand.ExecuteAsync(null);
        var current = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(ReviewState.Confirmed, current.Single(e => e.Id == setup.Rows[0].Id).Review);
        Assert.Equal(ReviewState.Unreviewed, current.Single(e => e.Id == setup.Rows[1].Id).Review);
        Assert.All(setup.Rows, e => Assert.Equal(ReviewState.Unreviewed, e.Review));
        Assert.Equal(3000, current.Sum(e => e.Amount)); Assert.False(vm.HasSelection); Assert.False(vm.IsBusy);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Rejected_review_keeps_the_snapshot_selection_and_database_unchanged()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        // A stale/malformed loaded snapshot must not be changed before the store rejects its write.
        setup.Rows[0].Amount = -1; vm.Start(); vm.Toggle(setup.Rows[0].Id);
        await vm.ReviewedCommand.ExecuteAsync(null);
        Assert.Equal(ReviewState.Unreviewed, setup.Rows[0].Review); Assert.True(vm.HasSelection);
        var current = await f.Store.GetEntryAsync(setup.Rows[0].Id, Ct);
        Assert.Equal(1000, current!.Amount); Assert.Equal(ReviewState.Unreviewed, current.Review);
        Assert.Single(f.Platform.Alerts); Assert.False(vm.IsBusy);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Tag_input_is_cancelable_normalized_and_applied_only_to_selected_rows()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        vm.Start(); vm.Toggle(setup.Rows[0].Id); f.Platform.Inputs.Enqueue(null);
        await vm.TagCommand.ExecuteAsync(null); Assert.True(vm.HasSelection);
        f.Platform.Inputs.Enqueue("  Fictitious  "); await vm.TagCommand.ExecuteAsync(null);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal("Fictitious", Assert.Single(rows.Single(e => e.Id == setup.Rows[0].Id).Tags));
        Assert.Empty(rows.Single(e => e.Id == setup.Rows[1].Id).Tags); Assert.Empty(setup.Rows[0].Tags);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Mixed_income_and_expense_category_change_is_rejected_without_a_picker_or_write()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f);
        var incomeCategory = new Category { Name = "Fictitious income", Kind = CategoryKind.Income };
        await f.Store.SaveCategoryAsync(incomeCategory, Ct);
        var income = new LedgerEntry { AccountId = setup.Account.Id, CategoryId = incomeCategory.Id, Amount = 500, Date = new(2026, 10, 9), Kind = EntryKind.Income };
        await f.Store.SaveEntryAsync(income, Ct);
        var vm = setup.Vm; vm.Load([.. setup.Rows, income], [.. setup.Categories, incomeCategory], id => id.ToString() ?? "");
        vm.Start(); vm.SelectAll([setup.Rows[0].Id, income.Id]); await vm.CategoryCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.Alerts); Assert.Empty(f.Platform.OfferedActions); Assert.True(vm.HasSelection);
        Assert.Equal(setup.Categories[0].Id, (await f.Store.GetEntryAsync(setup.Rows[0].Id, Ct))!.CategoryId);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Category_picker_excludes_archived_options_and_cancel_preserves_all_rows()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        setup.Categories[1].IsArchived = true; vm.Start(); vm.SelectAll(setup.Rows.Select(e => e.Id));
        f.Platform.Choices.Enqueue(null); await vm.CategoryCommand.ExecuteAsync(null);
        var actions = Assert.Single(f.Platform.OfferedActions); Assert.DoesNotContain("Fictitious alternate", actions);
        Assert.True(vm.HasSelection); Assert.All(await f.Store.GetEntriesAsync(cancellationToken: Ct), e => Assert.Equal(setup.Categories[0].Id, e.CategoryId));
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Cancelled_delete_keeps_selection_and_confirmed_delete_is_restored_once_by_undo()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        vm.Start(); vm.SelectAll(setup.Rows.Select(e => e.Id)); f.Platform.Confirmations.Enqueue(false);
        await vm.DeleteCommand.ExecuteAsync(null); Assert.True(vm.HasSelection); Assert.False(setup.Undo.CanUndo);
        f.Platform.Confirmations.Enqueue(true); await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.True(setup.Undo.CanUndo);
        await setup.Undo.UndoAsync(); await setup.Undo.UndoAsync();
        var restored = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(2, restored.Count);
        Assert.Equal(3000, restored.Sum(e => e.Amount)); Assert.Equal(setup.Rows.Select(e => e.Id).Order(), restored.Select(e => e.Id).Order());
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Reload_prunes_deleted_selected_ids_and_unknown_ids_never_create_an_undo_offer()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        vm.Start(); vm.SelectAll(setup.Rows.Select(e => e.Id)); vm.Load([setup.Rows[0]], setup.Categories, id => id.ToString() ?? "");
        Assert.True(vm.IsSelected(setup.Rows[0].Id)); Assert.False(vm.IsSelected(setup.Rows[1].Id));
        vm.Toggle(Guid.NewGuid()); vm.Stop(); f.Platform.Confirmations.Enqueue(true); await vm.DeleteCommand.ExecuteAsync(null);
        Assert.False(setup.Undo.CanUndo); Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Category_change_updates_only_selected_expenses_and_never_changes_a_transfer()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f);
        var destination = new Account { Name = "Fictitious destination", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(destination, Ct);
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = setup.Account.Id, ToAccountId = destination.Id,
            Amount = 900, ToAmount = 900, Date = new(2026, 10, 9) };
        Assert.True((await f.Store.SaveEntryAsync(transfer, Ct)).Succeeded);
        var vm = setup.Vm; vm.Load([.. setup.Rows, transfer], setup.Categories, id => setup.Categories.FirstOrDefault(c => c.Id == id)?.Name ?? "");
        vm.Start(); vm.SelectAll([setup.Rows[0].Id, transfer.Id]); f.Platform.Choices.Enqueue("Fictitious alternate");
        await vm.CategoryCommand.ExecuteAsync(null);
        Assert.Equal(setup.Categories[1].Id, (await f.Store.GetEntryAsync(setup.Rows[0].Id, Ct))!.CategoryId);
        Assert.Equal(setup.Categories[0].Id, (await f.Store.GetEntryAsync(setup.Rows[1].Id, Ct))!.CategoryId);
        var actual = (await f.Store.GetEntryAsync(transfer.Id, Ct))!;
        Assert.Equal(EntryKind.Transfer, actual.Kind); Assert.Null(actual.CategoryId);
        Assert.Equal(900, actual.Amount); Assert.Equal(900, actual.ToAmount); Assert.Equal(destination.Id, actual.ToAccountId);
        Assert.Equal(3, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Pending_dialog_freezes_selection_and_refuses_other_bulk_commands_until_it_finishes()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        var answer = new TaskCompletionSource<string?>(); f.Platform.PendingInput = () => answer.Task;
        vm.Start(); vm.Toggle(setup.Rows[0].Id); var pending = vm.TagCommand.ExecuteAsync(null);
        Assert.True(vm.IsBusy); vm.Toggle(setup.Rows[1].Id); vm.SelectAll(setup.Rows.Select(row => row.Id)); vm.Stop(); await vm.ReviewedCommand.ExecuteAsync(null);
        Assert.True(vm.IsSelected(setup.Rows[0].Id)); Assert.False(vm.IsSelected(setup.Rows[1].Id)); Assert.True(vm.IsSelecting);
        Assert.All(await f.Store.GetEntriesAsync(cancellationToken: Ct), e => Assert.Equal(ReviewState.Unreviewed, e.Review));
        answer.SetResult("Fictitious pending"); await pending; Assert.False(vm.IsBusy);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Single(rows.Single(e => e.Id == setup.Rows[0].Id).Tags); Assert.Empty(rows.Single(e => e.Id == setup.Rows[1].Id).Tags);
        Assert.Equal(3000, rows.Sum(e => e.Amount));
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Expired_delete_undo_cannot_restore_money_even_if_a_delayed_command_is_invoked()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        vm.Start(); vm.Toggle(setup.Rows[0].Id); f.Platform.Confirmations.Enqueue(true); await vm.DeleteCommand.ExecuteAsync(null);
        f.Time.Now = f.Time.Now.AddSeconds(8); await setup.Undo.UndoAsync();
        Assert.Null(await f.Store.GetEntryAsync(setup.Rows[0].Id, Ct));
        Assert.Equal(2000, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Theory, InlineData(10000), InlineData(100000), Trait("AT", "AT-110")]
    public async Task Complete_selection_survives_fresh_reordered_snapshots_and_prunes_only_removed_ids(int count)
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        var unchanged = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        // Large source snapshots stay fictitious and in memory; no financial command writes these rows.
        var rows = Enumerable.Range(0, count).Select(_ => new LedgerEntry
            { AccountId = setup.Account.Id, Kind = EntryKind.Expense, Amount = 1000, Date = new(2026, 10, 9) }).ToArray();
        vm.Load(rows, setup.Categories, _ => string.Empty); vm.Start();
        vm.SelectAll(rows.Select(row => row.Id).Append(Guid.NewGuid()));
        Assert.All(rows, row => Assert.True(vm.IsSelected(row.Id)));
        Assert.Equal(f.Translator.Format("Bulk_Selected", count), vm.SelectionText);

        var added = new LedgerEntry { AccountId = setup.Account.Id, Kind = EntryKind.Expense, Amount = 1000, Date = new(2026, 10, 9) };
        var fresh = rows.Skip(1).Take(count - 2).Reverse().Select(row => row.Copy()).Append(added).ToArray();
        vm.Load(fresh, setup.Categories, _ => string.Empty);
        Assert.All(fresh.Where(row => row.Id != added.Id), row => Assert.True(vm.IsSelected(row.Id)));
        Assert.False(vm.IsSelected(rows[0].Id)); Assert.False(vm.IsSelected(rows[^1].Id)); Assert.False(vm.IsSelected(added.Id));
        vm.Toggle(rows[0].Id); Assert.False(vm.IsSelected(rows[0].Id));
        vm.Toggle(added.Id); Assert.True(vm.IsSelected(added.Id));
        vm.SelectAll([rows[0].Id, rows[^1].Id, added.Id]);
        Assert.Equal(f.Translator.Format("Bulk_Selected", count - 1), vm.SelectionText);
        vm.Stop(); Assert.False(vm.HasSelection); Assert.False(vm.IsSelecting);
        Assert.Equal(unchanged, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)));
        Assert.False(setup.Undo.CanUndo);
    }

    [Fact, Trait("AT", "AT-110")]
    public async Task Retained_selection_reviews_the_fresh_snapshot_and_preserves_its_current_money_and_tags()
    {
        using var f = new FlowFixture(); var setup = await SetupAsync(f); var vm = setup.Vm;
        vm.Start(); vm.Toggle(setup.Rows[0].Id);
        var changed = setup.Rows[0].Copy(); changed.Amount = 4321; changed.Tags = ["Fictitious fresh"];
        Assert.True((await f.Store.SaveEntryAsync(changed, Ct)).Succeeded);
        var fresh = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        vm.Load(fresh, setup.Categories, _ => string.Empty);
        await vm.ReviewedCommand.ExecuteAsync(null);
        var current = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        var reviewed = current.Single(row => row.Id == changed.Id);
        Assert.Equal(4321, reviewed.Amount); Assert.Equal(changed.Tags, reviewed.Tags); Assert.Equal(ReviewState.Confirmed, reviewed.Review);
        Assert.Equal(ReviewState.Unreviewed, fresh.Single(row => row.Id == changed.Id).Review);
        Assert.Equal(2000, current.Single(row => row.Id != changed.Id).Amount);
        Assert.Equal(ReviewState.Unreviewed, current.Single(row => row.Id != changed.Id).Review);
        Assert.False(vm.HasSelection); Assert.False(setup.Undo.CanUndo);
    }

    private sealed record Setup(BulkTransactionsViewModel Vm, UndoService Undo, Account Account, List<Category> Categories, List<LedgerEntry> Rows);
    private static async Task<Setup> SetupAsync(FlowFixture f)
    {
        var account = new Account { Name = "Fictitious bulk", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var categories = new List<Category> { new() { Name = "Fictitious expense", Kind = CategoryKind.Expense }, new() { Name = "Fictitious alternate", Kind = CategoryKind.Expense } };
        foreach (var category in categories) { await f.Store.SaveCategoryAsync(category, Ct); }
        var rows = new List<LedgerEntry>();
        foreach (var amount in new[] { 1000, 2000 })
        {
            var row = new LedgerEntry { AccountId = account.Id, CategoryId = categories[0].Id, Kind = EntryKind.Expense, Amount = amount, Date = new(2026, 10, 9), Review = ReviewState.Unreviewed };
            await f.Store.SaveEntryAsync(row, Ct); rows.Add(row);
        }
        var undo = new UndoService(f.Store, f.Time);
        var vm = new BulkTransactionsViewModel(f.Store, f.Translator, f.Platform, undo, () => Task.CompletedTask);
        vm.Load(rows, categories, id => categories.FirstOrDefault(c => c.Id == id)?.Name ?? "");
        return new(vm, undo, account, categories, rows);
    }
}
