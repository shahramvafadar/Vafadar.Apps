namespace Vafadar.Finance.Core.Money;

/// <summary>
/// An ISO 4217 currency with its number of minor digits (EUR 2, JPY 0, KWD 3).
/// </summary>
public sealed record Currency(string Code, int MinorDigits)
{
    /// <summary>Gets the factor between major and minor units (100 for EUR).</summary>
    public long MinorFactor => MinorDigits switch { 0 => 1, 1 => 10, 2 => 100, 3 => 1000, 4 => 10000, _ => throw new InvalidOperationException() };

    /// <inheritdoc />
    public override string ToString() => Code;
}

/// <summary>
/// The currencies the app offers, with minor digits from ISO 4217.
/// </summary>
public static class Currencies
{
    private static readonly Dictionary<string, Currency> ByCode = new[]
    {
        ("EUR", 2), ("USD", 2), ("GBP", 2), ("CHF", 2), ("IRR", 2), ("TRY", 2), ("AED", 2), ("SAR", 2), ("QAR", 2),
        ("OMR", 3), ("KWD", 3), ("BHD", 3), ("JOD", 3), ("IQD", 3), ("AFN", 2), ("PKR", 2), ("INR", 2), ("CNY", 2),
        ("JPY", 0), ("KRW", 0), ("CAD", 2), ("AUD", 2), ("NZD", 2), ("SEK", 2), ("NOK", 2), ("DKK", 2), ("PLN", 2),
        ("CZK", 2), ("HUF", 2), ("RON", 2), ("BGN", 2), ("RUB", 2), ("UAH", 2), ("AMD", 2), ("AZN", 2), ("GEL", 2),
        ("EGP", 2), ("MAD", 2), ("ZAR", 2), ("BRL", 2), ("MXN", 2), ("ARS", 2), ("SGD", 2), ("HKD", 2), ("THB", 2),
        ("MYR", 2), ("IDR", 2), ("VND", 0), ("ISK", 0), ("CLP", 0), ("TND", 3), ("LYD", 3),
    }.ToDictionary(c => c.Item1, c => new Currency(c.Item1, c.Item2), StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets all known currencies ordered by code.</summary>
    public static IReadOnlyList<Currency> All { get; } = [.. ByCode.Values.OrderBy(c => c.Code, StringComparer.Ordinal)];

    /// <summary>Gets the euro.</summary>
    public static Currency Euro => ByCode["EUR"];

    /// <summary>Returns the currency for an ISO code.</summary>
    /// <exception cref="ArgumentException">The code is unknown.</exception>
    public static Currency Get(string code) =>
        TryGet(code, out var currency) ? currency : throw new ArgumentException($"Unknown currency '{code}'.", nameof(code));

    /// <summary>Tries to find the currency for an ISO code.</summary>
    public static bool TryGet(string? code, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Currency? currency)
    {
        currency = null;
        return code is not null && ByCode.TryGetValue(code, out currency);
    }
}
