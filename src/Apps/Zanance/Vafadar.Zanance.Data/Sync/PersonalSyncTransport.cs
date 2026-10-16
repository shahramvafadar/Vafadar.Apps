using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Platform secure storage for opaque sessions only; passwords never pass through this port.</summary>
public interface IPersonalSyncTokenStore
{
    /// <summary>Reads only the credentials bound to the reviewed endpoint and certificate.</summary>
    Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct);
    /// <summary>Durably replaces the rotating session before another request can use it.</summary>
    Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct);
    /// <summary>Removes the explicitly disconnected or refused session.</summary>
    Task RemoveAsync(string peer, CancellationToken ct);
}

/// <summary>Opaque maintained-framework session; default diagnostics deliberately contain no property values.</summary>
public sealed class PersonalSyncSession
{
    /// <summary>Maintained-framework access credential, confined to secure storage and request headers.</summary>
    public string AccessToken { get; init; } = "";
    /// <summary>One-use maintained-framework renewal credential.</summary>
    public string RefreshToken { get; init; } = "";
    /// <summary>Absolute renewal boundary from the injected clock.</summary>
    public DateTimeOffset ExpiresAt { get; init; }
    /// <inheritdoc />
    public override string ToString() => nameof(PersonalSyncSession);
}

/// <summary>A content-free service refusal suitable for translated UI classification.</summary>
public sealed class PersonalSyncHttpException(HttpStatusCode status) : Exception("The personal sync request was refused.")
{
    /// <summary>HTTP status only; response bodies, financial payloads and credentials are excluded.</summary>
    public HttpStatusCode Status { get; } = status;
}

