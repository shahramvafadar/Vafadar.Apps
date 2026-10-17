using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Zanance.App;

/// <summary>Runs requested independent performance phases after the original covered application startup.</summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale | ConfigChanges.LayoutDirection)]
public sealed class MainActivity : MauiAppCompatActivity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Window?.AddFlags(WindowManagerFlags.Secure);
        base.OnCreate(savedInstanceState);
        if (savedInstanceState is not null || PackageName != AndroidHomeReview.Package) return;
        var phase = Intent?.GetStringExtra("review_phase");
        var run = Intent?.GetStringExtra("review_run");
        var comparison = Intent?.GetStringExtra("review_pass");
        if (phase is not ("prepare" or "measure" or "compare") || run is null || !System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$")) return;
        if (phase != "prepare" && (comparison is null || !System.Text.RegularExpressions.Regex.IsMatch(comparison, "^[0-9a-f]{32}$"))) return;
        _ = RunAsync(phase, run, comparison);
    }

    private async Task RunAsync(string phase, string run, string? comparison)
    {
        var services = IPlatformApplication.Current!.Services;
        // MAUI registers IApplication, not the concrete App; use the instance owning the real window.
        var app = (App)Microsoft.Maui.Controls.Application.Current!;
        await app.WaitForStartupAsync();
        await AndroidHomeReview.RunAsync(app, services, phase, run, comparison);
    }

    /// <inheritdoc />
    public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        // Match the production activity: per-app language changes must not recreate onboarding over the measured Shell.
        if (IPlatformApplication.Current?.Services.GetService<Vafadar.Localization.ILocalizationService>() is { } local)
            AppLocales.FromSystem(local);
    }
}

/// <summary>Exposes only original private root replacement inside this independent compiled caller.</summary>
public partial class App
{
    /// <summary>Attaches the original Shell through the application's original root replacement.</summary>
    internal AppShell OpenReviewShell()
    {
        if (AppInfo.Current.PackageName != AndroidHomeReview.Package) throw new IOException("Independent package identity required.");
        var shell = CreateShell(); ReplaceRoot(Windows[0], shell); return shell;
    }
}
