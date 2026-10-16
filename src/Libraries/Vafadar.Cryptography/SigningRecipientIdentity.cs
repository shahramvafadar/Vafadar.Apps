using System.Security.Cryptography;

namespace Vafadar.Cryptography;

/// <summary>Public-only device authority; confirmation of its fingerprint belongs to explicit membership approval.</summary>
public sealed record PublicSigningRecipientIdentity(string SigningKey, string WrappingKey)
{
    /// <summary>Canonical digest of the two bounded standard DER public keys, not an identity-server assertion.</summary>
    public string Fingerprint
    {
        get
        {
            var signing = Decode(SigningKey, 256); var wrapping = Decode(WrappingKey, 1024);
            try
            {
                // SPKI encodings are self-delimiting DER. Domain separation prevents cross-protocol key reuse.
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                hash.AppendData("Vafadar signing recipient identity v1\0"u8); hash.AppendData(signing); hash.AppendData(wrapping);
                return Convert.ToHexStringLower(hash.GetHashAndReset());
            }
            finally { CryptographicOperations.ZeroMemory(signing); CryptographicOperations.ZeroMemory(wrapping); }
        }
    }

    /// <summary>Imports only the explicit public ES256 key, with no remote discovery or untrusted key resolver.</summary>
    public ECDsa OpenSigningKey()
    {
        var bytes = Decode(SigningKey, 256); var key = ECDsa.Create();
        try
        {
            key.ImportSubjectPublicKeyInfo(bytes, out var read);
            if (read != bytes.Length || key.KeySize != 256 || key.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7" ||
                !CryptographicOperations.FixedTimeEquals(bytes, key.ExportSubjectPublicKeyInfo()))
                throw new CryptographicException("Unsupported household public identity.");
            return key;
        }
        catch { key.Dispose(); throw; }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Imports only the standard RSA3072 public recipient key approved with its joint fingerprint.</summary>
    public RSA OpenWrappingKey()
    {
        var bytes = Decode(WrappingKey, 1024); var key = RSA.Create();
        try
        {
            key.ImportSubjectPublicKeyInfo(bytes, out var read);
            if (read != bytes.Length || key.KeySize != 3072 || !CryptographicOperations.FixedTimeEquals(bytes, key.ExportSubjectPublicKeyInfo()))
                throw new CryptographicException("Unsupported household recipient key.");
            return key;
        }
        catch { key.Dispose(); throw; }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Bounds public encodings before allocating or importing attacker-supplied DER.</summary>
    private static byte[] Decode(string text, int maximumBytes)
    {
        if (string.IsNullOrEmpty(text) || text.Length > (maximumBytes + 2) / 3 * 4)
            throw new CryptographicException("Household public identity bound exceeded.");
        try
        {
            var bytes = Convert.FromBase64String(text);
            if (bytes.Length == 0 || bytes.Length > maximumBytes || Convert.ToBase64String(bytes) != text)
                throw new CryptographicException("Household public identity encoding is invalid.");
            return bytes;
        }
        catch (FormatException) { throw new CryptographicException("Household public identity encoding is invalid."); }
    }
}

/// <summary>Owned native key handles; private export is for protected local persistence, never a service request or log.</summary>
public sealed class SigningRecipientIdentity : IDisposable
{
    private static readonly Jose.JwtSettings Settings = StandardJoseSuite.CreateSettings();
    private readonly ECDsa _signing;
    private readonly RSA _wrapping;

    private SigningRecipientIdentity(ECDsa signing, RSA wrapping)
    { _signing = signing; _wrapping = wrapping; }

    /// <summary>Generates independent maintained-platform signing/wrapping keys without owner credentials.</summary>
    public static SigningRecipientIdentity Create()
    {
        var signing = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        try { return new(signing, RSA.Create(3072)); }
        catch { signing.Dispose(); throw; }
    }

    /// <summary>Bound public-only identity used by explicit approval, signed membership and standard JOSE.</summary>
    public PublicSigningRecipientIdentity Public => new(Convert.ToBase64String(_signing.ExportSubjectPublicKeyInfo()),
        Convert.ToBase64String(_wrapping.ExportSubjectPublicKeyInfo()));

    /// <summary>The exact private signing handle; callers cannot select a different algorithm.</summary>
    internal ECDsa Signing => _signing;
    /// <summary>The exact private recipient handle, never exposed through public identity serialization.</summary>
    internal RSA Wrapping => _wrapping;

    /// <summary>Opens only fixed RSA-OAEP-256/A256GCM; callers must verify inner signer/context before using returned bytes.</summary>
    public byte[] DecryptRecipientEnvelope(string envelope)
    {
        if (string.IsNullOrEmpty(envelope) || envelope.Length > 1024 * 1024) throw new CryptographicException("Recipient envelope refused.");
        try { return Jose.JWT.DecryptBytes(envelope, _wrapping, Jose.JweAlgorithm.RSA_OAEP_256, Jose.JweEncryption.A256GCM, settings: Settings); }
        catch (Exception error) when (error is Jose.JoseException or ArgumentException or CryptographicException or FormatException or System.Text.Json.JsonException)
        { throw new CryptographicException("Recipient envelope refused."); }
    }

    /// <summary>Exports bounded standard PKCS8 private material for an already protected local store only.</summary>
    public byte[] ExportForProtectedStorage()
    {
        var signing = _signing.ExportPkcs8PrivateKey(); var wrapping = _wrapping.ExportPkcs8PrivateKey();
        try
        {
            var output = new byte[8 + signing.Length + wrapping.Length];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(output, signing.Length);
            signing.CopyTo(output, 4);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(output.AsSpan(4 + signing.Length), wrapping.Length);
            wrapping.CopyTo(output, 8 + signing.Length); return output;
        }
        finally { CryptographicOperations.ZeroMemory(signing); CryptographicOperations.ZeroMemory(wrapping); }
    }

    /// <summary>Imports exact bounded local PKCS8, rejects trailing bytes and validates the selected public algorithms.</summary>
    public static SigningRecipientIdentity ImportFromProtectedStorage(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 16 or > 8192) throw new CryptographicException("Household private material bound exceeded.");
        var signingLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes);
        if (signingLength is < 1 or > 512 || signingLength > bytes.Length - 8)
            throw new CryptographicException("Household private material framing is invalid.");
        var wrappingLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes[(4 + signingLength)..]);
        if (wrappingLength < 1 || wrappingLength != bytes.Length - 8 - signingLength)
            throw new CryptographicException("Household private material framing is invalid.");
        var signing = ECDsa.Create(); var wrapping = RSA.Create();
        try
        {
            signing.ImportPkcs8PrivateKey(bytes.Slice(4, signingLength), out var signingRead);
            wrapping.ImportPkcs8PrivateKey(bytes[(8 + signingLength)..], out var wrappingRead);
            if (signingRead != signingLength || wrappingRead != wrappingLength)
                throw new CryptographicException("Trailing household private material refused.");
            var identity = new SigningRecipientIdentity(signing, wrapping);
            using var checkedSigning = identity.Public.OpenSigningKey();
            using var checkedWrapping = identity.Public.OpenWrappingKey();
            return identity;
        }
        catch { signing.Dispose(); wrapping.Dispose(); throw; }
    }

    /// <inheritdoc />
    public void Dispose() { _signing.Dispose(); _wrapping.Dispose(); }
}
