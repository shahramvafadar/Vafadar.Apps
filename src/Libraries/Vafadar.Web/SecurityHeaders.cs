using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Vafadar.Web;

/// <summary>The response headers that tell a browser what a page may do.</summary>
public sealed class SecurityHeaderOptions
{
    /// <summary>
    /// Gets or sets the content security policy. The default allows only what the site itself serves: no inline
    /// script or style, no content of other hosts, no framing by other sites, forms that post to the site only.
    /// </summary>
    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; object-src 'none'";

    /// <summary>Gets or sets the device features the pages may use; by default none.</summary>
    public string PermissionsPolicy { get; set; } = "camera=(), geolocation=(), microphone=(), payment=(), usb=()";
}

/// <summary>Registration of the security headers.</summary>
public static class SecurityHeaderExtensions
{
    /// <summary>
    /// Adds the security headers to every response: the content security policy, no guessing of content types, no
    /// framing, a referrer without the path for other sites, and no device features.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, Action<SecurityHeaderOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        var options = new SecurityHeaderOptions();
        configure?.Invoke(options);
        return app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.ContentSecurityPolicy = options.ContentSecurityPolicy;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = options.PermissionsPolicy;
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                return Task.CompletedTask;
            });
            return next(context);
        });
    }
}
