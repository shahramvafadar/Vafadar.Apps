using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Exercises original protected period-review queues across the three supported financial calendars.</summary>
internal static class NativePeriodReminderReview
{
    private static string Root => Path.Combine(FileSystem.AppDataDirectory, "period-reminder-review");
    private static string Receipt => Path.Combine(Root, "receipt.json");
    private sealed record Identity(string Run, Guid AccountId, DateOnly Today, CalendarSystem Calendar, int StartDay);

    /// <summary>Reads only synthetic progress already produced in the original foreground process.</summary>
    internal static string ReadReceipt()
    {
        RequirePackage();
        return File.Exists(Receipt) ? File.ReadAllText(Receipt) : "{\"Stage\":\"starting\"}";
    }

    /// <summary>Preserves accepted state receipts and changes only owned reminder/review metadata, never ledger rows.</summary>
    internal static async Task PrepareAsync(IServiceProvider services, string run)
    {
        RequirePackage();
        Directory.CreateDirectory(Root);
        if (File.Exists(Receipt))
        {
            using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(Receipt));
            if (retained.RootElement.TryGetProperty("Completed", out var completed) && completed.GetBoolean()) return;
        }
        var observations = new List<object>();
        async Task Report(string stage, bool complete = false, string? error = null) =>
            await File.WriteAllTextAsync(Receipt, JsonSerializer.Serialize(new
            { Run = run, Pid = Android.OS.Process.MyPid(), Stage = stage, Completed = complete, ErrorType = error, Observations = observations }));
        try
        {
            var store = services.GetRequiredService<ZananceStore>();
            var goals = services.GetRequiredService<GoalStore>();
            var plans = services.GetRequiredService<PlanStore>();
            var reminders = services.GetRequiredService<ReminderService>();
            var local = services.GetRequiredService<ILocalizationService>();
            var text = services.GetRequiredService<Translator>();
            var dates = services.GetRequiredService<IDateFormatter>();
            var time = services.GetRequiredService<TimeProvider>();
            var marker = Path.Combine(Root, "identity.json");
            Identity ids;
            if (!File.Exists(marker))
            {
                if ((await store.GetAccountsAsync()).Count != 0 || (await store.GetEntriesAsync()).Count != 0
                    || (await goals.GetGoalsAsync()).Count != 0 || (await plans.GetSchedulesAsync()).Count != 0)
                    throw new IOException("Empty independent period-review installation required.");
                var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
                ids = new(run, Guid.Empty, today, local.CurrentCalendar, (await store.GetSettingsAsync()).MonthStartDay);
                await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(ids));
            }
            else ids = JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(marker)) ?? throw new IOException("Original period identity missing.");
            if (ids.Run != run) throw new IOException("Retain original period run.");
            await Report("permission");
            if (!await reminders.EnsurePermissionAsync()) throw new IOException("Original notification permission required.");

            string? accountPacket = null;
            async Task Check(string stage, PeriodCalendar calendar, DateOnly? finishedBoundary = null, bool missed = false,
                DateOnly? requiredBoundary = null)
            {
                var receipt = Path.Combine(Root, stage + ".json");
                if (File.Exists(receipt))
                { observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(receipt))); return; }
                var settingsBefore = JsonSerializer.Serialize(await store.GetSettingsAsync());
                await reminders.PendingRefresh;
                await reminders.RefreshAsync();
                var settings = await store.GetSettingsAsync();
                if (JsonSerializer.Serialize(settings) != settingsBefore)
                    throw new IOException("Reminder publication changed the original review progress or settings.");
                var accounts = await store.GetAccountsAsync();
                var first = accounts.Count == 0 ? (DateOnly?)null : accounts.Min(account => account.OpeningDate);
                // The existing pure planner is already qualified; compare its complete packet with the real native adapter.
                var expected = ReviewReminderPlanner.Plan(settings.ReviewReminderEnabled, settings.ReviewProgress,
                    time.GetLocalNow().DateTime, calendar, settings.MonthStartDay, first);
                var all = await LocalNotificationCenter.Current.GetPendingNotificationList();
                if (all.Count != expected.Count || all.Any(item => item.ReturningData != "review"))
                    throw new IOException("Original native period queue differs from its actual planner.");
                foreach (var item in expected)
                {
                    var request = all.Single(value => value.NotificationId == item.Id);
                    if (request.Schedule?.NotifyTime != item.NotifyAt || request.Title != text["App_Name"]
                        || request.Description != (settings.NotificationsShowDetails
                            ? text.Format("Reminder_ReviewDetails", dates.Format(item.PeriodFirst, DateFormatStyle.MonthYear))
                            : text["Reminder_ReviewGeneric"]))
                        throw new IOException("Original calendar date, native identity or privacy packet differs.");
                }
                if (finishedBoundary is { } boundary && all.Any(item => item.Schedule?.NotifyTime == boundary.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local)))
                    throw new IOException("A finished original financial period still has a native reminder.");
                if (requiredBoundary is { } required && !all.Any(item => item.Schedule?.NotifyTime == required.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local)))
                    throw new IOException("The original upcoming financial boundary has no native reminder.");
                if (missed && all.Any(item => item.Schedule?.NotifyTime <= time.GetLocalNow().DateTime))
                    throw new IOException("An elapsed period reminder was caught up.");
                if (accountPacket is null ? accounts.Count != 0 : JsonSerializer.Serialize(accounts.Single()) != accountPacket)
                    throw new IOException("Original period account changed.");
                if ((await store.GetEntriesAsync()).Count != 0 || (await goals.GetGoalsAsync()).Count != 0
                    || (await goals.GetAllocationsAsync()).Count != 0 || (await plans.GetSchedulesAsync()).Count != 0)
                    throw new IOException("Reminder review posted money, goals or plan work.");
                var result = new { Stage = stage, Calendar = calendar.ToString(), settings.MonthStartDay,
                    settings.ReviewReminderEnabled, PartialProgress = settings.ReviewProgress?.Contains(':') == true,
                    Details = settings.NotificationsShowDetails, Pending = all.Count,
                    Dates = expected.Select(item => item.NotifyAt), FinishedBoundaryAbsent = finishedBoundary is not null,
                    MissedTimesExcluded = missed, RequiredBoundaryPresent = requiredBoundary is not null,
                    ReviewSettingsUnchangedByPublication = true, FinancialRowsPreserved = true, LedgerEntries = 0, Allocations = 0 };
                await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(result));
                observations.Add(result); await Report(stage);
            }
            async Task Configure(bool enabled, int day, string? progress = null, bool details = false)
            {
                await store.UpdateSettingsAsync(value =>
                { value.ReviewReminderEnabled = enabled; value.MonthStartDay = day; value.ReviewProgress = progress; value.NotificationsShowDetails = details; });
            }
            // Empty-source observations precede the sole financial source setup and are never replayed.
            if (!File.Exists(Path.Combine(Root, "01-default-empty.json")))
            {
                if ((await store.GetSettingsAsync()).ReviewReminderEnabled) throw new IOException("New profile must default review reminders off.");
                await Check("01-default-empty", PeriodCalendar.Gregorian);
            }
            else await Check("01-default-empty", PeriodCalendar.Gregorian);
            if (!File.Exists(Path.Combine(Root, "02-enabled-empty.json")))
            { await Configure(true, ids.StartDay); await Check("02-enabled-empty", PeriodCalendar.Gregorian); }
            else await Check("02-enabled-empty", PeriodCalendar.Gregorian);
            var source = await store.GetAccountsAsync();
            if (source.Count == 0)
            {
                if (ids.AccountId != Guid.Empty) throw new IOException("Interrupted original account setup must not be reseeded.");
                var account = new Account { Name = "Period reminder fixture account", Type = AccountType.Checking,
                    CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = ids.Today.AddMonths(-3) };
                ids = ids with { AccountId = account.Id };
                await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(ids));
                await store.SaveAccountAsync(account);
            }
            var original = (await store.GetAccountsAsync()).Single();
            if (original.Id != ids.AccountId || original.OpeningBalance != 10000 || original.CurrencyCode != "EUR"
                || original.OpeningDate != ids.Today.AddMonths(-3)) throw new IOException("Original period account identity differs.");
            accountPacket = JsonSerializer.Serialize(original);
            if (!File.Exists(Path.Combine(Root, "03-disabled-source.json")))
            { await Configure(false, ids.StartDay); await Check("03-disabled-source", PeriodCalendar.Gregorian); }
            else await Check("03-disabled-source", PeriodCalendar.Gregorian);

            foreach (var pair in new[] { (PeriodCalendar.Gregorian, CalendarSystem.Gregorian),
                (PeriodCalendar.Persian, CalendarSystem.Persian), (PeriodCalendar.Hijri, CalendarSystem.Hijri) })
            {
                var (calendar, display) = pair; local.SetCalendar(display);
                var boundary = ids.Today.AddDays(1);
                while (PeriodMath.DayOf(boundary, calendar) > PeriodMath.MaxStartDay) boundary = boundary.AddDays(1);
                var day = PeriodMath.DayOf(boundary, calendar);
                var due = PeriodReview.Due(null, boundary, calendar, day, original.OpeningDate)
                    ?? throw new IOException("Original fictional financial period unavailable.");
                var partial = PeriodReview.Toggle(due, ReviewStep.Unreviewed, true);
                var finished = PeriodReview.Finish(due);
                async Task State(string suffix, bool enabled, string? progress = null, bool details = false, bool complete = false, bool missed = false)
                {
                    var stage = calendar + "-" + suffix;
                    if (!File.Exists(Path.Combine(Root, stage + ".json")))
                    {
                        var start = day;
                        if (missed)
                        {
                            var elapsed = ids.Today;
                            while (PeriodMath.DayOf(elapsed, calendar) > PeriodMath.MaxStartDay
                                || elapsed.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local) > time.GetLocalNow().DateTime)
                                elapsed = elapsed.AddDays(-1);
                            start = PeriodMath.DayOf(elapsed, calendar);
                        }
                        await Configure(enabled, start, progress, details);
                    }
                    await Check(stage, calendar, complete ? boundary : null, missed,
                        enabled && !complete && !missed ? boundary : null);
                }
                await State("future-generic", true);
                await State("partial", true, partial);
                await State("finished", true, finished, complete: true);
                await State("details", true, details: true);
                await State("disabled", false);
                await State("missed", true, missed: true);
            }
            local.SetCalendar(ids.Calendar);
            if (!File.Exists(Path.Combine(Root, "22-restored-off.json"))) await Configure(false, ids.StartDay);
            await Check("22-restored-off", ids.Calendar switch
            { CalendarSystem.Persian => PeriodCalendar.Persian, CalendarSystem.Hijri => PeriodCalendar.Hijri, _ => PeriodCalendar.Gregorian });
            await Report("complete", true);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { await Report("failed", error: error.GetType().Name); throw; }
    }

    /// <summary>Refuses every effect or receipt read outside the fixed independent reminder identity.</summary>
    private static void RequirePackage()
    {
        if (AppInfo.Current.PackageName != NativeReminderReview.Package) throw new IOException("Independent period-reminder identity required.");
    }
}
