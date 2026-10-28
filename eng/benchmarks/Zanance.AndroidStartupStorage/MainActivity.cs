using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Vafadar.Zanance.App;

/// <summary>Admits only finite nonfinancial layout cases on the fresh owned emulator package.</summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale | ConfigChanges.LayoutDirection)]
public sealed class MainActivity : MauiAppCompatActivity
{
    /// <summary>Never shares an identity with shipping Main or an unfinished review.</summary>
    internal const string Package = "pro.vafadar.zanance.startupstoragereview";

    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        if (PackageName != Package || Build.Hardware != "ranchu")
            throw new IOException("Fresh owned emulator required before startup notice layout.");
        Window?.AddFlags(WindowManagerFlags.Secure | WindowManagerFlags.KeepScreenOn);
        base.OnCreate(savedInstanceState);
        MainThread.BeginInvokeOnMainThread(() => ((StartupStorageApplication)Microsoft.Maui.Controls.Application.Current!).Show(Intent));
    }

    /// <inheritdoc />
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        MainThread.BeginInvokeOnMainThread(() => ((StartupStorageApplication)Microsoft.Maui.Controls.Application.Current!).Show(intent));
    }
}
