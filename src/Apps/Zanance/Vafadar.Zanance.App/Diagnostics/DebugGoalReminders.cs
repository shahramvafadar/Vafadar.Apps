#if DEBUG && ANDROID
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Opt-in Android notification fixture, restricted to an empty installation or its own single account.</summary>
internal static class DebugGoalReminders
{
    private sealed record FixtureIds(Guid AccountId, Guid GoalId);

    /// <summary>Schedules a fixture only when its explicit Debug environment flag is present.</summary>
    public static bool StartIfRequested(App app, IServiceProvider services)
    {
        if (Environment.GetEnvironmentVariable("VAFADAR_GOAL_REMINDER_PROOF") != "1") { return false; }
        app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            var file = Path.Combine(FileSystem.AppDataDirectory, "goal-reminder-proof.json");
            try
            {
                await RunAsync(app, services, file);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                await File.WriteAllTextAsync(file, JsonSerializer.Serialize(new { Passed = false, Error = ex.GetType().Name }));
            }
        });
        return true;
    }

    private static async Task RunAsync(App app, IServiceProvider services, string file)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var goals = services.GetRequiredService<GoalStore>();
        var marker = Path.Combine(FileSystem.AppDataDirectory, "goal-reminder-fixture.json");
        var ids = File.Exists(marker) ? JsonSerializer.Deserialize<FixtureIds>(await File.ReadAllTextAsync(marker)) : null;
        var accounts = await store.GetAccountsAsync();
        if (accounts.Any(a => a.Id != ids?.AccountId) || (await store.GetEntriesAsync()).Count > 0
            || (await goals.GetGoalsAsync()).Any(g => g.Id != ids?.GoalId))
        {
            throw new InvalidOperationException("The fixture requires an empty installation.");
        }

        var firstRun = accounts.Count == 0;
        var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        if (firstRun)
        {
            var account = new Account
            {
                Name = "Reminder fixture account", Type = AccountType.Savings, CurrencyCode = "EUR", OpeningDate = today,
            };
            var fixtureGoal = new Goal
            {
                Name = "Reminder fixture goal", Type = GoalType.AccountBalance,
                AccountId = account.Id, CurrencyCode = "EUR", TargetAmount = 100_00,
            };
            ids = new FixtureIds(account.Id, fixtureGoal.Id);
            await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(ids));
            await store.SaveAccountAsync(account);
            await goals.SaveGoalAsync(fixtureGoal);
            await goals.SaveContributionPlanAsync(ids.GoalId, new ContributionPlan
            {
                ReminderEnabled = true, Amount = 25_00,
                Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = today.AddDays(1) },
            });
            var settings = await store.GetSettingsAsync();
            settings.OnboardingCompleted = true;
            settings.NotificationsShowDetails = false;
            await store.SaveSettingsAsync(settings);
        }

        if (ids is null) { throw new InvalidOperationException("The fixture identity is missing."); }
        // This installation contains only the fixture. Permission still comes from the ordinary system dialog.
        app.ShowMainShell();
        var reminders = services.GetRequiredService<ReminderService>();
        var enabled = await reminders.EnsurePermissionAsync();
        await reminders.RefreshAsync();
        var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
        var scheduled = pending.Where(p => p.ReturningData == $"goal|{ids.GoalId}").ToList();
        var goal = await goals.GetGoalAsync(ids.GoalId);
        var eligible = goal?.State == GoalState.Active
            && (await goals.GetContributionPlansAsync()).Any(p => p.GoalId == ids.GoalId && p.ReminderEnabled);
        var passed = enabled && (eligible ? scheduled.Count > 0 : scheduled.Count == 0)
            && scheduled.All(p => p.Schedule?.NotifyTime?.Hour == 9 && p.Title == "Zanance"
                && !(p.Description ?? string.Empty).Contains("fixture", StringComparison.OrdinalIgnoreCase))
            && (await store.GetEntriesAsync()).Count == 0 && (await goals.GetAllocationsAsync()).Count == 0;
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(new
        {
            Passed = passed, PermissionEnabled = enabled, GoalActive = eligible, PendingGoalCount = scheduled.Count,
            Dates = scheduled.Select(p => p.Schedule?.NotifyTime), LedgerCount = (await store.GetEntriesAsync()).Count,
            DeliveryExpedited = firstRun,
        }));

        if (passed && firstRun)
        {
            var first = scheduled[0];
            // Expedite just this fictitious notification for a UI tap check, without changing the device clock.
            // Its normal 9:00 schedule was checked above; this does not prove exact-time or battery-policy delivery.
            await reminders.Scheduler.ShowAsync(new ReminderNotification(first.NotificationId, first.Title ?? string.Empty,
                first.Description ?? string.Empty, null, first.ReturningData ?? string.Empty));
        }
        await Shell.Current.GoToAsync(AppShell.GoalsRoute);
    }
}
#endif
