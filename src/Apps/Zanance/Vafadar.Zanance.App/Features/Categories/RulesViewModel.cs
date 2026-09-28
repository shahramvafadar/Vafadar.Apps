using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.Categories;

/// <summary>A rule in the list.</summary>
public sealed record RuleRow(Guid Id, string Match, string CategoryName);

/// <summary>A category a rule can suggest.</summary>
public sealed record RuleCategory(Guid Id, CategoryKind Kind, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Local categorization rules (F2-TX-04): the text to look for in payee or title and the category to suggest. Rules only
/// suggest categories for new entries and imports; saved entries never change.
/// </summary>
public sealed partial class RulesViewModel(ZananceStore store, Translator translator) : ViewModelBase
{
    public ObservableCollection<RuleRow> Rules { get; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<RuleCategory> CategoryOptions { get; set; } = [];

    [ObservableProperty]
    public partial RuleCategory? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial string MatchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial bool HasRules { get; set; }

    public async Task LoadAsync()
    {
        var lookup = new CategoryLookup(await store.GetCategoriesAsync(), translator);
        CategoryOptions =
        [
            .. lookup.All
                .Where(c => !c.IsArchived && c.SystemKey != DefaultCategories.Uncategorized)
                .OrderBy(c => c.Kind).ThenBy(c => c.SortOrder)
                .Select(c => new RuleCategory(c.Id, c.Kind, $"{translator[c.Kind == CategoryKind.Income ? "KindFilter_Income" : "KindFilter_Expenses"]} · {lookup.Name(c.Id)}")),
        ];
        SelectedCategory ??= CategoryOptions.FirstOrDefault();

        Rules.Clear();
        foreach (var rule in await store.GetCategoryRulesAsync())
        {
            Rules.Add(new RuleRow(rule.Id, rule.Match, lookup.Name(rule.CategoryId)));
        }

        HasRules = Rules.Count > 0;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        Error = null;
        if (MatchText.Trim().Length < CategoryRules.MinLength || SelectedCategory is null)
        {
            Error = translator["Rule_TextRequired"];
            return;
        }

        await store.SaveCategoryRuleAsync(new CategoryRule { Match = MatchText, CategoryId = SelectedCategory.Id, Kind = SelectedCategory.Kind });
        MatchText = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(RuleRow row)
    {
        if (await Shell.Current.DisplayAlertAsync(translator["Rule_Delete"], $"{row.Match} → {row.CategoryName}", translator["Common_Delete"], translator["Common_Cancel"]))
        {
            await store.DeleteCategoryRuleAsync(row.Id);
            await LoadAsync();
        }
    }
}