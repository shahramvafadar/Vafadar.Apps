using System.Globalization;
using Microsoft.Extensions.Localization;
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

        // A day or a year, a time that is only known to lie between two, and something that lasted. Other shapes
        // of time are not written by a page yet; saying that the time is not stated here is true, guessing a
        // wording for them would not be.
        return time switch
        {
            Occurrence { At: CalendarPosition position } => Of(position, visitor, text),
            OccurrenceWindow { Earliest: CalendarPosition earliest, Latest: CalendarPosition latest } => Between(earliest, latest, visitor, text),
            Period period when Start(period, visitor, text) is { } start && End(period, visitor, text) is { } end =>
                string.Format(CultureInfo.CurrentCulture, text["Date_Range"], start, end),
            _ => text["Date_NotStated"],
        };
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
        { Earliest: CalendarPosition earliest, Latest: CalendarPosition latest } => Between(earliest, latest, visitor, text),
        _ => null,
    };

    private static string? End(Period period, Visitor visitor, IStringLocalizer<Ui> text) => period.End switch
    {
        { State: PeriodEndState.Ongoing } => text["Date_Ongoing"],
        { State: PeriodEndState.EndedAtUnknownTime } => text["Date_EndedUnknown"],
        { State: PeriodEndState.Unknown } => text["Date_EndUnknown"],
        { Earliest: CalendarPosition earliest, Latest: CalendarPosition latest } => Between(earliest, latest, visitor, text),
        _ => null,
    };

    // One position when both ends of what is known are the same, "between" otherwise.
    private static string Between(CalendarPosition earliest, CalendarPosition latest, Visitor visitor, IStringLocalizer<Ui> text) =>
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
