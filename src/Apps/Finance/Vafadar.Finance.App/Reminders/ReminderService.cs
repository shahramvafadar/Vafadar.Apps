using System.Globalization;
using Vafadar.Core.Settings;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Core.Reminders;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;

namespace Vafadar.Finance.App.Reminders;

/// <summary>
/// Keeps device notifications in line with the data: reminders for due plan occurrences (REM-01..10) and the 80 %/100 %
/// budget alerts (BUD-06). It rebuilds everything on start, resume, after changes and after a restore, so there are no
/// duplicates and nothing outdated (REM-07, AT-36, AT-37).
/// </summary>
public sealed class ReminderService(
    IReminderScheduler scheduler,
    FinanceStore finance,
    PlanStore plans,
    ISettingsStore preferences,
    Translator translator,
    IDateFormatter dates,
    ILocalizationService localization,
    TimeProvider time)
{
    private const string BudgetAlertKey = "budget.alert.";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _pending;

    /// <summary>Gets the scheduler (for permission prompts and tap handling).</summary>
    public IReminderScheduler Scheduler => scheduler;

    /// <summary>Refreshes shortly after every change of entries, plans or settings (a burst of changes refreshes once).</summary>
    public void WatchChanges()
    {
        finance.Changed += (_, _) => RefreshSoon();
        plans.Changed += (_, _) => RefreshSoon();
    }

    /// <summary>Schedules a refresh after a short delay; later calls within the delay replace earlier ones.</summary>
    public void RefreshSoon()
    {
        _pending?.Cancel();
        _pending = new CancellationTokenSource();
        var token = _pending.Token;
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

            var settings = await finance.GetSettingsAsync();
            var accounts = (await finance.GetAccountsAsync()).ToDictionary(a => a.Id);
            var culture = localization.CurrentCulture;
            var planned = ReminderPlanner.Plan(await plans.GetSchedulesAsync(), await plans.GetStatesAsync(), time.GetLocalNow().DateTime);

            var notifications = ReminderPlanner.GroupByTime(planned).Select(group =>
            {
                if (group.Count > 1)
                {
                    // One summary for items due at the same time (REM-10); names only when details are allowed.
                    var body = settings.NotificationsShowDetails
                        ? string.Join(translator["Reminder_ListSeparator"], group.Select(r => r.Occurrence.Schedule.Name))
                        : translator["Reminder_Generic"];
                    return new ReminderNotification(group[0].Id, translator.Format("Reminder_Summary", group.Count), body, group[0].NotifyAt, "plans");
                }

                var reminder = group[0];
                var occurrence = reminder.Occurrence;
                var link = string.Create(CultureInfo.InvariantCulture, $"occurrence|{occurrence.Schedule.Id}|{occurrence.OriginalDate:yyyy-MM-dd}");
                if (!settings.NotificationsShowDetails)
                {
                    // Nothing about amounts, accounts or names on the lock screen unless the user chose it (REM-05, AT-38).
                    return new ReminderNotification(reminder.Id, translator["App_Name"], translator["Reminder_Generic"], reminder.NotifyAt, link);
                }

                var currency = accounts.TryGetValue(occurrence.Schedule.AccountId, out var account) ? account.CurrencyCode : settings.ReportCurrencyCode;
                var amount = occurrence.Amount is { } value ? MoneyText.Format(value, currency, culture, approximate: occurrence.AmountMode == Core.Plans.AmountMode.Estimated) : translator["Plan_AmountUnknown"];
                return new ReminderNotification(
                    reminder.Id,
                    occurrence.Schedule.Name,
                    translator.Format("Reminder_Details", amount, dates.Format(occurrence.DueDate, DateFormatStyle.Long)),
                    reminder.NotifyAt,
                    link);
            }).ToList();

            await scheduler.ReplaceAllAsync(notifications);
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

    // One alert per level and month: opening the app again or editing an entry never repeats it (BUD-06).
    private async Task CheckBudgetAsync(Core.Settings.FinanceSettings settings, List<Core.Accounts.Account> accounts, CultureInfo culture)
    {
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var (year, month) = PeriodMath.MonthOf(today, settings.BudgetCalendar);
        var budget = await finance.GetBudgetAsync(year, month, settings.BudgetCalendar, settings.ReportCurrencyCode);
        if (budget is not { AlertsEnabled: true, TotalLimit: { } limit })
        {
            return;
        }

        var (from, to) = PeriodMath.MonthRange(year, month, settings.BudgetCalendar);
        var entries = await finance.GetEntriesAsync(from, to);
        var status = new BudgetStatus(limit, BudgetCalculator.NetExpense(accounts, entries, from, to, budget.CurrencyCode, budget.AccountIds.Count > 0 ? budget.AccountIds : null));
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
