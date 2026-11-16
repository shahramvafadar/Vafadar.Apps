using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Vafadar.Web;

/// <summary>The direction text of a language is written in.</summary>
public static class TextDirection
{
    /// <summary>Returns the value of the <c>dir</c> attribute for a culture: "rtl" or "ltr".</summary>
    public static string Of(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return culture.TextInfo.IsRightToLeft ? "rtl" : "ltr";
    }

    /// <summary>
    /// Returns the value of the <c>dir</c> attribute for a language tag. The direction comes from the language –
    /// Persian, Arabic, Kurdish (Sorani), Hebrew and Urdu are right to left without being listed anywhere.
    /// </summary>
    public static string Of(string language) => Of(CultureInfo.GetCultureInfo(language));
}

/// <summary>
/// The cookies that remember a visitor's choices (language, theme, an app's own). They hold a choice and nothing
/// about the person; they are needed for the site to work as chosen and are never used for tracking.
/// </summary>
public static class PreferenceCookie
{
    /// <summary>Returns the options every preference cookie is written with.</summary>
    public static CookieOptions Options(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            MaxAge = TimeSpan.FromDays(365),
        };
    }
}

/// <summary>Where to go back to after a form was sent.</summary>
public static class ReturnAddress
{
    /// <summary>
    /// Returns the address when it is a path of this site, else the fallback. An address from a form can be written
    /// by anybody, so a full address, a network path ("//host") and anything that is not a plain path are refused.
    /// </summary>
    public static string Safe(string? address, string fallback = "/")
    {
        if (string.IsNullOrEmpty(address)
            || address[0] != '/'
            || (address.Length > 1 && (address[1] == '/' || address[1] == '\\'))
            || address.Any(char.IsControl))
        {
            return fallback;
        }

        return address;
    }
}

/// <summary>Whether a visitor wants the light or the dark appearance, or the one of their device.</summary>
public enum ThemeChoice
{
    /// <summary>Follow the device.</summary>
    Auto = 0,

    /// <summary>Always light.</summary>
    Light = 1,

    /// <summary>Always dark.</summary>
    Dark = 2,
}

/// <summary>
/// The visitor's theme choice. The server writes it into the page (<c>data-theme</c> on the root element), so the
/// right appearance is there with the first paint and no script is needed.
/// </summary>
public static class ThemePreference
{
    /// <summary>The name of the cookie.</summary>
    public const string CookieName = "vafadar.theme";

    /// <summary>Returns the choice of the visitor; <see cref="ThemeChoice.Auto"/> when they made none.</summary>
    public static ThemeChoice Read(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return TryParse(context.Request.Cookies[CookieName], out var choice) ? choice : ThemeChoice.Auto;
    }

    /// <summary>Returns the value of the <c>data-theme</c> attribute, or <see langword="null"/> to follow the device.</summary>
    public static string? Attribute(ThemeChoice choice) => choice switch
    {
        ThemeChoice.Light => "light",
        ThemeChoice.Dark => "dark",
        _ => null,
    };

    /// <summary>Reads "auto", "light" or "dark".</summary>
    public static bool TryParse(string? text, out ThemeChoice choice)
    {
        (var known, choice) = text switch
        {
            "auto" => (true, ThemeChoice.Auto),
            "light" => (true, ThemeChoice.Light),
            "dark" => (true, ThemeChoice.Dark),
            _ => (false, ThemeChoice.Auto),
        };
        return known;
    }

    /// <summary>
    /// Maps the endpoint a theme form posts to: the fields <c>theme</c> ("auto", "light", "dark") and
    /// <c>returnUrl</c>. It remembers the choice and goes back to the page. The form needs an antiforgery token.
    /// </summary>
    public static IEndpointConventionBuilder MapThemePreference(this IEndpointRouteBuilder endpoints, string pattern = "/preferences/theme")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.MapPost(pattern, (HttpContext context, [FromForm] string? theme, [FromForm] string? returnUrl) =>
        {
            if (!TryParse(theme, out var choice))
            {
                return Results.BadRequest();
            }

            if (choice == ThemeChoice.Auto)
            {
                context.Response.Cookies.Delete(CookieName, PreferenceCookie.Options(context));
            }
            else
            {
                context.Response.Cookies.Append(CookieName, Attribute(choice)!, PreferenceCookie.Options(context));
            }

            return Results.Redirect(ReturnAddress.Safe(returnUrl));
        });
    }
}
