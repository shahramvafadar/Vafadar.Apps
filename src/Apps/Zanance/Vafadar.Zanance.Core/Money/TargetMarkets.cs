namespace Vafadar.Zanance.Core.Money;

/// <summary>Reviewed target-country currency references; never changes a chosen account currency or stored amount.</summary>
public static class TargetMarkets
{
    private static readonly IReadOnlyDictionary<string, string> CurrencyCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["AT"]="EUR", ["BE"]="EUR", ["FR"]="EUR", ["DE"]="EUR", ["LI"]="CHF", ["LU"]="EUR", ["MC"]="EUR",
        ["NL"]="EUR", ["CH"]="CHF", ["GB"]="GBP", ["IE"]="EUR", ["US"]="USD", ["CA"]="CAD", ["AU"]="AUD",
    };
    /// <summary>Country set is extensible without changing language, local identity or financial schema.</summary>
    public static IReadOnlyList<string> Countries { get; } = [.. CurrencyCodes.Keys.Order(StringComparer.Ordinal)];
    /// <summary>Official ISO 4217 maintenance source, reviewed for these six existing two-minor-digit currencies.</summary>
    public const string CurrencySource = "https://www.six-group.com/en/products-services/financial-information/market-reference-data/data-standards.html";
    /// <summary>Gets the country's currency reference, not an automatic formatting/account selection.</summary>
    public static Currency? CurrencyFor(string? country) => country is not null && CurrencyCodes.TryGetValue(country, out var code) ? Currencies.Get(code) : null;
}
