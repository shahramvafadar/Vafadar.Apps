using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Vafadar.Dastan.Core;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>What a visitor can keep in their list: a name, an image or video, a thread.</summary>
public enum SavedKind
{
    /// <summary>The page of an entity.</summary>
    Entity = 0,

    /// <summary>The page of an image or a video.</summary>
    Media = 1,

    /// <summary>The page of a thread.</summary>
    Thread = 2,
}

/// <summary>One page a visitor kept: the collection, what kind of page, and which.</summary>
/// <param name="Collection">The name of the collection in addresses.</param>
/// <param name="Kind">What kind of page.</param>
/// <param name="Id">The identifier of what the page is about.</param>
public sealed record SavedPage(string Collection, SavedKind Kind, string Id)
{
    /// <summary>Gets the address of the page inside its language.</summary>
    public string Address => $"{Collection}/{Kind.ToString().ToLowerInvariant()}/{Id}";
}

/// <summary>
/// The list of a visitor (step 15.1, B-016): the pages they chose to keep, remembered in a cookie on their own
/// device. The site has no account and no store for it – nothing of the list is kept on the server, and nobody
/// but the browser that holds the cookie can read it. A list is small on purpose: it has to fit into a cookie.
/// </summary>
public static class SavedList
{
    /// <summary>The name of the cookie.</summary>
    public const string CookieName = "dastan.saved";

    /// <summary>The address the forms of the list are sent to.</summary>
    public const string Address = "/preferences/saved";

    /// <summary>The largest number of pages in a list.</summary>
    public const int Most = 30;

    // A cookie holds about four thousand characters; the list stays well below.
    private const int Longest = 3000;

    /// <summary>Returns the list the request carries, the page kept last at the end. What is no entry of a list is passed over.</summary>
    public static ImmutableArray<SavedPage> Read(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var written = context.Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(written) || written.Length > Longest)
        {
            return [];
        }

        return [.. written.Split('|').Select(Parse).OfType<SavedPage>().Distinct().Take(Most)];
    }

    /// <summary>Returns whether a page is in the list of the request.</summary>
    public static bool Has(HttpContext context, SavedPage page) => Read(context).Contains(page);

    /// <summary>Returns what a visitor may see of a kept page in a projection, or <see langword="false"/>: a list never shows more than its pages do.</summary>
    public static bool IsShown(Projection projection, SavedPage page)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(page);
        return page.Kind switch
        {
            SavedKind.Entity => projection.Find(new EntityId(page.Id)) is not null,
            SavedKind.Media => projection.Find(new MediaId(page.Id)) is not null,
            _ => projection.Thread(page.Id) is not null,
        };
    }

    /// <summary>
    /// Maps the endpoint of the forms: the fields <c>collection</c>, <c>kind</c>, <c>id</c>, <c>act</c> (<c>keep</c>
    /// or <c>drop</c>) and <c>returnUrl</c>. Keeping is refused for a page the visitor cannot see.
    /// </summary>
    public static IEndpointConventionBuilder MapSavedList(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(Address, (
            HttpContext context,
            DastanLibrary library,
            Visitor visitor,
            [FromForm] string? collection,
            [FromForm] string? kind,
            [FromForm] string? id,
            [FromForm] string? act,
            [FromForm] string? returnUrl) =>
        {
            if (Parse($"{collection}/{kind}/{id}") is not { } page || act is not ("keep" or "drop"))
            {
                return Results.BadRequest();
            }

            var list = Read(context).Remove(page);
            if (act == "keep")
            {
                // Only a page this visitor can open is kept: the list is no way around the reading position.
                if (library.Find(page.Collection) is not { } site || visitor.Of(site) is not { } projection || !IsShown(projection, page))
                {
                    return Results.BadRequest();
                }

                // A full list lets its oldest page go.
                list = [.. list.Add(page).TakeLast(Most)];
            }

            if (list.IsEmpty)
            {
                context.Response.Cookies.Delete(CookieName, PreferenceCookie.Options(context));
            }
            else
            {
                context.Response.Cookies.Append(CookieName, string.Join('|', list.Select(Write)), PreferenceCookie.Options(context));
            }

            return Results.Redirect(ReturnAddress.Safe(returnUrl));
        });

    private static string Write(SavedPage page) => $"{page.Collection}/{page.Kind.ToString().ToLowerInvariant()}/{page.Id}";

    private static SavedPage? Parse(string entry)
    {
        var parts = entry.Split('/');
        if (parts.Length != 3 || parts.Any(part => part.Length is 0 or > 120 || !part.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')))
        {
            return null;
        }

        SavedKind? kind = parts[1] switch { "entity" => SavedKind.Entity, "media" => SavedKind.Media, "thread" => SavedKind.Thread, _ => null };
        return kind is null ? null : new SavedPage(parts[0], kind.Value, parts[2]);
    }
}
