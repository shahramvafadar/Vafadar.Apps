using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Checks grouped goal taps and a stale original-file link after the same goal ids are restored into another protected profile.</summary>
internal static class NativeGoalProfileScopeReview
{
    private static string Root => Path.Combine(FileSystem.AppDataDirectory, "goal-profile-scope-review");
    private sealed record Identity(string Run, Guid Account, Guid Balance, Guid Earmark, DateOnly Date);
    private sealed record SavedNotification(int Id, string Title, string Body, string Link, DateTimeOffset? ScheduledAt);
    private static int _observing;
    private static int _tap;

    /// <summary>Observes only actual native scheduler events; subscription performs no database/key/financial read.</summary>
    internal static void ObserveNativeTap(IServiceProvider services)
    {
        if (Android.App.Application.Context.PackageName != NativeReminderReview.Package || Interlocked.Exchange(ref _observing, 1) != 0) return;
        services.GetRequiredService<ReminderService>().Scheduler.Tapped += (_, raw) =>
        {
            if (!File.Exists(Path.Combine(Root, "identity.json")) || GoalReminderLink.Parse(raw) is not { } link) return;
            var index = Interlocked.Increment(ref _tap);
            File.WriteAllText(Path.Combine(Root, $"tap-{Android.OS.Process.MyPid()}-{index}.json"), JsonSerializer.Serialize(new
            { Pid = Android.OS.Process.MyPid(), Index = index, link.Scope, Targets = link.Targets }));
        };
    }

