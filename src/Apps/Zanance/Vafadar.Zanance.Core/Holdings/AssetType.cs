using Vafadar.Core.Domain;

namespace Vafadar.Zanance.Core.Holdings;

/// <summary>What kind of thing an asset type is (ZEX-AS02). New values are appended, never renumbered.</summary>
public enum AssetKind
{
    /// <summary>Gold, silver, platinum or palladium by weight (jewellery, bars, granules).</summary>
    PreciousMetal = 0,

    /// <summary>Coins or bars counted in pieces.</summary>
    CoinOrBar = 1,

    /// <summary>Any other countable item.</summary>
    CountableItem = 2,

    /// <summary>Anything else.</summary>
    Other = 3,
}

/// <summary>How an asset type is measured (ZEX-P10). New values are appended, never renumbered.</summary>
public enum AssetDimension
{
    /// <summary>By weight, stored in milligrams; shown in g or kg.</summary>
    Mass = 0,

    /// <summary>By count, stored in thousandths of a unit.</summary>
    Count = 1,
}

/// <summary>The metal of a precious metal, coin or bar (for the fine-metal equivalent). New values are appended.</summary>
public enum Metal
{
    /// <summary>No metal or not stated.</summary>
    None = 0,

    /// <summary>Gold.</summary>
    Gold = 1,

    /// <summary>Silver.</summary>
    Silver = 2,

    /// <summary>Platinum.</summary>
    Platinum = 3,

    /// <summary>Palladium.</summary>
    Palladium = 4,

    /// <summary>Another metal.</summary>
    Other = 5,
}

/// <summary>
/// The identity of a holding (ZEX-D06, design §7.1): name, kind, dimension, metal, purity, unit weight, count unit,
/// divisibility and the currency of its prices. Quantities add up only within one asset type; 18 k and 24 k gold are
/// two types (ZEX-AS02, AT11).
/// </summary>
public sealed class AssetType : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the name, e.g. "18k gold" or "Bahar Azadi coin".</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the kind.</summary>
    public AssetKind Kind { get; set; }

    /// <summary>Gets or sets how the type is measured; fixed once events exist.</summary>
    public AssetDimension Dimension { get; set; }

    /// <summary>Gets or sets the metal of precious metals, coins and bars.</summary>
    public Metal Metal { get; set; }

    /// <summary>
    /// Gets or sets the purity in parts per 10,000 (24 k / 999.9 = 9999, 18 k = 7500); <see langword="null"/> when
    /// unknown – then no fine-metal amount is shown (ZEX-AS03).
    /// </summary>
    public int? PurityPer10000 { get; set; }

    /// <summary>Gets or sets the weight of one unit of a count type in mg; the derived weight is a view, never a second holding.</summary>
    public long? UnitWeightMg { get; set; }

    /// <summary>Gets or sets the name of one unit of a count type, e.g. "coin"; <see langword="null"/> = "piece".</summary>
    public string? CountUnitName { get; set; }

    /// <summary>Gets or sets a value indicating whether a count type allows fractions (up to 3 decimals).</summary>
    public bool Divisible { get; set; }

    /// <summary>Gets or sets the currency of prices and of the cost basis.</summary>
    public required string PriceCurrencyCode { get; set; }

    /// <summary>Gets or sets an optional note.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets the icon key.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets a value indicating whether the type is archived.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Where a holding is kept, e.g. "Home safe" or "Bank box" (design §7.1).</summary>
public sealed class AssetLocation : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets a value indicating whether the location is archived.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>What happened to a holding (design §7.4). New values are appended, never renumbered.</summary>
public enum AssetEventKind
{
    /// <summary>A holding the user already has; no money is taken from an account (AT39).</summary>
    Opening = 0,

    /// <summary>Bought with money from an account (linked asset purchase entry and fee).</summary>
    Purchase = 1,

    /// <summary>Sold for money to an account (linked asset sale entry and fee); proceeds are never income.</summary>
    Sale = 2,

    /// <summary>Moved from one location to another; the total stays.</summary>
    LocationTransfer = 3,

    /// <summary>Received as a gift or inheritance; not income.</summary>
    GiftReceived = 4,

    /// <summary>Given away, used up or otherwise gone; not spending.</summary>
    Outflow = 5,

