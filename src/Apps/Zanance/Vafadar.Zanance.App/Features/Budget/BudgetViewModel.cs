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
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Interaction;

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
public sealed partial class BudgetViewModel : ViewModelBase, Presentation.IThemeAware, IQueryAttributable
{
    // Traffic light of the palette (D-27): within the limit is a positive result (green), then amber, then red. Blue is
    // the action colour and never shows the state of money.
    private static Color Good => Palette.IncomeText;
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
    private long _pageRevision;
    private bool _visible = true;
    private Core.Reminders.BudgetReminderLink? _requestedReminder;
    private string? _visitCurrency;
    private string? _budgetScope;
    private BudgetEditorReview? _review;

    /// <summary>Gets the actual reviewed original view commands; financial presentation is never filtered by them.</summary>
    public BudgetActionsViewModel Actions { get; }
    /// <summary>Gets the same covered/busy state for period navigation and original view actions.</summary>
    public bool CanNavigate => Actions.CanInteract;

    /// <summary>Retains a validated notification destination until the actual page reload resolves its profile again.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("budgetReminder", out var value) && value is Core.Reminders.BudgetReminderLink reminder)
            _requestedReminder = reminder;
    }

    private readonly SemaphoreSlim _loading = new(1, 1);

    public BudgetViewModel(ZananceStore store, PlanStore plans, GoalStore goals, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time, IAppInteraction interaction)
    {
        _store = store;
        _plans = plans;
        _goals = goals;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        Actions = new(store, translator, dates, interaction, ReloadAfterActionAsync);
        Actions.EditorOpening += (_, _) => _backFromEditor = true;
        Actions.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Actions.CanInteract))
            {
                OnPropertyChanged(nameof(CanNavigate)); PreviousCommand.NotifyCanExecuteChanged(); NextCommand.NotifyCanExecuteChanged();
            }
        };
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

    // Weekly and two-week budgets (§10.3) are created in Advanced; Simple shows the period choice as soon as such a
    // budget exists, so it is never recomputed as a month (ZEX-S0502, SA17).
    [ObservableProperty]
    public partial bool ShowsPeriods { get; set; }

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
        if (_refreshing || !Actions.CanInteract || !Enum.IsDefined((BudgetPeriod)value))
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
        if (_refreshing || !Actions.CanInteract)
        {
            return;
        }

        await Presentation.Failures.GuardAsync(async () =>
        {
            await _store.UpdateSettingsAsync(s => s.FortnightStart = value);
            _periodStart = default;
            await LoadAsync();
        });
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    partial void OnConfirmedOnlyChanged(bool value)
    {
        if (!_refreshing && Actions.CanInteract) _ = Presentation.Failures.GuardAsync(LoadAsync);
    }

    private bool _backFromEditor;

    /// <summary>
    /// Called when the page appears: a new visit (a tab switch, an alert, Home) shows the current month; coming back from
    /// the editor keeps the edited month.
    /// </summary>
    public void OnShown()
    {
        _visible = true; _pageRevision++;
        if (!_backFromEditor)
        {
            _year = 0;
            _periodStart = default;
            _visitCurrency = null;
        }

        _backFromEditor = false;
    }

    /// <summary>Retires original commands, confirmations and publication when the native page disappears.</summary>
    public void OnHidden()
    {
        _visible = false; _pageRevision++; Actions.Retire();
    }

    /// <summary>Switches the budget of the shown month between limits and envelopes (used by the snapshot walk-through).</summary>
    internal async Task SetMethodAsync(BudgetMethod method)
    {
        if (_budget is { } budget)
        {
            budget.Method = method;
            await _store.SaveBudgetAsync(budget, review: _review);
            await LoadAsync();
        }
    }

    /// <summary>Gets the budget currencies to switch between (only shown with budgets in several currencies, ZEX-P02).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> CurrencyChoices { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedCurrency { get; set; }

    [ObservableProperty]
    public partial bool HasCurrencyChoices { get; set; }

    // The choice is remembered per profile and also decides the budget shown on Home.
    async partial void OnSelectedCurrencyChanged(string? value)
    {
        if (_refreshing || !Actions.CanInteract || string.IsNullOrEmpty(value) || value == _currency)
        {
            return;
        }

        // An async void handler: every failure is shown instead of ending the app.
        await Presentation.Failures.GuardAsync(async () =>
        {
            await _store.UpdateSettingsAsync(s => s.HomeBudgetCurrencyCode = value);
            _visitCurrency = value;
            await LoadAsync();
        });
    }

    /// <summary>Completes a native reminder navigation, including an already visible root tab, under the existing load gate.</summary>
    public async Task ShowReminderAsync(Core.Reminders.BudgetReminderLink reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        var pageRevision = _pageRevision;
        await _loading.WaitAsync();
        try
        {
            if (!_visible || pageRevision != _pageRevision) return;
            Actions.BeginLoad();
            // Publish the pending target only inside the gate; an older appearing reload must not consume a newer request.
            _requestedReminder = reminder;
            await LoadCoreAsync();
        }
        catch (Exception error) when (error is not OutOfMemoryException && (!_visible || pageRevision != _pageRevision)) { }
        catch { Actions.FailLoad(); throw; }
        finally { _loading.Release(); }
    }

    /// <summary>Loads the shown period.</summary>
    /// <remarks>
    /// Appearing, the period and currency switches and the confirmed-only switch can ask at the same moment; two loads
    /// filling the same lists would mix their lines, so loads run one after the other (as on Home, CR07-05).
    /// </remarks>
    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => _visible ? LoadAsync() : Task.CompletedTask;

    public async Task LoadAsync()
    {
        if (!_visible || Actions.IsBusy) return;
        await ReloadAfterActionAsync(null);
    }

    private async Task ReloadAfterActionAsync(BudgetVisit? visit)
    {
        var pageRevision = _pageRevision;
        await _loading.WaitAsync();
        try
        {
            if (!_visible || pageRevision != _pageRevision) return;
            if (visit is not null)
            {
                _year = visit.Year; _month = visit.Month; _calendar = visit.Calendar;
                _period = visit.Period; _periodStart = visit.Start; _visitCurrency = visit.Currency;
            }
            Actions.BeginLoad();
            await LoadCoreAsync();
        }
        catch (Exception error) when (error is not OutOfMemoryException && (!_visible || pageRevision != _pageRevision)) { }
        catch { Actions.FailLoad(); throw; }
        finally
        {
            _loading.Release();
        }
    }

    private async Task LoadCoreAsync()
    {
        var pageRevision = _pageRevision;
        void RequireVisible()
        {
            if (!_visible || pageRevision != _pageRevision) throw new OperationCanceledException("The original budget page retired.");
        }
        RequireVisible();
        var destination = _requestedReminder;
        var original = destination is null ? null : await _store.ResolveBudgetReminderAsync(destination);
        RequireVisible();
        if (destination is not null && original is null)
            throw new InvalidOperationException("The budget notification belongs to an unavailable profile or period.");
        if (original is not null)
        {
            _calendar = original.Calendar;
            _year = original.Year; _month = original.Month;
            _period = original.Period; _periodStart = original.PeriodStart;
            _visitCurrency = original.CurrencyCode;
        }
        var financialRead = await _store.GetBudgetReadSnapshotAsync();
        var reviewedBudgets = financialRead.Review;
        RequireVisible();
        var settings = reviewedBudgets.Settings ?? new ZananceSettings();
        _startDay = settings.MonthStartDay;
        if (_year == 0)
        {
            // Weekly rows have no month coordinates. Prepare navigation without replacing an alert's original calendar.
            if (_visitCurrency is null) _calendar = settings.BudgetCalendar;
            (_year, _month) = PeriodMath.MonthOf(Today, _calendar, _startDay);
        }

        // The budget of the default account's currency, or the one picked in the switcher (ZEX-P02).
        var allAccounts = reviewedBudgets.Accounts;
        _currency = _visitCurrency ?? BudgetCurrency.Resolve(settings, allAccounts);
        _budgetScope = reviewedBudgets.Choices.ReminderScope;
        var budgets = reviewedBudgets.Choices.Items.Select(item => item.ToBudget()).ToList();
        _refreshing = true;
        try
        {
            CurrencyChoices = BudgetCurrency.Choices(_currency, budgets, allAccounts);
            SelectedCurrency = _currency;
            HasCurrencyChoices = CurrencyChoices.Count > 1;
        }
        finally
        {
            _refreshing = false;
        }

        ShowsPeriods = settings.ShowsExisting(Feature.BudgetPeriods, budgets.Any(b => b.Period != BudgetPeriod.Month && b.CurrencyCode == _currency));
        if (!ShowsPeriods)
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

        if (destination is not null)
        {
            var current = await _store.ResolveBudgetReminderAsync(destination);
            RequireVisible();
            if (current != original) throw new InvalidOperationException("The original budget notification period changed; reload it.");
            _requestedReminder = null;
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
            if (_startDay > 1 || _calendar != Presentation.Calendars.ToPeriod(_localization.CurrentCalendar))
            {
                // A month with its own start day, or budget months in another calendar than the display: show the exact range.
                PeriodText = $"{_dates.Format(from, DateFormatStyle.Short)} – {_dates.Format(to, DateFormatStyle.Short)}";
            }

            _budget = original is null ? BudgetActionsViewModel.Find(budgets, new(_year, _month, _calendar, _period, _periodStart, _currency))
                : budgets.SingleOrDefault(budget => budget.Id == original.Id);
            var (py, pm) = PeriodMath.Previous(_year, _month);
            CanCopyPrevious = _budget is null && BudgetActionsViewModel.Find(budgets, new(py, pm, _calendar, _period, _periodStart, _currency)) is not null;
        }
        else
        {
            if (_periodStart == default)
            {
                _periodStart = BudgetPeriods.StartOf(_period, Today, _localization.FirstDayOfWeek, settings.FortnightStart);
            }

            // A budget made before the week start was changed keeps its own dates.
            _budget = original is null ? BudgetActionsViewModel.Find(budgets, new(_year, _month, _calendar, _period, _periodStart, _currency))
                : budgets.SingleOrDefault(budget => budget.Id == original.Id);
            if (_budget is not null)
            {
                _periodStart = _budget.PeriodStart;
            }

            (from, to) = BudgetPeriods.Range(_period, _periodStart);
            PeriodText = $"{_dates.Format(from, DateFormatStyle.Short)} – {_dates.Format(to, DateFormatStyle.Short)}";
            CanCopyPrevious = _budget is null && BudgetActionsViewModel.Find(budgets, new(_year, _month, _calendar, _period,
                BudgetPeriods.Step(_period, _periodStart, -1), _currency)) is not null;
        }

        CurrencyText = _translator.Format("Budget_Currency", _currency, _translator[Presentation.Calendars.NameKey(_calendar)]);
        HasBudget = _budget is not null;
        ScopeText = _budget is { AccountIds.Count: > 0 } ? _translator.Format("Budget_ScopeLimited", _budget.AccountIds.Count) : null;

        // Freeze financial inputs before dispatch; the worker never reads mutable page/filter state.
        var projection = await BudgetFinancialProjection.PrepareAsync(financialRead, _budget, from, to,
            _currency, Today, ConfirmedOnly);
        RequireVisible();
        await _store.ValidateBudgetEditorReviewAsync(reviewedBudgets);
        RequireVisible();
        var lookup = new CategoryLookup(reviewedBudgets.Categories, _translator);
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
        if (_budget is { } budget)
        {
            if (projection.Flex is { } flex)
                LoadFlex(budget, projection.Carry.Total, flex, culture);
            else if (projection.Total is { } total)
                TotalLines.Add(Line(_translator["Budget_Total"], Symbol.Wallet, Good, total, culture)
                    with { CarryText = Carry(projection.Carry.Total, budget.TotalLimit!.Value, culture) });
            foreach (var line in projection.Categories.OrderBy(line => lookup.Get(line.CategoryId)?.SortOrder ?? int.MaxValue))
                CategoryLines.Add(Line(lookup.Name(line.CategoryId), lookup.Icon(line.CategoryId), lookup.Color(line.CategoryId), line.Status, culture)
                    with { CarryText = Carry(line.Carry, line.OwnLimit, culture) });
        }
        if (projection.EnvelopeOutsideCurrentPeriod) EnvelopeNote = _translator["Envelope_CurrentMonthOnly"];
        else if (projection.Envelope is { } envelope) LoadEnvelopes(envelope, culture);

        HasTotal = TotalLines.Count > 0;
        HasCategoryLines = CategoryLines.Count > 0;

        UnreviewedText = projection.Unreviewed > 0 && !ConfirmedOnly
            ? _translator.Format("Budget_IncludesUnreviewed", projection.Unreviewed) : null;
        var planned = projection.Planned;
        PlannedText = planned.Count == 0 ? null : _translator.Format("Budget_Planned", MoneyText.Format(planned.Total, _currency, culture), planned.Count)
            + (planned.UnknownCount > 0 ? " · " + _translator.Format("Budget_PlannedUnknown", planned.UnknownCount) : string.Empty);
        EquivalentText = projection.Equivalent > 0 && _period == BudgetPeriod.Month
            ? _translator.Format("Budget_Equivalent", MoneyText.Format(projection.Equivalent, _currency, culture)) : null;
        _review = reviewedBudgets;
        Actions.Publish(reviewedBudgets, new(_year, _month, _calendar, _period, _periodStart, _currency), _budget?.Id);
    }


    // Flex (D-28): one limit for flexible spending; fixed bills expected from the plans, non-monthly bills with their
    // monthly share. Every amount is counted in one group only (BUD-12).
    private void LoadFlex(Core.Budgets.Budget budget, long carry, FlexSummary flex, CultureInfo culture)
    {
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
    private void LoadEnvelopes(EnvelopeSummary summary, CultureInfo culture)
    {
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

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private Task PreviousAsync()
    {
        if (!CanNavigate) return Task.CompletedTask;
        Move(-1);
        return LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private Task NextAsync()
    {
        if (!CanNavigate) return Task.CompletedTask;
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

    /// <summary>Gets the original reviewed editor command used by the page and existing native diagnostics.</summary>
    public IAsyncRelayCommand EditCommand => Actions.EditCommand;
    /// <summary>Gets the guarded previous-period copy command.</summary>
    public IAsyncRelayCommand CopyPreviousCommand => Actions.CopyPreviousCommand;
    /// <summary>Gets the guarded next-period copy/replacement command.</summary>
    public IAsyncRelayCommand CopyToNextCommand => Actions.CopyToNextCommand;
    /// <summary>Gets the separately authorized original deletion command.</summary>
    public IAsyncRelayCommand DeleteCommand => Actions.DeleteCommand;
}
