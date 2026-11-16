namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// A page is the answer for one visitor at one reading position. Nothing keeps it – not a cache on the way and not
/// the browser: when a visitor moves back in the story and then goes back in the browser, the page is asked for
/// again and shows what the reading position allows now, not what it allowed before (M0-06).
/// The token of a form on a page has the same effect as long as every page has a form; this rule does not depend
/// on that.
/// </summary>
public static class FreshPages
{
    /// <summary>Marks every page the site answers with as not to be stored. Files of the site keep their own rules.</summary>
    public static IApplicationBuilder UseFreshPages(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var response = context.Response;
                if (response.ContentType is { } type
                    && type.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
                    && response.Headers.CacheControl.Count == 0)
                {
                    response.Headers.CacheControl = "no-store";
                }

                return Task.CompletedTask;
            });
            return next(context);
        });
    }
}
