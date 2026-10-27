using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Owns one independent native snooze fixture without ledger posting; never referenced by production.</summary>
internal static class NativeReminderReview
{
    /// <summary>The only application identity authorized to own this fictitious qualification data.</summary>
#if SNOOZE_CACHE_REVIEW
    internal const string Package = "pro.vafadar.zanance.snoozecachereview";
#elif TIMED_IDLE_REMINDER_REVIEW
    internal const string Package = "pro.vafadar.zanance.timedidlereminderreview";
#elif LOCKED_REMINDER_REVIEW
    internal const string Package = "pro.vafadar.zanance.lockedreminderreview";
#else
    internal const string Package = "pro.vafadar.zanance.androidreminderreview";
#endif
    private const string AccountName = "Cold reminder fixture account";
    private const string PlanName = "Cold reminder fixture plan";
    private static string Marker => Path.Combine(FileSystem.AppDataDirectory, "native-reminder-fixture.txt");

    /// <summary>Publishes a real notification only for an explicit fresh or retained matching fixture.</summary>
    internal static async Task PrepareAsync(IServiceProvider services, string run, bool scheduledOnly = false)
    {
        RequirePackage();
        var reminders = services.GetRequiredService<ReminderService>();
        // D-475: ask permission before choosing a real future deadline, so owner response time cannot consume it.
        if (scheduledOnly && !await reminders.EnsurePermissionAsync()) throw new IOException("Native notification permission is required.");
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var accounts = await store.GetAccountsAsync();
        var schedules = await plans.GetSchedulesAsync();
        var fresh = !File.Exists(Marker);
        if (fresh)
        {
            if (accounts.Count != 0 || schedules.Count != 0 || (await store.GetEntriesAsync()).Count != 0)
                throw new IOException("Only an empty independent reminder installation can be initialized.");
            var now = services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime;
            var tomorrow = DateOnly.FromDateTime(now).AddDays(1);
            var future = now.AddMinutes(4);
            DateTime? scheduledAt = scheduledOnly
                ? new DateTime(future.Year, future.Month, future.Day, future.Hour, future.Minute, 0, DateTimeKind.Local) : null;
            var account = new Account { Name = AccountName, Type = AccountType.Checking, CurrencyCode = "EUR",
                OpeningDate = tomorrow.AddDays(-1), OpeningBalance = 100000 };
            var plan = new Schedule { Name = PlanName, AccountId = account.Id, Amount = 1234,
                AutoPost = false, ReminderEnabled = true,
                ReminderDaysBefore = scheduledAt is { } at ? tomorrow.DayNumber - DateOnly.FromDateTime(at).DayNumber : 0,
                ReminderTime = scheduledAt is { } due ? TimeOnly.FromDateTime(due) : new TimeOnly(9, 0),
                Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = tomorrow } };
            // Persist original identities before either write; interrupted fixtures are retained, never recreated.
            List<string> marker = [run, account.Id.ToString("N"), plan.Id.ToString("N"),
                tomorrow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)];
            if (scheduledAt is { } deadline) marker.Add(deadline.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            await File.WriteAllLinesAsync(Marker, marker);
            await store.SaveAccountAsync(account);
            await plans.SaveScheduleAsync(plan);
            await store.UpdateSettingsAsync(settings =>
            {
                settings.OnboardingCompleted = true;
                settings.NotificationsShowDetails = true;
            });
        }
        else if ((await File.ReadAllLinesAsync(Marker))[0] != run)
            throw new IOException("Retain the original reminder fixture run.");

