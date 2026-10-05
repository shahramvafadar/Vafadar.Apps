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
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Budget;

/// <summary>A category limit being edited; an empty text means "no limit for this category".</summary>
public sealed partial class LimitInput(Guid categoryId, string name, Symbol icon, Color color) : ObservableObject
{
    public Guid CategoryId { get; } = categoryId;

    public string Name { get; } = name;

    public Symbol Icon { get; } = icon;

    /// <summary>Gets or sets the category color in the current theme (set again when the theme changes).</summary>
    [ObservableProperty]
    public partial Color Color { get; set; } = color;

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
public sealed partial class BudgetEditorViewModel(ZananceStore store, Translator translator, ILocalizationService localization) : ViewModelBase, IQueryAttributable, Presentation.IUnsavedChanges, Presentation.IThemeAware
{
    private Core.Budgets.Budget? _budget;
    private int _year;
    private int _month;
    private PeriodCalendar _calendar;
    private BudgetPeriod _period;
    private DateOnly _periodStart;
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

    /// <summary>Gets a value indicating whether a weekly or two-week budget is edited (limits only).</summary>
    [ObservableProperty]
    public partial bool IsWeekly { get; set; }

    /// <summary>Gets a value indicating whether the method can be chosen (monthly budgets in Advanced).</summary>
    public bool ShowMethods => IsAdvanced && !IsWeekly;

