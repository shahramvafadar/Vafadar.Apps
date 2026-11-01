using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Retains actual future09:00 goal/review requests for timed native observation, never an immediate display copy.</summary>
internal static class NativeTimedGoalPeriodReview
{
    private static string Root => Path.Combine(FileSystem.AppDataDirectory, "timed-goal-period-review");
    private sealed record Identity(string Run, Guid Account, Guid Goal, DateOnly Date);

    /// <summary>Prepares only a fresh original protected source and preserves its real immutable deadline/requests.</summary>
    internal static async Task PrepareAsync(IServiceProvider services, string run)
    {
        RequirePackage(); Directory.CreateDirectory(Root);
        var receipt = Path.Combine(Root, "receipt.json");
        if (File.Exists(receipt)) throw new IOException("Retain already armed original requests; never rearm or replay timed preparation.");
        try
        {
            var store = services.GetRequiredService<ZananceStore>(); var goals = services.GetRequiredService<GoalStore>();
            var reminders = services.GetRequiredService<ReminderService>(); var local = services.GetRequiredService<ILocalizationService>();
            var text = services.GetRequiredService<Translator>(); var time = services.GetRequiredService<TimeProvider>();
            if (!await reminders.EnsurePermissionAsync()) throw new IOException("Original notification permission required.");
            var marker = Path.Combine(Root, "identity.json");
            if (File.Exists(marker)) throw new IOException("Retain partial timed initialization; never recreate its original financial source.");
            if ((await store.GetAccountsAsync()).Count != 0 || (await goals.GetGoalsAsync()).Count != 0 || (await store.GetEntriesAsync()).Count != 0)
                throw new IOException("Fresh empty independent timed source required.");
            var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime); var tomorrow = today.AddDays(1);
#if PHYSICAL_TIMED_REMINDER_REVIEW
            // D-607: choose an actual future representable boundary; never alter the clock or MonthStartDay policy.
            while (tomorrow.Day > 28) tomorrow = tomorrow.AddDays(1);
#endif
            if (tomorrow.Day > 28 || local.CurrentCalendar != CalendarSystem.Gregorian)
                throw new IOException("This original financial boundary requires the recorded Gregorian day1-to28, without changing clock or calendar.");
            var account = new Account { Name = "Timed reminder source", CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = today.AddMonths(-2) };
            var goal = new Goal { Name = "Timed reminder goal", Type = GoalType.AccountBalance, AccountId = account.Id, CurrencyCode = "EUR", TargetAmount = 15000 };
            var ids = new Identity(run, account.Id, goal.Id, tomorrow);
            await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(ids));
            if (!await store.SaveAccountAsync(account)) throw new IOException("Original timed account refused.");
            await goals.SaveGoalAsync(goal);
            await goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan { Amount = 2500, ReminderEnabled = true,
                Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = tomorrow } });
            await store.UpdateSettingsAsync(value => { value.OnboardingCompleted = true; value.NotificationsShowDetails = false;
                value.ReviewReminderEnabled = true; value.MonthStartDay = tomorrow.Day; value.ReviewProgress = null; });
            await SettleAsync(reminders);
            var before = await PacketAsync(services); await reminders.RefreshAsync();
            if (before != await PacketAsync(services)) throw new IOException("Original timed publication changed its source.");
            var all = await LocalNotificationCenter.Current.GetPendingNotificationList();
            var goalRequests = all.Where(request => GoalReminderLink.Parse(request.ReturningData) is not null).ToArray();
            var reviews = all.Where(request => request.ReturningData == "review").ToArray();
            var at = tomorrow.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local);
#if PHYSICAL_TIMED_REMINDER_REVIEW
            var reviewDates = PhysicalReviewDates(tomorrow, time.GetLocalNow().DateTime);
            if (goalRequests.Length != 1 || reviews.Length != reviewDates.Length || all.Count != reviewDates.Length + 1
                || all.Select(request => request.NotificationId).Distinct().Count() != all.Count
                || !reviews.Select(request => request.Schedule?.NotifyTime).Order().SequenceEqual(reviewDates.Select(value => (DateTimeOffset?)value)))
#else
            if (goalRequests.Length != 1 || reviews.Length != 3 || all.Count != 4 || all.Select(request => request.NotificationId).Distinct().Count() != 4)
