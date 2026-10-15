using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Vafadar.Zanance.App;

/// <summary>Runs only the requested original-page review in an independently owned Android installation.</summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale | ConfigChanges.LayoutDirection)]
public sealed class MainActivity : MauiAppCompatActivity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        if (PackageName != AndroidSyncReview.Package) throw new IOException("Independent compiled package identity required before window startup.");
        Window?.AddFlags(WindowManagerFlags.Secure);
        base.OnCreate(savedInstanceState);
#if SYNC_NETWORK_REVIEW
        var run = Intent?.GetStringExtra("network_run");
        if (savedInstanceState is null && run is not null && System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$"))
            _ = RunNetworkAsync(run, Intent?.GetBooleanExtra("network_resume", false) == true,
                Intent?.GetIntExtra("network_port", 0) ?? 0, Intent?.GetStringExtra("network_certificate"), Intent?.GetStringExtra("network_attachment"), Intent?.GetBooleanExtra("network_continue", false) == true);
#else
        var run = Intent?.GetStringExtra("review_run");
        if (savedInstanceState is null && run is not null && System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$"))
            _ = RunAsync(run, Intent?.GetBooleanExtra("review_resume", false) == true, Intent?.GetStringExtra("review_from_language"));
#endif
    }

#if SYNC_NETWORK_REVIEW
    private static async Task RunNetworkAsync(string run, bool resume, int port, string? certificate, string? attachment, bool continuation)
    {
        var app = (App)Microsoft.Maui.Controls.Application.Current!;
        await app.WaitForStartupAsync();
        await AndroidSyncNetwork.RunAsync(app, IPlatformApplication.Current!.Services, run, resume, port, certificate, attachment, continuation);
    }
#endif

    private static async Task RunAsync(string run, bool resume, string? fromLanguage)
    {
        var app = (App)Microsoft.Maui.Controls.Application.Current!;
        await app.WaitForStartupAsync();
        await AndroidSyncReview.RunAsync(app, IPlatformApplication.Current!.Services, run, resume, fromLanguage);
    }
}

/// <summary>Exposes original root replacement only in this independently compiled native caller.</summary>
public partial class App
{
    internal void ShowSyncReview(Page page)
    {
        if (AppInfo.Current.PackageName != AndroidSyncReview.Package) throw new IOException("Independent package required.");
        ReplaceRoot(Windows[0], new NavigationPage(page));
    }
}
