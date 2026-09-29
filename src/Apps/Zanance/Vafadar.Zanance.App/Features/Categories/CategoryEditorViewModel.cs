using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Categories;

/// <summary>A selectable icon or colour swatch.</summary>
public sealed partial class Swatch(string key, Symbol icon, Color color, string label = "", string selectedLabel = "") : ObservableObject
{
    public string Key { get; } = key;

    public Symbol Icon { get; } = icon;

    public Color Color { get; } = color;

    /// <summary>Gets what a screen reader says, e.g. "Colour 3, selected" – never a hex code or an icon key.</summary>
    public string Description => IsSelected && selectedLabel.Length > 0 ? selectedLabel : label;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    public partial bool IsSelected { get; set; }
}

/// <summary>A possible parent category.</summary>
public sealed record ParentChoice(Guid? Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>Creates and edits a category: name, kind (new only), icon, colour and one parent level (CAT-02).</summary>
public sealed partial class CategoryEditorViewModel : ViewModelBase, IQueryAttributable
{
    private static readonly string[] IconKeys =
    [
        "Home", "Food", "FoodPizza", "Cart", "ShoppingBag", "Flash", "Drop", "Fire", "Phone", "Laptop", "Tv", "VehicleCar",
        "Gas", "Airplane", "Beach", "Heart", "Pill", "Stethoscope", "Dumbbell", "Shield", "HatGraduation", "Book", "People",
        "Person", "Games", "MusicNote1", "Sport", "ArrowRepeatAll", "Gift", "Money", "Savings", "BuildingBank", "Briefcase",
        "Handshake", "Receipt", "Wrench", "PaintBrush", "Box", "Balloon", "Umbrella", "Star", "Tag", "MoreHorizontal", "QuestionCircle",
    ];

    private static readonly string[] ColorKeys =
    [
        "#2E7D32", "#00838F", "#0277BD", "#283593", "#4527A0", "#6A1B9A", "#AD1457", "#C62828", "#E65100", "#F9A825", "#5D4037", "#546E7A",
    ];

    private readonly ZananceStore _store;
    private readonly Translator _translator;
    private Category _category = new();
    private string _snapshot = string.Empty;
    private List<Category> _all = [];

    public CategoryEditorViewModel(ZananceStore store, Translator translator)
    {
        _store = store;
        _translator = translator;
        Title = translator["Category_NewTitle"];
        Name = string.Empty;
        KindNames = [translator["CategoryKind_Expense"], translator["CategoryKind_Income"]];
        SpendingTypeNames = [translator["SpendingType_Flexible"], translator["SpendingType_Fixed"], translator["SpendingType_NonMonthly"]];
        Parents = [];
        Icons = [.. IconKeys.Select((key, i) => Labelled(key, Presentation.Icons.Parse(key, Symbol.Tag), Colors.Transparent, "Category_IconOption", i))];
        Palette = [.. ColorKeys.Select((key, i) => Labelled(key, Symbol.Circle, Color.FromArgb(key), "Category_ColorOption", i))];
        SelectedColor = Color.FromArgb(ColorKeys[0]);
        Select(Icons, "Tag");
        Select(Palette, ColorKeys[0]);
    }

    public IReadOnlyList<string> KindNames { get; }

    /// <summary>Gets the flex spending types (D-28), in the order of <see cref="SpendingType"/>.</summary>
    public IReadOnlyList<string> SpendingTypeNames { get; }

    public IReadOnlyList<Swatch> Icons { get; }

    public IReadOnlyList<Swatch> Palette { get; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string? DefaultName { get; set; }

    [ObservableProperty]
    public partial string? NameError { get; set; }

    [ObservableProperty]
    public partial int KindIndex { get; set; }

    [ObservableProperty]
    public partial bool IsExisting { get; set; }

    // Flex budgets (D-28): how a top-level expense category is budgeted; sub-categories follow their parent.
    [ObservableProperty]
    public partial int SpendingTypeIndex { get; set; }

    /// <summary>Gets a value indicating whether the spending type can be chosen (a top-level expense category).</summary>
    public bool ShowSpendingType => Kind == CategoryKind.Expense && (!CanHaveParent || Parent?.Id is null);

    partial void OnParentChanged(ParentChoice? value) => OnPropertyChanged(nameof(ShowSpendingType));

    partial void OnCanHaveParentChanged(bool value) => OnPropertyChanged(nameof(ShowSpendingType));

    [ObservableProperty]
    public partial bool IsArchived { get; set; }

    [ObservableProperty]
    public partial bool CanHaveParent { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ParentChoice> Parents { get; set; }

    [ObservableProperty]
    public partial ParentChoice? Parent { get; set; }

    [ObservableProperty]
    public partial Color SelectedColor { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ParentChoice> MergeTargets { get; set; } = [];

    [ObservableProperty]
    public partial ParentChoice? MergeTarget { get; set; }

    public bool IsDirty => Snapshot() != _snapshot;

    private CategoryKind Kind => KindIndex == 1 ? CategoryKind.Income : CategoryKind.Expense;

    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));

    private async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var categories = await _store.GetCategoriesAsync();
        if (query.TryGetValue("id", out var value) && value is Guid id && categories.FirstOrDefault(c => c.Id == id) is { } category)
        {
            _category = category;
            Title = _translator["Category_EditTitle"];
            IsExisting = true;
            IsArchived = category.IsArchived;
            KindIndex = category.Kind == CategoryKind.Income ? 1 : 0;
            SpendingTypeIndex = (int)category.SpendingType;
            Name = category.Name ?? string.Empty;
            DefaultName = category.SystemKey is { } key ? _translator[$"Category_{key}"] : null;
            Select(Icons, category.Icon ?? "Tag");
            Select(Palette, category.Color ?? ColorKeys[0]);
            SelectedColor = CategoryLookup.ParseColor(category.Color);
        }

        // Only one level (CAT-02): a category with children, and the fallback category, cannot get a parent.
        CanHaveParent = _category.SystemKey != DefaultCategories.Uncategorized && !categories.Any(c => c.ParentId == _category.Id);
        MergeTargets = [.. categories
            .Where(c => IsExisting && c.Kind == _category.Kind && c.Id != _category.Id && !c.IsArchived)
            .OrderBy(c => c.SortOrder)
            .Select(c => new ParentChoice(c.Id, CategoryLookup.NameOf(c, _translator)))];
        BuildParents(categories);
        query.Clear();
        _snapshot = Snapshot();
    }

    private void BuildParents(List<Category> categories)
    {
        _all = categories;
        Parents =
        [
            new ParentChoice(null, _translator["Category_NoParent"]),
            .. categories
                .Where(c => c.Kind == Kind && c.ParentId is null && !c.IsArchived && c.Id != _category.Id && c.SystemKey != DefaultCategories.Uncategorized)
                .OrderBy(c => c.SortOrder)
                .Select(c => new ParentChoice(c.Id, CategoryLookup.NameOf(c, _translator))),
        ];
        Parent = Parents.FirstOrDefault(p => p.Id == _category.ParentId) ?? Parents[0];
    }

    partial void OnKindIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ShowSpendingType));
        if (!IsExisting && _all.Count > 0)
        {
            BuildParents(_all);
        }
    }

    private static void Select(IEnumerable<Swatch> swatches, string key)
    {
        foreach (var swatch in swatches)
        {
            swatch.IsSelected = string.Equals(swatch.Key, key, StringComparison.OrdinalIgnoreCase);
        }
    }

    private Swatch Labelled(string key, Symbol icon, Color color, string labelKey, int index)
    {
        var label = _translator.Format(labelKey, index + 1);
        return new Swatch(key, icon, color, label, _translator.Format("Common_SelectedItem", label));
    }

    [RelayCommand]
    private void SelectIcon(Swatch swatch) => Select(Icons, swatch.Key);

    [RelayCommand]
    private void SelectColor(Swatch swatch)
    {
        Select(Palette, swatch.Key);
        SelectedColor = swatch.Color;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        NameError = string.IsNullOrWhiteSpace(Name) && DefaultName is null ? _translator["Category_NameRequired"] : null;
        if (NameError is not null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            // A starter category keeps following the language until the user gives it an own name.
            _category.Name = string.IsNullOrWhiteSpace(Name) || Name.Trim() == DefaultName ? null : Name.Trim();
            if (!IsExisting)
            {
                _category.Kind = Kind;
                _category.SortOrder = _all.Count == 0 ? 0 : _all.Max(c => c.SortOrder) + 1;
            }

            _category.Icon = Icons.FirstOrDefault(i => i.IsSelected)?.Key;
            _category.Color = Palette.FirstOrDefault(c => c.IsSelected)?.Key;
            _category.ParentId = CanHaveParent ? Parent?.Id : null;
        _category.SpendingType = Enum.IsDefined((SpendingType)SpendingTypeIndex) ? (SpendingType)SpendingTypeIndex : SpendingType.Flexible;
            await _store.SaveCategoryAsync(_category);
            _snapshot = Snapshot();
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleArchiveAsync()
    {
        if (!IsArchived && !await Shell.Current.DisplayAlertAsync(
                _translator["Category_Archive"], _translator["Category_ArchiveMessage"], _translator["Category_Archive"], _translator["Common_Cancel"]))
        {
            return;
        }

        _category.IsArchived = !IsArchived;
        await _store.SaveCategoryAsync(_category);
        await Shell.Current.GoToAsync("..");
    }

    // CAT-02: merging keeps every entry; the source is archived afterwards.
    [RelayCommand]
    private async Task MergeAsync()
    {
        if (MergeTarget?.Id is not { } target || !await Shell.Current.DisplayAlertAsync(
                _translator["Category_Merge"], _translator.Format("Category_MergeMessage", MergeTarget.Name), _translator["Category_Merge"], _translator["Common_Cancel"]))
        {
            return;
        }

        await _store.MergeCategoryAsync(_category.Id, target);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task MoveUpAsync() => MoveAsync(-1);

    [RelayCommand]
    private Task MoveDownAsync() => MoveAsync(1);

    // The new position is taken over, so a later Save does not restore the old one.
    private async Task MoveAsync(int step)
    {
        await _store.MoveCategoryAsync(_category.Id, step);
        _category.SortOrder = (await _store.GetCategoriesAsync()).First(c => c.Id == _category.Id).SortOrder;
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (!IsDirty || await ConfirmDiscardAsync())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    public Task<bool> ConfirmDiscardAsync() => Shell.Current.DisplayAlertAsync(
        _translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]);

    private string Snapshot() => string.Join('|', Name, KindIndex, SpendingTypeIndex, Parent?.Id,
        Icons.FirstOrDefault(i => i.IsSelected)?.Key, Palette.FirstOrDefault(c => c.IsSelected)?.Key);
}
