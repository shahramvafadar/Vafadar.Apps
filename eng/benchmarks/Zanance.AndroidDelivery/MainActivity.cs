using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Maui;

namespace Vafadar.Zanance.DeliveryReview;

/// <summary>Opens only the isolated notification review window with foreground capture protection retained.</summary>
[Activity(Theme = "@style/Maui.MainTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public sealed class MainActivity : MauiAppCompatActivity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        if (PackageName != DeliveryApplication.Package) throw new InvalidOperationException("Independent package required.");
        DeliveryApplication.Run = Intent?.GetStringExtra("review_run");
        Window?.AddFlags(WindowManagerFlags.Secure);
        base.OnCreate(savedInstanceState);
    }
}
