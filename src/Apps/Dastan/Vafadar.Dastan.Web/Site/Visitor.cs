using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Vafadar.Core.Text;
using Vafadar.Dastan.Core;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The visitor of one request: their language, how far they have read and, from that, what they may see. Every
/// request builds its own; nothing of a visitor is kept on the server.
/// </summary>
public sealed class Visitor
{
    private readonly string _source;
    private readonly HttpContext _context;

    /// <summary>Initializes a new instance of the <see cref="Visitor"/> class from the current request.</summary>
    public Visitor(IHttpContextAccessor accessor, DastanSite site)
    {
        var context = accessor.HttpContext ?? throw new InvalidOperationException("A visitor exists during a request only.");
        ArgumentNullException.ThrowIfNull(site);
        _source = site.Language;
        _context = context;
        Language = context.GetSiteLanguage();
        Theme = ThemePreference.Read(context);
        Boundary = ReadingPreference.Read(context, site);
        Projection = site.At(Boundary);
    }

    /// <summary>Gets the language of the page.</summary>
    public SiteLanguage Language { get; }

    /// <summary>Gets the appearance the visitor chose.</summary>
    public ThemeChoice Theme { get; }

    /// <summary>Gets the stage the visitor has read to.</summary>
    public ReadingLevel Boundary { get; }

    /// <summary>Gets what the visitor may see, or <see langword="null"/> when nothing is released to the site's audience.</summary>
    public Projection? Projection { get; }

    /// <summary>Returns what the visitor may see of another collection of the site, at the stage they have read it to.</summary>
    public Projection? Of(DastanSite collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        return collection.At(ReadingPreference.Read(_context, collection));
    }

    /// <summary>Returns a text in the language of the page, or in the language of the collection when it has no translation.</summary>
    public ResolvedText Text(LocalizedText text) => text.Resolve(Language.Tag, _source);

    /// <summary>Returns a number as the page shows it: with Persian digits on a Persian page.</summary>
    public string Number(int value)
    {
        var digits = value.ToString(CultureInfo.InvariantCulture);
        return Language.Tag == "fa" ? Digits.ToPersian(digits) : digits;
    }
}

/// <summary>
/// How far a visitor has read, remembered in a cookie of its own per collection. The cookie belongs to the visitor:
/// an address never carries a reading position, so a link somebody sends cannot reveal more than the receiver has
/// read (decision 5 of the delivery plan).
/// </summary>
public static class ReadingPreference
{
    /// <summary>Returns the name of the cookie of a collection.</summary>
    public static string CookieName(string collection) => $"dastan.reading.{collection}";

    /// <summary>Returns the stage the visitor has read to; the starting stage when they chose none or chose one that does not exist.</summary>
    public static ReadingLevel Read(HttpContext context, DastanSite site)
    {
        // Who signed in has read as far as their account says, on every device (step 16.2); what the device
        // holds counts for who is not signed in.
        var level = AccountState.Of(context).Belongings is { } follows
            ? follows.Positions.TryGetValue(site.Collection, out var kept) ? kept : (int?)null
            : Device(context, site);
        return level is { } read && read < site.StageCount ? new ReadingLevel(read) : site.DefaultLevel;
    }

    /// <summary>Returns the stage the device of a request chose for a collection, or nothing when it chose none that exists.</summary>
    public static int? Device(HttpContext context, DastanSite site)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(site);
        var saved = context.Request.Cookies[CookieName(site.Collection)];
        return int.TryParse(saved, NumberStyles.None, CultureInfo.InvariantCulture, out var level) && level < site.StageCount ? level : null;
    }

    /// <summary>
    /// Maps the endpoint the reading control posts to: the fields <c>collection</c>, <c>level</c> and
    /// <c>returnUrl</c>. Moving forward and moving back are the same explicit act of the visitor.
    /// </summary>
    public static IEndpointConventionBuilder MapReadingPreference(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/preferences/reading", (
            HttpContext context,
            DastanLibrary library,
            AccountDesk desk,
            [FromForm] string? collection,
            [FromForm] string? level,
            [FromForm] string? returnUrl) =>
        {
            if (library.Find(collection) is not { } site
                || !int.TryParse(level, NumberStyles.None, CultureInfo.InvariantCulture, out var chosen)
                || chosen >= site.StageCount)
            {
                return Results.BadRequest();
            }

            if (AccountState.Of(context).Session is { } session)
            {
                // Signed in, the choice is kept with the account; the device keeps what it held.
                using var store = desk.Open();
                store.SetPosition(session.Id, site.Collection, chosen == site.DefaultLevel.Value ? null : chosen);
            }
            else if (chosen == site.DefaultLevel.Value)
            {
                context.Response.Cookies.Delete(CookieName(site.Collection), PreferenceCookie.Options(context));
            }
            else
            {
                context.Response.Cookies.Append(
                    CookieName(site.Collection),
                    chosen.ToString(CultureInfo.InvariantCulture),
                    PreferenceCookie.Options(context));
            }

            return Results.Redirect(ReturnAddress.Safe(returnUrl));
        });
}
