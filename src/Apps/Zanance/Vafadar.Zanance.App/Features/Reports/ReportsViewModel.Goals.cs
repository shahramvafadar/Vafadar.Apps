using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>A suggested monthly contribution for a goal within the capacity; accepting it changes no budget or entry.</summary>
public sealed record SuggestionRow(Guid GoalId, string Name, string NeededText, string SuggestedText, long Suggested, bool CanAccept);

/// <summary>R3 – goals and capacity: progress, capacity and its split, essential coverage.</summary>
public sealed partial class ReportsViewModel
{
    private CapacityResult? _capacity;
    private EssentialCoverage? _coverage;

    public ObservableCollection<Goals.GoalRow> GoalRows { get; } = [];

    public ObservableCollection<SuggestionRow> Suggestions { get; } = [];

    public ObservableCollection<AmountLine> CapacityLines { get; } = [];

    [ObservableProperty]
    public partial bool HasGoalRows { get; set; }

    /// <summary>Gets the capacity per month, or "not enough history" (02 §9.3).</summary>
    [ObservableProperty]
    public partial string? CapacityText { get; set; }

    [ObservableProperty]
    public partial bool HasSuggestions { get; set; }

    /// <summary>Gets the essential coverage, e.g. "4.0 months" (ZEX-K07).</summary>
    [ObservableProperty]
    public partial string? CoverageText { get; set; }

    [ObservableProperty]
    public partial string? CoverageDetail { get; set; }

    private async Task BuildGoalsAsync(IReadOnlyCollection<LedgerEntry> entries)
    {
        var today = Today;
        var goals = await _goals.GetGoalsAsync();
        var plans = await _goals.GetContributionPlansAsync();
        var progress = await _goalPresenter.EvaluateAsync(_goals, _accounts, entries, today, goals);

        // K03: every active or paused goal with its progress and the next step.
        GoalRows.Clear();
        foreach (var item in progress.Where(p => p.Goal.State is GoalState.Active or GoalState.Paused))
        {
            GoalRows.Add(_goalPresenter.Row(item.Goal, item));
        }

        HasGoalRows = GoalRows.Count > 0;

        // Capacity and its split over the active goals of this currency (Advanced); a suggestion, never an allocation.
        Suggestions.Clear();
        CapacityLines.Clear();
        CapacityText = null;
        CoverageText = null;
        CoverageDetail = null;
        if (IsAdvanced)
        {
            var schedules = await _plans.GetSchedulesAsync();
            _capacity = CapacityCalculator.Compute(_accounts, entries, schedules, await _plans.GetStatesAsync(), goals, plans, _currency, today, Calendar, _startDay);
            if (_capacity.Amount is { } capacity)
            {
                CapacityText = _translator.Format("Report_CapacityValue", Money(capacity));
                CapacityLines.Add(new AmountLine(_translator["Report_CapIncome"], Money(_capacity.Income), false));
                CapacityLines.Add(new AmountLine("− " + _translator["Report_CapSpending"], Money(_capacity.Spending), false));
                CapacityLines.Add(new AmountLine("− " + _translator["Report_CapNonMonthly"], Money(_capacity.NonMonthly), false));
                CapacityLines.Add(new AmountLine("− " + _translator["Report_CapPrincipal"], Money(_capacity.LoanPrincipal), false));
                CapacityLines.Add(new AmountLine("− " + _translator["Report_CapGoals"], Money(_capacity.OtherGoals), false));
                CapacityLines.Add(new AmountLine("= " + _translator["Report_Capacity"], Money(capacity), true, capacity < 0));

                // Goals without a plan of their own share what is left; those with a plan are already in G.
                var planned = plans.Select(p => p.GoalId).ToHashSet();
                var needs = progress.Where(p => p.Goal.State == GoalState.Active && p.Goal.Type != GoalType.HoldingQuantity && !planned.Contains(p.Goal.Id) && string.Equals(p.Goal.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase))
                    .Select(p => CapacityCalculator.Need(p.Goal, p.Remaining, today))
                    .Where(n => n.PerMonth > 0)
                    .ToList();
                var names = goals.ToDictionary(g => g.Id, g => g.Name);
                foreach (var suggestion in CapacityCalculator.Distribute(capacity, needs))
                {
                    Suggestions.Add(new SuggestionRow(suggestion.GoalId, names[suggestion.GoalId],
                        _translator.Format("Report_Needs", Money(suggestion.Needed)), Money(suggestion.Suggested), suggestion.Suggested, suggestion.Suggested > 0));
                }
            }
            else
            {
                CapacityText = _translator["Report_NotEnoughHistory"];
            }

            // K07: months of essential costs the usable money covers; protected money is named, not removed.
            _coverage = KpiCatalog.Coverage(_accounts, entries, await _store.GetCategoriesAsync(), schedules, _currency, today, Calendar, _startDay);
            CoverageText = _coverage.Status switch
            {
                CoverageStatus.Ok or CoverageStatus.BalanceNegative => _translator.Format("Report_Months", _coverage.Months!.Value.ToString("0.0", Culture)),
                CoverageStatus.NotEnoughHistory => _translator["Report_NotEnoughHistory"],
                _ => _translator["Report_NotAvailable"],
            };
            var protectedMoney = LiquidityCalculator.ProtectedMoney(goals, await _goals.GetAllocationsAsync(), _accounts, entries, today).GetValueOrDefault(_currency);
            CoverageDetail = _coverage.Status is CoverageStatus.Ok or CoverageStatus.BalanceNegative
                ? _translator.Format("Report_CoverageDetail", Money(_coverage.UsableMoney), Money(_coverage.MonthlyEssential))
                  + (protectedMoney > 0 ? " · " + _translator.Format("Report_OfWhichProtected", Money(protectedMoney)) : string.Empty)
                : null;
        }

        HasSuggestions = Suggestions.Count > 0;
        await BuildStatusAsync(entries, full: false);
    }