#endif
                throw new IOException("Original scheduled goal/period queue differs.");
            var link = GoalReminderLink.Parse(goalRequests[0].ReturningData)!;
            if (link.Targets.Count != 1 || link.Targets[0].GoalId != goal.Id || link.Targets[0].OriginalDate != tomorrow
                || link.Scope != (await goals.GetWorkSnapshotAsync()).ReminderScope || goalRequests[0].Schedule?.NotifyTime != at)
                throw new IOException("Original actual goal deadline/scope differs.");
            var due = all.Where(request => request.Schedule?.NotifyTime == at).ToArray();
            if (due.Length != 2 || due.Count(request => request.ReturningData == "review") != 1
                || goalRequests[0].Description != text["Reminder_GoalGeneric"] || due.Single(request => request.ReturningData == "review").Description != text["Reminder_ReviewGeneric"]
                || all.Any(request => request.Title != text["App_Name"] || request.Schedule?.NotifyTime <= time.GetLocalNow()))
                throw new IOException("Original generic future09:00 requests required.");
            await File.WriteAllTextAsync(Path.Combine(Root, "source.json"), before);
            await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new
            {
                Run = run, PreparedPid = Android.OS.Process.MyPid(), Stage = "armed", Date = tomorrow, MonthStartDay = tomorrow.Day,
                AllPending = all.Select(request => new { request.NotificationId, Kind = request.ReturningData == "review" ? "review" : "goal", At = request.Schedule?.NotifyTime }),
                Wanted = due.Select(request => new { request.NotificationId, request.Title, request.Description, Kind = request.ReturningData == "review" ? "review" : "goal", At = request.Schedule?.NotifyTime }),
                SourceUnchanged = true, LedgerEntries = 0, Allocations = 0, ImmediateCopies = 0
            }));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            await File.WriteAllTextAsync(Path.Combine(Root, "error.json"), JsonSerializer.Serialize(new { Run = run, ErrorType = error.GetType().Name }));
            throw;
        }
    }

    /// <summary>Reads synthetic receipt/current process and SDK pending ids; original financial reads require an already protected endpoint.</summary>
    internal static async Task<string> ReadStatusAsync(IServiceProvider services)
    {
        RequirePackage();
        var receipt = Path.Combine(Root, "receipt.json"); var error = Path.Combine(Root, "error.json");
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var source = Path.Combine(Root, "source.json");
        var read = location.IsProtected && File.Exists(source);
        var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
        return JsonSerializer.Serialize(new { Pid = Android.OS.Process.MyPid(), Windows = Application.Current?.Windows.Count ?? 0,
            Protected = location.IsProtected, SourceRead = read, SourceMatch = read ? await File.ReadAllTextAsync(source) == await PacketAsync(services) : (bool?)null,
            PendingIds = pending.Select(request => request.NotificationId),
            Receipt = File.Exists(receipt) ? JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(receipt)) : (JsonElement?)null,
            Error = File.Exists(error) ? JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(error)) : (JsonElement?)null });
    }

