using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Zanance.App;

/// <summary>Runs requested independent performance phases after the original covered application startup.</summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
#if LOADED_COLD_START_REVIEW || CAMERA_CAPTURE_REVIEW
    Permission = Android.Manifest.Permission.Dump,
#endif
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale | ConfigChanges.LayoutDirection)]
public sealed class MainActivity : MauiAppCompatActivity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
#if LOADED_COLD_START_REVIEW
        if (OperatingSystem.IsAndroidVersionAtLeast(36)) LoadedColdStartNative.CaptureActivityEntry();
#endif
        Window?.AddFlags(WindowManagerFlags.Secure);
#if NATIVE_FAULT_PC_REVIEW
        var faultObserverAccepted = savedInstanceState is not null || NativeFaultPcObserver.TryArm(this);
#endif
        base.OnCreate(savedInstanceState);
#if CAMERA_CAPTURE_REVIEW
        // This fresh source-only caller cannot enter historical preparation or financial phases.
        if (PackageName == "pro.vafadar.zanance.receiptcamerareview")
        {
            if (savedInstanceState is null) CameraCaptureReview.Start(this);
            return;
        }
#endif
#if CORECLR_RUNTIME_REVIEW
        // Capture only the original window; a provider request must never start storage preparation.
        CoreClrRuntimeObservation.Attach(this);
#endif
#if NATIVE_FAULT_PC_REVIEW
        if (!faultObserverAccepted) return;
#endif
        if (savedInstanceState is not null || PackageName != AndroidHomeReview.Package) return;
#if RECEIPT_SOURCE_REVIEW
        // A flat device can retain the last app-requested landscape rotation after Unspecified.
        // Return only this disposable caller's window to portrait before resuming the original app;
        // never write system rotation/display settings or replay source/help qualification.
        if (PackageName == "pro.vafadar.zanance.receiptsourcereview"
            && Intent?.GetBooleanExtra("review_return_portrait", false) == true)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            return;
        }
#endif
#if LOADED_COLD_START_REVIEW
        // No older writer/preparation phase may run in this one-purpose current-source cohort.
        LoadedColdStartReview.Start(this);
        return;
#else
        var phase = Intent?.GetStringExtra("review_phase");
        var run = Intent?.GetStringExtra("review_run");
        var comparison = Intent?.GetStringExtra("review_pass");
#if TRANSACTIONS_HEADER_REVIEW
        if (phase != "transactions-add-header") return;
#endif
#if LEDGER_CONFLICT_REVIEW
        if (phase != "ledger-conflict") return;
#endif
#if LEDGER_CODEC_COMPATIBILITY
        if (phase != "ledger-codec-compat") return;
#endif
#if SUPA_CSV_REVIEW
        // This new caller cannot replay any prior financial or reminder cohort.
        if (phase != "supa-csv") return;
#endif
#if HOME_BUDGET_REVIEW
        // This new identity has no authority to replay an older caller's financial preparation.
        if (phase != "home-budget-refresh") return;
