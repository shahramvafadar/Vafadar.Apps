#if CLOUD_MICROSOFT
using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace Vafadar.Zanance.App;

/// <summary>
/// Receives the end of the Microsoft sign-in in the browser (MSAL redirect <c>msal{client-id}://auth</c>, D-35). Without
/// a configured client (CLOUD_MICROSOFT) the activity is not compiled at all.
/// </summary>
[Activity(Exported = true, NoHistory = true, LaunchMode = Android.Content.PM.LaunchMode.SingleTask)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryBrowsable, Intent.CategoryDefault],
    DataHost = "auth",
    DataScheme = "msal" + AppSecrets.MicrosoftEntraClientId)]
public sealed class MsalRedirectActivity : BrowserTabActivity
{
}
#endif
