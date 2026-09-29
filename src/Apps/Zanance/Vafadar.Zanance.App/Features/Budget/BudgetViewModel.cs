using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Budget;

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
    Color ProgressColor,
    string? CarryText = null);

/// <summary>A bill group of a flex budget (D-28): fixed bills or the monthly share of non-monthly bills.</summary>
public sealed record FlexLine(string Title, string Detail, Symbol Icon, Color IconColor, Color TileBackground, Color TileStroke);

/// <summary>
/// The monthly budget (UI-10). A missing budget is shown as "no budget", never as zero (BUD-01); a zero limit has no
/// percentage (BUD-05); overspending and negative net expense are shown as they are.
/// </summary>
public sealed partial class BudgetViewModel : ViewModelBase
{
    private static Color Good => Palette.Primary;
    private static Color Near => Palette.NearLimit;
    private static Color Over => Palette.ExpenseText;

    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly GoalStore _goals;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private Core.Budgets.Budget? _budget;
    private PeriodCalendar _calendar;
    private int _startDay = 1;
    private string _currency = Currencies.Euro.Code;
    private int _year;
    private int _month;
    private BudgetPeriod _period;
    private DateOnly _periodStart;
    private bool _refreshing;

    public BudgetViewModel(ZananceStore store, PlanStore plans, GoalStore goals, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time)
    {
        _store = store;
        _plans = plans;
        _goals = goals;
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

    // Envelopes (§10.3, BUD-11/12): money at hand that is not assigned to an envelope or a goal yet.
    [ObservableProperty]
    public partial bool IsEnvelopes { get; set; }

    [ObservableProperty]
    public partial string? UnassignedText { get; set; }

    [ObservableProperty]
    public partial Color? UnassignedColor { get; set; }

    [ObservableProperty]
    public partial string? EnvelopeNote { get; set; }

    [ObservableProperty]
    public partial string? CategoriesHeader { get; set; }

    public ObservableCollection<AmountLine> EnvelopeLines { get; } = [];

    // Flex (D-28): the bill groups and the month's whole plan; the total line is the flexible limit.
    public ObservableCollection<FlexLine> FlexLines { get; } = [];

    [ObservableProperty]
    public partial bool IsFlex { get; set; }

    [ObservableProperty]
    public partial string? FlexTotalText { get; set; }

    [ObservableProperty]
    public partial Symbol PreviousIcon { get; set; }

    [ObservableProperty]
    public partial Symbol NextIcon { get; set; }

    // Weekly and two-week budgets (§10.3) are an Advanced choice; Simple mode keeps months.
    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> PeriodNames { get; set; } = [];

    [ObservableProperty]
    public partial int PeriodIndex { get; set; }

    [ObservableProperty]
    public partial bool IsTwoWeeks { get; set; }

    [ObservableProperty]
    public partial DateOnly FortnightStart { get; set; }

    [ObservableProperty]
    public partial string PreviousLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NextLabel { get; set; } = string.Empty;

    partial void OnPeriodIndexChanged(int value)
    {
        if (_refreshing || !Enum.IsDefined((BudgetPeriod)value))
        {
            return;
        }

        _period = (BudgetPeriod)value;
        _periodStart = default;
        _ = Presentation.Failures.GuardAsync(LoadAsync);
    }

    // The day two-week periods repeat from (e.g. a payday); changing it shows the period of today on the new grid.
    async partial void OnFortnightStartChanged(DateOnly value)
    {
        if (_refreshing)
        {
            return;
        }

        await Presentation.Failures.GuardAsync(async () =>
        {
            var settings = await _store.GetSettingsAsync();
            settings.FortnightStart = value;
            await _store.SaveSettingsAsync(settings);
            _periodStart = default;
            await LoadAsync();
        });
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    partial void OnConfirmedOnlyChanged(bool value) => _ = LoadAsync();

    private bool _backFromEditor;

    /// <summary>
    /// Called when the page appears: a new visit (a tab switch, an alert, Home) shows the current month; coming back from
    /// the editor keeps the edited month.
    /// </summary>
    public void OnShown()
    {
        if (!_backFromEditor)
        {
            _year = 0;
            _periodStart = default;
        }

        _backFromEditor = false;
    }

    /// <summary>Switches the budget of the shown month between limits and envelopes (used by the snapshot walk-through).</summary>
    internal async Task SetMethodAsync(BudgetMethod method)
    {
        if (_budget is { } budget)
        {
            budget.Method = method;
            await _store.SaveBudgetAsync(budget);
            await LoadAsync();
        }
    }

    public async Task LoadAsync()
    {
        var settings = await _store.GetSettingsAsync();
        _startDay = settings.MonthStartDay;
        if (_year == 0)
        {
            _calendar = settings.BudgetCalendar;
            (_year, _month) = PeriodMath.MonthOf(Today, _calendar, _startDay);
        }

        _currency = settings.ReportCurrencyCode;
        IsAdvanced = settings.Mode == Core.Settings.ExperienceMode.Advanced;
        if (!IsAdvanced)
        {
            _period = BudgetPeriod.Month;
        }

        _refreshing = true;
        try
        {
            PeriodNames = [_translator["Budget_PeriodMonth"], _translator["Budget_PeriodWeek"], _translator["Budget_PeriodTwoWeeks"]];
            PeriodIndex = (int)_period;
            IsTwoWeeks = _period == BudgetPeriod.TwoWeeks;
            FortnightStart = settings.FortnightStart ?? BudgetPeriods.StartOf(BudgetPeriod.Week, Today, _localization.FirstDayOfWeek);
        }
        finally
        {
            _refreshing = false;
        }

        // Chevrons point in the reading direction (UX: direction-dependent icons mirror in RTL).
        PreviousIcon = _localization.IsRightToLeft ? Symbol.ChevronRight : Symbol.ChevronLeft;
        NextIcon = _localization.IsRightToLeft ? Symbol.ChevronLeft : Symbol.ChevronRight;
        PreviousLabel = _translator[_period == BudgetPeriod.Month ? "Budget_PreviousMonth" : "Budget_PreviousPeriod"];
        NextLabel = _translator[_period == BudgetPeriod.Month ? "Budget_NextMonth" : "Budget_NextPeriod"];

        DateOnly from, to;
        if (_period == BudgetPeriod.Month)
        {
            (from, to) = PeriodMath.MonthRange(_year, _month, _calendar, _startDay);
            PeriodText = _dates.Format(from, DateFormatStyle.MonthYear);
            if (_startDay > 1 || (_calendar == PeriodCalendar.Persian) != (_localization.CurrentCalendar == CalendarSystem.Persian))
            {
                // A month with its own start day, or budget months in another calendar than the display: show the exact range.
                PeriodText = $"{_dates.Format(from, DateFormatStyle.Short)} – {_dates.Format(to, DateFormatStyle.Short)}";
            }

            _budget = await _store.GetBudgetAsync(_year, _month, _calendar, _currency);
            var (py, pm) = PeriodMath.Previous(_year, _month);
            CanCopyPrevious = _budget is null && await _store.GetBudgetAsync(py, pm, _calendar, _currency) is not null;
        }
        else
        {
            if (_periodStart == default)
            {
                _periodStart = BudgetPeriods.StartOf(_period, Today, _localization.FirstDayOfWeek, settings.FortnightStart);
            }

            // A budget made before the week start was changed keeps its own dates.
            _budget = await _store.GetBudgetAsync(_period, _periodStart, _currency);
            if (_budget is not null)
            {
                _periodStart = _budget.PeriodStart;
            }

            (from, to) = BudgetPeriods.Range(_period, _periodStart);
            PeriodText = $"{_dates.Format(from, DateFormatStyle.Short)} – {_dates.Format(to, DateFormatStyle.Short)}";
            CanCopyPrevious = _budget is null && await _store.GetBudgetAsync(_period, _periodStart.AddDays(-1), _currency) is not null;
        }

        CurrencyText = _translator.Format("Budget_Currency", _currency, _translator[_calendar == PeriodCalendar.Persian ? "Calendar_Persian" : "Calendar_Gregorian"]);
        HasBudget = _budget is not null;
        ScopeText = _budget is { AccountIds.Count: > 0 } ? _translator.Format("Budget_ScopeLimited", _budget.AccountIds.Count) : null;

        var accounts = await _store.GetAccountsAsync();
        var entries = await _store.GetEntriesAsync(from, to);
        var categories = await _store.GetCategoriesAsync();
        var lookup = new CategoryLookup(categories, _translator);
        var culture = _localization.CurrentCulture;

        CategoryLines.Clear();
        TotalLines.Clear();
        EnvelopeLines.Clear();
        FlexLines.Clear();
        FlexTotalText = null;
        // Envelopes and flex are monthly methods; weekly budgets are read as limits.
        IsEnvelopes = _period == BudgetPeriod.Month && _budget?.Method == BudgetMethod.Envelopes;
        IsFlex = _period == BudgetPeriod.Month && _budget?.Method == BudgetMethod.Flex;
        CategoriesHeader = _translator[IsEnvelopes ? "Budget_MethodEnvelopes" : "Budget_Categories"];
        UnassignedText = EnvelopeNote = null;
        var envelopes = new List<BudgetStatus>();
        if (_budget is { } budget)
        {
            var accountIds = budget.AccountIds.Count > 0 ? budget.AccountIds : null;

            // Rollover (§10.3): the limits of this month plus what the previous months passed on.
            var carry = await _store.GetBudgetCarryAsync(budget);
            if (IsFlex)
            {
                await LoadFlexAsync(budget, carry.Total, accounts, entries, categories, from, to, culture);
            }
            else if (budget.TotalLimit is { } limit)
            {
                var spent = BudgetCalculator.NetExpense(accounts, entries, from, to, _currency, accountIds, confirmedOnly: ConfirmedOnly);
                TotalLines.Add(Line(_translator["Budget_Total"], Symbol.Wallet, Good, new BudgetStatus(limit + carry.Total, spent), culture) with { CarryText = Carry(carry.Total, limit, culture) });
            }

            foreach (var categoryLimit in budget.CategoryLimits.Where(_ => !IsFlex).OrderBy(l => lookup.Get(l.CategoryId)?.SortOrder ?? int.MaxValue))
            {
                var spent = BudgetCalculator.NetExpense(accounts, entries, from, to, _currency, accountIds, [categoryLimit.CategoryId], categories, ConfirmedOnly);
                var categoryCarry = carry.For(categoryLimit.CategoryId);
                envelopes.Add(new BudgetStatus(categoryLimit.Limit + categoryCarry, spent));
                CategoryLines.Add(Line(lookup.Name(categoryLimit.CategoryId), lookup.Icon(categoryLimit.CategoryId), lookup.Color(categoryLimit.CategoryId), new BudgetStatus(categoryLimit.Limit + categoryCarry, spent), culture)
                    with { CarryText = Carry(categoryCarry, categoryLimit.Limit, culture) });
            }
        }

        if (IsEnvelopes && _budget is { } envelopeBudget)
        {
            await LoadEnvelopesAsync(envelopeBudget, accounts, envelopes, from, to, culture);
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
        EquivalentText = equivalent > 0 && _period == BudgetPeriod.Month ? _translator.Format("Budget_Equivalent", MoneyText.Format(equivalent, _currency, culture)) : null;
    }

    // Flex (D-28): one limit for flexible spending; fixed bills expected from the plans, non-monthly bills with their
    // monthly share. Every amount is counted in one group only (BUD-12).
    private async Task LoadFlexAsync(Core.Budgets.Budget budget, long carry, IReadOnlyList<Account> accounts, IReadOnlyList<LedgerEntry> entries,
        IReadOnlyList<Core.Categories.Category> categories, DateOnly from, DateOnly to, CultureInfo culture)
    {
        var limit = (budget.TotalLimit ?? 0) + carry;
        var flex = FlexCalculator.Summarize(limit, accounts, entries, await _plans.GetSchedulesAsync(), await _plans.GetStatesAsync(), categories,
            from, to, _currency, Today, budget.AccountIds.Count > 0 ? budget.AccountIds : null, ConfirmedOnly);
        string Money(long value) => MoneyText.Format(value, _currency, culture);

        if (budget.TotalLimit is { } own)
        {
            TotalLines.Add(Line(_translator["Flex_Flexible"], Symbol.Wallet, Good, flex.Flexible, culture) with { CarryText = Carry(carry, own, culture) });
        }

        var fixedDetail = flex.Fixed.Planned > 0
            ? _translator.Format("Flex_FixedDetail", Money(flex.Fixed.Spent), Money(flex.Fixed.Planned), Money(flex.Fixed.Open))
            : _translator.Format("Flex_Paid", Money(flex.Fixed.Spent));
        FlexLines.Add(new FlexLine(_translator["Flex_Fixed"], fixedDetail + Unknown(flex.Fixed.UnknownCount), Symbol.CalendarClock, Palette.PlanText, Palette.PlanBackground, Palette.PlanLine));
        FlexLines.Add(new FlexLine(_translator["Flex_NonMonthly"],
            _translator.Format("Flex_NonMonthlyDetail", Money(flex.NonMonthly.Planned), Money(flex.NonMonthly.Spent)) + Unknown(flex.NonMonthly.UnknownCount),
            Symbol.Savings, Palette.SavingText, Palette.SavingBackground, Palette.SavingLine));
        FlexTotalText = _translator.Format("Flex_Total", Money(flex.Total));
    }

    private string Unknown(int count) => count > 0 ? " · " + _translator.Format("Budget_PlannedUnknown", count) : string.Empty;

    // The balance at hand is today's, so the summary is shown for the current month only.
    private async Task LoadEnvelopesAsync(Core.Budgets.Budget budget, IReadOnlyList<Account> accounts, List<BudgetStatus> envelopes, DateOnly from, DateOnly to, CultureInfo culture)
    {
        var today = Today;
        if (today < from || today > to)
        {
            EnvelopeNote = _translator["Envelope_CurrentMonthOnly"];
            return;
        }

        var cash = LedgerCalculator.InScope(accounts, budget.AccountIds.Count > 0 ? budget.AccountIds : null)
            .Where(a => !a.IsArchived && !a.Type.IsOutsideCash() && string.Equals(a.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var allEntries = await _store.GetEntriesAsync();
        var balances = cash.ToDictionary(a => a.Id, a => LedgerCalculator.Balance(a, allEntries, today));
        var earmarked = Core.Goals.GoalCalculator.Accounts(await _goals.GetGoalsAsync(), await _goals.GetAllocationsAsync(), balances)
            .Sum(e => Math.Clamp(e.Earmarked, 0, Math.Max(0, e.Balance)));
        var summary = EnvelopeCalculator.Summarize(balances.Values.Sum(), earmarked, envelopes);

        string Money(long value) => MoneyText.Format(value, _currency, culture);
        UnassignedText = Money(summary.Unassigned);
        UnassignedColor = summary.IsOverAssigned ? Over : Good;
        EnvelopeLines.Add(new AmountLine(_translator["Envelope_Available"], Money(summary.Available), false));
        if (summary.Earmarked > 0)
        {
            EnvelopeLines.Add(new AmountLine(_translator["Envelope_ForGoals"], Money(-summary.Earmarked), false));
        }

        EnvelopeLines.Add(new AmountLine(_translator["Envelope_InEnvelopes"], Money(-summary.InEnvelopes), false));
        EnvelopeNote = summary.IsOverAssigned ? _translator["Envelope_OverAssigned"]
            : summary.Overspent > 0 ? _translator.Format("Envelope_Overspent", Money(summary.Overspent))
            : null;
    }

    private string? Carry(long carry, long limit, CultureInfo culture) => carry == 0
        ? null
        : _translator.Format(carry > 0 ? "Rollover_Carried" : "Rollover_Deducted", MoneyText.Format(Math.Abs(carry), _currency, culture), MoneyText.Format(limit, _currency, culture));

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
            status.IsOver ? Over : Palette.SecondaryText,
            progress,
            color);
    }

    [RelayCommand]
    private Task PreviousAsync()
    {
        Move(-1);
        return LoadAsync();
    }

    [RelayCommand]
    private Task NextAsync()
    {
        Move(1);
        return LoadAsync();
    }

    private void Move(int count)
    {
        if (_period == BudgetPeriod.Month)
        {
            (_year, _month) = count < 0 ? PeriodMath.Previous(_year, _month) : PeriodMath.Next(_year, _month);
        }
        else
        {
            _periodStart = BudgetPeriods.Step(_period, _periodStart, count);
        }
    }

    [RelayCommand]
    private Task EditAsync()
    {
        _backFromEditor = true;
        return Shell.Current.GoToAsync(AppShell.BudgetEditorRoute, new Dictionary<string, object>
        {
            ["year"] = _year,
            ["month"] = _month,
            ["calendar"] = _calendar,
            ["currency"] = _currency,
            ["period"] = _period,
            ["start"] = _periodStart,
        });
    }

    [RelayCommand]
    private async Task CopyPreviousAsync()
    {
        if (_period != BudgetPeriod.Month)
        {
            if (await _store.GetBudgetAsync(_period, _periodStart.AddDays(-1), _currency) is { } previousWeek)
            {
                await _store.SaveBudgetAsync(BudgetPlanning.CopyTo(previousWeek, _periodStart));
                await LoadAsync();
            }

            return;
        }

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
        var nextStart = _period == BudgetPeriod.Month ? default : BudgetPeriods.Step(_period, _periodStart, 1);
        string target;
        Core.Budgets.Budget? existing;
        if (_period == BudgetPeriod.Month)
        {
            var (nextFrom, _) = PeriodMath.MonthRange(ny, nm, _calendar, _startDay);
            target = _dates.Format(nextFrom, DateFormatStyle.MonthYear);
            existing = await _store.GetBudgetAsync(ny, nm, _calendar, _currency);
        }
        else
        {
            var (first, last) = BudgetPeriods.Range(_period, nextStart);
            target = $"{_dates.Format(first, DateFormatStyle.Short)} – {_dates.Format(last, DateFormatStyle.Short)}";
            existing = await _store.GetBudgetAsync(_period, nextStart, _currency);
        }

        var message = _translator.Format(existing is null ? "Budget_CopyMessage" : "Budget_CopyReplaceMessage", target);
        if (!await Shell.Current.DisplayAlertAsync(_translator["Budget_CopyNext"], message, _translator["Budget_Copy"], _translator["Common_Cancel"]))
        {
            return;
        }

        var copy = _period == BudgetPeriod.Month ? BudgetPlanning.CopyTo(_budget, ny, nm) : BudgetPlanning.CopyTo(_budget, nextStart);
        if (existing is not null)
        {
            await _store.DeleteBudgetAsync(existing.Id);
        }

        await _store.SaveBudgetAsync(copy);
        if (_period == BudgetPeriod.Month)
        {
            (_year, _month) = (ny, nm);
        }
        else
        {
            _periodStart = nextStart;
        }

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
