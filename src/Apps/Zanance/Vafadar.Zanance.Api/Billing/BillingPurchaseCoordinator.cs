using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Finite original queue, retained before the first provider query.</summary>
internal sealed record BillingPurchaseWork(string[] PurchaseIds);

/// <summary>Original durable provider lease; no database connection or key reader crosses HTTP.</summary>
internal sealed record BillingPurchaseLease(string PurchaseId, Guid LeaseOwner, int Index, BillingIssuer Issuer,
    BillingEnvironment Environment, BillingProduct Hint, string Reference, DateTimeOffset StartedAt, long ObservedRecheckRevision = 0, bool ExistingReference = false)
{
    /// <summary>Never print a raw purchase reference in a record's diagnostic representation.</summary>
    public override string ToString() => "Original billing provider lease";
}

/// <summary>Actual verified purchases, durable original continuation and global ownership, independent of financial storage.</summary>
internal sealed partial class BillingPurchaseCoordinator(IDbContextFactory<ServiceDbContext> factory, BillingAuthority authority,
    BillingRegistry registry, IEnumerable<IBillingProvider> providers, IDataProtectionProvider protection, TimeProvider time)
{
    /// <summary>Continues only the exact original accepted queue; successful prefix steps are not queried again.</summary>
    internal async Task<BillingCommandResult> ExecuteAsync(Guid customer, string signed, ClaimsPrincipal actor, CancellationToken ct)
    {
        while (true)
        {
            var next = await NextAsync(customer, signed, actor, ct);
            if (next.Reply is not null) return next.Reply;
            var lease = next.Lease ?? throw new BillingRefusal(503, "OriginalBillingWorkUnavailable");
            var provider = providers.SingleOrDefault(row => row.Issuer == lease.Issuer)
                ?? throw new BillingRefusal(503, "BillingProviderUnavailable");
            BillingVerifiedPurchase verified;
            try
            {
                verified = lease.ExistingReference
                    ? await provider.VerifyExistingAsync(lease.Reference, lease.Hint, lease.Environment, ct)
                    : await provider.VerifyAsync(lease.Reference, lease.Hint, lease.Environment, ct);
                if (verified.Reference != lease.Reference || verified.Issuer != lease.Issuer || verified.Environment != lease.Environment ||
                    string.IsNullOrEmpty(verified.Canonical) || verified.Canonical.Length > 8192 || verified.Canonical.Any(char.IsControl))
                    throw new BillingRefusal(422, "StoreVerificationDiffers");
                verified.Facts.Validate();
                await AcceptAsync(customer, signed, actor, lease, verified, ct);
            }
            catch
            {
                // This awaited operation is terminal here. Release only its still-owned visible lease;
                // original requests, uncertain commits, accepted prefixes and another worker's lease stay intact.
                await ReleaseFailedLeaseAsync(lease); throw;
            }
            // Grant persistence precedes acknowledgement. A failure leaves durable work, not an invented refund or another purchase.
            await TryAcknowledgeAsync(provider, verified, ct);
        }
    }

    /// <summary>Captures a new finite queue or leases its next original step under short actual authority locks.</summary>
    private async Task<(BillingCommandResult? Reply, BillingPurchaseLease? Lease)> NextAsync(Guid customerId,
        string signed, ClaimsPrincipal actor, CancellationToken ct)
    {
        using var signer = await authority.OpenAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var scope = await BillingRequestScope.OpenAsync(db, actor, time, ct);
        await BillingRequestScope.LockCustomerAsync(db, customerId, ct);
        var customer = await db.Set<BillingCustomerRow>().SingleOrDefaultAsync(row => row.Id == customerId, ct)
            ?? throw new BillingRefusal(404, "BillingDeviceUnavailable");
        var now = time.GetUtcNow();
        var command = BillingRegistry.Verify(customer, signed, scope.UserId, now, requireFresh: false);
        var journal = await BillingCommandStore.ReadOriginalAsync(db, customerId, command.OperationId, signed, ct);
        if (journal?.ProtectedResponse is { Length: > 0 })
        { var original = BillingCommandStore.ReadResponse(journal, protection); await scope.CommitAsync(ct); return (original, null); }
        if (command.Operation is not (BillingOperation.VerifyPurchase or BillingOperation.RestorePurchase or BillingOperation.Refresh))
            throw new BillingRefusal(422, "BillingOperationRefused");
        if (journal is null)
        {
            BillingCommandStore.RequireFresh(command, customer, now);
            await db.Set<BillingCommandRow>().Where(row => row.CustomerId == customerId && row.ExpiresAt < now && row.ProtectedResponse != "").ExecuteDeleteAsync(ct);
            if (await db.Set<BillingCommandRow>().CountAsync(row => row.CustomerId == customerId, ct) >= 256)
                throw new BillingRefusal(413, "BillingPendingWorkBound");
            journal = BillingCommandStore.NewJournal(customerId, command.OperationId, signed, now, protection);
            db.Add(journal);
            var ids = await CaptureWorkAsync(db, customer, command, scope.UserId, ct);
            journal.ProtectedWork = protection.CreateProtector("zanance-billing-work-v1").Protect(
                JsonSerializer.Serialize(new BillingPurchaseWork(ids), BillingProviderJson.Default.BillingPurchaseWork));
            await db.SaveChangesAsync(ct);
        }
        var work = ReadWork(journal);
        if (journal.CompletedWork < 0 || journal.CompletedWork > work.PurchaseIds.Length)
            throw new BillingRefusal(503, "OriginalBillingWorkUnavailable");
        if (journal.CompletedWork == work.PurchaseIds.Length)
        {
            var snapshot = await registry.SnapshotAsync(db, customer, scope.UserId, command.OperationId, ct, BillingSnapshotKind.Operation);
            var result = BillingAuthority.CommandResult(snapshot, signer, signed,
                command.Operation != BillingOperation.Refresh && journal.LastState is not (null or BillingPurchaseState.Pending),
                command.Operation == BillingOperation.Refresh ? null : journal.LastState, command.NativePaymentId,
                command.NativePaymentId is not null && command.Operation != BillingOperation.Refresh && journal.LastState is not (null or BillingPurchaseState.Pending)
                    ? command.ProductHint : null);
            BillingCommandStore.SaveResponse(journal, result, protection);
            await scope.CommitAsync(ct); return (result, null);
        }
        var requested = work.PurchaseIds[journal.CompletedWork];
        var id = await ResolveAliasAsync(db, requested, ct);
        await BillingRequestScope.LockPurchaseAsync(db, id, ct);
        var purchase = await db.Set<BillingPurchaseRow>().SingleOrDefaultAsync(row => row.Id == id, ct)
            ?? throw new BillingRefusal(503, "OriginalBillingWorkUnavailable");
        if (purchase.LeaseUntil > now) throw new BillingRefusal(409, "OriginalBillingVerificationPending");
        await RequireQueryAccessAsync(db, customer, command, purchase, scope.UserId, now, ct);
        // Public Apple restore always rechecks its original JWS, even when a previous alias resolves to a verified chain.
        // Only an authorized Refresh may use the stored canonical numeric transaction id.
        var existingReference = command.Operation == BillingOperation.Refresh;
        var reference = existingReference
            ? protection.CreateProtector("zanance-billing-provider-reference-v1").Unprotect(purchase.ProtectedReference)
            : command.ProviderReference ?? throw new BillingRefusal(422, "StorePurchaseReferenceRequired");
        var lease = new BillingPurchaseLease(id, Guid.NewGuid(), journal.CompletedWork, purchase.Issuer, purchase.Environment, purchase.Product,
            reference, now, purchase.RequestedRecheckRevision, existingReference);
        purchase.LeaseOwner = lease.LeaseOwner; purchase.LeaseUntil = now.AddMinutes(2);
        await scope.CommitAsync(ct); return (null, lease);
    }

    /// <summary>Only exact original provider references or already authorized purchases enter the durable work list.</summary>
    private async Task<string[]> CaptureWorkAsync(ServiceDbContext db, BillingCustomerRow customer,
        BillingCommand command, string? user, CancellationToken ct)
    {
        if (command.Operation == BillingOperation.Refresh)
        {
            if (command.ProviderReference is not null || command.ProductHint is not null) throw new BillingRefusal(422, "BillingCommandFieldsRefused");
            var ids = await db.Set<BillingPurchaseRow>().Where(p => p.Verified && p.Environment == customer.Environment &&
                p.State != BillingPurchaseState.Expired && p.State != BillingPurchaseState.Revoked &&
                ((p.Product == BillingProduct.ProMonthly || p.Product == BillingProduct.ProYearly || p.Issuer == BillingIssuer.Web)
                    ? user != null && p.UserId == user
                    : p.Issuer == customer.Distribution && (db.Set<BillingDevicePurchaseRow>().Any(d => d.CustomerId == customer.Id && d.PurchaseId == p.Id) ||
                        user != null && p.UserId == user))).OrderBy(p => p.Id).Select(p => p.Id).Take(33).ToArrayAsync(ct);
            if (ids.Length > 32) throw new BillingRefusal(413, "BillingRefreshBound");
            return ids;
        }
        if (command.ProductHint is not { } hint || !Enum.IsDefined(hint) || command.ProviderReference is not { Length: > 0 and <= 16384 } reference || reference.Any(char.IsControl))
            throw new BillingRefusal(422, "StorePurchaseReferenceRequired");
        if (BillingProducts.RequiresIdentity(hint, customer.Distribution) && user is null)
            throw new BillingRefusal(403, "CurrentPurchaseIdentityRequired");
        var id = await ResolveAliasAsync(db, BillingAuthority.PurchaseId(customer.Distribution, customer.Environment, reference), ct);
        await BillingRequestScope.LockPurchaseAsync(db, id, ct);
        var purchase = await db.Set<BillingPurchaseRow>().SingleOrDefaultAsync(row => row.Id == id, ct);
        if (purchase is null)
        {
            if (await db.Set<BillingPurchaseRow>().CountAsync(row => row.CustomerId == customer.Id && !row.Verified, ct) >= 32)
                throw new BillingRefusal(413, "BillingUnverifiedWorkBound");
            purchase = new() { Id = id, Issuer = customer.Distribution, Environment = customer.Environment, CustomerId = customer.Id,
                Product = hint, State = BillingPurchaseState.Pending,
                ProtectedReference = protection.CreateProtector("zanance-billing-provider-reference-v1").Protect(reference) };
            db.Add(purchase);
        }
        await RequireQueryAccessAsync(db, customer, command, purchase, user, time.GetUtcNow(), ct);
        return [id];
    }

    /// <summary>A refresh cannot take another user's Pro reference; explicit store-local restoration is separately verified.</summary>
    private static async Task RequireQueryAccessAsync(ServiceDbContext db, BillingCustomerRow customer, BillingCommand command,
        BillingPurchaseRow purchase, string? user, DateTimeOffset now, CancellationToken ct)
    {
        if (purchase.Environment != customer.Environment || purchase.Verified && BillingProducts.RequiresIdentity(purchase.Product, purchase.Issuer) &&
            (user is null || user != purchase.UserId)) throw new BillingRefusal(403, "PurchaseIdentityDiffers");
        if (!purchase.Verified && purchase.CustomerId != customer.Id && purchase.LeaseUntil > now)
            throw new BillingRefusal(409, "OriginalBillingVerificationPending");
        if (purchase.Verified && command.Operation != BillingOperation.RestorePurchase && purchase.CustomerId != customer.Id &&
            (user is null || purchase.UserId != user) && !await db.Set<BillingDevicePurchaseRow>().AnyAsync(row => row.CustomerId == customer.Id && row.PurchaseId == purchase.Id, ct))
            throw new BillingRefusal(403, "ExplicitStoreRestoreRequired");
    }

    /// <summary>Resolves only immutable previously verified chain aliases.</summary>
    private static async Task<string> ResolveAliasAsync(ServiceDbContext db, string id, CancellationToken ct)
        => await db.Set<BillingPurchaseAliasRow>().Where(row => row.Id == id).Select(row => row.PurchaseId).SingleOrDefaultAsync(ct) ?? id;
    /// <summary>Reads the exact original finite queue; never discovers or replaces work during retry.</summary>
    private BillingPurchaseWork ReadWork(BillingCommandRow row)
    {
        var json = protection.CreateProtector("zanance-billing-work-v1").Unprotect(row.ProtectedWork);
        var work = StrictJsonPayload.Deserialize(System.Text.Encoding.UTF8.GetBytes(json), BillingProviderJson.Default.BillingPurchaseWork, 65536);
        if (work.PurchaseIds.Length > 32 || work.PurchaseIds.Distinct(StringComparer.Ordinal).Count() != work.PurchaseIds.Length ||
            work.PurchaseIds.Any(id => id.Length != 64 || id.Any(c => !char.IsAsciiHexDigit(c))))
            throw new BillingRefusal(503, "OriginalBillingWorkUnavailable");
        return work;
    }

    /// <summary>Terminal failure is not a live task; release only this original lease without resetting its journal.</summary>
    private async Task ReleaseFailedLeaseAsync(BillingPurchaseLease lease)
    {
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await using var db = await factory.CreateDbContextAsync(cleanup.Token);
            await db.Set<BillingPurchaseRow>().Where(row => row.Id == lease.PurchaseId && row.LeaseOwner == lease.LeaseOwner)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.LeaseOwner, (Guid?)null)
                    .SetProperty(row => row.LeaseUntil, (DateTimeOffset?)null), cleanup.Token);
        }
        catch (Exception error) when (error is Npgsql.NpgsqlException or OperationCanceledException or InvalidOperationException)
        { /* Unavailable cleanup retains the finite original lease; it never destroys uncertain work. */ }
    }
}
