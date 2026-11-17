using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Vafadar.Dastan.Store;
using Vafadar.Web;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>
/// The tool with accounts (step 11.7, <c>Studio:Accounts</c>): it answers other machines, and nothing of it is
/// shown to somebody who has not signed in. An editor signs in with the name and the password of an account of
/// the store; every act is then kept under that name, and no form asks for one.
/// </summary>
public static class StudioAccess
{
    /// <summary>The claim that holds what a sign-in was made with; it stops holding with another password.</summary>
    public const string StampClaim = "dastan.studio.stamp";

    /// <summary>The address of the page that signs in, without the language.</summary>
    public const string SignInPath = "/sign-in";

    /// <summary>The address that signs out.</summary>
    public const string SignOutPath = "/sign-out";

    // What is answered to somebody who has not signed in: the page that signs in, what a page is drawn with, and
    // the pages that say something went wrong.
    private static readonly string[] Open = [SignInPath, "/preferences/theme", "/error", "/not-found", StudioHealth.Address];
    private static readonly string[] Drawn = [".css", ".js", ".svg", ".png", ".ico", ".woff2", ".webmanifest"];

    /// <summary>Adds what signing in needs: the sign-in kept in a cookie, its keys, and the proxies that are believed.</summary>
    public static void AddStudioAccounts(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.Name = "dastan.studio.session";
            options.Cookie.HttpOnly = true;

            // The language is the first part of an address: a sign-in is for the whole tool, not for one language.
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = true;

            // Every request asks the store whether the sign-in still holds: another password or a closed account
            // ends it at once, not when the cookie runs out.
            options.Events.OnValidatePrincipal = async context =>
            {
                var store = context.HttpContext.RequestServices.GetRequiredService<EditorialStore>();
                if (Session(context.Principal) is not { } session || !store.Holds(session))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
                }
            };
        });

        // The keys of the sign-in outlive the process only where a folder for them is named; without one,
        // everybody signs in again after the tool was started anew.
        if (builder.Configuration["Studio:Keys"] is { Length: > 0 } keys)
        {
            builder.Services.AddDataProtection().SetApplicationName("dastan-studio").PersistKeysToFileSystem(new DirectoryInfo(keys));
        }

        // Behind a proxy that holds the certificate, the tool learns from the proxy how the editor came. It
        // believes only the proxies it was told to; told nothing, it believes nobody.
        var proxies = builder.Configuration.GetSection("Studio:TrustedProxies").Get<string[]>() ?? [];
        if (proxies.Length > 0)
        {
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                foreach (var proxy in proxies)
                {
                    if (proxy.Contains('/', StringComparison.Ordinal))
                    {
                        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(proxy));
                    }
                    else
                    {
                        options.KnownProxies.Add(IPAddress.Parse(proxy));
                    }
                }
            });
        }
    }

    /// <summary>
    /// Hears the proxies that are believed, and refuses a request that did not come encrypted from another
    /// machine: a password and what is not published do not travel in the open.
    /// </summary>
    public static void UseEncryptedOnly(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (app.Configuration.GetSection("Studio:TrustedProxies").Get<string[]>() is { Length: > 0 })
        {
            app.UseForwardedHeaders();
        }

        app.Use(async (context, next) =>
        {
            // A request without an address comes from inside the process – the tests of the tool.
            if (!context.Request.IsHttps && context.Connection.RemoteIpAddress is { } address && !IPAddress.IsLoopback(address))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("The tool of the editors is reached over https.").ConfigureAwait(false);
                return;
            }

            await next(context).ConfigureAwait(false);
        });
    }

    /// <summary>Reads who signed in, and sends everybody else to the page that signs in.</summary>
    public static void UseSignedInOnly(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseAuthentication();
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (context.User.Identity?.IsAuthenticated == true
                || Open.Contains(path, StringComparer.OrdinalIgnoreCase)
                || Drawn.Any(kind => path.EndsWith(kind, StringComparison.OrdinalIgnoreCase)))
            {
                await next(context).ConfigureAwait(false);
            }
            else if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            {
                // After signing in the editor arrives where they wanted to go.
                var wanted = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                context.Response.Redirect($"{context.Request.PathBase}{SignInPath}?to={Uri.EscapeDataString(wanted)}");
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
        });
    }

    /// <summary>Maps the address that signs out: a form of the tool, sent with its token.</summary>
    public static void MapSignOut(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapPost(SignOutPath, async (HttpContext context, [FromForm] string? returnUrl) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            return Results.Redirect(ReturnAddress.Safe(returnUrl));
        });
    }

    /// <summary>Signs an editor in for the browser of the request.</summary>
    public static Task SignIn(HttpContext context, EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(session);
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, session.Name), new Claim(StampClaim, session.Stamp)], CookieAuthenticationDefaults.AuthenticationScheme);
        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    /// <summary>Returns the sign-in a request carries, or nothing.</summary>
    public static EditorSession? Session(ClaimsPrincipal? user) =>
        user?.Identity is { IsAuthenticated: true, Name: { Length: > 0 } name } && user.FindFirstValue(StampClaim) is { } stamp ? new EditorSession(name, stamp) : null;
}
