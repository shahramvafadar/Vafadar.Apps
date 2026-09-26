using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Goals;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;

namespace Vafadar.Finance.App.Features.Goals;

/// <summary>A goal in a list.</summary>
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
    string? StateText);

/// <summary>Formats goal status for lists and details (F2-GOAL-02, F2-GOAL-04, F2-GOAL-05).</summary>
public sealed class GoalPresenter(Translator translator, IDateFormatter dates, ILocalizationService localization)
{
    /// <summary>Returns the recorded balance of every account.</summary>
    public static Dictionary<Guid, long> Balances(IEnumerable<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, DateOnly today) =>
        accounts.ToDictionary(a => a.Id, a => LedgerCalculator.Balance(a, entries, today));

    public string Money(long amount, string currency) => MoneyText.Format(amount, currency, localization.CurrentCulture);

    public GoalRow Row(Goal goal, GoalStatus? status, DateOnly today)
    {
        var icon = Icons.Parse(goal.Icon, Symbol.Savings);
        if (status is null)
        {
            // Completed or archived: no funding, only the history.
            return new GoalRow(goal.Id, goal.Name, icon, Money(goal.TargetAmount, goal.CurrencyCode), 1, Color.FromArgb("#9E9E9E"),
                null, null, null, translator[$"GoalState_{goal.State}"]);
        }

        var funded = translator.Format("Goal_Funded", Money(status.Funded, goal.CurrencyCode), Money(goal.TargetAmount, goal.CurrencyCode));
        var date = goal.TargetDate is { } target ? translator.Format("Goal_By", dates.Format(target, DateFormatStyle.Long)) : null;
        string? suggestion = null;
        if (status.IsReached)
        {
            suggestion = translator["Goal_Reached"];
        }
        else if (status.SuggestedContribution is { } next && next > 0)
        {
            suggestion = status.Opportunities <= 0
                ? translator.Format("Goal_SuggestNow", Money(next, goal.CurrencyCode))
                : translator.Format(goal.Frequency == ContributionFrequency.Weekly ? "Goal_SuggestWeekly" : "Goal_SuggestMonthly", Money(next, goal.CurrencyCode), status.Opportunities);
        }

        var warnings = new List<string>();
        if (status.Unfunded > 0)
        {
            warnings.Add(translator.Format("Goal_Unfunded", Money(status.Unfunded, goal.CurrencyCode)));
        }

        if (status.IsOverdue(today))
        {
            warnings.Add(translator["Goal_Overdue"]);
        }

        var color = status.IsReached ? Color.FromArgb("#1B5E20") : status.Unfunded > 0 ? Color.FromArgb("#8D5B00") : Color.FromArgb("#2E7D32");
        return new GoalRow(goal.Id, goal.Name, icon, funded, status.Progress, color, date, suggestion,
            warnings.Count > 0 ? string.Join(Environment.NewLine, warnings) : null, null);
    }
}