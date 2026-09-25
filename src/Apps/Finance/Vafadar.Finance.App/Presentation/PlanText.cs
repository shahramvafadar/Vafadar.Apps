using System.Globalization;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Core.Plans;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;

namespace Vafadar.Finance.App.Presentation;

/// <summary>Human-readable texts for plans and occurrences.</summary>
internal sealed class PlanText(Translator translator, IDateFormatter dates, CultureInfo culture)
{
    private static readonly PersianCalendar Persian = new();

    /// <summary>Describes a rule, e.g. "Every 2 weeks on Friday" or "Every month on the last day (Persian calendar)".</summary>
    public string Rule(RecurrenceRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var n = rule.Interval;
        var text = rule.Frequency switch
        {
            Frequency.Once => translator.Format("Rule_Once", dates.Format(rule.Start, DateFormatStyle.Long)),
            Frequency.Daily => n == 1 ? translator["Rule_Daily"] : translator.Format("Rule_EveryNDays", n),
            Frequency.Weekly => translator.Format(n == 1 ? "Rule_Weekly" : "Rule_EveryNWeeks", culture.DateTimeFormat.GetDayName(rule.Start.DayOfWeek), n),
            Frequency.Monthly => translator.Format(n == 1 ? "Rule_Monthly" : "Rule_EveryNMonths", DayText(rule), n),
            _ => translator.Format(n == 1 ? "Rule_Yearly" : "Rule_EveryNYears", YearDayText(rule), n),
        };

        if (rule.Frequency is Frequency.Monthly or Frequency.Yearly && rule.Calendar == PeriodCalendar.Persian)
        {
            text += " · " + translator["Calendar_Persian"];
        }

        return rule.End switch
        {
            EndKind.OnDate when rule.EndDate is { } end => text + " · " + translator.Format("Rule_Until", dates.Format(end, DateFormatStyle.Short)),
            EndKind.AfterCount when rule.Count is { } count => text + " · " + translator.Format("Rule_Times", count),
            _ => text,
        };
    }

    /// <summary>The amount of an occurrence: exact, approximate (≈) or unknown (REC-03).</summary>
    public string Amount(long? amount, AmountMode mode, string currencyCode) => (amount, mode) switch
    {
        (null, _) or (_, AmountMode.Unknown) => translator["Plan_AmountUnknown"],
        ({ } value, AmountMode.Estimated) => "≈ " + MoneyText.Format(value, currencyCode, culture),
        ({ } value, _) => MoneyText.Format(value, currencyCode, culture),
    };

    /// <summary>The status label of an occurrence.</summary>
    public string Status(OccurrenceView status) => translator[$"Occurrence_{status}"];

    /// <summary>A due date, e.g. "Mon 5 May" plus "today"/"overdue" hints are added by the caller.</summary>
    public string Date(DateOnly date) => dates.Format(date, DateFormatStyle.Long);

    private string DayText(RecurrenceRule rule) =>
        rule.DayRule == MonthDayRule.LastDayOfMonth
            ? translator["Rule_LastDay"]
            : translator.Format("Rule_OnDay", rule.Calendar == PeriodCalendar.Persian ? Persian.GetDayOfMonth(rule.Start.ToDateTime(TimeOnly.MinValue)) : rule.Start.Day);

    private string YearDayText(RecurrenceRule rule) =>
        rule.DayRule == MonthDayRule.LastDayOfMonth
            ? translator["Rule_LastDay"]
            : translator.Format("Rule_OnDate", dates.Format(rule.Start, DateFormatStyle.DayMonth));
}
