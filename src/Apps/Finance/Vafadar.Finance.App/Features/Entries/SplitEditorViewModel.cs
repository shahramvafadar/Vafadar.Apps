using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Entries;

/// <summary>A category to choose for a split part; <see cref="Id"/> is <see langword="null"/> for "Uncategorized".</summary>
public sealed record SplitCategory(Guid? Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>One part of a split.</summary>
public sealed partial class SplitPart(IReadOnlyList<SplitCategory> categories, Action changed) : ObservableObject
{
    public IReadOnlyList<SplitCategory> Categories { get; } = categories;

    [ObservableProperty]
    public partial SplitCategory? Category { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    partial void OnAmountTextChanged(string value) => changed();
}

/// <summary>
/// Splits one purchase or income across categories, or changes and joins an existing split (F2-TX-01). The parts must
/// add up exactly to the amount; the account balance changes by the same total.
/// </summary>
public sealed partial class SplitEditorViewModel(FinanceStore store, Translator translator, ILocalizationService localization) : ViewModelBase, IQueryAttributable
{
    private List<LedgerEntry> _parts = [];
    private Currency _currency = Currencies.Euro;
    private long _total;
    private IReadOnlyList<SplitCategory> _categories = [];

    public ObservableCollection<SplitPart> Parts { get; } = [];

    [ObservableProperty]
    public partial string TotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? RemainingText { get; set; }

    [ObservableProperty]
    public partial Color RemainingColor { get; set; } = Color.FromArgb("#5F6368");

    [ObservableProperty]
    public partial bool CanSave { get; set; }

    [ObservableProperty]
    public partial bool IsSplit { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue("id", out var value) || value is not Guid id || await store.GetEntryAsync(id) is not { } entry)
        {
            return;
        }

        query.Clear();
        _parts = entry.GroupId is { } group ? [.. (await store.GetGroupAsync(group)).OrderBy(e => e.Id == entry.Id ? 0 : 1).ThenBy(e => e.CreatedAt)] : [entry];
        if (!EntryActions.CanSplit(_parts))
        {
            Error = translator["Split_NotPossible"];
            return;
        }

        var accounts = await store.GetAccountsAsync();
        _currency = Currencies.TryGet(accounts.FirstOrDefault(a => a.Id == entry.AccountId)?.CurrencyCode ?? string.Empty, out var currency) ? currency : Currencies.Euro;
        _total = _parts.Sum(p => p.Amount);
        TotalText = MoneyText.Format(_total, _currency.Code, localization.CurrentCulture);
        IsSplit = _parts.Count > 1;

        var kind = entry.Kind == EntryKind.Income ? CategoryKind.Income : CategoryKind.Expense;
        var lookup = new CategoryLookup(await store.GetCategoriesAsync(), translator);
        _categories =
        [
            new SplitCategory(null, translator["Category_Uncategorized"]),
            .. lookup.All
                .Where(c => c.Kind == kind && !c.IsArchived)
                .OrderBy(c => lookup.Get(c.ParentId)?.SortOrder ?? c.SortOrder).ThenBy(c => c.ParentId is null ? 0 : 1).ThenBy(c => c.SortOrder)
                .Select(c => new SplitCategory(c.Id, c.ParentId is { } parent ? $"{lookup.Name(parent)} › {CategoryLookup.NameOf(c, translator)}" : CategoryLookup.NameOf(c, translator))),
        ];

        Parts.Clear();
        foreach (var part in _parts)
        {
            AddPart(part.CategoryId, part.Amount);
        }

        if (Parts.Count == 1)
        {
            AddPart(null, 0);
        }

        Update();
    }

    private void AddPart(Guid? categoryId, long amount) => Parts.Add(new SplitPart(_categories, Update)
    {
        Category = _categories.FirstOrDefault(c => c.Id == categoryId) ?? _categories[0],
        AmountText = amount > 0 ? MoneyText.ForInput(amount, _currency.Code, localization.CurrentCulture) : string.Empty,
    });

    [RelayCommand]
    private void Add()
    {
        AddPart(null, 0);
        Update();
    }

    [RelayCommand]
    private void Remove(SplitPart part)
    {
        if (Parts.Count > 2)
        {
            Parts.Remove(part);
            Update();
        }
    }

    // Shows how much is still to assign; saving is possible only when the parts add up exactly (F2-TX-01).
    private void Update()
    {
        var assigned = 0L;
        var valid = true;
        foreach (var part in Parts)
        {
            if (!MoneyAmount.TryParse(part.AmountText, _currency, localization.CurrentCulture, out var amount) || amount <= 0)
            {
                valid = false;
                continue;
            }

            assigned += amount;
        }

        var rest = _total - assigned;
        var culture = localization.CurrentCulture;
        RemainingText = rest switch
        {
            > 0 => translator.Format("Split_Remaining", MoneyText.Format(rest, _currency.Code, culture)),
            < 0 => translator.Format("Split_TooMuch", MoneyText.Format(-rest, _currency.Code, culture)),
            _ when !valid => translator["Split_EnterAll"],
            _ => translator["Split_Balanced"],
        };
        RemainingColor = rest == 0 && valid ? Color.FromArgb("#1B5E20") : Color.FromArgb("#8D5B00");
        CanSave = valid && rest == 0 && Parts.Count >= 2;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanSave || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var shares = Parts.Select(p =>
            {
                MoneyAmount.TryParse(p.AmountText, _currency, localization.CurrentCulture, out var amount);
                return (p.Category?.Id, amount);
            }).ToList();
            var (save, delete) = EntryActions.Split(_parts, shares);
            var result = await store.SaveEntriesAsync(save, [.. delete]);
            if (!result.Succeeded)
            {
                Error = string.Join(Environment.NewLine, result.Errors.Select(e => translator[$"LedgerError_{e}"]));
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task JoinAsync()
    {
        if (!IsSplit || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var (save, delete) = EntryActions.Join(_parts);
            var result = await store.SaveEntriesAsync([save], [.. delete]);
            if (result.Succeeded)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}