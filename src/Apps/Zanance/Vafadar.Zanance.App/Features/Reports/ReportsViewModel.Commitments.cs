using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>An open payment of the next days (ZEX-K04).</summary>
public sealed record CommitmentRow(string Name, string DateText, string AmountText, bool IsOverdue, Guid ScheduleId);

/// <summary>R2 – commitments and cost of living: headroom, next 30 days, twelve months, debts and receivables.</summary>
public sealed partial class ReportsViewModel
{
    private LiquidityResult? _liquidity;

    public ObservableCollection<CommitmentRow> CommitmentRows { get; } = [];

    public ObservableCollection<AmountLine> YearlyLines { get; } = [];

    public ObservableCollection<AmountLine> ReceivableLines { get; } = [];

    public ObservableCollection<PlanReportRow> PlanRows { get; } = [];

    /// <summary>Gets the headroom, e.g. "−330.00 EUR around 24 Oct" (04 §3); an estimate, never "safe to spend".</summary>
    [ObservableProperty]
    public partial string? HeadroomText { get; set; }

    [ObservableProperty]
    public partial Color? HeadroomColor { get; set; }

    [ObservableProperty]
    public partial string? HeadroomSentence { get; set; }

    /// <summary>Gets what the headroom leaves out or cannot know: day-to-day spending, unknown amounts.</summary>
    [ObservableProperty]
    public partial string? HeadroomNote { get; set; }

    [ObservableProperty]
    public partial string? CommitmentsTotalText { get; set; }

    [ObservableProperty]
    public partial bool HasCommitments { get; set; }

    [ObservableProperty]
    public partial string? YearlyTotalText { get; set; }

    [ObservableProperty]
    public partial bool HasYearly { get; set; }

    [ObservableProperty]
    public partial string? DebtText { get; set; }

    [ObservableProperty]
    public partial string? DebtRatioText { get; set; }

    [ObservableProperty]
    public partial bool HasDebt { get; set; }

    [ObservableProperty]
    public partial string? ReceivablesText { get; set; }

    [ObservableProperty]
    public partial bool HasReceivables { get; set; }

    [ObservableProperty]
    public partial bool HasPlanRows { get; set; }

