using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization.Metadata;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Billing;

/// <summary>Owned billing HTTPS origin only; personal user-selected peers do not become paid issuers.</summary>
public sealed class BillingHttpTransport : IBillingServiceTransport, IDisposable
{
    private readonly HttpClient http;
    private readonly PersonalSyncTransport identity;
    private readonly bool useIdentity;
    /// <inheritdoc />
    public string PeerBinding { get; }
    /// <summary>Normal composition requires the fixed system-trusted origin; local qualification is an explicit independent build effect.</summary>
    public BillingHttpTransport(LocalSyncPeer peer, IPersonalSyncTokenStore tokens, Action validateAccess, bool qualification = false, bool useIdentity = true)
    {
        if (!peer.UsesSystemTrust && !qualification) throw new ArgumentException("Only the owned billing service can issue paid state.");
        this.useIdentity = useIdentity;
        PeerBinding = peer.BindingKey("billing-authority-v1");
        http = LocalSyncHttp.CreateClient(peer); http.Timeout = TimeSpan.FromSeconds(30);
        identity = new(peer, tokens, verifyAccess: validateAccess);
    }
    /// <inheritdoc />
    public Task<string?> CurrentUserAsync(CancellationToken ct) => useIdentity ? identity.BillingIdentityAsync(ct) : Task.FromResult<string?>(null);
    /// <inheritdoc />
    public Task<BillingTimeResult> TimeAsync(Guid nonce, CancellationToken ct) => SendAsync(HttpMethod.Get,
        "billing/time/" + nonce.ToString("D"), null, BillingJson.Default.BillingTimeResult, ct);
    /// <inheritdoc />
    public Task<BillingRegistrationResult> RegisterAsync(string? user, BillingRegistrationEnvelope input, CancellationToken ct)
        => user is null ? SendAsync(HttpMethod.Post, "billing/devices", JsonContent.Create(input, BillingJson.Default.BillingRegistrationEnvelope),
            BillingJson.Default.BillingRegistrationResult, ct) : identity.BillingRegisterAsync(user, input, ct);
    /// <inheritdoc />
    public Task<BillingCommandResult> CommandAsync(string? user, Guid customer, BillingSignedRequest input, CancellationToken ct)
        => user is null ? SendAsync(HttpMethod.Post, "billing/devices/" + customer.ToString("D") + "/commands",
            JsonContent.Create(input, BillingJson.Default.BillingSignedRequest), BillingJson.Default.BillingCommandResult, ct)
            : identity.BillingCommandAsync(user, customer, input, ct);

    /// <summary>Explicit current identity sign-in; the password is never retained by this transport or a billing slot.</summary>
    public async Task SignInIdentityAsync(string email, string password, CancellationToken ct)
    { await identity.SignInAsync(email, password, ct); }
    /// <summary>Actual current-session revocation and local credential retirement, without touching a purchase or another device.</summary>
    public Task SignOutIdentityAsync(CancellationToken ct) => identity.SignOutAsync(ct);
    /// <summary>Uniform registration response, never account-existence discovery or automatic sign-in.</summary>
    public Task RegisterIdentityAsync(string email, string password, CancellationToken ct)
        => IdentityRequestAsync("identity/register", new BillingIdentityInput(email, password), BillingIdentityJson.Default.BillingIdentityInput, ct);
    /// <summary>Uniform recovery request; the service alone decides whether a confirmed recipient receives instructions.</summary>
    public Task RecoverIdentityAsync(string email, CancellationToken ct)
        => IdentityRequestAsync("identity/recover", new BillingIdentityInput(email, ""), BillingIdentityJson.Default.BillingIdentityInput, ct);
    /// <summary>Explicit one-use challenge completion in the request body, never a logged URL.</summary>
    public Task CompleteIdentityAsync(Guid challenge, string token, string? password, CancellationToken ct)
        => IdentityRequestAsync("identity/complete", new BillingIdentityCompletion(challenge, token, password), BillingIdentityJson.Default.BillingIdentityCompletion, ct);
    private async Task IdentityRequestAsync<T>(string path, T input, JsonTypeInfo<T> json, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input, json) };
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) throw new PersonalSyncHttpException(response.StatusCode);
    }

    /// <summary>Bounded strict anonymous transport; raw receipts, keys, private response bodies and SDK messages are never logged.</summary>
    private async Task<T> SendAsync<T>(HttpMethod method, string path, HttpContent? content, JsonTypeInfo<T> type, CancellationToken ct) where T : class
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) throw new PersonalSyncHttpException(response.StatusCode);
        if (response.Content.Headers.ContentLength > 262144) throw new InvalidDataException("Billing response bound exceeded.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct); using var memory = new MemoryStream(); var buffer = new byte[8192];
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) != 0)
            {
                if (memory.Length + read > 262144) throw new InvalidDataException("Billing response bound exceeded.");
                await memory.WriteAsync(buffer.AsMemory(0, read), ct);
            }
            return StrictJsonPayload.Deserialize(memory.GetBuffer().AsMemory(0, checked((int)memory.Length)), type, 262144);
        }
        finally { CryptographicOperations.ZeroMemory(buffer); CryptographicOperations.ZeroMemory(memory.GetBuffer()); }
    }
    /// <inheritdoc />
    public void Dispose() { http.Dispose(); identity.Dispose(); }
}

/// <summary>Ephemeral native identity request only; credentials must not enter diagnostic record formatting.</summary>
internal sealed record BillingIdentityInput(string Email, string Password)
{
    /// <inheritdoc />
    public override string ToString() => "Unretained identity request";
}
/// <summary>One-use identity completion body, excluded from private purchase state and every portable preference.</summary>
internal sealed record BillingIdentityCompletion(Guid Id, string Token, string? Password)
{
    /// <inheritdoc />
    public override string ToString() => "Unretained identity completion";
}
/// <summary>AOT-safe credentials submitted only to fixed owned identity endpoints.</summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(BillingIdentityInput))]
[System.Text.Json.Serialization.JsonSerializable(typeof(BillingIdentityCompletion))]
internal partial class BillingIdentityJson : System.Text.Json.Serialization.JsonSerializerContext;
