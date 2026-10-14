namespace Vafadar.Data;

/// <summary>Non-secret immutable fingerprint of authenticated recovery mappings for one reviewed retirement.</summary>
public sealed record ProfileRecoveryRetirementCapture
{
    /// <summary>Captures the existing public recovery identity, bounded target count and canonical SHA-256 digest.</summary>
    public ProfileRecoveryRetirementCapture(Guid recoveryId, int count, string digest)
    {
        if (recoveryId == Guid.Empty || count is < 0 or > 4096 || digest is not { Length: 64 } || !digest.All(char.IsAsciiHexDigitLower))
            throw new ArgumentException("Invalid recovery retirement fingerprint.");
        RecoveryId = recoveryId; Count = count; Digest = digest;
    }
    /// <summary>The exact already enabled recovery generation.</summary>
    public Guid RecoveryId { get; }
    /// <summary>The number of authenticated captured mappings; absence of metadata never means zero.</summary>
    public int Count { get; }
    /// <summary>The complete ordered target ciphertext/confirmation fingerprint, containing no plaintext data key.</summary>
    public string Digest { get; }
}
