#if ANDROID || IOS
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using Plugin.LocalNotification.EventArgs;

namespace Vafadar.Finance.App.Reminders;

/// <summary>
/// Local notifications on Android and iOS (Plugin.LocalNotification, MIT). Reminders use inexact scheduling, so the app
/// needs no exact-alarm permission and never promises a reminder to the second (REM-09).
/// </summary>
internal sealed class LocalNotificationScheduler : IReminderScheduler
{
    private readonly INotificationService _service = LocalNotificationCenter.Current;

    public LocalNotificationScheduler()
    {
        _service.NotificationActionTapped += OnTapped;
    }

    public event EventHandler<string>? Tapped;

    public bool IsSupported => _service.IsSupported;

    public Task<bool> AreEnabledAsync() => _service.AreNotificationsEnabled();

    public Task<bool> RequestPermissionAsync() => _service.RequestNotificationPermission(new NotificationPermission
    {
        Android = { RequestPermissionToScheduleExactAlarm = false },
    });

    public async Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> reminders)
    {
        ArgumentNullException.ThrowIfNull(reminders);
        _service.CancelAll();
        foreach (var reminder in reminders)
        {
            await _service.Show(Request(reminder));
        }
    }

    public Task ShowAsync(ReminderNotification notification) => _service.Show(Request(notification));

    private static NotificationRequest Request(ReminderNotification reminder)
    {
        var request = new NotificationRequest
        {
            NotificationId = reminder.Id,
            Title = reminder.Title,
            Description = reminder.Body,
            ReturningData = reminder.Link,
        };

        if (reminder.NotifyAt is { } at)
        {
            request.Schedule = new NotificationRequestSchedule
            {
                NotifyTime = at,
                Android = { ScheduleMode = AndroidScheduleMode.InexactAllowWhileIdle },
            };
        }

        return request;
    }

    private void OnTapped(NotificationActionEventArgs e)
    {
        if (e.IsTapped && e.Request?.ReturningData is { Length: > 0 } link)
        {
            Tapped?.Invoke(this, link);
        }
    }
}
#endif
