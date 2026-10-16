using Google.Apis.Auth.OAuth2;
using System.Text.Json;
using Vafadar.Cryptography;
using static Vafadar.Zanance.Api.Billing.BillingProviderFields;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Scoped server credential port; native apps and tests never receive operator credentials.</summary>
internal interface IGoogleBillingCredentials
{
    /// <summary>Gets one short-lived official API bearer without logging it.</summary>
    Task<string> AccessAsync(CancellationToken ct);
}

/// <summary>Loads only an explicitly mounted private service account; no ambient machine accounts or interactive flow.</summary>
internal sealed class GoogleBillingCredentials(IConfiguration configuration, ServiceStorage storage) : IGoogleBillingCredentials, IDisposable
{
    private readonly SemaphoreSlim _credentialGate = new(1);
    private GoogleCredential? _credential;
    /// <summary>Uses only this service's explicitly mounted credential, never machine-wide accounts or interactive sign-in.</summary>
    public async Task<string> AccessAsync(CancellationToken ct)
    {
        await _credentialGate.WaitAsync(ct);
        try
        {
            if (_credential is null)
            {
                var configured = configuration["Billing:Google:ServiceAccountFile"];
                var privateDirectory = Path.GetFullPath(Path.Combine(storage.Root, "billing-credentials"));
                if (string.IsNullOrEmpty(configured) || !Path.IsPathFullyQualified(configured))
                    throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable");
                var path = Path.GetFullPath(configured);
                if (Path.GetDirectoryName(path) != privateDirectory || !File.Exists(path) || new FileInfo(path).Length is < 1 or > 65536 ||
                    (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 ||
                    (File.GetAttributes(privateDirectory) & FileAttributes.ReparsePoint) != 0)
                    throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable");
                // Inspect only fixed endpoint/type metadata before the maintained parser consumes operator-owned secrets.
                var bytes = await File.ReadAllBytesAsync(path, ct);
                try
                {
                    using var metadata = StrictJsonPayload.Deserialize(bytes, BillingProviderJson.Default.JsonDocument, 65536);
                    if (Text(metadata.RootElement, "type") != "service_account" ||
                        Text(metadata.RootElement, "token_uri") != "https://oauth2.googleapis.com/token")
                        throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable");
                    using var original = new MemoryStream(bytes, writable: false);
                    var credential = await CredentialFactory.FromStreamAsync<ServiceAccountCredential>(original, ct);
                    _credential = GoogleCredential.FromServiceAccountCredential(credential).CreateScoped("https://www.googleapis.com/auth/androidpublisher");
                }
                finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
            }
            return await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync(null, ct);
        }
        catch (Exception error) when (error is IOException or JsonException or InvalidOperationException or HttpRequestException or UnauthorizedAccessException
            or ArgumentException or System.Security.Cryptography.CryptographicException or Google.Apis.Auth.OAuth2.Responses.TokenResponseException)
        { throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable"); }
        finally { _credentialGate.Release(); }
    }

    /// <inheritdoc />
    public void Dispose() => _credentialGate.Dispose();
}
