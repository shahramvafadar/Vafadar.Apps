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
        | ConfigChanges.LayoutDirection | ConfigChanges.FontScale)]
public class MainActivity : MauiAppCompatActivity
{
    private Android.Views.View? _recentsCover;
    private int _configurationDensityDpi;
    private float _configurationFontScale;
    private TaskCompletionSource<bool>? _notificationSettingsReturn;

    // Start protected before the first frame, then apply the saved foreground choice (D-63).
    protected override void OnCreate(Bundle? savedInstanceState)
    {
#if DEBUG
        if (PackageName == "pro.vafadar.zanance.householdlifecycle" && Intent?.GetBooleanExtra("household_conflict_review", false) == true)
            System.Environment.SetEnvironmentVariable("VAFADAR_HOUSEHOLD_CONFLICT_REVIEW", "1");
        if (PackageName == "pro.vafadar.zanance.householdlifecycle" && Intent?.GetBooleanExtra("household_recovery_review", false) == true)
            System.Environment.SetEnvironmentVariable("VAFADAR_HOUSEHOLD_RECOVERY_REVIEW", "1");
        if (PackageName == "pro.vafadar.zanance.householdlifecycle" && Intent?.GetBooleanExtra("household_lifecycle_review", false) == true)
            System.Environment.SetEnvironmentVariable("VAFADAR_HOUSEHOLD_ANDROID_REVIEW", "1");
        if (PackageName == "pro.vafadar.zanance.copyacceptance" && Intent?.GetBooleanExtra("sale_copy_review", false) == true)
            System.Environment.SetEnvironmentVariable("VAFADAR_SALE_COPY_REVIEW", "1");
        if (PackageName == "pro.vafadar.zanance.accessacceptance" && Intent?.GetBooleanExtra("device_unlock_review", false) == true)
            System.Environment.SetEnvironmentVariable("VAFADAR_DEVICE_UNLOCK_REVIEW", "1");
#endif
        Window?.AddFlags(WindowManagerFlags.Secure);
        base.OnCreate(savedInstanceState);
        _configurationDensityDpi = Resources?.Configuration?.DensityDpi ?? 0;
        _configurationFontScale = Resources?.Configuration?.FontScale ?? 0;
        if (OperatingSystem.IsAndroidVersionAtLeast(33)) { SetRecentsScreenshotEnabled(false); }
        // Android 10+ can resume several activities. Keep screenshot protection until this one is on top.
        Security.ScreenProtection.SetBackground(OperatingSystem.IsAndroidVersionAtLeast(29));
        UpdateSystemBarIcons();

        // A recreated activity or a start from the recent apps delivers the old intent again; only a fresh tap counts.
        if (savedInstanceState is null && Intent is { } intent && !intent.Flags.HasFlag(ActivityFlags.LaunchedFromHistory))
        {
            OpenFromWidget(intent);
        }
    }

    protected override void OnPause()
    {
        ApplyForegroundPrivacy(false);
        base.OnPause();
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (!OperatingSystem.IsAndroidVersionAtLeast(29)) ApplyForegroundPrivacy(true);
        _notificationSettingsReturn?.TrySetResult(true);
    }

    /// <summary>Protects the app when Android Overview shows a live surface without pausing the activity.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("android29.0")]
    public override void OnTopResumedActivityChanged(bool isTopResumedActivity)
    {
        base.OnTopResumedActivityChanged(isTopResumedActivity);
        ApplyForegroundPrivacy(isTopResumedActivity);
    }

    private void ApplyForegroundPrivacy(bool foreground)
    {
        Security.ScreenProtection.SetBackground(!foreground);

        // Overview can keep drawing the live activity even when saved recents screenshots are disabled.
        // Cover the original window content as well as protecting screen capture.
        if (foreground)
        {
            if (_recentsCover is not null) _recentsCover.Visibility = ViewStates.Gone;
            return;
        }

        if (Window?.DecorView is not ViewGroup decor) return;
        if (_recentsCover is null)
        {
            _recentsCover = new Android.Views.View(this) { ImportantForAccessibility = ImportantForAccessibility.No };
            _recentsCover.SetBackgroundColor(Android.Graphics.Color.Black);
            decor.AddView(_recentsCover, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
        }

        _recentsCover.BringToFront();
        _recentsCover.Visibility = ViewStates.Visible;
    }

    /// <summary>Opens this app's Android settings and waits for the same activity to resume.</summary>
    internal async Task<bool> OpenNotificationSettingsAsync()
    {
        if (_notificationSettingsReturn is not null)
        {
            return false;
        }

        var returned = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _notificationSettingsReturn = returned;
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                using var intent = new Intent(Android.Provider.Settings.ActionAppNotificationSettings);
                intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, PackageName);
                StartActivity(intent);
            }
            else
            {
                AppInfo.Current.ShowSettingsUI();
            }

