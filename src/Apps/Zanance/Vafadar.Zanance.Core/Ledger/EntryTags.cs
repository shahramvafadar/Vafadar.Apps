namespace Vafadar.Zanance.Core.Ledger;

/// <summary>Rules for entry tags (F2-TX-04): trimmed, without a leading #, unique ignoring case, at most 10 of 30 characters.</summary>
public static class EntryTags
{
    /// <summary>Most tags per entry.</summary>
    public const int MaxTags = 10;

    /// <summary>Longest tag.</summary>
    public const int MaxLength = 30;

    private static readonly char[] Separators = [',', '،', ';', '\n'];

    /// <summary>Parses user input such as "#car, fuel ;trip" into normalized tags.</summary>
    public static List<string> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return Normalize(text.Split(Separators, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Normalizes a list of tags.</summary>
    public static List<string> Normalize(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        return
        [
            .. tags
                .Select(t => t.Trim().TrimStart('#').Trim())
                .Where(t => t.Length > 0)
                .Select(t => t.Length > MaxLength ? t[..MaxLength].TrimEnd() : t)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Take(MaxTags),
        ];
    }

    /// <summary>
    /// Formats a tag for display as "#tag" in a directional isolate chosen from its first letter (LRI for Latin, RLI for
    /// Persian or Arabic), so the # stays in front in both layouts. Explicit isolates and marks are used because not
    /// every platform renders isolates.
    /// </summary>
    public static string Display(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        var first = tag.FirstOrDefault(char.IsLetter);
        var rtl = first is >= '\u0590' and <= '\u08FF' or >= '\uFB1D' and <= '\uFDFF' or >= '\uFE70' and <= '\uFEFF';
        // Marks inside the isolate keep the order on renderers that ignore isolates (Windows), as in MoneyText.
        return rtl ? $"\u2067\u200F#{tag}\u200F\u2069" : $"\u2066\u200E#{tag}\u200E\u2069";
    }

    /// <summary>Formats tags for an input field.</summary>
    public static string Format(IEnumerable<string> tags) => string.Join(", ", tags);

    /// <summary>Returns all tags in use, most used first, for suggestions and filters.</summary>
    public static IReadOnlyList<string> InUse(IEnumerable<LedgerEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return
        [
            .. entries
                .SelectMany(e => e.Tags)
                .GroupBy(t => t, StringComparer.CurrentCultureIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
                .Select(g => g.First()),
        ];
    }
}