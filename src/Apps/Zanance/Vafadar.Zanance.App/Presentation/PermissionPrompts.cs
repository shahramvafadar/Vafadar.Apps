using System.Diagnostics;
using Vafadar.Localization;
using Vafadar.Zanance.App.Reminders;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Asks for system permissions the way each platform expects (D-38): in context, with a short explanation before a
/// system dialog that can be answered only once, and a way into the system settings once the system no longer asks.
/// The texts of the system dialogs themselves are the platform's (Android) or our usage descriptions (iOS Info.plist).
/// </summary>
internal static class PermissionPrompts
{
    private const string NotificationsOfferedKey = "permissions.notifications_offered";

    // A system dialog takes the user a moment to answer; a refusal faster than this came without one (blocked before).
    private static readonly TimeSpan AnsweredWithoutDialog = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// Offers reminders once per device after the first start: an explanation first, then the system dialog (Android 13
    /// and later, iOS). A "no" is respected; Plans and Settings offer to turn reminders on later.
    /// </summary>
    public static async Task OfferNotificationsAsync(ReminderService reminders, Translator translator)
    {
        if (!reminders.Scheduler.IsSupported || Preferences.Default.Get(NotificationsOfferedKey, false))
        {
            return;
        }

        if (await reminders.Scheduler.AreEnabledAsync())
        {
            // Allowed already (e.g. Android before 13 allows notifications by default).
            Preferences.Default.Set(NotificationsOfferedKey, true);
            return;
        }

        if (Shell.Current is not { } shell)
        {
            return;
        }

        Preferences.Default.Set(NotificationsOfferedKey, true);
        if (await shell.DisplayAlertAsync(translator["Notify_OfferTitle"], translator["Notify_OfferText"], translator["Notify_Allow"], translator["Notify_NotNow"])
            && await reminders.EnsurePermissionAsync())
        {
            reminders.RefreshSoon();
        }
    }

    /// <summary>
    /// The user turns notifications on (Plans, Settings): the system asks while it still can; once it no longer shows
    /// its dialog, the notification settings of the app open after a short explanation. Returns whether they are on.
    /// </summary>
    public static async Task<bool> EnableNotificationsAsync(ReminderService reminders, Translator translator)
    {
        var watch = Stopwatch.StartNew();
        if (await reminders.EnsurePermissionAsync())
        {
            reminders.RefreshSoon();
            return true;
        }

        if (reminders.Scheduler.IsSupported && watch.Elapsed < AnsweredWithoutDialog && Shell.Current is { } shell
            && await shell.DisplayAlertAsync(translator["Notify_OffTitle"], translator["Notify_OffText"], translator["Common_OpenSettings"], translator["Common_Cancel"]))
        {
            OpenNotificationSettings();
        }

        return false;
    }

    /// <summary>Opens the notification settings of the app (Android 8 and later), otherwise the app's settings page.</summary>
    public static void OpenNotificationSettings()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && Platform.CurrentActivity is { } activity)
        {
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionAppNotificationSettings);
            intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, activity.PackageName);
            activity.StartActivity(intent);
            return;
        }
#endif
        AppInfo.Current.ShowSettingsUI();
    }

    /// <summary>
    /// Gets a value indicating whether "Take a photo" is offered: on phones with a camera. Windows offers files only,
    /// receipts there are scans or PDFs.
    /// </summary>
    public static bool CanTakePhoto =>
#if ANDROID
        CameraCapture.IsAvailable;
#elif IOS
        MediaPicker.Default.IsCaptureSupported;
#else
        false;
#endif

    /// <summary>
    /// Takes a photo with the camera and returns its content, or <see langword="null"/> when the user cancelled or the
    /// camera may not be used. Android uses the system camera app, which needs no permission of Zanance; iOS asks with
    /// the usage description of Info.plist, and leads to the settings once access was refused.
    /// </summary>
    public static async Task<byte[]?> TakePhotoAsync(Translator translator)
    {
#if ANDROID
        try
        {
            return await CameraCapture.TakePhotoAsync();
        }
        catch (Android.Content.ActivityNotFoundException)
        {
            await Shell.Current.DisplayAlertAsync(translator["Receipt_Title"], translator["Camera_NotAvailable"], translator["Common_Ok"]);
            return null;
        }
#elif IOS
        if (await Permissions.CheckStatusAsync<Permissions.Camera>() == PermissionStatus.Denied)
        {
            // iOS asks only once; after a refusal only the settings can allow the camera.
            if (await Shell.Current.DisplayAlertAsync(translator["Camera_OffTitle"], translator["Camera_OffText"], translator["Common_OpenSettings"], translator["Common_Cancel"]))
            {
                AppInfo.Current.ShowSettingsUI();
            }

            return null;
        }

        try
        {
            if (await MediaPicker.Default.CapturePhotoAsync() is not { } file)
            {
                return null;
            }

            await using var stream = await file.OpenReadAsync();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return buffer.ToArray();
        }
        catch (PermissionException)
        {
            // Refused in the system dialog just now: respected without a second question.
            return null;
        }
#else
        await Task.CompletedTask;
        _ = translator;
        return null;
#endif
    }
}
