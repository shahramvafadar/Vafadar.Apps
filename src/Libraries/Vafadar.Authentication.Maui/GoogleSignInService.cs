#if ANDROID
using System.Net.Http.Headers;
using System.Text.Json;
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.Identity;
using Android.Gms.Common.Apis;
using Android.Gms.Extensions;

namespace Vafadar.Authentication.Maui;

/// <summary>
/// Google sign-in on Android with the Google Identity authorization API (Google Play services): the user grants the
/// requested scopes in Google's own dialog, and access tokens are fetched from Play services when needed. No refresh
/// token or password is stored by the app; only the e-mail address of the account is kept to show it.
/// </summary>
public sealed class GoogleSignInService : IExternalSignInService
{
    /// <summary>The request code of the consent dialog.</summary>
    internal const int RequestCode = 0x6a0c;

    private const string AccountKey = "vafadar.auth.google.account";
    private static readonly string[] IdentityScopes = ["openid", "email"];
    private static TaskCompletionSource<Intent?>? _pending;

    /// <inheritdoc />
    public ExternalIdentityProvider Provider => ExternalIdentityProvider.Google;

    /// <inheritdoc />
    public ValueTask<ExternalAccount?> GetCurrentAccountAsync(CancellationToken cancellationToken = default)
    {
        var email = Microsoft.Maui.Storage.Preferences.Default.Get<string?>(AccountKey, null);
        return ValueTask.FromResult(email is null ? null : new ExternalAccount(Provider, email, email, null));
    }

    /// <inheritdoc />
    public async ValueTask<string> GetAccessTokenAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        if (await GetCurrentAccountAsync(cancellationToken) is null)
        {
            throw new AuthenticationRequiredException(Provider);
        }

        var result = await AuthorizeAsync(scopes);
        return result.HasResolution || string.IsNullOrEmpty(result.AccessToken)
            ? throw new AuthenticationRequiredException(Provider)
            : result.AccessToken;
    }

    /// <inheritdoc />
    public async ValueTask<ExternalAccount> SignInAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? throw new InvalidOperationException("No activity to sign in from.");
        var all = scopes.Concat(IdentityScopes).Distinct().ToList();
        var result = await AuthorizeAsync(all);
        if (result.HasResolution)
        {
            // Google's consent dialog; its result comes back through the activity (see SignInActivityResults).
            _pending = new TaskCompletionSource<Intent?>(TaskCreationOptions.RunContinuationsAsynchronously);
            activity.StartIntentSenderForResult(result.PendingIntent!.IntentSender, RequestCode, null, 0, 0, 0);
            using (cancellationToken.Register(() => _pending?.TrySetCanceled()))
            {
                var data = await _pending.Task;
                result = data is null
                    ? throw new OperationCanceledException("The sign-in was cancelled.", cancellationToken)
                    : Identity.GetAuthorizationClient(activity).GetAuthorizationResultFromIntent(data);
            }
        }

        if (string.IsNullOrEmpty(result.AccessToken))
        {
            throw new OperationCanceledException("The sign-in was cancelled.", cancellationToken);
        }

        var email = await EmailAsync(result.AccessToken, cancellationToken) ?? "Google";
        Microsoft.Maui.Storage.Preferences.Default.Set(AccountKey, email);
        return new ExternalAccount(Provider, email, email, null);
    }

    /// <inheritdoc />
    public async ValueTask SignOutAsync(CancellationToken cancellationToken = default)
    {
        Microsoft.Maui.Storage.Preferences.Default.Remove(AccountKey);

        // The grant is revoked as well, so the next sign-in asks again; the account is signed out even when offline.
        try
        {
            var result = await AuthorizeAsync([.. IdentityScopes]);
            if (!result.HasResolution && !string.IsNullOrEmpty(result.AccessToken))
            {
                using var http = new HttpClient();
                using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("token", result.AccessToken)]);
                await http.PostAsync(new Uri("https://oauth2.googleapis.com/revoke"), content, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine($"Google revoke failed: {ex.GetType().Name}");
        }
    }

    /// <summary>Completes a pending consent dialog.</summary>
    internal static void OnConsentResult(Result resultCode, Intent? data) =>
        _pending?.TrySetResult(resultCode == Result.Ok ? data : null);

    private static async Task<AuthorizationResult> AuthorizeAsync(IEnumerable<string> scopes)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? throw new AuthenticationRequiredException(ExternalIdentityProvider.Google);
        var request = AuthorizationRequest.InvokeBuilder()
            .SetRequestedScopes([.. scopes.Select(s => new Scope(s))])
            .Build();
        return await Identity.GetAuthorizationClient(activity).Authorize(request).AsAsync<AuthorizationResult>();
    }

    private static async Task<string?> EmailAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            using var http = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return json.RootElement.TryGetProperty("email", out var email) ? email.GetString() : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }
}
#endif
