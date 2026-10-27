using System.Globalization;

namespace Vafadar.Zanance.PhysicalReaderGestures;

/// <summary>Finite original captions with distinct category and structurally guarded primary-tab scopes (D-593).</summary>
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
        // D-593: for example, Italian Cancel and Undo share a caption. Refuse the alias rather than guessing its command.
        foreach (var key in new[] { "Common_Save", "Common_Delete", "Common_Undo" }) result.Remove(Caption(read(key)));
        return result;
    }

    /// <summary>Names the four read-only primary destinations, never a translated financial action.</summary>
    internal static IReadOnlySet<string> PrimaryTabsFromOriginalResources(Func<string, string> read) =>
        ShellTabsFromOriginalResources(read).Take(4).ToHashSet(StringComparer.Ordinal);

    /// <summary>Retains the original five-tab shell order for native role/row admission, not arbitrary caption matching.</summary>
    internal static IReadOnlyList<string> ShellTabsFromOriginalResources(Func<string, string> read)
    {
        var tabs = new[] { "Tab_Home", "Tab_Transactions", "Tab_Plans", "Tab_Insights", "Tab_More" }
            .Select(key => Caption(read(key))).ToArray();
        if (tabs.Distinct(StringComparer.Ordinal).Count() != 5)
            throw new InvalidOperationException("Five distinct original shell tabs are required.");
        return tabs;
    }

    /// <summary>Rejects absent or unbounded captions; no user-provided name or arbitrary operation is admitted.</summary>
    private static string Caption(string value) => !string.IsNullOrWhiteSpace(value)
        && value.Length <= 256 && value.IndexOfAny(['\0', '\r', '\n']) < 0
        ? value : throw new InvalidOperationException("A bounded original caption is required.");
}
