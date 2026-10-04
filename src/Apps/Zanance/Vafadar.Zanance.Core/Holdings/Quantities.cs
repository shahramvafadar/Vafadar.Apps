using System.Globalization;

namespace Vafadar.Zanance.Core.Holdings;

/// <summary>A unit a quantity is typed or shown in.</summary>
public enum QuantityUnit
{
    /// <summary>Grams (mass).</summary>
    Gram = 0,

    /// <summary>Kilograms (mass).</summary>
    Kilogram = 1,

    /// <summary>Units of a count type, e.g. coins.</summary>
    Piece = 2,
}

/// <summary>
/// Quantities in the base units of design §7.2 (ZEX-P10): mass in milligrams, count in thousandths of a unit, both
/// <see cref="long"/> with checked arithmetic. 1 kg = 1,000 g = 1,000,000 mg exactly; input and display use up to three
/// decimals of a gram or a unit; an indivisible count type takes whole units only.
/// </summary>
public static class Quantities
{
    /// <summary>Base units per gram (mg) and per unit (thousandths).</summary>
    public const long PerGramOrUnit = 1_000;

    /// <summary>Returns the base units of one <paramref name="unit"/>.</summary>
    public static long Factor(QuantityUnit unit) => unit switch
    {
        QuantityUnit.Kilogram => 1_000_000,
        _ => PerGramOrUnit,
    };

    /// <summary>Returns the units that fit the dimension of a type.</summary>
    public static IReadOnlyList<QuantityUnit> UnitsOf(AssetDimension dimension) =>
        dimension == AssetDimension.Mass ? [QuantityUnit.Gram, QuantityUnit.Kilogram] : [QuantityUnit.Piece];

    /// <summary>
    /// Parses a typed quantity into base units. Any digit script; '.' and ',' (and the Persian separators) are read like
    /// amounts: with both, the last one is the decimal separator; one kind used several times groups thousands; a single
    /// one is the decimal separator unless it is the culture's group separator followed by exactly three digits – so in
    /// German "1.5" is 1.5 g and "1.500" is 1,500 g, in English "1,500" is 1,500 g. Refuses more than three decimals of a
    /// gram or unit, fractions of an indivisible count type, zero, negatives and badly grouped numbers.
    /// </summary>
    public static bool TryParse(string? text, QuantityUnit unit, AssetType type, CultureInfo culture, out long quantity)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(culture);
        quantity = 0;
        if (Normalize(text, culture) is not { } cleaned
            || !decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            return false;
        }

        var exact = value * Factor(unit);
        if (exact != decimal.Truncate(exact) || exact > long.MaxValue)
        {
            return false;
        }

