using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vafadar.Authentication.OAuth;

namespace Vafadar.Authentication.Google;

/// <summary>
/// Google sign-in for installed apps without Google Play services (iOS and the Windows desktop): the OAuth 2.0
/// authorization-code flow with PKCE through the system browser, as described in Google's "OAuth 2.0 for iOS &amp;
/// Desktop Apps". No client secret is used: iOS clients have none, and for desktop clients it is optional with PKCE.
/// </summary>
/// <remarks>
/// <para>
/// The refresh token is kept in the platform's protected storage (<see cref="IProtectedValueStore"/>: the keychain on
/// iOS, DPAPI on Windows) together with the account e-mail and the granted scopes; access tokens live in memory only.
/// Signing out removes the stored token and revokes the grant at Google, so the next sign-in asks again.
/// </para>
/// <para>
/// Only the caller's scopes (for backups: <c>drive.appdata</c>) plus <c>openid</c> and <c>email</c> are requested. Google
/// lets the user untick single scopes on the consent page; a sign-in without the caller's scopes is refused and its
/// grant revoked, because the backup could not work with it.
/// </para>
/// </remarks>
public sealed class GoogleInstalledAppSignInService : IExternalSignInService
{
    /// <summary>Google's authorization endpoint.</summary>
    public const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

    /// <summary>Google's token endpoint (code exchange and refresh).</summary>
    public const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    /// <summary>Google's revocation endpoint.</summary>
    public const string RevocationEndpoint = "https://oauth2.googleapis.com/revoke";

    // An access token is renewed this long before it expires, so a running upload does not lose it.
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(1);

    private readonly string _clientId;
    private readonly IAuthorizationBrowser _browser;
    private readonly IProtectedValueStore _store;
    private readonly HttpClient _http;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpires;

