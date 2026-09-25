using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Categories;

/// <summary>A category in the list; sub-categories are indented under their parent.</summary>
public sealed record CategoryItem(Guid Id, string Name, Symbol Icon, Color Color, Color Background, bool IsChild);

/// <summary>Category management (CAT-01..03): defaults are editable, nothing is deleted – only archived.</summary>
public sealed partial class CategoriesViewModel(FinanceStore store, Translator translator) : ViewModelBase
{
    public ObservableCollection<CategoryItem> Expense { get; } = [];

    public ObservableCollection<CategoryItem> Income { get; } = [];

    public ObservableCollection<CategoryItem> Archived { get; } = [];

    [ObservableProperty]
    public partial bool HasArchived { get; set; }

    public async Task LoadAsync()
    {
        var categories = await store.GetCategoriesAsync();
        Expense.Clear();
        Income.Clear();
        Archived.Clear();

        foreach (var category in categories.Where(c => c.IsArchived))
        {
            Archived.Add(ToItem(category, false));
        }

        foreach (var parent in categories.Where(c => !c.IsArchived && c.ParentId is null).OrderBy(c => c.SortOrder))
        {
            var target = parent.Kind == CategoryKind.Income ? Income : Expense;
            target.Add(ToItem(parent, false));
            foreach (var child in categories.Where(c => !c.IsArchived && c.ParentId == parent.Id).OrderBy(c => c.SortOrder))
            {
                target.Add(ToItem(child, true));
            }
        }

        HasArchived = Archived.Count > 0;
    }

    private CategoryItem ToItem(Category category, bool isChild)
    {
        var color = CategoryLookup.ParseColor(category.Color);
        return new CategoryItem(category.Id, CategoryLookup.NameOf(category, translator), Icons.Parse(category.Icon, Symbol.Tag), color, color.WithAlpha(0.12f), isChild);
    }

    [RelayCommand]
    private Task OpenAsync(CategoryItem item) =>
        Shell.Current.GoToAsync(AppShell.CategoryEditorRoute, new Dictionary<string, object> { ["id"] = item.Id });

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(AppShell.CategoryEditorRoute);
}
