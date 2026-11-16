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

        // Media carry the day or the year they were made. Other shapes of time are not written by a page yet;
        // saying that the time is not stated here is true, guessing a wording for them would not be.
        return time is Occurrence { At: CalendarPosition position } ? Of(position, visitor, text) : text["Date_NotStated"];
    }

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
