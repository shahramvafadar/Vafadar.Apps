using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

internal sealed partial class BillingPurchaseCoordinator
{
    /// <summary>Rechecks current session and exact lease after HTTP, then commits verified ownership and its original queue prefix.</summary>
    private async Task AcceptAsync(Guid customerId, string signed, ClaimsPrincipal actor, BillingPurchaseLease lease,
        BillingVerifiedPurchase verified, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var scope = await BillingRequestScope.OpenAsync(db, actor, time, ct);
        await BillingRequestScope.LockCustomerAsync(db, customerId, ct);
        var customer = await db.Set<BillingCustomerRow>().SingleOrDefaultAsync(row => row.Id == customerId, ct)
            ?? throw new BillingRefusal(404, "BillingDeviceUnavailable");
        var now = time.GetUtcNow();
        var command = BillingRegistry.Verify(customer, signed, scope.UserId, now, requireFresh: false);
        // A cold SDK query identifies a subscription SKU, not its base plan. Fresh provider facts
        // must match the original reviewed product before issuing a native completion receipt.
        if (command.NativePaymentId is not null && command.ProductHint != (verified.Facts.ScheduledChange?.Product ?? verified.Facts.Product))
            throw new BillingRefusal(422, "OriginalNativeProductDiffers");
        if (command.NativePaymentId is not null && verified.Facts.ScheduledChange is not null && command.ExpectedReplacementId is null)
            throw new BillingRefusal(422, "OriginalReplacementBindingRequired");
        if (command.ExpectedReplacementId is { } expected && (verified.ReplacedReference is null ||
            BillingAuthority.PurchaseId(verified.Issuer, verified.Environment, verified.ReplacedReference) != expected))
            throw new BillingRefusal(422, "OriginalReplacedPurchaseDiffers");
        var journal = await BillingCommandStore.ReadOriginalAsync(db, customerId, command.OperationId, signed, ct)
            ?? throw new BillingRefusal(409, "OriginalBillingWorkUnavailable");
        var work = ReadWork(journal);
        if (journal.ProtectedResponse.Length != 0 || journal.CompletedWork != lease.Index || lease.Index >= work.PurchaseIds.Length)
            throw new BillingRefusal(409, "OriginalBillingWorkChanged");
        var canonicalId = BillingAuthority.PurchaseId(verified.Issuer, verified.Environment, verified.Canonical);
        var replaced = verified.ReplacedReference is { } previous
            ? await ResolveAliasAsync(db, BillingAuthority.PurchaseId(verified.Issuer, verified.Environment, previous), ct) : null;
        foreach (var id in new[] { lease.PurchaseId, canonicalId, replaced }.OfType<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            await BillingRequestScope.LockPurchaseAsync(db, id, ct);
        var reserved = await db.Set<BillingPurchaseRow>().SingleOrDefaultAsync(row => row.Id == lease.PurchaseId, ct)
            ?? throw new BillingRefusal(409, "OriginalBillingWorkUnavailable");
        if (reserved.LeaseOwner != lease.LeaseOwner || reserved.LeaseUntil <= now ||
            await ResolveAliasAsync(db, work.PurchaseIds[lease.Index], ct) != reserved.Id)
            throw new BillingRefusal(409, "BillingVerificationSuperseded");
        BillingPurchaseRow purchase;
        if (canonicalId == reserved.Id) purchase = reserved;
        else
        {
            if (reserved.Verified) throw new BillingRefusal(409, "OriginalStoreChainDiffers");
            purchase = await db.Set<BillingPurchaseRow>().SingleOrDefaultAsync(row => row.Id == canonicalId, ct)
                ?? new() { Id = canonicalId, Issuer = verified.Issuer, Environment = verified.Environment,
                    CustomerId = customer.Id, Product = verified.Facts.Product };
            if (purchase.Verified && purchase.VerifiedAt > lease.StartedAt || purchase.LeaseUntil > now)
                throw new BillingRefusal(409, "BillingVerificationSuperseded");
            if (db.Entry(purchase).State == EntityState.Detached) db.Add(purchase);
        }
        var owner = await BillingPurchaseOwnership.ResolveAsync(db, customer, purchase, verified, scope.UserId,
            command.Operation == BillingOperation.RestorePurchase, ct, command.ExpectedReplacementId);
        if (verified.Facts.StartsAt > now.AddMinutes(1)) throw new BillingRefusal(422, "StoreTimeRefused");
        var facts = BillingAcceptedTerms.Preserve(purchase, verified.Facts);
        purchase.CustomerId = owner.CustomerId; purchase.UserId = owner.UserId;
        purchase.ProviderAccountBinding = verified.AccountBinding; purchase.Product = facts.Product;
        purchase.State = facts.State; purchase.AccessUntil = facts.State == BillingPurchaseState.Grace
            ? facts.GraceUntil : facts.PaidUntil;
        purchase.FactsJson = JsonSerializer.Serialize(facts, BillingJson.Default.BillingPurchaseFacts);
        purchase.ProtectedReference = protection.CreateProtector("zanance-billing-provider-reference-v1").Protect(verified.Canonical);
        purchase.Verified = true; purchase.VerifiedAt = now; purchase.Revision = checked(purchase.Revision + 1);
        purchase.RecheckFailures = 0; purchase.RecheckAt = purchase.RequestedRecheckRevision != lease.ObservedRecheckRevision
            ? now : BillingReconciliation.NextCheck(now, facts);
        purchase.AcknowledgePending = verified.Issuer == BillingIssuer.GooglePlay && !verified.Facts.Acknowledged &&
            verified.Facts.State is BillingPurchaseState.Active or BillingPurchaseState.Grace;
        if (purchase.AcknowledgePending && purchase.RecheckAt > now.AddMinutes(1)) purchase.RecheckAt = now.AddMinutes(1);
        purchase.LeaseOwner = null; purchase.LeaseUntil = null;
        var alias = await db.Set<BillingPurchaseAliasRow>().SingleOrDefaultAsync(row => row.Id == lease.PurchaseId, ct);
        if (alias is not null && alias.PurchaseId != canonicalId) throw new BillingRefusal(409, "OriginalStoreChainDiffers");
        if (alias is null) db.Add(new BillingPurchaseAliasRow { Id = lease.PurchaseId, PurchaseId = canonicalId });
        if (reserved.Id != purchase.Id) db.Remove(reserved);
        if (!await db.Set<BillingDevicePurchaseRow>().AnyAsync(row => row.CustomerId == customerId && row.PurchaseId == purchase.Id, ct))
            db.Add(new BillingDevicePurchaseRow { CustomerId = customerId, PurchaseId = purchase.Id });
        if (replaced is not null && replaced != purchase.Id)
        {
            var old = await db.Set<BillingPurchaseRow>().SingleOrDefaultAsync(row => row.Id == replaced && row.Verified, ct);
            if (old is not null)
            {
                if (old.Product == BillingProduct.PlusLifetime || old.UserId is not null && old.UserId != owner.UserId)
                    throw new BillingRefusal(403, "ReplacedPurchaseOwnerDiffers");
                var oldFacts = StrictJsonPayload.Deserialize(System.Text.Encoding.UTF8.GetBytes(old.FactsJson), BillingJson.Default.BillingPurchaseFacts, 8192);
                oldFacts = oldFacts with { State = BillingPurchaseState.Revoked, AutoRenews = false, GraceUntil = null, ScheduledChange = null };
                oldFacts.Validate(); old.State = oldFacts.State; old.FactsJson = JsonSerializer.Serialize(oldFacts, BillingJson.Default.BillingPurchaseFacts);
                // The fresh verified replacement is also the authoritative current revocation observation
                // for its old chain. Keep that original source visible among bounded recent terminal facts.
                old.AcknowledgePending = false; old.VerifiedAt = now; old.Revision = checked(old.Revision + 1);
            }
        }
        journal.CompletedWork = checked(journal.CompletedWork + 1); journal.LastState = verified.Facts.State;
        await scope.CommitAsync(ct);
    }

    /// <summary>Actual provider acknowledgement follows committed grant persistence; failure stays durable for a worker.</summary>
    private async Task TryAcknowledgeAsync(IBillingProvider provider, BillingVerifiedPurchase verified, CancellationToken ct)
    {
        if (verified.Issuer != BillingIssuer.GooglePlay || verified.Facts.Acknowledged ||
            verified.Facts.State is not (BillingPurchaseState.Active or BillingPurchaseState.Grace)) return;
        try
        {
            await provider.AcknowledgeAsync(verified, ct);
            var id = BillingAuthority.PurchaseId(verified.Issuer, verified.Environment, verified.Canonical);
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await BillingRequestScope.LockPurchaseAsync(db, id, ct);
            var row = await db.Set<BillingPurchaseRow>().SingleOrDefaultAsync(p => p.Id == id && p.Verified, ct);
            if (row is not null && row.AcknowledgePending)
            {
                // A concurrent newer revocation must not be replaced by this older provider result.
                row.AcknowledgePending = false;
                var current = StrictJsonPayload.Deserialize(System.Text.Encoding.UTF8.GetBytes(row.FactsJson), BillingJson.Default.BillingPurchaseFacts, 8192);
                row.FactsJson = JsonSerializer.Serialize(current with { Acknowledged = true }, BillingJson.Default.BillingPurchaseFacts);
                await db.SaveChangesAsync(ct);
            }
            await transaction.CommitAsync(ct);
        }
        catch (BillingRefusal) { /* The original purchase remains committed and its acknowledgement work remains pending. */ }
    }
}
