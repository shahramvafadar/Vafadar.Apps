using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Features.Home;

/// <summary>Selects the original recent rows from Home's complete, already newest-first ledger snapshot.</summary>
internal static class HomeRecentEntries
{
    /// <summary>Preserves source order and references, excluding future dates and stopping after three eligible rows.</summary>
    /// <remarks>The caller supplies the stable Date/CreatedAt descending order guaranteed by HomeReadSnapshot.</remarks>
    internal static IReadOnlyList<LedgerEntry> FromNewestFirst(IReadOnlyList<LedgerEntry> entries, DateOnly today)
    {
        var recent = new List<LedgerEntry>(3);
        // D-383: do not sort or traverse the older suffix again on the window thread.
        for (var index = 0; index < entries.Count && recent.Count < 3; index++)
        {
            var entry = entries[index];
            if (entry.Date <= today) recent.Add(entry);
        }
        return recent;
    }
}
