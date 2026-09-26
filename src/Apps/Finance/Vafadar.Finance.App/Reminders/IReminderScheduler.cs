namespace Vafadar.Finance.App.Reminders;

/// <summary>A notification to hand to the platform.</summary>
/// <param name="Id">Stable id.</param>
/// <param name="Title">Title – generic unless the user allowed details (REM-05).</param>
/// <param name="Body">Text – generic unless the user allowed details (REM-05).</param>
/// <param name="NotifyAt">Local time; <see langword="null"/> shows it now.</param>
/// <param name="Link">Where a tap leads, e.g. <c>occurrence|{plan}|{date}</c> (REM-04).</param>
/// <param name="CanSnooze">Whether the notification offers "In 1 hour" and "Tomorrow" (REM-04).</param>
public sealed record ReminderNotification(int Id, string Title, string Body, DateTime? NotifyAt, string Link, bool CanSnooze = false);

/// <summary>A snooze action chosen on a notification.</summary>
public sealed record SnoozeRequest(ReminderNotification Notification, Vafadar.Finance.Core.Reminders.SnoozeChoice Choice);

/// <summary>Device notifications. Everything else in the app works without them (REM-02, AT-34).</summary>
public interface IReminderScheduler
{
    /// <summary>Raised with the link of a notification the user tapped.</summary>
    event EventHandler<string>? Tapped;

    /// <summary>Raised when the user snoozes a notification; the app is not necessarily opened.</summary>
    event EventHandler<SnoozeRequest>? SnoozeRequested;

    /// <summary>Sets the titles of the snooze actions in the current language.</summary>
    void SetSnoozeActions(string oneHour, string tomorrow);

    /// <summary>Gets a value indicating whether this platform delivers notifications.</summary>
    bool IsSupported { get; }

    /// <summary>Returns whether notifications are currently allowed.</summary>
    Task<bool> AreEnabledAsync();

    /// <summary>Asks for permission – only when the user turns a reminder on (REM-03).</summary>
    Task<bool> RequestPermissionAsync();

    /// <summary>Replaces all scheduled reminders of the app by <paramref name="reminders"/>; duplicates cannot remain (REM-07).</summary>
    Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> reminders);

    /// <summary>Shows one notification now (e.g. a budget alert).</summary>
    Task ShowAsync(ReminderNotification notification);
}

/// <summary>Platforms without local notifications in phase 1 (Windows): reminders live in the app's due centre.</summary>
internal sealed class NoReminderScheduler : IReminderScheduler
{
    public event EventHandler<string>? Tapped
    {
        add { }
        remove { }
    }

    public event EventHandler<SnoozeRequest>? SnoozeRequested
    {
        add { }
        remove { }
    }

    public void SetSnoozeActions(string oneHour, string tomorrow)
    {
    }

    public bool IsSupported => false;

    public Task<bool> AreEnabledAsync() => Task.FromResult(false);

    public Task<bool> RequestPermissionAsync() => Task.FromResult(false);

    public Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> reminders) => Task.CompletedTask;

    public Task ShowAsync(ReminderNotification notification) => Task.CompletedTask;
}
