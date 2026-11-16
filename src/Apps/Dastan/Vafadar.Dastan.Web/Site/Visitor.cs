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
    private const string SourceLanguage = "en";

    /// <summary>Initializes a new instance of the <see cref="Visitor"/> class from the current request.</summary>
    public Visitor(IHttpContextAccessor accessor, DastanSite site)
    {
        var context = accessor.HttpContext ?? throw new InvalidOperationException("A visitor exists during a request only.");
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

    /// <summary>Returns a text in the language of the page, or in the language of the collection when it has no translation.</summary>
    public ResolvedText Text(LocalizedText text) => text.Resolve(Language.Tag, SourceLanguage);

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
        var saved = context.Request.Cookies[CookieName(site.Collection)];
        return int.TryParse(saved, NumberStyles.None, CultureInfo.InvariantCulture, out var level) && level < site.StageCount
            ? new ReadingLevel(level)
            : site.DefaultLevel;
    }

    /// <summary>
    /// Maps the endpoint the reading control posts to: the fields <c>collection</c>, <c>level</c> and
    /// <c>returnUrl</c>. Moving forward and moving back are the same explicit act of the visitor.
    /// </summary>
    public static IEndpointConventionBuilder MapReadingPreference(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/preferences/reading", (
            HttpContext context,
            DastanSite site,
            [FromForm] string? collection,
            [FromForm] string? level,
            [FromForm] string? returnUrl) =>
        {
            if (collection != site.Collection
                || !int.TryParse(level, NumberStyles.None, CultureInfo.InvariantCulture, out var chosen)
                || chosen >= site.StageCount)
            {
                return Results.BadRequest();
            }

            if (chosen == site.DefaultLevel.Value)
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
