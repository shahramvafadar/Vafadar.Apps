using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api;

/// <summary>Fresh credential proof for registering a pending deletion request.</summary>
public sealed record AccountDeletionInput(string? Password)
{
    /// <inheritdoc />
    public override string ToString() => "Unretained account deletion input";
}

/// <summary>Identifies the original accepted request, without disclosing private account data.</summary>
public sealed record AccountDeletionReceipt(Guid ReceiptId, DateTimeOffset RequestedAt);

/// <summary>Receives and looks up authenticated account deletion requests.</summary>
internal static class AccountDeletionRoutes
{
    /// <summary>Maps the original caller's request resource for use by the app and the account website.</summary>
    internal static void Map(WebApplication app)
    {
        var request = app.MapGroup("/identity/deletion-request").RequireAuthorization();
        request.MapGet("", ReadAsync);
        request.MapPost("", RequestAsync);
        AccountDeletionPage.Map(app);
    }

    private static async Task<IResult> ReadAsync(HttpContext http, ServiceDbContext db, ServiceStorage storage, TimeProvider time, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        await using var scope = await SyncRequestScope.OpenAsync(http.User, db, time, ct);
        if (scope is null) return Results.Unauthorized();
        var current = await db.AccountDeletionRequests.AsNoTracking().SingleOrDefaultAsync(row => row.UserId == scope.Owner, ct);
        if (current is null) return Results.NoContent();
        var receipt = Receipt(current);
        return await AccountDeletionRestoreFence.RecordAcknowledgementAsync(storage, receipt, scope.Owner, ct)
            ? Results.Ok(receipt) : Results.StatusCode(503);
    }

    private static async Task<IResult> RequestAsync(AccountDeletionInput input, HttpContext http, UserManager<ServiceUser> users,
        SignInManager<ServiceUser> signIn, ServiceDbContext db, ServiceStorage storage, TimeProvider time, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        if (input.Password is not { Length: >= 12 and <= 256 }) return Results.BadRequest();
        await using var scope = await SyncRequestScope.OpenAsync(http.User, db, time, ct);
        if (scope is null) return Results.Unauthorized();
        var user = await users.FindByIdAsync(scope.Owner);
        if (user is null) return Results.Unauthorized();
        if (!(await signIn.CheckPasswordSignInAsync(user, input.Password, lockoutOnFailure: true)).Succeeded)
        {
            // Keep the normal lockout counter even when credential proof is refused.
            await scope.CommitAsync(ct);
            return Results.Unauthorized();
        }

        var receipt = await QueueAsync(db, scope.Owner, time, ct);
        await scope.CommitAsync(ct);
        if (!await AccountDeletionRestoreFence.RecordAcknowledgementAsync(storage, receipt, scope.Owner, ct))
            return Results.StatusCode(503);
        return Results.Accepted(value: receipt);
    }

    // Both entry points hold the original owner's transaction lock before calling this method.
    internal static async Task<AccountDeletionReceipt> QueueAsync(ServiceDbContext db, string owner, TimeProvider time, CancellationToken ct)
    {
        var current = await db.AccountDeletionRequests.SingleOrDefaultAsync(row => row.UserId == owner, ct);
        if (current is null)
        {
            // PostgreSQL stores less precision than .NET ticks. Keep the first receipt identical to later reads.
            var requestedAt = DateTimeOffset.FromUnixTimeMilliseconds(time.GetUtcNow().ToUnixTimeMilliseconds());
            current = new() { UserId = owner, ReceiptId = Guid.NewGuid(), RequestedAt = requestedAt };
            db.AccountDeletionRequests.Add(current);
        }

        return Receipt(current);
    }

    private static AccountDeletionReceipt Receipt(AccountDeletionRequest row) => new(row.ReceiptId, row.RequestedAt);
}
