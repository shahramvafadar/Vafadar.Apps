using System.Globalization;
using Vafadar.Core.Settings;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

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
    public void WatchSnoozes() => scheduler.SnoozeRequested += (_, request) => _ = SnoozeAsync(request);

    // REM-04: a snooze repeats the reminder; the occurrence and its due date stay unchanged.
    private async Task SnoozeAsync(SnoozeRequest request)
    {
        // The same lock as a refresh, which reads and writes the snoozes too.
        await _gate.WaitAsync();
        try
        {
            var original = request.Notification;
            var snooze = new SnoozedReminder(
                ReminderSnoozes.IdFor(original.Id), original.Link, original.Title, original.Body,
                ReminderSnoozes.NotifyAt(request.Choice, time.GetLocalNow().DateTime));
            preferences.Set(SnoozeKey, ReminderSnoozes.Serialize(ReminderSnoozes.Add(ReminderSnoozes.Deserialize(preferences.Get(SnoozeKey)), snooze)));
            await scheduler.ShowAsync(new ReminderNotification(snooze.Id, snooze.Title, snooze.Body, snooze.NotifyAt, snooze.Link, CanSnooze: true));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine($"Snooze failed: {ex}");
        }
        finally
        {
            _gate.Release();
        }
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

            var settings = await store.GetSettingsAsync();
            var accounts = (await store.GetAccountsAsync()).ToDictionary(a => a.Id);
            var culture = localization.CurrentCulture;
            scheduler.SetSnoozeActions(translator["Reminder_SnoozeHour"], translator["Reminder_SnoozeTomorrow"]);
            var schedules = await plans.GetSchedulesAsync();
            var states = await plans.GetStatesAsync();
            var now = time.GetLocalNow().DateTime;
            var planned = ReminderPlanner.Plan(schedules, states, now);

            var notifications = ReminderPlanner.GroupByTime(planned).Select(group =>
            {
                if (group.Count > 1)
                {
                    // One summary for items due at the same time (REM-10); names only when details are allowed.
                    var body = settings.NotificationsShowDetails
                        ? string.Join(translator["Reminder_ListSeparator"], group.Select(r => r.Occurrence.Schedule.Name))
                        : translator["Reminder_Generic"];
                    return new ReminderNotification(group[0].Id, translator.Format("Reminder_Summary", group.Count), body, group[0].NotifyAt, "plans", CanSnooze: true);
                }

                var reminder = group[0];
                var occurrence = reminder.Occurrence;
                var link = string.Create(CultureInfo.InvariantCulture, $"occurrence|{occurrence.Schedule.Id}|{occurrence.OriginalDate:yyyy-MM-dd}");
                if (!settings.NotificationsShowDetails)
                {
                    // Nothing about amounts, accounts or names on the lock screen unless the user chose it (REM-05, AT-38).
                    return new ReminderNotification(reminder.Id, translator["App_Name"], translator["Reminder_Generic"], reminder.NotifyAt, link, CanSnooze: true);
                }

                var currency = accounts.TryGetValue(occurrence.Schedule.AccountId, out var account) ? account.CurrencyCode : settings.ReportCurrencyCode;
                var amount = occurrence.Amount is { } value ? MoneyText.Format(value, currency, culture, approximate: occurrence.AmountMode == Core.Plans.AmountMode.Estimated) : translator["Plan_AmountUnknown"];
                return new ReminderNotification(
                    reminder.Id,
                    occurrence.Schedule.Name,
                    translator.Format("Reminder_Details", amount, dates.Format(occurrence.DueDate, DateFormatStyle.Long)),
                    reminder.NotifyAt,
                    link,
                    CanSnooze: true);
            }).ToList();

            // Contract dates (F2-CON-01): cancellation deadlines and review dates; names only when details are allowed.
            notifications.AddRange(ContractReminderPlanner.Plan(schedules, now).Select(reminder =>
            {
                var link = string.Create(CultureInfo.InvariantCulture, $"plan|{reminder.Schedule.Id}");
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

            // Snoozed reminders survive the rebuild while their occurrence is still open (REM-04, AT-36).
            bool IsOpen(string link)
            {
                if (link == "plans")
                {
                    return true;
                }

                if (link.Split('|') is not ["occurrence", var plan, var date] || !Guid.TryParse(plan, out var planId)
                    || !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var original))
                {
                    return false;
                }

                var state = states.FirstOrDefault(s => s.ScheduleId == planId && s.OriginalDate == original);
                return schedules.Any(s => s.Id == planId && s.State == Core.Plans.ScheduleState.Active)
                    && state?.Status is not (Core.Plans.OccurrenceStatus.Settled or Core.Plans.OccurrenceStatus.Skipped);
            }

            var snoozes = ReminderSnoozes.Keep(ReminderSnoozes.Deserialize(preferences.Get(SnoozeKey)), now, IsOpen);
            preferences.Set(SnoozeKey, snoozes.Count > 0 ? ReminderSnoozes.Serialize(snoozes) : null);
            notifications.AddRange(snoozes
                .Where(s => notifications.All(n => n.Id != s.Id))
                .Select(s => new ReminderNotification(s.Id, s.Title, s.Body, s.NotifyAt, s.Link, CanSnooze: true)));

            // One device queue across plan, contract, goal, review and snooze reminders, within the iOS pending limit.
            await scheduler.ReplaceAllAsync(notifications.OrderBy(n => n.NotifyAt).ThenBy(n => n.Id)
                .Take(ReminderPlanner.MaxPending).ToList());
            await CheckBudgetAsync(settings, accounts.Values.ToList(), culture);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine($"Reminders could not be updated: {ex}");
        }
        finally
        {
            _gate.Release();
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
