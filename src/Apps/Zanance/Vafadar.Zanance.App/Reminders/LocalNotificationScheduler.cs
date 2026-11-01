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
    /// <summary>Serializes native queue mutation against a clock-change receiver in this process.</summary>
    internal static readonly SemaphoreSlim QueueGate = new(1, 1);
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

    /// <inheritdoc />
    public ReminderElapsedDeadline? CaptureHourDeadline()
    {
#if ANDROID
        return ReminderElapsedClock.CaptureHour();
#else
        return null;
#endif
    }

    /// <inheritdoc />
    public DateTimeOffset ResolveHourDeadline(ReminderElapsedDeadline deadline, DateTimeOffset fallback)
    {
#if ANDROID
        return ReminderElapsedClock.Resolve(deadline, fallback);
#else
        return fallback;
#endif
    }

    public Task<bool> AreEnabledAsync() => _service.AreNotificationsEnabled();

    public Task<bool> RequestPermissionAsync() => _service.RequestNotificationPermission(new NotificationPermission
    {
        Android = { RequestPermissionToScheduleExactAlarm = false },
    });

    public async Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> reminders)
    {
        ArgumentNullException.ThrowIfNull(reminders);
        await QueueGate.WaitAsync();
        try { await ReplaceUnderGateAsync(reminders); }
        finally { QueueGate.Release(); }
    }

    private async Task ReplaceUnderGateAsync(IReadOnlyList<ReminderNotification> reminders)
    {
        var requests = reminders.Select(reminder => (Request: Request(reminder), Source: reminder)).ToArray();
#if ANDROID
        // A process may die after SDK acceptance but before source preferences commit. An
        // unbound request must be refused, never classified from stale snooze preferences.
        ReminderClockStore.Replace([]);
#endif
        // Only notifications still waiting are replaced; ones already shown (a reminder, the budget alert) stay in the
        // notification shade until the user clears them.
        var pending = await _service.GetPendingNotificationList();
        if (pending.Count > 0)
        {
            _service.Cancel([.. pending.Select(p => p.NotificationId)]);
        }

        foreach (var item in requests)
            await ReminderPublication.RequireAcceptedAsync(_service.Show(item.Request));
#if ANDROID
        ReminderClockStore.Replace(requests.Where(item => item.Request.Schedule.NotifyTime is not null)
            .Select(item => ClockBinding(item.Request, item.Source)));
#endif
    }

    // The SDK returns false for rejected requests without throwing; never consume a budget level after that result.
    public async Task ShowAsync(ReminderNotification notification)
    {
        await QueueGate.WaitAsync();
        try
        {
            var request = Request(notification);
#if ANDROID
            ReminderClockStore.Remove(request.NotificationId);
#endif
            await ReminderPublication.RequireAcceptedAsync(_service.Show(request));
#if ANDROID
            if (request.Schedule.NotifyTime is not null)
                ReminderClockStore.Put(ClockBinding(request, notification));
#endif
        }
        finally { QueueGate.Release(); }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(int notificationId)
    {
        await QueueGate.WaitAsync();
        try
        {
#if ANDROID
            ReminderClockStore.Remove(notificationId);
#endif
            _service.Cancel(notificationId);
            _service.Clear(notificationId);
        }
        finally { QueueGate.Release(); }
    }

#if ANDROID
    private static ReminderNativeClockBinding ClockBinding(NotificationRequest request, ReminderNotification source) =>
        new(request.NotificationId, request.Schedule.NotifyTime!.Value, source.NotifyInstant is null && !source.PreserveInstant,
            request.Schedule.Android.AlarmType == AndroidAlarmType.ElapsedRealtimeWakeup ? source.ElapsedDeadline : null);
#endif

    /// <summary>Builds the original native request without publishing it or changing financial state.</summary>
    internal static NotificationRequest Request(ReminderNotification reminder)
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
            var scheduled = reminder.NotifyInstant ?? ReminderLocalTime.Resolve(at, TimeZoneInfo.Local);
#if ANDROID
            var elapsed = reminder.NotifyInstant is not null && ReminderElapsedClock.IsCurrent(reminder.ElapsedDeadline);
            if (elapsed) scheduled = ReminderElapsedClock.Resolve(reminder.ElapsedDeadline!, scheduled);
#endif
            if (reminder.DeliveryExpiresAt is { } expiry && (expiry < scheduled || expiry < DateTimeOffset.UtcNow))
                throw new InvalidOperationException("The original snooze delivery day has expired.");
            request.Schedule = new NotificationRequestSchedule
            {
                NotifyTime = scheduled,
                // Android battery restrictions can defer inexact delivery beyond an hour (D-507).
                // The owner accepts it through this original local day, never into tomorrow.
                Android =
                {
                    ScheduleMode = AndroidScheduleMode.InexactAllowWhileIdle,
                    AllowedDelay = reminder.DeliveryExpiresAt is { } end ? end - scheduled : ReminderDeliveryWindow.UntilEndOfDay(scheduled, TimeZoneInfo.Local),
                },
            };
#if ANDROID
            if (elapsed) request.Schedule.Android.AlarmType = AndroidAlarmType.ElapsedRealtimeWakeup;
#endif
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
