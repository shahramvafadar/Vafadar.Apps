using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Vafadar.Backup.Security;

/// <summary>
/// Password-based encryption of backup packages with AES-256-GCM and a PBKDF2-SHA256 derived key.
/// </summary>
/// <remarks>
/// <para>Layout (all integers little endian):</para>
/// <code>
/// magic "VBE1" (4) | PBKDF2 iterations (4) | salt (16) | nonce (12) | tag (16) | ciphertext (n)
/// </code>
/// <para>
/// The header (everything before the tag) is authenticated as associated data, so changing the iteration count, salt
/// or nonce is detected. Passwords are normalized first (see <see cref="NormalizePassword"/>), so the same password
/// typed on different keyboards produces the same key.
/// </para>
/// <para>The password is never stored. If the user forgets it, the backup cannot be recovered.</para>
/// </remarks>
public static class BackupEncryption
{
    /// <summary>The default PBKDF2 iteration count (OWASP recommendation for PBKDF2-HMAC-SHA256).</summary>
    public const int DefaultIterations = 600_000;

    private const int MaxIterations = 10_000_000;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int HeaderSize = 4 + 4 + SaltSize + NonceSize;

    private static ReadOnlySpan<byte> Magic => "VBE1"u8;

    /// <summary>Returns whether <paramref name="data"/> is an encrypted package.</summary>
    public static bool IsEncrypted(ReadOnlySpan<byte> data) => data.StartsWith(Magic);

    /// <summary>Encrypts <paramref name="plaintext"/> with <paramref name="password"/>.</summary>
    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, string password) =>
        Encrypt(plaintext, password, DefaultIterations);

    /// <summary>Decrypts a package created by <see cref="Encrypt(ReadOnlySpan{byte}, string)"/>.</summary>
    /// <exception cref="BackupException">The data is not an encrypted package, or the password is wrong.</exception>
    public static byte[] Decrypt(ReadOnlySpan<byte> data, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        EnsureSupported();

        if (data.Length < HeaderSize + TagSize || !IsEncrypted(data))
        {
            throw new BackupException(BackupError.InvalidFormat, "The data is not an encrypted backup package.");
        }

        var header = data[..HeaderSize];
        var iterations = BinaryPrimitives.ReadInt32LittleEndian(header[4..8]);
        if (iterations is <= 0 or > MaxIterations)
        {
            throw new BackupException(BackupError.InvalidFormat, "The encrypted backup package header is invalid.");
        }

        var salt = header.Slice(8, SaltSize);
        var nonce = header.Slice(8 + SaltSize, NonceSize);
        var tag = data.Slice(HeaderSize, TagSize);
        var ciphertext = data[(HeaderSize + TagSize)..];

        Span<byte> key = stackalloc byte[KeySize];
        DeriveKey(password, salt, iterations, key);

        var plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, header);
            return plaintext;
        }
        catch (AuthenticationTagMismatchException ex)
        {
            throw new BackupException(BackupError.InvalidPasswordOrCorrupted, "The password is wrong or the backup is damaged.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>
    /// Normalizes a password: Unicode NFC, Arabic Yeh/Kaf to their Persian forms, and Persian/Arabic-Indic digits to
    /// ASCII digits. Different Persian keyboard layouts produce different code points for these characters.
    /// </summary>
    public static string NormalizePassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var builder = new StringBuilder(password.Normalize(NormalizationForm.FormC));
        for (var i = 0; i < builder.Length; i++)
        {
            builder[i] = builder[i] switch
            {
                'ي' or 'ى' => 'ی', // Arabic Yeh / Alef Maksura -> Persian Yeh
                'ك' => 'ک', // Arabic Kaf -> Persian Keheh
                >= '۰' and <= '۹' => (char)('0' + (builder[i] - '۰')), // Persian digits
                >= '٠' and <= '٩' => (char)('0' + (builder[i] - '٠')), // Arabic-Indic digits
                var c => c,
            };
        }

        return builder.ToString();
    }

    internal static byte[] Encrypt(ReadOnlySpan<byte> plaintext, string password, int iterations)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(iterations, MaxIterations);
        EnsureSupported();

        var result = new byte[HeaderSize + TagSize + plaintext.Length];
        var span = result.AsSpan();

        Magic.CopyTo(span);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..8], iterations);
        var salt = span.Slice(8, SaltSize);
        var nonce = span.Slice(8 + SaltSize, NonceSize);
        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(nonce);

        var header = span[..HeaderSize];
        var tag = span.Slice(HeaderSize, TagSize);
        var ciphertext = span[(HeaderSize + TagSize)..];

        Span<byte> key = stackalloc byte[KeySize];
        DeriveKey(password, salt, iterations, key);
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, header);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        return result;
    }

    private static void DeriveKey(string password, ReadOnlySpan<byte> salt, int iterations, Span<byte> key)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(NormalizePassword(password));
        try
        {
            Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, key, iterations, HashAlgorithmName.SHA256);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static void EnsureSupported()
    {
        if (!AesGcm.IsSupported)
        {
            throw new PlatformNotSupportedException("AES-GCM is not supported on this platform; encrypted backups are unavailable.");
        }
    }
}
