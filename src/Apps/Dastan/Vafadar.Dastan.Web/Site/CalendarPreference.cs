using Microsoft.AspNetCore.Mvc;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The calendar a visitor reads dates in beside the Gregorian one (D-100): Solar Hijri, Lunar Hijri, or none.
/// What is stored is always Gregorian (D-043), so that every date is compared without a conversion; a date is
/// shown as it is stored, and the same date in the calendar of the visitor stands beside it and is said to be
/// converted. Somebody who chose nothing reads in the calendar of the language of the page.
/// </summary>
public static class CalendarPreference
{
    /// <summary>The name of the cookie.</summary>
    public const string CookieName = "dastan.calendar";

    /// <summary>The address the form of the choice posts to.</summary>
    public const string Address = "/preferences/calendar";

    /// <summary>Returns the calendar of the visitor: their choice, else the calendar of the language of the page.</summary>
    public static CalendarSystem Read(HttpContext context, string language)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Parse(context.Request.Cookies[CookieName]) ?? CalendarConversion.Of(language);
    }

    /// <summary>Returns the word a calendar is chosen and remembered by.</summary>
    public static string Word(CalendarSystem calendar) => calendar switch
    {
        CalendarSystem.SolarHijri => "solar-hijri",
        CalendarSystem.LunarHijri => "lunar-hijri",
        _ => "gregorian",
    };

    /// <summary>Reads "gregorian", "solar-hijri" or "lunar-hijri"; nothing for anything else.</summary>
    public static CalendarSystem? Parse(string? word) => word switch
    {
        "gregorian" => CalendarSystem.Gregorian,
        "solar-hijri" => CalendarSystem.SolarHijri,
        "lunar-hijri" => CalendarSystem.LunarHijri,
        _ => null,
    };

    /// <summary>
    /// Maps the endpoint the form of the choice posts to: the fields <c>calendar</c> and <c>returnUrl</c>. It
    /// remembers the choice and goes back to the page. The form needs an antiforgery token.
    /// </summary>
    public static IEndpointConventionBuilder MapCalendarPreference(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.MapPost(Address, (HttpContext context, [FromForm] string? calendar, [FromForm] string? returnUrl) =>
        {
            if (Parse(calendar) is not { } chosen)
            {
                return Results.BadRequest();
            }

            context.Response.Cookies.Append(CookieName, Word(chosen), PreferenceCookie.Options(context));
            return Results.Redirect(ReturnAddress.Safe(returnUrl));
        });
    }
}
