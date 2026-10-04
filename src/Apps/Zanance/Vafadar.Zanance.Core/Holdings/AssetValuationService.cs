namespace Vafadar.Zanance.Core.Holdings;

/// <summary>The value of one asset type at a date: known with its price date, or unknown (never zero, AT13).</summary>
/// <param name="AssetType">The asset type.</param>
/// <param name="Quantity">The quantity at the date.</param>
/// <param name="Value">The value in minor units of the price currency; <see langword="null"/> without a price.</param>
/// <param name="PriceDate">The date of the price used.</param>
/// <param name="FromPurchase">Whether the price was taken from a purchase.</param>
public sealed record HoldingValue(AssetType AssetType, long Quantity, long? Value, DateOnly? PriceDate, bool FromPurchase);

/// <summary>The fine-metal equivalent of one metal over the types with a known purity (design §7.3).</summary>
/// <param name="Metal">The metal.</param>
/// <param name="FineMassMg">Σ mass × purity in mg.</param>
/// <param name="TypeCount">How many types are included.</param>
/// <param name="ExcludedTypeCount">Types of this metal without purity or weight, left out and listed.</param>
public sealed record FineMetal(Metal Metal, long FineMassMg, int TypeCount, int ExcludedTypeCount);

/// <summary>
/// Values holdings with their dated prices (design §7.7): value(t) = quantity(t) × the latest price of the type on or
/// before t. A price after t is never used, so a new price never rewrites history (ZEX-AS13); a type without a price
/// has value unknown and makes valued totals incomplete (ZEX-AS06).
/// </summary>
public static class AssetValuationService
{
    /// <summary>Returns the latest price of a type on or before <paramref name="at"/>, or <see langword="null"/>.</summary>
    public static AssetValuation? PriceAt(IEnumerable<AssetValuation> valuations, Guid assetTypeId, DateOnly at) =>
        valuations.Where(v => v.AssetTypeId == assetTypeId && v.Date <= at).OrderByDescending(v => v.Date).ThenByDescending(v => v.CreatedAt).FirstOrDefault();

    /// <summary>Returns the value of <paramref name="quantity"/> base units at a price per gram or unit (minor × 1,000).</summary>
    public static long ValueOf(long quantity, long pricePerUnitMilli) =>
        (long)Math.Round((decimal)quantity * pricePerUnitMilli / (Quantities.PerGramOrUnit * 1_000m), MidpointRounding.AwayFromZero);

    /// <summary>Returns the price per gram or unit (minor × 1,000) of a total value of a holding (the "total" input mode).</summary>
    public static long PricePerUnitMilliOf(long totalValue, long quantity) =>
        quantity <= 0 ? 0 : (long)Math.Round((decimal)totalValue * Quantities.PerGramOrUnit * 1_000m / quantity, MidpointRounding.AwayFromZero);

    /// <summary>Returns the value of every type with a quantity at <paramref name="at"/>.</summary>
    public static IReadOnlyList<HoldingValue> Values(IEnumerable<AssetType> types, IReadOnlyCollection<AssetEvent> events, IReadOnlyCollection<AssetValuation> valuations, DateOnly at)
    {
        ArgumentNullException.ThrowIfNull(types);
        var result = new List<HoldingValue>();
        foreach (var type in types)
        {
            var quantity = HoldingsLedger.Quantity(events, type.Id, at);
            if (quantity == 0)
            {
                continue;
            }

            var price = PriceAt(valuations, type.Id, at);
            result.Add(new HoldingValue(type, quantity, price is null ? null : ValueOf(quantity, price.PricePerUnitMilli), price?.Date, price?.Source == ValuationSource.Purchase));
        }

        return result;
    }

    /// <summary>Returns the fine-metal equivalent per metal (gold, silver, …) of the types with a known purity.</summary>
    public static IReadOnlyList<FineMetal> FineMetals(IEnumerable<HoldingValue> values) =>
    [
        .. values
            .Where(v => v.AssetType.Metal is not (Metal.None or Metal.Other))
            .GroupBy(v => v.AssetType.Metal)
            .Select(g =>
            {
                var fine = g.Select(v => Quantities.FineMassMg(v.Quantity, v.AssetType)).ToList();
                return new FineMetal(g.Key, fine.Where(f => f is not null).Sum(f => f!.Value), fine.Count(f => f is not null), fine.Count(f => f is null));
            })
            .Where(f => f.TypeCount > 0),
    ];
}
