namespace Vafadar.Zanance.Core.Holdings;

/// <summary>The quantity of one asset type at one location.</summary>
public sealed record HoldingPosition(Guid AssetTypeId, Guid LocationId, long Quantity);

/// <summary>A point in the history where a holding would become negative (design §7.5).</summary>
/// <param name="AssetTypeId">The asset type.</param>
/// <param name="LocationId">The location.</param>
/// <param name="Date">The first date the quantity would be negative.</param>
/// <param name="Available">What was held there on that date before the event (≥ 0).</param>
public sealed record HoldingConflict(Guid AssetTypeId, Guid LocationId, DateOnly Date, long Available);

/// <summary>The realised result of one sale (design §7.6); <see cref="Result"/> is <see langword="null"/> with an unknown basis.</summary>
public sealed record SaleResult(AssetEvent Sale, long? RemovedBasis, long? Result);

/// <summary>The cost basis of an asset type after all events (weighted average cost, ZEX-P08).</summary>
/// <param name="Quantity">The total quantity.</param>
/// <param name="Basis">The remaining basis in minor units of the price currency; <see langword="null"/> when unknown.</param>
/// <param name="Sales">Each sale with its removed basis and realised result.</param>
public sealed record CostBasis(long Quantity, long? Basis, IReadOnlyList<SaleResult> Sales)
{
    /// <summary>Gets the basis per gram or unit in minor units × 1,000, or <see langword="null"/>.</summary>
    public long? PerUnitMilli => Basis is { } basis && Quantity > 0 ? basis * Quantities.PerGramOrUnit * 1_000 / Quantity : null;
}

/// <summary>
/// Running quantities, the validation over the full history and the cost basis of holdings (design §7.4–§7.6). Pure
/// functions over the events; all arithmetic is checked, so an overflow fails instead of showing a wrong quantity.
/// </summary>
public static class HoldingsLedger
{
    /// <summary>Returns the quantity per asset type and location at the end of <paramref name="at"/>, without zero lines.</summary>
    public static IReadOnlyList<HoldingPosition> Positions(IEnumerable<AssetEvent> events, DateOnly at)
    {
        ArgumentNullException.ThrowIfNull(events);
        var totals = new Dictionary<(Guid Type, Guid Location), long>();
        foreach (var e in events.Where(e => e.Date <= at))
        {
            Add(totals, (e.AssetTypeId, e.LocationId), e.EffectAt(e.LocationId));
            if (e.Kind == AssetEventKind.LocationTransfer && e.ToLocationId is { } to)
            {
                Add(totals, (e.AssetTypeId, to), e.EffectAt(to));
            }
        }

        return [.. totals.Where(t => t.Value != 0).Select(t => new HoldingPosition(t.Key.Type, t.Key.Location, t.Value))];
    }

    /// <summary>Returns the total quantity of an asset type at the end of <paramref name="at"/>.</summary>
    public static long Quantity(IEnumerable<AssetEvent> events, Guid assetTypeId, DateOnly at) =>
        events.Where(e => e.AssetTypeId == assetTypeId && e.Date <= at).Aggregate(0L, (sum, e) => checked(sum + e.TotalEffect));

    /// <summary>
    /// Returns the first point where any holding would be negative (design §7.5, AT16), or <see langword="null"/>. Call it
    /// with the events as they would be after an add, edit or delete: the whole history is checked, not only today.
    /// Events of one day count increases before decreases, so buying and selling on the same day works.
    /// </summary>
    public static HoldingConflict? FindConflict(IEnumerable<AssetEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var changes = new List<(Guid Type, Guid Location, DateOnly Date, long Delta)>();
        foreach (var e in events)
        {
            changes.Add((e.AssetTypeId, e.LocationId, e.Date, e.EffectAt(e.LocationId)));
            if (e.Kind == AssetEventKind.LocationTransfer && e.ToLocationId is { } to)
            {
                changes.Add((e.AssetTypeId, to, e.Date, e.EffectAt(to)));
            }
        }

        foreach (var holding in changes.GroupBy(c => (c.Type, c.Location)))
        {
            long running = 0;
            foreach (var day in holding.GroupBy(c => c.Date).OrderBy(g => g.Key))
            {
                var before = running;
                running = checked(running + day.Where(c => c.Delta > 0).Sum(c => c.Delta));
                var available = running;
                running = checked(running + day.Where(c => c.Delta < 0).Sum(c => c.Delta));
                if (running < 0)
                {
                    return new HoldingConflict(holding.Key.Type, holding.Key.Location, day.Key, Math.Max(before, available));
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the cost basis of one asset type after all its events, in date order (design §7.6): purchases and valued
    /// gifts or openings add their basis; every outflow removes basis × q / Q; transfers change nothing. An opening or
    /// gift without a value makes the basis unknown from that event on; sales then show no result.
    /// </summary>
    public static CostBasis Basis(IEnumerable<AssetEvent> events, Guid assetTypeId)
    {
        ArgumentNullException.ThrowIfNull(events);
        long quantity = 0;
        long? basis = 0;
        var sales = new List<SaleResult>();
        foreach (var e in events.Where(e => e.AssetTypeId == assetTypeId).OrderBy(e => e.Date).ThenBy(e => e.TotalEffect < 0).ThenBy(e => e.CreatedAt))
        {
            switch (e.Kind)
            {
                case AssetEventKind.Opening or AssetEventKind.Purchase or AssetEventKind.GiftReceived:
                    quantity = checked(quantity + e.Quantity);
                    basis = basis is { } known && e.BasisAmount is { } added ? checked(known + added) : null;
                    break;
                case AssetEventKind.Correction when e.IsIncrease:
                    // A found quantity adds no money: the basis per unit falls, as with any free addition.
                    quantity = checked(quantity + e.Quantity);
                    break;
                case AssetEventKind.Sale or AssetEventKind.Outflow or AssetEventKind.Correction:
                    var removed = basis is { } current && quantity > 0 ? RoundedShare(current, e.Quantity, quantity) : (long?)null;
                    quantity = checked(quantity - e.Quantity);
                    basis = basis is { } b && removed is { } r ? b - r : null;
                    if (e.Kind == AssetEventKind.Sale)
                    {
                        sales.Add(new SaleResult(e, removed, removed is { } cost && e.ProceedsAmount is { } proceeds ? proceeds - cost : null));
                    }

                    break;
            }
        }

        return new CostBasis(quantity, quantity == 0 ? 0 : basis, sales);
    }

    // basis × q / Q, rounded half away from zero to the minor unit.
    private static long RoundedShare(long basis, long part, long whole) =>
        (long)Math.Round((decimal)basis * part / whole, MidpointRounding.AwayFromZero);

    private static void Add(Dictionary<(Guid, Guid), long> totals, (Guid, Guid) key, long delta) =>
        totals[key] = checked(totals.GetValueOrDefault(key) + delta);
}
