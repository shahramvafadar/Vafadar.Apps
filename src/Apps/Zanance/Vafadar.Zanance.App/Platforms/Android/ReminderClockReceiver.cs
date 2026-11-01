using Android.App;
using Android.Content;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Vafadar.Zanance.App.Reminders;

namespace Vafadar.Zanance.App;

/// <summary>Rebinds the existing device queue to a real clock/zone change without opening financial storage or a window.</summary>
[BroadcastReceiver(Name = "pro.vafadar.zanance.ReminderClockReceiver", Enabled = true, Exported = false)]
[IntentFilter([Intent.ActionTimezoneChanged, Intent.ActionTimeChanged])]
public sealed class ReminderClockReceiver : BroadcastReceiver
{
    /// <inheritdoc />
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action is not (Intent.ActionTimezoneChanged or Intent.ActionTimeChanged)) return;
        var wallClockChanged = intent.Action == Intent.ActionTimeChanged;
        var pending = GoAsync();
        _ = Task.Run(async () =>
        {
            try { await RebuildAsync(wallClockChanged); }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // Preserve unprocessed requests on failure. A normal covered source refresh can
                // retry; a broadcast never fabricates financial authority or clears the whole queue.
                System.Diagnostics.Debug.WriteLine($"Reminder clock rebuild failed: {ex.GetType().Name}");
            }
            finally { pending?.Finish(); }
        });
    }

    /// <summary>Changes only pending calendar deadlines and their existing same-day allowance, keeping ids and payloads.</summary>
    internal static async Task RebuildAsync(bool wallClockChanged)
    {
        await LocalNotificationScheduler.QueueGate.WaitAsync();
        try
        {
            TimeZoneInfo.ClearCachedData();
            var zone = TimeZoneInfo.Local;
            var service = LocalNotificationCenter.Current;
            var pending = (await service.GetPendingNotificationList()).Where(request => request.Schedule.NotifyTime is not null && request.Schedule.RepeatType == NotificationRepeat.No)
                .Select(request => (Request: request, Binding: ReminderClockStore.Read(request.NotificationId, request.Schedule.NotifyTime!.Value))).ToArray();
            var accepted = new List<ReminderNativeClockBinding>();
            // Invalidate before any native mutation. After a crash/partial refusal, missing
            // metadata refuses clock reinterpretation until ordinary source refresh republishes it.
            ReminderClockStore.Replace([]);
            foreach (var item in pending)
            {
                var request = item.Request;
                if (item.Binding is not { } binding) continue;
                var original = binding.Scheduled;
                var originalExpiry = original + request.Schedule.Android.AllowedDelay;
                if (!binding.FollowsLocalClock && originalExpiry < DateTimeOffset.UtcNow)
                {
                    service.Cancel(request.NotificationId);
                    continue;
                }
                var elapsedCorrection = wallClockChanged && !binding.FollowsLocalClock && ReminderElapsedClock.IsCurrent(binding.ElapsedDeadline);
                if (!binding.FollowsLocalClock && !elapsedCorrection) { accepted.Add(binding); continue; }
                // Calendar requests keep their wall components. A same-boot elapsed hour instead
                // keeps its actual remaining interval through TIME_SET, never extending its original day.
                var scheduled = elapsedCorrection
                    ? TimeZoneInfo.ConvertTime(ReminderElapsedClock.Resolve(binding.ElapsedDeadline!, original), zone)
                    : ReminderLocalTime.Resolve(original.DateTime, zone);
                if (elapsedCorrection && scheduled > originalExpiry)
                {
                    service.Cancel(request.NotificationId);
                    continue;
                }
                var allowance = elapsedCorrection ? originalExpiry - scheduled : ReminderDeliveryWindow.UntilEndOfDay(scheduled, zone);
                if (scheduled + allowance < DateTimeOffset.Now)
                {
                    service.Cancel(request.NotificationId);
                    continue;
                }
                if (elapsedCorrection && scheduled <= DateTimeOffset.UtcNow) service.Cancel(request.NotificationId);
                request.Schedule.NotifyTime = scheduled;
                request.Schedule.Android.AllowedDelay = allowance;
                await ReminderPublication.RequireAcceptedAsync(service.Show(request));
                accepted.Add(new(request.NotificationId, scheduled, binding.FollowsLocalClock, binding.ElapsedDeadline));
            }
            ReminderClockStore.Replace(accepted);
        }
        finally { LocalNotificationScheduler.QueueGate.Release(); }
    }
}
