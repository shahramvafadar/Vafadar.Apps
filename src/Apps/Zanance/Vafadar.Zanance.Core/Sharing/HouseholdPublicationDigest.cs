using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Canonical public ciphertext manifest framing shared by the writer, service and verifying recipients.</summary>
public static class HouseholdPublicationDigest
{
    /// <summary>Maximum immutable parts in one atomic publication; paging never weakens this complete-manifest bound.</summary>
    public const int MaximumParts = 32768;
    /// <summary>Maximum encoded ciphertext characters per part, independently of general HTTP limits.</summary>
    public const int MaximumPartCharacters = 1024 * 1024;
    /// <summary>Finite ciphertext/JOSE allowance above original clear profile/identity framing; the original256MiB financial graph bound remains independently enforced.</summary>
    public const long MaximumBytes = 8L * 1024 * 1024 * 1024;
    /// <summary>Complete public manifest storage bound; network reads use separately bounded manifest pages.</summary>
    public const int MaximumReceiptBytes = 8 * 1024 * 1024;

    /// <summary>Hashes exact encoded ciphertext, with no decoding, normalization or financial interpretation.</summary>
    public static string Ciphertext(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext) || ciphertext.Length > MaximumPartCharacters ||
            ciphertext.Any(character => character is < '!' or > '~'))
            throw new ArgumentException("Invalid bounded household ciphertext.", nameof(ciphertext));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(ciphertext)));
    }

    /// <summary>Binds ordered positions, opaque identities, original versions and complete ciphertext hashes without ambiguous concatenation.</summary>
    public static string Manifest(IReadOnlyList<HouseholdPartCommitment> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Count is < 1 or > MaximumParts) throw new ArgumentException("Invalid household manifest bound.", nameof(parts));
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData("Zanance household ciphertext manifest v1\0"u8);
        Span<byte> number = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(number, parts.Count); hash.AppendData(number);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < parts.Count; index++)
        {
            var part = parts[index];
            if (part is null || part.Index != index || part.BaseVersion < 0 ||
                !HouseholdProtectionContext.IsFingerprint(part.RecordId) ||
                !HouseholdProtectionContext.IsFingerprint(part.Digest) || !seen.Add(part.RecordId))
                throw new ArgumentException("Invalid household manifest shape.", nameof(parts));
            BinaryPrimitives.WriteInt64BigEndian(number, index); hash.AppendData(number);
            hash.AppendData(Convert.FromHexString(part.RecordId));
            BinaryPrimitives.WriteInt64BigEndian(number, part.BaseVersion); hash.AppendData(number);
            hash.AppendData(Convert.FromHexString(part.Digest));
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    /// <summary>Canonical complete encrypted grant inventory bound by independent new-owner acceptance, regardless of wire ordering.</summary>
    public static string Grants(IReadOnlyList<HouseholdEncryptedGrant> grants)
    {
        ArgumentNullException.ThrowIfNull(grants);
        if (grants.Count is < 1 or > 384 || grants.Any(grant => grant is null || grant.Epoch < 1 || grant.DeviceId == Guid.Empty ||
            !HouseholdProtectionContext.IsFingerprint(grant.Digest)) || grants.Select(grant => (grant.Epoch, grant.DeviceId)).Distinct().Count() != grants.Count)
            throw new ArgumentException("Invalid household grant manifest.", nameof(grants));
        var text = new StringBuilder("Zanance household grants v1\n");
        foreach (var grant in grants.OrderBy(grant => grant.Epoch).ThenBy(grant => grant.DeviceId))
            text.Append(grant.Epoch.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(':')
                .Append(grant.DeviceId.ToString("N")).Append(':').Append(grant.Digest).Append('\n');
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(text.ToString())));
    }
}
