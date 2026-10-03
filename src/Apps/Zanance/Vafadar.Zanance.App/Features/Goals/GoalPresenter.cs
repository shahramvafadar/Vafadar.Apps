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
public sealed class GoalPresenter(Translator translator, IDateFormatter dates, ILocalizationService localization)
{
    /// <summary>Returns the recorded balance of every account.</summary>
    public static Dictionary<Guid, long> Balances(IEnumerable<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, DateOnly today) =>
        accounts.ToDictionary(a => a.Id, a => LedgerCalculator.Balance(a, entries, today));

    public string Money(long amount, string currency) => MoneyText.Format(amount, currency, localization.CurrentCulture);

    /// <summary>Returns the row of a goal; <paramref name="progress"/> is <see langword="null"/> for completed and archived goals.</summary>
    public GoalRow Row(Goal goal, GoalProgress? progress)
    {
        ArgumentNullException.ThrowIfNull(goal);
        var icon = Icons.Parse(goal.Icon, goal.Type == GoalType.AccountBalance ? Symbol.BuildingBank : Symbol.Savings);
        if (progress is null)
        {
            // Completed or archived: no funding, only the history.
            return new GoalRow(goal.Id, goal.Name, icon, Money(goal.TargetAmount, goal.CurrencyCode), 1, Palette.Muted,
                null, null, null, translator[$"GoalState_{goal.State}"]);
        }

        var culture = localization.CurrentCulture;
        var currency = goal.CurrencyCode;
        var funded = translator.Format(goal.Type == GoalType.AccountBalance ? "Goal_BalanceProgress" : "Goal_Funded", Money(Math.Max(0, progress.Current), currency), Money(goal.TargetAmount, currency));
        var date = goal.TargetDate is { } target ? translator.Format("Goal_By", dates.Format(target, DateFormatStyle.Long)) : null;
        var remaining = progress.IsReached
            ? (progress.Overshoot > 0 ? translator.Format("Goal_AboveTarget", Money(progress.Overshoot, currency)) : null)
            : translator.Format("Goal_Remaining", Math.Round(progress.Progress * 100).ToString("0", culture), Money(progress.Remaining, currency));

        string? suggestion = null;
        if (progress.IsReached)
        {
            suggestion = translator["Goal_Reached"];
        }
        else if (progress.Required is { } next && next > 0)
        {
            suggestion = progress.Opportunities <= 0
                ? translator.Format("Goal_SuggestNow", Money(next, currency))
                : translator.Format("Goal_SuggestPerDate", Money(next, currency), progress.Opportunities);
        }

        string? plan = null;
        if (!progress.IsReached && progress.Eta is { } eta && progress.PlannedContribution is { } contribution && goal.State == GoalState.Active)
        {
            plan = translator.Format("Goal_EtaPlan", dates.Format(eta, DateFormatStyle.MonthYear), Money(contribution, currency));
        }

        var warnings = new List<string>();
        if (progress.Unfunded > 0)
        {
            warnings.Add(translator.Format("Goal_Unfunded", Money(progress.Unfunded, currency)));
        }

        switch (progress.Notice)
        {
            case GoalNotice.Overdue:
                warnings.Add(translator.Format("Goal_OverdueNeeded", Money(progress.Remaining, currency)));
                break;
            case GoalNotice.AccountUnavailable:
                warnings.Add(translator["Goal_AccountArchived"]);
                break;
            case GoalNotice.NegativeBalance:
                warnings.Add(translator.Format("Goal_NegativeBalance", Money(progress.Current, currency)));
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
