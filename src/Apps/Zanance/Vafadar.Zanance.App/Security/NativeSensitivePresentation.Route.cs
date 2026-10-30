namespace Vafadar.Zanance.App.Security;

/// <summary>Exact original modal-route comparison; no unrelated route, query or traversal inherits sensitive authority.</summary>
internal static partial class NativeSensitivePresentation
{
    /// <summary>Matches only the original single modal route proposed by MAUI's PagePushing path.</summary>
    internal static bool IsExactTarget(string proposed, string expected)
    {
        if (string.IsNullOrWhiteSpace(proposed) || string.IsNullOrWhiteSpace(expected)
            || proposed.IndexOfAny(['/', '?', '#']) >= 0 || expected.IndexOfAny(['/', '?', '#']) >= 0) return false;
        var decoded = Uri.UnescapeDataString(proposed);
        return decoded.IndexOfAny(['/', '?', '#']) < 0 && string.Equals(decoded, expected, StringComparison.Ordinal);
    }
}