#if PHYSICAL_TIMED_REMINDER_REVIEW
    /// <summary>Enumerates only the declared Gregorian boundary and original62-day horizon, without invoking the planner under test.</summary>
    private static DateTimeOffset[] PhysicalReviewDates(DateOnly first, DateTime now)
    {
        var until = now.AddDays(ReminderPlanner.HorizonDays);
        return Enumerable.Range(0, 3).Select(month => new DateTimeOffset(first.AddMonths(month).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local)))
            .Where(at => at > now && at <= until).ToArray();
    }

    /// <summary>Completes only diagnostic snapshot/receipt capture after the original accepted writes, never replaying their preparation.</summary>
    internal static async Task<string> CapturePhysicalAdmissionAsync(IServiceProvider services, string run)
    {
        var admission = await ReadPhysicalAdmissionAsync(services, run);
        using var checkedSource = JsonDocument.Parse(admission);
        var ids = JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(Path.Combine(Root, "identity.json")))!;
        var now = services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime;
        var at = ids.Date.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local);
        if (at <= now || checkedSource.RootElement.GetProperty("SelectedPlannerCount").GetInt32() != 1)
            throw new IOException("Original future physical goal is not admitted.");
        var all = await LocalNotificationCenter.Current.GetPendingNotificationList();
        var goal = all.Where(request => GoalReminderLink.Parse(request.ReturningData) is not null).ToArray();
        var reviews = all.Where(request => request.ReturningData == "review").ToArray();
        var dates = PhysicalReviewDates(ids.Date, now);
        if (goal.Length != 1 || goal[0].Schedule?.NotifyTime != at || reviews.Length != dates.Length
            || all.Count != dates.Length + 1 || all.Select(item => item.NotificationId).Distinct().Count() != all.Count
            || !reviews.Select(item => item.Schedule?.NotifyTime).Order().SequenceEqual(dates.Select(value => (DateTimeOffset?)value)))
            throw new IOException("Original exact physical queue differs; never normalize a missing request.");
        var link = GoalReminderLink.Parse(goal[0].ReturningData)!;
        var snapshot = await services.GetRequiredService<GoalStore>().GetWorkSnapshotAsync();
        if (link.Scope != snapshot.ReminderScope || link.Targets.Count != 1 || link.Targets[0].GoalId != ids.Goal
            || link.Targets[0].OriginalDate != ids.Date || goal[0].NotificationId != GoalReminderPlanner.StableId(ids.Goal, ids.Date))
            throw new IOException("Original physical goal payload/identity differs.");
        var packet = await PacketAsync(services);
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(packet)));
        if (digest != checkedSource.RootElement.GetProperty("SourceSHA256").GetString())
            throw new IOException("Original physical source changed during diagnostic admission.");
        var source = Path.Combine(Root, "source.json");
        if (File.Exists(source))
        {
            if (await File.ReadAllTextAsync(source) != packet) throw new IOException("Retain original diagnostic snapshot; never replace it.");
        }
        else
        {
            await using var stream = new FileStream(source, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(packet);
        }
        var receipt = Path.Combine(Root, "receipt.json");
        if (File.Exists(receipt)) throw new IOException("Original physical admission is already captured; read it instead.");
        var wanted = all.Where(item => item.Schedule?.NotifyTime == at).ToArray();
        if (wanted.Length != 2) throw new IOException("The original simultaneous physical pair is missing.");
        var result = JsonSerializer.Serialize(new
        {
            Run = run, PreparedPid = checkedSource.RootElement.GetProperty("Pid").GetInt32(), Stage = "admitted-after-observer-correction",
            Date = ids.Date, MonthStartDay = ids.Date.Day, SourceUnchanged = true, SourceSHA256 = digest,
            SourceBaselineCapturedAfterReadOnlyObserverUpgrade = true, OriginalExpectationFailurePreserved = true,
            AllPending = all.Select(item => new { item.NotificationId, Kind = item.ReturningData == "review" ? "review" : "goal", At = item.Schedule?.NotifyTime }),
            Wanted = wanted.Select(item => new { item.NotificationId, item.Title, item.Description, Kind = item.ReturningData == "review" ? "review" : "goal", At = item.Schedule?.NotifyTime }),
            LedgerEntries = 0, Allocations = 0, ImmediateCopies = 0, FinancialCommandsReplayed = 0, ExplicitRefreshOrRearm = 0
        });
        await using var output = new FileStream(receipt, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await using var receiptWriter = new StreamWriter(output);
        await receiptWriter.WriteAsync(result);
        return result;
    }

    /// <summary>Diagnoses only the already accepted fresh physical source; never saves, refreshes or rearms it.</summary>
    internal static async Task<string> ReadPhysicalAdmissionAsync(IServiceProvider services, string run)
    {
        RequirePackage();
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.physicaltimedreminderreview"
            || !Guid.TryParseExact(run, "N", out _) || services.GetRequiredService<Security.AppLockService>().IsLocked)
            throw new IOException("Original unlocked independent physical source required.");
        var ids = JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(Path.Combine(Root, "identity.json")))
            ?? throw new IOException("Retain original physical source identity.");
        if (ids.Run != run) throw new IOException("Physical source run differs.");
        var store = services.GetRequiredService<ZananceStore>();
        var goals = services.GetRequiredService<GoalStore>();
        var time = services.GetRequiredService<TimeProvider>();
        var now = time.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(now);
        var planSnapshot = await services.GetRequiredService<PlanStore>().GetWorkSnapshotAsync();
        var goalSnapshot = await goals.GetWorkSnapshotAsync();
        var financials = await store.GetReminderFinancialSnapshotAsync(planSnapshot, goalSnapshot, today);
        if (financials.Accounts.Count != 1 || goalSnapshot.Goals.Count != 1 || goalSnapshot.ContributionPlans.Count != 1
            || financials.Entries.Count != 0 || goalSnapshot.Allocations.Count != 0 || planSnapshot.Schedules.Count != 0)
            throw new IOException("Original single-source shape differs; do not infer or repair it.");
        var account = financials.Accounts.Single(); var goal = goalSnapshot.Goals.Single();
        var contribution = goalSnapshot.ContributionPlans.Single();
        if (account.Id != ids.Account || account.Name != "Timed reminder source" || account.OpeningBalance != 10000
            || account.CurrencyCode != "EUR" || goal.Id != ids.Goal || goal.Name != "Timed reminder goal"
            || goal.AccountId != account.Id || goal.TargetAmount != 15000 || goal.CurrencyCode != "EUR"
            || goal.Type != GoalType.AccountBalance || contribution.GoalId != goal.Id)
            throw new IOException("Declared fictitious source identity differs.");
        var progress = GoalProgressService.Evaluate(goalSnapshot.Goals, goalSnapshot.Allocations,
            financials.Accounts.ToList(), financials.Entries, goalSnapshot.ContributionPlans, today, financials.Events, financials.Types).Single();
        var rawDates = ContributionSchedule.Dates(contribution.Rule, today, today.AddDays(ReminderPlanner.HorizonDays));
        var eligible = GoalReminderPlanner.Plan([progress], [contribution], now);
        var selected = GoalReminderPlanner.Plan(goalSnapshot.Work.CanRemind(goal.Id) ? [progress] : [], [contribution], now);
        var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
        return JsonSerializer.Serialize(new
        {
            Run = run, Pid = Android.OS.Process.MyPid(), Today = today, IntendedDate = ids.Date,
            Current = progress.Current, Target = progress.Target, progress.IsReached, progress.Notice, GoalState = goal.State,
            contribution.ReminderEnabled, contribution.Amount, contribution.Rule,
            RawDates = rawDates, EligiblePlannerCount = eligible.Count, SelectedPlannerCount = selected.Count,
            OriginalWorkAllowsReminder = goalSnapshot.Work.CanRemind(goal.Id),
            Pending = pending.Select(item => new { item.NotificationId, item.ReturningData, At = item.Schedule?.NotifyTime }),
            SourceSHA256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(await PacketAsync(services)))),
            NoFinancialWriteRefreshRearmOrPreparation = true, NoMainSourceOrKeyRead = true
        });
    }
