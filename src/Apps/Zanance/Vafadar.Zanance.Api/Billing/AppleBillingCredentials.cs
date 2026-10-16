using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jose;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Explicit private Apple server-key port, never available to native callers.</summary>
internal interface IAppleBillingCredentials
{
    /// <summary>Creates one short-lived fixed-bundle ES256 API bearer from the operator's private mount.</summary>
    Task<string> AccessAsync(CancellationToken ct);
}

/// <summary>Loads only this service's configured key, with no ambient developer account or interactive login.</summary>
internal sealed class AppleBillingCredentials(IConfiguration configuration, ServiceStorage storage, TimeProvider time) : IAppleBillingCredentials
{
    /// <inheritdoc />
    public async Task<string> AccessAsync(CancellationToken ct)
    {
        byte[]? bytes = null; char[]? pem = null;
        try
        {
            var issuer = configuration["Billing:Apple:IssuerId"]; var keyId = configuration["Billing:Apple:KeyId"];
            var configured = configuration["Billing:Apple:PrivateKeyFile"];
            var directory = Path.GetFullPath(Path.Combine(storage.Root, "billing-credentials"));
            if (!Guid.TryParseExact(issuer, "D", out _) || keyId is not { Length: > 0 and <= 64 } || !keyId.All(char.IsAsciiLetterOrDigit) ||
                string.IsNullOrEmpty(configured) || !Path.IsPathFullyQualified(configured))
                throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable");
            var path = Path.GetFullPath(configured);
            if (Path.GetDirectoryName(path) != directory || !File.Exists(path) || new FileInfo(path).Length is < 1 or > 16384 ||
                (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable");
            bytes = await File.ReadAllBytesAsync(path, ct); if (bytes.Length > 16384) throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable");
            pem = Encoding.UTF8.GetChars(bytes);
            using var key = ECDsa.Create(); key.ImportFromPem(pem);
            if (key.KeySize != 256 || key.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7")
                throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable");
            var now = time.GetUtcNow(); using var payload = new MemoryStream();
            using (var writer = new Utf8JsonWriter(payload))
            {
                writer.WriteStartObject(); writer.WriteString("iss", issuer);
                writer.WriteNumber("iat", now.ToUnixTimeSeconds()); writer.WriteNumber("exp", now.AddMinutes(5).ToUnixTimeSeconds());
                writer.WriteString("aud", "appstoreconnect-v1"); writer.WriteString("bid", "pro.vafadar.zanance"); writer.WriteEndObject();
            }
            return JWT.EncodeBytes(payload.ToArray(), key, JwsAlgorithm.ES256,
                extraHeaders: new Dictionary<string, object> { ["kid"] = keyId, ["typ"] = "JWT" }, settings: AppleBillingSignatures.Settings);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or CryptographicException or ArgumentException or JoseException)
        { throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable"); }
        finally { if (bytes is not null) CryptographicOperations.ZeroMemory(bytes); if (pem is not null) Array.Clear(pem); }
    }
}
