using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;
using Vafadar.Zanance.App.Reminders;

namespace Vafadar.Zanance.DeliveryReview;

/// <summary>Runs only notification publication through original production sources; it has no financial store.</summary>
public sealed class DeliveryApplication : Application
{
    /// <summary>The isolated package accepted by every native entry point.</summary>
    internal const string Package = "pro.vafadar.zanance.reminderdeliveryreview";
    /// <summary>The original explicit run selected before window creation.</summary>
    internal static string? Run { get; set; }
    private readonly Label _status = new() { Text = "Notification-only review; no accounts or transactions." };

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var button = new Button { Text = "Publish same-day delivery checks" };
        button.Clicked += async (_, _) =>
        {
            button.IsEnabled = false;
            try { await PublishAsync(); _status.Text = "Scheduled; observe original native receiver and notification records."; }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                File.WriteAllText(Path.Combine(FileSystem.AppDataDirectory, "failure.json"), JsonSerializer.Serialize(new { Error = error.GetType().Name }));
                _status.Text = error.GetType().Name;
            }
        };
        return new Window(new ContentPage { Content = new VerticalStackLayout { Padding = 24, Children = { _status, button } } });
    }

    /// <summary>Publishes two deliberately late original requests once, without reseeding any financial data.</summary>
    private static async Task PublishAsync()
    {
        if (Android.App.Application.Context.PackageName != Package || Run is null || !Regex.IsMatch(Run, "^[0-9a-f]{32}$"))
            throw new InvalidOperationException("Explicit independent run required.");
        var root = FileSystem.AppDataDirectory;
        if (File.Exists(Path.Combine(root, "started.json"))) throw new InvalidOperationException("Preserve started publication; never replay it.");
        var scheduler = new LocalNotificationScheduler();
        if (!await scheduler.AreEnabledAsync() && !await scheduler.RequestPermissionAsync())
            throw new InvalidOperationException("Original native notification permission required.");
        var now = TimeProvider.System.GetLocalNow();
        var due = DateTime.SpecifyKind(now.Date.AddHours(9), DateTimeKind.Local);
        if (now - new DateTimeOffset(due) <= TimeSpan.FromHours(1))
            throw new InvalidOperationException("Real same-day delay beyond one hour required; never change the clock.");
        var reminders = new[]
        {
            new ReminderNotification(19994001, "Zanance", "A goal contribution is due. Open Zanance to review it.", due, "review-goal"),
            new ReminderNotification(19994002, "Zanance", "Your period review is ready when you are.", due, "review"),
        };
        var validations = reminders.Select(value =>
        {
            var original = LocalNotificationScheduler.Request(value);
            var midnight = new DateTimeOffset(DateTime.SpecifyKind(due.Date.AddDays(1), DateTimeKind.Local));
            return new
            {
                value.Id, Due = original.Schedule.NotifyTime, original.Schedule.Android.AllowedDelay,
                AcceptsRealLateTime = original.Schedule.Android.IsValidNotifyTime(now, original.Schedule.NotifyTime),
                AcceptsLastDayTick = original.Schedule.Android.IsValidNotifyTime(midnight.AddTicks(-1), original.Schedule.NotifyTime),
                RejectsNextDay = !original.Schedule.Android.IsValidNotifyTime(midnight, original.Schedule.NotifyTime),
            };
        }).ToArray();
        if (validations.Any(value => !value.AcceptsRealLateTime || !value.AcceptsLastDayTick || !value.RejectsNextDay))
            throw new InvalidOperationException("Original request/SDK calendar-day boundaries differ.");
        File.WriteAllText(Path.Combine(root, "started.json"), JsonSerializer.Serialize(new { Run, Now = now, Requests = validations }));
        foreach (var reminder in reminders)
        {
            await scheduler.ShowAsync(reminder);
            // A separate receipt prevents replaying the first accepted request after a later observation failure.
            File.WriteAllText(Path.Combine(root, $"accepted-{reminder.Id}.json"), JsonSerializer.Serialize(new { Run, reminder.Id }));
        }
        var expired = reminders[0] with { Id = 19994003, NotifyAt = due.AddDays(-1) };
        var refused = false;
        try { await scheduler.ShowAsync(expired); }
        catch (InvalidOperationException) { refused = true; }
        if (!refused) throw new InvalidOperationException("Yesterday's original request was accepted.");
        File.WriteAllText(Path.Combine(root, "receipt.json"), JsonSerializer.Serialize(new { Run, Now = now, Requests = validations, YesterdayRefused = true, FinancialCommands = 0 }));
    }
}
