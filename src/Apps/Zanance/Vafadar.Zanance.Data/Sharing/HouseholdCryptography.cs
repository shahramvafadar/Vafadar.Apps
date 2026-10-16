using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jose;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Fixed standard JOSE suite; no service-visible plaintext, key discovery, compression or algorithm fallback.</summary>
public sealed class HouseholdCryptography(TimeProvider time)
{
    /// <summary>Bounds one clear signed message; large captures/attachments need separately bound pieces.</summary>
    public const int MaximumPlaintextBytes = 256 * 1024;
    /// <summary>Bounds compact serialization before parsing, verification or allocation.</summary>
    public const int MaximumEnvelopeCharacters = 1024 * 1024;
    /// <summary>Standard AES256 epoch key size.</summary>
    public const int EpochKeyBytes = 32;
    private static readonly JwtSettings Settings = StandardJoseSuite.CreateSettings();

    /// <summary>Generates one independent epoch key for protected local storage and approved device grants.</summary>
    public static byte[] CreateEpochKey() => RandomNumberGenerator.GetBytes(EpochKeyBytes);

    /// <summary>Signs exact authority before maintained AES256-GCM authenticated JWE encryption.</summary>
    public string EncryptForMembers(HouseholdProtectionContext context, ReadOnlySpan<byte> plaintext,
        SigningRecipientIdentity author, byte[] epochKey)
    {
        RequireMembers(context, epochKey);
        return Encrypt(context, plaintext, author, epochKey, JweAlgorithm.DIR, EpochId(context));
    }

    /// <summary>Encrypts one verified recipient's material using standard RSA-OAEP-256 and AES256-GCM.</summary>
    public string EncryptForDevice(HouseholdProtectionContext context, ReadOnlySpan<byte> plaintext,
        SigningRecipientIdentity author, PublicSigningRecipientIdentity recipient)
    {
        RequireDevice(context, recipient);
        using var wrapping = recipient.OpenWrappingKey();
        return Encrypt(context, plaintext, author, wrapping, JweAlgorithm.RSA_OAEP_256, recipient.Fingerprint);
    }

    /// <summary>Verifies content against trusted expected membership/context, never its advertised issuer/key.</summary>
    public byte[] DecryptForMembers(HouseholdProtectionContext context, string envelope,
        PublicSigningRecipientIdentity author, byte[] epochKey)
    {
        RequireMembers(context, epochKey);
        return Decrypt(context, envelope, author, EpochId(context),
            () => JWT.DecryptBytes(envelope, epochKey, JweAlgorithm.DIR, JweEncryption.A256GCM, settings: Settings));
    }

    /// <summary>Opens only this approved device's grant, bound to exact author, epoch, operation and recipient.</summary>
    public byte[] DecryptForDevice(HouseholdProtectionContext context, string envelope,
        PublicSigningRecipientIdentity author, SigningRecipientIdentity recipient)
    {
        RequireDevice(context, recipient.Public);
        return Decrypt(context, envelope, author, recipient.Public.Fingerprint, () => recipient.DecryptRecipientEnvelope(envelope));
    }

