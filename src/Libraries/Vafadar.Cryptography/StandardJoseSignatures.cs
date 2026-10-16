using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Jose;

namespace Vafadar.Cryptography;

/// <summary>Bounded standard ES256 signed-message operations; callers still validate their exact domain/version/authority.</summary>
public static class StandardJoseSignatures
{
    private static readonly JwtSettings Settings = StandardJoseSuite.CreateSettings();

    /// <summary>Signs explicit bytes with an owned P256 key and fixed purpose, without serializing private key handles.</summary>
    public static string Sign(byte[] payload, SigningRecipientIdentity author, string type)
    {
        RequireType(type);
        if (payload is not { Length: > 0 and <= 512 * 1024 }) throw Refusal();
        try
        {
            return JWT.EncodeBytes(payload, author.Signing, JwsAlgorithm.ES256,
                extraHeaders: new Dictionary<string, object> { ["typ"] = type, ["kid"] = author.Public.Fingerprint }, settings: Settings);
        }
        catch (Exception error) when (IsContentError(error)) { throw Refusal(); }
    }

    /// <summary>Verifies only the caller's previously trusted exact public identity and purpose; never remote JKU/kid lookup.</summary>
    public static byte[] Verify(string token, PublicSigningRecipientIdentity trustedAuthor, string type, int maximumBytes)
    {
        RequireType(type);
        if (maximumBytes is < 1 or > 512 * 1024 || string.IsNullOrEmpty(token) ||
            token.Length > (maximumBytes + 2) / 3 * 4 + 4096 || token.Count(character => character == '.') != 2) throw Refusal();
        try
        {
            var header = JWT.Headers(token, settings: Settings);
            if (!header.TryGetValue("typ", out var actualType) || actualType is not string expectedType || expectedType != type ||
                !header.TryGetValue("kid", out var kid) || kid is not string expectedKid || expectedKid != trustedAuthor.Fingerprint)
                throw Refusal();
            using var signing = trustedAuthor.OpenSigningKey();
            var payload = JWT.VerifyBytes(token, signing, JwsAlgorithm.ES256, settings: Settings);
            if (payload.Length is < 1 || payload.Length > maximumBytes)
            { CryptographicOperations.ZeroMemory(payload); throw Refusal(); }
            return payload;
        }
        catch (Exception error) when (IsContentError(error)) { throw Refusal(); }
    }

    /// <summary>Verifies exact standard signature/purpose before strict caller-owned source-generated JSON, rejecting duplicate property reinterpretation.</summary>
    public static T VerifyJson<T>(string token, PublicSigningRecipientIdentity trustedAuthor, string type, int maximumBytes,
        JsonTypeInfo<T> json) where T : class
    {
        byte[]? payload = null;
        try
        {
            payload = Verify(token, trustedAuthor, type, maximumBytes);
            return StrictJsonPayload.Deserialize(payload, json, maximumBytes);
        }
        catch (Exception error) when (IsContentError(error)) { throw Refusal(); }
        finally { if (payload is not null) CryptographicOperations.ZeroMemory(payload); }
    }

    /// <summary>Allows only an explicit caller-owned printable purpose; it never comes from the message being verified.</summary>
    private static void RequireType(string type)
    {
        if (string.IsNullOrEmpty(type) || type.Length > 128 || type.Any(character => character is < '!' or > '~')) throw Refusal();
    }
    /// <summary>Library errors are classified without exposing raw tokens, claims or private material.</summary>
    private static bool IsContentError(Exception error) => error is JoseException or CryptographicException or ArgumentException
        or FormatException or System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException;
    /// <summary>Content-free refusal with no potentially sensitive inner exception.</summary>
    private static CryptographicException Refusal() => new("Protected signed message was refused.");
}
