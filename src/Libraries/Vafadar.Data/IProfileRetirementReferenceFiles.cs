namespace Vafadar.Data;

/// <summary>Actual native private-root observations used to discover complete profile retirement references.</summary>
public interface IProfileRetirementReferenceFiles
{
    /// <summary>Lists bounded direct entries without following directories or applying a deletion pattern.</summary>
    Task<IReadOnlyList<string>> ListNamesAsync(string root, CancellationToken ct = default);
    /// <summary>Captures the verified native root/file identity, complete length and content fingerprint.</summary>
    Task<ProfileRetirementArtifact> CaptureAsync(string root, string name, CancellationToken ct = default);
    /// <summary>Reads a bounded complete file or prefix; the caller clears the returned owned buffer.</summary>
    Task<byte[]> ReadAsync(string root, string name, int maximumBytes, bool prefix, CancellationToken ct = default);
}