        quantity = (long)exact;
        return type.Dimension == AssetDimension.Mass || type.Divisible || quantity % PerGramOrUnit == 0;
    }

    // The typed number with '.' as its only separator, or null when its separators are ambiguous or misplaced.
    private static string? Normalize(string? text, CultureInfo culture)
    {
        var value = Vafadar.Core.Text.Digits.ToAscii((text ?? string.Empty).Trim())
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace("\u202F", string.Empty, StringComparison.Ordinal);
        if (value.Length == 0 || value.Any(c => !char.IsAsciiDigit(c) && c is not '.' and not ','))
        {
            return null;
        }

        var dots = value.Count(c => c == '.');
        var commas = value.Count(c => c == ',');
        char? decimalSeparator;
        if (dots > 0 && commas > 0)
        {
            decimalSeparator = value.LastIndexOf('.') > value.LastIndexOf(',') ? '.' : ',';
            if (value.Count(c => c == decimalSeparator) > 1)
            {
                return null;
            }
        }
        else if (dots + commas == 0)
        {
            decimalSeparator = null;
        }
        else
        {
            var separator = dots > 0 ? '.' : ',';
            var digitsAfter = value.Length - value.LastIndexOf(separator) - 1;
            var cultureGroup = Vafadar.Core.Text.Digits.ToAscii(culture.NumberFormat.NumberGroupSeparator) is { Length: 1 } g ? g[0] : '\0';
            var isGroup = dots + commas > 1 || (separator == cultureGroup && digitsAfter == 3);
            decimalSeparator = isGroup ? null : separator;
        }

        var groupSeparator = decimalSeparator switch { '.' => ',', ',' => '.', _ => dots > 0 ? '.' : ',' };
        var decimalIndex = decimalSeparator is { } d ? value.IndexOf(d) : -1;
        var integer = decimalIndex < 0 ? value : value[..decimalIndex];
        var fraction = decimalIndex < 0 ? string.Empty : value[(decimalIndex + 1)..];
        if (integer.Contains(groupSeparator))
        {
            var groups = integer.Split(groupSeparator);
            if (groups[0].Length is 0 or > 3 || groups.Skip(1).Any(group => group.Length != 3))
            {
                return null;
            }

            integer = string.Concat(groups);
        }

        if (fraction.Contains(groupSeparator) || (integer.Length == 0 && fraction.Length == 0))
        {
            return null;
        }

        return (integer.Length == 0 ? "0" : integer) + (fraction.Length > 0 ? "." + fraction : string.Empty);
    }

    /// <summary>Formats a quantity, e.g. "50.000 g", "1.25 kg" or "3 coins" (the unit name comes from the caller).</summary>
    public static string Format(long quantity, AssetType type, CultureInfo culture, string gramSymbol = "g", string? countName = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(culture);
        if (type.Dimension == AssetDimension.Mass)
        {
            return ((decimal)quantity / PerGramOrUnit).ToString("#,0.000", culture) + " " + gramSymbol;
        }

        var units = (decimal)quantity / PerGramOrUnit;
        var number = type.Divisible ? units.ToString("#,0.###", culture) : units.ToString("#,0", culture);
        return countName is null ? number : number + " " + countName;
    }

    /// <summary>Returns the number of a quantity in <paramref name="unit"/> for an input field, without group separators.</summary>
    public static string ForInput(long quantity, QuantityUnit unit, CultureInfo culture) =>
        ((decimal)quantity / Factor(unit)).ToString("0.######", culture);

    /// <summary>
    /// Returns the weight in mg a holding stands for: the quantity itself for mass types, count × unit weight for count
    /// types with a unit weight, otherwise <see langword="null"/>.
    /// </summary>
    public static long? MassMg(long quantity, AssetType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.Dimension == AssetDimension.Mass)
        {
            return quantity;
        }

        return type.UnitWeightMg is { } unitWeight ? checked(quantity * unitWeight / PerGramOrUnit) : null;
    }

    /// <summary>Returns the fine-metal mass in mg (mass × purity), or <see langword="null"/> without purity or weight.</summary>
    public static long? FineMassMg(long quantity, AssetType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.PurityPer10000 is { } purity && MassMg(quantity, type) is { } mass ? checked(mass * purity / 10_000) : null;
    }
}

/// <summary>
/// Purity input (ZEX-AS03): karat (1–24) or fineness (‰, e.g. 750 or 999.9), stored in parts per 10,000. 24 k counts as
/// 999.9 (9999), as dealers sell it; every other karat is k / 24 rounded to the nearest part.
/// </summary>
public static class Purity
{
    /// <summary>Common purities: 24, 22, 21, 18 and 14 karat.</summary>
    public static IReadOnlyList<(string Label, int Per10000)> Common { get; } =
    [
        ("24 k", 9999), ("22 k", 9167), ("21 k", 8750), ("18 k", 7500), ("14 k", 5833),
    ];

    /// <summary>Returns the parts per 10,000 of a karat value (1–24).</summary>
    public static int FromKarat(decimal karat) =>
        karat >= 24 ? 9999 : (int)Math.Round(karat / 24m * 10_000m, MidpointRounding.AwayFromZero);

    /// <summary>Returns the parts per 10,000 of a fineness in per mille (e.g. 750 → 7500, 999.9 → 9999).</summary>
    public static int FromFineness(decimal perMille) => (int)Math.Round(perMille * 10m, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Parses "18", "18k", "18 k" (karat) or "750", "999.9" (fineness ‰). Values up to 24 are karat, from 100 to 1000
    /// fineness; anything else is refused.
    /// </summary>
    public static bool TryParse(string? text, CultureInfo culture, out int per10000)
    {
        ArgumentNullException.ThrowIfNull(culture);
        per10000 = 0;
        var cleaned = Vafadar.Core.Text.Digits.ToAscii((text ?? string.Empty).Trim().TrimEnd('k', 'K', '‰').Trim())
            .Replace(culture.NumberFormat.NumberDecimalSeparator, ".", StringComparison.Ordinal)
            .Replace('٫', '.');
        if (!decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            return false;
        }

        if (value <= 24)
        {
            per10000 = FromKarat(value);
            return true;
        }

        if (value is >= 100 and <= 1000)
        {
            per10000 = FromFineness(value);
            return true;
        }

        return false;
    }

    /// <summary>Formats a purity as fineness, e.g. "750/1000" or "999.9/1000".</summary>
    public static string Format(int per10000, CultureInfo culture) => (per10000 / 10m).ToString("0.#", culture) + "/1000";
}
