#if DEBUG && ANDROID
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Opt-in native review-notification check, restricted to fictitious reminder fixture data.</summary>
internal static class DebugReviewReminders
{
    private sealed record FixtureIds(Guid AccountId, Guid? GoalId = null);

    /// <summary>Runs only when the explicit Debug-only fixture flag is present.</summary>
    public static bool StartIfRequested(App app, IServiceProvider services)
    {
        if (Environment.GetEnvironmentVariable("VAFADAR_REVIEW_REMINDER_PROOF") != "1") { return false; }
        app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            var report = Path.Combine(FileSystem.AppDataDirectory, "review-reminder-proof.json");
            try { await RunAsync(app, services, report); }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { Passed = false, Error = ex.GetType().Name }));
            }
        });
        return true;
    }

    private static async Task RunAsync(App app, IServiceProvider services, string report)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var goals = services.GetRequiredService<GoalStore>();
        var marker = Path.Combine(FileSystem.AppDataDirectory, "review-reminder-fixture.json");
        var firstRun = !File.Exists(marker);
        var identitySource = firstRun ? Path.Combine(FileSystem.AppDataDirectory, "goal-reminder-fixture.json") : marker;
        var ids = File.Exists(identitySource) ? JsonSerializer.Deserialize<FixtureIds>(await File.ReadAllTextAsync(identitySource)) : null;
        var accounts = await store.GetAccountsAsync();
        if (accounts.Any(a => a.Id != ids?.AccountId || a.Name is not ("Reminder fixture account" or "Review fixture account"))
            || (await store.GetEntriesAsync()).Count > 0 || (await goals.GetGoalsAsync()).Any(g => g.Id != ids?.GoalId))
        {
            throw new InvalidOperationException("Only the isolated reminder fixture may be used.");
        }
        if (firstRun)
        {
            var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
            var account = accounts.SingleOrDefault() ?? new Account { Name = "Review fixture account", CurrencyCode = "EUR", Type = AccountType.Cash };
            account.OpeningDate = today.AddMonths(-2);
            ids = new FixtureIds(account.Id, ids?.GoalId);
            await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(ids));
            await store.SaveAccountAsync(account);
            await store.UpdateSettingsAsync(s =>
            {
                s.OnboardingCompleted = true;
                s.ReviewReminderEnabled = true;
                s.MonthStartDay = Math.Clamp(today.Day, 1, 28);
                s.NotificationsShowDetails = false;
            });
        }
        app.ShowMainShell();
        var reminders = services.GetRequiredService<ReminderService>();
        var permission = await reminders.EnsurePermissionAsync();
        var before = await store.GetSettingsAsync();
        await reminders.RefreshAsync();
        var pending = (await LocalNotificationCenter.Current.GetPendingNotificationList()).Where(n => n.ReturningData == "review").ToList();
        var after = await store.GetSettingsAsync();
        var passed = permission && (after.ReviewReminderEnabled ? pending.Count > 0 : pending.Count == 0)
            && pending.All(n => n.Schedule?.NotifyTime?.Hour == 9 && n.Title == "Zanance"
                && !(n.Description ?? string.Empty).Contains("fixture", StringComparison.OrdinalIgnoreCase))
            && before.ReviewProgress == after.ReviewProgress && (await store.GetEntriesAsync()).Count == 0;
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new
        {
            Passed = passed, PermissionEnabled = permission, ReminderEnabled = after.ReviewReminderEnabled,
            PendingCount = pending.Count, Dates = pending.Select(n => n.Schedule?.NotifyTime),
            ReviewUnchanged = before.ReviewProgress == after.ReviewProgress, LedgerCount = (await store.GetEntriesAsync()).Count,
            DeliveryExpedited = firstRun,
        }));
        if (passed && firstRun && pending.Count > 0)
        {
            var first = pending[0];
            // Expedite the fixture only; the normal future 09:00 schedule was checked above. This is not Doze timing evidence.
            // A queued refresh cancels pending ids, including a just-expedited request with that same id. Give the
            // diagnostic its own id so ordinary queue rebuilds cannot clear the delivered tap target.
            await reminders.Scheduler.ShowAsync(new ReminderNotification(first.NotificationId ^ 0x2000_0000, first.Title ?? string.Empty,
                first.Description ?? string.Empty, null, "review"));
        }
        await Shell.Current.GoToAsync(AppShell.SettingsRoute);
    }
}
#endif