    /// <summary>Creates the service for a Google OAuth client of type iOS or Desktop app.</summary>
    /// <param name="clientId">The OAuth client id (<c>….apps.googleusercontent.com</c>).</param>
    /// <param name="browser">Shows Google's sign-in page and returns the redirect.</param>
    /// <param name="store">Keeps the refresh token in the platform's protected storage.</param>
    /// <param name="http">The HTTP client for Google's token endpoints; a new one when omitted.</param>
    /// <param name="time">The clock for token lifetimes; the system clock when omitted.</param>
    public GoogleInstalledAppSignInService(string clientId, IAuthorizationBrowser browser, IProtectedValueStore store, HttpClient? http = null, TimeProvider? time = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        _clientId = clientId;
        _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _http = http ?? new HttpClient();
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Gets the scopes that are always requested besides the caller's: they name the account.</summary>
    public static IReadOnlyList<string> IdentityScopes { get; } = ["openid", "email"];

    /// <inheritdoc />
    public ExternalIdentityProvider Provider => ExternalIdentityProvider.Google;

    /// <inheritdoc />
    public async ValueTask<ExternalAccount?> GetCurrentAccountAsync(CancellationToken cancellationToken = default)
    {
        var state = await ReadStateAsync(cancellationToken);
        return state is null ? null : ToAccount(state);
    }

    /// <inheritdoc />
    /// <exception cref="AuthenticationRequiredException">Nobody is signed in, or the grant was revoked or expired.</exception>
    /// <exception cref="HttpRequestException">Google could not be reached.</exception>
    public async ValueTask<string> GetAccessTokenAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = await ReadStateAsync(cancellationToken) ?? throw new AuthenticationRequiredException(Provider);
            if (!Covers(state.Scopes, scopes))
            {
                // More access is needed than the user granted: only a new consent can give it.
                throw new AuthenticationRequiredException(Provider);
            }

            if (_accessToken is not null && _time.GetUtcNow() < _accessTokenExpires - ExpiryMargin)
            {
                return _accessToken;
            }

            var tokens = await RequestTokensAsync(
                [new("grant_type", "refresh_token"), new("refresh_token", state.RefreshToken), new("client_id", _clientId)],
                refreshing: true,
                cancellationToken);

            // Google may rotate the refresh token or report the scopes again; both are kept up to date.
            var granted = tokens.Scope is null ? state.Scopes : SplitScopes(tokens.Scope);
            if (tokens.RefreshToken is not null || !granted.SequenceEqual(state.Scopes))
            {
                await WriteStateAsync(state with { RefreshToken = tokens.RefreshToken ?? state.RefreshToken, Scopes = granted }, cancellationToken);
            }

            Remember(tokens);
            return tokens.AccessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">The user closed the browser or declined.</exception>
    /// <exception cref="InvalidOperationException">Google refused the sign-in or the requested access was not granted.</exception>
    /// <exception cref="HttpRequestException">Google could not be reached.</exception>
    public async ValueTask<ExternalAccount> SignInAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        var requested = scopes.Concat(IdentityScopes).Distinct(StringComparer.Ordinal).ToList();
        var pkce = PkceCodes.Create();
        var expectedState = PkceCodes.NewState();

        IReadOnlyDictionary<string, string> answer;
        Uri redirectUri;
        await using (var session = await _browser.StartAsync(cancellationToken))
        {
            redirectUri = session.RedirectUri;
            answer = await session.AuthorizeAsync(BuildAuthorizationUri(redirectUri, requested, pkce.Challenge, expectedState), cancellationToken);
        }

        if (answer.TryGetValue("error", out var error))
        {
            throw error == "access_denied"
                ? new OperationCanceledException("The sign-in was cancelled.", cancellationToken)
                : new InvalidOperationException($"Google sign-in failed ({error}).");
        }

        // The state proves that the answer belongs to this request (RFC 6749 §10.12).
        if (!answer.TryGetValue("state", out var state) || !FixedTimeEquals(state, expectedState))
        {
            throw new InvalidOperationException("The sign-in answer does not belong to this sign-in.");
        }

        if (!answer.TryGetValue("code", out var code) || code.Length == 0)
        {
            throw new InvalidOperationException("Google returned no authorization code.");
        }

        var tokens = await RequestTokensAsync(
            [
                new("grant_type", "authorization_code"),
                new("code", code),
                new("code_verifier", pkce.Verifier),
                new("redirect_uri", redirectUri.ToString()),
                new("client_id", _clientId),
            ],
            refreshing: false,
            cancellationToken);

        if (tokens.RefreshToken is null)
        {
            throw new InvalidOperationException("Google returned no refresh token.");
        }

        var granted = tokens.Scope is null ? requested : SplitScopes(tokens.Scope);
        if (!Covers(granted, scopes))
        {
            // The user unticked the app's folder on the consent page: the backup cannot work, so nothing is kept.
            await RevokeQuietlyAsync(tokens.RefreshToken, cancellationToken);
            throw new InvalidOperationException("The access needed for the backup was not granted.");
        }

        var (subject, email) = ReadIdToken(tokens.IdToken);
        var saved = new GoogleTokenState(tokens.RefreshToken, subject, email, granted);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WriteStateAsync(saved, cancellationToken);
            Remember(tokens);
        }
        finally
        {
            _gate.Release();
        }

