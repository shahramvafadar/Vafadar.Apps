namespace Vafadar.Zanance.PhysicalReaderGestures;

/// <summary>Only copied public native tab metadata; never a control, command or private model.</summary>
internal sealed record ReaderTabNode(string? Caption, string? Class, bool Visible, bool Enabled, bool Clickable,
    bool Password, bool Selected, int[] Bounds);

/// <summary>D-593: a translated caption alone cannot distinguish a read-only tab from another action.</summary>
internal static class PhysicalReaderTabRow
{
    /// <summary>Admits only one complete original five-peer row and a primary target in its original native position.</summary>
    internal static bool Admits(IReadOnlyList<ReaderTabNode> nodes, IReadOnlyList<string> captions,
        string expected, int minimumPixels, int[] viewport)
    {
        if (captions.Count != 5 || captions.Distinct(StringComparer.Ordinal).Count() != 5
            || !captions.Take(4).Contains(expected, StringComparer.Ordinal) || minimumPixels <= 0
            || viewport.Length != 4 || viewport[2] <= viewport[0] || viewport[3] <= viewport[1]) return false;
        var row = new List<ReaderTabNode>();
        foreach (var caption in captions)
        {
            var matches = nodes.Where(node => node.Caption == caption && node.Class == "android.widget.FrameLayout"
                && node.Visible && node.Enabled && !node.Password).ToArray();
            if (matches.Length != 1) return false;
            var node = matches[0];
            // TalkBack can remove the redundant click action from the selected peer. Never allow that for a new target.
            if (!node.Selected && !node.Clickable) return false;
            if (node.Bounds.Length != 4 || node.Bounds[0] < viewport[0] || node.Bounds[1] < viewport[1]
                || node.Bounds[2] > viewport[2] || node.Bounds[3] > viewport[3]
                || (long)node.Bounds[2] - node.Bounds[0] < minimumPixels
                || (long)node.Bounds[3] - node.Bounds[1] < minimumPixels) return false;
            row.Add(node);
        }
        if (row.Count(node => node.Selected) != 1) return false;
        var target = row[captions.ToList().IndexOf(expected)];
        if (target.Selected || !target.Clickable) return false;
        var first = row[0].Bounds;
        // Android lays this owned five-tab shell in one contiguous band; derive it from actual peers, never an inset.
        for (var index = 0; index < row.Count; index++)
        {
            var current = row[index].Bounds;
            if (current[1] != first[1] || current[3] != first[3]
                || index > 0 && row[index - 1].Bounds[2] != current[0]) return false;
        }
        return true;
    }
}
