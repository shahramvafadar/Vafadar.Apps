#if IOS
using Vafadar.Authentication.OAuth;

namespace Vafadar.Authentication.Maui;

/// <summary>
/// The iOS authorization browser: MAUI's <c>WebAuthenticator</c>, which shows the provider's page in an
/// <c>ASWebAuthenticationSession</c> (Safari, outside the app) and returns when the page redirects to the app's URL
/// scheme. The scheme must be listed in <c>CFBundleURLTypes</c> of Info.plist, otherwise iOS cannot return.
/// </summary>
/// <param name="redirectUri">The redirect URI with the app's scheme, e.g. <c>com.googleusercontent.apps.…:/oauth2redirect</c>.</param>
internal sealed class WebAuthenticatorAuthorizationBrowser(Uri redirectUri) : IAuthorizationBrowser
{
    public ValueTask<IAuthorizationBrowserSession> StartAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IAuthorizationBrowserSession>(new Session(redirectUri));

    private sealed class Session(Uri redirectUri) : IAuthorizationBrowserSession
    {
        public Uri RedirectUri { get; } = redirectUri;

        public async Task<IReadOnlyDictionary<string, string>> AuthorizeAsync(Uri authorizationUri, CancellationToken cancellationToken = default)
        {
            // The session shares Safari's cookies (not ephemeral), so a user already signed in to Google only picks the
            // account. Closing the sheet throws TaskCanceledException, which callers treat as "cancelled".
            var result = await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                Microsoft.Maui.Authentication.WebAuthenticator.Default.AuthenticateAsync(new Microsoft.Maui.Authentication.WebAuthenticatorOptions
                {
                    Url = authorizationUri,
                    CallbackUrl = RedirectUri,
                    PrefersEphemeralWebBrowserSession = false,
                }));
            cancellationToken.ThrowIfCancellationRequested();
            return new Dictionary<string, string>(result.Properties, StringComparer.Ordinal);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
#endif