    /// <summary>A correction of the quantity, with a reason.</summary>
    Correction = 6,
}

/// <summary>
/// One change of a holding (design §7.4). <see cref="Quantity"/> is a positive amount in the base unit of the type (mg
/// or thousandths of a unit); <see cref="Kind"/> gives its direction. Purchases and sales share a <see cref="GroupId"/>
/// with their money entry and fee, so they are saved, deleted and undone together (ZEX-AS14, AT17).
/// </summary>
public sealed class AssetEvent : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the asset type.</summary>
    public Guid AssetTypeId { get; set; }

    /// <summary>Gets or sets the location (for a transfer: the source).</summary>
    public Guid LocationId { get; set; }

    /// <summary>Gets or sets the destination of a location transfer.</summary>
    public Guid? ToLocationId { get; set; }

    /// <summary>Gets or sets the kind.</summary>
    public AssetEventKind Kind { get; set; }

    /// <summary>Gets or sets the date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the quantity in the base unit (&gt; 0).</summary>
    public long Quantity { get; set; }

    /// <summary>
    /// Gets or sets the cost basis added (purchase, gift with a value, opening with a price) in minor units of the type's
    /// price currency; <see langword="null"/> = unknown.
    /// </summary>
    public long? BasisAmount { get; set; }

    /// <summary>Gets or sets the proceeds of a sale in minor units of the type's price currency.</summary>
    public long? ProceedsAmount { get; set; }

    /// <summary>Gets or sets an increase (<see langword="true"/>) or decrease of a correction.</summary>
    public bool IsIncrease { get; set; }

    /// <summary>Gets or sets the id shared with the linked money entry and fee.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Gets or sets the reason of a correction or an outflow.</summary>
    public string? Reason { get; set; }

    /// <summary>Gets or sets an optional note.</summary>
    public string? Note { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Returns the signed effect of the event on the quantity at <paramref name="locationId"/>.</summary>
    public long EffectAt(Guid locationId)
    {
        long effect = 0;
        if (LocationId == locationId)
        {
            effect += Kind switch
            {
                AssetEventKind.Opening or AssetEventKind.Purchase or AssetEventKind.GiftReceived => Quantity,
                AssetEventKind.Sale or AssetEventKind.Outflow or AssetEventKind.LocationTransfer => -Quantity,
                AssetEventKind.Correction => IsIncrease ? Quantity : -Quantity,
                _ => 0,
            };
        }

        if (Kind == AssetEventKind.LocationTransfer && ToLocationId == locationId)
        {
            effect += Quantity;
        }

        return effect;
    }

    /// <summary>Returns the signed effect on the total quantity of the type (a transfer changes nothing).</summary>
    public long TotalEffect => Kind switch
    {
        AssetEventKind.Opening or AssetEventKind.Purchase or AssetEventKind.GiftReceived => Quantity,
        AssetEventKind.Sale or AssetEventKind.Outflow => -Quantity,
        AssetEventKind.Correction => IsIncrease ? Quantity : -Quantity,
        _ => 0,
    };
}

/// <summary>Where a valuation came from.</summary>
public enum ValuationSource
{
    /// <summary>Entered by the user.</summary>
    Manual = 0,

    /// <summary>Taken from the price of a purchase, clearly marked.</summary>
    Purchase = 1,
}

/// <summary>
/// A dated price of an asset type (design §7.7): <see cref="PricePerUnitMilli"/> is the price of one gram (mass) or one
/// unit (count) in minor units × 1,000, so a price per gram keeps its precision. A price never changes a quantity
/// (ZEX-AS11, AT38), and a new price never rewrites earlier values (ZEX-AS13).
/// </summary>
public sealed class AssetValuation : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the asset type.</summary>
    public Guid AssetTypeId { get; set; }

    /// <summary>Gets or sets the date the price applies from.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the price of one gram or one unit in minor units × 1,000.</summary>
    public long PricePerUnitMilli { get; set; }

    /// <summary>Gets or sets the currency (the type's price currency).</summary>
    public required string CurrencyCode { get; set; }

    /// <summary>Gets or sets where the price came from.</summary>
    public ValuationSource Source { get; set; }

    /// <summary>Gets or sets an optional note.</summary>
    public string? Note { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
