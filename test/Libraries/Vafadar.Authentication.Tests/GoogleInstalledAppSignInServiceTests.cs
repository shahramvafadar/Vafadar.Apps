using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Vafadar.Authentication.Google;
using Vafadar.Authentication.OAuth;
using Vafadar.Testing;

namespace Vafadar.Authentication.Tests;

public sealed class GoogleInstalledAppSignInServiceTests
{
    private const string ClientId = "123-desktop.apps.googleusercontent.com";
    private const string DriveScope = "https://www.googleapis.com/auth/drive.appdata";
    private static readonly string[] Scopes = [DriveScope];

    private readonly FakeHttpMessageHandler _http = new();
    private readonly FakeBrowser _browser = new();
    private readonly MemoryStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    private readonly GoogleInstalledAppSignInService _service;

    public GoogleInstalledAppSignInServiceTests()
    {
        _service = new GoogleInstalledAppSignInService(ClientId, _browser, _store, new HttpClient(_http), _time);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sign_in_uses_pkce_without_a_client_secret_and_keeps_the_refresh_token_protected()
    {
        _http.RespondJson(TokenJson(refreshToken: "refresh-1", scope: $"{DriveScope} openid https://www.googleapis.com/auth/userinfo.email"));

        var account = await _service.SignInAsync(Scopes, Ct);

        Assert.Equal(new ExternalAccount(ExternalIdentityProvider.Google, "sub-42", "user@example.com", null), account);

        // The authorization request: own client, PKCE S256, state, the app folder plus the identity scopes only.
        var query = Query(_browser.AuthorizationUri!);
        Assert.Equal(GoogleInstalledAppSignInService.AuthorizationEndpoint, _browser.AuthorizationUri!.GetLeftPart(UriPartial.Path));
        Assert.Equal(ClientId, query["client_id"]);
        Assert.Equal(_browser.RedirectUri.ToString(), query["redirect_uri"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal($"{DriveScope} openid email", query["scope"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.False(query.ContainsKey("client_secret"));

        // The token request proves the verifier that belongs to the challenge and sends no secret.
        var form = Form(_http.Requests.Single());
        Assert.Equal(GoogleInstalledAppSignInService.TokenEndpoint, _http.Requests.Single().Uri.ToString());
        Assert.Equal("authorization_code", form["grant_type"]);
        Assert.Equal("the-code", form["code"]);
        Assert.Equal(query["code_challenge"], PkceCodes.ChallengeOf(form["code_verifier"]));
        Assert.Equal(_browser.RedirectUri.ToString(), form["redirect_uri"]);
        Assert.Equal(ClientId, form["client_id"]);
        Assert.False(form.ContainsKey("client_secret"));

        Assert.Contains("refresh-1", _store.Value, StringComparison.Ordinal);
        Assert.Equal(account, await _service.GetCurrentAccountAsync(Ct));

        // The access token of the sign-in is used without another request.
        Assert.Equal("access-1", await _service.GetAccessTokenAsync(Scopes, Ct));
        Assert.Single(_http.Requests);
    }

    [Fact]
    public async Task An_answer_for_another_request_is_refused()
    {
        _browser.State = "forged";

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SignInAsync(Scopes, Ct).AsTask());

        Assert.Empty(_http.Requests);
        Assert.Null(_store.Value);
    }

    [Fact]
    public async Task Declining_on_googles_page_is_a_cancellation()
    {
        _browser.Error = "access_denied";

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.SignInAsync(Scopes, Ct).AsTask());

        Assert.Null(_store.Value);
    }

    [Fact]
    public async Task Without_the_app_folder_scope_nothing_is_kept_and_the_grant_is_revoked()
    {
        _http
            .RespondJson(TokenJson(refreshToken: "refresh-1", scope: "openid https://www.googleapis.com/auth/userinfo.email"))
            .RespondStatus(HttpStatusCode.OK);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SignInAsync(Scopes, Ct).AsTask());

        Assert.Null(_store.Value);
        Assert.Equal(GoogleInstalledAppSignInService.RevocationEndpoint, _http.Requests[1].Uri.ToString());
        Assert.Equal("refresh-1", Form(_http.Requests[1])["token"]);
    }

    [Fact]
    public async Task Access_tokens_are_refreshed_shortly_before_they_expire()
    {
        await SignInAsync();
        _http.RespondJson("""{ "access_token": "access-2", "expires_in": 3599, "refresh_token": "refresh-2" }""");

        _time.Advance(TimeSpan.FromMinutes(58));
        Assert.Equal("access-1", await _service.GetAccessTokenAsync(Scopes, Ct));
        _time.Advance(TimeSpan.FromMinutes(1.5));
        Assert.Equal("access-2", await _service.GetAccessTokenAsync(Scopes, Ct));

        var form = Form(_http.Requests[^1]);
        Assert.Equal("refresh_token", form["grant_type"]);
        Assert.Equal("refresh-1", form["refresh_token"]);
        Assert.Equal(ClientId, form["client_id"]);
        Assert.False(form.ContainsKey("client_secret"));

        // A rotated refresh token replaces the old one.
        Assert.Contains("refresh-2", _store.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_revoked_grant_means_sign_in_again_and_forgets_the_account()
    {
        await SignInAsync();
        _time.Advance(TimeSpan.FromHours(2));
        _http.RespondJson("""{ "error": "invalid_grant", "error_description": "Token has been expired or revoked." }""", HttpStatusCode.BadRequest);

        var error = await Assert.ThrowsAsync<AuthenticationRequiredException>(() => _service.GetAccessTokenAsync(Scopes, Ct).AsTask());

        Assert.Equal(ExternalIdentityProvider.Google, error.Provider);
        Assert.Null(_store.Value);
        Assert.Null(await _service.GetCurrentAccountAsync(Ct));
    }

    [Fact]
    public async Task A_server_problem_is_temporary_and_keeps_the_account()
    {
        await SignInAsync();
        _time.Advance(TimeSpan.FromHours(2));
        _http.RespondStatus(HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetAccessTokenAsync(Scopes, Ct).AsTask());

        Assert.NotNull(await _service.GetCurrentAccountAsync(Ct));
    }

    [Fact]
    public async Task Nobody_signed_in_needs_a_sign_in()
    {
        Assert.Null(await _service.GetCurrentAccountAsync(Ct));
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => _service.GetAccessTokenAsync(Scopes, Ct).AsTask());
    }

    [Fact]
    public async Task More_access_than_granted_needs_a_new_consent()
    {
        await SignInAsync();

        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => _service.GetAccessTokenAsync(["https://www.googleapis.com/auth/drive"], Ct).AsTask());
    }

    [Fact]
    public async Task Sign_out_removes_the_token_and_revokes_the_grant_even_when_offline()
    {
        await SignInAsync();
        _http.Respond(_ => throw new HttpRequestException("offline"));

        await _service.SignOutAsync(Ct);

        Assert.Null(_store.Value);
        Assert.Null(await _service.GetCurrentAccountAsync(Ct));
        Assert.Equal(GoogleInstalledAppSignInService.RevocationEndpoint, _http.Requests[^1].Uri.ToString());
        Assert.Equal("refresh-1", Form(_http.Requests[^1])["token"]);
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => _service.GetAccessTokenAsync(Scopes, Ct).AsTask());
    }

    [Fact]
    public async Task A_damaged_entry_counts_as_signed_out()
    {
        _store.Value = "{ not json";

        Assert.Null(await _service.GetCurrentAccountAsync(Ct));
        Assert.Null(_store.Value);
    }

    [Fact]
    public void The_id_token_names_the_account()
    {
        Assert.Equal(("sub-42", "user@example.com"), GoogleInstalledAppSignInService.ReadIdToken(IdToken()));
        Assert.Equal((null, null), GoogleInstalledAppSignInService.ReadIdToken("not-a-jwt"));
        Assert.Equal((null, null), GoogleInstalledAppSignInService.ReadIdToken(null));
    }

    private async Task SignInAsync()
    {
        _http.RespondJson(TokenJson(refreshToken: "refresh-1", scope: $"{DriveScope} openid"));
        await _service.SignInAsync(Scopes, Ct);
    }

    private static string TokenJson(string refreshToken, string scope) => JsonSerializer.Serialize(new Dictionary<string, object>
    {
        ["access_token"] = "access-1",
        ["expires_in"] = 3599,
        ["refresh_token"] = refreshToken,
        ["scope"] = scope,
        ["token_type"] = "Bearer",
        ["id_token"] = IdToken(),
    });

    private static string IdToken()
    {
        static string Part(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{Part("""{"alg":"RS256"}""")}.{Part("""{"sub":"sub-42","email":"user@example.com","email_verified":true}""")}.signature";
    }

    private static Dictionary<string, string> Query(Uri uri) => Decode(uri.Query.TrimStart('?'));

    private static Dictionary<string, string> Form(RecordedRequest request) => Decode(request.BodyText);

    private static Dictionary<string, string> Decode(string text) => text
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair => pair.Split('=', 2))
        .ToDictionary(p => Uri.UnescapeDataString(p[0].Replace('+', ' ')), p => Uri.UnescapeDataString(p[1].Replace('+', ' ')));

    /// <summary>A browser that answers Google's page at once with a code, an error or a forged state.</summary>
    private sealed class FakeBrowser : IAuthorizationBrowser, IAuthorizationBrowserSession
    {
        public Uri RedirectUri { get; } = new("http://127.0.0.1:50123/");

        public Uri? AuthorizationUri { get; private set; }

        public string? State { get; set; }

        public string? Error { get; set; }

        public ValueTask<IAuthorizationBrowserSession> StartAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IAuthorizationBrowserSession>(this);

        public Task<IReadOnlyDictionary<string, string>> AuthorizeAsync(Uri authorizationUri, CancellationToken cancellationToken = default)
        {
            AuthorizationUri = authorizationUri;
            var state = State ?? Query(authorizationUri)["state"];
            IReadOnlyDictionary<string, string> answer = Error is null
                ? new Dictionary<string, string> { ["code"] = "the-code", ["state"] = state }
                : new Dictionary<string, string> { ["error"] = Error, ["state"] = state };
            return Task.FromResult(answer);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class MemoryStore : IProtectedValueStore
    {
        public string? Value { get; set; }

        public ValueTask<string?> ReadAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(Value);

        public ValueTask WriteAsync(string value, CancellationToken cancellationToken = default)
        {
            Value = value;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            Value = null;
            return ValueTask.CompletedTask;
        }
    }
}
