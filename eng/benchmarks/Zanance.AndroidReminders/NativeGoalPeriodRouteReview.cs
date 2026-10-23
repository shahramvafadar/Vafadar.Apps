using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Qualifies actual notification content taps to original goal and current-review pages on fictional protected data.</summary>
internal static class NativeGoalPeriodRouteReview
{
    private static string Root => Path.Combine(FileSystem.AppDataDirectory, "goal-period-route-review");
    private sealed record Identity(string Run, Guid Account, Guid Goal, DateOnly Date);

    /// <summary>Publishes only an explicit unfinished synthetic route copy, retaining original financial dates and payload.</summary>
    internal static async Task PublishAsync(IServiceProvider services, string run, string phase)
    {
        RequirePackage(); Directory.CreateDirectory(Root);
        try
        {
            if (phase is not ("goal" or "review")) throw new IOException("Explicit original route phase required.");
            var completed = Path.Combine(Root, phase + "-posted.json");
            if (File.Exists(completed)) throw new IOException("Accepted route notification must not be republished.");
            var store = services.GetRequiredService<ZananceStore>(); var goals = services.GetRequiredService<GoalStore>();
            var reminders = services.GetRequiredService<ReminderService>();
            if (!await reminders.EnsurePermissionAsync()) throw new IOException("Original native notification permission required.");
            var marker = Path.Combine(Root, "identity.json"); Identity ids;
            if (!File.Exists(marker))
            {
                if (phase != "goal" || (await store.GetAccountsAsync()).Count != 0 || (await goals.GetGoalsAsync()).Count != 0
                    || (await store.GetEntriesAsync()).Count != 0) throw new IOException("Fresh empty independent route source required.");
                var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                var account = new Account { Name = "Original reminder route account", Type = AccountType.Savings,
                    CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = today.AddMonths(-2) };
                var goal = new Goal { Name = "Original reminder route goal", Type = GoalType.AccountBalance,
                    AccountId = account.Id, CurrencyCode = "EUR", TargetAmount = 15000 };
                ids = new(run, account.Id, goal.Id, today.AddDays(1));
                await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(ids));
                if (!await store.SaveAccountAsync(account)) throw new IOException("Original account refused.");
                await goals.SaveGoalAsync(goal);
                await goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan { Amount = 2500, ReminderEnabled = true,
                    Rule = new RecurrenceRule { Start = ids.Date, Frequency = Frequency.Once } });
                await store.UpdateSettingsAsync(settings => { settings.OnboardingCompleted = true; settings.NotificationsShowDetails = false;
                    settings.ReviewReminderEnabled = true; settings.MonthStartDay = Math.Min(28, today.Day); settings.ReviewProgress = null; });
                await SettleAsync(reminders);
                await File.WriteAllTextAsync(Path.Combine(Root, "source.json"), await PacketAsync(services));
            }
            else ids = JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(marker)) ?? throw new IOException("Retain original identity.");
            if (ids.Run != run || !File.Exists(Path.Combine(Root, "source.json"))) throw new IOException("Preserve original partial route setup; no reseeding.");
            var appLock = services.GetRequiredService<AppLockService>();
            if (appLock.IsEnabled || appLock.IsLocked) throw new IOException("No configured credential acceptance in this cohort.");
            if (phase == "review")
            {
                if (!File.Exists(Path.Combine(Root, "goal-posted.json"))) throw new IOException("Original goal publication missing.");
                var arrived = await MainThread.InvokeOnMainThreadAsync(() => Shell.Current?.CurrentPage is GoalDetailPage
                    { BindingContext: GoalDetailViewModel { CanView: true, Summary: { } row } } && row.Id == ids.Goal);
                if (!arrived) throw new IOException("Actual original goal destination required before next phase.");
                await File.WriteAllTextAsync(Path.Combine(Root, "goal-arrived.json"), JsonSerializer.Serialize(new { Run = run, Pid = Android.OS.Process.MyPid(), OriginalGoal = ids.Goal }));
            }
            await SettleAsync(reminders); await reminders.RefreshAsync();
            if (!await MatchesAsync(services)) throw new IOException("Original source changed before notification.");
            var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
            var request = phase == "goal" ? pending.Single(value => GoalReminderLink.Parse(value.ReturningData) is not null)
                : pending.Where(value => value.ReturningData == "review").OrderBy(value => value.Schedule?.NotifyTime).First();
            if (phase == "goal")
            {
                var link = GoalReminderLink.Parse(request.ReturningData)!;
                if (link.Targets.Count != 1 || link.Targets[0].GoalId != ids.Goal || link.Targets[0].OriginalDate != ids.Date
                    || request.Schedule?.NotifyTime != ids.Date.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local))
                    throw new IOException("Original saved goal request differs.");
            }
            var posting = Path.Combine(Root, phase + "-posting.json");
            if (File.Exists(posting)) throw new IOException("Retain partial original notification publication; never duplicate it.");
            var result = new { Run = run, Pid = Android.OS.Process.MyPid(), Phase = phase, request.NotificationId,
                request.Title, request.Description, OriginalScheduledAt = request.Schedule?.NotifyTime, ImmediateCopy = true,
                FinancialSourceUnchanged = true, CredentialConfigured = false };
            await File.WriteAllTextAsync(posting, JsonSerializer.Serialize(result));
            // This explicit independent display copy qualifies content routing, never actual09:00 alarm execution.
            await reminders.Scheduler.ShowAsync(new ReminderNotification(request.NotificationId, request.Title ?? string.Empty,
                request.Description ?? string.Empty, null, request.ReturningData ?? string.Empty));
            await File.WriteAllTextAsync(completed, JsonSerializer.Serialize(result));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            await File.WriteAllTextAsync(Path.Combine(Root, "error.json"), JsonSerializer.Serialize(new { Run = run, Phase = phase, ErrorType = error.GetType().Name }));
            throw;
        }
    }

    /// <summary>Reads synthetic receipt, actual current destination and original source only after protected startup; never prepares storage.</summary>
    internal static async Task<string> ReadStatusAsync(IServiceProvider services)
    {
        RequirePackage();
        var marker = Path.Combine(Root, "identity.json");
        var ids = File.Exists(marker) ? JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(marker)) : null;
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var appLock = services.GetRequiredService<AppLockService>();
        var page = await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var current = Shell.Current?.CurrentPage;
            var goal = current?.BindingContext as GoalDetailViewModel; var review = current?.BindingContext as PeriodReviewViewModel;
            return new { Page = current?.GetType().Name, GoalId = goal?.Summary?.Id, GoalName = goal?.Summary?.Name,
                GoalReady = goal?.CanView == true, ReviewTitle = review?.Title, ReviewReady = review is { NothingToReview: false, Steps.Count: 5 },
                ReviewDone = review?.Steps.Count(step => step.IsDone) ?? 0 };
        });
        var posted = new List<JsonElement>();
        foreach (var phase in new[] { "goal", "review" })
        {
            var file = Path.Combine(Root, phase + "-posted.json");
            if (File.Exists(file)) posted.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(file)));
        }
        var error = Path.Combine(Root, "error.json");
        return JsonSerializer.Serialize(new { Run = ids?.Run, Pid = Android.OS.Process.MyPid(), Protected = location.IsProtected,
            SourceRead = location.IsProtected && ids is not null, SourceMatch = location.IsProtected && ids is not null && await MatchesAsync(services),
            Locked = appLock.IsLocked, CredentialConfigured = appLock.IsEnabled, Destination = page, Posted = posted,
            Error = File.Exists(error) ? JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(error)) : (JsonElement?)null });
    }

    /// <summary>Follows only genuinely replaced coalesced handles, retaining failures and original metadata.</summary>
    private static async Task SettleAsync(ReminderService reminders)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            var task = reminders.PendingRefresh;
            try { await task; }
            catch (OperationCanceledException) when (task.IsCanceled)
            { await Task.Yield(); if (ReferenceEquals(task, reminders.PendingRefresh)) throw; }
            if (ReferenceEquals(task, reminders.PendingRefresh)) return;
            if (watch.Elapsed > TimeSpan.FromSeconds(10)) throw new IOException("Original timer did not settle.");
        }
    }

    private static async Task<bool> MatchesAsync(IServiceProvider services) => File.Exists(Path.Combine(Root, "source.json"))
        && await File.ReadAllTextAsync(Path.Combine(Root, "source.json")) == await PacketAsync(services);

    /// <summary>Compares complete intentionally populated protected source rows and absence of any financial posting.</summary>
    private static async Task<string> PacketAsync(IServiceProvider services)
    {
        var store = services.GetRequiredService<ZananceStore>(); var goals = services.GetRequiredService<GoalStore>();
        var plans = services.GetRequiredService<PlanStore>(); var holdings = services.GetRequiredService<HoldingStore>();
        return JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Goals = await goals.GetGoalsAsync(),
            Contributions = await goals.GetContributionPlansAsync(), Settings = await store.GetSettingsAsync(),
            Entries = await store.GetEntriesAsync(), Allocations = await goals.GetAllocationsAsync(), Schedules = await plans.GetSchedulesAsync(),
            States = await plans.GetStatesAsync(), Events = await holdings.GetEventsAsync(), Types = await holdings.GetTypesAsync() });
    }

    private static void RequirePackage()
    {
        if (AppInfo.Current.PackageName != NativeReminderReview.Package) throw new IOException("Independent route identity required.");
    }
}