            // Settings does not supply a permission result. Resume lets the caller query
            // the current permission after the user has returned to this original activity.
            return await returned.Task;
        }
        finally
        {
            _notificationSettingsReturn = null;
        }
    }

    /// <inheritdoc />
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
        {
            AndroidConfigurationFonts.ResumePendingReveal(this);
        }
    }

    // Google's consent dialog and Microsoft's browser sign-in return here (cloud backup, D-35), and so does the camera
    // app with a receipt photo (D-38).
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (!AndroidDocumentPicker.OnActivityResult(this, requestCode, resultCode, data)
            && !Interaction.AndroidHouseholdRecoverySave.OnActivityResult(this, requestCode, resultCode, data)
            && !CameraCapture.OnActivityResult(this, requestCode, resultCode))
        {
            Vafadar.Authentication.Maui.SignInActivityResults.OnActivityResult(requestCode, resultCode, data);
        }
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        _notificationSettingsReturn?.TrySetResult(false);
        CameraCapture.OnDestroyed(this);
        AndroidDocumentPicker.OnDestroyed(this);
        Interaction.AndroidHouseholdRecoverySave.OnDestroyed(this);
        base.OnDestroy();
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
#if DEBUG
        if (PackageName == "pro.vafadar.zanance.householdlifecycle" && intent?.GetBooleanExtra("household_recovery_review", false) == true)
            Diagnostics.DebugHouseholdRecoveryAndroid.ApplyInputMode(intent.GetStringExtra("recovery_mode"));
        if (PackageName == "pro.vafadar.zanance.householdlifecycle" && intent?.GetBooleanExtra("household_conflict_review", false) == true)
            Diagnostics.DebugHouseholdConflictAndroid.ApplyMode(intent.GetStringExtra("household_conflict_mode"));
#endif
        OpenFromWidget(intent);
    }

    // The app language chosen in the Android settings (D-32) arrives as a configuration change; the app rebuilds its
    // screens itself, so the activity is not recreated.
    public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
    {
        var densityChanged = _configurationDensityDpi > 0 && newConfig.DensityDpi > 0 &&
            _configurationDensityDpi != newConfig.DensityDpi;
        var fontScaleChanged = _configurationFontScale > 0 && newConfig.FontScale > 0 &&
            _configurationFontScale != newConfig.FontScale;
        if (newConfig.DensityDpi > 0)
        {
            _configurationDensityDpi = newConfig.DensityDpi;
        }

        if (newConfig.FontScale > 0)
        {
            _configurationFontScale = newConfig.FontScale;
        }

        base.OnConfigurationChanged(newConfig);
        if (IPlatformApplication.Current?.Services.GetService<Vafadar.Localization.ILocalizationService>() is { } localization)
        {
            AppLocales.FromSystem(localization);
        }

        QuickAddWidget.Refresh(this);
        UpdateSystemBarIcons();
        // D1192: Android otherwise recreates the window on a font-scale change, losing an unsaved form.
        // Retain the original root and use native SP mapping so Android's nonlinear scaling remains authoritative.
        if (densityChanged || fontScaleChanged)
        {
            AndroidConfigurationFonts.Refresh(this, densityChanged, fontScaleChanged);
        }
    }

    // The status and navigation bars show the page background (Resources/values/styles.xml), so their icons are dark
    // in the light theme and light in the dark one. A theme chosen in the app arrives as a ui mode change.
    private void UpdateSystemBarIcons()
    {
        if (Window is not { } window)
        {
            return;
        }

        var light = (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightMask) != Android.Content.Res.UiMode.NightYes;
        if (AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView) is not { } bars)
        {
            return;
        }

        bars.AppearanceLightStatusBars = light;
        bars.AppearanceLightNavigationBars = light;
    }

    // A tap on the quick add widget (D-30) opens a new entry of that kind, after the app lock.
    private static void OpenFromWidget(Intent? intent)
    {
        if (intent?.Action == QuickAddWidget.Action && intent.GetStringExtra(QuickAddWidget.KindExtra) is ("Expense" or "Income" or "Transfer") and var kind)
        {
            intent.SetAction(null);
            App.OpenLink("entry|" + kind);
        }
    }
}
