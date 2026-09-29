using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

public sealed class ZananceStoreTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;

    public ZananceStoreTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Fact]
    [Trait("AT", "AT-03")]
    public async Task Saving_the_same_entry_twice_creates_one_entry()
    {
        var account = await NewAccountAsync();
        var entry = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 500, Date = new DateOnly(2026, 10, 1) };

        await Task.WhenAll(_store.SaveEntryAsync(entry, Ct), Task.Delay(1, Ct));
        await _store.SaveEntryAsync(entry, Ct);

        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Invalid_entries_are_not_saved_and_report_the_reason()
    {
        var account = await NewAccountAsync();
        var result = await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 0 }, Ct);

        Assert.Contains(LedgerError.AmountMustBePositive, result.Errors);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Delete_and_undo_restore_the_entry_unchanged()
    {
        var account = await NewAccountAsync();
        var entry = new LedgerEntry { Kind = EntryKind.Income, AccountId = account.Id, Amount = 700, Date = new DateOnly(2026, 10, 2), Note = "line 1\nline 2" };
        await _store.SaveEntryAsync(entry, Ct);
        var created = (await _store.GetEntryAsync(entry.Id, Ct))!.CreatedAt;

        var deleted = await _store.DeleteEntryAsync(entry.Id, Ct);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
        await _store.RestoreEntriesAsync(deleted, Ct);

        var restored = await _store.GetEntryAsync(entry.Id, Ct);
        Assert.NotNull(restored);
        Assert.Equal("line 1\nline 2", restored.Note);
        Assert.Equal(created, restored.CreatedAt);
    }

    [Fact]
    public async Task Currency_of_an_account_with_entries_cannot_change()
    {
        var account = await NewAccountAsync();
        await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Income, AccountId = account.Id, Amount = 1, Date = new DateOnly(2026, 10, 1) }, Ct);

        account.CurrencyCode = "USD";

        Assert.False(await _store.SaveAccountAsync(account, Ct));
        Assert.Equal("EUR", (await _store.GetAccountsAsync(cancellationToken: Ct)).Single().CurrencyCode);
    }

    [Fact]
    [Trait("AT", "AT-10")]
    public async Task Archived_account_keeps_its_history_in_past_reports()
    {
        var account = await NewAccountAsync();
        await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 4_000, Date = new DateOnly(2026, 10, 10) }, Ct);

        account.IsArchived = true;
        Assert.True(await _store.SaveAccountAsync(account, Ct));

        Assert.Empty(await _store.GetAccountsAsync(includeArchived: false, cancellationToken: Ct));
        var all = await _store.GetAccountsAsync(cancellationToken: Ct);
        var entries = await _store.GetEntriesAsync(cancellationToken: Ct);
        var october = LedgerCalculator.Totals(all, entries, new LedgerFilter(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));
        Assert.Equal(4_000, october.Single().GrossExpense);
    }

    [Fact]
    public async Task Default_categories_are_created_once()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        await _store.EnsureDefaultCategoriesAsync(Ct);

        var categories = await _store.GetCategoriesAsync(Ct);
        Assert.Equal(DefaultCategories.All.Count, categories.Count);
        Assert.All(categories, c => Assert.NotNull(c.SystemKey));
    }

    [Fact]
    public async Task Defaults_added_later_are_created_without_touching_existing_categories()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var categories = await _store.GetCategoriesAsync(Ct);
        var food = categories.Single(c => c.SystemKey == "Food");
        food.Name = "Groceries";
        await _store.SaveCategoryAsync(food, Ct);
        var fees = categories.Single(c => c.SystemKey == DefaultCategories.Fees);
        await _services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContext().Categories.Where(c => c.Id == fees.Id).ExecuteDeleteAsync(Ct);

        await _store.EnsureDefaultCategoriesAsync(Ct);

        categories = await _store.GetCategoriesAsync(Ct);
        Assert.Equal(DefaultCategories.All.Count, categories.Count);
        Assert.Equal("Groceries", categories.Single(c => c.SystemKey == "Food").Name);
    }

    [Fact]
    [Trait("AT", "AT-08")]
    public async Task Transfer_and_fee_are_saved_together_and_deleted_together_with_undo()
    {
        var checking = await NewAccountAsync();
        var savings = await NewAccountAsync("Savings");
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = checking.Id, ToAccountId = savings.Id, Amount = 5_000, Date = new DateOnly(2026, 10, 3) };
        var fee = EntryActions.SyncTransferFee(transfer, null, 150, null)!;

        Assert.True((await _store.SaveEntriesAsync([transfer, fee], [], Ct)).Succeeded);
        Assert.Equal(2, (await _store.GetGroupAsync(transfer.GroupId!.Value, Ct)).Count);

        var deleted = await _store.DeleteEntryAsync(transfer.Id, Ct);
        Assert.Equal(2, deleted.Count);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));

        await _store.RestoreEntriesAsync(deleted, Ct);
        Assert.Equal(2, (await _store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Removing_a_fee_deletes_it_in_the_same_save()
    {
        var checking = await NewAccountAsync();
        var savings = await NewAccountAsync("Savings");
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = checking.Id, ToAccountId = savings.Id, Amount = 5_000, Date = new DateOnly(2026, 10, 3) };
        var fee = EntryActions.SyncTransferFee(transfer, null, 150, null)!;
        await _store.SaveEntriesAsync([transfer, fee], [], Ct);

        Assert.True((await _store.SaveEntriesAsync([transfer], [fee.Id], Ct)).Succeeded);

        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task An_invalid_entry_in_a_batch_saves_nothing()
    {
        var checking = await NewAccountAsync();
        var valid = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 100, Date = new DateOnly(2026, 10, 3) };
        var invalid = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 0, Date = new DateOnly(2026, 10, 3) };

        var result = await _store.SaveEntriesAsync([valid, invalid], [], Ct);

        Assert.False(result.Succeeded);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    [Trait("AT", "AT-15")]
    public async Task Refunds_are_limited_to_the_purchase_amount()
    {
        var checking = await NewAccountAsync();
        var purchase = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 1_000, Date = new DateOnly(2026, 10, 3) };
        await _store.SaveEntryAsync(purchase, Ct);
        await _store.SaveEntryAsync(EntryActions.CreateRefund(purchase, 600, checking.Id, new DateOnly(2026, 10, 4)), Ct);

        var tooMuch = await _store.SaveEntryAsync(EntryActions.CreateRefund(purchase, 500, checking.Id, new DateOnly(2026, 10, 5)), Ct);

        Assert.Contains(LedgerError.RefundExceedsPurchase, tooMuch.Errors);
        Assert.Single(await _store.GetRefundsAsync(purchase.Id, Ct));
    }

    [Fact]
    public async Task Budget_limits_are_replaced_on_save_and_budgets_are_found_by_period()
    {
        var food = Guid.CreateVersion7();
        var housing = Guid.CreateVersion7();
        var budget = new Vafadar.Zanance.Core.Budgets.Budget { Year = 1406, Month = 1, Calendar = Vafadar.Zanance.Core.Budgets.PeriodCalendar.Persian, CurrencyCode = "EUR", TotalLimit = 100_000 };
        budget.CategoryLimits.Add(new() { CategoryId = food, Limit = 30_000 });
        await _store.SaveBudgetAsync(budget, Ct);

        budget.TotalLimit = 120_000;
        budget.CategoryLimits = [new() { CategoryId = housing, Limit = 50_000 }];
        await _store.SaveBudgetAsync(budget, Ct);

        var reloaded = await _store.GetBudgetAsync(1406, 1, Vafadar.Zanance.Core.Budgets.PeriodCalendar.Persian, "EUR", Ct);
        Assert.NotNull(reloaded);
        Assert.Equal(120_000, reloaded.TotalLimit);
        Assert.Equal(housing, reloaded.CategoryLimits.Single().CategoryId);
        Assert.Null(await _store.GetBudgetAsync(1406, 1, Vafadar.Zanance.Core.Budgets.PeriodCalendar.Gregorian, "EUR", Ct));

        await _store.DeleteBudgetAsync(budget.Id, Ct);
        Assert.Empty(await _store.GetBudgetsAsync(Ct));
    }

    [Fact]
    [Trait("AT", "AT-54")]
    public async Task Import_is_all_or_nothing_skips_known_ids_and_can_be_undone()
    {
        var account = await NewAccountAsync();
        var before = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 100, Date = new DateOnly(2026, 10, 2) };
        await _store.SaveEntryAsync(before, Ct);
        var first = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 200, Date = new DateOnly(2026, 10, 3) };
        var invalid = new LedgerEntry { Kind = EntryKind.Expense, AccountId = Guid.CreateVersion7(), Amount = 300, Date = new DateOnly(2026, 10, 3) };

        var rejected = await _store.ImportAsync([first, invalid], Ct);
        Assert.Null(rejected.BatchId);
        Assert.Single(rejected.Errors);
        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));

        var imported = await _store.ImportAsync([first, new LedgerEntry(before.Id) { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 100, Date = new DateOnly(2026, 10, 2) }], Ct);
        Assert.Equal(1, imported.Imported);
        Assert.Equal(1, imported.Skipped);
        Assert.Single(await _store.GetImportBatchesAsync(Ct));

        Assert.Equal(1, await _store.UndoImportAsync(imported.BatchId!.Value, Ct));
        Assert.Equal(before.Id, (await _store.GetEntriesAsync(cancellationToken: Ct)).Single().Id);
    }

    [Fact]
    public async Task Merging_categories_moves_entries_and_limits_and_archives_the_source()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var categories = await _store.GetCategoriesAsync(Ct);
        var food = categories.Single(c => c.SystemKey == "Food");
        var leisure = categories.Single(c => c.SystemKey == "Leisure");
        var account = await NewAccountAsync();
        var entry = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 100, Date = new DateOnly(2026, 10, 2), CategoryId = leisure.Id };
        await _store.SaveEntryAsync(entry, Ct);
        var budget = new Vafadar.Zanance.Core.Budgets.Budget { Year = 2026, Month = 10, CurrencyCode = "EUR" };
        budget.CategoryLimits.Add(new() { CategoryId = leisure.Id, Limit = 5_000 });
        budget.CategoryLimits.Add(new() { CategoryId = food.Id, Limit = 20_000 });
        await _store.SaveBudgetAsync(budget, Ct);

        await _store.MergeCategoryAsync(leisure.Id, food.Id, Ct);

        Assert.Equal(food.Id, (await _store.GetEntryAsync(entry.Id, Ct))!.CategoryId);
        Assert.True((await _store.GetCategoriesAsync(Ct)).Single(c => c.Id == leisure.Id).IsArchived);
        Assert.Equal(25_000, (await _store.GetBudgetsAsync(Ct)).Single().CategoryLimits.Single().Limit);
    }

    [Fact]
    public async Task Categories_can_be_reordered()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var expense = (await _store.GetCategoriesAsync(Ct)).Where(c => c.Kind == CategoryKind.Expense).OrderBy(c => c.SortOrder).ToList();

        await _store.MoveCategoryAsync(expense[1].Id, -1, Ct);

        var reordered = (await _store.GetCategoriesAsync(Ct)).Where(c => c.Kind == CategoryKind.Expense).OrderBy(c => c.SortOrder).ToList();
        Assert.Equal(expense[1].Id, reordered[0].Id);
        Assert.Equal(expense[0].Id, reordered[1].Id);
    }

    [Fact]
    public async Task Merging_a_main_category_into_its_own_child_makes_the_child_a_main_category()
    {
        var food = new Category { Kind = CategoryKind.Expense, Name = "Food" };
        await _store.SaveCategoryAsync(food, Ct);
        var groceries = new Category { Kind = CategoryKind.Expense, Name = "Groceries", ParentId = food.Id };
        var restaurants = new Category { Kind = CategoryKind.Expense, Name = "Restaurants", ParentId = food.Id };
        await _store.SaveCategoryAsync(groceries, Ct);
        await _store.SaveCategoryAsync(restaurants, Ct);

        await _store.MergeCategoryAsync(food.Id, groceries.Id, Ct);

        var all = await _store.GetCategoriesAsync(Ct);
        Assert.Null(all.Single(c => c.Id == groceries.Id).ParentId);
        Assert.Equal(groceries.Id, all.Single(c => c.Id == restaurants.Id).ParentId);
        Assert.True(all.Single(c => c.Id == food.Id).IsArchived);
    }

    [Fact]
    public async Task Templates_are_kept_in_order_follow_a_category_merge_and_are_deleted_with_all_data()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var account = await NewAccountAsync();
        var categories = (await _store.GetCategoriesAsync(Ct)).Where(c => c.Kind == CategoryKind.Expense).ToList();
        await _store.SaveTemplateAsync(new EntryTemplate { Name = "Fuel", Kind = EntryKind.Expense, AccountId = account.Id, CategoryId = categories[0].Id }, Ct);
        await _store.SaveTemplateAsync(new EntryTemplate { Name = "Coffee", Kind = EntryKind.Expense, AccountId = account.Id, CategoryId = categories[1].Id, Amount = 350 }, Ct);

        var templates = await _store.GetTemplatesAsync(Ct);
        Assert.Equal(["Fuel", "Coffee"], templates.Select(t => t.Name));

        await _store.MergeCategoryAsync(categories[1].Id, categories[0].Id, Ct);
        Assert.All(await _store.GetTemplatesAsync(Ct), t => Assert.Equal(categories[0].Id, t.CategoryId));

        await _store.DeleteTemplateAsync(templates[0].Id, Ct);
        Assert.Equal("Coffee", Assert.Single(await _store.GetTemplatesAsync(Ct)).Name);

        await _store.DeleteAllDataAsync(Ct);
        Assert.Empty(await _store.GetTemplatesAsync(Ct));
    }

    [Fact]
    public async Task Goals_and_allocations_are_stored_without_touching_balances_and_are_deleted_with_all_data()
    {
        var goals = _services.GetRequiredService<GoalStore>();
        var account = await NewAccountAsync();
        var goal = new Vafadar.Zanance.Core.Goals.Goal { Name = "Travel", TargetAmount = 1_000_00, CurrencyCode = account.CurrencyCode };
        await goals.SaveGoalAsync(goal, Ct);
        await goals.AddAllocationAsync(new Vafadar.Zanance.Core.Goals.GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = 300_00, Date = new DateOnly(2026, 10, 1) }, Ct);
        await goals.AddAllocationAsync(new Vafadar.Zanance.Core.Goals.GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = -50_00, Date = new DateOnly(2026, 10, 2) }, Ct);

        Assert.Equal(250_00, (await goals.GetAllocationsAsync(goal.Id, Ct)).Sum(a => a.Amount));
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => goals.AddAllocationAsync(new Vafadar.Zanance.Core.Goals.GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = 0 }, Ct));

        await _store.DeleteAllDataAsync(Ct);
        Assert.Empty(await goals.GetGoalsAsync(Ct));
        Assert.Empty(await goals.GetAllocationsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Budget_rollover_carries_the_rest_of_the_previous_months()
    {
        var account = await NewAccountAsync();
        await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 300_00, Date = new DateOnly(2026, 10, 5) }, Ct);
        await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 450_00, Date = new DateOnly(2026, 11, 5) }, Ct);
        Budget Month(int month, BudgetRollover rollover) => new() { Year = 2026, Month = month, Calendar = PeriodCalendar.Gregorian, CurrencyCode = account.CurrencyCode, TotalLimit = 500_00, Rollover = rollover };
        await _store.SaveBudgetAsync(Month(10, BudgetRollover.None), Ct);
        await _store.SaveBudgetAsync(Month(11, BudgetRollover.Surplus), Ct);
        var december = Month(12, BudgetRollover.Surplus);
        await _store.SaveBudgetAsync(december, Ct);

        // October leaves 200, November receives it and leaves 250 for December.
        Assert.Equal(250_00, (await _store.GetBudgetCarryAsync(december, Ct)).Total);
        Assert.Equal(BudgetRollover.Surplus, (await _store.GetBudgetAsync(2026, 12, PeriodCalendar.Gregorian, account.CurrencyCode, Ct))!.Rollover);
    }

    [Fact]
    public async Task A_rule_text_has_one_category_rules_follow_a_merge_and_are_deleted_with_all_data()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var expense = (await _store.GetCategoriesAsync(Ct)).Where(c => c.Kind == CategoryKind.Expense).ToList();
        await _store.SaveCategoryRuleAsync(new CategoryRule { Match = "Market", CategoryId = expense[0].Id, Kind = CategoryKind.Expense }, Ct);
        await _store.SaveCategoryRuleAsync(new CategoryRule { Match = " market ", CategoryId = expense[1].Id, Kind = CategoryKind.Expense }, Ct);

        var rule = Assert.Single(await _store.GetCategoryRulesAsync(Ct));
        Assert.Equal(expense[1].Id, rule.CategoryId);
        Assert.Equal("market", rule.Match);
        await Assert.ThrowsAsync<ArgumentException>(() => _store.SaveCategoryRuleAsync(new CategoryRule { Match = "x", CategoryId = expense[0].Id }, Ct));

        await _store.MergeCategoryAsync(expense[1].Id, expense[2].Id, Ct);
        Assert.Equal(expense[2].Id, Assert.Single(await _store.GetCategoryRulesAsync(Ct)).CategoryId);

        await _store.DeleteAllDataAsync(Ct);
        Assert.Empty(await _store.GetCategoryRulesAsync(Ct));
    }

    [Fact]
    public async Task Saved_filters_keep_their_settings_and_a_second_save_with_the_same_name_replaces_the_first()
    {
        var category = Guid.NewGuid();
        await _store.SaveSavedFilterAsync(new SavedFilter { Name = "Groceries", Period = 1, Kind = 1, CategoryIds = [category], Search = "#home" }, Ct);
        await _store.SaveSavedFilterAsync(new SavedFilter { Name = "Trip", From = new DateOnly(2026, 7, 1), To = new DateOnly(2026, 7, 20), UnreviewedOnly = true, InTotalsOnly = true }, Ct);
        await _store.SaveSavedFilterAsync(new SavedFilter { Name = " groceries ", Period = 2, Kind = 1, CategoryIds = [category] }, Ct);

        var filters = await _store.GetSavedFiltersAsync(Ct);
        Assert.Equal(["groceries", "Trip"], filters.Select(f => f.Name));
        Assert.Equal((2, 1, category), (filters[0].Period, filters[0].Kind, filters[0].CategoryIds.Single()));
        Assert.Null(filters[0].Search);
        Assert.True(filters[1].HasCustomRange);
        Assert.True(filters[1].UnreviewedOnly);
        Assert.True(filters[1].InTotalsOnly);
        Assert.False(filters[0].InTotalsOnly);

        await _store.DeleteSavedFilterAsync(filters[1].Id, Ct);
        Assert.Single(await _store.GetSavedFiltersAsync(Ct));
        await _store.DeleteAllDataAsync(Ct);
        Assert.Empty(await _store.GetSavedFiltersAsync(Ct));
    }

    [Fact]
    public async Task The_budget_method_is_kept_when_a_budget_is_updated()
    {
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", CategoryLimits = [new BudgetCategoryLimit { CategoryId = Guid.NewGuid(), Limit = 400_00 }] };
        await _store.SaveBudgetAsync(budget, Ct);
        budget.Method = BudgetMethod.Envelopes;
        await _store.SaveBudgetAsync(budget, Ct);

        var stored = await _store.GetBudgetAsync(2026, 10, PeriodCalendar.Gregorian, "EUR", Ct);
        Assert.Equal(BudgetMethod.Envelopes, stored!.Method);
        Assert.Equal(BudgetMethod.Envelopes, BudgetPlanning.CopyTo(stored, 2026, 11).Method);
    }

    [Fact]
    public async Task Loan_terms_are_stored_with_the_account()
    {
        var loan = new Account { Name = "Car loan", Type = AccountType.Loan, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1), OpeningBalance = -500_000, InterestRate = 4.95m, Installment = 18_500 };
        await _store.SaveAccountAsync(loan, Ct);

        var stored = (await _store.GetAccountsAsync(cancellationToken: Ct)).Single(a => a.Id == loan.Id);
        Assert.Equal(4.95m, stored.InterestRate);
        Assert.Equal(18_500, stored.Installment);
        Assert.Null((await NewAccountAsync()).InterestRate);
    }

    [Fact]
    public async Task Receipts_of_removed_split_parts_move_to_the_remaining_part()
    {
        var account = await NewAccountAsync();
        var kept = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1_000, Date = new DateOnly(2026, 10, 1) };
        var removed = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 500, Date = new DateOnly(2026, 10, 1) };
        await _store.SaveEntriesAsync([kept, removed], [], Ct);
        await _store.AddAttachmentAsync(new EntryAttachment { EntryId = removed.Id, FileName = "receipt.pdf", ContentType = "application/pdf", Data = [1] }, Ct);

        await _store.SaveEntriesAsync([kept], [removed.Id], kept.Id, Ct);

        Assert.Equal(0, await _store.PurgeOrphanAttachmentsAsync(Ct));
        Assert.Single(await _store.GetAttachmentsAsync(kept.Id, Ct));
    }

    [Fact]
    public async Task Attachments_are_listed_without_content_survive_undo_and_are_purged_with_their_entry()
    {
        var account = await NewAccountAsync();
        var entry = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1_250, Date = new DateOnly(2026, 10, 1) };
        await _store.SaveEntryAsync(entry, Ct);
        await _store.AddAttachmentAsync(new EntryAttachment { EntryId = entry.Id, FileName = "receipt.jpg", ContentType = "image/jpeg", Data = [1, 2, 3] }, Ct);
        await Assert.ThrowsAsync<ArgumentException>(() => _store.AddAttachmentAsync(new EntryAttachment { EntryId = entry.Id, FileName = "empty", ContentType = "text/plain" }, Ct));

        var info = Assert.Single(await _store.GetAttachmentsAsync(entry.Id, Ct));
        Assert.Equal(3, info.Size);
        Assert.True(info.IsImage);
        Assert.Equal([1, 2, 3], (await _store.GetAttachmentAsync(info.Id, Ct))!.Data);

        // Deleted and restored within the undo time: the receipt is still there.
        var deleted = await _store.DeleteEntryAsync(entry.Id, Ct);
        await _store.RestoreEntriesAsync(deleted, Ct);
        Assert.Equal(0, await _store.PurgeOrphanAttachmentsAsync(Ct));
        Assert.Single(await _store.GetAttachmentsAsync(entry.Id, Ct));

        await _store.DeleteEntryAsync(entry.Id, Ct);
        Assert.Equal(1, await _store.PurgeOrphanAttachmentsAsync(Ct));
        Assert.Empty(await _store.GetAttachmentsAsync(entry.Id, Ct));
    }

    [Fact]
    public async Task Deleting_all_data_leaves_an_empty_database()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var account = await NewAccountAsync();
        await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Income, AccountId = account.Id, Amount = 1, Date = new DateOnly(2026, 10, 2) }, Ct);
        await _store.SaveSettingsAsync(await _store.GetSettingsAsync(Ct), Ct);

        await _store.DeleteAllDataAsync(Ct);

        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Empty(await _store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Empty(await _store.GetCategoriesAsync(Ct));
        Assert.False(_store.GetSettings().OnboardingCompleted);
    }

    [Fact]
    public async Task Settings_are_created_on_first_use_and_persist()
    {
        var settings = await _store.GetSettingsAsync(Ct);
        settings.ReportCurrencyCode = "USD";
        settings.OnboardingCompleted = true;
        await _store.SaveSettingsAsync(settings, Ct);

        var reloaded = await _store.GetSettingsAsync(Ct);
        Assert.Equal("USD", reloaded.ReportCurrencyCode);
        Assert.True(reloaded.OnboardingCompleted);
    }

    [Fact]
    public async Task Second_settlement_of_the_same_occurrence_is_rejected_by_the_database()
    {
        var account = await NewAccountAsync();
        var schedule = Guid.CreateVersion7();
        var date = new DateOnly(2026, 10, 1);
        await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1, Date = date, ScheduleId = schedule, OccurrenceDate = date }, Ct);

        await Assert.ThrowsAnyAsync<Exception>(() => _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1, Date = date, ScheduleId = schedule, OccurrenceDate = date }, Ct));
        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    private async Task<Account> NewAccountAsync(string name = "Checking")
    {
        var account = new Account { Name = name, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 10, 1) };
        await _store.SaveAccountAsync(account, Ct);
        return account;
    }
}
