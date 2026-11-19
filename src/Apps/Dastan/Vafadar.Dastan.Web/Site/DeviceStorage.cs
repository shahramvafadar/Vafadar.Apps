using Microsoft.AspNetCore.Mvc;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// What the site puts on the device of a visitor, and what it asks before it does (the page "Cookies", and
/// § 25 TDDDG, which carries Article 5(3) of Directive 2002/58/EC): the site stores only what the service a
/// visitor asked for cannot do without – the language of a page, a choice the visitor made, a sign-in – and
/// that needs no consent. Two things are more than that and are done only when the visitor says so:
/// keeping pages on the device to read them without a connection, and nothing else.
/// </summary>
public static class DeviceStorage
{
    /// <summary>The cookie that says the visitor has read the note about what the site stores.</summary>
    public const string NoticeCookie = "dastan.notice";

    /// <summary>The cookie that says the visitor wants pages kept on the device for a time without a connection.</summary>
    public const string OfflineCookie = "dastan.offline";

    /// <summary>The version of the note: a note that says something new is shown again.</summary>
    public const string NoticeVersion = "1";

    /// <summary>Returns whether the visitor has seen the note about what the site stores, in its present version.</summary>
    public static bool HasSeenNotice(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Request.Cookies[NoticeCookie] == NoticeVersion;
    }

    /// <summary>Returns whether the visitor agreed to pages being kept on the device.</summary>
    public static bool KeepsOffline(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Request.Cookies[OfflineCookie] == "on";
    }

    /// <summary>Maps the two forms: the note that was read, and keeping pages on the device turned on or off.</summary>
    public static IEndpointRouteBuilder MapDeviceStorage(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/preferences/storage-note", (HttpContext context, [FromForm] string? returnUrl) =>
        {
            context.Response.Cookies.Append(NoticeCookie, NoticeVersion, PreferenceCookie.Options(context));
            return Results.Redirect(ReturnAddress.Safe(returnUrl));
        });

        endpoints.MapPost("/preferences/offline", (HttpContext context, [FromForm] string? answer, [FromForm] string? returnUrl) =>
        {
            switch (answer)
            {
                case "on":
                    context.Response.Cookies.Append(OfflineCookie, "on", PreferenceCookie.Options(context));
                    break;
                case "off":
                    // Taken back as easily as it was given. The pages the device kept are let go by the script of
                    // the site, which finds the worker turned off; the worker itself lets go of them on this form.
                    context.Response.Cookies.Delete(OfflineCookie, PreferenceCookie.Options(context));
                    break;
                default:
                    return Results.BadRequest();
            }

            return Results.Redirect(ReturnAddress.Safe(returnUrl));
        });
        return endpoints;
    }
}
