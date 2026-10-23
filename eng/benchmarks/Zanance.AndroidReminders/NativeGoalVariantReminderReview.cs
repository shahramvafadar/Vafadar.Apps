using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Localization;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Qualifies saved rule calendars and unavailable goal sources without replaying the earlier lifecycle cohort.</summary>
internal static class NativeGoalVariantReminderReview
{
    private static string Root => Path.Combine(FileSystem.AppDataDirectory, "goal-variant-review");
    private static string Receipt => Path.Combine(Root, "receipt.json");
    private sealed record Identity(string Run, Guid Account, Guid Type, Guid BalanceGoal, Guid QuantityGoal,
        DateOnly Date, CalendarSystem DisplayCalendar);

    /// <summary>Reads only previously persisted synthetic observations; no storage preparation or transition occurs.</summary>
    internal static string ReadReceipt()
    {
        RequirePackage();
        return File.Exists(Receipt) ? File.ReadAllText(Receipt) : "{\"Stage\":\"starting\"}";
    }

    /// <summary>Continues original accepted metadata stages with empty ledger, allocation and holding-event tables.</summary>
    internal static async Task PrepareAsync(IServiceProvider services, string run)
    {
        RequirePackage(); Directory.CreateDirectory(Root);
        if (File.Exists(Receipt))
        {
            using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(Receipt));
            if (prior.RootElement.GetProperty("Run").GetString() != run) throw new IOException("Original variant run required.");
            if (prior.RootElement.GetProperty("Completed").GetBoolean()) return;
        }
        var observations = new List<JsonElement>();
        async Task Report(string stage, bool complete = false, string? error = null) =>
            await File.WriteAllTextAsync(Receipt, JsonSerializer.Serialize(new
            { Run = run, Pid = Android.OS.Process.MyPid(), Stage = stage, Completed = complete, ErrorType = error, Observations = observations }));
        try
        {
            var store = services.GetRequiredService<ZananceStore>(); var goals = services.GetRequiredService<GoalStore>();
            var holdings = services.GetRequiredService<HoldingStore>(); var reminders = services.GetRequiredService<ReminderService>();
            var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
            var time = services.GetRequiredService<TimeProvider>();
            await Report("permission");
            if (!await reminders.EnsurePermissionAsync()) throw new IOException("Original notification permission required.");
            var marker = Path.Combine(Root, "identity.json"); Identity ids;
            if (!File.Exists(marker))
            {
                if ((await store.GetAccountsAsync()).Count != 0 || (await goals.GetGoalsAsync()).Count != 0
                    || (await holdings.GetTypesAsync()).Count != 0 || (await store.GetEntriesAsync()).Count != 0)
                    throw new IOException("Empty independent goal-variant source required.");
                var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
                var account = new Account { Name = "Original variant savings", Type = AccountType.Savings,
                    OpeningBalance = 10000, CurrencyCode = "EUR", OpeningDate = day };
                var type = new AssetType { Name = "Original variant holding", Kind = AssetKind.CountableItem,
                    Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
                var balance = new Goal { Name = "Original variant balance", Type = GoalType.AccountBalance,
                    AccountId = account.Id, CurrencyCode = "EUR", TargetAmount = 15000 };
                var quantity = new Goal { Name = "Original variant quantity", Type = GoalType.HoldingQuantity,
                    AssetTypeId = type.Id, CurrencyCode = "EUR", TargetAmount = 5000 };
                ids = new(run, account.Id, type.Id, balance.Id, quantity.Id, day.AddDays(1), local.CurrentCalendar);
                await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(ids));
                if (!await store.SaveAccountAsync(account) || !await holdings.SaveTypeAsync(type)) throw new IOException("Original source setup refused.");
                await goals.SaveGoalAsync(balance); await goals.SaveGoalAsync(quantity);
                await goals.SaveContributionPlanAsync(balance.Id, new ContributionPlan { Amount = 2500 });
                await goals.SaveContributionPlanAsync(quantity.Id, new ContributionPlan { Amount = 1000 });
                await store.UpdateSettingsAsync(value => { value.OnboardingCompleted = true; value.NotificationsShowDetails = false; });
            }
            else ids = JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(marker)) ?? throw new IOException("Original identity unavailable.");
            if (ids.Run != run || (await store.GetAccountsAsync()).Count != 1 || (await holdings.GetTypesAsync()).Count != 1
                || (await goals.GetGoalsAsync()).Count != 2 || (await goals.GetContributionPlansAsync()).Count != 2)
                throw new IOException("Preserve original partial source; no reseeding.");

