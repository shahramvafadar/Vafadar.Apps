using System.Globalization;

namespace Vafadar.Localization;

/// <summary>
/// Countries and regions the user can choose (PR-05). A region only suggests formats such as the first day of the
/// week; it never selects the language, currency or calendar and never involves location access (LOC-05).
/// </summary>
public static class Regions
{
    // First day of the week per territory (CLDR "firstDay"); all other territories start on Monday.
    private static readonly HashSet<string> SaturdayFirst = new(StringComparer.OrdinalIgnoreCase)
    {
        "AE", "AF", "BH", "DJ", "DZ", "EG", "IQ", "IR", "JO", "KW", "LY", "OM", "QA", "SD", "SY",
    };

    private static readonly HashSet<string> SundayFirst = new(StringComparer.OrdinalIgnoreCase)
    {
        "AG", "AS", "BD", "BR", "BS", "BT", "BW", "BZ", "CA", "CN", "CO", "DM", "DO", "ET", "GT", "GU", "HK", "HN",
        "ID", "IL", "IN", "JM", "JP", "KE", "KH", "KR", "LA", "MH", "MM", "MO", "MT", "MX", "MZ", "NI", "NP", "PA",
        "PE", "PH", "PK", "PR", "PT", "PY", "SA", "SG", "SV", "TH", "TT", "TW", "UM", "US", "VE", "VI", "WS", "YE",
        "ZA", "ZW",
    };

    private static readonly Lazy<IReadOnlyList<string>> AllCodes = new(() =>
    [
        .. CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(c => TryRegion(c.Name)?.TwoLetterISORegionName)
            .Where(code => code is { Length: 2 } && char.IsLetter(code[0]) && char.IsLetter(code[1]))
            .Select(code => code!.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal),
    ]);

    /// <summary>Gets the ISO 3166-1 alpha-2 codes known to the platform.</summary>
    public static IReadOnlyList<string> All => AllCodes.Value;

    /// <summary>Returns whether <paramref name="code"/> is a known region code.</summary>
    public static bool IsKnown(string? code) =>
        code is { Length: 2 } && All.Contains(code.ToUpperInvariant(), StringComparer.Ordinal);

    /// <summary>Returns the region's name in the current UI language, or the code when the platform has no name.</summary>
    public static string DisplayName(string code) => TryRegion(code)?.DisplayName ?? code;

    /// <summary>
    /// Returns the conventional first day of the week: the region's when one is chosen, otherwise the language's.
    /// </summary>
    public static DayOfWeek FirstDayOfWeek(string? region, CultureInfo languageCulture)
    {
        ArgumentNullException.ThrowIfNull(languageCulture);
        if (string.IsNullOrEmpty(region))
        {
            return languageCulture.DateTimeFormat.FirstDayOfWeek;
        }

        return SaturdayFirst.Contains(region) ? DayOfWeek.Saturday
            : SundayFirst.Contains(region) ? DayOfWeek.Sunday
            : DayOfWeek.Monday;
    }

    private static RegionInfo? TryRegion(string name)
    {
        try
        {
            return new RegionInfo(name);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}