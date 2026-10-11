using System.Globalization;
using Vafadar.Core.Settings;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
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
    TimeProvider time)
{
    private const string BudgetAlertKey = "budget.alert.";
    private const string SnoozeKey = "reminders.snoozed";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _pending;

    /// <summary>Gets the scheduler (for permission prompts and tap handling).</summary>
    public IReminderScheduler Scheduler => scheduler;

    /// <summary>Refreshes shortly after every change of entries, plans, goals or holdings (a burst of changes refreshes once).</summary>
    public void WatchChanges()
    {
        store.Changed += (_, _) => RefreshSoon();
        plans.Changed += (_, _) => RefreshSoon();
        goals.Changed += (_, _) => RefreshSoon();
        holdings.Changed += (_, _) => RefreshSoon();
    }

    /// <summary>
    /// Handles snooze actions. Registered at app start, so it also works when Android starts the process only to deliver
    /// the action (the app is not opened).
    /// </summary>
    public void WatchSnoozes()
    {
        // A scheduled Android receiver can initialize the process without opening a page. No database startup work.
        scheduler.SetSnoozeActions(translator["Reminder_SnoozeHour"], translator["Reminder_SnoozeTomorrow"]);
        scheduler.SnoozeRequested += (_, request) => _ = SnoozeAsync(request);
    }

    /// <summary>Rebuilds a snoozed occurrence from current file, rule, rights and privacy; never changes its due date.</summary>
    public async Task SnoozeAsync(SnoozeRequest request)
    {
        if (!scheduler.IsSupported) return;
        await _gate.WaitAsync();
        try
        {
            if (!await scheduler.AreEnabledAsync()) return;
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
        // Changes are reported from any thread: swap the timer atomically, so two changes at once can never both start a
        // refresh, and release the one it replaces.
        var next = new CancellationTokenSource();
        var token = next.Token;
        if (Interlocked.Exchange(ref _pending, next) is { } previous)
        {
            previous.Cancel();
            previous.Dispose();
        }

        _ = Task.Delay(TimeSpan.FromSeconds(1.5), token).ContinueWith(_ => RefreshAsync(), token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    /// <summary>Rebuilds reminders and checks budget alerts; failures never affect the ledger.</summary>
    public async Task RefreshAsync()
    {
        if (!scheduler.IsSupported)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (!await scheduler.AreEnabledAsync())
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
        var snapshot = await plans.GetWorkSnapshotAsync();
        var settings = await store.GetSettingsAsync();
        var accounts = (await store.GetAccountsAsync()).ToDictionary(a => a.Id);
        var culture = localization.CurrentCulture;
        scheduler.SetSnoozeActions(translator["Reminder_SnoozeHour"], translator["Reminder_SnoozeTomorrow"]);
        var schedules = snapshot.Schedules;
        var states = snapshot.States;
        var now = time.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(now);
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
        var contributionPlans = await goals.GetContributionPlansAsync();
        if (contributionPlans.Any(p => p.ReminderEnabled))
        {
            var progress = GoalProgressService.Evaluate(
                await goals.GetGoalsAsync(), await goals.GetAllocationsAsync(), accounts.Values.ToList(),
                await store.GetEntriesAsync(), contributionPlans, DateOnly.FromDateTime(now),
                await holdings.GetEventsAsync(), await holdings.GetTypesAsync());
            notifications.AddRange(GoalReminderPlanner.Plan(progress, contributionPlans, now)
                .GroupBy(r => r.NotifyAt).Select(group =>
                {
                    var first = group.First();
                    var items = group.ToList();
                    var single = items.Count == 1;
                    var link = single ? string.Create(CultureInfo.InvariantCulture, $"goal|{first.Goal.Id}") : "goals";
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
        notifications.AddRange(ReviewReminderPlanner.Plan(settings.ReviewReminderEnabled, settings.ReviewProgress,
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
        await ValidatePublicationAsync(snapshot);
        preferences.Set(SnoozeKey, snoozes.Count > 0 ? ReminderSnoozes.Serialize(snoozes) : null);
        notifications.AddRange(snoozes.Where(s => notifications.All(n => n.Id != s.Id))
            .Select(s => new ReminderNotification(s.Id, s.Title, s.Body, s.NotifyAt, s.Link, CanSnooze: true)));
        await scheduler.ReplaceAllAsync(notifications.OrderBy(n => n.NotifyAt).ThenBy(n => n.Id)
            .Take(ReminderPlanner.MaxPending).ToList());
        await ValidatePublicationAsync(snapshot);
        await CheckBudgetAsync(settings, accounts.Values.ToList(), culture);

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

    // Retire both a previously pending queue and a newly published one when this read loses its file/rights.
    // Otherwise a failure before native publication leaves earlier unavailable work scheduled (D-140).
    private async Task ValidatePublicationAsync(PlanWorkSnapshot snapshot)
    {
        try { await plans.ValidateWorkSnapshotAsync(snapshot); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            await scheduler.ReplaceAllAsync([]);
            throw;
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

    // One alert per level and period: opening the app again or editing an entry never repeats it (BUD-06). The budget
    // of this month and, when there are any, the weekly and two-week budgets of today are checked – in every currency
    // that has a budget, not only the one shown on Home (ZEX-P02).
    private async Task CheckBudgetAsync(Core.Settings.ZananceSettings settings, List<Core.Accounts.Account> accounts, CultureInfo culture)
    {
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var (year, month) = PeriodMath.MonthOf(today, settings.BudgetCalendar, settings.MonthStartDay);
        var categories = await store.GetCategoriesAsync();
        var budgets = new List<Budget?>();
        foreach (var currency in (await store.GetBudgetsAsync()).Select(b => b.CurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            budgets.Add(await store.GetBudgetAsync(year, month, settings.BudgetCalendar, currency));
            budgets.Add(await store.GetBudgetAsync(BudgetPeriod.Week, today, currency));
            budgets.Add(await store.GetBudgetAsync(BudgetPeriod.TwoWeeks, today, currency));
        }

        foreach (var budget in budgets)
        {
            if (budget is { AlertsEnabled: true, TotalLimit: not null })
            {
                await CheckBudgetAsync(budget, settings, accounts, categories, culture, today);
            }
        }
    }

    private async Task CheckBudgetAsync(Budget budget, Core.Settings.ZananceSettings settings, List<Core.Accounts.Account> accounts, IReadOnlyList<Core.Categories.Category> categories, CultureInfo culture, DateOnly today)
    {
        var limit = budget.TotalLimit!.Value + (await store.GetBudgetCarryAsync(budget)).Total;
        var (from, to) = BudgetPeriods.Range(budget, settings.MonthStartDay);
        var entries = await store.GetEntriesAsync(from, to);
        var status = new BudgetStatus(limit, FlexCalculator.SpentAgainstLimit(budget, accounts, entries, categories, from, to));
        var key = BudgetAlertKey + budget.Id.ToString("N");
        var previous = int.TryParse(preferences.Get(key), NumberStyles.None, CultureInfo.InvariantCulture, out var level) ? level : 0;
        var current = (int)status.Alert;
        if (current <= previous)
        {
            if (current < previous)
            {
                // Spending went down again (e.g. a refund): allow the alert to fire again later.
                preferences.Set(key, current.ToString(CultureInfo.InvariantCulture));
            }

            return;
        }

        preferences.Set(key, current.ToString(CultureInfo.InvariantCulture));
        var body = !settings.NotificationsShowDetails
            ? translator["Reminder_BudgetGeneric"]
            : status.IsOver
                ? translator.Format("Budget_Over", MoneyText.Format(-status.Remaining, budget.CurrencyCode, culture))
                : translator.Format("Home_BudgetLeft", MoneyText.Format(status.Remaining, budget.CurrencyCode, culture), MoneyText.Format(limit, budget.CurrencyCode, culture));
        await scheduler.ShowAsync(new ReminderNotification(ReminderPlanner.StableId(budget.Id, today) ^ 0x4000_0000, translator["Budget_Title"], body, null, "budget"));
    }
}
