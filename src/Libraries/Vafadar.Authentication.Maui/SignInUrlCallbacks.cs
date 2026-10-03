#if IOS
using Foundation;
using Microsoft.Identity.Client;

namespace Vafadar.Authentication.Maui;

/// <summary>
/// Passes URLs opened in the app to the sign-in flows on iOS. Call it from <c>AppDelegate.OpenUrl</c>:
/// <code>if (SignInUrlCallbacks.OpenUrl(url)) return true;</code>
/// </summary>
/// <remarks>
/// MSAL signs in through <c>ASWebAuthenticationSession</c>, which normally completes without this call; the callback is
/// still required by MSAL for the redirect <c>msauth.{bundle-id}://auth</c> when iOS hands the URL to the app instead.
/// Google's redirect is handled by MAUI's <c>WebAuthenticator</c> through the base implementation of <c>OpenUrl</c>.
/// </remarks>
public static class SignInUrlCallbacks
{
    /// <summary>Returns <see langword="true"/> when <paramref name="url"/> was the end of a Microsoft sign-in.</summary>
    public static bool OpenUrl(NSUrl url)
    {
        ArgumentNullException.ThrowIfNull(url);
        return url.Scheme?.StartsWith("msauth.", StringComparison.OrdinalIgnoreCase) == true
            && AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url);
    }
}
#endif
