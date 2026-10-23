using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Localization;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Qualifies original native goal queues and privacy on one isolated, fictional protected installation.</summary>
internal static class NativeGoalReminderReview
{
    private static string Root => Path.Combine(FileSystem.AppDataDirectory, "goal-reminder-review");
    private static string Receipt => Path.Combine(Root, "receipt.json");
    private sealed record Identity(string Run, Guid AccountId, Guid GoalId, DateOnly Date);

    /// <summary>Returns only synthetic progress already written by this foreground caller.</summary>
    internal static string ReadReceipt()
    {
        RequirePackage();
        return File.Exists(Receipt) ? File.ReadAllText(Receipt) : "{\"Stage\":\"starting\"}";
    }

    /// <summary>Runs only unfinished metadata transitions; financial ledger and allocation rows must stay empty.</summary>
    internal static async Task PrepareAsync(IServiceProvider services, string run)
    {
        RequirePackage();
        Directory.CreateDirectory(Root);
        var observations = new List<object>();
        async Task Report(string stage, bool completed = false, string? error = null) =>
            await File.WriteAllTextAsync(Receipt, JsonSerializer.Serialize(new
            { Run = run, Pid = Android.OS.Process.MyPid(), Stage = stage, Completed = completed, ErrorType = error, Observations = observations }));
        try
        {
            if (File.Exists(Receipt))
            {
                using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(Receipt));
                if (retained.RootElement.TryGetProperty("Completed", out var finished) && finished.GetBoolean())
                    return; // Keep the complete original receipt; never reseed or replay a successful native cohort.
            }
            var store = services.GetRequiredService<ZananceStore>();
            var goals = services.GetRequiredService<GoalStore>();
            var reminders = services.GetRequiredService<ReminderService>();
            var text = services.GetRequiredService<Translator>();
            await Report("permission");
            if (!await reminders.EnsurePermissionAsync()) throw new IOException("Original notification permission required.");

            var marker = Path.Combine(Root, "identity.json");
            Identity ids;
            if (!File.Exists(marker))
            {
                if ((await store.GetAccountsAsync()).Count != 0 || (await goals.GetGoalsAsync()).Count != 0
                    || (await store.GetEntriesAsync()).Count != 0 || (await services.GetRequiredService<PlanStore>().GetSchedulesAsync()).Count != 0)
                    throw new IOException("Empty independent goal fixture required.");
                var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                var account = new Account { Name = "Goal reminder fixture account", Type = AccountType.Savings,
                    CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 10000 };
                var goal = new Goal { Name = "Goal reminder fixture goal", Type = GoalType.AccountBalance,
                    AccountId = account.Id, CurrencyCode = "EUR", TargetAmount = 15000, Note = "Fictitious reminder only" };
                ids = new(run, account.Id, goal.Id, today.AddDays(1));
                // Persist ownership before setup. A partial initialization is refused, never silently reseeded.
                await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(ids));
                await store.SaveAccountAsync(account);
                await goals.SaveGoalAsync(goal);
                await goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan { Amount = 2500,
                    Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = ids.Date }, ReminderEnabled = false });
                await store.UpdateSettingsAsync(settings =>
                { settings.OnboardingCompleted = true; settings.NotificationsShowDetails = false; });
            }
            else ids = JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(marker)) ?? throw new IOException("Original identity missing.");
            if (ids.Run != run) throw new IOException("Retain original run identity.");
            var accountPacket = JsonSerializer.Serialize((await store.GetAccountsAsync()).Single());

            async Task Check(string stage, bool expectedPending, bool details = false)
            {
                var file = Path.Combine(Root, stage + ".json");
                if (File.Exists(file))
                {
                    observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(file)));
                    return;
                }
                await reminders.PendingRefresh;
                await reminders.RefreshAsync();
                var all = await LocalNotificationCenter.Current.GetPendingNotificationList();
                var pending = all.Where(item => GoalReminderLink.Parse(item.ReturningData) is not null).ToArray();
                if (all.Count != pending.Length || pending.Length != (expectedPending ? 1 : 0))
                    throw new IOException("Original goal native queue count differs.");
                var current = await goals.GetGoalAsync(ids.GoalId) ?? throw new IOException("Original goal missing.");
                var contribution = (await goals.GetContributionPlansAsync()).Single();
                if (JsonSerializer.Serialize((await store.GetAccountsAsync()).Single()) != accountPacket
                    || (await store.GetEntriesAsync()).Count != 0 || (await goals.GetAllocationsAsync()).Count != 0
                    || (await services.GetRequiredService<PlanStore>().GetSchedulesAsync()).Count != 0
                    || current.Name != "Goal reminder fixture goal" || current.AccountId != ids.AccountId
                    || current.Type != GoalType.AccountBalance || current.CurrencyCode != "EUR"
                    || current.Note != "Fictitious reminder only" || contribution.GoalId != ids.GoalId
                    || contribution.Amount != 2500 || contribution.Rule.Frequency != Frequency.Once || contribution.Rule.Start != ids.Date)
                    throw new IOException("Original fictional financial source changed.");
                if (pending.Length == 1)
                {
                    var first = pending[0];
                    var scope = (await goals.GetWorkSnapshotAsync()).ReminderScope;
                    var link = GoalReminderLink.Parse(first.ReturningData) ?? throw new IOException("Original goal route missing.");
                    if (first.NotificationId != GoalReminderPlanner.StableId(ids.GoalId, ids.Date)
                        || first.Schedule?.NotifyTime != ids.Date.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local)
                        || link.Scope != scope || link.Targets.Count != 1 || link.Targets[0].GoalId != ids.GoalId
                        || link.Targets[0].OriginalDate != ids.Date || first.Title != (details ? current.Name : text["App_Name"])
                        || (!details && first.Description != text["Reminder_GoalGeneric"])
                        || (details && (first.Description == text["Reminder_GoalGeneric"] || string.IsNullOrWhiteSpace(first.Description))))
                        throw new IOException("Original native goal time, scoped identity or privacy differs.");
                }
                var result = new { Stage = stage, Pending = pending.Length, GoalState = current.State.ToString(),
                    Target = current.TargetAmount, ReminderEnabled = contribution.ReminderEnabled,
                    Generic = !details, NotifyAt = pending.SingleOrDefault()?.Schedule?.NotifyTime,
                    FinancialRowsPreserved = true, LedgerEntries = 0, Allocations = 0 };
                await File.WriteAllTextAsync(file, JsonSerializer.Serialize(result));
                observations.Add(result);
                await Report(stage);
            }
            async Task Transition(string stage, GoalState state, long target, bool enabled, bool details = false)
            {
                if (File.Exists(Path.Combine(Root, stage + ".json"))) { await Check(stage, enabled && state == GoalState.Active && target > 10000, details); return; }
                var goal = await goals.GetGoalAsync(ids.GoalId) ?? throw new IOException("Original goal missing.");
                if (goal.State != state || goal.TargetAmount != target)
                { goal.State = state; goal.TargetAmount = target; await goals.SaveGoalAsync(goal); }
                var plan = (await goals.GetContributionPlansAsync()).Single();
                if (plan.ReminderEnabled != enabled) { plan.ReminderEnabled = enabled; await goals.SaveContributionPlanAsync(ids.GoalId, plan); }
                var settings = await store.GetSettingsAsync();
                if (settings.NotificationsShowDetails != details)
                    await store.UpdateSettingsAsync(value => value.NotificationsShowDetails = details);
                await Check(stage, enabled && state == GoalState.Active && target > 10000, details);
            }
            await Check("01-default-off", false);
            await Transition("02-enabled", GoalState.Active, 15000, true);
            await Transition("03-paused", GoalState.Paused, 15000, true);
            await Transition("04-resumed", GoalState.Active, 15000, true);
            await Transition("05-completed", GoalState.Completed, 15000, true);
            await Transition("06-archived", GoalState.Archived, 15000, true);
            await Transition("07-reached", GoalState.Active, 10000, true);
            await Transition("08-unreached", GoalState.Active, 15000, true);
            await Transition("09-details", GoalState.Active, 15000, true, true);
            await Transition("10-disabled", GoalState.Active, 15000, false);
            await Transition("11-generic-restored", GoalState.Active, 15000, true);
            await Report("complete", true);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            await Report("failed", error: error.GetType().Name);
            throw;
        }
    }

    /// <summary>Rejects native effects and reads outside the independent reminder identity.</summary>
    private static void RequirePackage()
    {
        if (AppInfo.Current.PackageName != NativeReminderReview.Package) throw new IOException("Independent goal reminder identity required.");
    }
}