    private async Task BuildCommitmentsAsync(IReadOnlyCollection<LedgerEntry> entries)
    {
        var today = Today;
        var schedules = await _plans.GetSchedulesAsync();
        var states = await _plans.GetStatesAsync();

        // K02 and headroom: usable accounts, the next 30 days, protected money and the day-to-day estimate if set.
        var forecasts = ForecastCalculator.Compute(_accounts, entries, schedules, states, today, today.AddDays(30));
        var protectedMoney = LiquidityCalculator.ProtectedMoney(await _goals.GetGoalsAsync(), await _goals.GetAllocationsAsync(), _accounts, entries, today);
        _liquidity = LiquidityCalculator.Compute(forecasts, protectedMoney, today, _settings.EssentialEstimate, _settings.EssentialEstimatePeriod, _settings.EssentialEstimateCurrency)
            .FirstOrDefault(l => string.Equals(l.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase));
        BuildHeadroom();

        // K04: what must not be forgotten; known, estimated and unknown amounts stay apart.
        CommitmentRows.Clear();
        var commitments = KpiCatalog.Commitments(_accounts, schedules, states, today).FirstOrDefault(c => string.Equals(c.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase));
        if (commitments is null || commitments.Items.Count == 0)
        {
            CommitmentsTotalText = _translator["Report_NoCommitments"];
        }
        else
        {
            CommitmentsTotalText = string.Join(" · ", new[]
            {
                Money(commitments.Fixed),
                commitments.Estimated > 0 ? "≈ " + Money(commitments.Estimated) : null,
                commitments.UnknownCount > 0 ? _translator.Format("Report_UnknownCount", commitments.UnknownCount) : null,
            }.Where(s => s is not null));
            foreach (var item in commitments.Items.Take(IsAdvanced ? 50 : 3))
            {
                var amount = item.Amount is { } known ? (item.IsEstimate ? "≈ " : string.Empty) + Money(known) : "?";
                CommitmentRows.Add(new CommitmentRow(item.Name, _dates.Format(item.DueDate, DateFormatStyle.Short) + (item.IsOverdue ? " · " + _translator["Occurrence_Overdue"] : string.Empty), amount, item.IsOverdue, item.ScheduleId));
            }
        }

        HasCommitments = CommitmentRows.Count > 0;

        // K08 (Advanced): what regular payments cost over a year; a monthly share is not money set aside.
        YearlyLines.Clear();
        YearlyTotalText = null;
        if (IsAdvanced)
        {
            var yearly = KpiCatalog.Yearly(_accounts, await _store.GetCategoriesAsync(), schedules, states, today).FirstOrDefault(y => string.Equals(y.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase));
            if (yearly is not null)
            {
                YearlyTotalText = _translator.Format("Report_YearlyTotals", Money(yearly.Fixed), Money(yearly.NonMonthly), "≈ " + Money(yearly.Estimated))
                    + (yearly.UnknownCount > 0 ? " · " + _translator.Format("Report_UnknownCount", yearly.UnknownCount) : string.Empty);
                foreach (var plan in yearly.Plans)
                {
                    var share = plan.MonthlyShare is { } monthly ? " · " + _translator.Format("Report_MonthlyShare", Money(monthly)) : string.Empty;
                    YearlyLines.Add(new AmountLine(plan.Schedule.Name + share, (plan.Group == CommitmentGroup.Estimated ? "≈ " : string.Empty) + Money(plan.Total), false));
                }
            }
        }

        HasYearly = YearlyLines.Count > 0;

        // K09: next installment in Simple, its share of income in Advanced (this financial month).
        var (monthFrom, monthTo) = PeriodMath.MonthRange(PeriodMath.MonthOf(today, Calendar, _startDay).Year, PeriodMath.MonthOf(today, Calendar, _startDay).Month, Calendar, _startDay);
        var debt = KpiCatalog.Debt(_accounts, entries, schedules, states, monthFrom, monthTo, today).FirstOrDefault(d => string.Equals(d.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase));
        DebtText = debt switch
        {
            null => null,
            { NextDate: { } date } => _translator.Format("Report_NextInstallment", debt.NextAmount is { } next ? Money(next) : "?", _dates.Format(date, DateFormatStyle.Short)),
            { LoansWithoutPlan: > 0 } => _translator["Report_AddPaymentDay"],
            _ => null,
        };
        DebtRatioText = IsAdvanced && debt is not null && debt.Installments > 0
            ? _translator.Format("Report_DebtRatio", debt.RatioPercent is { } ratio ? ratio.ToString("0.0", Culture) + " %" : _translator["Report_NotAvailable"], Money(debt.Interest), Money(debt.Principal))
            : null;
        HasDebt = DebtText is not null;

        // K12: money owed to the user; never usable money until received.
        ReceivableLines.Clear();
        var receivables = KpiCatalog.Receivables(_accounts, entries, today).FirstOrDefault(r => string.Equals(r.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase));
        ReceivablesText = receivables is null
            ? _translator["Report_NoReceivables"]
            : _translator.Format("Report_Receivables", Money(receivables.Total), receivables.Items.Count)
              + (receivables.Over90Count > 0 ? " · " + _translator.Format("Report_Over90", receivables.Over90Count) : string.Empty);
        if (receivables is not null && IsAdvanced)
        {
            ReceivableLines.Add(new AmountLine(_translator["Report_Age0to30"], Money(receivables.UpTo30Days), false));
            ReceivableLines.Add(new AmountLine(_translator["Report_Age31to90"], Money(receivables.UpTo90Days), false));
            ReceivableLines.Add(new AmountLine(_translator["Report_AgeOver90"], Money(receivables.Over90Days), false, receivables.Over90Days > 0));
            foreach (var item in receivables.Items)
            {
                var due = item.DueDate is { } dueDate ? " · " + _translator.Format(item.IsOverdue ? "Report_DueOverdue" : "Report_Due", _dates.Format(dueDate, DateFormatStyle.Short)) : string.Empty;
                ReceivableLines.Add(new AmountLine($"{item.Name} · {_translator.Format("Report_Days", item.AgeDays)}{due}", Money(item.Amount), false, item.IsOverdue));
            }
        }

        HasReceivables = receivables is not null;

        // Plans versus actual of the financial month (Advanced).
        PlanRows.Clear();
        if (IsAdvanced)
        {
            BuildPlans(entries, schedules, states, monthFrom, monthTo);
        }

        HasPlanRows = PlanRows.Count > 0;
        await BuildStatusAsync(entries, full: false, unknownAmounts: _liquidity?.UnknownCount ?? 0);
    }

    private void BuildHeadroom()
    {
        if (_liquidity is not { } l)
        {
            HeadroomText = null;
            HeadroomSentence = _translator["Report_NoUsableAccounts"];
            HeadroomNote = null;
            return;
        }

        var date = _dates.Format(l.MinimumDate, DateFormatStyle.Short);
        HeadroomText = Money(l.Headroom);
        HeadroomColor = l.Headroom < 0 ? EntryPresenter.DangerColor : Palette.AmountText;
        HeadroomSentence = l.Minimum < 0
            ? _translator.Format("Home_BalanceBelowZero", date)
            : l.Headroom < 0
                ? _translator.Format("Report_UsesProtected", Money(-l.Headroom), date)
                : _translator.Format("Report_LowestBalance", Money(l.Minimum), date);
        HeadroomNote = string.Join(" · ", new[]
        {
            l.IncludesEssential ? null : _translator["Report_EssentialNotIncluded"],
            l.IsIncomplete ? _translator.Format("Report_Incomplete", l.UnknownCount) : null,
            _translator["Report_EstimateNotGuarantee"],
        }.Where(s => s is not null));
    }

