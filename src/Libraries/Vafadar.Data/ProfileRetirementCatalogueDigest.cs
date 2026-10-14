namespace Vafadar.Data;

/// <summary>Non-secret exact preference-state fingerprints; profile names and replacement packets never enter this journal.</summary>
public sealed record ProfileRetirementCatalogueDigest
{
    /// <summary>Captures distinct canonical SHA-256 states before and after the reviewed atomic catalogue mutation.</summary>
    public ProfileRetirementCatalogueDigest(string original, string removed)
    {
        if (!Valid(original) || !Valid(removed) || original == removed) throw new ArgumentException("Invalid retirement catalogue digests.");
        Original = original; Removed = removed;
    }
    /// <summary>Original complete device preference-state fingerprint.</summary>
    public string Original { get; }
    /// <summary>Exact post-removal state fingerprint, preserving other rows and current selection.</summary>
    public string Removed { get; }
    private static bool Valid(string value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigitLower);
}