    /// <summary>Uses the complete standard library JWS/JWE implementation, never hand-written cryptographic framing.</summary>
    private string Encrypt(HouseholdProtectionContext context, ReadOnlySpan<byte> plaintext,
        SigningRecipientIdentity author, object recipientKey, JweAlgorithm algorithm, string keyId)
    {
        if (plaintext.Length is < 1 or > MaximumPlaintextBytes) throw Refusal();
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes))
        {
            writer.WriteStartObject(); writer.WriteString("iss", author.Public.Fingerprint); writer.WriteString("aud", context.Audience);
            writer.WriteNumber("iat", time.GetUtcNow().ToUnixTimeSeconds());
            writer.WriteString("space", context.SpaceId.ToString("N")); writer.WriteString("epoch", context.Epoch.ToString(CultureInfo.InvariantCulture));
            writer.WriteString("operation", context.OperationId.ToString("N")); writer.WriteString("recipient", context.Recipient);
            writer.WriteString("format", "zanance.household.v1"); writer.WriteBase64String("content", plaintext); writer.WriteEndObject();
        }
        var payload = bytes.ToArray(); byte[]? signedBytes = null;
        try
        {
            var signed = StandardJoseSignatures.Sign(payload, author, "zanance-household+jwt");
            signedBytes = Encoding.UTF8.GetBytes(signed);
            var encrypted = JWT.EncodeBytes(signedBytes, recipientKey, algorithm, JweEncryption.A256GCM,
                extraHeaders: new Dictionary<string, object> { ["typ"] = "zanance-household+jwe", ["cty"] = "JWT", ["kid"] = keyId }, settings: Settings);
            if (encrypted.Length > MaximumEnvelopeCharacters) throw Refusal();
            return encrypted;
        }
        catch (Exception error) when (IsContentFailure(error)) { throw Refusal(); }
        finally
        {
            CryptographicOperations.ZeroMemory(payload); CryptographicOperations.ZeroMemory(bytes.GetBuffer());
            if (signedBytes is not null) CryptographicOperations.ZeroMemory(signedBytes);
        }
    }

    /// <summary>Strict dedicated decrypt/verify methods prevent signed/encrypted/none or asymmetric/symmetric confusion.</summary>
    private static byte[] Decrypt(HouseholdProtectionContext context, string envelope,
        PublicSigningRecipientIdentity author, string keyId, Func<byte[]> decrypt)
    {
        if (string.IsNullOrEmpty(envelope) || envelope.Length > MaximumEnvelopeCharacters || envelope.Count(character => character == '.') != 4)
            throw Refusal();
        byte[]? signedBytes = null; byte[]? payload = null;
        try
        {
            var outer = JWT.Headers(envelope, settings: Settings);
            RequireHeader(outer, "typ", "zanance-household+jwe"); RequireHeader(outer, "cty", "JWT"); RequireHeader(outer, "kid", keyId);
            signedBytes = decrypt();
            if (signedBytes.Length > MaximumEnvelopeCharacters) throw Refusal();
            var signed = Encoding.UTF8.GetString(signedBytes);
            if (signed.Count(character => character == '.') != 2) throw Refusal();
            var inner = JWT.Headers(signed, settings: Settings);
            RequireHeader(inner, "typ", "zanance-household+jwt"); RequireHeader(inner, "kid", author.Fingerprint);
            payload = StandardJoseSignatures.Verify(signed, author, "zanance-household+jwt", MaximumPlaintextBytes * 2);
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 8 });
            var values = document.RootElement;
            if (values.ValueKind != JsonValueKind.Object || values.EnumerateObject().Count() != 9 ||
                values.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != 9 ||
                values.GetProperty("format").GetString() != "zanance.household.v1" ||
                values.GetProperty("iss").GetString() != author.Fingerprint || values.GetProperty("aud").GetString() != context.Audience ||
                values.GetProperty("space").GetString() != context.SpaceId.ToString("N") ||
                values.GetProperty("epoch").GetString() != context.Epoch.ToString(CultureInfo.InvariantCulture) ||
                values.GetProperty("operation").GetString() != context.OperationId.ToString("N") ||
                values.GetProperty("recipient").GetString() != context.Recipient || !values.GetProperty("iat").TryGetInt64(out _)) throw Refusal();
            // Archive lifetime is not JWT exp. Current signed membership and durable sequence/idempotency
            // authority must separately authorize applying the row; a valid cipher never grants that authority.
            var encoded = values.GetProperty("content").GetString();
            if (encoded is null || encoded.Length > (MaximumPlaintextBytes + 2) / 3 * 4) throw Refusal();
            var clear = Convert.FromBase64String(encoded);
            if (clear.Length is < 1 or > MaximumPlaintextBytes)
            { CryptographicOperations.ZeroMemory(clear); throw Refusal(); }
            return clear;
        }
        catch (Exception error) when (IsContentFailure(error)) { throw Refusal(); }
        finally
        {
            if (signedBytes is not null) CryptographicOperations.ZeroMemory(signedBytes);
            if (payload is not null) CryptographicOperations.ZeroMemory(payload);
        }
    }

    /// <summary>Rejects wrong-purpose or wrong-length direct material before library operations.</summary>
    private static void RequireMembers(HouseholdProtectionContext context, byte[] key)
    {
        if (context.Purpose is not (HouseholdProtectionPurpose.Financial or HouseholdProtectionPurpose.Attachment) ||
            context.Recipient != "members" || key is not { Length: EpochKeyBytes }) throw Refusal();
    }
    /// <summary>Requires an out-of-band verified exact device, not a server-advertised replacement recipient.</summary>
    private static void RequireDevice(HouseholdProtectionContext context, PublicSigningRecipientIdentity recipient)
    {
        if (context.Purpose is not (HouseholdProtectionPurpose.DeviceGrant or HouseholdProtectionPurpose.Recovery or HouseholdProtectionPurpose.OwnerRoot) ||
            context.Recipient != recipient.Fingerprint) throw Refusal();
    }
    /// <summary>Canonical outer routing identifier, checked against trusted local epoch authority.</summary>
    private static string EpochId(HouseholdProtectionContext context) => context.SpaceId.ToString("N") + ":" + context.Epoch.ToString(CultureInfo.InvariantCulture);
    /// <summary>Exact header comparisons never download or resolve key references.</summary>
    private static void RequireHeader(IDictionary<string, object> header, string name, string expected)
    { if (!header.TryGetValue(name, out var value) || value is not string text || text != expected) throw Refusal(); }
    /// <summary>Content-free failure without raw token/claim or a library inner exception.</summary>
    private static CryptographicException Refusal() => new("Household protected message was refused.");
    /// <summary>Untrusted content errors never escape with potentially sensitive library exception descriptions.</summary>
    private static bool IsContentFailure(Exception error) => error is JoseException or ArgumentException or CryptographicException
        or FormatException or JsonException or KeyNotFoundException or InvalidOperationException;
}
