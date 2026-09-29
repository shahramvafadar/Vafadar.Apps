using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Budget;

/// <summary>A category limit being edited; an empty text means "no limit for this category".</summary>
public sealed partial class LimitInput(Guid categoryId, string name, Symbol icon, Color color) : ObservableObject
{
    public Guid CategoryId { get; } = categoryId;

    public string Name { get; } = name;

    public Symbol Icon { get; } = icon;

    public Color Color { get; } = color;

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the suggested limit in minor units (§10.3), or <see langword="null"/> without history.</summary>
    public long? Suggested { get; set; }

    /// <summary>Gets or sets the average spending shown under the limit, e.g. "Average 118.40".</summary>
    public string? SuggestionText { get; set; }
}

/// <summary>An account that the budget may cover (BUD-03).</summary>
public sealed partial class ScopeAccount(Guid id, string name) : ObservableObject
{
    public Guid Id { get; } = id;

    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsIncluded { get; set; } = true;
}

/// <summary>Creates or edits the budget of one month (BUD-01, BUD-02). Other months are never changed (BUD-07).</summary>
public sealed partial class BudgetEditorViewModel(ZananceStore store, Translator translator, ILocalizationService localization) : ViewModelBase, IQueryAttributable
{
    private Core.Budgets.Budget? _budget;
    private int _year;
    private int _month;
    private PeriodCalendar _calendar;
    private string _currency = Currencies.Euro.Code;

    public ObservableCollection<LimitInput> Limits { get; } = [];

    public ObservableCollection<ScopeAccount> ScopeAccounts { get; } = [];

