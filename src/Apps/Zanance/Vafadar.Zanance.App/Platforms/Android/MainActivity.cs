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
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // Financial data never appears in the recent-apps preview or in screenshots, with or without the app lock
    // (SEC-02, owner decision D-23). Set before the first frame is drawn.
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Window?.AddFlags(WindowManagerFlags.Secure);
        base.OnCreate(savedInstanceState);
        OpenFromWidget(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        OpenFromWidget(intent);
    }

    // A tap on the quick add widget (D-30) opens a new entry of that kind, after the app lock.
    private static void OpenFromWidget(Intent? intent)
    {
        if (intent?.Action == QuickAddWidget.Action && intent.GetStringExtra(QuickAddWidget.KindExtra) is { } kind)
        {
            App.OpenLink("entry|" + kind);
        }
    }
}
