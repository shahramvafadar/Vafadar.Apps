#if ANDROID || IOS
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using Plugin.LocalNotification.Core.Models.AppleOption;
using Plugin.LocalNotification.EventArgs;
using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.App.Reminders;

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
    private readonly NotificationCategory _snoozeCategory = new(NotificationCategoryType.Reminder);
#if ANDROID
    private bool _categoryRegistered;
#endif

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
        // Plugin.LocalNotification 14.1.2 on Android appends categories and reads the first matching instance (D-140). Retain that instance;
        // replace its complete action list so live translations never select an earlier registered caption.
        _snoozeCategory.ActionList = [Action(SnoozeHourAction, oneHour), Action(SnoozeTomorrowAction, tomorrow)];
#if ANDROID
        if (_categoryRegistered) return;
#endif
        _service.RegisterCategoryList([_snoozeCategory]);
#if ANDROID
        _categoryRegistered = true;
#endif
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
        // Only notifications still waiting are replaced; ones already shown (a reminder, the budget alert) stay in the
        // notification shade until the user clears them.
        var pending = await _service.GetPendingNotificationList();
        if (pending.Count > 0)
        {
            _service.Cancel([.. pending.Select(p => p.NotificationId)]);
        }

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

        // Android status bar: the white Zanance silhouette (only its alpha is used) with the brand colour as accent (D-26).
        request.Android.IconSmallName = new AndroidIcon("ic_stat_zanance");
        request.Android.Color = new AndroidColor(unchecked((int)0xFF0C44A8));

        if (reminder.NotifyAt is { } at)
        {
            request.Schedule = new NotificationRequestSchedule
            {
                NotifyTime = at,
                // Android can deliver an inexact alarm more than a minute late (D-140). The plugin's default
                // one-minute rejection window must not discard normal OS delivery; keep a bounded one-hour window.
                Android =
                {
                    ScheduleMode = AndroidScheduleMode.InexactAllowWhileIdle,
                    AllowedDelay = TimeSpan.FromHours(1),
                },
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
