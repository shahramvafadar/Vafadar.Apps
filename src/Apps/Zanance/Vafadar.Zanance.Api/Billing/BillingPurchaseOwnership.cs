using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Verified provider account owner, never a client-supplied purchase owner.</summary>
internal sealed record BillingPurchaseOwner(Guid CustomerId, string? UserId);

/// <summary>Store-local restoration and identity-bound Pro have deliberately different ownership rules.</summary>
internal static class BillingPurchaseOwnership
{
    /// <summary>Checks original provider account binding and current identity under the purchase lock.</summary>
    internal static async Task<BillingPurchaseOwner> ResolveAsync(ServiceDbContext db, BillingCustomerRow customer,
        BillingPurchaseRow purchase, BillingVerifiedPurchase verified, string? user, bool restore, CancellationToken ct, string? expectedReplacementId = null)
    {
        if (verified.Issuer != customer.Distribution && !purchase.Verified || verified.Environment != customer.Environment ||
            verified.AccountBinding.Length is < 1 or > 128 ||
            !BillingProducts.RequiresIdentity(verified.Facts.Product, verified.Issuer) && verified.Issuer != customer.Distribution)
            throw new BillingRefusal(422, "StoreAccountBindingRefused");
        if (purchase.Verified)
        {
            if (purchase.Issuer != verified.Issuer || purchase.Environment != verified.Environment ||
                purchase.ProviderAccountBinding != verified.AccountBinding)
                throw new BillingRefusal(409, "OriginalStoreAccountBindingDiffers");
            if (BillingProducts.RequiresIdentity(verified.Facts.Product, verified.Issuer) && (user is null || purchase.UserId != user))
                throw new BillingRefusal(403, "PurchaseIdentityDiffers");
            if (!restore && purchase.CustomerId != customer.Id && (user is null || purchase.UserId != user) &&
                !await db.Set<BillingDevicePurchaseRow>().AnyAsync(row => row.CustomerId == customer.Id && row.PurchaseId == purchase.Id, ct))
                throw new BillingRefusal(403, "ExplicitStoreRestoreRequired");
            return new(purchase.CustomerId, purchase.UserId ?? (restore ? user : null));
        }
        var local = await db.Set<BillingCustomerRow>().SingleOrDefaultAsync(row => row.Distribution == verified.Issuer &&
            row.Environment == verified.Environment && row.AnonymousAccountBinding == verified.AccountBinding, ct);
        BillingIdentityRow? identity;
        if (verified.Issuer == BillingIssuer.GooglePlay)
            identity = await db.Set<BillingIdentityRow>().SingleOrDefaultAsync(row => row.Environment == verified.Environment &&
                row.GoogleAccountBinding == verified.AccountBinding, ct);
        else if (verified.Issuer == BillingIssuer.AppStore && Guid.TryParseExact(verified.AccountBinding, "D", out var token))
            identity = await db.Set<BillingIdentityRow>().SingleOrDefaultAsync(row => row.Environment == verified.Environment && row.AccountToken == token, ct);
        else throw new BillingRefusal(422, "StoreAccountBindingRefused");
        if (local is not null && identity is not null) throw new BillingRefusal(409, "AmbiguousOriginalStoreBinding");
        if (BillingProducts.RequiresIdentity(verified.Facts.Product, verified.Issuer))
        {
            if (identity is null || user is null || identity.UserId != user)
            {
                // Play may retain the original obfuscated account id when an anonymous Plus purchase is
                // upgraded after explicit identity association. Only its freshly verified replacement chain,
                // locked existing purchase and same already associated identity can carry that ownership.
                if (verified.Issuer == BillingIssuer.GooglePlay && user is not null && verified.ReplacedReference is { } previous)
                {
                    var id = BillingAuthority.PurchaseId(verified.Issuer, verified.Environment, previous);
                    if (expectedReplacementId is null || expectedReplacementId == id)
                    {
                        var canonical = await db.Set<BillingPurchaseAliasRow>().Where(row => row.Id == id).Select(row => row.PurchaseId).SingleOrDefaultAsync(ct) ?? id;
                        var old = await db.Set<BillingPurchaseRow>().SingleOrDefaultAsync(row => row.Id == canonical && row.Verified &&
                            row.Issuer == verified.Issuer && row.Environment == verified.Environment && row.UserId == user &&
                            row.ProviderAccountBinding == verified.AccountBinding && row.Product != BillingProduct.PlusLifetime, ct);
                        if (old is not null) return new(old.CustomerId, user);
                    }
                }
                throw new BillingRefusal(403, "CurrentPurchaseIdentityRequired");
            }
            return new(customer.Id, user);
        }
        // Without an account, an explicit actual-store restore is possession of the high-entropy
        // verified store purchase reference. It does not link Pro, transfer identity or cross stores.
        if (verified.Issuer != customer.Distribution || local is null && identity is null)
            throw new BillingRefusal(422, "OriginalPurchaseRegistrationRequired");
        if (local is not null)
        {
            if (local.Id != customer.Id && !restore) throw new BillingRefusal(403, "ExplicitStoreRestoreRequired");
            return new(local.Id, restore ? user : null);
        }
        if (!restore && identity!.UserId != user) throw new BillingRefusal(403, "PurchaseIdentityDiffers");
        var originalCustomer = await db.Set<BillingCustomerRow>().Where(row => row.UserId == identity!.UserId &&
            row.Environment == verified.Environment && row.Distribution == verified.Issuer).OrderBy(row => row.CreatedAt)
            .Select(row => row.Id).FirstOrDefaultAsync(ct);
        if (originalCustomer == Guid.Empty) throw new BillingRefusal(422, "OriginalPurchaseRegistrationRequired");
        return new(originalCustomer, identity!.UserId);
    }
}
