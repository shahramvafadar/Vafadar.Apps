#if ANDROID || IOS
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using Plugin.LocalNotification.Core.Models.AppleOption;
using Vafadar.Finance.Core.Reminders;
using Plugin.LocalNotification.EventArgs;

namespace Vafadar.Finance.App.Reminders;

/// <summary>
/// Local notifications on Android and iOS (Plugin.LocalNotification, MIT). Reminders use inexact scheduling, so the app
/// needs no exact-alarm permission and never promises a reminder to the second (REM-09).
/// </summary>
internal sealed class LocalNotificationScheduler : IReminderScheduler
{
    private const int SnoozeHourAction = 101;
    private const int SnoozeTomorrowAction = 102;
    private readonly INotificationService _service = LocalNotificationCenter.Current;
    private string _oneHour = "+1 h";
    private string _tomorrow = "+1 d";

    public LocalNotificationScheduler()
    {
        _service.NotificationActionTapped += OnTapped;
    }

    public event EventHandler<string>? Tapped;

    public event EventHandler<SnoozeRequest>? SnoozeRequested;

    // Actions work in the background: snoozing never opens the app and never changes a due date (REM-04).
    public void SetSnoozeActions(string oneHour, string tomorrow)
    {
        if (oneHour == _oneHour && tomorrow == _tomorrow)
        {
            return;
        }

        _oneHour = oneHour;
        _tomorrow = tomorrow;
        _service.RegisterCategoryList(
        [
            new NotificationCategory(NotificationCategoryType.Reminder)
            {
                ActionList =
                [
                    Action(SnoozeHourAction, oneHour),
                    Action(SnoozeTomorrowAction, tomorrow),
                ],
            },
        ]);
    }

    private static NotificationAction Action(int id, string title) => new(id)
    {
        Title = title,
        Android = { LaunchAppWhenTapped = false },
        Apple = { Action = AppleActionType.None },
    };

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
            CategoryType = reminder.CanSnooze ? NotificationCategoryType.Reminder : NotificationCategoryType.None,
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
        if (e.Request is not { ReturningData: { Length: > 0 } link } request)
        {
            return;
        }

        if (e.ActionId is SnoozeHourAction or SnoozeTomorrowAction)
        {
            _service.Clear(request.NotificationId);
            var notification = new ReminderNotification(request.NotificationId, request.Title ?? string.Empty, request.Description ?? string.Empty, null, link, CanSnooze: true);
            SnoozeRequested?.Invoke(this, new SnoozeRequest(notification, e.ActionId == SnoozeTomorrowAction ? SnoozeChoice.Tomorrow : SnoozeChoice.OneHour));
        }
        else if (e.IsTapped)
        {
            Tapped?.Invoke(this, link);
        }
    }
}
#endif
