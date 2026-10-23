using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Zanance.App;

/// <summary>Prepares one explicit fictitious native-notification fixture through the original protected application.</summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale | ConfigChanges.LayoutDirection)]
public sealed class MainActivity : MauiAppCompatActivity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        if (PackageName != NativeReminderReview.Package)
            throw new IOException("Independent reminder identity required before window creation.");
        Window?.AddFlags(WindowManagerFlags.Secure);
        base.OnCreate(savedInstanceState);
        var run = Intent?.GetStringExtra("review_run");
        if (savedInstanceState is not null || PackageName != NativeReminderReview.Package ||
            run is null || !System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$")) return;
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        _ = PrepareAsync(run, Intent?.GetBooleanExtra("review_scheduled_only", false) == true,
            Intent?.GetBooleanExtra("review_goal_lifecycle", false) == true,
            Intent?.GetBooleanExtra("review_period_lifecycle", false) == true,
            Intent?.GetBooleanExtra("review_goal_variants", false) == true, Intent?.GetStringExtra("review_route_phase"), Intent?.GetStringExtra("review_profile_phase"));
    }

    /// <inheritdoc />
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        var run = intent?.GetStringExtra("review_run");
        var profilePhase = intent?.GetStringExtra("review_profile_phase");
        if (PackageName == NativeReminderReview.Package && profilePhase is "restore" or "finish"
            && run is not null && System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$"))
        { _ = PrepareAsync(run, false, false, false, false, null, profilePhase); return; }
        if (PackageName != NativeReminderReview.Package || intent?.GetStringExtra("review_route_phase") != "review"
            || run is null || !System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$")) return;
        _ = PrepareAsync(run, false, false, false, false, "review", null);
    }

    /// <summary>Waits for the real covered foreground startup before preparing notification metadata.</summary>
    private async Task PrepareAsync(string run, bool scheduledOnly, bool goalLifecycle, bool periodLifecycle, bool goalVariants, string? routePhase, string? profilePhase)
    {
        try
        {
            var app = (App)Microsoft.Maui.Controls.Application.Current!;
            await app.WaitForStartupAsync();
            if ((scheduledOnly ? 1 : 0) + (goalLifecycle ? 1 : 0) + (periodLifecycle ? 1 : 0) + (goalVariants ? 1 : 0) + (routePhase is null ? 0 : 1) + (profilePhase is null ? 0 : 1) > 1)
                throw new IOException("Choose one independent reminder review.");
            if (profilePhase is not null)
                await NativeGoalProfileScopeReview.PrepareAsync(IPlatformApplication.Current!.Services, run, profilePhase);
            else if (routePhase is not null)
                await NativeGoalPeriodRouteReview.PublishAsync(IPlatformApplication.Current!.Services, run, routePhase);
            else if (goalVariants)
                await NativeGoalVariantReminderReview.PrepareAsync(IPlatformApplication.Current!.Services, run);
            else if (periodLifecycle)
                await NativePeriodReminderReview.PrepareAsync(IPlatformApplication.Current!.Services, run);
            else if (goalLifecycle)
            {
                await NativeGoalReminderReview.PrepareAsync(IPlatformApplication.Current!.Services, run);
            }
            else await NativeReminderReview.PrepareAsync(IPlatformApplication.Current!.Services, run, scheduledOnly);
            Android.Util.Log.Info("ZananceReminderReview", "fixture-ready");
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Android.Util.Log.Error("ZananceReminderReview", error.GetType().FullName + "|" + error.StackTrace);
        }
    }
}
