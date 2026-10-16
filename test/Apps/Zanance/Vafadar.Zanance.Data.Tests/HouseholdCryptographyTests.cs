using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Production standard crypto interoperability and authority refusals; this does not establish the complete shared app.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdCryptographyTests
{
    /// <summary>Independent framework GCM/ECDSA verifies the actual compact JWE/JWS bytes, not another wrapper round trip.</summary>
    [Fact]
    public void Member_envelope_interoperates_with_standard_framework_crypto_and_exact_authority()
    {
        using var owner = SigningRecipientIdentity.Create();
        var key = HouseholdCryptography.CreateEpochKey(); var crypto = new HouseholdCryptography(TimeProvider.System);
        var context = Context(); var clear = "Fictitious KWD expense: 1400 minor units"u8.ToArray();
        try
        {
            var envelope = crypto.EncryptForMembers(context, clear, owner, key);
            var parts = envelope.Split('.'); Assert.Equal(5, parts.Length); Assert.Empty(parts[1]);
            var signed = OpenGcm(parts, key);
            try
            {
                VerifyFrameworkSignature(Encoding.UTF8.GetString(signed), owner.Public);
                var restored = crypto.DecryptForMembers(context, envelope, owner.Public, key);
                try { Assert.True(CryptographicOperations.FixedTimeEquals(clear, restored), "Original synthetic clear bytes changed."); }
                finally { CryptographicOperations.ZeroMemory(restored); }
            }
            finally { CryptographicOperations.ZeroMemory(signed); }
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(clear); }
    }

    /// <summary>Framework RSA-OAEP-SHA256/MGF1 and GCM open the library's actual approved-device grant.</summary>
    [Fact]
    public void Device_grant_uses_sha256_oaep_and_restored_private_handles_without_server_secrets()
    {
        using var owner = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
        var privateBytes = device.ExportForProtectedStorage(); var epoch = HouseholdCryptography.CreateEpochKey();
        try
        {
            using var restoredDevice = SigningRecipientIdentity.ImportFromProtectedStorage(privateBytes);
            Assert.Equal(device.Public.Fingerprint, restoredDevice.Public.Fingerprint);
            var context = new HouseholdProtectionContext(Guid.NewGuid(), 1, Guid.NewGuid(), HouseholdProtectionPurpose.DeviceGrant, device.Public.Fingerprint);
            var crypto = new HouseholdCryptography(TimeProvider.System);
            var envelope = crypto.EncryptForDevice(context, epoch, owner, restoredDevice.Public);
            var signingLength = BinaryPrimitives.ReadInt32BigEndian(privateBytes);
            using var rsa = RSA.Create(); rsa.ImportPkcs8PrivateKey(privateBytes.AsSpan(8 + signingLength), out _);
            var parts = envelope.Split('.'); var cek = rsa.Decrypt(Decode(parts[1]), RSAEncryptionPadding.OaepSHA256);
            try
            {
                var signed = OpenGcm(parts, cek);
                try { VerifyFrameworkSignature(Encoding.UTF8.GetString(signed), owner.Public); }
                finally { CryptographicOperations.ZeroMemory(signed); }
            }
            finally { CryptographicOperations.ZeroMemory(cek); }
            var received = crypto.DecryptForDevice(context, envelope, owner.Public, restoredDevice);
            try { Assert.True(CryptographicOperations.FixedTimeEquals(epoch, received), "Original synthetic epoch changed."); }
            finally { CryptographicOperations.ZeroMemory(received); }
            using var stranger = SigningRecipientIdentity.Create();
            Assert.Throws<CryptographicException>(() => crypto.DecryptForDevice(context, envelope, owner.Public, stranger));
        }
        finally { CryptographicOperations.ZeroMemory(privateBytes); CryptographicOperations.ZeroMemory(epoch); }
    }

    /// <summary>A valid ciphertext is not transferable to another scope, operation, epoch, purpose, signer or key.</summary>
    [Theory]
    [InlineData("space")]
    [InlineData("operation")]
    [InlineData("epoch")]
    [InlineData("purpose")]
    [InlineData("author")]
    [InlineData("key")]
    [InlineData("ciphertext")]
    [InlineData("short-tag")]
    [InlineData("algorithm")]
    public void Changed_authority_or_cryptographic_content_is_refused(string change)
    {
        using var owner = SigningRecipientIdentity.Create(); using var stranger = SigningRecipientIdentity.Create();
        var key = HouseholdCryptography.CreateEpochKey(); var wrongKey = HouseholdCryptography.CreateEpochKey();
        try
        {
            var crypto = new HouseholdCryptography(TimeProvider.System); var context = Context();
            var envelope = crypto.EncryptForMembers(context, "Fictitious financial packet"u8, owner, key);
            var expected = new HouseholdProtectionContext(change == "space" ? Guid.NewGuid() : context.SpaceId,
                change == "epoch" ? 2 : 1, change == "operation" ? Guid.NewGuid() : context.OperationId,
                change == "purpose" ? HouseholdProtectionPurpose.Attachment : HouseholdProtectionPurpose.Financial, "members");
            if (change is "ciphertext" or "short-tag" or "algorithm")
            {
                var parts = envelope.Split('.');
                if (change == "ciphertext") parts[3] = (parts[3][0] == 'A' ? "B" : "A") + parts[3][1..];
                if (change == "short-tag") parts[4] = Encode(Decode(parts[4])[..12]);
                if (change == "algorithm") parts[0] = Encode("{\"alg\":\"RSA1_5\",\"enc\":\"A256GCM\",\"typ\":\"zanance-household+jwe\",\"cty\":\"JWT\",\"kid\":\"refused\"}"u8);
                envelope = string.Join('.', parts);
            }
            Assert.Throws<CryptographicException>(() => crypto.DecryptForMembers(expected, envelope,
                change == "author" ? stranger.Public : owner.Public, change == "key" ? wrongKey : key));
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(wrongKey); }
    }

    /// <summary>Actual maximum input crosses nested base64 framing without a hidden lower limit or truncation.</summary>
    [Fact]
    public void Maximum_bounded_payload_is_preserved_and_oversized_payload_is_refused()
    {
        using var owner = SigningRecipientIdentity.Create(); var key = HouseholdCryptography.CreateEpochKey();
        var content = RandomNumberGenerator.GetBytes(HouseholdCryptography.MaximumPlaintextBytes);
        try
        {
            var crypto = new HouseholdCryptography(TimeProvider.System); var context = Context();
            var envelope = crypto.EncryptForMembers(context, content, owner, key);
            var received = crypto.DecryptForMembers(context, envelope, owner.Public, key);
            try { Assert.True(CryptographicOperations.FixedTimeEquals(content, received), "Bounded synthetic bytes changed."); }
            finally { CryptographicOperations.ZeroMemory(received); }
            Assert.Throws<CryptographicException>(() => crypto.EncryptForMembers(context, new byte[content.Length + 1], owner, key));
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(content); }
    }

    /// <summary>Public signed authority cannot be reused under a different expected domain type or after alteration.</summary>
    [Fact]
    public void Signed_metadata_requires_exact_trusted_identity_type_and_payload()
    {
        using var owner = SigningRecipientIdentity.Create();
        var payload = "{\"fictitious\":true,\"version\":1}"u8.ToArray();
        var token = StandardJoseSignatures.Sign(payload, owner, "zanance-household-membership+jwt");
        var received = StandardJoseSignatures.Verify(token, owner.Public, "zanance-household-membership+jwt", 4096);
        try { Assert.True(CryptographicOperations.FixedTimeEquals(payload, received), "Public synthetic metadata changed."); }
        finally { CryptographicOperations.ZeroMemory(received); }
        Assert.Throws<CryptographicException>(() => StandardJoseSignatures.Verify(token, owner.Public, "zanance-household-commit+jwt", 4096));
        var parts = token.Split('.'); parts[1] = Encode("{\"fictitious\":true,\"version\":2}"u8);
        Assert.Throws<CryptographicException>(() => StandardJoseSignatures.Verify(string.Join('.', parts), owner.Public, "zanance-household-membership+jwt", 4096));
    }

    private static HouseholdProtectionContext Context() => new(Guid.NewGuid(), 1, Guid.NewGuid(), HouseholdProtectionPurpose.Financial, "members");

    private static byte[] OpenGcm(string[] parts, byte[] key)
    {
        var cipher = Decode(parts[3]); var clear = new byte[cipher.Length];
        using var aes = new AesGcm(key, 16); aes.Decrypt(Decode(parts[2]), cipher, Decode(parts[4]), clear, Encoding.ASCII.GetBytes(parts[0]));
        return clear;
    }

    private static void VerifyFrameworkSignature(string signed, PublicSigningRecipientIdentity owner)
    {
        var parts = signed.Split('.'); Assert.Equal(3, parts.Length);
        using var key = ECDsa.Create(); key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(owner.SigningKey), out _);
        Assert.True(key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), Decode(parts[2]), HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation), "Independent ES256 verification failed.");
    }

    private static byte[] Decode(string text) => Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/') + new string('=', (4 - text.Length % 4) % 4));
    private static string Encode(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
