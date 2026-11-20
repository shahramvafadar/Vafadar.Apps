using System.Globalization;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Resources;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// A time as a page writes it. It says what the record says and no more: the era is always named, "about" stays
/// "about", and a date is never moved into another calendar – a Gregorian day on a Persian page is that Gregorian
/// day, written with Persian digits and named as such.
/// </summary>
public static class TimeText
{
    /// <summary>Returns the words for a time of a record.</summary>
    public static string Of(TemporalExpression time, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);

        // A day or a year, a depth of time, a place in a telling; a time that is only known to lie between two;
        // something that lasted; how long something took; and one of several times (ADR 0021). A time that is
        // named by another event is not written here: saying that it is not stated is true, a guess is not.
        return time switch
        {
            Occurrence occurrence => Of(occurrence.At, visitor, text),
            OccurrenceWindow window => Between(window.Earliest, window.Latest, visitor, text),
            Period period when Start(period, visitor, text) is { } start && End(period, visitor, text) is { } end =>
                string.Format(CultureInfo.CurrentCulture, text["Date_Range"], start, end),
            ElapsedTime elapsed => Elapsed(elapsed, visitor, text),
            AlternativeOccurrences alternatives => alternatives.Options.Select(option => Of(option, visitor, text))
                .Aggregate((all, next) => string.Format(CultureInfo.CurrentCulture, text["Date_Or"], all, next)),
            UnknownTime => text["Profile_Unknown"],
            _ => text["Date_NotStated"],
        };
    }

    /// <summary>
    /// Writes a position of time: a date of a calendar, a depth of time with what it is counted from, or a
    /// place in a telling. Nothing is converted: the number, the unit and the present are those of the source.
    /// </summary>
    public static string Of(TimePosition position, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        return position switch
        {
            CalendarPosition calendar => Of(calendar, visitor, text),
            AgePosition age => Of(age, visitor, text),
            NarrativePosition step => string.Format(
                CultureInfo.CurrentCulture, text[step.Order == NarrativeOrder.InWorld ? "Date_StepInWorld" : "Date_Step"], visitor.Number(step.Ordinal)),
            _ => text["Date_NotStated"],
        };
    }

    /// <summary>Writes a depth of time: so many years, thousand, million or billion years ago, or before the year it names.</summary>
    public static string Of(AgePosition age, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(age);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        var years = string.Format(CultureInfo.CurrentCulture, text[$"Age_{age.Unit}"], Number(age.Value, visitor));
        var written = age.Reference switch
        {
            { IsSpecified: false } => string.Format(CultureInfo.CurrentCulture, text["Age_Ago"], years),
            { CommonEraYear: 1950 } => string.Format(CultureInfo.CurrentCulture, text["Age_BeforePresent"], years),
            { CommonEraYear: { } year } => string.Format(CultureInfo.CurrentCulture, text["Age_BeforeYear"], years, Year(Era.CommonEra, year, visitor, text)),
            _ => string.Format(CultureInfo.CurrentCulture, text["Age_Ago"], years),
        };
        if (age.Basis == AgeBasis.RadiocarbonYears)
        {
            written = string.Format(CultureInfo.CurrentCulture, text["Age_Radiocarbon"], written);
        }

        return age.IsApproximate ? string.Format(CultureInfo.CurrentCulture, text["Date_About"], written) : written;
    }

    /// <summary>Writes a time a visitor asked for, in the words of the page (ADR 0021).</summary>
    public static string Of(AskedTime asked, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(asked);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        switch (asked)
        {
            case AskedTime.Years years:
                return years.IsOneYear
                    ? Year(years.First.Era, years.First.Number, visitor, text)
                    : string.Format(CultureInfo.CurrentCulture, text["Date_Range"], Year(years.First.Era, years.First.Number, visitor, text), Year(years.Last.Era, years.Last.Number, visitor, text));
            case AskedTime.Ago ago:
                var (size, unit) = (ago.UnitSize, ago.Unit);
                string Depth(decimal depth) => string.Format(
                    CultureInfo.CurrentCulture, text["Age_Ago"], string.Format(CultureInfo.CurrentCulture, text[$"Age_{unit}"], Number(depth / size, visitor)));
                return ago.Oldest == ago.Youngest
                    ? Depth(ago.Oldest)
                    : string.Format(CultureInfo.CurrentCulture, text["Date_Between"], Depth(ago.Youngest), Depth(ago.Oldest));
            default:
                return text["Date_NotStated"];
        }
    }

    // A number with a part after the point, in the digits of the visitor.
    private static string Number(decimal value, Visitor visitor)
    {
        var digits = value.ToString("0.######", CultureInfo.InvariantCulture);
        return visitor.Language.Tag == "fa" ? Vafadar.Core.Text.Digits.ToPersian(digits).Replace('.', '٫') : digits;
    }

    private static string Elapsed(ElapsedTime elapsed, Visitor visitor, IStringLocalizer<Ui> text)
    {
        var written = string.Format(CultureInfo.CurrentCulture, text[$"Date_Elapsed_{elapsed.Unit}"], Number(elapsed.Value, visitor));
        return elapsed.IsApproximate ? string.Format(CultureInfo.CurrentCulture, text["Date_About"], written) : written;
    }

    /// <summary>
    /// Returns a year as a chart writes it on its axis and its bars: the number and the short name of its era.
    /// The era is named here too – a bare number on an axis that crosses the era would be read as the wrong year.
    /// </summary>
    public static string Year(Era era, int year, Visitor visitor, IStringLocalizer<Ui> text, bool approximate = false)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        var written = string.Format(CultureInfo.CurrentCulture, text["Year_Short"], visitor.Number(year), text[$"Era_Short_{era}"]);
        return approximate ? string.Format(CultureInfo.CurrentCulture, text["Year_About"], written) : written;
    }

    private static string? Start(Period period, Visitor visitor, IStringLocalizer<Ui> text) => period.Start switch
    {
        { State: PeriodStartState.Unknown } => text["Date_StartUnknown"],
        { Earliest: { } earliest, Latest: { } latest } => Between(earliest, latest, visitor, text),
        _ => null,
    };

    private static string? End(Period period, Visitor visitor, IStringLocalizer<Ui> text) => period.End switch
    {
        { State: PeriodEndState.Ongoing } => text["Date_Ongoing"],
        { State: PeriodEndState.EndedAtUnknownTime } => text["Date_EndedUnknown"],
        { State: PeriodEndState.Unknown } => text["Date_EndUnknown"],
        { Earliest: { } earliest, Latest: { } latest } => Between(earliest, latest, visitor, text),
        _ => null,
    };

    // One position when both ends of what is known are the same, "between" otherwise.
    private static string Between(TimePosition earliest, TimePosition latest, Visitor visitor, IStringLocalizer<Ui> text) =>
        earliest == latest
            ? Of(earliest, visitor, text)
            : string.Format(CultureInfo.CurrentCulture, text["Date_Between"], Of(earliest, visitor, text), Of(latest, visitor, text));

    /// <summary>Returns the words for a day on which the editors did something, which is a Gregorian day.</summary>
    public static string Of(DateOnly day, Visitor visitor, IStringLocalizer<Ui> text) =>
        Of(CalendarPosition.OfDay(CalendarSystem.Gregorian, Era.CommonEra, day.Year, day.Month, day.Day), visitor, text);

    /// <summary>Returns the words for a position in a calendar.</summary>
    public static string Of(CalendarPosition position, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);

        var year = visitor.Number(position.Year);

        // The months of the common era have names in both languages of the site. A month of another calendar is
        // written as its number rather than under a name the site has not been given.
        var named = position.Era is Era.CommonEra or Era.BeforeCommonEra;
        var date = position switch
        {
            { Month: { } month, Day: { } day } when named =>
                string.Format(CultureInfo.CurrentCulture, text["Date_Day"], visitor.Number(day), text[$"Month_{month.ToString(CultureInfo.InvariantCulture)}"], year),
            { Month: { } month, Day: { } day } => $"{year}/{visitor.Number(month)}/{visitor.Number(day)}",
            { Month: { } month } when named =>
                string.Format(CultureInfo.CurrentCulture, text["Date_Month"], text[$"Month_{month.ToString(CultureInfo.InvariantCulture)}"], year),
            { Month: { } month } => $"{year}/{visitor.Number(month)}",
            _ => year,
        };

        var withEra = string.Format(CultureInfo.CurrentCulture, text["Date_WithEra"], date, text[$"Era_{position.Era}"]);
        return position.IsApproximate ? string.Format(CultureInfo.CurrentCulture, text["Date_About"], withEra) : withEra;
    }
}
