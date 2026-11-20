namespace Vafadar.Zanance.App.Security;

/// <summary>Device-wide Android screenshot preference; recent-apps protection remains unconditional (D-63).</summary>
public static class ScreenProtection
{
    private const string Key = "security.blockScreenshots";
    private static bool _background;

    /// <summary>Gets whether screenshots are blocked; new installations keep the privacy-preserving default.</summary>
    public static bool BlockScreenshots => RequiresLegacyProtection || Preferences.Default.Get(Key, true);

    // Before API 33, Android can capture the current task before activity callbacks run.
    internal static bool RequiresLegacyProtection => OperatingSystem.IsAndroid() && !OperatingSystem.IsAndroidVersionAtLeast(33);

#if ANDROID
    /// <summary>Includes owned MAUI modal windows in the original device screenshot policy (D-726).</summary>
    internal static void RegisterNativePages() => AndroidPageScreenProtection.Register(_background || BlockScreenshots);
#endif

    /// <summary>Saves and immediately applies the foreground screenshot choice.</summary>
    public static void Set(bool block)
    {
        if (RequiresLegacyProtection) return;
        Preferences.Default.Set(Key, block);
        Apply();
    }

    /// <summary>Applies protection before Android takes its background snapshot, or restores the foreground choice.</summary>
    public static void SetBackground(bool background)
    {
        _background = background;
        Apply();
    }

    /// <summary>Applies the saved choice to the current Android activity.</summary>
    public static void Apply()
    {
#if ANDROID
        if (!MainThread.IsMainThread) { MainThread.BeginInvokeOnMainThread(Apply); return; }
        var window = Platform.CurrentActivity?.Window;
        var block = _background || BlockScreenshots;
        if (block) { window?.AddFlags(Android.Views.WindowManagerFlags.Secure); }
        else { window?.ClearFlags(Android.Views.WindowManagerFlags.Secure); }
        AndroidPageScreenProtection.Apply(block);
#endif
    }
}
