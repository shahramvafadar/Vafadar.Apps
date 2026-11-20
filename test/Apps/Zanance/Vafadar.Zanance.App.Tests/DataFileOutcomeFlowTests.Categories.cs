using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class DataFileOutcomeFlowTests
{
    /// <summary>An open picker cannot apply a category after its original displayed row has been replaced.</summary>
    [Fact]
    public async Task Category_picker_retires_when_the_displayed_csv_row_is_rebuilt()
    {
        using var f = new FlowFixture();
        var files = new Files();
        await LedgerAsync(f, files);
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        files.Picked = new("fictitious-refresh.csv", "date;amount;title\r\n2026-10-09;-9.25;Fictitious pharmacy\r\n");
        var (_, _, _, vm) = Create(f, new(), files);
        await vm.LoadAsync();
        await vm.PickCommand.ExecuteAsync(null);
        await vm.PreviewCommand.ExecuteAsync(null);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var original = Assert.Single(vm.LedgerPreviewRows);
        var release = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingChoice = () => release.Task;

        var open = vm.ChooseCsvCategoryCommand.ExecuteAsync(original);
        var options = Assert.Single(f.Platform.OfferedActions);
        var healthLabel = f.Translator["Category_Health"];
        var choice = Assert.Single(options, option => option.EndsWith(". " + healthLabel, StringComparison.Ordinal));
        vm.SkipDuplicates = !vm.SkipDuplicates;
        var current = Assert.Single(vm.LedgerPreviewRows);
        Assert.NotSame(original, current);
        release.SetResult(choice);
        await open;

        Assert.Same(current, Assert.Single(vm.LedgerPreviewRows));
        Assert.NotEqual(healthLabel, current.Category);

        // A new selection on the replacement row still belongs to the current draft.
        f.Platform.PendingChoice = null;
        f.Platform.Choices.Enqueue(choice);
        await vm.ChooseCsvCategoryCommand.ExecuteAsync(current);
        Assert.Equal(healthLabel, Assert.Single(vm.LedgerPreviewRows).Category);
        Assert.True(vm.CanRunImport);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Qualifies only the new editable generic CSV classification with isolated SQLite.</summary>
    [Fact]
    public async Task Local_category_csv_drafts_preserve_original_rules_and_commit_only_explicit_choices()
    {
        using var f = new FlowFixture();
        var files = new Files();
        await LedgerAsync(f, files);
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        var categories = (await f.Store.GetCategoriesAsync(Ct)).Where(category => category.Kind == CategoryKind.Expense).ToArray();
        var health = categories.Single(item => item.SystemKey == "Health");
        var food = categories.Single(item => item.SystemKey == "Food");
        await f.Store.SaveCategoryRuleAsync(new CategoryRule { Match = "Rule market", Kind = CategoryKind.Expense, CategoryId = health.Id }, Ct);
        files.Picked = new("fictitious-category.csv", "date;amount;title\r\n2026-10-09;-12.34;Fictitious supermarket\r\n2026-10-10;-22.00;Rule market supermarket\r\n2026-10-11;-35.00;Fictitious unknown description\r\n");
        var (store, _, _, vm) = Create(f, new(), files);
        await vm.LoadAsync();
        await vm.PickCommand.ExecuteAsync(null);
        await vm.PreviewCommand.ExecuteAsync(null);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.Equal(3, vm.LedgerPreviewRows.Count);
        var candidate = vm.LedgerPreviewRows[0];
        Assert.True(candidate.CanUseCategory);
        Assert.Contains(f.Translator["Category_Food"], candidate.CategoryHint);
        Assert.False(vm.LedgerPreviewRows[1].CanChooseCategory);
        Assert.Equal(f.Translator["CategoryReview_NoMatch"], vm.LedgerPreviewRows[2].CategoryHint);
        vm.UseCsvCategoryCommand.Execute(candidate);
        Assert.Equal(f.Translator["Category_Food"], vm.LedgerPreviewRows[0].Category);
        Assert.False(vm.LedgerPreviewRows[0].CanUseCategory);
        await vm.ChooseCsvCategoryCommand.ExecuteAsync(candidate);
        Assert.Empty(f.Platform.OfferedActions);
        f.Platform.Choices.Enqueue(f.Translator["CategoryReview_Unassigned"]);
        await vm.ChooseCsvCategoryCommand.ExecuteAsync(vm.LedgerPreviewRows[2]);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.True(vm.CanRunImport);
        await vm.ImportCommand.ExecuteAsync(null);
        var saved = (await store.GetEntriesAsync(cancellationToken: Ct)).OrderBy(entry => entry.Date).ToArray();
        Assert.Equal(3, saved.Length);
        Assert.Equal(food.Id, saved[0].CategoryId);
        Assert.Equal(health.Id, saved[1].CategoryId);
        Assert.Equal(categories.Single(item => item.SystemKey == DefaultCategories.Uncategorized).Id, saved[2].CategoryId);
        Assert.All(saved, entry => Assert.Equal(ReviewState.Unreviewed, entry.Review));
    }

    [Fact]
    public async Task Explicit_csv_category_wins_and_an_ambiguous_line_can_be_manually_changed()
    {
        using var f = new FlowFixture();
        var files = new Files();
        await LedgerAsync(f, files);
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        var categories = (await f.Store.GetCategoriesAsync(Ct)).Where(category => category.Kind == CategoryKind.Expense && !category.IsArchived)
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Id).ToArray();
        var health = categories.Single(item => item.SystemKey == "Health");
        var foodLabel = f.Translator["Category_Food"];
        var healthLabel = f.Translator["Category_Health"];
        files.Picked = new("fictitious-category-manual.csv",
            $"date;amount;title;category\r\n2026-10-09;-9.25;Fictitious pharmacy;{foodLabel}\r\n2026-10-10;-6.50;Pharmacy supermarket;");
        var (_, _, _, vm) = Create(f, new(), files);
        await vm.LoadAsync();
        await vm.PickCommand.ExecuteAsync(null);
        await vm.PreviewCommand.ExecuteAsync(null);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.Equal(foodLabel, vm.LedgerPreviewRows[0].Category);
        Assert.Null(vm.LedgerPreviewRows[0].CategoryHint);
        Assert.Equal(f.Translator["CategoryReview_ConflictingSignals"], vm.LedgerPreviewRows[1].CategoryHint);
        f.Platform.Choices.Enqueue($"{Array.IndexOf(categories, health) + 1}. {healthLabel}");
        await vm.ChooseCsvCategoryCommand.ExecuteAsync(vm.LedgerPreviewRows[1]);
        Assert.Equal(healthLabel, vm.LedgerPreviewRows[1].Category);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
