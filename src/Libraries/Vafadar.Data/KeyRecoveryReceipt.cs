using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Vafadar.Data;

/// <summary>Authenticates exact retained original and recovered native file objects with the finance-verified DEK.</summary>
/// <remarks>The receipt is non-secret metadata, not a new cipher. Standard HMAC-SHA256 binds existing profile/
/// installation/generation ids and immutable native identities/lengths/SHA-256. Missing state is never inferred.</remarks>
public static class KeyRecoveryReceipt
{
    /// <summary>The supported bounded ASCII receipt size.</summary>
    public const int MaximumBytes = 2048;

    /// <summary>Creates a confirmation only after the host verified the owned DEK against existing finance.</summary>
    public static byte[] Create(DatabaseKeyBinding binding, ProfileRetirementArtifact? original,
        ProfileRetirementArtifact recovered, ReadOnlySpan<byte> key)
    {
        ArgumentNullException.ThrowIfNull(binding); ArgumentNullException.ThrowIfNull(recovered);
        RequireArtifacts(binding, original, recovered);
        if (key.Length != 32) throw new CryptographicException("Recovery receipt requires the exact data key.");
        var fields = new[] { "1", binding.ProfileId.ToString("N"), binding.InstallationId.ToString("N"), binding.GenerationId.ToString("N"),
            original is null ? "0" : "1", original?.NativeIdentity ?? "-", original?.RootIdentity ?? "-",
            original?.Length.ToString(CultureInfo.InvariantCulture) ?? "-", original?.Sha256 ?? "-",
            recovered.NativeIdentity, recovered.RootIdentity, recovered.Length.ToString(CultureInfo.InvariantCulture), recovered.Sha256 };
        var body = Encoding.ASCII.GetBytes(string.Join('|', fields));
        var tag = HMACSHA256.HashData(key, body);
        var result = Encoding.ASCII.GetBytes(Encoding.ASCII.GetString(body) + "|" + Convert.ToHexStringLower(tag));
        if (result.Length > MaximumBytes) throw new IOException("Recovery receipt exceeds its bound.");
        return result;
    }

    /// <summary>Requires exact authenticated current native objects; same bytes in replacement files/directories fail.</summary>
    public static void Verify(ReadOnlySpan<byte> receipt, DatabaseKeyBinding binding, ProfileRetirementArtifact? original,
        ProfileRetirementArtifact recovered, ReadOnlySpan<byte> key)
    {
        if (receipt.Length is <= 0 or > MaximumBytes) throw new IOException("Recovery receipt is missing or exceeds its bound.");
        var expected = Create(binding, original, recovered, key);
        if (!CryptographicOperations.FixedTimeEquals(receipt, expected)) throw new CryptographicException("Recovered file authority differs.");
    }
    private static void RequireArtifacts(DatabaseKeyBinding binding, ProfileRetirementArtifact? original, ProfileRetirementArtifact recovered)
    {
        var name = $"{binding.ProfileId:N}-{binding.GenerationId:N}.key";
        if (recovered.Root != "keys" || recovered.Name != name + ".recovered" || recovered.Length is <= 0 or > 4096
            || original is not null && (original.Root != "keys" || original.Name != name || original.Length > 4096
                || original.RootIdentity != recovered.RootIdentity))
            throw new IOException("Recovery receipt requires exact bound native key artifacts.");
    }
}
