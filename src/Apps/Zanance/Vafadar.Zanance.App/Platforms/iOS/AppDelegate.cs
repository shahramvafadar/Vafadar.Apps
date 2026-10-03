using Foundation;
using UIKit;

namespace Vafadar.Zanance.App;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <summary>
    /// URLs opened in the app: the end of a Microsoft sign-in (<c>msauth.pro.vafadar.zanance://auth</c>) goes to MSAL
    /// (D-50); everything else, including Google's redirect for MAUI's WebAuthenticator, to the MAUI default.
    /// </summary>
    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options) =>
        Vafadar.Authentication.Maui.SignInUrlCallbacks.OpenUrl(url) || base.OpenUrl(application, url, options);
}