    partial void OnIsWeeklyChanged(bool value) => OnPropertyChanged(nameof(ShowMethods));

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
        OnPropertyChanged(nameof(ShowMethods));
        OnPropertyChanged(nameof(ShowCategoryLimits));
        OnPropertyChanged(nameof(ShowFillSuggestions));
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));

    private CategoryLookup? _lookup;

    // The limits keep what was typed; only the category colors follow the theme.
    Task Presentation.IThemeAware.RefreshThemeAsync()
    {
        if (_lookup is { } lookup)
        {
            foreach (var limit in Limits)
            {
                limit.Color = lookup.Color(limit.CategoryId);
            }
        }

        return Task.CompletedTask;
    }

    private async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        _loaded = false;
        _year = query.TryGetValue("year", out var year) && year is int y ? y : 0;
        _month = query.TryGetValue("month", out var month) && month is int m ? m : 0;
        _calendar = query.TryGetValue("calendar", out var calendar) && calendar is PeriodCalendar c ? c : PeriodCalendar.Gregorian;
        _currency = query.TryGetValue("currency", out var currency) && currency is string code ? code : Currencies.Euro.Code;
        _period = query.TryGetValue("period", out var period) && period is BudgetPeriod p ? p : BudgetPeriod.Month;
        _periodStart = query.TryGetValue("start", out var start) && start is DateOnly s ? s : default;
        IsWeekly = _period != BudgetPeriod.Month;
        CurrencyCode = _currency;
        query.Clear();

        _budget = IsWeekly
            ? await store.GetBudgetAsync(_period, _periodStart, _currency)
            : await store.GetBudgetAsync(_year, _month, _calendar, _currency);
        IsAdvanced = (await store.GetSettingsAsync()).Shows(Feature.BudgetOptions);

        // Category limits are an Advanced option; in Simple they stay saved and are summarised (BUD-02, UX-02).
        var limitCount = _budget?.CategoryLimits.Count ?? 0;
        HiddenLimitsText = !IsAdvanced && limitCount > 0 ? translator.Format("Budget_HiddenLimits", limitCount) : null;
        var culture = localization.CurrentCulture;
        TotalText = _budget?.TotalLimit is { } total ? MoneyText.ForInput(total, _currency, culture) : string.Empty;
        AlertsEnabled = _budget?.AlertsEnabled ?? true;
        RolloverNames = [translator["Rollover_None"], translator["Rollover_Surplus"], translator["Rollover_Both"]];
        RolloverIndex = (int)(_budget?.Rollover ?? BudgetRollover.None);
        MethodNames = [translator["Budget_MethodLimits"], translator["Budget_MethodEnvelopes"], translator["Budget_MethodFlex"]];
        MethodIndex = IsWeekly ? (int)BudgetMethod.Limits : (int)(_budget?.Method ?? BudgetMethod.Limits);
        if (!IsAdvanced && RolloverIndex != 0)
        {
            HiddenLimitsText = string.Join(Environment.NewLine, new[] { HiddenLimitsText, translator[$"Rollover_Active_{(BudgetRollover)RolloverIndex}"] }.Where(t => t is not null));
        }

        // Envelopes and flex stay active in Simple and are named, so the budget reads the same in both modes (ZEX-S0502).
        if (!IsAdvanced && MethodIndex != (int)BudgetMethod.Limits)
        {
            HiddenLimitsText = string.Join(Environment.NewLine, new[] { HiddenLimitsText, translator.Format("Budget_MethodActive", MethodNames[MethodIndex]) }.Where(t => t is not null));
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

        var lookup = _lookup = new CategoryLookup(await store.GetCategoriesAsync(), translator);
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
        _snapshot = Snapshot();
    }

    // The average of the last months in the same scope and financial month as the budget page (§10.3).
    private async Task LoadSuggestionsAsync()
    {
        var settings = await store.GetSettingsAsync();
        var periods = BudgetPeriods.Before(_period, _year, _month, _periodStart, _calendar, settings.MonthStartDay, BudgetSuggestions.PeriodsFor(_period));
        var suggestion = BudgetSuggestions.Suggest(
            await store.GetAccountsAsync(),
            await store.GetEntriesAsync(periods[^1].First, periods[0].Last),
            await store.GetCategoriesAsync(),
            periods,
            _currency,
            _budget?.AccountIds,
            flexibleOnly: IsFlex);

        var culture = localization.CurrentCulture;
        _totalSuggestion = suggestion.Total?.Suggested;
        var key = (_period, suggestion.Total?.Months) switch
        {
            (BudgetPeriod.Month, 1) => "Budget_SuggestionOne",
            (BudgetPeriod.Month, _) => "Budget_Suggestion",
            (_, 1) => "Budget_SuggestionOnePeriod",
            (BudgetPeriod.Week, _) => "Budget_SuggestionWeeks",
            _ => "Budget_SuggestionFortnights",
        };
        TotalSuggestionText = suggestion.Total is { } total
            ? translator.Format(key, MoneyText.Format(total.Suggested, _currency, culture), total.Months)
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
        // A second tap while saving would add a new budget twice.
        if (IsBusy)
        {
            return;
        }

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

        // An empty list means "all accounts": with every account unticked it would silently cover them all.
        if (ScopeAccounts.Count > 0 && ScopeAccounts.All(a => !a.IsIncluded))
        {
            Error = translator["Budget_NoAccountChosen"];
            return;
        }

        var budget = _budget ?? (IsWeekly
            ? new Core.Budgets.Budget { Period = _period, PeriodStart = _periodStart, Calendar = _calendar, CurrencyCode = _currency }
            : new Core.Budgets.Budget { Year = _year, Month = _month, Calendar = _calendar, CurrencyCode = _currency });
        budget.TotalLimit = total;
        budget.CategoryLimits = limits;
        budget.AlertsEnabled = AlertsEnabled;
        budget.Rollover = (BudgetRollover)Math.Clamp(RolloverIndex, 0, 2);
        budget.Method = IsWeekly ? BudgetMethod.Limits : (BudgetMethod)Math.Clamp(MethodIndex, 0, 2);
        budget.AccountIds = ScopeAccounts.All(a => a.IsIncluded) ? [] : [.. ScopeAccounts.Where(a => a.IsIncluded).Select(a => a.Id)];
        IsBusy = true;
        try
        {
            await store.SaveBudgetAsync(budget);

            // From now on the page edits the saved budget, so a later save never adds a second one.
            _budget = budget;
        }
        finally
        {
            IsBusy = false;
        }

        await Presentation.Failures.GuardAsync(() => Shell.Current.GoToAsync(".."));
    }

    [RelayCommand]
    private Task CancelAsync() => Presentation.UnsavedChanges.LeaveAsync(this);

    // The input as it was loaded or saved; leaving with a change asks first (CR12).
    private string? _snapshot;

    /// <inheritdoc />
    public bool IsDirty => _snapshot is not null && Snapshot() != _snapshot;

    private string Snapshot() => Presentation.UnsavedChanges.Fingerprint(
        TotalText, AlertsEnabled, RolloverIndex, MethodIndex,
        string.Join(',', Limits.Select(l => l.Text)), string.Join(',', ScopeAccounts.Select(a => a.IsIncluded)));
}
