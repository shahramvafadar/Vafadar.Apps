using System.Text;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Previously sold capabilities are not relabeled by a later operator catalog edit.</summary>
internal static class BillingAcceptedTerms
{
    /// <summary>Preserves first accepted same-product terms and refuses impossible Lifetime chain substitution.</summary>
    internal static BillingPurchaseFacts Preserve(BillingPurchaseRow purchase, BillingPurchaseFacts current)
    {
        current.Validate();
        if (!purchase.Verified) return current;
        var previous = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(purchase.FactsJson),
            BillingJson.Default.BillingPurchaseFacts, 8192);
        previous.Validate();
        if (previous.Product != purchase.Product) throw new BillingRefusal(503, "OriginalPurchaseTermsUnavailable");
        if (previous.Product != current.Product &&
            (previous.Product == BillingProduct.PlusLifetime || current.Product == BillingProduct.PlusLifetime))
            throw new BillingRefusal(422, "OriginalStoreChainDiffers");
        if (previous.ScheduledChange is { } accepted && accepted.Product == current.Product)
            return current with { TermsVersion = accepted.TermsVersion };
        var scheduled = current.ScheduledChange;
        if (scheduled is not null && previous.ScheduledChange is { } original && scheduled.Product == original.Product && scheduled.EffectiveAt == original.EffectiveAt)
            scheduled = scheduled with { TermsVersion = original.TermsVersion };
        return previous.Product == current.Product ? current with { TermsVersion = previous.TermsVersion, ScheduledChange = scheduled } : current;
    }
}
