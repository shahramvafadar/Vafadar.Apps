namespace Vafadar.Data;

/// <summary>Exact non-secret native file identity and content fingerprint captured before catalogue removal.</summary>
public sealed record ProfileRetirementArtifact
{
    /// <summary>Creates one direct-file reference in a host-owned named private root; paths and wildcards are refused.</summary>
    public ProfileRetirementArtifact(string root, string name, string nativeIdentity, long length, string sha256, string rootIdentity = "0")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root); ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeIdentity); ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootIdentity);
        if (root.Length > 32 || root.Any(c => !char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-'))
            throw new ArgumentException("Invalid private root identifier.");
        if (name.Length > 255 || name is "." or ".." || name.Any(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c)))
            throw new ArgumentException("Retirement requires a direct regular filename.");
        if (nativeIdentity.Length > 64 || nativeIdentity.Any(c => !char.IsAsciiHexDigitLower(c) && !char.IsAsciiDigit(c)))
            throw new ArgumentException("Invalid native file identity.");
        if (rootIdentity.Length > 64 || rootIdentity.Any(c => !char.IsAsciiHexDigitLower(c) && !char.IsAsciiDigit(c)))
            throw new ArgumentException("Invalid native root identity.");
        if (length < 0 || sha256.Length != 64 || sha256.Any(c => !char.IsAsciiHexDigitLower(c) && !char.IsAsciiDigit(c)))
            throw new ArgumentException("Invalid captured file fingerprint.");
        Root = root; Name = name; NativeIdentity = nativeIdentity; Length = length; Sha256 = sha256; RootIdentity = rootIdentity;
    }
    /// <summary>Known private root identifier, resolved only by the native host.</summary>
    public string Root { get; }
    /// <summary>Exact direct filename; never an absolute or relative path.</summary>
    public string Name { get; }
    /// <summary>Native volume/file identifier, preventing same-content replacement deletion.</summary>
    public string NativeIdentity { get; }
    /// <summary>Native directory identity; absence must not be accepted in a replacement root.</summary>
    public string RootIdentity { get; }
    /// <summary>Captured complete file length.</summary>
    public long Length { get; }
    /// <summary>Lowercase SHA-256 of captured bytes, not a key or plaintext payload.</summary>
    public string Sha256 { get; }
}
