namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// What a visitor gets when a form is refused – the page it came from was open too long, or what it sent is not
/// something the site offers. A bare status would leave them on an empty page; they are sent to a page that says
/// what happened and that nothing was changed.
/// </summary>
public static class RefusedForms
{
    /// <summary>The address of the page that explains a refused form, without a language.</summary>
    public const string Page = "/refused";

    /// <summary>Sends a visitor whose form was refused to the page that says so, in their language.</summary>
    public static IApplicationBuilder UseRefusedFormPage(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use(async (context, next) =>
        {
            var refused = false;
            try
            {
                await next(context);
                refused = context.Response.StatusCode == StatusCodes.Status400BadRequest;
            }
            catch (BadHttpRequestException) when (HttpMethods.IsPost(context.Request.Method) && !context.Response.HasStarted)
            {
                // While developing, the framework reports a form it cannot read – one without its token, for
                // example – as an error instead of a status. For the visitor it is the same refusal.
                refused = true;
            }

            // "See other": the browser asks for the page instead of sending the form again. The page is addressed
            // without a language, so the site answers it in the language of the visitor's last page.
            if (refused && HttpMethods.IsPost(context.Request.Method) && !context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.Headers.Location = Page;
                context.Response.StatusCode = StatusCodes.Status303SeeOther;
            }
        });
    }
}