        return ToAccount(saved);
    }

    /// <inheritdoc />
    public async ValueTask SignOutAsync(CancellationToken cancellationToken = default)
    {
        GoogleTokenState? state;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            state = await ReadStateAsync(cancellationToken);
            _accessToken = null;
            await _store.ClearAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        // The account is signed out on this device even when the revocation cannot reach Google (offline).
        if (state is not null)
        {
            await RevokeQuietlyAsync(state.RefreshToken, cancellationToken);
        }
    }

    /// <summary>Builds the authorization request (exposed for tests and diagnostics).</summary>
    public Uri BuildAuthorizationUri(Uri redirectUri, IEnumerable<string> scopes, string codeChallenge, string state)
    {
        ArgumentNullException.ThrowIfNull(redirectUri);
        ArgumentNullException.ThrowIfNull(scopes);
        var query = new (string Name, string Value)[]
        {
            ("client_id", _clientId),
            ("redirect_uri", redirectUri.ToString()),
            ("response_type", "code"),
            ("scope", string.Join(' ', scopes)),
            ("code_challenge", codeChallenge),
            ("code_challenge_method", PkceCodes.Method),
            ("state", state),
            ("prompt", "select_account"),
        };
        return new Uri(AuthorizationEndpoint + "?" + string.Join('&', query.Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value)}")));
    }

    private static ExternalAccount ToAccount(GoogleTokenState state)
    {
        var email = state.Email ?? "Google";
        return new ExternalAccount(ExternalIdentityProvider.Google, state.Subject ?? email, email, null);
    }

    private static bool Covers(IReadOnlyCollection<string> granted, IEnumerable<string> needed) =>
        needed.All(scope => granted.Contains(scope, StringComparer.Ordinal));

    private static IReadOnlyList<string> SplitScopes(string scopes) =>
        scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    // The ID token comes straight from Google's token endpoint over TLS, so its claims are read without checking the
    // signature (OpenID Connect Core §3.1.3.7); it only names the account, it never grants anything.
    internal static (string? Subject, string? Email) ReadIdToken(string? idToken)
    {
        var parts = idToken?.Split('.');
        if (parts is not { Length: 3 })
        {
            return (null, null);
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
            using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
            var root = json.RootElement;
            return (
                root.TryGetProperty("sub", out var sub) ? sub.GetString() : null,
                root.TryGetProperty("email", out var email) ? email.GetString() : null);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
        {
            return (null, null);
        }
    }

    private void Remember(TokenResponse tokens)
    {
        _accessToken = tokens.AccessToken;
        _accessTokenExpires = _time.GetUtcNow().AddSeconds(Math.Max(0, tokens.ExpiresIn));
    }

    private async Task<TokenResponse> RequestTokensAsync(IEnumerable<KeyValuePair<string, string>> form, bool refreshing, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync(new Uri(TokenEndpoint), content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = ErrorCode(body);
            if (refreshing && error == "invalid_grant")
            {
                // Revoked, expired (unused for six months, or the password changed) or removed by the user at Google.
                await _store.ClearAsync(cancellationToken);
                _accessToken = null;
                throw new AuthenticationRequiredException(Provider);
            }

            if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new HttpRequestException($"Google sign-in is not available right now ({(int)response.StatusCode}).", null, response.StatusCode);
            }

            throw new InvalidOperationException($"Google refused the token request ({error ?? ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture)}).");
        }

        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var accessToken = root.GetProperty("access_token").GetString();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Google returned no access token.");
            }

            return new TokenResponse(
                accessToken,
                root.TryGetProperty("expires_in", out var expires) && expires.TryGetInt64(out var seconds) ? seconds : 3600,
                root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null,
                root.TryGetProperty("scope", out var scope) ? scope.GetString() : null,
                root.TryGetProperty("id_token", out var id) ? id.GetString() : null);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException)
        {
            throw new InvalidOperationException("Google returned an unreadable token response.", ex);
        }
    }

    private static string? ErrorCode(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String ? error.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task RevokeQuietlyAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("token", token)]);
            using var response = await _http.PostAsync(new Uri(RevocationEndpoint), content, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"Google revoke failed: {ex.GetType().Name}");
        }
    }

    private async Task<GoogleTokenState?> ReadStateAsync(CancellationToken cancellationToken)
    {
        var json = await _store.ReadAsync(cancellationToken);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var state = JsonSerializer.Deserialize(json, GoogleAuthJsonContext.Default.GoogleTokenState);
            if (state is { RefreshToken.Length: > 0 })
            {
                return state;
            }
        }
        catch (JsonException)
        {
        }

        // A damaged entry means "not signed in", never an error on every start.
        await _store.ClearAsync(cancellationToken);
        return null;
    }

    private ValueTask WriteStateAsync(GoogleTokenState state, CancellationToken cancellationToken) =>
        _store.WriteAsync(JsonSerializer.Serialize(state, GoogleAuthJsonContext.Default.GoogleTokenState), cancellationToken);

    private sealed record TokenResponse(string AccessToken, long ExpiresIn, string? RefreshToken, string? Scope, string? IdToken);
}
