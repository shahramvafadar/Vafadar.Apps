using System.Text;
using System.Text.Json;
using Google.Apis.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;
using static Vafadar.Zanance.Api.Billing.BillingProviderFields;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Authenticated events schedule a fresh provider query; notification labels never grant or revoke purchases.</summary>
internal sealed class BillingNotifications(IDbContextFactory<ServiceDbContext> factory, IDataProtectionProvider protection,
    IConfiguration configuration, TimeProvider time)
{
    /// <summary>Validates Google's maintained OIDC verifier, exact push audience and configured verified service account.</summary>
    internal async Task GoogleAsync(HttpRequest request, BillingEnvironment environment, CancellationToken ct)
    {
        if (!Enum.IsDefined(environment)) throw new BillingRefusal(404, "BillingNotificationUnavailable");
        var audience = configuration[$"Billing:Google:Notifications:{environment}:Audience"];
        var sender = configuration[$"Billing:Google:Notifications:{environment}:ServiceAccount"];
        if (string.IsNullOrWhiteSpace(audience) || !Uri.TryCreate(audience, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 ||
            string.IsNullOrWhiteSpace(sender) || !sender.EndsWith(".gserviceaccount.com", StringComparison.Ordinal))
            throw new BillingRefusal(503, "BillingNotificationConfigurationUnavailable");
        var bearer = request.Headers.Authorization.ToString();
        if (!bearer.StartsWith("Bearer ", StringComparison.Ordinal) || bearer.Length is < 8 or > 16384)
            throw new BillingRefusal(401, "BillingNotificationAuthenticationRequired");
        try
        {
            var identity = await GoogleJsonWebSignature.ValidateAsync(bearer[7..], new GoogleJsonWebSignature.ValidationSettings
            { Audience = [audience], IssuedAtClockTolerance = TimeSpan.FromSeconds(30), ExpirationTimeClockTolerance = TimeSpan.Zero }).WaitAsync(ct);
            if (!identity.EmailVerified || !string.Equals(identity.Email, sender, StringComparison.Ordinal))
                throw new BillingRefusal(401, "BillingNotificationAuthenticationRequired");
        }
        catch (InvalidJwtException) { throw new BillingRefusal(401, "BillingNotificationAuthenticationRequired"); }
        catch (HttpRequestException) { throw new BillingRefusal(503, "BillingNotificationAuthenticationUnavailable"); }
        using var body = await BillingModule.ReadAsync(request, BillingProviderJson.Default.JsonDocument, ct);
        var message = Object(body.RootElement, "message"); var eventId = Text(message, "messageId");
        var data = Text(message, "data", 32768); byte[] bytes;
        try { bytes = Convert.FromBase64String(data); }
        catch (FormatException) { throw new BillingRefusal(422, "BillingNotificationRefused"); }
        try
        {
            using var decoded = StrictJsonPayload.Deserialize(bytes, BillingProviderJson.Default.JsonDocument, 24576);
            var row = decoded.RootElement;
            if (Text(row, "packageName") != "pro.vafadar.zanance") throw new BillingRefusal(422, "StoreApplicationDiffers");
            var subscription = row.TryGetProperty("subscriptionNotification", out var sub) && sub.ValueKind == JsonValueKind.Object;
            var product = row.TryGetProperty("oneTimeProductNotification", out var item) && item.ValueKind == JsonValueKind.Object;
            var voided = row.TryGetProperty("voidedPurchaseNotification", out var refund) && refund.ValueKind == JsonValueKind.Object;
            var test = row.TryGetProperty("testNotification", out var probe) && probe.ValueKind == JsonValueKind.Object;
            if ((subscription ? 1 : 0) + (product ? 1 : 0) + (voided ? 1 : 0) + (test ? 1 : 0) != 1)
                throw new BillingRefusal(422, "BillingNotificationRefused");
            if (!test) await ScheduleAsync(BillingIssuer.GooglePlay, environment, eventId,
                Text(subscription ? sub : product ? item : refund, "purchaseToken", 8192), ct);
        }
        finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Authenticates Apple's V2 outer message and its original transaction before scheduling fresh provider authority.</summary>
    internal async Task AppleAsync(HttpRequest request, BillingEnvironment environment, AppleBillingSignatures signatures, CancellationToken ct)
    {
        if (!Enum.IsDefined(environment)) throw new BillingRefusal(404, "BillingNotificationUnavailable");
        var envelope = await BillingModule.ReadAsync(request, BillingProviderJson.Default.AppleNotificationEnvelope, ct);
        using var message = signatures.Verify(envelope.SignedPayload);
        var row = message.RootElement; var data = Object(row, "data");
        if (Text(row, "version") != "2.0" || Text(data, "bundleId") != "pro.vafadar.zanance" ||
            Text(data, "environment") != ApplePurchaseProvider.EnvironmentName(environment) ||
            !Guid.TryParseExact(Text(row, "notificationUUID", 64), "D", out var eventId) || eventId == Guid.Empty)
            throw new BillingRefusal(422, "BillingNotificationRefused");
        if (environment == BillingEnvironment.Production &&
            (!long.TryParse(configuration["Billing:Apple:AppAppleId"], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var appId) || appId <= 0 ||
                !data.TryGetProperty("appAppleId", out var actual) || !actual.TryGetInt64(out var actualId) || actualId != appId))
            throw new BillingRefusal(422, "StoreApplicationDiffers");
        if (Text(row, "notificationType") == "TEST") return;
        using var transaction = signatures.Verify(Text(data, "signedTransactionInfo", 65536));
        if (Text(transaction.RootElement, "bundleId") != "pro.vafadar.zanance" ||
            Text(transaction.RootElement, "environment") != ApplePurchaseProvider.EnvironmentName(environment))
            throw new BillingRefusal(422, "StoreApplicationDiffers");
        await ScheduleAsync(BillingIssuer.AppStore, environment, eventId.ToString("D"),
            AppleBillingFields.Transaction(Text(transaction.RootElement, "originalTransactionId", 64)), ct);
    }

    /// <summary>Deduplicates an authenticated event and atomically schedules its known purchase, without applying old event state.</summary>
    internal async Task ScheduleAsync(BillingIssuer issuer, BillingEnvironment environment, string eventId, string reference, CancellationToken ct)
    {
        if (eventId.Length is < 1 or > 256 || reference.Length is < 1 or > 8192 || eventId.Any(char.IsControl) || reference.Any(char.IsControl))
            throw new BillingRefusal(422, "BillingNotificationRefused");
        var id = BillingAuthority.PurchaseId(issuer, environment, reference);
        var eventHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            Encoding.UTF8.GetBytes($"zanance-billing-event-v1:{(int)issuer}:{(int)environment}:{eventId}")));
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        id = await db.Set<BillingPurchaseAliasRow>().Where(p => p.Id == id).Select(p => p.PurchaseId).SingleOrDefaultAsync(ct) ?? id;
        await BillingRequestScope.LockPurchaseAsync(db, id, ct);
        // Unknown tokens cannot create an owner, reservation, paid grant or unbounded private notification collection.
        var purchase = await db.Set<BillingPurchaseRow>().SingleOrDefaultAsync(p => p.Id == id && p.Verified, ct);
        if (purchase is null) { await tx.CommitAsync(ct); return; }
        if (await db.Set<BillingNotificationRow>().AnyAsync(p => p.Id == eventHash, ct)) { await tx.CommitAsync(ct); return; }
        var now = time.GetUtcNow();
        await db.Set<BillingNotificationRow>().Where(p => p.Completed && p.ReceivedAt < now.AddDays(-7)).ExecuteDeleteAsync(ct);
        if (await db.Set<BillingNotificationRow>().CountAsync(ct) >= 100000)
            throw new BillingRefusal(503, "BillingNotificationRetentionBound");
        db.Add(new BillingNotificationRow { Id = eventHash, Issuer = issuer, Environment = environment,
            ProtectedReference = protection.CreateProtector("zanance-billing-provider-reference-v1").Protect(reference),
            ReceivedAt = now, Completed = true, RetryAt = now });
        purchase.RecheckAt = now; purchase.RequestedRecheckRevision = checked(purchase.RequestedRecheckRevision + 1);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}

/// <summary>Signed Apple V2 transport; diagnostics must not print its embedded receipt.</summary>
internal sealed record AppleNotificationEnvelope([property: System.Text.Json.Serialization.JsonPropertyName("signedPayload")] string SignedPayload)
{
    /// <inheritdoc />
    public override string ToString() => "Apple notification proof";
}
