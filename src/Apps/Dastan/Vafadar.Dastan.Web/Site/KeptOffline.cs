namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// Which pages a device may keep for a time without a connection (D-064, step 17.5): the public pages of the
/// collections and comparisons, as a visitor who is not signed in was given them. The site says so with a
/// header on the answer, and the service worker keeps only what carries it. A page of an account, of a person,
/// of a family tree, a photograph, a search or a list is never marked – and neither is any page for a visitor
/// who is signed in, whose name stands on every page.
/// </summary>
public static class KeptOffline
{
    /// <summary>The header that says an answer may be kept.</summary>
    public const string Header = "Dastan-Keep";

    /// <summary>What the header says for a page.</summary>
    public const string Page = "page";

    /// <summary>Marks the answers that may be kept. It stands after the language was taken out of the path and after the visitor is known.</summary>
    public static IApplicationBuilder UseKeptOffline(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use((context, next) =>
        {
            // An address with a question – what somebody searched for, the word of thanks after a form – is a
            // moment of one visitor and not a page: it is not kept.
            if (HttpMethods.IsGet(context.Request.Method) && !context.Request.QueryString.HasValue && IsPublicContent(context.Request.Path))
            {
                context.Response.OnStarting(() =>
                {
                    // Asked when the answer is known: a page that was found, for somebody who is not signed in.
                    if (context.Response.StatusCode == StatusCodes.Status200OK
                        && context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true
                        && AccountDesk.Session(context.User) is null)
                    {
                        context.Response.Headers[Header] = Page;
                    }

                    return Task.CompletedTask;
                });
            }

            return next(context);
        });
    }

    /// <summary>
    /// Returns whether a path, without its language, is a page of public content: the start page, a page of a
    /// collection or of a comparison. Every other first part of an address is something of the site itself,
    /// in whatever case it is written – the site finds its pages without looking at the case. The search of a
    /// collection is not such a page: what it shows is what somebody asked.
    /// </summary>
    public static bool IsPublicContent(PathString path)
    {
        var parts = (path.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
        var first = parts.FirstOrDefault();
        return first is null
            || string.Equals(first, "compare", StringComparison.OrdinalIgnoreCase)
            || (!DastanLibrary.IsTaken(first) && !first.StartsWith('_') && !first.Contains('.', StringComparison.Ordinal)
                && !parts.Skip(1).Any(part => string.Equals(part, "search", StringComparison.OrdinalIgnoreCase)));
    }
}
