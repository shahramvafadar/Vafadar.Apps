namespace Vafadar.Zanance.Core.Commerce;

/// <summary>One separately configured offer, never a stack of discounts.</summary>
public enum BillingOfferKind
{
    /// <summary>Normal catalog purchase and renewal.</summary>
    Standard,
    /// <summary>Store-eligible14-day Pro introduction.</summary>
    ProTrial,
    /// <summary>Verified Lifetime owner's25-percent first annual Pro year.</summary>
    LifetimeAnnual,
    /// <summary>Explicit country/time-scoped store campaign.</summary>
    Campaign,
}

/// <summary>Actual store price phase; provider micro-units are not ledger money or an app-generated charge.</summary>
public sealed record BillingPricePhase(long PriceMicros, string Currency, string FormattedPrice, string Period,
    int Cycles, bool Recurring);

/// <summary>Actual store catalog result joined to a trusted configured offer definition.</summary>
public sealed record BillingStoreOffer(string Id, BillingProduct Product, BillingOfferKind Kind,
    IReadOnlyList<BillingPricePhase> Phases, bool StoreEligible, DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null, string? Country = null);

/// <summary>Eligibility comes from verified purchase/store/catalog facts, never from a profile switch or backup.</summary>
public static class BillingOffers
{
    /// <summary>Refuses unsupported phases, free-price tricks and stacked/mislabelled introductory periods.</summary>
    public static bool IsEligible(BillingStoreOffer offer, bool verifiedLifetime, bool verifiedProIdentity,
        string storeCountry, DateTimeOffset trustedNow, int currencyMinorDigits)
    {
        if (currencyMinorDigits is < 0 or > 4 || !Enum.IsDefined(offer.Product) || string.IsNullOrWhiteSpace(offer.Id) || offer.Id.Length > 128 || !offer.StoreEligible || !Enum.IsDefined(offer.Kind) ||
            offer.Phases.Count is < 1 or > 3 || offer.Phases.Any(p => p.PriceMicros < 0 || p.Currency.Length != 3 ||
                string.IsNullOrWhiteSpace(p.FormattedPrice) || p.FormattedPrice.Length > 128 || p.Cycles < 0 || p.Period.Length > 16)) return false;
        var plan = BillingProducts.Plan(offer.Product); var period = BillingProducts.Period(offer.Product);
        if (plan == ProductPlan.Pro && !verifiedProIdentity || verifiedLifetime && plan == ProductPlan.Plus) return false;
        if (offer.Phases.Select(p => p.Currency).Distinct(StringComparer.Ordinal).Count() != 1) return false;
        var first = offer.Phases[0]; var last = offer.Phases[^1];
        var ordinaryPeriod = period == BillingPeriod.Month ? "P1M" : period == BillingPeriod.Year ? "P1Y" : "";
        if (period == BillingPeriod.None)
            return offer.Kind is BillingOfferKind.Standard or BillingOfferKind.Campaign && offer.Phases.Count == 1 &&
                first.PriceMicros > 0 && !first.Recurring && first.Cycles == 1 && first.Period.Length == 0 && InWindow();
        if (!last.Recurring || last.PriceMicros <= 0 || last.Period != ordinaryPeriod || last.Cycles != 0) return false;
        return offer.Kind switch
        {
            BillingOfferKind.Standard => offer.Phases.Count == 1,
            BillingOfferKind.ProTrial => plan == ProductPlan.Pro && offer.Phases.Count == 2 &&
                first.PriceMicros == 0 && first.Period == "P14D" && first.Cycles == 1 && !first.Recurring,
            // Compare actual provider phases within ISO minor-unit rounding; never calculate or submit a charge.
            BillingOfferKind.LifetimeAnnual => verifiedLifetime && plan == ProductPlan.Pro && period == BillingPeriod.Year &&
                offer.Phases.Count == 2 && first.PriceMicros > 0 && first.PriceMicros < last.PriceMicros &&
                first.Period == "P1Y" && first.Cycles == 1 && !first.Recurring && LifetimePriceMatches(),
            BillingOfferKind.Campaign => offer.Phases.Count == 2 && first.PriceMicros > 0 && first.PriceMicros < last.PriceMicros &&
                first.Period == ordinaryPeriod && first.Cycles == 1 && !first.Recurring && InWindow(),
            _ => false,
        };

        bool InWindow() => offer.Kind != BillingOfferKind.Campaign ||
            offer.StartsAt is { } start && offer.EndsAt is { } end && start < end && trustedNow >= start && trustedNow < end &&
            !string.IsNullOrWhiteSpace(offer.Country) && offer.Country == storeCountry;

        bool LifetimePriceMatches()
        {
            decimal divisor = 1;
            for (var digit = 0; digit < currencyMinorDigits; digit++) divisor *= 10;
            return Math.Abs(first.PriceMicros - last.PriceMicros * 0.75m) <= 500000m / divisor;
        }
    }
}
