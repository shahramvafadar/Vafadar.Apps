namespace Vafadar.Backup;

/// <summary>Names device-only recovery points separately from portable exports and pre-restore safety copies.</summary>
public static class AutomaticBackupFileName
{
    /// <summary>Returns the exact profile-local prefix without accepting path or portable-set separators.</summary>
    public static string Prefix(string? set)
    {
        if (set?.IndexOfAny(['_', '~', '/', '\\']) >= 0) throw new ArgumentException("Unsafe automatic backup set.", nameof(set));
        return string.IsNullOrEmpty(set) ? "automatic-main-" : "automatic-set-" + set + "-";
    }

    /// <summary>Accepts only complete automatic filenames for this set, never safety or interrupted temporary files.</summary>
    public static bool Matches(string name, string? set)
    {
        var prefix = Prefix(set);
        return name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".vbs", StringComparison.Ordinal)
            && name.Length == prefix.Length + 36 && Guid.TryParseExact(name.AsSpan(prefix.Length, 32), "N", out var id) && id != Guid.Empty;
    }
}
