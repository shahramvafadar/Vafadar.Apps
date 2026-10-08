using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Zanance.Encryption.Windows.Tests;

/// <summary>Fictitious key-envelope behaviour on the current Windows user, without reading existing protected values.</summary>
[SupportedOSPlatform("windows")]
public sealed class KeyWrappingTests
{
    /// <summary>Checks user-bound protection and profile-specific context without existing user secrets.</summary>
    [Fact]
    [Trait("AT", "AT-75")]
    public void DpapiUserScopeRoundTripsAndRejectsDifferentProfileEntropy()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var entropy = Encoding.UTF8.GetBytes("fictitious-sec01-profile-a-generation-1");
        var wrapped = ProtectedData.Protect(key, entropy, DataProtectionScope.CurrentUser);
        var restored = ProtectedData.Unprotect(wrapped, entropy, DataProtectionScope.CurrentUser);
        try
        {
            Assert.Equal(key, restored);
            Assert.NotEqual(key, wrapped);
            Assert.Throws<CryptographicException>(() => ProtectedData.Unprotect(wrapped,
                Encoding.UTF8.GetBytes("fictitious-sec01-profile-b-generation-1"), DataProtectionScope.CurrentUser));
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(restored); }
    }

    /// <summary>Checks authenticated wrapping with a generated fictitious password and versioned KDF policy.</summary>
    [Fact]
    [Trait("AT", "AT-75")]
    public void PasswordDerivedEnvelopeRejectsWrongPasswordTamperingAndDifferentProfile()
    {
        // Generated test material only; never a user password. This measures the existing PBKDF2 policy path.
        var password = RandomNumberGenerator.GetBytes(32);
        var wrong = RandomNumberGenerator.GetBytes(32);
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = RandomNumberGenerator.GetBytes(32);
        var kek = Rfc2898DeriveBytes.Pbkdf2(password, salt, 600000, HashAlgorithmName.SHA256, 32);
        var wrongKek = Rfc2898DeriveBytes.Pbkdf2(wrong, salt, 600000, HashAlgorithmName.SHA256, 32);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[32]; var tag = new byte[16]; var recovered = new byte[32];
        var aad = Encoding.UTF8.GetBytes("v1|fictitious-profile-a|generation-1");
        try
        {
            using var aes = new AesGcm(kek, 16);
            aes.Encrypt(nonce, key, cipher, tag, aad);
            aes.Decrypt(nonce, cipher, tag, recovered, aad);
            Assert.Equal(key, recovered);
            using var invalid = new AesGcm(wrongKek, 16);
            Assert.Throws<AuthenticationTagMismatchException>(() => invalid.Decrypt(nonce, cipher, tag, recovered, aad));
            Assert.Throws<AuthenticationTagMismatchException>(() => aes.Decrypt(nonce, cipher, tag, recovered, Encoding.UTF8.GetBytes("profile-b")));
            cipher[0] ^= 1;
            Assert.Throws<AuthenticationTagMismatchException>(() => aes.Decrypt(nonce, cipher, tag, recovered, aad));
        }
        finally
        {
            foreach (var material in new[] { password, wrong, key, kek, wrongKek, recovered }) CryptographicOperations.ZeroMemory(material);
        }
    }
}
