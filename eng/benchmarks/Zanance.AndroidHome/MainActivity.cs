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
        var baseline = Intent?.GetStringExtra("review_baseline") ?? comparison;
        if (phase is not ("prepare" or "measure" or "compare" or "guard" or "filters" or "schema" or "billing-owned" or "tables" or "legends" or "budget-titles" or "home-legends" or "plan-rows" or "ledger-rows" or "ledger" or "read-profile" or "ledger-read" or "plan-publication" or "plan-publication-fixed" or "home-stages") || run is null || !System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$")) return;
        if (phase != "prepare" && (comparison is null || !System.Text.RegularExpressions.Regex.IsMatch(comparison, "^[0-9a-f]{32}$"))) return;
        if (phase == "compare" && (baseline is null || !System.Text.RegularExpressions.Regex.IsMatch(baseline, "^[0-9a-f]{32}$"))) return;
        // D-320: sleeping display invalidates native frame/layout observations. Keep only this
        // explicitly requested independent review window awake; never change a device setting or app lock.
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        // Independent diagnostics retain only exception type and source stack, never messages or values.
        Presentation.Failures.Observed = error =>
        {
            var stack = error.StackTrace ?? string.Empty;
            Android.Util.Log.Info("ZananceHomeFailure", error.GetType().FullName + "|" + stack[..Math.Min(stack.Length, 6000)]);
        };
        _ = RunAsync(phase, run, comparison, baseline);
    }

    private async Task RunAsync(string phase, string run, string? comparison, string? baseline)
    {
        var services = IPlatformApplication.Current!.Services;
        // MAUI registers IApplication, not the concrete App; use the instance owning the real window.
        var app = (App)Microsoft.Maui.Controls.Application.Current!;
        Android.Util.Log.Info("ZananceHomeStage", comparison + "|startup-wait");
        await app.WaitForStartupAsync();
        Android.Util.Log.Info("ZananceHomeStage", comparison + "|startup-ready");
        await AndroidHomeReview.RunAsync(app, services, phase, run, comparison, baseline);
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