    // Accepting stores a monthly contribution plan for the goal from the next financial month; no budget, entry or
    // money set aside changes (AT23).
    [RelayCommand]
    private async Task AcceptSuggestionAsync(SuggestionRow row)
    {
        if (!row.CanAccept)
        {
            return;
        }

        var current = PeriodMath.MonthOf(Today, Calendar, _startDay);
        var next = PeriodMath.Next(current.Year, current.Month);
        await _goals.SaveContributionPlanAsync(row.GoalId, new ContributionPlan
        {
            Method = ContributionMethod.FixedAmount,
            Amount = row.Suggested,
            Rule = new Core.Plans.RecurrenceRule { Frequency = Core.Plans.Frequency.Monthly, Start = PeriodMath.MonthRange(next.Year, next.Month, Calendar, _startDay).First },
        });
        await Shell.Current.DisplayAlertAsync(_translator["Report_R3"], _translator.Format("Report_Accepted", row.SuggestedText, row.Name), _translator["Common_Ok"]);
        await LoadAsync();
    }

    [RelayCommand]
    private Task OpenGoalAsync(Goals.GoalRow row) => Shell.Current.GoToAsync(AppShell.GoalDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });

    [RelayCommand]
    private Task ExplainCapacityAsync() => ExplainAsync("Capacity", CapacityText ?? string.Empty, [.. CapacityLines], () => Drill(KindFilter.All, null, null, PreviousMonths().From, PreviousMonths().To));

    [RelayCommand]
    private Task ExplainCoverageAsync() => ExplainAsync("K07", CoverageText ?? string.Empty,
        _coverage is { } c ? [new(_translator["Report_UsableMoney"], Money(c.UsableMoney), false), new(_translator["Report_EssentialMonth"], Money(c.MonthlyEssential), false)] : [],
        () => Drill(KindFilter.Expenses, null, null, PreviousMonths().From, PreviousMonths().To));

    [RelayCommand]
    private Task ExplainGoalsAsync() => ExplainAsync("K03", string.Empty, [.. GoalRows.Select(g => new AmountLine(g.Name, g.FundedText, false))], () => Shell.Current.GoToAsync(AppShell.GoalsRoute));

    // The three complete financial months capacity and coverage are based on.
    private (DateOnly From, DateOnly To) PreviousMonths()
    {
        var current = PeriodMath.MonthOf(Today, Calendar, _startDay);
        var last = PeriodMath.Previous(current.Year, current.Month);
        var first = PeriodMath.Previous(PeriodMath.Previous(last.Year, last.Month).Year, PeriodMath.Previous(last.Year, last.Month).Month);
        return (PeriodMath.MonthRange(first.Year, first.Month, Calendar, _startDay).First, PeriodMath.MonthRange(last.Year, last.Month, Calendar, _startDay).Last);
    }
}
