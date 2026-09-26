using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Budget;

/// <summary>A budget limit with its status, ready for display (BUD-04, BUD-05).</summary>
public sealed record BudgetLine(
    string Name,
    Symbol Icon,
    Color IconColor,
    string SpentText,
    string LimitText,
    string StatusText,
    Color StatusColor,
    double Progress,
    Color ProgressColor);

/// <summary>
/// The monthly budget (UI-10). A missing budget is shown as "no budget", never as zero (BUD-01); a zero limit has no
/// percentage (BUD-05); overspending and negative net expense are shown as they are.
/// </summary>
public sealed partial class BudgetViewModel : ViewModelBase
{
    private static readonly Color Good = Color.FromArgb("#2E7D32");
    private static readonly Color Near = Color.FromArgb("#F9A825");
    private static readonly Color Over = Color.FromArgb("#B71C1C");

    private readonly FinanceStore _store;
    private readonly PlanStore _plans;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private Core.Budgets.Budget? _budget;
    private PeriodCalendar _calendar;
    private string _currency = Currencies.Euro.Code;
    private int _year;
    private int _month;

    public BudgetViewModel(FinanceStore store, PlanStore plans, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time)
    {
        _store = store;
        _plans = plans;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        PeriodText = string.Empty;
    }

    public ObservableCollection<BudgetLine> TotalLines { get; } = [];

    public ObservableCollection<BudgetLine> CategoryLines { get; } = [];

    [ObservableProperty]
    public partial string PeriodText { get; set; }

    [ObservableProperty]
    public partial string? CurrencyText { get; set; }

    [ObservableProperty]
    public partial string? ScopeText { get; set; }

    [ObservableProperty]
    public partial bool HasBudget { get; set; }

    [ObservableProperty]
    public partial bool CanCopyPrevious { get; set; }

    [ObservableProperty]
    public partial bool HasTotal { get; set; }

    [ObservableProperty]
    public partial bool HasCategoryLines { get; set; }

    [ObservableProperty]
    public partial bool ConfirmedOnly { get; set; }

    [ObservableProperty]
    public partial string? UnreviewedText { get; set; }

    [ObservableProperty]
    public partial string? PlannedText { get; set; }

    [ObservableProperty]
    public partial string? EquivalentText { get; set; }

    [ObservableProperty]
    public partial Symbol PreviousIcon { get; set; }

