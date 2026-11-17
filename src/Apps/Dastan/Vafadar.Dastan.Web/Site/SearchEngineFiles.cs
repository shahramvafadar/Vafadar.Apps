using System.Security;
using System.Text;
using Microsoft.Extensions.Options;
using Vafadar.Dastan.Core;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// What a search engine asks a site for (step 14.4): <c>robots.txt</c> and, for a public site, the list of its
/// pages. A demonstration tells every engine to stay away and has no list. The list names what a visitor sees
/// who has read nothing yet – a page of a later reading stage is in no list.
/// </summary>
public static class SearchEngineFiles
{
    /// <summary>The address of the rules for search engines.</summary>
    public const string Robots = "/robots.txt";

    /// <summary>The address of the list of the pages.</summary>
    public const string Sitemap = "/sitemap.xml";

    /// <summary>Returns the address the site was reached by, without a path: <c>https://dastan.example</c>.</summary>
    public static string Origin(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}";
    }

    /// <summary>Returns the addresses of the pages of the site, each without its language, for a visitor who has read nothing yet.</summary>
    public static IReadOnlyList<string> Pages(DastanSite site)
    {
        ArgumentNullException.ThrowIfNull(site);
        var pages = new List<string> { string.Empty };
        if (site.At(ReadingLevel.Opening) is { } projection)
        {
            var collection = site.Collection;
            pages.Add(collection);
            pages.AddRange(projection.Stories.Select((story, index) => index == 0 ? $"{collection}/story" : $"{collection}/story/{story}"));
            pages.Add($"{collection}/gallery");
            if (!projection.Timeline().IsEmpty)
            {
                pages.Add($"{collection}/timeline");
            }

            if (!projection.Places().IsEmpty)
            {
                pages.Add($"{collection}/places");
            }

            if (projection.Years() is not null)
            {
                pages.Add($"{collection}/at");
            }

            if (site.Revision is not null)
            {
                pages.Add($"{collection}/revisions");
            }

            pages.AddRange(projection.Entities.Select(entity => $"{collection}/entity/{entity.Id}"));
            pages.AddRange(projection.Media.Select(item => $"{collection}/media/{item.Id}"));
        }

        pages.AddRange(site.Comparisons.Select(comparison => $"compare/{comparison.Id}"));
        return pages;
    }

    /// <summary>Adds both addresses to the site.</summary>
    public static IEndpointRouteBuilder MapSearchEngineFiles(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet(Robots, (HttpContext context, DastanSite site) =>
        {
            // The forms of the preferences change something and show nothing; no engine has business there.
            var rules = site.Audience == Audience.Public
                ? $"User-agent: *\nAllow: /\nDisallow: /preferences/\n\nSitemap: {Origin(context)}{Sitemap}\n"
                : "User-agent: *\nDisallow: /\n";
            return Results.Text(rules, "text/plain", Encoding.UTF8);
        });

        endpoints.MapGet(Sitemap, (HttpContext context, DastanSite site, IOptions<SiteLanguageOptions> languages) =>
        {
            if (site.Audience != Audience.Public)
            {
                return Results.NotFound();
            }

            // Every page in every language of the site, each naming the same page in the other languages.
            var origin = Origin(context);
            var tags = languages.Value.Languages;
            var list = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">\n");
            foreach (var page in Pages(site))
            {
                foreach (var tag in tags)
                {
                    list.Append("  <url><loc>").Append(Address(origin, tag, page)).Append("</loc>");
                    foreach (var other in tags)
                    {
                        list.Append("<xhtml:link rel=\"alternate\" hreflang=\"").Append(other).Append("\" href=\"").Append(Address(origin, other, page)).Append("\"/>");
                    }

                    list.Append("</url>\n");
                }
            }

            list.Append("</urlset>\n");
            return Results.Text(list.ToString(), "application/xml", Encoding.UTF8);
        });
        return endpoints;
    }

    private static string Address(string origin, string language, string page) =>
        SecurityElement.Escape($"{origin}/{language}/{string.Join('/', page.Split('/').Select(Uri.EscapeDataString))}")!;
}
