using System.Globalization;

namespace Vafadar.Zanance.PhysicalReaderGestures;

/// <summary>D-579: finite original nonfinancial captions, including the changed category selection state.</summary>
internal static class PhysicalReaderCaptions
{
    /// <summary>Builds one language's reviewed whitelist from the original bundled application/shared resources.</summary>
    internal static IReadOnlySet<string> FromOriginalResources(Func<string, string> read)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in new[] { "Entry_NewExpense", "Common_Cancel", "Tab_More", "About_Title", "Common_Back", "Tab_Home" })
            result.Add(Caption(read(key)));

        var format = Caption(read("Common_ChipSelected"));
        var first = format.IndexOf("{0}", StringComparison.Ordinal);
        if (first < 0 || format.IndexOf("{0}", first + 3, StringComparison.Ordinal) >= 0
            || format.Replace("{0}", "", StringComparison.Ordinal).IndexOfAny(['{', '}']) >= 0)
            throw new InvalidOperationException("One original category selection placeholder is required.");

        foreach (var key in new[] { "Category_Food", "Category_Housing" })
        {
            var name = Caption(read(key));
            var selected = Caption(string.Format(CultureInfo.InvariantCulture, format, name));
            if (selected == name) throw new InvalidOperationException("Original selection state must remain distinct.");
            result.Add(name);
            result.Add(selected);
        }
        return result;
    }

    /// <summary>Rejects absent or unbounded captions; no user-provided name or arbitrary operation is admitted.</summary>
    private static string Caption(string value) => !string.IsNullOrWhiteSpace(value)
        && value.Length <= 256 && value.IndexOfAny(['\0', '\r', '\n']) < 0
        ? value : throw new InvalidOperationException("A bounded original caption is required.");
}
