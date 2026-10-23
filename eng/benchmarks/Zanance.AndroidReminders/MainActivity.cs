using Android.App;
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
            Intent?.GetBooleanExtra("review_period_lifecycle", false) == true);
    }

    /// <summary>Waits for the real covered foreground startup before preparing notification metadata.</summary>
    private async Task PrepareAsync(string run, bool scheduledOnly, bool goalLifecycle, bool periodLifecycle)
    {
        try
        {
            var app = (App)Microsoft.Maui.Controls.Application.Current!;
            await app.WaitForStartupAsync();
            if ((scheduledOnly ? 1 : 0) + (goalLifecycle ? 1 : 0) + (periodLifecycle ? 1 : 0) > 1)
                throw new IOException("Choose one independent reminder review.");
            if (periodLifecycle)
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