    [ObservableProperty]
    public partial Symbol NextIcon { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    partial void OnConfirmedOnlyChanged(bool value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        var settings = await _store.GetSettingsAsync();
        if (_year == 0)
        {
            _calendar = settings.BudgetCalendar;
            (_year, _month) = PeriodMath.MonthOf(Today, _calendar);
        }

        _currency = settings.ReportCurrencyCode;

        // Chevrons point in the reading direction (UX: direction-dependent icons mirror in RTL).
        PreviousIcon = _localization.IsRightToLeft ? Symbol.ChevronRight : Symbol.ChevronLeft;
        NextIcon = _localization.IsRightToLeft ? Symbol.ChevronLeft : Symbol.ChevronRight;

        var (from, to) = PeriodMath.MonthRange(_year, _month, _calendar);
        PeriodText = _dates.Format(from, DateFormatStyle.MonthYear);
        if ((_calendar == PeriodCalendar.Persian) != (_localization.CurrentCalendar == CalendarSystem.Persian))
        {
            // The budget months follow another calendar than the display: show the exact date range instead.
            PeriodText = $"{_dates.Format(from, DateFormatStyle.Short)} – {_dates.Format(to, DateFormatStyle.Short)}";
        }

        CurrencyText = _translator.Format("Budget_Currency", _currency, _translator[_calendar == PeriodCalendar.Persian ? "Calendar_Persian" : "Calendar_Gregorian"]);

        _budget = await _store.GetBudgetAsync(_year, _month, _calendar, _currency);
        HasBudget = _budget is not null;
        ScopeText = _budget is { AccountIds.Count: > 0 } ? _translator.Format("Budget_ScopeLimited", _budget.AccountIds.Count) : null;
        var (py, pm) = PeriodMath.Previous(_year, _month);
        CanCopyPrevious = _budget is null && await _store.GetBudgetAsync(py, pm, _calendar, _currency) is not null;

        var accounts = await _store.GetAccountsAsync();
        var entries = await _store.GetEntriesAsync(from, to);
        var categories = await _store.GetCategoriesAsync();
        var lookup = new CategoryLookup(categories, _translator);
        var culture = _localization.CurrentCulture;

        CategoryLines.Clear();
        TotalLines.Clear();
        if (_budget is { } budget)
        {
            var accountIds = budget.AccountIds.Count > 0 ? budget.AccountIds : null;
            if (budget.TotalLimit is { } limit)
            {
                var spent = BudgetCalculator.NetExpense(accounts, entries, from, to, _currency, accountIds, confirmedOnly: ConfirmedOnly);
                TotalLines.Add(Line(_translator["Budget_Total"], Symbol.Wallet, Good, new BudgetStatus(limit, spent), culture));
            }

            foreach (var categoryLimit in budget.CategoryLimits.OrderBy(l => lookup.Get(l.CategoryId)?.SortOrder ?? int.MaxValue))
            {
                var spent = BudgetCalculator.NetExpense(accounts, entries, from, to, _currency, accountIds, [categoryLimit.CategoryId], categories, ConfirmedOnly);
                CategoryLines.Add(Line(lookup.Name(categoryLimit.CategoryId), lookup.Icon(categoryLimit.CategoryId), lookup.Color(categoryLimit.CategoryId), new BudgetStatus(categoryLimit.Limit, spent), culture));
            }
        }

        HasTotal = TotalLines.Count > 0;
        HasCategoryLines = CategoryLines.Count > 0;

        var unreviewed = entries.Count(e => e.Review == ReviewState.Unreviewed && e.Kind is EntryKind.Expense or EntryKind.Refund);
        UnreviewedText = unreviewed > 0 && !ConfirmedOnly ? _translator.Format("Budget_IncludesUnreviewed", unreviewed) : null;

        // Plans of the month: real occurrences (BUD-10) and monthly shares of non-monthly plans (BUD-09).
        var schedules = await _plans.GetSchedulesAsync();
        var planned = BudgetPlanning.PlannedInPeriod(schedules, await _plans.GetStatesAsync(), accounts.ToDictionary(a => a.Id), from, to, _currency, Today);
        PlannedText = planned.Count == 0 ? null : _translator.Format("Budget_Planned", MoneyText.Format(planned.Total, _currency, culture), planned.Count)
            + (planned.UnknownCount > 0 ? " · " + _translator.Format("Budget_PlannedUnknown", planned.UnknownCount) : string.Empty);
        var equivalent = schedules
            .Where(s => s.State == Core.Plans.ScheduleState.Active && s.Kind == EntryKind.Expense && accounts.Any(a => a.Id == s.AccountId && a.CurrencyCode == _currency))
            .Select(BudgetPlanning.MonthlyEquivalent)
            .Sum(v => v ?? 0);
        EquivalentText = equivalent > 0 ? _translator.Format("Budget_Equivalent", MoneyText.Format(equivalent, _currency, culture)) : null;
    }

    private BudgetLine Line(string name, Symbol icon, Color iconColor, BudgetStatus status, CultureInfo culture)
    {
        string statusText;
        if (status.IsOver)
        {
            statusText = _translator.Format("Budget_Over", MoneyText.Format(-status.Remaining, _currency, culture));
        }
        else if (status.UsagePercent is { } percent)
        {
            statusText = _translator.Format("Budget_Left", MoneyText.Format(status.Remaining, _currency, culture), Math.Round(percent, 0).ToString(culture));
        }
        else
        {
            statusText = _translator["Budget_ZeroLimit"];
        }

        var color = status.Alert switch
        {
            BudgetAlert.Exceeded => Over,
            BudgetAlert.Near => Near,
            _ => Good,
        };

        var progress = status.Limit > 0 ? Math.Clamp((double)status.Spent / status.Limit, 0, 1) : status.Spent > 0 ? 1 : 0;
        return new BudgetLine(
            name,
            icon,
            iconColor,
            MoneyText.Format(status.Spent, _currency, culture),
            _translator.Format("Budget_Of", MoneyText.Format(status.Limit, _currency, culture)),
            statusText,
            status.IsOver ? Over : Color.FromArgb("#5F6368"),
            progress,
            color);
    }

    [RelayCommand]
    private Task PreviousAsync()
    {
        (_year, _month) = PeriodMath.Previous(_year, _month);
        return LoadAsync();
    }

    [RelayCommand]
    private Task NextAsync()
    {
        (_year, _month) = PeriodMath.Next(_year, _month);
        return LoadAsync();
    }

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync(AppShell.BudgetEditorRoute, new Dictionary<string, object>
    {
        ["year"] = _year,
        ["month"] = _month,
        ["calendar"] = _calendar,
        ["currency"] = _currency,
    });

    [RelayCommand]
    private async Task CopyPreviousAsync()
    {
        var (py, pm) = PeriodMath.Previous(_year, _month);
        if (await _store.GetBudgetAsync(py, pm, _calendar, _currency) is { } previous)
        {
            await _store.SaveBudgetAsync(BudgetPlanning.CopyTo(previous, _year, _month));
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task CopyToNextAsync()
    {
        if (_budget is null)
        {
            return;
        }

        var (ny, nm) = PeriodMath.Next(_year, _month);
        var (nextFrom, _) = PeriodMath.MonthRange(ny, nm, _calendar);
        var target = _dates.Format(nextFrom, DateFormatStyle.MonthYear);
        var existing = await _store.GetBudgetAsync(ny, nm, _calendar, _currency);
        var message = _translator.Format(existing is null ? "Budget_CopyMessage" : "Budget_CopyReplaceMessage", target);
        if (!await Shell.Current.DisplayAlertAsync(_translator["Budget_CopyNext"], message, _translator["Budget_Copy"], _translator["Common_Cancel"]))
        {
            return;
        }

        var copy = BudgetPlanning.CopyTo(_budget, ny, nm);
        if (existing is not null)
        {
            await _store.DeleteBudgetAsync(existing.Id);
        }

        await _store.SaveBudgetAsync(copy);
        (_year, _month) = (ny, nm);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_budget is null || !await Shell.Current.DisplayAlertAsync(_translator["Budget_Delete"], _translator["Budget_DeleteMessage"], _translator["Common_Delete"], _translator["Common_Cancel"]))
        {
            return;
        }

        await _store.DeleteBudgetAsync(_budget.Id);
        await LoadAsync();
    }
}
