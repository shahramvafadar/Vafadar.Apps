using FluentIcons.Common;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>A goal in a list, on Home and in its details.</summary>
/// <param name="Id">The goal.</param>
/// <param name="Name">Its name.</param>
/// <param name="Icon">Its icon.</param>
/// <param name="FundedText">"2,000 of 5,000 EUR".</param>
/// <param name="Progress">The bar, 0 to 1.</param>
/// <param name="ProgressColor">Teal while saving, amber when money is missing, green when reached.</param>
/// <param name="DateText">"Needed by …", when there is a date.</param>
/// <param name="SuggestionText">The contribution needed per date, or "Target reached".</param>
/// <param name="WarningText">Overdue, unfunded, archived account or negative balance.</param>
/// <param name="StateText">A state chip with text (paused, completed, archived), never colour only.</param>
public sealed record GoalRow(
    Guid Id,
    string Name,
    Symbol Icon,
    string FundedText,
    double Progress,
    Color ProgressColor,
    string? DateText,
    string? SuggestionText,
    string? WarningText,
    string? StateText)
{
    /// <summary>Gets the remaining amount and percentage, e.g. "40 % · 3,000 EUR left".</summary>
    public string? RemainingText { get; init; }

    /// <summary>Gets the estimate of the user's plan, e.g. "~ 25 Sep 2027 with 250 EUR per date"; only when valid.</summary>
    public string? PlanText { get; init; }

    /// <summary>Gets the whole card as one sentence for screen readers (ZEX-S0304).</summary>
    public string Description => string.Join(". ", new[] { Name, FundedText, RemainingText, PlanText, WarningText, StateText }.Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>Formats goals for lists, details and Home (F2-GOAL-02, F2-GOAL-04, F2-GOAL-05, ZEX-S0302..S0304).</summary>
public sealed class GoalPresenter(Translator translator, IDateFormatter dates, ILocalizationService localization, Data.HoldingStore holdings, Holdings.HoldingText holdingText)
{
    private IReadOnlyList<Core.Holdings.AssetType> _types = [];

    /// <summary>
    /// Evaluates goals of every type with the holdings a quantity goal counts (ZEX-S0701) and keeps the holding types for
    /// <see cref="Row"/>, so quantities show in their unit.
    /// </summary>
    public async Task<IReadOnlyList<GoalProgress>> EvaluateAsync(Data.GoalStore goals, IReadOnlyCollection<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, DateOnly today, IEnumerable<Goal>? all = null)
    {
        ArgumentNullException.ThrowIfNull(goals);
        _types = await holdings.GetTypesAsync();
        var events = await holdings.GetEventsAsync();
        return GoalProgressService.Evaluate(all ?? await goals.GetGoalsAsync(), await goals.GetAllocationsAsync(), accounts, entries, await goals.GetContributionPlansAsync(), today, events, _types);
    }

    /// <summary>Returns an amount of a goal: money, or the quantity of its holding in the holding's unit.</summary>
    public string Amount(Goal goal, long value)
    {
        ArgumentNullException.ThrowIfNull(goal);
        return goal.Type == GoalType.HoldingQuantity && _types.FirstOrDefault(t => t.Id == goal.AssetTypeId) is { } type
            ? holdingText.Quantity(value, type)
            : Money(value, goal.CurrencyCode);
    }

    /// <summary>Returns the holding type of a quantity goal, if known.</summary>
    public Core.Holdings.AssetType? TypeOf(Goal goal) => goal?.Type == GoalType.HoldingQuantity ? _types.FirstOrDefault(t => t.Id == goal.AssetTypeId) : null;
    /// <summary>Returns the recorded balance of every account.</summary>
    public static Dictionary<Guid, long> Balances(IEnumerable<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, DateOnly today) =>
        accounts.ToDictionary(a => a.Id, a => LedgerCalculator.Balance(a, entries, today));

    public string Money(long amount, string currency) => MoneyText.Format(amount, currency, localization.CurrentCulture);

    /// <summary>Returns the row of a goal; <paramref name="progress"/> is <see langword="null"/> for completed and archived goals.</summary>
    public GoalRow Row(Goal goal, GoalProgress? progress)
    {
        ArgumentNullException.ThrowIfNull(goal);
        var icon = Icons.Parse(goal.Icon, goal.Type switch { GoalType.AccountBalance => Symbol.BuildingBank, GoalType.HoldingQuantity => Symbol.Diamond, _ => Symbol.Savings });
        if (progress is null)
        {
            // Completed or archived: no funding, only the history.
            return new GoalRow(goal.Id, goal.Name, icon, Amount(goal, goal.TargetAmount), 1, Palette.Muted,
                null, null, null, translator[$"GoalState_{goal.State}"]);
        }

        var culture = localization.CurrentCulture;
        var currency = goal.CurrencyCode;
        var funded = translator.Format(goal.Type == GoalType.Earmark ? "Goal_Funded" : "Goal_BalanceProgress", Amount(goal, Math.Max(0, progress.Current)), Amount(goal, goal.TargetAmount));
        var date = goal.TargetDate is { } target ? translator.Format("Goal_By", dates.Format(target, DateFormatStyle.Long)) : null;
        var remaining = progress.IsReached
            ? (progress.Overshoot > 0 ? translator.Format("Goal_AboveTarget", Amount(goal, progress.Overshoot)) : null)
            : translator.Format("Goal_Remaining", Math.Round(progress.Progress * 100).ToString("0", culture), Amount(goal, progress.Remaining));

        string? suggestion = null;
        if (progress.IsReached)
        {
            suggestion = translator["Goal_Reached"];
        }
        else if (progress.Required is { } next && next > 0)
        {
            suggestion = progress.Opportunities <= 0
                ? translator.Format("Goal_SuggestNow", Amount(goal, next))
                : translator.Format("Goal_SuggestPerDate", Amount(goal, next), progress.Opportunities);
        }

        string? plan = null;
        if (!progress.IsReached && progress.Eta is { } eta && progress.PlannedContribution is { } contribution && goal.State == GoalState.Active)
        {
            plan = translator.Format("Goal_EtaPlan", dates.Format(eta, DateFormatStyle.MonthYear), Amount(goal, contribution));
        }

        var warnings = new List<string>();
        if (progress.Unfunded > 0)
        {
            warnings.Add(translator.Format("Goal_Unfunded", Amount(goal, progress.Unfunded)));
        }

        switch (progress.Notice)
        {
            case GoalNotice.Overdue:
                warnings.Add(translator.Format("Goal_OverdueNeeded", Amount(goal, progress.Remaining)));
                break;
            case GoalNotice.AccountUnavailable:
                warnings.Add(translator["Goal_AccountArchived"]);
                break;
            case GoalNotice.HoldingUnavailable:
                warnings.Add(translator["Goal_HoldingUnavailable"]);
                break;
            case GoalNotice.NegativeBalance:
                warnings.Add(translator.Format("Goal_NegativeBalance", Amount(goal, progress.Current)));
                break;
        }

        // Savings are teal; amber while money is missing, green once the goal is reached (D-27).
        var color = progress.IsReached ? Palette.IncomeText : progress.Unfunded > 0 || warnings.Count > 0 ? Palette.NearLimit : Palette.SavingText;
        return new GoalRow(goal.Id, goal.Name, icon, funded, progress.Progress, color, date, suggestion,
            warnings.Count > 0 ? string.Join(Environment.NewLine, warnings) : null,
            goal.State == GoalState.Paused ? translator["GoalState_Paused"] : null)
        {
            RemainingText = remaining,
            PlanText = plan,
        };
    }
}