/// <summary>Authenticated pinned local transport; no database ownership is held over its network requests.</summary>
public sealed class PersonalSyncTransport : IProfileSynchronizationTransport, IDisposable
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> PeerRequests = new(StringComparer.Ordinal);
    private readonly HttpClient http;
    private readonly IPersonalSyncTokenStore tokens;
    private readonly TimeProvider time;
    private readonly SemaphoreSlim requests;
    private readonly string credentialKey;
    private readonly Action? verifyAccess;
    private const int MaximumResponseBytes = 3 * 1024 * 1024;
    /// <summary>The immutable public endpoint and certificate identity reviewed by the owner.</summary>
    public LocalSyncPeer Peer { get; }
    /// <inheritdoc />
    public ProfileSynchronizationKind Kind => ProfileSynchronizationKind.Personal;

    /// <summary>Creates a per-peer pinned connection without reading credentials or making a request.</summary>
    public PersonalSyncTransport(LocalSyncPeer peer, IPersonalSyncTokenStore tokens, TimeProvider? time = null, Action? verifyAccess = null)
    {
        this.verifyAccess = verifyAccess;
        Peer = peer; this.tokens = tokens; this.time = time ?? TimeProvider.System;
        http = LocalSyncHttp.CreateClient(peer, this.time); credentialKey = peer.BindingKey("session");
        // Multiple profile windows share the same rotating framework session, including sign-out.
        requests = PeerRequests.GetOrAdd(credentialKey, static _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>Signs in explicitly; the password lives only in this request and is never remembered.</summary>
    public async Task<SyncIdentity> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        if (email is not { Length: > 3 and <= 254 } || password is not { Length: >= 12 and <= 256 })
            throw new ArgumentException("A bounded identity sign-in is required.");
        await requests.WaitAsync(ct);
        try
        {
            var session = await GrantAsync("identity/login", new SyncSignInBody { Email = email, Password = password },
                SyncCredentialJsonContext.Default.SyncSignInBody, ct);
            // Do not persist a credential for an endpoint that cannot resolve its actual authenticated owner.
            var identity = await SendCoreAsync(HttpMethod.Get, "sync/identity", null, PersonalSyncJsonContext.Default.SyncIdentity, session, ct);
            await tokens.WriteAsync(credentialKey, session, ct); return identity;
        }
        finally { requests.Release(); }
    }

    /// <summary>Forgets only this reviewed peer's credentials; callers pause the local profile separately.</summary>
    public async Task ForgetAsync(CancellationToken ct = default)
    {
        await requests.WaitAsync(ct);
        try { await tokens.RemoveAsync(credentialKey, ct); }
        finally { requests.Release(); }
    }

    /// <summary>Reads the actual authenticated owner rather than interpreting an opaque bearer token.</summary>
    public Task<SyncIdentity> IdentityAsync(CancellationToken ct = default) => GetAsync("sync/identity", PersonalSyncJsonContext.Default.SyncIdentity, ct);
    /// <summary>Lists the owner's visible personal spaces.</summary>
    public Task<SyncProfile[]> ProfilesAsync(Guid device, CancellationToken ct = default) =>
        GetAsync("sync/profiles?deviceId=" + device.ToString("D"), PersonalSyncJsonContext.Default.SyncProfileArray, ct);
    /// <summary>Transfers only an explicitly reviewed unpublished initialization to this actual session device.</summary>
    public Task<SyncProfile> ClaimInitializationAsync(Guid profile, SyncClaimInitialization input, CancellationToken ct = default) =>
        JsonAsync(HttpMethod.Post, Space(profile) + "/initialization", input, PersonalSyncJsonContext.Default.SyncClaimInitialization, PersonalSyncJsonContext.Default.SyncProfile, ct);
    /// <summary>Lists only owner-authorized devices.</summary>
    public Task<SyncDevice[]> DevicesAsync(CancellationToken ct = default) => GetAsync("sync/devices", PersonalSyncJsonContext.Default.SyncDeviceArray, ct);
    /// <summary>Resolves the retained actual session's device without silently changing its owner-visible name.</summary>
    public Task<SyncDevice> CurrentDeviceAsync(CancellationToken ct = default) => GetAsync("sync/current-device", PersonalSyncJsonContext.Default.SyncDevice, ct);
    /// <summary>Registers or updates this actual maintained identity session's single revocable device.</summary>
    public Task<SyncDevice> RegisterDeviceAsync(string name, CancellationToken ct = default) =>
        JsonAsync(HttpMethod.Post, "sync/devices", new SyncNameInput(name), PersonalSyncJsonContext.Default.SyncNameInput, PersonalSyncJsonContext.Default.SyncDevice, ct);
    /// <summary>Creates one idempotent private space from an explicit owner action.</summary>
    public Task<SyncProfile> CreateProfileAsync(SyncCreateProfile input, CancellationToken ct = default) =>
        JsonAsync(HttpMethod.Post, "sync/profiles", input, PersonalSyncJsonContext.Default.SyncCreateProfile, PersonalSyncJsonContext.Default.SyncProfile, ct);
    /// <summary>Revokes the selected device and its maintained session.</summary>
    public Task RevokeDeviceAsync(Guid device, CancellationToken ct = default) => EmptyAsync(HttpMethod.Delete, "sync/devices/" + device.ToString("D"), null, ct);
    /// <summary>Opens or observes the same immutable operation after interruption.</summary>
    public Task<SyncRoundHandle> OpenAsync(Guid profile, SyncOpenRound input, CancellationToken ct = default) =>
        JsonAsync(HttpMethod.Post, Space(profile) + "/rounds", input, PersonalSyncJsonContext.Default.SyncOpenRound, PersonalSyncJsonContext.Default.SyncRoundHandle, ct);
    /// <summary>Uploads one immutable proposal part.</summary>
    public Task PutPartAsync(Guid profile, Guid device, Guid round, SyncRoundPart part, CancellationToken ct = default) =>
        EmptyAsync(HttpMethod.Put, Round(profile, round) + "/parts?deviceId=" + device.ToString("D"), JsonContent.Create(part, PersonalSyncJsonContext.Default.SyncRoundPart), ct);
    /// <summary>Requests atomic publication; a conflict outcome publishes none of the round.</summary>
    public Task<SyncCommitResult> CommitAsync(Guid profile, Guid device, Guid round, SyncCommitRound input, CancellationToken ct = default) =>
        JsonAsync(HttpMethod.Post, Round(profile, round) + "/commit?deviceId=" + device.ToString("D"), input,
            PersonalSyncJsonContext.Default.SyncCommitRound, PersonalSyncJsonContext.Default.SyncCommitResult, ct);
    /// <summary>Abandons only an uncommitted operation, retaining its nonce against delayed replay.</summary>
    public Task AbandonAsync(Guid profile, Guid device, Guid round, CancellationToken ct = default) =>
        EmptyAsync(HttpMethod.Delete, Round(profile, round) + "?deviceId=" + device.ToString("D"), null, ct);
    /// <summary>Reads a bounded immutable page through the original captured cursor.</summary>
    public Task<SyncFeedPage> FeedAsync(Guid profile, Guid device, long after, long through, CancellationToken ct = default) =>
        GetAsync(FormattableString.Invariant($"{Space(profile)}/feed?deviceId={device:D}&after={after}&through={through}"), PersonalSyncJsonContext.Default.SyncFeedPage, ct);
    /// <summary>Declares exact immutable attachment content before sending pieces.</summary>
    public Task<SyncBlobStatus> DeclareBlobAsync(Guid profile, Guid device, string hash, long length, CancellationToken ct = default) =>
        JsonAsync(HttpMethod.Post, Blob(profile, device, hash), new SyncBlobDeclaration(length), PersonalSyncJsonContext.Default.SyncBlobDeclaration,
            PersonalSyncJsonContext.Default.SyncBlobStatus, ct);
    /// <summary>Uploads one bounded piece; credentials never enter its address or content.</summary>
    public Task PutBlobAsync(Guid profile, Guid device, string hash, int index, byte[] bytes, CancellationToken ct = default)
    {
        if (bytes.Length > 2 * 1024 * 1024 || index < 0) throw new ArgumentException("A bounded attachment piece is required.");
        var content = new ByteArrayContent(bytes); content.Headers.ContentType = new("application/octet-stream");
        return EmptyAsync(HttpMethod.Put, Blob(profile, device, hash, "/parts/" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)), content, ct);
    }
    /// <summary>Requests actual revalidation of the complete retained attachment.</summary>
    public Task<SyncBlob> CompleteBlobAsync(Guid profile, Guid device, string hash, CancellationToken ct = default) =>
        GetOrPostAsync(HttpMethod.Post, Blob(profile, device, hash, "/complete"), PersonalSyncJsonContext.Default.SyncBlob, ct);
    /// <summary>Reads only one completed, reverified bounded attachment piece.</summary>
    public async Task<byte[]> ReadBlobAsync(Guid profile, Guid device, string hash, int index, CancellationToken ct = default)
    {
        await requests.WaitAsync(ct);
        try
        {
            var session = await SessionAsync(ct);
            using var request = Request(HttpMethod.Get, Blob(profile, device, hash, "/parts/" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)), null, session);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            Demand(response); verifyAccess?.Invoke();
            var bytes = await ReadBoundedAsync(response.Content, 2 * 1024 * 1024, ct);
            try { ct.ThrowIfCancellationRequested(); verifyAccess?.Invoke(); return bytes; }
            catch { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); throw; }
        }
        finally { requests.Release(); }
    }

    private Task<T> GetAsync<T>(string path, JsonTypeInfo<T> type, CancellationToken ct) => GetOrPostAsync(HttpMethod.Get, path, type, ct);

    /// <summary>Reuses actual pinned session renewal/serialization for bounded public household metadata and ciphertext only.</summary>
    internal Task<T> HouseholdGetAsync<T>(string path, JsonTypeInfo<T> type, CancellationToken ct)
    { RequireHouseholdPath(path); return GetAsync(path, type, ct); }

    /// <summary>No financial-row/blob/plain-key DTO can enter this separate household metadata transport lane.</summary>
    internal Task<TResponse> HouseholdPostAsync<TBody, TResponse>(string path, TBody body, JsonTypeInfo<TBody> bodyType,
        JsonTypeInfo<TResponse> responseType, CancellationToken ct)
    {
        RequireHouseholdPath(path); RequireHouseholdBody<TBody>();
        return JsonAsync(HttpMethod.Post, path, body, bodyType, responseType, ct);
    }

    /// <summary>Uploads only the fixed opaque-part contract through the shared original authenticated transport.</summary>
    internal Task HouseholdPutAsync(string path, HouseholdOpaquePart part, CancellationToken ct)
    {
        RequireHouseholdPath(path);
        return EmptyAsync(HttpMethod.Put, path, JsonContent.Create(part, HouseholdMetadataJsonContext.Default.HouseholdOpaquePart), ct);
    }

    /// <summary>Bodyless explicit household operations cannot carry a financial proposal or private key.</summary>
    internal Task HouseholdEmptyAsync(HttpMethod method, string path, CancellationToken ct)
    { RequireHouseholdPath(path); return EmptyAsync(method, path, null, ct); }

    /// <summary>Routes consist only of internal fixed segments and validated opaque ids; redirects/absolute addresses cannot reuse the session.</summary>
    private static void RequireHouseholdPath(string path)
    {
        if (!path.StartsWith("households/", StringComparison.Ordinal) || path.Contains("..", StringComparison.Ordinal) ||
            path.Contains("://", StringComparison.Ordinal) || path.Any(character => character is '\\' or '#' or '%' or '\r' or '\n'))
            throw new ArgumentException("An internal household route is required.", nameof(path));
    }

    /// <summary>Explicit wire allowlist: generic JSON reuse cannot accidentally route original readable financial payloads into household service storage.</summary>
    private static void RequireHouseholdBody<T>()
    {
        var type = typeof(T);
        if (type != typeof(HouseholdReserveSpace) && type != typeof(HouseholdPublishMembership) && type != typeof(HouseholdCreateInvitation) &&
            type != typeof(HouseholdClaimInvitation) && type != typeof(HouseholdEnrollDevice) && type != typeof(HouseholdRebindDevice) &&
            type != typeof(HouseholdOpenRound) && type != typeof(HouseholdCommitRound) && type != typeof(HouseholdUploadParts) &&
            type != typeof(HouseholdOpenRecoverySeed) && type != typeof(HouseholdUploadRecoveryEpochs) && type != typeof(HouseholdRecoverOwner) &&
            type != typeof(HouseholdOpenOwnership) && type != typeof(HouseholdPrepareOwnership) && type != typeof(HouseholdAuthorizeOwnership) &&
            type != typeof(HouseholdAcceptOwnership) && type != typeof(HouseholdObserveOwnership) && type != typeof(HouseholdCancelOwnership) && type != typeof(HouseholdDeleteRequest))
            throw new InvalidOperationException("Only explicit public household metadata or protected content may use this transport.");
    }
    private async Task<T> GetOrPostAsync<T>(HttpMethod method, string path, JsonTypeInfo<T> type, CancellationToken ct)
    {
        await requests.WaitAsync(ct);
        try { return await SendCoreAsync(method, path, null, type, await SessionAsync(ct), ct); }
        finally { requests.Release(); }
    }
    private async Task<TResponse> JsonAsync<TBody, TResponse>(HttpMethod method, string path, TBody body,
        JsonTypeInfo<TBody> bodyType, JsonTypeInfo<TResponse> responseType, CancellationToken ct)
    {
        await requests.WaitAsync(ct);
        try { return await SendCoreAsync(method, path, JsonContent.Create(body, bodyType), responseType, await SessionAsync(ct), ct); }
        finally { requests.Release(); }
    }
    private async Task EmptyAsync(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        using (content)
        {
            await requests.WaitAsync(ct);
            try
            {
                using var request = Request(method, path, content, await SessionAsync(ct));
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                Demand(response); ct.ThrowIfCancellationRequested(); verifyAccess?.Invoke();
            }
            finally { requests.Release(); }
        }
    }
    private async Task<PersonalSyncSession> SessionAsync(CancellationToken ct)
    {
        verifyAccess?.Invoke();
        var session = await tokens.ReadAsync(credentialKey, ct) ?? throw new PersonalSyncHttpException(HttpStatusCode.Unauthorized);
        if (session.ExpiresAt > time.GetUtcNow().AddSeconds(30)) return session;
        try
        {
            var renewed = await GrantAsync("identity/refresh", new SyncRefreshBody { RefreshToken = session.RefreshToken }, SyncCredentialJsonContext.Default.SyncRefreshBody, ct);
            await tokens.WriteAsync(credentialKey, renewed, ct); return renewed;
        }
        catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Unauthorized)
        { await tokens.RemoveAsync(credentialKey, ct); throw; }
    }
    private async Task<PersonalSyncSession> GrantAsync<T>(string path, T body, JsonTypeInfo<T> type, CancellationToken ct)
    {
        var grant = await SendCoreAsync(HttpMethod.Post, path, JsonContent.Create(body, type), SyncCredentialJsonContext.Default.SyncTokenGrant, null, ct);
        if (grant.TokenType != "Bearer" || grant.AccessToken is not { Length: >= 1 and <= 8192 } || grant.RefreshToken is not { Length: >= 1 and <= 8192 } ||
            grant.ExpiresIn is < 1 or > 86400) throw new InvalidDataException("The personal session response is invalid.");
        return new() { AccessToken = grant.AccessToken, RefreshToken = grant.RefreshToken, ExpiresAt = time.GetUtcNow().AddSeconds(grant.ExpiresIn) };
    }
    private async Task<T> SendCoreAsync<T>(HttpMethod method, string path, HttpContent? content, JsonTypeInfo<T> type, PersonalSyncSession? session, CancellationToken ct)
    {
        verifyAccess?.Invoke();
        using var request = Request(method, path, content, session);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        Demand(response); verifyAccess?.Invoke(); var bytes = await ReadBoundedAsync(response.Content, MaximumResponseBytes, ct);
        try { verifyAccess?.Invoke(); return JsonSerializer.Deserialize(bytes, type) ?? throw new InvalidDataException("The personal sync response is empty."); }
        finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
    }
    private static HttpRequestMessage Request(HttpMethod method, string path, HttpContent? content, PersonalSyncSession? session)
    {
        // Every address is assembled from internal relative route segments and validated identifiers only.
        var request = new HttpRequestMessage(method, path) { Content = content };
        if (session is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return request;
    }
    private static void Demand(HttpResponseMessage response)
    { if (!response.IsSuccessStatusCode) throw new PersonalSyncHttpException(response.StatusCode); }
    private static async Task<byte[]> ReadBoundedAsync(HttpContent content, int maximum, CancellationToken ct)
    {
        if (content.Headers.ContentLength > maximum) throw new InvalidDataException("The personal sync response exceeds its bound.");
        await using var stream = await content.ReadAsStreamAsync(ct); using var result = new MemoryStream(); var buffer = new byte[81920];
        try
        {
            int count;
            while ((count = await stream.ReadAsync(buffer, ct)) != 0)
            {
                if (result.Length + count > maximum) throw new InvalidDataException("The personal sync response exceeds its bound.");
                result.Write(buffer, 0, count);
            }
            return result.ToArray();
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(buffer);
            if (result.TryGetBuffer(out var storage)) System.Security.Cryptography.CryptographicOperations.ZeroMemory(storage.AsSpan());
        }
    }
    private static string Space(Guid profile) => "sync/profiles/" + profile.ToString("D");
    private static string Round(Guid profile, Guid round) => Space(profile) + "/rounds/" + round.ToString("D");
    private static string Blob(Guid profile, Guid device, string hash, string suffix = "")
    {
        if (!SyncProtocolDigest.IsDigest(hash)) throw new ArgumentException("A content address is required.");
        return Space(profile) + "/blobs/" + hash + suffix + "?deviceId=" + device.ToString("D");
    }
    /// <inheritdoc />
    public void Dispose() => http.Dispose();
}

