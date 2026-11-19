using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>Private browser administration for the isolated synthetic pilot.</summary>
internal static class AiPilotAdmin
{
    private const string Panel = "/admin/analysis";
    internal const string LoginPath = Panel + "/login";
    private const string Scheme = "Zanance.Analysis.Operator";
    private const string CsrfField = "__RequestVerificationToken";
    private sealed record Operator(string UserId);

    /// <summary>Enables a short-lived browser session for one explicitly configured existing identity.</summary>
    internal static void Add(IServiceCollection services, bool pilot, string? operatorId)
    {
        if (string.IsNullOrEmpty(operatorId)) return;
        if (!pilot || operatorId.Length > 256 || operatorId.Any(char.IsWhiteSpace))
            throw new InvalidOperationException("The analysis operator panel requires an isolated pilot and an explicit user ID.");

        services.AddSingleton(new Operator(operatorId));
        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "__Host-ZananceAnalysisCsrf";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddAuthentication().AddCookie(Scheme, options =>
        {
            options.Cookie.Name = "__Host-ZananceAnalysisOperator";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.Redirect(LoginPath);
                return Task.CompletedTask;
            };
            options.Events.OnValidatePrincipal = async context =>
            {
                var services = context.HttpContext.RequestServices;
                var users = services.GetRequiredService<UserManager<ServiceUser>>();
                var principal = context.Principal!;
                var configured = services.GetRequiredService<Operator>();
                if (IsLoopback(context.HttpContext) && principal.FindFirstValue(ClaimTypes.NameIdentifier) == configured.UserId &&
                    await users.FindByIdAsync(configured.UserId) is { EmailConfirmed: true, TwoFactorEnabled: false } user &&
                    !await users.IsLockedOutAsync(user) && user.SecurityStamp is { } stamp &&
                    principal.FindFirstValue("zanance:operator-stamp") == stamp) return;
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(Scheme);
            };
        });
    }

    /// <summary>Maps the form endpoints only for the configured pilot operator.</summary>
    internal static void Map(WebApplication app)
    {
        if (app.Services.GetService<Operator>() is null) return;
        var pages = app.MapGroup(Panel);
        pages.AddEndpointFilter(async (invocation, next) =>
        {
            var http = invocation.HttpContext;
            if (!IsLoopback(http)) return Results.NotFound();
            http.Response.Headers.CacheControl = "no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers["Referrer-Policy"] = "no-referrer";
            if (HttpMethods.IsPost(http.Request.Method) &&
                (!http.Request.HasFormContentType || http.Request.ContentLength is not (> 0 and <= 4096) ||
                !await http.RequestServices.GetRequiredService<IAntiforgery>().IsRequestValidAsync(http)))
                return Results.BadRequest("The form expired or is invalid. Reload the panel and try again.");
            return await next(invocation);
        });
        pages.MapGet("/login", (HttpContext http, IAntiforgery csrf) => LoginPage(http, csrf));
        pages.MapPost("/login", LoginAsync);
        var operations = pages.MapGroup("").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = Scheme });
        operations.MapGet("", ShowAsync);
        operations.MapGet("/pending", PendingAsync);
        operations.MapPost("/selection", SelectAsync);
        operations.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(Scheme);
            return Results.Redirect(LoginPath);
        });
    }

    private static bool IsLoopback(HttpContext http) => http.Request.IsHttps &&
        (http.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || http.Request.Host.Host == "127.0.0.1") &&
        http.Connection.RemoteIpAddress is { } remote && IPAddress.IsLoopback(remote);

    private static async Task<IResult> LoginAsync(HttpContext http, IAntiforgery csrf, Operator configured,
        UserManager<ServiceUser> users, SignInManager<ServiceUser> signIn, ServiceDbContext db, CancellationToken ct)
    {
        var form = await http.Request.ReadFormAsync(ct);
        if (Field(form, "email") is not { Length: > 0 and <= 254 } email ||
            Field(form, "password") is not { Length: > 0 and <= 256 } password ||
            await users.FindByIdAsync(configured.UserId) is not { } user)
            return LoginPage(http, csrf, 401);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await Sync.SyncRequestScope.LockOwnerAsync(db, user.Id, ct);
        await db.Entry(user).ReloadAsync(ct);
        if (db.Entry(user).State == EntityState.Detached || !user.EmailConfirmed || user.TwoFactorEnabled ||
            user.NormalizedEmail != users.NormalizeEmail(email) ||
            !(await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded || user.SecurityStamp is not { } stamp)
        {
            await transaction.CommitAsync(ct);
            return LoginPage(http, csrf, 401);
        }
        await transaction.CommitAsync(ct);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim("zanance:operator-stamp", stamp)], Scheme));
        await http.SignInAsync(Scheme, principal);
        return Results.Redirect(Panel);
    }

    private static async Task<IResult> SelectAsync(HttpContext http, AiPilotProvider pilot, ServiceDbContext db, CancellationToken ct)
    {
        var form = await http.Request.ReadFormAsync(ct);
        AiPurpose? purpose = Field(form, "purpose") switch
        {
            "report" => AiPurpose.PeriodExplanation,
            "comparison" => AiPurpose.PeriodComparison,
            _ => null,
        };
        if (purpose is null || Field(form, "model") is not { Length: > 0 and <= 64 } model ||
            !long.TryParse(Field(form, "revision"), NumberStyles.None, CultureInfo.InvariantCulture, out var revision))
            return Results.BadRequest("Choose a model for the selected task.");
        try
        {
            await pilot.SelectModelAsync(db, purpose.Value, model, http.User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct, revision);
            return Results.Redirect(Panel + "?saved=" + (purpose == AiPurpose.PeriodExplanation ? "report" : "comparison"));
        }
        catch (AiRefusal refusal) when (refusal.Status == 409)
        {
            return Results.Redirect(Panel + "?conflict=1");
        }
        catch (InvalidOperationException)
        {
            return Results.Redirect(Panel + "?unavailable=1");
        }
    }

    private static async Task<IResult> ShowAsync(HttpContext http, IAntiforgery csrf, AiPilotProvider pilot, ServiceDbContext db, CancellationToken ct)
    {
        var token = Token(http, csrf);
        var report = await pilot.SelectionAsync(db, AiPurpose.PeriodExplanation, ct);
        var comparison = await pilot.SelectionAsync(db, AiPurpose.PeriodComparison, ct);
        var message = http.Request.Query.ContainsKey("conflict") ? "The selection changed in another session. Review the current model before saving." :
            http.Request.Query.ContainsKey("unavailable") ? "That model is unavailable for this task. Check the provider setup or choose another model." :
            http.Request.Query["saved"] == "report" ? "Report selection saved." :
            http.Request.Query["saved"] == "comparison" ? "Comparison selection saved." : "";
        return Page(http, "Analysis models", $$"""
            <header><a href="{{Panel}}">Zanance management</a><form method="post" action="{{Panel}}/logout">{{token}}<button class="quiet">Sign out</button></form></header>
            <main><h1>Analysis models</h1><p>Choose a model for each task in the synthetic pilot. Changes apply to new requests.</p><p><a href="{{Panel}}/pending">Pending requests</a></p>
            {{(message.Length == 0 ? "" : $"<p class=\"notice\" role=\"status\">{message}</p>")}}
            <div class="tasks">{{Card("report", "Simple reports", AiPurpose.PeriodExplanation, report, token)}}
            {{Card("comparison", "Financial comparisons", AiPurpose.PeriodComparison, comparison, token)}}</div>
            </main>
            """);
    }

    private static async Task<IResult> PendingAsync(HttpContext http, ServiceDbContext db, CancellationToken ct)
    {
        const int pageSize = 50;
        // The queue reports original dispatch metadata only. It cannot create a lookup, a new dispatch or a credit movement.
        var pending = await (from request in db.Set<AiRequestRow>().AsNoTracking()
                             join offer in db.Set<AiOfferRow>().AsNoTracking() on request.OfferId equals offer.Id
                             where request.State == AiRequestState.Pending && offer.PilotRouteId != null
                             orderby request.CreatedAt, request.Id
                             select new { request.Id, offer.Purpose, offer.Provider, offer.Model, request.CreatedAt,
                                 request.StatusCheckedAt, HasHandle = request.VendorOperationId != null })
            .Take(pageSize + 1).ToListAsync(ct);
        var rows = string.Concat(pending.Take(pageSize).Select(item => $$"""
            <tr><td><code>{{item.Id:D}}</code></td><td>{{(item.Purpose == AiPurpose.PeriodExplanation ? "Simple report" : "Financial comparison")}}</td>
            <td>{{WebUtility.HtmlEncode(item.Provider)}} / {{WebUtility.HtmlEncode(item.Model)}}</td><td>{{Timestamp(item.CreatedAt)}}</td>
            <td>{{Timestamp(item.StatusCheckedAt)}}</td><td>{{(item.HasHandle ? "Recorded" : "Missing")}}</td></tr>
            """));
        var details = pending.Count == 0 ? "<p>No pending pilot requests.</p>" : $$"""
            <div class="table-scroll" tabindex="0" aria-label="Pending requests table"><table>
            <caption>Oldest requests first</caption>
            <thead><tr><th scope="col">Request</th><th scope="col">Task</th><th scope="col">Original route</th>
            <th scope="col">Accepted (UTC)</th><th scope="col">Last status check (UTC)</th><th scope="col">Vendor reference</th></tr></thead>
            <tbody>{{rows}}</tbody></table></div>
            {{(pending.Count > pageSize ? "<p>Showing the oldest 50 requests. More are pending.</p>" : "")}}
            """;
        return Page(http, "Pending analysis requests", $$"""
            <header><a href="{{Panel}}">Analysis models</a></header>
            <main><h1>Pending analysis requests</h1>
            <p>A missing vendor reference leaves the outcome unknown. This page shows saved status only.</p>
            {{details}}</main>
            """);

        static string Timestamp(DateTimeOffset? time) => time?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "—";
    }


    private static string Card(string key, string label, AiPurpose purpose, (string Model, long RouteId, bool Available) selection, string token)
    {
        var models = AiPilotProvider.Models(purpose);
        var choices = string.Join("", models.Prepend("off").Select(model =>
            $"<option value=\"{model}\"{(model == selection.Model ? " selected" : "")}>{(model == "off" ? "Off" : model)}</option>"));
        if (selection.Model != "off" && !models.Contains(selection.Model, StringComparer.Ordinal))
            choices = $"<option value=\"\" selected disabled>{WebUtility.HtmlEncode(selection.Model)} (legacy)</option>" + choices;
        var current = selection.Model == "off" ? "Off" : WebUtility.HtmlEncode(selection.Model) + (selection.Available ? "" : " (unavailable)");
        return $$"""
            <section aria-labelledby="{{key}}-heading"><h2 id="{{key}}-heading">{{label}}</h2>
            <p>Current model <strong id="{{key}}-current">{{current}}</strong></p>
            <form method="post" action="{{Panel}}/selection">{{token}}
            <input type="hidden" name="purpose" value="{{key}}">
            <input type="hidden" name="revision" value="{{selection.RouteId.ToString(CultureInfo.InvariantCulture)}}">
            <label for="{{key}}-model">Model for {{label.ToLowerInvariant()}}</label><select id="{{key}}-model" name="model" required>{{choices}}</select>
            <button>Save {{(key == "report" ? "report" : "comparison")}} selection</button></form></section>
            """;
    }

    private static IResult LoginPage(HttpContext http, IAntiforgery csrf, int status = 200) => Page(http, "Operator sign-in", $$"""
        <header><span>Zanance management</span></header><main class="login"><h1>Operator sign-in</h1>
        {{(status == 401 ? "<p class=\"notice\" role=\"alert\">Sign-in failed. Use the configured operator account.</p>" : "")}}
        <form method="post" action="{{LoginPath}}">{{Token(http, csrf)}}
        <label for="email">Email</label><input id="email" name="email" type="email" autocomplete="username" maxlength="254" required>
        <label for="password">Password</label><input id="password" name="password" type="password" autocomplete="current-password" maxlength="256" required>
        <button>Sign in</button></form></main>
        """, status);

    private static string? Field(IFormCollection form, string key) => form[key].Count == 1 ? form[key][0] : null;

    private static string Token(HttpContext http, IAntiforgery csrf) =>
        $"<input type=\"hidden\" name=\"{CsrfField}\" value=\"{WebUtility.HtmlEncode(csrf.GetAndStoreTokens(http).RequestToken)}\">";

    private static IResult Page(HttpContext http, string title, string content, int status = 200)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        http.Response.Headers.ContentSecurityPolicy = $"default-src 'none'; style-src 'nonce-{nonce}'; form-action 'self'; base-uri 'none'; frame-ancestors 'none'";
        return Results.Content($$"""
            <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{title}} · Zanance</title><style nonce="{{nonce}}">
            :root{color-scheme:light dark;font:16px/1.55 system-ui,sans-serif}*{box-sizing:border-box}body{margin:0}
            header,main{max-width:68rem;margin:auto;padding:1.25rem clamp(1rem,4vw,3rem)}header{display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid GrayText;font-weight:650}header a{color:inherit}
            h1{font-size:clamp(1.8rem,5vw,2.6rem);line-height:1.15;margin:.6rem 0}h2{font-size:1.2rem;margin:0}p{margin:.75rem 0 1rem}
            .tasks{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,22rem),1fr));gap:1rem;margin:1.5rem 0}.login{max-width:32rem}
            section,.notice{padding:1.25rem;border:1px solid GrayText;border-radius:.55rem}label{display:block;font-weight:600;margin-top:1rem}input,select,button{font:inherit;min-height:2.75rem}
            input,select{display:block;width:100%;padding:.4rem .55rem;margin-top:.25rem;border:1px solid GrayText;border-radius:.3rem}button{margin-top:1rem;padding:.4rem .85rem;border-radius:.3rem;border:1px solid transparent;background:#174a83;color:white;cursor:pointer}button.quiet{margin:0;background:transparent;color:inherit;border-color:GrayText}
            .table-scroll{overflow-x:auto}table{border-collapse:collapse;width:100%;text-align:left}caption{text-align:left;font-weight:600}th,td{padding:.5rem;border-bottom:1px solid GrayText}
            :focus-visible{outline:3px solid Highlight;outline-offset:3px}
            </style></head><body>{{content}}</body></html>
            """, "text/html; charset=utf-8", statusCode: status);
    }
}