            async Task<string> Packet() => JsonSerializer.Serialize(new
            {
                Accounts = await store.GetAccountsAsync(), Goals = await goals.GetGoalsAsync(), Plans = await goals.GetContributionPlansAsync(),
                Types = await holdings.GetTypesAsync(), Settings = await store.GetSettingsAsync(), Entries = await store.GetEntriesAsync(),
                Allocations = await goals.GetAllocationsAsync(), Events = await holdings.GetEventsAsync()
            });
            async Task Stage(string stage, PeriodCalendar calendar, CalendarSystem display, bool archivedAccount = false,
                bool archivedType = false, bool missed = false, bool disabled = false)
            {
                var path = Path.Combine(Root, stage + ".json");
                if (File.Exists(path)) { observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(path))); return; }
                var account = (await store.GetAccountsAsync()).Single();
                if (account.IsArchived != archivedAccount) { account.IsArchived = archivedAccount; if (!await store.SaveAccountAsync(account)) throw new IOException("Original archive refused."); }
                var type = (await holdings.GetTypesAsync()).Single();
                if (type.IsArchived != archivedType) { type.IsArchived = archivedType; if (!await holdings.SaveTypeAsync(type)) throw new IOException("Original type archive refused."); }
                foreach (var plan in await goals.GetContributionPlansAsync())
                {
                    var start = missed ? ids.Date.AddDays(-2) : ids.Date;
                    var frequency = missed ? Frequency.Once : Frequency.Monthly;
                    if (plan.Rule.Calendar != calendar || plan.Rule.Start != start || plan.Rule.Frequency != frequency
                        || plan.ReminderEnabled != !disabled || plan.Rule.End != EndKind.AfterCount || plan.Rule.Count != 2)
                    {
                        plan.Rule = new RecurrenceRule { Start = start, Frequency = frequency, Calendar = calendar, End = EndKind.AfterCount, Count = 2 };
                        plan.ReminderEnabled = !disabled; await goals.SaveContributionPlanAsync(plan.GoalId, plan);
                    }
                }
                local.SetCalendar(display);
                // A display-calendar callback can replace a coalesced timer while it is awaited.
                // Follow only the newly published handle; never replay metadata or retry an actual failure.
                var deadline = System.Diagnostics.Stopwatch.StartNew();
                while (true)
                {
                    var pendingRefresh = reminders.PendingRefresh;
                    try { await pendingRefresh; }
                    catch (OperationCanceledException) when (pendingRefresh.IsCanceled)
                    {
                        // Cancellation continuations can run inline before RefreshSoon assigns its new handle.
                        // Yield back to that publisher before checking exact replacement identity.
                        await Task.Yield();
                        if (ReferenceEquals(pendingRefresh, reminders.PendingRefresh)) throw;
                    }
                    if (ReferenceEquals(pendingRefresh, reminders.PendingRefresh)) break;
                    if (deadline.Elapsed > TimeSpan.FromSeconds(10)) throw new IOException("Original coalesced source did not settle.");
                }
                var before = await Packet(); await reminders.RefreshAsync();
                if (before != await Packet()) throw new IOException("Publication mutated original variant source.");
                var accounts = await store.GetAccountsAsync(); var types = await holdings.GetTypesAsync();
                var sourceGoals = await goals.GetGoalsAsync(); var plans = await goals.GetContributionPlansAsync();
                if (accounts.Single().Id != ids.Account || accounts.Single().OpeningBalance != 10000 || accounts.Single().CurrencyCode != "EUR"
                    || types.Single().Id != ids.Type || sourceGoals.Single(g => g.Id == ids.BalanceGoal).TargetAmount != 15000
                    || sourceGoals.Single(g => g.Id == ids.QuantityGoal).TargetAmount != 5000
                    || (await store.GetEntriesAsync()).Count != 0 || (await goals.GetAllocationsAsync()).Count != 0
                    || (await holdings.GetEventsAsync()).Count != 0 || (await services.GetRequiredService<PlanStore>().GetSchedulesAsync()).Count != 0)
                    throw new IOException("Variant financial source changed.");
                var progress = GoalProgressService.Evaluate(sourceGoals, [], accounts, [], plans,
                    DateOnly.FromDateTime(time.GetLocalNow().DateTime), [], types);
                if ((progress.Single(p => p.Goal.Id == ids.BalanceGoal).Notice == GoalNotice.AccountUnavailable) != archivedAccount
                    || (progress.Single(p => p.Goal.Id == ids.QuantityGoal).Notice == GoalNotice.HoldingUnavailable) != archivedType)
                    throw new IOException("Original unavailable-source notice differs.");
                var expected = GoalReminderPlanner.Plan(progress, plans, time.GetLocalNow().DateTime).GroupBy(item => item.NotifyAt).ToArray();
                var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
                if (pending.Count != expected.Length || pending.Count != (missed || disabled || archivedAccount && archivedType ? 0 : 2))
                    throw new IOException("Original variant native queue count differs.");
                var scope = (await goals.GetWorkSnapshotAsync()).ReminderScope;
                foreach (var group in expected)
                {
                    var request = pending.Single(value => value.NotificationId == group.First().Id);
                    var link = GoalReminderLink.Parse(request.ReturningData) ?? throw new IOException("Original scoped link missing.");
                    var actual = link.Targets.Select(item => item.GoalId.ToString("N") + ":" + item.OriginalDate.DayNumber).Order().ToArray();
                    var target = group.Select(item => item.Goal.Id.ToString("N") + ":" + item.Date.DayNumber).Order().ToArray();
                    if (request.Schedule?.NotifyTime != group.Key || group.Key.TimeOfDay != TimeSpan.FromHours(9)
                        || group.Key <= time.GetLocalNow().DateTime || link.Scope != scope || !actual.SequenceEqual(target)
                        || request.Title != text["App_Name"] || request.Description != text["Reminder_GoalGeneric"])
                        throw new IOException("Original native grouped dates, scoped targets or privacy differ.");
                }
                if (!missed && !disabled && !(archivedAccount && archivedType)
                    && !expected.Any(group => DateOnly.FromDateTime(group.Key) == ids.Date)) throw new IOException("Original first rule date missing.");
                var result = JsonSerializer.SerializeToElement(new { Stage = stage, Calendar = calendar.ToString(), Display = display.ToString(),
                    ArchivedAccount = archivedAccount, ArchivedHolding = archivedType, Missed = missed, Disabled = disabled,
                    Pending = pending.Count, Dates = expected.Select(group => group.Key), Targets = expected.Select(group => group.Count()),
                    OriginalSourceUnchangedByPublication = true, LedgerEntries = 0, Allocations = 0, HoldingEvents = 0 });
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(result)); observations.Add(result); await Report(stage);
            }
            await Stage("01-gregorian", PeriodCalendar.Gregorian, CalendarSystem.Gregorian);
            await Stage("02-gregorian-display-persian", PeriodCalendar.Gregorian, CalendarSystem.Persian);
            await Stage("03-persian", PeriodCalendar.Persian, CalendarSystem.Persian);
            await Stage("04-persian-display-hijri", PeriodCalendar.Persian, CalendarSystem.Hijri);
            await Stage("05-hijri", PeriodCalendar.Hijri, CalendarSystem.Hijri);
            await Stage("06-hijri-display-gregorian", PeriodCalendar.Hijri, CalendarSystem.Gregorian);
            await Stage("07-account-unavailable", PeriodCalendar.Hijri, CalendarSystem.Gregorian, archivedAccount: true);
            await Stage("08-both-unavailable", PeriodCalendar.Hijri, CalendarSystem.Gregorian, archivedAccount: true, archivedType: true);
            await Stage("09-account-restored", PeriodCalendar.Hijri, CalendarSystem.Gregorian, archivedType: true);
            await Stage("10-holding-restored", PeriodCalendar.Hijri, CalendarSystem.Gregorian);
            await Stage("11-missed", PeriodCalendar.Hijri, CalendarSystem.Gregorian, missed: true);
            await Stage("12-restored-off", PeriodCalendar.Hijri, ids.DisplayCalendar, disabled: true);
            await Report("complete", true);
        }
        catch (Exception error) when (error is not OutOfMemoryException) { await Report("failed", error: error.GetType().Name); throw; }
    }

    /// <summary>Rejects effects outside the independent fixture identity.</summary>
    private static void RequirePackage()
    {
        if (AppInfo.Current.PackageName != NativeReminderReview.Package) throw new IOException("Independent variant identity required.");
    }
}
