using System.Globalization;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Presentation;

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

        if (rule.SecondDay is { } second)
        {
            text += " · " + translator.Format("Rule_AlsoOnDay", second);
        }

        if (rule.WeekendShift != WeekendShift.None && (rule.WeekendDays != 0 || rule.HolidayRegion is not null))
        {
            text += " · " + translator[rule.WeekendShift == WeekendShift.Before ? "Weekend_BeforeShort" : "Weekend_AfterShort"];
            if (rule.HolidayRegion is not null)
            {
                text += " · " + translator["Holiday_Short"];
            }
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
        ({ } value, AmountMode.Estimated) => MoneyText.Format(value, currencyCode, culture, approximate: true),
        ({ } value, _) => MoneyText.Format(value, currencyCode, culture),
    };

    /// <summary>The status label of an occurrence.</summary>
    public string Status(OccurrenceView status) => translator[$"Occurrence_{status}"];

    /// <summary>The day number and the month name of a date for a date tile, in the display calendar.</summary>
    public (string Day, string Month) DayAndMonth(DateOnly date)
    {
        var parts = dates.Format(date, DateFormatStyle.DayMonth).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var dayPart = parts.FirstOrDefault(p => p.Any(char.IsDigit)) ?? string.Empty;
        var month = string.Join(' ', parts.Where(p => !ReferenceEquals(p, dayPart))).Trim(',', '.');
        if (!culture.TextInfo.IsRightToLeft && month.Length > 4)
        {
            month = month[..3];
        }

        return (new string([.. dayPart.Where(char.IsDigit)]), month);
    }

    /// <summary>The weekday of a date, e.g. "Friday".</summary>
    public string Weekday(DateOnly date) => culture.DateTimeFormat.GetDayName(date.DayOfWeek);

    /// <summary>A due date, e.g. "Mon 5 May" plus "today"/"overdue" hints are added by the caller.</summary>
    public string Date(DateOnly date) => dates.Format(date, DateFormatStyle.Long);

    private string DayText(RecurrenceRule rule) =>
        rule.DayRule.IsWeekday() ? WeekdayText(rule)
        : rule.DayRule == MonthDayRule.LastDayOfMonth
            ? translator["Rule_LastDay"]
            : translator.Format("Rule_OnDay", rule.Calendar == PeriodCalendar.Persian ? Persian.GetDayOfMonth(rule.Start.ToDateTime(TimeOnly.MinValue)) : rule.Start.Day);

    // "on the 2nd Monday" or "on the last Friday"; a start in the fifth week is the last weekday (REC-12).
    private string WeekdayText(RecurrenceRule rule)
    {
        var weekday = culture.DateTimeFormat.GetDayName(rule.Start.DayOfWeek);
        var day = rule.Calendar == PeriodCalendar.Persian ? Persian.GetDayOfMonth(rule.Start.ToDateTime(TimeOnly.MinValue)) : rule.Start.Day;
        var week = MonthDayRules.WeekOf(day);
        return rule.DayRule == MonthDayRule.NthWeekday && week <= 4
            ? translator.Format("Rule_NthWeekday", translator[$"Ordinal_{week}"], weekday)
            : translator.Format("Rule_LastWeekday", weekday);
    }

    // A yearly weekday rule names the month through the start date, e.g. "on the 4th Thursday (26 November)".
    private string YearDayText(RecurrenceRule rule) =>
        rule.DayRule.IsWeekday() ? translator.Format("Rule_YearWeekday", WeekdayText(rule), dates.Format(rule.Start, DateFormatStyle.DayMonth))
        : rule.DayRule == MonthDayRule.LastDayOfMonth
            ? translator["Rule_LastDay"]
            : translator.Format("Rule_OnDate", dates.Format(rule.Start, DateFormatStyle.DayMonth));
}
