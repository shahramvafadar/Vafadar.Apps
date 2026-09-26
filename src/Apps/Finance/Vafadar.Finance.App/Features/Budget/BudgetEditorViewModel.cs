using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Budget;

/// <summary>A category limit being edited; an empty text means "no limit for this category".</summary>
public sealed partial class LimitInput(Guid categoryId, string name, Symbol icon, Color color) : ObservableObject
{
    public Guid CategoryId { get; } = categoryId;

    public string Name { get; } = name;

    public Symbol Icon { get; } = icon;

    public Color Color { get; } = color;

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;
}

/// <summary>Creates or edits the budget of one month (BUD-01, BUD-02). Other months are never changed (BUD-07).</summary>
public sealed partial class BudgetEditorViewModel(FinanceStore store, Translator translator, ILocalizationService localization) : ViewModelBase, IQueryAttributable
{
    private Core.Budgets.Budget? _budget;
    private int _year;
    private int _month;
    private PeriodCalendar _calendar;
    private string _currency = Currencies.Euro.Code;

    public ObservableCollection<LimitInput> Limits { get; } = [];

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

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
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
            if (!MoneyAmount.TryParse(TotalText, currency, culture, out var parsed) || parsed < 0)
            {
                Error = translator["Amount_Invalid"];
                return;
            }

            total = parsed;
        }

        var limits = new List<BudgetCategoryLimit>();
        foreach (var input in Limits.Where(l => !string.IsNullOrWhiteSpace(l.Text)))
        {
            if (!MoneyAmount.TryParse(input.Text, currency, culture, out var limit) || limit < 0)
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
        await store.SaveBudgetAsync(budget);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
