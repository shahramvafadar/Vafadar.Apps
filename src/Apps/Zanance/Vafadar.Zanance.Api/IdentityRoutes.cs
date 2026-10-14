using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Vafadar.Zanance.Api;

/// <summary>Bounded identity input; machine responses contain no credentials or recipient details.</summary>
public sealed record IdentityInput(string Email, string Password);
/// <summary>Explicit challenge completion, always sent in a body rather than a logged URL.</summary>
public sealed record ChallengeInput(Guid Id, string Token, string? Password);
/// <summary>Framework refresh token submitted once.</summary>
public sealed record RefreshInput(string RefreshToken);
/// <summary>Protected outbox payload, delivered only by a configured private operator port.</summary>
public sealed record PrivateMail(string Recipient, string Token);

/// <summary>Maintained Identity credential operations with durable one-use grants.</summary>
public static class IdentityRoutes
{
    /// <summary>Maps the complete optional identity lifecycle without billing or financial sync.</summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/identity/register", async (IdentityInput input, UserManager<ServiceUser> users, ServiceDbContext db,
            IDataProtectionProvider protection, TimeProvider time, CancellationToken ct) =>
        {
            if (!Valid(input)) return Results.BadRequest();
            // Uniform response avoids declaring whether a recipient already has an identity.
            if (await users.FindByEmailAsync(input.Email) is not null) return Results.Accepted();
            var user = new ServiceUser { UserName = input.Email, Email = input.Email };
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var created = await users.CreateAsync(user, input.Password);
            if (!created.Succeeded) return Results.Accepted();
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            AddChallenge(db, protection, time, user, "confirm", token);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Accepted();
        });
        app.MapPost("/identity/recover", async (IdentityInput input, UserManager<ServiceUser> users, ServiceDbContext db,
            IDataProtectionProvider protection, TimeProvider time, CancellationToken ct) =>
        {
            if (input.Email is null || input.Email.Length > 254) return Results.BadRequest();
            var user = await users.FindByEmailAsync(input.Email);
            if (user is null || !user.EmailConfirmed) return Results.Accepted();
            var token = await users.GeneratePasswordResetTokenAsync(user);
            AddChallenge(db, protection, time, user, "reset", token);
            await db.SaveChangesAsync(ct);
            return Results.Accepted();
        });
        app.MapPost("/identity/complete", CompleteAsync);
        app.MapPost("/identity/login", async (IdentityInput input, SignInManager<ServiceUser> signIn,
            UserManager<ServiceUser> users, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            if (!Valid(input)) return Results.Unauthorized();
            var user = await users.FindByEmailAsync(input.Email);
            if (user is null || !(await signIn.CheckPasswordSignInAsync(user, input.Password, lockoutOnFailure: true)).Succeeded)
                return Results.Unauthorized();
            var session = new DeviceSession { Id = Guid.NewGuid(), UserId = user.Id, ExpiresAt = time.GetUtcNow().AddDays(14) };
            db.Sessions.Add(session); await db.SaveChangesAsync(ct);
            return await GrantAsync(signIn, user, session);
        });
        app.MapPost("/identity/refresh", async (RefreshInput input, IOptionsMonitor<BearerTokenOptions> options,
            SignInManager<ServiceUser> signIn, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            if (input.RefreshToken is null || input.RefreshToken.Length > 8192) return Results.Unauthorized();
            var ticket = options.Get(IdentityConstants.BearerScheme).RefreshTokenProtector.Unprotect(input.RefreshToken);
            if (ticket is null || ticket.Properties.ExpiresUtc <= time.GetUtcNow() ||
                !Guid.TryParse(ticket.Principal.FindFirstValue(ServiceHost.SessionClaim), out var id) ||
                !int.TryParse(ticket.Principal.FindFirstValue(ServiceHost.GenerationClaim), out var generation)) return Results.Unauthorized();
            var user = await signIn.ValidateSecurityStampAsync(ticket.Principal);
            if (user is null) return Results.Unauthorized();
            var count = await db.Sessions.Where(s => s.Id == id && s.UserId == user.Id && !s.Revoked && s.Generation == generation && s.ExpiresAt > time.GetUtcNow())
                .ExecuteUpdateAsync(u => u.SetProperty(s => s.Generation, s => s.Generation + 1), ct);
            if (count != 1) return Results.Unauthorized();
            var session = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == id, ct);
            return await GrantAsync(signIn, user, session);
        });
        app.MapGet("/sessions", async (ClaimsPrincipal principal, ServiceDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Sessions.AsNoTracking().Where(s => s.UserId == principal.FindFirstValue(ClaimTypes.NameIdentifier))
                .Select(s => new { s.Id, s.ExpiresAt, s.Revoked }).ToListAsync(ct))).RequireAuthorization();
        app.MapDelete("/sessions/{id:guid}", async (Guid id, ClaimsPrincipal principal, ServiceDbContext db, CancellationToken ct) =>
        {
            var changed = await db.Sessions.Where(s => s.Id == id && s.UserId == principal.FindFirstValue(ClaimTypes.NameIdentifier))
                .ExecuteUpdateAsync(u => u.SetProperty(s => s.Revoked, true), ct);
            return changed == 1 ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();
        app.MapPost("/sessions/revoke-all", async (ClaimsPrincipal principal, ServiceDbContext db, CancellationToken ct) =>
        {
            await db.Sessions.Where(s => s.UserId == principal.FindFirstValue(ClaimTypes.NameIdentifier)).ExecuteUpdateAsync(u => u.SetProperty(s => s.Revoked, true), ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static bool Valid(IdentityInput input) => input.Email is { Length: > 3 and <= 254 } && input.Email.Contains('@', StringComparison.Ordinal) && input.Password is { Length: >= 12 and <= 256 };

    private static void AddChallenge(ServiceDbContext db, IDataProtectionProvider protection, TimeProvider time, ServiceUser user, string purpose, string token) =>
        db.Challenges.Add(new IdentityChallenge { Id = Guid.NewGuid(), UserId = user.Id, Purpose = purpose, ExpiresAt = time.GetUtcNow().AddMinutes(15),
            ProtectedMail = protection.CreateProtector("identity-mail-v1").Protect(JsonSerializer.Serialize(new PrivateMail(user.Email ?? throw new InvalidOperationException("Recipient required."), token))) });

    private static async Task<IResult> GrantAsync(SignInManager<ServiceUser> signIn, ServiceUser user, DeviceSession session)
    {
        var principal = await signIn.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)(principal.Identity ?? throw new InvalidOperationException("Identity required."));
        identity.AddClaim(new Claim(ServiceHost.SessionClaim, session.Id.ToString()));
        identity.AddClaim(new Claim(ServiceHost.GenerationClaim, session.Generation.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return Results.SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme);
    }

    private static async Task<IResult> CompleteAsync(ChallengeInput input, UserManager<ServiceUser> users, ServiceDbContext db,
        IDataProtectionProvider protection, TimeProvider time, CancellationToken ct)
    {
        if (input.Token is null || input.Token.Length > 8192 || input.Password?.Length > 256) return Results.BadRequest();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var claimed = await db.Challenges.Where(c => c.Id == input.Id && !c.Consumed && c.ExpiresAt > time.GetUtcNow())
            .ExecuteUpdateAsync(u => u.SetProperty(c => c.Consumed, true), ct);
        if (claimed != 1) return Results.BadRequest();
        var challenge = await db.Challenges.AsNoTracking().SingleAsync(c => c.Id == input.Id, ct);
        var mail = JsonSerializer.Deserialize<PrivateMail>(protection.CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail));
        if (mail is null || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(mail.Token), System.Text.Encoding.UTF8.GetBytes(input.Token))) return Results.BadRequest();
        var user = await users.FindByIdAsync(challenge.UserId);
        if (user is null) return Results.BadRequest();
        var result = challenge.Purpose == "confirm" ? await users.ConfirmEmailAsync(user, input.Token) :
            input.Password is { Length: >= 12 } ? await users.ResetPasswordAsync(user, input.Token, input.Password) : IdentityResult.Failed();
        if (!result.Succeeded) return Results.BadRequest();
        if (challenge.Purpose == "reset")
            await db.Sessions.Where(s => s.UserId == user.Id).ExecuteUpdateAsync(u => u.SetProperty(s => s.Revoked, true), ct);
        await tx.CommitAsync(ct);
        return Results.NoContent();
    }
}