    private void BuildPlans(IReadOnlyCollection<LedgerEntry> entries, IReadOnlyList<Core.Plans.Schedule> schedules, IReadOnlyList<Core.Plans.OccurrenceState> states, DateOnly from, DateOnly to)
    {
        var byId = _accounts.ToDictionary(a => a.Id);
        foreach (var row in ReportCalculator.PlanVsActual(schedules, states, entries, from, to, Today))
        {
            var currency = byId.TryGetValue(row.Schedule.AccountId, out var account) ? account.CurrencyCode : _currency;
            var details = _translator.Format("Report_PlanDetails", row.SettledCount, row.PlannedCount)
                + (row.UnknownCount > 0 ? " · " + _translator.Format("Budget_PlannedUnknown", row.UnknownCount) : string.Empty)
                + (row.OpenCount > 0 ? " · " + _translator.Format("Report_PlanOpen", row.OpenCount) : string.Empty)
                + (row.Variance is { } variance and not 0 ? " · " + _translator.Format("Report_PlanVariance", Money(variance, currency, showPlus: true)) : string.Empty)
                + (BudgetPlanning.MonthlyEquivalent(row.Schedule) is { } monthly ? " · " + _translator.Format("Report_PlanMonthly", Core.Money.MoneyText.Format(monthly, currency, Culture, approximate: true)) : string.Empty);
            PlanRows.Add(new PlanReportRow(row.Schedule.Name, Money(row.Planned, currency), Money(row.Actual, currency), details));
        }
    }

    [RelayCommand]
    private Task ExplainHeadroomAsync()
    {
        if (_liquidity is not { } l)
        {
            return Task.CompletedTask;
        }

        var lines = new List<AmountLine> { new(_translator.Format("Report_UsableToday", _dates.Format(Today, DateFormatStyle.Short)), Money(l.Start), false) };
        lines.AddRange(l.Items.Where(i => i.Effect is not null).Select(i => new AmountLine($"{i.Name} · {_dates.Format(i.Date, DateFormatStyle.Short)}", Money(i.Effect!.Value, showPlus: true), false, i.Effect < 0)));
        if (l.EssentialPerDay is { } perDay)
        {
            var days = l.MinimumDate.DayNumber - Today.DayNumber;
            lines.Add(new AmountLine(_translator.Format("Report_DayToDay", days, Money((long)Math.Round(perDay, MidpointRounding.AwayFromZero))), Money(-l.EssentialToMinimum), false, true));
        }

        lines.Add(new AmountLine(_translator.Format("Report_LowestOn", _dates.Format(l.MinimumDate, DateFormatStyle.Short)), Money(l.Minimum), true, l.Minimum < 0));
        if (l.Protected > 0)
        {
            lines.Add(new AmountLine(_translator["Report_Protected"], Money(-l.Protected), false, true));
        }

        lines.Add(new AmountLine(_translator["Report_Headroom"], Money(l.Headroom), true, l.Headroom < 0));
        foreach (var later in l.ItemsAfter.Where(i => i.Effect > 0).Take(2))
        {
            lines.Add(new AmountLine(_translator.Format("Report_ComesAfter", later.Name, _dates.Format(later.Date, DateFormatStyle.Short)), Money(later.Effect!.Value, showPlus: true), false));
        }

        return ExplainAsync("K02", _translator.Format("Report_Around", Money(l.Headroom), _dates.Format(l.MinimumDate, DateFormatStyle.Short)), lines, () => Shell.Current.GoToAsync(AppShell.ForecastRoute));
    }

    [RelayCommand]
    private Task ExplainCommitmentsAsync() => ExplainAsync("K04", CommitmentsTotalText ?? string.Empty,
        [.. CommitmentRows.Select(c => new AmountLine($"{c.Name} · {c.DateText}", c.AmountText, false, c.IsOverdue))], () => Shell.Current.GoToAsync("//plans"));

    [RelayCommand]
    private Task ExplainYearlyAsync() => ExplainAsync("K08", YearlyTotalText ?? string.Empty, [.. YearlyLines], () => Shell.Current.GoToAsync("//plans"));

    [RelayCommand]
    private Task ExplainDebtAsync() => ExplainAsync("K09", DebtRatioText ?? DebtText ?? string.Empty, [], () => Shell.Current.GoToAsync(AppShell.AccountsRoute));

    [RelayCommand]
    private Task ExplainReceivablesAsync() => ExplainAsync("K12", ReceivablesText ?? string.Empty, [.. ReceivableLines], () => Shell.Current.GoToAsync(AppShell.ReimbursementsRoute));

    [RelayCommand]
    private Task OpenCommitmentAsync(CommitmentRow row) => Shell.Current.GoToAsync(AppShell.PlanDetailRoute, new Dictionary<string, object> { ["id"] = row.ScheduleId });
}
