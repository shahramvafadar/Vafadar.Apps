using System.Collections.Immutable;

namespace Vafadar.Zanance.Core.Money;

/// <summary>
/// An informal unit in which the user wants to see and enter amounts of a currency, e.g. the toman for the Iranian rial
/// (FX-07). One unit is 10^<see cref="Exponent"/> major units of the currency, so a conversion only moves the decimal
/// point and is exact. Amounts are always stored in the currency itself; exports and exchange rates keep the ISO unit.
/// </summary>
/// <param name="CurrencyCode">The ISO 4217 code of the currency.</param>
/// <param name="Name">The name shown instead of the currency code, chosen by the user.</param>
/// <param name="Exponent">The power of ten of the factor, 1 to 6 (1 = one unit is 10 major units).</param>
public sealed record DisplayUnit(string CurrencyCode, string Name, int Exponent)
{
    /// <summary>The largest supported exponent.</summary>
    public const int MaxExponent = 6;

    /// <summary>Gets the number of major units of the currency in one display unit.</summary>
    public long Factor => (long)Math.Pow(10, Exponent);

    /// <summary>Returns whether the unit can be used: a known currency, a name and an exponent from 1 to 6.</summary>
    public bool IsValid => Currencies.TryGet(CurrencyCode, out _) && !string.IsNullOrWhiteSpace(Name) && Name.Trim().Length <= 20 && Exponent is >= 1 and <= MaxExponent;
}

/// <summary>
/// The display units the user has defined (FX-07). Nothing is set by default and nothing is derived from the language
/// or region; the app sets them from its preferences at start and whenever the user changes them.
/// </summary>
public static class DisplayUnits
{
    private static ImmutableDictionary<string, DisplayUnit> _units = ImmutableDictionary<string, DisplayUnit>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the defined units.</summary>
    public static IReadOnlyCollection<DisplayUnit> All => [.. _units.Values.OrderBy(u => u.CurrencyCode, StringComparer.Ordinal)];

    /// <summary>Replaces all units; invalid ones are ignored.</summary>
    public static void Set(IEnumerable<DisplayUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        var valid = units.Where(u => u.IsValid).GroupBy(u => u.CurrencyCode.ToUpperInvariant()).Select(g => g.Last() with { CurrencyCode = g.Key, Name = g.Last().Name.Trim() });
        _units = ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, valid.Select(u => KeyValuePair.Create(u.CurrencyCode, u)));
    }

    /// <summary>Returns the unit of a currency, if the user defined one.</summary>
    public static bool TryGet(string? currencyCode, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out DisplayUnit? unit)
    {
        unit = null;
        return currencyCode is not null && _units.TryGetValue(currencyCode, out unit);
    }
}
