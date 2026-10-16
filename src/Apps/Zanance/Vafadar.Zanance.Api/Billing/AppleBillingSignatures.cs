using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Jose;
using Vafadar.Cryptography;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Fixed ES256 JOSE and system PKIX verification; no private cipher, remote key URL or machine trust-root mutation.</summary>
internal sealed class AppleBillingSignatures(TimeProvider time)
{
    private const string RootSha256 = "63343abfb89a6a03ebb57e9b3f5fa7be7c4f5c756f3017b3a8c488c3653e9179";
    internal static readonly JwtSettings Settings = new JwtSettings().RegisterMapper(new BillingJoseHeaderMapper());

    /// <summary>Authenticates Apple's exact three-certificate chain, signing purposes and current online revocation before reading claims.</summary>
    internal JsonDocument Verify(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 65536 || token.Count(c => c == '.') != 2)
            throw new BillingRefusal(422, "AppleSignedDataRefused");
        byte[]? payload = null;
        try
        {
            var headers = JWT.Headers(token, settings: Settings);
            if (headers.Count != 2 || !headers.TryGetValue("alg", out var algorithm) || algorithm is not string alg || alg != "ES256" ||
                !headers.TryGetValue("x5c", out var chainValue) || chainValue is not string[] certificates || certificates.Length != 3)
                throw new BillingRefusal(422, "AppleSignedDataRefused");
            using var leaf = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificates[0]));
            using var intermediate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificates[1]));
            using var suppliedRoot = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificates[2]));
            using var rootStream = typeof(AppleBillingSignatures).Assembly.GetManifestResourceStream(
                "Vafadar.Zanance.Api.Billing.AppleRootCA-G3.cer") ?? throw new BillingRefusal(503, "AppleTrustAnchorUnavailable");
            using var memory = new MemoryStream(); rootStream.CopyTo(memory); var rootBytes = memory.ToArray();
            if (Convert.ToHexStringLower(SHA256.HashData(rootBytes)) != RootSha256)
                throw new BillingRefusal(503, "AppleTrustAnchorUnavailable");
            using var root = X509CertificateLoader.LoadCertificate(rootBytes);
            if (!suppliedRoot.RawData.AsSpan().SequenceEqual(root.RawData) ||
                !leaf.Extensions.Any(e => e.Oid?.Value == "1.2.840.113635.100.6.11.1") ||
                !intermediate.Extensions.Any(e => e.Oid?.Value == "1.2.840.113635.100.6.2.1"))
                throw new BillingRefusal(422, "AppleCertificatePurposeRefused");
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(root); chain.ChainPolicy.ExtraStore.Add(intermediate);
            chain.ChainPolicy.DisableCertificateDownloads = true;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            chain.ChainPolicy.VerificationTime = time.GetUtcNow().UtcDateTime;
            chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(5);
            if (!chain.Build(leaf))
            {
                if (chain.ChainStatus.Any(s => (s.Status & (X509ChainStatusFlags.RevocationStatusUnknown | X509ChainStatusFlags.OfflineRevocation)) != 0))
                    throw new BillingRefusal(503, "AppleCertificateStatusUnavailable");
                throw new BillingRefusal(422, "AppleCertificateChainRefused");
            }
            if (chain.ChainElements.Count != 3 || !chain.ChainElements[1].Certificate.RawData.AsSpan().SequenceEqual(intermediate.RawData) ||
                !chain.ChainElements[2].Certificate.RawData.AsSpan().SequenceEqual(root.RawData))
                throw new BillingRefusal(422, "AppleCertificateChainRefused");
            using var key = leaf.GetECDsaPublicKey() ?? throw new BillingRefusal(422, "AppleSignatureKeyRefused");
            if (key.KeySize != 256 || key.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7")
                throw new BillingRefusal(422, "AppleSignatureKeyRefused");
            payload = JWT.VerifyBytes(token, key, JwsAlgorithm.ES256, settings: Settings);
            var document = StrictJsonPayload.Deserialize(payload, BillingProviderJson.Default.JsonDocument, 32768);
            try
            {
                var signed = AppleBillingFields.Milliseconds(document.RootElement, "signedDate");
                if (signed > time.GetUtcNow().AddMinutes(1)) throw new BillingRefusal(422, "AppleSignedTimeRefused");
                return document;
            }
            catch { document.Dispose(); throw; }
        }
        catch (Exception error) when (error is JoseException or CryptographicException or JsonException or FormatException or ArgumentException or InvalidOperationException)
        { throw new BillingRefusal(422, "AppleSignedDataRefused"); }
        finally { if (payload is not null) CryptographicOperations.ZeroMemory(payload); }
    }
}

/// <summary>Bounded reflection-free provider JOSE headers; no untrusted JKU, embedded key or algorithm negotiation.</summary>
internal sealed class BillingJoseHeaderMapper : IJsonMapper
{
    /// <inheritdoc />
    public string Serialize(object obj)
    {
        if (obj is not IDictionary<string, object> values || values.Count > 4) throw new JsonException();
        using var memory = new MemoryStream();
        using (var writer = new Utf8JsonWriter(memory))
        {
            writer.WriteStartObject();
            foreach (var (name, value) in values)
            {
                if (name is not ("alg" or "kid" or "typ") || value is not string text || text.Length > 256) throw new JsonException();
                writer.WriteString(name, text);
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(memory.GetBuffer(), 0, checked((int)memory.Length));
    }
    /// <inheritdoc />
    public T Parse<T>(string json)
    {
        if (json.Length > 16384 || !typeof(T).IsAssignableFrom(typeof(Dictionary<string, object>))) throw new JsonException();
        using var document = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(json), BillingProviderJson.Default.JsonDocument, 16384);
        var values = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (values.Count >= 4) throw new JsonException();
            if (property.Name == "x5c" && property.Value.ValueKind == JsonValueKind.Array && property.Value.GetArrayLength() == 3)
            {
                var certificates = property.Value.EnumerateArray().Select(v => v.ValueKind == JsonValueKind.String &&
                    v.GetString() is { Length: > 0 and <= 8192 } s ? s : throw new JsonException()).ToArray();
                values.Add(property.Name, certificates);
            }
            else if (property.Name is "alg" or "kid" or "typ" && property.Value.ValueKind == JsonValueKind.String &&
                property.Value.GetString() is { Length: > 0 and <= 256 } text) values.Add(property.Name, text);
            else throw new JsonException();
        }
        return (T)(object)values;
    }
}

/// <summary>Exact Apple scalar readers; no locale, floating-point timestamp or truthy coercion.</summary>
internal static class AppleBillingFields
{
    /// <summary>Reads actual integral epoch milliseconds within the supported UTC range.</summary>
    internal static DateTimeOffset Milliseconds(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var value) || !value.TryGetInt64(out var milliseconds) || milliseconds < 0)
            throw new BillingRefusal(422, "AppleTimeRefused");
        try { return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds); }
        catch (ArgumentOutOfRangeException) { throw new BillingRefusal(422, "AppleTimeRefused"); }
    }
    /// <summary>Reads only a bounded decimal transaction identifier, never a URL or arbitrary API path.</summary>
    internal static string Transaction(string value) => value.Length is > 0 and <= 64 && value.All(char.IsAsciiDigit)
        ? value : throw new BillingRefusal(422, "AppleTransactionRefused");
}