#endif

    private static async Task<string> PacketAsync(IServiceProvider services)
    {
        var store = services.GetRequiredService<ZananceStore>(); var goals = services.GetRequiredService<GoalStore>();
        var plans = services.GetRequiredService<PlanStore>(); var holdings = services.GetRequiredService<HoldingStore>();
        return JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Goals = await goals.GetGoalsAsync(), Contributions = await goals.GetContributionPlansAsync(),
            Settings = await store.GetSettingsAsync(), Entries = await store.GetEntriesAsync(), Allocations = await goals.GetAllocationsAsync(),
            Plans = await plans.GetSchedulesAsync(), States = await plans.GetStatesAsync(), Types = await holdings.GetTypesAsync(), Events = await holdings.GetEventsAsync() });
    }

    private static async Task SettleAsync(ReminderService reminders)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            var task = reminders.PendingRefresh;
            try { await task; }
            catch (OperationCanceledException) when (task.IsCanceled) { await Task.Yield(); if (ReferenceEquals(task, reminders.PendingRefresh)) throw; }
            if (ReferenceEquals(task, reminders.PendingRefresh)) return;
            if (watch.Elapsed > TimeSpan.FromSeconds(10)) throw new IOException("Original coalesced timer did not settle.");
        }
    }

    private static void RequirePackage()
    { if (AppInfo.Current.PackageName != NativeReminderReview.Package) throw new IOException("Independent timed identity required."); }
}
