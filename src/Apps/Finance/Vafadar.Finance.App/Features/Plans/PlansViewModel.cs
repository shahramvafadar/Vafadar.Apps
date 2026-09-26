using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.App.Reminders;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Plans;

/// <summary>An occurrence or a plan in the plan lists.</summary>
public sealed record PlanRow(
    Guid ScheduleId,
    DateOnly? OriginalDate,
    string Title,
    string Subtitle,
    string AmountText,
    Color AmountColor,
    Symbol Icon,
    Color IconColor,
    Color IconBackground,
    string? Badge,
    Color BadgeText,
    Color BadgeBackground);

/// <summary>The plan centre (UI-07): due and overdue, upcoming and all plans. Works without notification permission (REM-02).</summary>
public sealed partial class PlansViewModel : ViewModelBase
{
    private const int UpcomingDays = 60;

    private static readonly Color WarningText = Color.FromArgb("#8D5B00");
    private static readonly Color WarningBackground = Color.FromArgb("#FFF4E0");
    private static readonly Color OverdueText = Color.FromArgb("#B71C1C");
    private static readonly Color OverdueBackground = Color.FromArgb("#FFEBEE");
    private static readonly Color NeutralText = Color.FromArgb("#37474F");
    private static readonly Color NeutralBackground = Color.FromArgb("#ECEFF1");

    private readonly FinanceStore _finance;
    private readonly PlanStore _plans;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private readonly ReminderService _reminders;
    private List<Schedule> _schedules = [];
    private List<OccurrenceState> _states = [];
    private Dictionary<Guid, Account> _accounts = [];
    private CategoryLookup? _categories;

    public PlansViewModel(FinanceStore finance, PlanStore plans, Translator translator, ILocalizationService localization, IDateFormatter dates, TimeProvider time, ReminderService reminders)
    {
        _reminders = reminders;
        _finance = finance;
        _plans = plans;
        _translator = translator;
        _localization = localization;
        _dates = dates;
        _time = time;
        SegmentNames = [translator["Plans_Due"], translator["Plans_Upcoming"], translator["Plans_All"]];
    }

    public IReadOnlyList<string> SegmentNames { get; }

    public ObservableCollection<PlanRow> Rows { get; } = [];

    [ObservableProperty]
    public partial int SegmentIndex { get; set; }