    [ObservableProperty]
    public partial string TotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool AlertsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string? CurrencyCode { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    [ObservableProperty]
    public partial string? HiddenLimitsText { get; set; }

    // Suggestions for adjusting limits (§10.3): shown only, taken over on request.
    [ObservableProperty]
    public partial string? TotalSuggestionText { get; set; }

    [ObservableProperty]
    public partial bool HasCategorySuggestions { get; set; }

    private long? _totalSuggestion;
    private bool _loaded;

    // Rollover (§10.3) is an Advanced option; in Simple mode an active rollover stays and is summarised (UX-02).
    [ObservableProperty]
    public partial IReadOnlyList<string> RolloverNames { get; set; } = [];

    [ObservableProperty]
    public partial int RolloverIndex { get; set; }

    // Envelopes (§10.3) are an optional Advanced way of reading the same limits (BUD-12).
    [ObservableProperty]
    public partial IReadOnlyList<string> MethodNames { get; set; } = [];

    [ObservableProperty]
    public partial int MethodIndex { get; set; }

    /// <summary>Gets a value indicating whether the flex method is chosen: the limit is then for flexible spending only (D-28).</summary>
    public bool IsFlex => MethodIndex == (int)BudgetMethod.Flex;

    /// <summary>Gets a value indicating whether category limits can be edited (Advanced, not with flex).</summary>
    public bool ShowCategoryLimits => IsAdvanced && !IsFlex;

    /// <summary>Gets a value indicating whether empty category limits can be filled with suggestions.</summary>
    public bool ShowFillSuggestions => ShowCategoryLimits && HasCategorySuggestions;

    partial void OnHasCategorySuggestionsChanged(bool value) => OnPropertyChanged(nameof(ShowFillSuggestions));

    partial void OnMethodIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsFlex));
        OnPropertyChanged(nameof(ShowCategoryLimits));
        OnPropertyChanged(nameof(ShowFillSuggestions));

        // With flex the overall suggestion covers flexible spending only.
        if (_loaded)
        {
            _ = Presentation.Failures.GuardAsync(LoadSuggestionsAsync);
        }
    }

    partial void OnIsAdvancedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowCategoryLimits));
        OnPropertyChanged(nameof(ShowFillSuggestions));
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));

    private async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        _loaded = false;
        _year = query.TryGetValue("year", out var year) && year is int y ? y : 0;
        _month = query.TryGetValue("month", out var month) && month is int m ? m : 0;
        _calendar = query.TryGetValue("calendar", out var calendar) && calendar is PeriodCalendar c ? c : PeriodCalendar.Gregorian;
        _currency = query.TryGetValue("currency", out var currency) && currency is string code ? code : Currencies.Euro.Code;
        CurrencyCode = _currency;
        query.Clear();

        _budget = await store.GetBudgetAsync(_year, _month, _calendar, _currency);
        IsAdvanced = (await store.GetSettingsAsync()).Mode == Core.Settings.ExperienceMode.Advanced;

        // Category limits are an Advanced option; in Simple they stay saved and are summarised (BUD-02, UX-02).
        var limitCount = _budget?.CategoryLimits.Count ?? 0;
        HiddenLimitsText = !IsAdvanced && limitCount > 0 ? translator.Format("Budget_HiddenLimits", limitCount) : null;
        var culture = localization.CurrentCulture;
        TotalText = _budget?.TotalLimit is { } total ? MoneyText.ForInput(total, _currency, culture) : string.Empty;
        AlertsEnabled = _budget?.AlertsEnabled ?? true;
        RolloverNames = [translator["Rollover_None"], translator["Rollover_Surplus"], translator["Rollover_Both"]];
        RolloverIndex = (int)(_budget?.Rollover ?? BudgetRollover.None);
        MethodNames = [translator["Budget_MethodLimits"], translator["Budget_MethodEnvelopes"], translator["Budget_MethodFlex"]];
        MethodIndex = (int)(_budget?.Method ?? BudgetMethod.Limits);
        if (!IsAdvanced && RolloverIndex != 0)
        {
            HiddenLimitsText = string.Join(Environment.NewLine, new[] { HiddenLimitsText, translator[$"Rollover_Active_{(BudgetRollover)RolloverIndex}"] }.Where(t => t is not null));
        }

        // Default scope: accounts in totals with the budget currency; an explicit list narrows it (BUD-03).
        ScopeAccounts.Clear();
        foreach (var account in (await store.GetAccountsAsync(includeArchived: false)).Where(a => a.IncludeInTotals && a.CurrencyCode == _currency))
        {
            ScopeAccounts.Add(new ScopeAccount(account.Id, account.Name)
            {
                IsIncluded = _budget is not { AccountIds.Count: > 0 } || _budget.AccountIds.Contains(account.Id),
            });
        }

        var lookup = new CategoryLookup(await store.GetCategoriesAsync(), translator);
        Limits.Clear();
        foreach (var category in lookup.All.Where(c => c.Kind == CategoryKind.Expense && c.ParentId is null && !c.IsArchived).OrderBy(c => c.SortOrder))
        {
            var input = new LimitInput(category.Id, CategoryLookup.NameOf(category, translator), lookup.Icon(category.Id), lookup.Color(category.Id));
            if (_budget?.CategoryLimits.FirstOrDefault(l => l.CategoryId == category.Id) is { } limit)
            {
                input.Text = MoneyText.ForInput(limit.Limit, _currency, culture);
            }

            Limits.Add(input);
        }

        await LoadSuggestionsAsync();
        _loaded = true;
    }

    // The average of the last months in the same scope and financial month as the budget page (§10.3).
    private async Task LoadSuggestionsAsync()
    {
        var settings = await store.GetSettingsAsync();
        var (first, _) = PeriodMath.MonthRange(_year, _month, _calendar, settings.MonthStartDay);
        var (py, pm) = (_year, _month);
        for (var i = 0; i < BudgetSuggestions.Months; i++)
        {
            (py, pm) = PeriodMath.Previous(py, pm);
        }

        var from = PeriodMath.MonthRange(py, pm, _calendar, settings.MonthStartDay).First;
        var suggestion = BudgetSuggestions.Suggest(
            await store.GetAccountsAsync(),
            await store.GetEntriesAsync(from, first.AddDays(-1)),
            await store.GetCategoriesAsync(),
            _year,
            _month,
            _calendar,
            settings.MonthStartDay,
            _currency,
            _budget?.AccountIds,
            flexibleOnly: IsFlex);

        var culture = localization.CurrentCulture;
        _totalSuggestion = suggestion.Total?.Suggested;
        TotalSuggestionText = suggestion.Total is { } total
            ? translator.Format(total.Months == 1 ? "Budget_SuggestionOne" : "Budget_Suggestion", MoneyText.Format(total.Suggested, _currency, culture), total.Months)
            : null;
        foreach (var input in Limits)
        {
            if (suggestion.Categories.TryGetValue(input.CategoryId, out var category))
            {
                input.Suggested = category.Suggested;
                input.SuggestionText = translator.Format("Budget_CategoryAverage", MoneyText.Format(category.Average, _currency, culture));
            }
        }

        HasCategorySuggestions = Limits.Any(l => l.Suggested is not null);
    }

    [RelayCommand]
    private void UseTotalSuggestion()
    {
        if (_totalSuggestion is { } total)
        {
            TotalText = MoneyText.ForInput(total, _currency, localization.CurrentCulture);
        }
    }

    // Fills only empty category limits; limits the user set stay as they are.
    [RelayCommand]
    private void FillSuggestions()
    {
        var culture = localization.CurrentCulture;
        foreach (var input in Limits.Where(l => l.Suggested is not null && string.IsNullOrWhiteSpace(l.Text)))
        {
            input.Text = MoneyText.ForInput(input.Suggested!.Value, _currency, culture);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        Error = null;
        var culture = localization.CurrentCulture;
        var currency = Currencies.TryGet(_currency, out var known) ? known : Currencies.Euro;

        long? total = null;
        if (!string.IsNullOrWhiteSpace(TotalText))
        {
            if (!MoneyText.TryParse(TotalText, currency, culture, out var parsed) || parsed < 0)
            {
                Error = translator["Amount_Invalid"];
                return;
            }

            total = parsed;
        }

        var limits = new List<BudgetCategoryLimit>();
        foreach (var input in Limits.Where(l => !string.IsNullOrWhiteSpace(l.Text)))
        {
            if (!MoneyText.TryParse(input.Text, currency, culture, out var limit) || limit < 0)
            {
                Error = $"{input.Name}: {translator["Amount_Invalid"]}";
                return;
            }

            limits.Add(new BudgetCategoryLimit { CategoryId = input.CategoryId, Limit = limit });
        }

        if (total is null && limits.Count == 0)
        {
            Error = translator["Budget_NothingToSave"];
            return;
        }

        var budget = _budget ?? new Core.Budgets.Budget { Year = _year, Month = _month, Calendar = _calendar, CurrencyCode = _currency };
        budget.TotalLimit = total;
        budget.CategoryLimits = limits;
        budget.AlertsEnabled = AlertsEnabled;
        budget.Rollover = (BudgetRollover)Math.Clamp(RolloverIndex, 0, 2);
        budget.Method = (BudgetMethod)Math.Clamp(MethodIndex, 0, 2);
        budget.AccountIds = ScopeAccounts.All(a => a.IsIncluded) ? [] : [.. ScopeAccounts.Where(a => a.IsIncluded).Select(a => a.Id)];
        await store.SaveBudgetAsync(budget);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