    /// <summary>Executes one explicit original-source preparation/restore/cleanup phase; accepted native display copies cannot repeat.</summary>
    internal static async Task PrepareAsync(IServiceProvider services, string run, string phase)
    {
        RequirePackage(); Directory.CreateDirectory(Root);
        try
        {
            if (phase is not ("grouped" or "restore" or "finish")) throw new IOException("Explicit profile scope phase required.");
            var profiles = services.GetRequiredService<ProfileService>(); var store = services.GetRequiredService<ZananceStore>();
            var goals = services.GetRequiredService<GoalStore>(); var reminders = services.GetRequiredService<ReminderService>();
            var backup = services.GetRequiredService<IBackupService>();
            if (services.GetRequiredService<AppLockService>() is { IsEnabled: true } or { IsLocked: true })
                throw new IOException("This cohort does not configure or bypass credentials.");
            var marker = Path.Combine(Root, "identity.json"); Identity ids;
            if (!File.Exists(marker))
            {
                if (phase != "grouped" || profiles.All.Count != 1 || !profiles.Current.IsMain
                    || (await store.GetAccountsAsync()).Count != 0 || (await goals.GetGoalsAsync()).Count != 0 || (await store.GetEntriesAsync()).Count != 0)
                    throw new IOException("Fresh isolated original profile required.");
                if (!await reminders.EnsurePermissionAsync()) throw new IOException("Original notification permission required.");
                var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                var account = new Account { Name = "Original scoped account", CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = today };
                var balance = new Goal { Name = "Original grouped balance", CurrencyCode = "EUR", Type = GoalType.AccountBalance, AccountId = account.Id, TargetAmount = 15000 };
                var earmark = new Goal { Name = "Original grouped earmark", CurrencyCode = "EUR", Type = GoalType.Earmark, TargetAmount = 5000 };
                ids = new(run, account.Id, balance.Id, earmark.Id, today.AddDays(1));
                await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(ids));
                if (!await store.SaveAccountAsync(account)) throw new IOException("Original source account refused.");
                await goals.SaveGoalAsync(balance); await goals.SaveGoalAsync(earmark);
                foreach (var goal in new[] { balance, earmark })
                    await goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan { Amount = goal.Id == balance.Id ? 2500 : 1000,
                        ReminderEnabled = true, Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = ids.Date } });
                await store.UpdateSettingsAsync(value => { value.OnboardingCompleted = true; value.NotificationsShowDetails = false; });
                await SettleAsync(reminders);
                await File.WriteAllTextAsync(Path.Combine(Root, "financial-a.json"), await FinancialAsync(services));
                await File.WriteAllTextAsync(Path.Combine(Root, "scope-a.txt"), (await goals.GetWorkSnapshotAsync()).ReminderScope);
            }
            else ids = JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(marker)) ?? throw new IOException("Original scope identity missing.");
            if (ids.Run != run || !File.Exists(Path.Combine(Root, "financial-a.json"))) throw new IOException("Retain original partial source; no reseeding.");
            var a = await File.ReadAllTextAsync(Path.Combine(Root, "scope-a.txt"));
            if (phase == "finish")
            {
                if (File.Exists(Path.Combine(Root, "complete.json"))) throw new IOException("Completed scope cohort must not repeat.");
                var b = await ReadBAsync(profiles);
                if (profiles.Current.Id != b.Id || !await MatchesAsync(services) || (await goals.GetWorkSnapshotAsync()).ReminderScope == a)
                    throw new IOException("Retain exact restored destination before cleanup.");
                if (!await profiles.SwitchToAsync(profiles.All.Single(profile => profile.IsMain))) throw new IOException("Original source return refused.");
                if ((await goals.GetWorkSnapshotAsync()).ReminderScope != a || !await MatchesAsync(services)) throw new IOException("Original source changed on return.");
                await profiles.DeleteAsync(b); ((App)Application.Current!).ShowCurrentProfile(); await SettleAsync(reminders);
                if (profiles.All.Count != 1 || !profiles.Current.IsMain || !await MatchesAsync(services)) throw new IOException("Original catalogue cleanup differs.");
                await File.WriteAllTextAsync(Path.Combine(Root, "complete.json"), JsonSerializer.Serialize(new { Run = run, Pid = Android.OS.Process.MyPid(), SourceUnchanged = true, Profiles = 1 }));
                return;
            }
            if (File.Exists(Path.Combine(Root, phase + "-posted.json"))) throw new IOException("Accepted scope display must not repeat.");
            SavedNotification request;
            if (phase == "grouped")
            {
                if (!profiles.Current.IsMain || !await MatchesAsync(services)) throw new IOException("Original grouped source differs.");
                await SettleAsync(reminders); await reminders.RefreshAsync();
                var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
                var original = pending.Single(value => GoalReminderLink.Parse(value.ReturningData) is not null);
                var link = GoalReminderLink.Parse(original.ReturningData)!;
                if (link.Scope != a || link.Targets.Count != 2 || !link.Targets.Select(target => target.GoalId).Order().SequenceEqual(new[] { ids.Balance, ids.Earmark }.Order())
                    || link.Targets.Any(target => target.OriginalDate != ids.Date)) throw new IOException("Original grouped native request differs.");
                request = new(original.NotificationId, original.Title ?? string.Empty, original.Description ?? string.Empty, original.ReturningData, original.Schedule?.NotifyTime);
                await File.WriteAllTextAsync(Path.Combine(Root, "original-notification.json"), JsonSerializer.Serialize(request));
            }
            else
            {
                if (!File.Exists(Path.Combine(Root, "grouped-posted.json"))) throw new IOException("Original group publication missing.");
                var ready = await MainThread.InvokeOnMainThreadAsync(() => Shell.Current?.CurrentPage is GoalsPage
                    { BindingContext: GoalsViewModel view } && view.Loading.IsReady && view.Active.Select(row => row.Id).Order().SequenceEqual(new[] { ids.Balance, ids.Earmark }.Order()));
                if (profiles.Current.IsMain && !ready) throw new IOException("Actual original grouped destination required.");
                var package = Path.Combine(Root, "original.zanance");
                if (!File.Exists(package))
                {
                    if (!profiles.Current.IsMain || !await MatchesAsync(services)) throw new IOException("Original export source required.");
                    await File.WriteAllBytesAsync(package, await backup.CreatePackageAsync(null));
                }
                var bFile = Path.Combine(Root, "profile-b.txt"); LocalProfile b;
                if (!File.Exists(bFile))
                {
                    var name = "Restored scope " + run[..8]; var matches = profiles.All.Where(profile => profile.Name == name).ToArray();
                    if (matches.Length > 1) throw new IOException("Ambiguous original destination; no profile recreation.");
                    b = matches.SingleOrDefault() ?? await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), name);
                    await File.WriteAllTextAsync(bFile, b.Id);
                }
                else b = await ReadBAsync(profiles);
                if (profiles.Current.Id != b.Id && !await profiles.SwitchToAsync(b)) throw new IOException("Original destination switch refused.");
                var restored = Path.Combine(Root, "restored.json");
                if (!File.Exists(restored))
                {
                    var restoring = Path.Combine(Root, "restoring.json");
                    if (File.Exists(restoring)) throw new IOException("Retain partial accepted restore; never replay it.");
                    await File.WriteAllTextAsync(restoring, JsonSerializer.Serialize(new { Run = run, Profile = b.Id }));
                    await backup.RestorePackageAsync(await File.ReadAllBytesAsync(package), null);
                    if (!await MatchesAsync(services)) throw new IOException("Original financial graph was not restored exactly.");
                    await File.WriteAllTextAsync(restored, JsonSerializer.Serialize(new { Run = run, Profile = b.Id, SameGoalIds = true }));
                }
                ((App)Application.Current!).ShowCurrentProfile(); await SettleAsync(reminders); await reminders.RefreshAsync();
                if (!await MatchesAsync(services) || (await goals.GetWorkSnapshotAsync()).ReminderScope == a)
                    throw new IOException("Restored cloned identities require their own original file scope.");
                request = JsonSerializer.Deserialize<SavedNotification>(await File.ReadAllTextAsync(Path.Combine(Root, "original-notification.json")))!;
            }
            var posting = Path.Combine(Root, phase + "-posting.json");
            if (File.Exists(posting)) throw new IOException("Retain partial original scope publication; no repeat Show.");
            var scope = (await goals.GetWorkSnapshotAsync()).ReminderScope;
            var result = new { Run = run, Pid = Android.OS.Process.MyPid(), Phase = phase, NotificationId = request.Id,
                request.Title, Description = request.Body, request.ScheduledAt, ImmediateCopy = true,
                OriginalScope = a, CurrentScope = scope, CurrentProfile = profiles.Current.Id, SameFinancialGraph = await MatchesAsync(services) };
            await File.WriteAllTextAsync(posting, JsonSerializer.Serialize(result));
            await reminders.Scheduler.ShowAsync(new ReminderNotification(request.Id, request.Title, request.Body, null, request.Link));
            await File.WriteAllTextAsync(Path.Combine(Root, phase + "-posted.json"), JsonSerializer.Serialize(result));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            await File.WriteAllTextAsync(Path.Combine(Root, "error.json"), JsonSerializer.Serialize(new { Run = run, Phase = phase, ErrorType = error.GetType().Name }));
            throw;
        }
    }

    /// <summary>Reads actual native callback/ready page and protected synthetic source, never initializing storage or invoking navigation.</summary>
    internal static async Task<string> ReadStatusAsync(IServiceProvider services)
    {
        RequirePackage(); var marker = Path.Combine(Root, "identity.json");
        var ids = File.Exists(marker) ? JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(marker)) : null;
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>(); var profiles = services.GetRequiredService<ProfileService>();
        var goals = services.GetRequiredService<GoalStore>(); var appLock = services.GetRequiredService<AppLockService>();
        var ui = await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var page = Shell.Current?.CurrentPage; var vm = page?.BindingContext as GoalsViewModel;
            return new { Page = page?.GetType().Name, Ready = vm?.Loading.IsReady == true,
                GoalIds = vm?.Active.Select(row => row.Id), Names = vm?.Active.Select(row => row.Name) };
        });
        var posted = new List<JsonElement>();
        foreach (var phase in new[] { "grouped", "restore" })
        { var file = Path.Combine(Root, phase + "-posted.json"); if (File.Exists(file)) posted.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(file))); }
        var taps = Directory.Exists(Root) ? Directory.GetFiles(Root, "tap-*.json").Select(file => JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(file))).ToArray() : [];
        var error = Path.Combine(Root, "error.json");
        return JsonSerializer.Serialize(new { Run = ids?.Run, Pid = Android.OS.Process.MyPid(), Protected = location.IsProtected,
            StartupComplete = ((App)Application.Current!).WaitForStartupAsync().IsCompletedSuccessfully,
            CurrentProfile = profiles.Current.Id, ProfileCount = profiles.All.Count,
            Scope = location.IsProtected && ids is not null ? (await goals.GetWorkSnapshotAsync()).ReminderScope : null,
            SourceMatch = location.IsProtected && ids is not null && await MatchesAsync(services),
            Locked = appLock.IsLocked, CredentialConfigured = appLock.IsEnabled, Destination = ui, Posted = posted, NativeTaps = taps,
            Complete = File.Exists(Path.Combine(Root, "complete.json")),
            Error = File.Exists(error) ? JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(error)) : (JsonElement?)null });
    }

    private static async Task<LocalProfile> ReadBAsync(ProfileService profiles)
    {
        var id = await File.ReadAllTextAsync(Path.Combine(Root, "profile-b.txt"));
        return profiles.All.Single(profile => profile.Id == id && !profile.IsMain);
    }

    private static async Task<bool> MatchesAsync(IServiceProvider services) => File.Exists(Path.Combine(Root, "financial-a.json"))
        && await File.ReadAllTextAsync(Path.Combine(Root, "financial-a.json")) == await FinancialAsync(services);

    private static async Task<string> FinancialAsync(IServiceProvider services)
    {
        var store = services.GetRequiredService<ZananceStore>(); var goals = services.GetRequiredService<GoalStore>();
        var plans = services.GetRequiredService<PlanStore>(); var holdings = services.GetRequiredService<HoldingStore>();
        return JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Goals = await goals.GetGoalsAsync(), Contributions = await goals.GetContributionPlansAsync(),
            Entries = await store.GetEntriesAsync(), Allocations = await goals.GetAllocationsAsync(), Plans = await plans.GetSchedulesAsync(), States = await plans.GetStatesAsync(),
            Types = await holdings.GetTypesAsync(), Events = await holdings.GetEventsAsync() });
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
            if (watch.Elapsed > TimeSpan.FromSeconds(10)) throw new IOException("Original source timer did not settle.");
        }
    }

    private static void RequirePackage()
    { if (AppInfo.Current.PackageName != NativeReminderReview.Package) throw new IOException("Independent profile scope identity required."); }
}
