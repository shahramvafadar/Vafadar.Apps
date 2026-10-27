#if SNOOZE_CACHE_REVIEW
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Android.App;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Core.Settings;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>One fresh emulator queue reconstruction; no financial entry, old reminder cohort or production package.</summary>
internal static class SnoozeCacheReview
{
    private static readonly object Gate = new();
    private static string? originalRun;
    private static Task? observation;

    /// <summary>Admits the first warm or cold request; only an unconsumed terminal observer may resume the same run.</summary>
    internal static void Start(Activity activity, string run)
    {
        lock (Gate)
        {
            if (originalRun is not null && originalRun != run || observation is { IsCompleted: false }
                || File.Exists(Path.Combine(FileSystem.AppDataDirectory, "snooze-cache-consumed.json"))) return;
            originalRun = run;
            observation = RunAsync(activity, run);
        }
    }

    /// <summary>Consumes an explicit fresh request once and preserves the accepted source before the native queue change.</summary>
    private static async Task RunAsync(Activity activity, string run)
    {
        var stage = "admission";
        try
        {
            if (activity.PackageName != "pro.vafadar.zanance.snoozecachereview"
                || Android.OS.Build.Hardware != "ranchu" || (int)Android.OS.Build.VERSION.SdkInt != 36)
                throw new IOException("Fresh API36 emulator required.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var app = (App)Microsoft.Maui.Controls.Application.Current!;
            Android.Util.Log.Info("ZananceSnoozeCache", JsonSerializer.Serialize(new
            { Run = run, Pid = Android.OS.Process.MyPid(), Stage = "waiting-original-startup", ObservationStarted = true }));
            await app.WaitForStartupAsync().WaitAsync(timeout.Token);
            while (!activity.HasWindowFocus) await Task.Delay(100, timeout.Token);
            var marker = Path.Combine(FileSystem.AppDataDirectory, "snooze-cache-consumed.json");
            await using (var file = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await JsonSerializer.SerializeAsync(file, new { Run = run, Pid = Android.OS.Process.MyPid() }, cancellationToken: timeout.Token);

            var services = IPlatformApplication.Current!.Services;
            // Retire only this fresh installation's automatic watcher so its delayed refresh cannot repair the cache first.
            await services.GetRequiredService<ReminderService>().DisposeAsync();
            await using var reminders = ActivatorUtilities.CreateInstance<ReminderService>(services);
            stage = "permission";
            if (!await reminders.EnsurePermissionAsync()) throw new IOException("Original notification permission required.");
            var store = services.GetRequiredService<ZananceStore>();
            var plans = services.GetRequiredService<PlanStore>();
            var preferences = services.GetRequiredService<ISettingsStore>();
            if ((await store.GetAccountsAsync()).Count != 0 || (await plans.GetSchedulesAsync()).Count != 0
                || (await store.GetEntriesAsync()).Count != 0 || preferences.Get("reminders.snoozed") is not null)
                throw new IOException("Only a fresh empty independent source is admitted.");

            var now = services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime;
            var tomorrow = DateOnly.FromDateTime(now).AddDays(1);
            var account = new Account { Name = "Snooze cache fixture", CurrencyCode = "EUR", Type = AccountType.Checking,
                OpeningDate = DateOnly.FromDateTime(now), OpeningBalance = 0 };
            var plan = new Schedule { Name = "Snooze cache future plan", AccountId = account.Id, Amount = 100,
                AutoPost = false, ReminderEnabled = true, ReminderDaysBefore = 0, ReminderTime = new(9, 0),
                Rule = new() { Frequency = Frequency.Once, Start = tomorrow } };
            stage = "one-time-metadata";
            await using (var identity = new FileStream(Path.Combine(FileSystem.AppDataDirectory, "snooze-cache-identity.json"),
                FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await JsonSerializer.SerializeAsync(identity, new { Run = run, AccountId = account.Id, PlanId = plan.Id,
                    OriginalDate = tomorrow, OpeningBalance = 0, AutoPost = false }, cancellationToken: timeout.Token);
            await store.SaveAccountAsync(account);
            await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "snooze-cache-account-accepted.txt"), account.Id.ToString("N"), timeout.Token);
            await plans.SaveScheduleAsync(plan);
            await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "snooze-cache-plan-accepted.txt"), plan.Id.ToString("N"), timeout.Token);
            var work = await plans.GetWorkSnapshotAsync();
            var planned = ReminderPlanner.Plan(work.Schedules, work.States, now).Single();
            var link = PlanReminderLink.Format(work.ReminderScope, [new(plan.Id, tomorrow)]);
            var saved = new SnoozedReminder(ReminderSnoozes.IdFor(planned.Id), link, "Stale cached title", "Stale cached body", now.AddHours(2));
            var validCache = ReminderSnoozes.Serialize([saved]);
            var damaged = "[" + string.Join(',', Enumerable.Repeat("null", ReminderSnoozes.MaxSnoozes)) + "," + validCache[1..^1] + ",null]";
            preferences.Set("reminders.snoozed", damaged);
            var before = await SourceAsync(store, plans);

            stage = "original-publication";
            await reminders.RefreshAsync();
            var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
            var expectedIds = new[] { planned.Id, saved.Id }.Order().ToArray();
            if (!pending.Select(item => item.NotificationId).Order().SequenceEqual(expectedIds))
                throw new IOException("Original future plan and retained snooze must both reach the actual SDK.");
            var current = ReminderSnoozes.Deserialize(preferences.Get("reminders.snoozed")).Single();
            var nativeSnooze = pending.Single(item => item.NotificationId == saved.Id);
            if (current.Id != saved.Id || current.NotifyAt != saved.NotifyAt || current.Link != saved.Link
                || nativeSnooze.ReturningData != saved.Link || nativeSnooze.Schedule?.NotifyTime != saved.NotifyAt
                || current.Title == saved.Title || current.Body == saved.Body)
                throw new IOException("Retain original identity/deadline and rebuild caption from current source.");
            var after = await SourceAsync(store, plans);
            if (before != after || (await store.GetEntriesAsync()).Count != 0)
                throw new IOException("Original complete fictitious account/plan/settings/ledger packet changed.");

            stage = "owned-pending-retirement";
            LocalNotificationCenter.Current.Cancel(expectedIds);
            if ((await LocalNotificationCenter.Current.GetPendingNotificationList()).Count != 0)
                throw new IOException("Fresh package pending requests remain.");
            preferences.Set("reminders.snoozed", null);
            var result = new { Run = run, Pid = Android.OS.Process.MyPid(), Uid = Android.OS.Process.MyUid(), Completed = true,
                LeadingNullSlots = ReminderSnoozes.MaxSnoozes, NativePendingObserved = pending.Count,
                OriginalSourceSHA256 = before, OriginalSourceUnchanged = true, Entries = 0,
                OriginalIdentityDeadlineAndCurrentPrivacyPreserved = true, PendingAfterCleanup = 0 };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(result);
            await File.WriteAllBytesAsync(Path.Combine(FileSystem.AppDataDirectory, "snooze-cache-result.json"), bytes, timeout.Token);
            Android.Util.Log.Info("ZananceSnoozeCache", Encoding.UTF8.GetString(bytes));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Android.Util.Log.Error("ZananceSnoozeCache", JsonSerializer.Serialize(new
            { Run = run, Pid = Android.OS.Process.MyPid(), Completed = false, Stage = stage, ErrorType = error.GetType().FullName }));
        }
    }

    /// <summary>Hashes the fresh typed source only; no private shipping database, key or raw financial packet is exported.</summary>
    private static async Task<string> SourceAsync(ZananceStore store, PlanStore plans)
    {
        var work = await plans.GetWorkSnapshotAsync();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), work.Schedules, work.States, Settings = await store.GetSettingsAsync() });
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
#endif