    [ObservableProperty]
    public partial int UnreviewedCount { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial string? EmptyText { get; set; }

    [ObservableProperty]
    public partial bool HasNoPlans { get; set; }

    [ObservableProperty]
    public partial bool RemindersOff { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public async Task LoadAsync()
    {
        _schedules = await _plans.GetSchedulesAsync();
        _states = await _plans.GetStatesAsync();
        _accounts = (await _finance.GetAccountsAsync()).ToDictionary(a => a.Id);
        _categories = new CategoryLookup(await _finance.GetCategoriesAsync(), _translator);
        UnreviewedCount = (await _finance.GetEntriesAsync()).Count(e => e.Review == ReviewState.Unreviewed);
        HasNoPlans = _schedules.Count == 0;

        // Reminders were chosen but notifications are not allowed: say so, the plans themselves keep working (AT-34).
        RemindersOff = _reminders.Scheduler.IsSupported
            && _schedules.Any(s => s.ReminderEnabled && s.State == ScheduleState.Active)
            && !await _reminders.Scheduler.AreEnabledAsync();
        Refresh();
    }

    partial void OnSegmentIndexChanged(int value) => Refresh();

    private void Refresh()
    {
        if (_categories is null)
        {
            return;
        }

        var text = new PlanText(_translator, _dates, _localization.CurrentCulture);
        var today = Today;
        Rows.Clear();

        switch (SegmentIndex)
        {
            case 0:
                foreach (var occurrence in _schedules.Where(s => s.State == ScheduleState.Active)
                             .SelectMany(s => Occurrences.OpenUpTo(s, _states, today, s.ActiveFrom ?? s.Rule.Start))
                             .OrderBy(o => o.DueDate))
                {
                    Rows.Add(OccurrenceRow(occurrence, text, today));
                }

                EmptyText = _translator[HasNoPlans ? "Plans_Empty" : "Plans_NothingDue"];
                break;

            case 1:
                foreach (var occurrence in _schedules.Where(s => s.State == ScheduleState.Active)
                             .SelectMany(s => Occurrences.Between(s, _states, today.AddDays(1), today.AddDays(UpcomingDays), today))
                             .Where(o => o.IsOpen)
                             .OrderBy(o => o.DueDate))
                {
                    Rows.Add(OccurrenceRow(occurrence, text, today));
                }

                EmptyText = _translator[HasNoPlans ? "Plans_Empty" : "Plans_NothingUpcoming"];
                break;

            default:
                foreach (var schedule in _schedules.Where(s => !_schedules.Any(n => n.PreviousScheduleId == s.Id)))
                {
                    Rows.Add(ScheduleRow(schedule, text, today));
                }

                EmptyText = _translator["Plans_Empty"];
                break;
        }

        IsEmpty = Rows.Count == 0;
    }

    private PlanRow OccurrenceRow(Occurrence occurrence, PlanText text, DateOnly today)
    {
        var schedule = occurrence.Schedule;
        var (icon, color) = Look(schedule);
        var (badge, badgeText, badgeBackground) = occurrence.Status switch
        {
            OccurrenceView.Overdue => (_translator.Format("Occurrence_OverdueDays", today.DayNumber - occurrence.DueDate.DayNumber), OverdueText, OverdueBackground),
            OccurrenceView.Due => (_translator["Occurrence_Due"], WarningText, WarningBackground),
            _ => ((string?)null, NeutralText, NeutralBackground),
        };

        return new PlanRow(
            schedule.Id,
            occurrence.OriginalDate,
            schedule.Name,
            text.Date(occurrence.DueDate),
            text.Amount(occurrence.Amount, occurrence.AmountMode, CurrencyOf(schedule.AccountId)),
            AmountColor(schedule.Kind),
            icon,
            color,
            color.WithAlpha(0.12f),
            badge,
            badgeText,
            badgeBackground);
    }

    private PlanRow ScheduleRow(Schedule schedule, PlanText text, DateOnly today)
    {
        var (icon, color) = Look(schedule);
        var next = schedule.State == ScheduleState.Active ? Occurrences.NextOpen(schedule, _states, today, today) : null;
        var subtitle = text.Rule(schedule.Rule);
        if (next is not null)
        {
            subtitle += Environment.NewLine + _translator.Format("Plan_NextDue", text.Date(next.DueDate));
        }

        var (badge, badgeText, badgeBackground) = schedule.State switch
        {
            ScheduleState.Paused => (_translator["Plan_Paused"], WarningText, WarningBackground),
            ScheduleState.Ended => (_translator["Plan_Ended"], NeutralText, NeutralBackground),
            _ => ((string?)null, NeutralText, NeutralBackground),
        };

        return new PlanRow(
            schedule.Id,
            null,
            schedule.Name,
            subtitle,
            text.Amount(schedule.Amount, schedule.AmountMode, CurrencyOf(schedule.AccountId)),
            AmountColor(schedule.Kind),
            icon,
            color,
            color.WithAlpha(0.12f),
            badge,
            badgeText,
            badgeBackground);
    }

    private (Symbol Icon, Color Color) Look(Schedule schedule) => schedule.Kind == EntryKind.Transfer
        ? (Symbol.ArrowSwap, EntryPresenter.NeutralColor)
        : (Icons.Parse(schedule.Icon, _categories!.Icon(schedule.CategoryId)), _categories.Color(schedule.CategoryId));

    private static Color AmountColor(EntryKind kind) => kind switch
    {
        EntryKind.Income => EntryPresenter.IncomeColor,
        EntryKind.Expense => EntryPresenter.ExpenseColor,
        _ => EntryPresenter.NeutralColor,
    };

    private string CurrencyOf(Guid accountId) => _accounts.TryGetValue(accountId, out var account) ? account.CurrencyCode : "EUR";

    [RelayCommand]
    private Task OpenAsync(PlanRow row) => row.OriginalDate is { } date
        ? Shell.Current.GoToAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = row.ScheduleId, ["date"] = date })
        : Shell.Current.GoToAsync(AppShell.PlanDetailRoute, new Dictionary<string, object> { ["id"] = row.ScheduleId });

    [RelayCommand]
    private async Task AddAsync()
    {
        var hasAccounts = (await _finance.GetAccountsAsync(includeArchived: false)).Count > 0;
        await Shell.Current.GoToAsync(hasAccounts ? AppShell.PlanEditorRoute : AppShell.AccountEditorRoute);
    }

    [RelayCommand]
    private async Task EnableRemindersAsync()
    {
        RemindersOff = !await _reminders.EnsurePermissionAsync();
        _reminders.RefreshSoon();
    }

    [RelayCommand]
    private Task ReviewEntriesAsync() => Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["unreviewed"] = true });
}
