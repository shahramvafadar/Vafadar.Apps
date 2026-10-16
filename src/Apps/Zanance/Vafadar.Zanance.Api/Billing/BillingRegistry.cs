using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Original device registration and complete signed snapshots; neither creates a paid right.</summary>
internal sealed class BillingRegistry(IDbContextFactory<ServiceDbContext> factory, BillingAuthority authority,
    BillingCatalog catalog, IDataProtectionProvider protection, TimeProvider time)
{
    /// <summary>Registers actual key possession, preserving exact initial response across interruption.</summary>
    internal async Task<BillingRegistrationResult> RegisterAsync(BillingRegistrationEnvelope request, ClaimsPrincipal principal, CancellationToken ct)
    {
        var key = BillingAuthority.ReadPublic(request.PublicIdentity);
        var body = StandardJoseSignatures.VerifyJson(request.Signed, key, BillingAuthority.RegistrationPurpose, 32768, BillingJson.Default.BillingRegistration);
        if (body.Id == Guid.Empty || body.PublicIdentity != request.PublicIdentity ||
            body.Distribution is not (BillingIssuer.GooglePlay or BillingIssuer.AppStore) || !Enum.IsDefined(body.Environment))
            throw new BillingRefusal(422, "BillingRegistrationRefused");
        using var signer = await authority.OpenAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var scope = await BillingRequestScope.OpenAsync(db, principal, time, ct);
        RequireIdentityHash(body.IdentityHash, scope.UserId);
        await BillingRequestScope.LockCustomerAsync(db, body.Id, ct);
        var row = await db.Set<BillingCustomerRow>().SingleOrDefaultAsync(x => x.Id == body.Id, ct);
        var protector = protection.CreateProtector("zanance-billing-registration-v1");
        if (row is not null)
        {
            if (row.PublicIdentity != body.PublicIdentity || row.Distribution != body.Distribution || row.Environment != body.Environment ||
                row.RegistrationIdentityHash != body.IdentityHash || string.IsNullOrEmpty(row.ProtectedRegistrationResponse))
                throw new BillingRefusal(409, "OriginalBillingRegistrationDiffers");
            var original = StrictJsonPayload.Deserialize(protector.Unprotect(Convert.FromBase64String(row.ProtectedRegistrationResponse)),
                BillingJson.Default.BillingRegistrationResult, 65536);
            await scope.CommitAsync(ct); return original;
        }
        row = new() { Id = body.Id, PublicIdentity = body.PublicIdentity, DeviceFingerprint = key.Fingerprint,
            Distribution = body.Distribution, Environment = body.Environment, UserId = scope.UserId,
            RegistrationIdentityHash = body.IdentityHash, CreatedAt = time.GetUtcNow() };
        row.AccountToken = Guid.NewGuid();
        row.AnonymousAccountBinding = body.Distribution == BillingIssuer.GooglePlay ? BillingAuthority.GoogleAccount(row.AccountToken) : row.AccountToken.ToString("D");
        db.Add(row);
        // The immutable registration replay must not reissue historical paid grants after a later refund.
        // A fresh signed command with a fresh nonce/time proof is required for a complete current snapshot.
        var snapshot = await SnapshotAsync(db, row, scope.UserId, body.Id, ct, BillingSnapshotKind.Registration);
        var result = new BillingRegistrationResult(row.Id, snapshot.AccountToken, BillingAuthority.Public(signer),
            BillingAuthority.Sign(snapshot, signer), catalog.Read(row.Distribution, row.Environment));
        row.ProtectedRegistrationResponse = Convert.ToBase64String(protector.Protect(JsonSerializer.SerializeToUtf8Bytes(result, BillingJson.Default.BillingRegistrationResult)));
        await scope.CommitAsync(ct); return result;
    }

    /// <summary>Requires the exact actual optional identity; an arbitrary client hash cannot bind or switch users.</summary>
    internal static void RequireIdentityHash(string? declared, string? actual)
    {
        var expected = actual is null ? null : BillingAuthority.IdentityHash(actual);
        if (declared != expected) throw new BillingRefusal(403, "BillingIdentityDiffers");
    }

    /// <summary>Authenticates the original device command before any purchase, provider query or key effect.</summary>
    internal static BillingCommand Verify(BillingCustomerRow customer, string signed, string? user, DateTimeOffset now, bool requireFresh = true)
    {
        var command = StandardJoseSignatures.VerifyJson(signed, BillingAuthority.ReadPublic(customer.PublicIdentity),
            BillingAuthority.CommandPurpose, 32768, BillingJson.Default.BillingCommand);
        RequireIdentityHash(command.IdentityHash, user);
        if (user is not null && customer.UserId is not null && customer.UserId != user)
            throw new BillingRefusal(403, "BillingIdentityDiffers");
        if (command.CustomerId != customer.Id || command.OperationId == Guid.Empty || !Enum.IsDefined(command.Operation) ||
            command.ExpectedRevision < 0 || requireFresh && (command.Deadline <= now || command.Deadline > now.AddMinutes(10)))
            throw new BillingRefusal(409, "OriginalBillingCommandUnavailable");
        if (command.ExpectedReplacementId is { } replacement && (command.NativePaymentId is null || command.ProductHint is null ||
            command.Operation is not (BillingOperation.VerifyPurchase or BillingOperation.RestorePurchase) ||
            replacement.Length != 64 || !replacement.All(char.IsAsciiHexDigit)))
            throw new BillingRefusal(422, "OriginalReplacementBindingRefused");
        if (customer.Revoked && command.Operation != BillingOperation.RetireDevice)
            throw new BillingRefusal(410, "BillingDeviceRetired");
        return command;
    }

    /// <summary>Captures only actual current rights; a complete replacement snapshot removes old expired/revoked claims.</summary>
    internal async Task<BillingSnapshot> SnapshotAsync(ServiceDbContext db, BillingCustomerRow customer, string? user,
        Guid requestId, CancellationToken ct, BillingSnapshotKind kind = BillingSnapshotKind.Current)
    {
        if (user is not null && customer.UserId is not null && user != customer.UserId)
            throw new BillingRefusal(403, "BillingIdentityDiffers");
        var account = customer.AccountToken;
        if (user is not null)
        {
            var binding = await db.Set<BillingIdentityRow>().SingleOrDefaultAsync(row => row.UserId == user && row.Environment == customer.Environment, ct);
            if (binding is null)
            {
                var token = Guid.NewGuid();
                binding = new() { UserId = user, Environment = customer.Environment, AccountToken = token,
                    GoogleAccountBinding = BillingAuthority.GoogleAccount(token, identity: true) };
                db.Add(binding);
            }
            account = binding.AccountToken;
        }
        var now = time.GetUtcNow();
        if (customer.Revoked || kind == BillingSnapshotKind.Registration) return Create([]);
        var visible = db.Set<BillingPurchaseRow>().Where(p => p.Verified && p.Environment == customer.Environment &&
            ((p.Product == BillingProduct.ProMonthly || p.Product == BillingProduct.ProYearly || p.Issuer == BillingIssuer.Web)
                ? user != null && p.UserId == user
                : p.Issuer == customer.Distribution &&
                  (db.Set<BillingDevicePurchaseRow>().Any(d => d.CustomerId == customer.Id && d.PurchaseId == p.Id) || user != null && p.UserId == user)));
        var live = await visible.Where(p => p.State != BillingPurchaseState.Expired && p.State != BillingPurchaseState.Revoked)
            .OrderBy(p => p.Id).Take(33).ToArrayAsync(ct);
        if (live.Length > 32) throw new BillingRefusal(413, "BillingSnapshotBound");
        // Current is a complete nonce-bound replacement, not a historical journal. Old terminal purchases
        // may be omitted safely; they cannot crowd an active grant out after many legitimate replacements.
        var terminal = await visible.Where(p => p.State == BillingPurchaseState.Expired || p.State == BillingPurchaseState.Revoked)
            .OrderByDescending(p => p.VerifiedAt).ThenBy(p => p.Id).Take(32 - live.Length).ToArrayAsync(ct);
        var purchases = live.Concat(terminal).OrderBy(p => p.Id);
        var grants = new List<BillingCachedGrant>();
        foreach (var purchase in purchases)
        {
            var matching = user is not null && purchase.UserId == user;
            if (!BillingProducts.AppliesTo(purchase.Product, purchase.Issuer, customer.Distribution, matching)) continue;
            var bytes = System.Text.Encoding.UTF8.GetBytes(purchase.FactsJson);
            var facts = StrictJsonPayload.Deserialize(bytes, BillingJson.Default.BillingPurchaseFacts, 8192);
            facts.Validate();
            if (facts.Product != purchase.Product || facts.State != purchase.State)
                throw new BillingRefusal(503, "OriginalBillingStateUnavailable");
            grants.Add(new(purchase.Issuer, purchase.Environment, facts, matching, purchase.VerifiedAt, purchase.Id));
        }
        if (grants.Count > 32) throw new BillingRefusal(413, "BillingSnapshotBound");
        return Create(grants.ToArray());

        BillingSnapshot Create(IReadOnlyList<BillingCachedGrant> facts) => new(1, kind, customer.Id, requestId, customer.DeviceFingerprint,
            customer.Distribution, customer.Environment, user is null ? null : BillingAuthority.IdentityHash(user), account,
            customer.Revision, time.GetUtcNow(), facts, customer.Revoked,
            kind == BillingSnapshotKind.Current ? catalog.Read(customer.Distribution, customer.Environment) : null);
    }
}