#endif
        var baseline = Intent?.GetStringExtra("review_baseline") ?? comparison;
        if (phase is not ("supa-csv" or "home-budget-refresh" or "home-financial-resume" or "home-financial-refresh" or "receipt-source-help" or "scanned-receipts" or "receipt-corpus" or "csv-literal-final" or "forecast-return" or "relocked-destinations" or "validation-layout" or "prepare" or "prepare-unreviewed" or "home-review-count" or "home-review-count-fixed" or "home-add-header" or "home-add-header-fixed" or "transactions-add-header" or "transactions-header-os" or "transactions-undo-os" or "ledger-conflict" or "ledger-codec-compat" or "plans-add-header" or "headers-os" or "rate-outcomes-os" or "home-refresh" or "home-refresh-references" or "holding-detail-retired-callback" or "holding-detail" or "holding-overview-captions" or "holding-overview" or "currency-input" or "currency-focus" or "home-goals-worker" or "goal-list" or "goal-detail" or "goals-os" or "goals-os-report" or "toolbar-observer" or "navigation-pulse" or "plan-form" or "entry-workflow" or "entry-form" or "entry-detail" or "holding-event-publication" or "holding-location" or "holding-type" or "holding-archive" or "holding-history" or "holding-price" or "holding-quantity-input" or "holding-fee-action" or "holding-fee-input" or "holding-refund-review" or "holding-financial-review" or "split-financial-review" or "import-financial-review" or "occurrence-refresh" or "occurrence-state" or "occurrence-commit" or "settlement-commit" or "settlement-commit-fixed" or "settlement-commit-observed" or "getting-started" or "measure" or "compare" or "guard" or "filters" or "schema" or "billing-owned" or "tables" or "legends" or "original-amount-errors" or "budget-scope-captions" or "budget-period-captions" or "snapshot-difference" or "import-group-undo" or "forecast-missing-rate" or "budget-financial-read" or "budget-titles" or "home-legends" or "plan-rows" or "ledger-rows" or "font-scale" or "ledger" or "read-profile" or "ledger-read" or "plan-publication" or "plan-publication-fixed" or "retire-home-read-blocks" or "repair-schema-probe" or "home-current-stages" or "home-stages" or "reimbursements" or "reimbursement-page" or "reimbursement-visibility" or "reimbursement-page-fixed" or "home-template-reload" or "settings-text" or "retained-aggregate" or "editor-text" or "csv-input" or "csv-input-empty" or "csv-quotation" or "ecb-reference" or "ecb-reference-report" or "root-retirement" or "settings-read" or "narrow-surfaces" or "destination-fee" or "destination-fee-fixed" or "destination-fee-native" or "destination-fee-lifecycle" or "destination-fee-owned" or "destination-fee-resume" or "destination-fee-inspect" or "destination-fee-prepared" or "destination-fee-migrations" or "destination-fee-effect" or "destination-fee-unicode") || run is null || !System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$")) return;
        if (phase is not ("prepare" or "prepare-unreviewed") && (comparison is null || !System.Text.RegularExpressions.Regex.IsMatch(comparison, "^[0-9a-f]{32}$"))) return;
        if (phase is ("csv-literal-final" or "compare" or "holding-overview-captions" or "holding-detail" or "holding-detail-retired-callback") && (baseline is null || !System.Text.RegularExpressions.Regex.IsMatch(baseline, "^[0-9a-f]{32}$"))) return;
        // D-320: sleeping display invalidates native frame/layout observations. Keep only this
        // explicitly requested independent review window awake; never change a device setting or app lock.
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        // Independent diagnostics retain only exception type and source stack, never messages or values.
        Presentation.Failures.Observed = error =>
        {
            var stack = error.StackTrace ?? string.Empty;
            Android.Util.Log.Info("ZananceHomeFailure", error.GetType().FullName + "|" + stack[..Math.Min(stack.Length, 6000)]);
        };
        if (phase == "plan-form" && PackageName == "pro.vafadar.zanance.householdmoneyreview"
            && (Intent?.GetBooleanExtra("review_household_os", false) == true || Intent?.GetBooleanExtra("review_household_captions", false) == true)
            && Intent.GetBooleanExtra("review_secondary_household", false))
        {
            // D-444: stop the retained fictional peer before startup can schedule foreground synchronization.
            IPlatformApplication.Current!.Services.GetRequiredService<Features.Sync.PersonalSyncForegroundWorker>().Dispose();
            Android.Util.Log.Info("ZananceHomeStage", comparison + "|household-os-foreground-sync-stopped");
        }
        _ = RunAsync(phase, run, comparison, baseline);
#endif
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

#if CAMERA_CAPTURE_REVIEW
    /// <inheritdoc />
    protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        // Forward the real external Activity result to the original production camera adapter.
        if (CameraCapture.OnActivityResult(requestCode, resultCode))
            CameraCaptureReview.RecordNativeResult(requestCode, resultCode);
    }
#endif

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

    /// <summary>Hosts an original Home page in this independent package without altering production startup.</summary>
    internal void OpenFinancialReviewHome(Features.Home.HomePage page)
    {
        if (AppInfo.Current.PackageName != AndroidHomeReview.Package) throw new IOException("Independent package identity required.");
        ReplaceRoot(Windows[0], new NavigationPage(page));
    }
}
