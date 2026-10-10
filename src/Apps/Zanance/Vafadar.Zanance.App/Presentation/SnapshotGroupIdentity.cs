namespace Vafadar.Zanance.App.Presentation;

/// <summary>Compares complete grouped presentation snapshots without replacing their native bound source.</summary>
internal static class SnapshotGroupIdentity
{
    /// <summary>
    /// Returns true only for identical captions, group boundaries and ordered row objects. Fresh data/display
    /// snapshots have new row identities, even when their financial ids remain the same.
    /// </summary>
    public static bool Matches<TGroup, TRow>(IReadOnlyList<TGroup> current, IReadOnlyList<TGroup> next,
        Func<TGroup, string> header, Func<TGroup, string> netText, Func<TGroup, IReadOnlyList<TRow>> rows)
        where TRow : class
    {
        if (current.Count != next.Count) { return false; }
        for (var groupIndex = 0; groupIndex < current.Count; groupIndex++)
        {
            var oldGroup = current[groupIndex];
            var newGroup = next[groupIndex];
            if (header(oldGroup) != header(newGroup) || netText(oldGroup) != netText(newGroup)) { return false; }
            var oldRows = rows(oldGroup);
            var newRows = rows(newGroup);
            if (oldRows.Count != newRows.Count) { return false; }
            for (var rowIndex = 0; rowIndex < oldRows.Count; rowIndex++)
            {
                if (!ReferenceEquals(oldRows[rowIndex], newRows[rowIndex])) { return false; }
            }
        }

        return true;
    }
}
