using System.Security.Cryptography;

namespace Vafadar.Backup.Security;

/// <summary>Standard AES-256-GCM for device-local safety packages with an authenticated opaque key reference.</summary>
/// <remarks>VBS1 | identity (48 bytes) | random nonce (12) | tag (16) | ciphertext. The header before
/// the tag is AAD. This is not a password format; platform key storage and retention belong to the host.</remarks>
public static class DeviceSafetyEncryption
{
    /// <summary>Maximum complete safety package accepted by this in-memory codec.</summary>
    public const int MaximumPackageBytes = 256 * 1024 * 1024;
    private const int HeaderSize = 64;
    private const int TagSize = 16;

    /// <summary>Recognizes the internal device-only format; never interprets it as an unprotected portable ZIP.</summary>
    public static bool IsEncrypted(ReadOnlySpan<byte> data) => data.StartsWith("VBS1"u8);

    /// <summary>Encrypts with a caller-owned 256-bit key and 48-byte non-secret platform binding.</summary>
    public static byte[] Protect(ReadOnlySpan<byte> package, ReadOnlySpan<byte> key, ReadOnlySpan<byte> identity)
    {
        if (package.Length is 0 or > MaximumPackageBytes || key.Length != 32 || identity.Length != 48)
            throw new ArgumentException("Safety package, key or identity length is invalid.");
        var result = new byte[checked(HeaderSize + TagSize + package.Length)];
        "VBS1"u8.CopyTo(result); identity.CopyTo(result.AsSpan(4, 48));
        RandomNumberGenerator.Fill(result.AsSpan(52, 12));
        using var cipher = new AesGcm(key, TagSize);
        cipher.Encrypt(result.AsSpan(52, 12), package, result.AsSpan(HeaderSize + TagSize), result.AsSpan(HeaderSize, TagSize), result.AsSpan(0, HeaderSize));
        return result;
    }

    /// <summary>Returns the authenticated key-reference candidate; the caller must authenticate the full envelope before using content.</summary>
    public static byte[] ReadIdentity(ReadOnlySpan<byte> envelope)
    {
        if (!IsEncrypted(envelope) || envelope.Length <= HeaderSize + TagSize || envelope.Length > MaximumPackageBytes + HeaderSize + TagSize)
            throw new BackupException(BackupError.InvalidFormat, "Invalid device safety envelope.");
        return envelope.Slice(4, 48).ToArray();
    }

    /// <summary>Authenticates all header/content bytes and releases plaintext only after successful AES-GCM verification.</summary>
    public static byte[] Open(ReadOnlySpan<byte> envelope, ReadOnlySpan<byte> key)
    {
        _ = ReadIdentity(envelope);
        if (key.Length != 32) throw new CryptographicException("Invalid safety key length.");
        var result = new byte[envelope.Length - HeaderSize - TagSize];
        try
        {
            using var cipher = new AesGcm(key, TagSize);
            cipher.Decrypt(envelope.Slice(52, 12), envelope[(HeaderSize + TagSize)..], envelope.Slice(HeaderSize, TagSize), result, envelope[..HeaderSize]);
            return result;
        }
        catch { CryptographicOperations.ZeroMemory(result); throw; }
    }
}