        if (!await FinancialStateMatchesAsync(services))
            throw new IOException("The original independent financial fixture changed.");
        var retainedMarker = await File.ReadAllLinesAsync(Marker);
        if (scheduledOnly != (retainedMarker.Length == 5)) throw new IOException("Retain the original reminder qualification mode.");
        if (!scheduledOnly && !await reminders.EnsurePermissionAsync()) throw new IOException("Native notification permission is required.");
        await reminders.RefreshAsync();
        var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
        var original = pending.Single(item => PlanReminderLink.Parse(item.ReturningData) is not null);
        if (scheduledOnly)
        {
            var expectedAt = DateTime.ParseExact(retainedMarker[4], "O", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind);
            if (original.Schedule?.NotifyTime != expectedAt) throw new IOException("Original native reminder deadline differs.");
            // Preserve the actual original scheduled request. Never Show an immediate copy in this mode.
            return;
        }
        // Expedite only this fictitious display; the plan date, balance and actual native snooze action are unchanged.
        await reminders.Scheduler.ShowAsync(new ReminderNotification(original.NotificationId, original.Title ?? string.Empty,
            original.Description ?? string.Empty, null, original.ReturningData ?? string.Empty, CanSnooze: true));
    }

    /// <summary>Reads narrow synthetic status without preparing storage or opening an application window.</summary>
    internal static async Task<string[]> ReadStatusAsync(IServiceProvider services)
    {
        RequirePackage();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var snoozes = ReminderSnoozes.Deserialize(services.GetRequiredService<ISettingsStore>().Get("reminders.snoozed"));
        var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
        var repeated = pending.Count(item => snoozes.Any(snooze => item.NotificationId == snooze.Id));
        var marker = File.Exists(Marker) ? await File.ReadAllLinesAsync(Marker) : [];
        var planned = pending.Where(item => PlanReminderLink.Parse(item.ReturningData) is not null).ToArray();
        return [marker.Length > 0 ? marker[0] : "unprepared",
            Android.OS.Process.MyPid().ToString(System.Globalization.CultureInfo.InvariantCulture),
            (Microsoft.Maui.Controls.Application.Current?.Windows.Count ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            location.IsProtected.ToString(), snoozes.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            repeated.ToString(System.Globalization.CultureInfo.InvariantCulture),
            (location.IsProtected && await FinancialStateMatchesAsync(services)).ToString(), "none",
            planned.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), marker.Length == 5 ? marker[4] : "none",
            (planned.Length == 1 ? planned[0].NotificationId : 0).ToString(System.Globalization.CultureInfo.InvariantCulture)];
    }

    /// <summary>Checks all intentionally populated fixture fields and absence of financial posting.</summary>
    private static async Task<bool> FinancialStateMatchesAsync(IServiceProvider services)
    {
        if (!File.Exists(Marker)) return false;
        var marker = await File.ReadAllLinesAsync(Marker);
        var store = services.GetRequiredService<ZananceStore>();
        var accounts = await store.GetAccountsAsync();
        var schedules = await services.GetRequiredService<PlanStore>().GetSchedulesAsync();
        var scheduledAt = marker.Length == 5
            ? DateTime.ParseExact(marker[4], "O", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)
            : (DateTime?)null;
        return marker.Length is 4 or 5 && accounts.Count == 1 && schedules.Count == 1 &&
            accounts[0].Id.ToString("N") == marker[1] && accounts[0].Name == AccountName &&
            accounts[0].CurrencyCode == "EUR" && accounts[0].OpeningBalance == 100000 &&
            accounts[0].Type == AccountType.Checking &&
            accounts[0].OpeningDate.AddDays(1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) == marker[3] &&
            schedules[0].Id.ToString("N") == marker[2] && schedules[0].Name == PlanName &&
            schedules[0].AccountId == accounts[0].Id && schedules[0].Amount == 1234 &&
            schedules[0].Rule.Frequency == Frequency.Once &&
            schedules[0].ReminderDaysBefore == (scheduledAt is { } at ? schedules[0].Rule.Start.DayNumber - DateOnly.FromDateTime(at).DayNumber : 0) &&
            (scheduledAt is not { } deadline || schedules[0].ReminderTime == TimeOnly.FromDateTime(deadline)) &&
            schedules[0].Rule.Start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) == marker[3] &&
            schedules[0].ReminderEnabled && !schedules[0].AutoPost &&
            (await store.GetEntriesAsync()).Count == 0;
    }

    /// <summary>Refuses every native fixture effect outside this independently compiled application id.</summary>
    private static void RequirePackage()
    {
        if (AppInfo.Current.PackageName != Package) throw new IOException("Independent reminder identity required.");
    }
}
