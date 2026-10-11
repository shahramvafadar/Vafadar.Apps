using System.Globalization;
using System.Text.Json;
using Vafadar.Core.Settings;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Reminders;

/// <summary>
/// Keeps device notifications in line with the data: reminders for due plan occurrences (REM-01..10), goal contributions, period reviews
/// and the 80 %/100 % budget alerts (BUD-06). It rebuilds everything on start, resume, after changes and after a restore, so there are no
/// duplicates and nothing outdated (REM-07, AT-36, AT-37).
/// </summary>
public sealed class ReminderService(
    IReminderScheduler scheduler,
    ZananceStore store,
    PlanStore plans,
    GoalStore goals,
    HoldingStore holdings,
    ISettingsStore preferences,
    Translator translator,
    IDateFormatter dates,
    ILocalizationService localization,
    TimeProvider time,
    ResourceChoiceStore? choices = null,
    BudgetChoiceStore? budgetChoices = null) : IDisposable, IAsyncDisposable
{
    private const string BudgetAlertKey = "budget.alert.";
    private const string SnoozeKey = "reminders.snoozed";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _pending;
    private readonly object _changesGate = new();
    private Profiles.IProfileCatalogueChanges? _watchedProfiles;
    private bool _watching;
    private bool _watchingSnoozes;
    private int _disposed;
    private Task _pendingRefresh = Task.CompletedTask;

    /// <summary>Gets the current coalesced batch, including its final guards, for exact owned-lifetime review.</summary>
    internal Task PendingRefresh { get { lock (_changesGate) return _pendingRefresh; } }

    /// <summary>Gets the scheduler (for permission prompts and tap handling).</summary>
    public IReminderScheduler Scheduler => scheduler;

    /// <summary>Watches original financial/resource/catalogue changes once; subscription does not read a database.</summary>
    public void WatchChanges(Profiles.IProfileCatalogueChanges? profiles = null)
    {
        lock (_changesGate)
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            if (_watching)
            {
                if (!ReferenceEquals(profiles, _watchedProfiles))
                    throw new InvalidOperationException("A reminder watcher cannot inherit another original catalogue.");
                return;
            }
            _watchedProfiles = profiles;
            store.Changed += OnDataChanged; plans.Changed += OnDataChanged;
            if (choices is not null) choices.Changed += OnDataChanged;
            if (budgetChoices is not null) budgetChoices.Changed += OnDataChanged;
            goals.Changed += OnDataChanged; holdings.Changed += OnDataChanged;
            if (profiles is not null) profiles.Changed += OnDataChanged;
            _watching = true;
        }
    }

    private void OnDataChanged(object? sender, EventArgs args) => RefreshSoon();
    private void OnSnoozeRequested(object? sender, SnoozeRequest request) => _ = SnoozeAsync(request);

    /// <summary>Detaches lifetime-scoped sources and cancels delayed work without deleting valid native reminders.</summary>
    public void Dispose()
    {
        lock (_changesGate)
        {
            if (_disposed != 0) return;
            Volatile.Write(ref _disposed, 1);
            if (_watching)
            {
                store.Changed -= OnDataChanged; plans.Changed -= OnDataChanged;
                if (choices is not null) choices.Changed -= OnDataChanged;
                if (budgetChoices is not null) budgetChoices.Changed -= OnDataChanged;
                goals.Changed -= OnDataChanged; holdings.Changed -= OnDataChanged;
                if (_watchedProfiles is not null) _watchedProfiles.Changed -= OnDataChanged;
            }
            if (_watchingSnoozes) scheduler.SnoozeRequested -= OnSnoozeRequested;
            _pending?.Cancel(); _pending?.Dispose(); _pending = null;
            // Native awaits may already own the semaphore; do not dispose it underneath their final Release.
        }
    }

    /// <summary>Detaches delayed work and drains the current native/database batch before an owned fixture is removed.</summary>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        try { await PendingRefresh; } catch (OperationCanceledException) { }
        await _gate.WaitAsync();
        _gate.Release();
    }

    /// <summary>
    /// Handles snooze actions. Registered at app start, so it also works when Android starts the process only to deliver
    /// the action (the app is not opened).
    /// </summary>
    public void WatchSnoozes()
    {
        // A scheduled Android receiver can initialize the process without opening a page. No database startup work.
        lock (_changesGate)
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            if (_watchingSnoozes) return;
            scheduler.SetSnoozeActions(translator["Reminder_SnoozeHour"], translator["Reminder_SnoozeTomorrow"]);
            scheduler.SnoozeRequested += OnSnoozeRequested;
            _watchingSnoozes = true;
        }
    }

    /// <summary>Rebuilds a snoozed occurrence from current file, rule, rights and privacy; never changes its due date.</summary>
    public async Task SnoozeAsync(SnoozeRequest request)
    {
        if (Volatile.Read(ref _disposed) != 0 || !scheduler.IsSupported) return;
        await _gate.WaitAsync();
        try
        {
            if (Volatile.Read(ref _disposed) != 0 || !await scheduler.AreEnabledAsync()) return;
            await RefreshUnderGateAsync(request);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (ex is CommercialWriteRejectedException) await scheduler.ReplaceAllAsync([]);
            System.Diagnostics.Debug.WriteLine($"Snooze failed: {ex}");
        }
        finally { _gate.Release(); }
    }

    /// <summary>Schedules a refresh after a short delay; later calls within the delay replace earlier ones.</summary>
    public void RefreshSoon()
    {
        // Catalogue events and disposal may race financial events from a writer thread. Serialize only the
        // short timer/subscription state; no provider, database or native await runs under this gate.
        lock (_changesGate)
        {
            if (_disposed != 0) return;
            var next = new CancellationTokenSource(); var token = next.Token;
            _pending?.Cancel(); _pending?.Dispose(); _pending = next;
            _pendingRefresh = Task.Delay(TimeSpan.FromSeconds(1.5), time, token).ContinueWith(_ => RefreshAsync(), token,
                TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default).Unwrap();
        }
    }

    /// <summary>Rebuilds reminders and checks budget alerts; failures never affect the ledger.</summary>
    public async Task RefreshAsync()
    {
        if (Volatile.Read(ref _disposed) != 0 || !scheduler.IsSupported)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (Volatile.Read(ref _disposed) != 0 || !await scheduler.AreEnabledAsync())
            {
                return;
            }

            await RefreshUnderGateAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (ex is CommercialWriteRejectedException) await scheduler.ReplaceAllAsync([]);
            System.Diagnostics.Debug.WriteLine($"Reminders could not be updated: {ex}");
        }
        finally { _gate.Release(); }
    }

    // Refresh and action share the gate and queue cap. Do not persist an action before its actual-file read succeeds.
    private async Task RefreshUnderGateAsync(SnoozeRequest? request = null)
    {
        PlanWorkSnapshot snapshot;
        GoalWorkSnapshot goalSnapshot;
        try { snapshot = await plans.GetWorkSnapshotAsync(); goalSnapshot = await goals.GetWorkSnapshotAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // A retired read must also clear the older queue, before any new native work is published.
            await scheduler.ReplaceAllAsync([]);
            throw;
        }
        var now = time.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(now);
        ZananceSettings settings; ReminderFinancialSnapshot financials;
        try
        {
            settings = await store.GetSettingsAsync();
            financials = await store.GetReminderFinancialSnapshotAsync(snapshot, goalSnapshot, today);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A failed original financial read must retire the older pending queue before any new publication.
            await scheduler.ReplaceAllAsync([]); throw;
        }
        var accounts = financials.Accounts.ToDictionary(a => a.Id);
        var culture = localization.CurrentCulture;
        var originalDisplay = CaptureDisplay();
        scheduler.SetSnoozeActions(translator["Reminder_SnoozeHour"], translator["Reminder_SnoozeTomorrow"]);
        var schedules = snapshot.Schedules;
        var states = snapshot.States;
        // Filter before the planner's cap: unavailable plans must not displace selected reminders.
        var planned = ReminderPlanner.Plan(schedules.Where(s => snapshot.Work.CanRemind(s.Id)), states, now);
        var notifications = ReminderPlanner.GroupByTime(planned).Select(group =>
            Describe(group[0].Id, group[0].NotifyAt, group.Select(r => r.Occurrence).ToList())).ToList();

        // Contract dates (F2-CON-01): cancellation deadlines and review dates; names only when details are allowed.
        notifications.AddRange(ContractReminderPlanner.Plan(schedules.Where(s => snapshot.Work.CanRemind(s.Id, contract: true)), now).Select(reminder =>
        {
            var link = PlanReminderLink.Format(snapshot.ReminderScope, [new(reminder.Schedule.Id, null)]);
            if (!settings.NotificationsShowDetails)
            {
                return new ReminderNotification(reminder.Id, translator["App_Name"], translator["Reminder_ContractGeneric"], reminder.NotifyAt, link);
            }

            var key = reminder.Kind == ContractDateKind.Cancellation ? "Reminder_CancelBy" : "Reminder_ReviewOn";
            return new ReminderNotification(reminder.Id, reminder.Schedule.Name, translator.Format(key, dates.Format(reminder.Date, DateFormatStyle.Long)), reminder.NotifyAt, link);
        }));

        // Contributions only remind the user. Progress includes funded earmarks and held quantities, not prices.
        var contributionPlans = goalSnapshot.ContributionPlans;
        if (contributionPlans.Any(p => p.ReminderEnabled))
        {
            var progress = GoalProgressService.Evaluate(
                goalSnapshot.Goals, goalSnapshot.Allocations, accounts.Values.ToList(),
                financials.Entries, contributionPlans, today, financials.Events, financials.Types);
            // Apply selection/paid eligibility before the cap, retaining every original input in progress evaluation.
            notifications.AddRange(GoalReminderPlanner.Plan(progress.Where(p => goalSnapshot.Work.CanRemind(p.Goal.Id)), contributionPlans, now)
                .GroupBy(r => r.NotifyAt).Select(group =>
                {
                    var first = group.First();
                    var items = group.ToList();
                    var single = items.Count == 1;
                    var link = GoalReminderLink.Format(goalSnapshot.ReminderScope, items.Select(item => new GoalReminderTarget(item.Goal.Id, item.Date)));
                    var title = settings.NotificationsShowDetails
                        ? single ? first.Goal.Name : translator.Format("Reminder_GoalSummary", items.Count)
                        : translator["App_Name"];
                    var body = settings.NotificationsShowDetails
                        ? single ? translator.Format("Reminder_GoalDetails", dates.Format(first.Date, DateFormatStyle.Long))
                            : string.Join(translator["Reminder_ListSeparator"], items.Select(r => r.Goal.Name))
                        : translator["Reminder_GoalGeneric"];
                    return new ReminderNotification(first.Id, title, body, first.NotifyAt, link);
                }));
        }

        // Match Home and the review page's display calendar, independently of the language or new-budget defaults.
        var reviewCalendar = localization.CurrentCalendar switch
        {
            CalendarSystem.Persian => PeriodCalendar.Persian,
            CalendarSystem.Hijri => PeriodCalendar.Hijri,
            _ => PeriodCalendar.Gregorian,
        };
        var firstData = accounts.Count == 0 ? (DateOnly?)null : accounts.Values.Min(a => a.OpeningDate);
        var originalSettingsPacket = JsonSerializer.Serialize(settings);
        notifications.AddRange(ReviewReminderPlanner.Plan(settings.ReviewReminderEnabled && snapshot.CanRemindPeriodReview, settings.ReviewProgress,
            now, reviewCalendar, settings.MonthStartDay, firstData).Select(reminder =>
            new ReminderNotification(reminder.Id, translator["App_Name"],
                settings.NotificationsShowDetails
                    ? translator.Format("Reminder_ReviewDetails", dates.Format(reminder.PeriodFirst, DateFormatStyle.MonthYear))
                    : translator["Reminder_ReviewGeneric"], reminder.NotifyAt, "review")));

        var snoozes = new List<SnoozedReminder>();
        foreach (var saved in ReminderSnoozes.Deserialize(preferences.Get(SnoozeKey)).Take(ReminderSnoozes.MaxSnoozes))
        {
            if (saved.NotifyAt <= now || Rebuild(saved.Id, saved.NotifyAt, saved.Link) is not { } current) continue;
            snoozes.Add(new(current.Id, current.Link, current.Title, current.Body, saved.NotifyAt));
        }
        if (request is not null)
        {
            var id = ReminderSnoozes.IdFor(request.Notification.Id);
            var at = ReminderSnoozes.NotifyAt(request.Choice, now);
            if (Rebuild(id, at, request.Notification.Link) is { } current)
                snoozes = ReminderSnoozes.Add(snoozes, new(id, current.Link, current.Title, current.Body, at)).ToList();
        }

        // Legacy unscoped groups cannot prove their profile or members. Drop that cache, rebuilding normal reminders.
        // The original routing format remains supported for retained historical taps, not for new snoozes.
        await ValidateCurrentBatchAsync();
        notifications.AddRange(snoozes.Where(s => notifications.All(n => n.Id != s.Id))
            .Select(s => new ReminderNotification(s.Id, s.Title, s.Body, s.NotifyAt, s.Link, CanSnooze: true)));
        try
        {
            await scheduler.ReplaceAllAsync(notifications.OrderBy(n => n.NotifyAt).ThenBy(n => n.Id)
                .Take(ReminderPlanner.MaxPending).ToList());
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // SDK refusal can follow a partially accepted batch; keep its cache retryable and withdraw pending work.
            await scheduler.ReplaceAllAsync([]); throw;
        }
        await ValidateCurrentBatchAsync();
        preferences.Set(SnoozeKey, snoozes.Count > 0 ? ReminderSnoozes.Serialize(snoozes) : null);
        await CheckBudgetAsync(culture);

        string CaptureDisplay() => JsonSerializer.Serialize(new
        {
            Language = localization.CurrentLanguage.CultureName, Culture = localization.CurrentCulture.Name,
            localization.FormattingCultureName, localization.CurrentCalendar, localization.CurrentDigits,
            NativeDigits.IsEnabled, NativeDigits.PreserveSeparators,
        });
        async Task ValidateCurrentBatchAsync()
        {
            try
            {
                await store.ValidateReminderPreparationAsync(financials, snapshot, goalSnapshot, originalSettingsPacket, firstData);
                if (originalDisplay != CaptureDisplay() || today != DateOnly.FromDateTime(time.GetLocalNow().DateTime))
                    throw new InvalidOperationException("The original reminder display or local day changed.");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // The old queue and a newly accepted native batch must both retire when privacy/period facts change.
                await scheduler.ReplaceAllAsync([]); throw;
            }
        }

        ReminderNotification? Rebuild(int id, DateTime at, string link)
        {
            if (PlanReminderLink.Parse(link) is not { IsContract: false } target || target.Scope != snapshot.ReminderScope)
                return null;
            var items = new List<Occurrence>();
            foreach (var member in target.Targets)
            {
                var schedule = schedules.FirstOrDefault(s => s.Id == member.PlanId);
                if (schedule is not { State: ScheduleState.Active, ReminderEnabled: true }
                    || !snapshot.Work.CanRemind(schedule.Id)) continue;
                if (Occurrences.Find(schedule, states, member.OriginalDate!.Value, today) is { IsOpen: true } occurrence)
                    items.Add(occurrence);
            }
            return items.Count == 0 ? null : Describe(id, at, items);
        }

        ReminderNotification Describe(int id, DateTime at, IReadOnlyList<Occurrence> items)
        {
            var link = PlanReminderLink.Format(snapshot.ReminderScope, items.Select(o => new PlanReminderTarget(o.Schedule.Id, o.OriginalDate)));
            if (!settings.NotificationsShowDetails)
                return new(id, translator["App_Name"], translator["Reminder_Generic"], at, link, CanSnooze: true);
            if (items.Count > 1)
                return new(id, translator.Format("Reminder_Summary", items.Count),
                    string.Join(translator["Reminder_ListSeparator"], items.Select(o => o.Schedule.Name)), at, link, CanSnooze: true);
            var occurrence = items[0];
            var currency = accounts.TryGetValue(occurrence.Schedule.AccountId, out var account)
                ? account.CurrencyCode : settings.ReportCurrencyCode;
            var amount = occurrence.Amount is { } value
                ? MoneyText.Format(value, currency, culture, approximate: occurrence.AmountMode == AmountMode.Estimated)
                : translator["Plan_AmountUnknown"];
            return new(id, occurrence.Schedule.Name,
                translator.Format("Reminder_Details", amount, dates.Format(occurrence.DueDate, DateFormatStyle.Long)), at, link, CanSnooze: true);
        }
    }

    /// <summary>Asks for notification permission when the user turns a reminder on (REM-03).</summary>
    public async Task<bool> EnsurePermissionAsync()
    {
        if (!scheduler.IsSupported)
        {
            return false;
        }

        return await scheduler.AreEnabledAsync() || await scheduler.RequestPermissionAsync();
    }

    // Each original current period has its own threshold cache. New-work rights affect delivery, never financial inputs.
    private async Task CheckBudgetAsync(CultureInfo culture)
    {
        var snapshot = await store.GetBudgetAlertSnapshotAsync();
        var updates = new List<(string Key, string Value)>();
        var published = new List<int>();
        try
        {
            foreach (var candidate in snapshot.Candidates)
            {
                var status = candidate.Status;
                var key = BudgetAlertKey + candidate.BudgetId.ToString("N");
                var previous = int.TryParse(preferences.Get(key), NumberStyles.None, CultureInfo.InvariantCulture, out var level) ? level : 0;
                var current = (int)status.Alert;
                if (current == previous) continue;
                if (current > previous)
                {
                    await store.ValidateBudgetAlertSnapshotAsync(snapshot);
                    var body = !snapshot.ShowDetails
                        ? translator["Reminder_BudgetGeneric"]
                        : status.IsOver
                            ? translator.Format("Budget_Over", MoneyText.Format(-status.Remaining, candidate.CurrencyCode, culture))
                            : translator.Format("Home_BudgetLeft", MoneyText.Format(status.Remaining, candidate.CurrencyCode, culture),
                                MoneyText.Format(status.Limit, candidate.CurrencyCode, culture));
                    var id = ReminderPlanner.StableId(candidate.BudgetId, snapshot.Today) ^ 0x4000_0000;
                    // Track before Show: a native failure can occur after a platform partially publishes the notification.
                    published.Add(id);
                    await scheduler.ShowAsync(new(id, translator["Budget_Title"], body, null,
                        BudgetReminderLink.Format(snapshot.ReminderScope, candidate.BudgetId)));
                    await store.ValidateBudgetAlertSnapshotAsync(snapshot);
                }
                // A refund can lower the level. Commit all device caches only after the entire batch remains valid.
                updates.Add((key, current.ToString(CultureInfo.InvariantCulture)));
            }
            await store.ValidateBudgetAlertSnapshotAsync(snapshot);
            foreach (var update in updates) preferences.Set(update.Key, update.Value);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            foreach (var id in published) await scheduler.RemoveAsync(id);
            throw;
        }
    }
}
