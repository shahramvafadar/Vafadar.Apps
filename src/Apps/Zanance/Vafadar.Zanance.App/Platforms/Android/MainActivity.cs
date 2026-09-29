using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Vafadar.Zanance.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale
        | ConfigChanges.LayoutDirection)]
public class MainActivity : MauiAppCompatActivity
{
    // Financial data never appears in the recent-apps preview or in screenshots, with or without the app lock
    // (SEC-02, owner decision D-23). Set before the first frame is drawn.
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Window?.AddFlags(WindowManagerFlags.Secure);
        base.OnCreate(savedInstanceState);

        // A recreated activity or a start from the recent apps delivers the old intent again; only a fresh tap counts.
        if (savedInstanceState is null && Intent is { } intent && !intent.Flags.HasFlag(ActivityFlags.LaunchedFromHistory))
        {
            OpenFromWidget(intent);
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        OpenFromWidget(intent);
    }

    // The app language chosen in the Android settings (D-32) arrives as a configuration change; the app rebuilds its
    // screens itself, so the activity is not recreated.
    public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        if (IPlatformApplication.Current?.Services.GetService<Vafadar.Localization.ILocalizationService>() is { } localization)
        {
            AppLocales.FromSystem(localization);
        }

        QuickAddWidget.Refresh(this);
    }

    // A tap on the quick add widget (D-30) opens a new entry of that kind, after the app lock.
    private static void OpenFromWidget(Intent? intent)
    {
        if (intent?.Action == QuickAddWidget.Action && intent.GetStringExtra(QuickAddWidget.KindExtra) is { } kind)
        {
            intent.SetAction(null);
            App.OpenLink("entry|" + kind);
        }
    }
}
