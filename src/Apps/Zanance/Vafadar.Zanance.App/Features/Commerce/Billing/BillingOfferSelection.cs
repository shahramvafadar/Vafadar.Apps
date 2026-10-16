using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data.Billing;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Joins actual SDK ownership with fresh signed grants; no raw provider reference enters a presented offer.</summary>
internal static class BillingOfferSelection
{
    /// <summary>Matches only the retained native target's old/new subscription SKU during a cold owned-purchase query.</summary>
    internal static BillingProduct? OriginalNativeProduct(BillingClientView view, NativeBillingPurchase purchase,
        IReadOnlyList<BillingProductMapping> catalog)
    {
        if (view.NativePaymentId is null || view.NativeProduct is not { } target || purchase.StoreProductId is null) return null;
        var source = view.NativeReplacementId is { } id
            ? view.Snapshot?.Grants.SingleOrDefault(grant => grant.PurchaseId == id)?.Facts.Product : null;
        return catalog.Any(mapping => (mapping.Product == target || source is not null && mapping.Product == source) &&
            mapping.StoreProductId == purchase.StoreProductId) ? target : null;
    }

    /// <summary>Offers one explicit replacement instead of a parallel subscription, or explains missing restore/management authority.</summary>
    internal static ProductBillingPresentation Select(BillingIssuer distribution, BillingEnvironment environment,
        BillingClientView local, BillingClientView? identity, IReadOnlyList<BillingProductMapping> catalog,
        IReadOnlyList<NativeBillingPurchase> owned, IReadOnlyList<NativeBillingOffer> offered, string country, bool hasIdentity)
    {
        if (owned.Count > 32 || offered.Count > 40) throw new InvalidDataException("Native purchase selection bound exceeded.");
        var current = identity?.Snapshot ?? local.Snapshot ?? throw new InvalidDataException("Fresh store context required.");
        var needsAssociation = false;
        if (owned.Any(purchase => purchase.Pending)) return Result(ProductBillingStatus.Pending, []);
        var grants = (local.Snapshot?.Grants ?? []).Concat(identity?.Snapshot?.Grants ?? [])
            .Where(grant => grant.Environment == environment).GroupBy(grant => (grant.Issuer, grant.PurchaseId))
            .Select(group => group.OrderByDescending(grant => grant.MatchingIdentity).ThenByDescending(grant => grant.VerifiedAt).First()).ToArray();
        var nativeSubscriptions = new List<(NativeBillingPurchase Native, BillingCachedGrant Grant)>();
        foreach (var purchase in owned)
        {
            var id = BillingAccountBinding.PurchaseId(distribution, environment, purchase.Reference);
            var grant = grants.SingleOrDefault(item => item.Issuer == distribution && item.PurchaseId == id);
            if (grant is null || purchase.StoreProductId is null || !catalog.Any(mapping => mapping.Product == grant.Facts.Product && mapping.StoreProductId == purchase.StoreProductId))
                return Result(ProductBillingStatus.RecoveryRequired, []); // Explicit Restore, never an implicit identity link.
            if (BillingProducts.Period(grant.Facts.Product) != BillingPeriod.None) nativeSubscriptions.Add((purchase, grant));
        }
        if (grants.Any(grant => grant.Issuer == distribution && BillingProducts.Period(grant.Facts.Product) != BillingPeriod.None &&
            grant.Facts.HasAccessAt(current.ServerTime) && !nativeSubscriptions.Any(item => item.Grant.PurchaseId == grant.PurchaseId)))
            return Result(ProductBillingStatus.ManageExisting, []); // A different active store account cannot replace this signed purchase.
        if (nativeSubscriptions.Count > 1 || nativeSubscriptions.Any(item => item.Grant.Facts.State is not BillingPurchaseState.Active ||
            !item.Grant.Facts.HasAccessAt(current.ServerTime) || item.Grant.Facts.ScheduledChange is not null))
            return Result(ProductBillingStatus.ManageExisting, []);
        var existing = nativeSubscriptions.SingleOrDefault();
        var otherSubscription = grants.Any(grant => grant.Issuer != distribution && BillingProducts.Period(grant.Facts.Product) != BillingPeriod.None &&
            grant.Facts.HasAccessAt(current.ServerTime));
        needsAssociation = hasIdentity && existing.Grant is { } old && BillingProducts.Plan(old.Facts.Product) == ProductPlan.Plus &&
            identity?.Snapshot?.Grants.Any(grant => grant.PurchaseId == old.PurchaseId && grant.MatchingIdentity) != true;
        var choices = new List<NativeBillingOffer>();
        foreach (var offer in offered)
        {
            var pro = BillingProducts.Plan(offer.Offer.Product) == ProductPlan.Pro;
            if (!Eligible(offer, pro ? identity : local, hasIdentity, country)) continue;
            if (offer.Offer.Product == BillingProduct.PlusLifetime)
            {
                if (grants.Any(grant => grant.Issuer == distribution && grant.Facts.Product == BillingProduct.PlusLifetime && grant.Facts.HasAccessAt(current.ServerTime))) continue;
                choices.Add(offer with { ExistingSubscription = existing.Grant is not null || otherSubscription }); continue;
            }
            if (otherSubscription || pro && needsAssociation) continue;
            if (existing.Grant is null) { choices.Add(offer); continue; }
            if (offer.StoreProductId is null) continue;
            var replacement = BillingReplacementPolicy.Create(existing.Grant, offer.Offer.Product,
                existing.Native.StoreProductId!, offer.StoreProductId, current.ServerTime);
            if (replacement is not null) choices.Add(offer with { Replacement = replacement, ExistingSubscription = true });
        }
        var status = choices.Count != 0 ? ProductBillingStatus.Ready : existing.Grant is not null || otherSubscription
            ? ProductBillingStatus.ManageExisting : ProductBillingStatus.NotConfigured;
        return Result(status, choices);

        ProductBillingPresentation Result(ProductBillingStatus status, IReadOnlyList<NativeBillingOffer> choices)
            => new(status, local, identity, choices, true, hasIdentity,
                (status is ProductBillingStatus.Ready or ProductBillingStatus.ManageExisting) && needsAssociation);
    }

    private static bool Eligible(NativeBillingOffer offer, BillingClientView? view, bool hasIdentity, string country)
    {
        if (view?.Snapshot is not { } snapshot || view.Decision.NeedsVerification || snapshot.Retired) return false;
        var lifetime = snapshot.Grants.Any(grant => grant.Facts.Product == BillingProduct.PlusLifetime && grant.Facts.HasAccessAt(snapshot.ServerTime) &&
            (BillingProducts.Plan(offer.Offer.Product) != ProductPlan.Pro || grant.MatchingIdentity));
        return Currencies.TryGet(offer.Offer.Phases.FirstOrDefault()?.Currency, out var currency) && BillingOffers.IsEligible(offer.Offer,
            lifetime, hasIdentity, country, snapshot.ServerTime, currency.MinorDigits);
    }
}
