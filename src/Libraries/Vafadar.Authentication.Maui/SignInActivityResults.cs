#if ANDROID
using Android.App;
using Android.Content;

namespace Vafadar.Authentication.Maui;

/// <summary>
/// Passes activity results to the sign-in flows on Android. Call it from <c>MainActivity.OnActivityResult</c>:
/// <code>SignInActivityResults.OnActivityResult(requestCode, resultCode, data);</code>
/// </summary>
public static class SignInActivityResults
{
    /// <summary>Hands the result to Google's consent dialog or to MSAL's browser flow.</summary>
    public static void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode == GoogleSignInService.RequestCode)
        {
            GoogleSignInService.OnConsentResult(resultCode, data);
            return;
        }

        Microsoft.Identity.Client.AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
    }
}
#endif
