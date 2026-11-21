using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api;

/// <summary>Public browser entry point for the same pending deletion request as the app.</summary>
internal static class AccountDeletionPage
{
    internal const string Path = "/identity/delete-account";
    private const string Rejected = "We could not verify your account. Check your credentials and try again.";

    internal static void Map(WebApplication app)
    {
        app.MapGet(Path, (HttpContext http, IAntiforgery csrf) => Form(http, csrf));
        app.MapPost(Path, SubmitAsync);
    }

    private static async Task<IResult> SubmitAsync(HttpContext http, IAntiforgery csrf, UserManager<ServiceUser> users,
        SignInManager<ServiceUser> signIn, ServiceDbContext db, TimeProvider time, CancellationToken ct)
    {
        if (http.Request.ContentLength is not (> 0 and <= 4096) ||
            !MediaTypeHeaderValue.TryParse(http.Request.ContentType, out var contentType) ||
            !contentType.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) ||
            !await csrf.IsRequestValidAsync(http))
            return Form(http, csrf, "Reload this page before submitting the request.", 400);

        var form = await http.Request.ReadFormAsync(ct);
        if (form["email"].Count != 1 || form["password"].Count != 1 ||
            form["email"][0] is not { Length: > 3 and <= 254 } email || !email.Contains('@', StringComparison.Ordinal) ||
            form["password"][0] is not { Length: >= 12 and <= 256 } password)
            return Form(http, csrf, Rejected, 401);

        var user = await users.FindByEmailAsync(email);
        if (user is null) return Form(http, csrf, Rejected, 401);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await SyncRequestScope.LockOwnerAsync(db, user.Id, ct);
        await db.Entry(user).ReloadAsync(ct);
        if (db.Entry(user).State == EntityState.Detached) return Form(http, csrf, Rejected, 401);
        if (!(await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded)
        {
            await transaction.CommitAsync(ct);
            return Form(http, csrf, Rejected, 401);
        }

        var receipt = await AccountDeletionRoutes.QueueAsync(db, user.Id, time, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Page(http, $"""
            <p>Your request has been received and is pending.</p>
            <p>Receipt reference <strong>{receipt.ReceiptId:D}</strong></p>
            <p>Keep this reference for your records.</p>
            """, 202);
    }

    private static IResult Form(HttpContext http, IAntiforgery csrf, string? feedback = null, int status = 200)
    {
        var tokens = csrf.GetAndStoreTokens(http);
        return Page(http, $"""
            <p>Use your Zanance account email and password to request deletion of your account and associated service data.</p>
            <p>Submitting the request creates a pending receipt. Local data on your devices is managed separately in the app.</p>
            {(feedback is null ? "" : $"<p role=\"alert\">{WebUtility.HtmlEncode(feedback)}</p>")}
            <form action="{Path}" method="post">
              <input type="hidden" name="{WebUtility.HtmlEncode(tokens.FormFieldName)}" value="{WebUtility.HtmlEncode(tokens.RequestToken)}">
              <p><label for="email">Email</label><br>
              <input id="email" name="email" type="email" autocomplete="username" maxlength="254" required></p>
              <p><label for="password">Password</label><br>
              <input id="password" name="password" type="password" autocomplete="current-password" maxlength="256" required></p>
              <button type="submit">Request account deletion</button>
            </form>
            """, status);
    }

    private static IResult Page(HttpContext http, string body, int status)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; form-action 'self'; base-uri 'none'; frame-ancestors 'none'";
        http.Response.Headers["Referrer-Policy"] = "no-referrer";
        http.Response.Headers.XContentTypeOptions = "nosniff";
        return Results.Content($"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Delete your Zanance account</title></head>
            <body><main><h1>Delete your Zanance account</h1>{body}</main></body>
            </html>
            """, "text/html; charset=utf-8", statusCode: status);
    }
}