/// <summary>Ephemeral request only; never a persisted or diagnostic representation.</summary>
internal sealed class SyncSignInBody
{
    /// <summary>Explicit sign-in address.</summary>
    public string Email { get; init; } = "";
    /// <summary>Unretained request password.</summary>
    public string Password { get; init; } = "";
}
/// <summary>One-use renewal request only.</summary>
internal sealed class SyncRefreshBody
{
    /// <summary>Maintained-framework renewal credential.</summary>
    public string RefreshToken { get; init; } = "";
}
/// <summary>Bounded maintained-framework response without diagnostic property printing.</summary>
internal sealed class SyncTokenGrant
{
    /// <summary>Required maintained bearer scheme.</summary>
    public string TokenType { get; init; } = "";
    /// <summary>Opaque short-lived credential.</summary>
    public string AccessToken { get; init; } = "";
    /// <summary>Opaque one-use renewal credential.</summary>
    public string RefreshToken { get; init; } = "";
    /// <summary>Relative validity in seconds.</summary>
    public long ExpiresIn { get; init; }
}
/// <summary>AOT-safe credential serialization; credentials never use financial metadata serialization.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SyncSignInBody))]
[JsonSerializable(typeof(SyncRefreshBody))]
[JsonSerializable(typeof(SyncTokenGrant))]
internal partial class SyncCredentialJsonContext : JsonSerializerContext { }

/// <summary>AOT-safe session serialization for platform secure storage only, never portable preferences.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PersonalSyncSession))]
public partial class PersonalSyncSessionJsonContext : JsonSerializerContext { }
