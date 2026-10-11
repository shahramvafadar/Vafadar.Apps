using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

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
    Color BadgeBackground)
{
    /// <summary>Gets the day of the due date for a date tile; <see langword="null"/> for plan rows, which show an icon.</summary>
    public string? DayText { get; init; }

    /// <summary>Gets the month of the due date.</summary>
    public string? MonthText { get; init; }

    /// <summary>Gets the text colour of the date tile: violet for the future, red when overdue, green for income (D-27).</summary>
    public Color? TileText { get; init; }

    /// <summary>Gets the background of the date tile.</summary>
    public Color? TileBackground { get; init; }

    /// <summary>Gets the outline of the date or icon tile.</summary>
    public Color? TileStroke { get; init; }

    /// <summary>Gets the outline of the badge.</summary>
    public Color? BadgeStroke { get; init; }

    /// <summary>Gets a value indicating whether the row shows a date tile.</summary>
    public bool HasDate => DayText is not null;

    /// <summary>Gets a value indicating whether the row shows an icon tile.</summary>
    public bool HasIcon => DayText is null;

    /// <summary>Gets what a screen reader says for the row: name, amount, date or rule and status (e.g. "3 days overdue").</summary>
    public string Description =>
        string.Join(", ", new[] { Title, AmountText, Subtitle, Badge }.Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>Returns <see cref="Description"/> (Windows names list rows after it).</summary>
    public override string ToString() => Description;
}

/// <summary>The plan centre (UI-07): due and overdue, upcoming and all plans. Works without notification permission (REM-02).</summary>
public sealed partial class PlansViewModel : ViewModelBase, Presentation.IThemeAware
{
    private const int UpcomingDays = 60;

    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private readonly ReminderService _reminders;
    private List<Schedule> _schedules = [];
    private List<OccurrenceState> _states = [];
    private Core.Commerce.PlanWorkPolicy _work = Core.Commerce.PlanWorkPolicy.Inactive;
    private Dictionary<Guid, Account> _accounts = [];
    private CategoryLookup? _categories;

    public PlansViewModel(ZananceStore store, PlanStore plans, Translator translator, ILocalizationService localization, IDateFormatter dates, TimeProvider time, ReminderService reminders)
    {
        _reminders = reminders;
        _store = store;
        _plans = plans;
        _translator = translator;
        _localization = localization;
        _dates = dates;
        _time = time;
        SegmentNames = [translator["Plans_Due"], translator["Plans_Upcoming"], translator["Plans_All"]];
    }

    public IReadOnlyList<string> SegmentNames { get; }

    /// <summary>Gets the rows of the chosen list; replaced as a whole, so the list lays out once per change.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<PlanRow> Rows { get; set; } = [];

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

    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        var snapshot = await _plans.GetWorkSnapshotAsync();
        _schedules = snapshot.Schedules;
        _states = snapshot.States;
        _work = snapshot.Work;
        _accounts = (await _store.GetAccountsAsync()).ToDictionary(a => a.Id);
        _categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
        UnreviewedCount = await _store.CountUnreviewedAsync();
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

        var text = new PlanText(_translator, _dates, _localization);
        var today = Today;
        var rows = new List<PlanRow>();

        switch (SegmentIndex)
        {
            case 0:
                foreach (var occurrence in PlanActions.InForce(_schedules)
                             .SelectMany(s => Occurrences.OpenUpTo(s, _states, today, s.ActiveFrom ?? s.Rule.Start))
                             .Where(o => _work.AllowsOccurrence(o, today))
                             .OrderBy(o => o.DueDate))
                {
                    rows.Add(OccurrenceRow(occurrence, text, today));
                }

                EmptyText = _translator[HasNoPlans ? "Plans_Empty" : "Plans_NothingDue"];
                break;

            case 1:
                foreach (var occurrence in PlanActions.InForce(_schedules)
                             .SelectMany(s => Occurrences.Between(s, _states, today.AddDays(1), today.AddDays(UpcomingDays), today))
                             .Where(o => o.IsOpen && _work.AllowsOccurrence(o, today))
                             .OrderBy(o => o.DueDate))
                {
                    rows.Add(OccurrenceRow(occurrence, text, today));
                }

                EmptyText = _translator[HasNoPlans ? "Plans_Empty" : "Plans_NothingUpcoming"];
                break;

            default:
                foreach (var schedule in _schedules.Where(s => !_schedules.Any(n => n.PreviousScheduleId == s.Id)))
                {
                    rows.Add(ScheduleRow(schedule, text, today));
                }

                EmptyText = _translator["Plans_Empty"];
                break;
        }

        Rows = rows;
        IsEmpty = rows.Count == 0;
    }

    private PlanRow OccurrenceRow(Occurrence occurrence, PlanText text, DateOnly today)
    {
        var schedule = occurrence.Schedule;
        var (icon, color) = Look(schedule);
        var overdue = occurrence.Status == OccurrenceView.Overdue;
        var (badge, look) = occurrence.Status switch
        {
            OccurrenceView.Overdue => (PlanText.OverdueDays(_translator, today.DayNumber - occurrence.DueDate.DayNumber), PlanLook.Danger),
            OccurrenceView.Due => (_translator["Occurrence_Due"], PlanLook.Future),
            _ => ((string?)null, PlanLook.Neutral),
        };

        // The tile shows the date, so the second line names the weekday and the account.
        var subtitle = text.Weekday(occurrence.DueDate) + " · " + (_accounts.TryGetValue(schedule.AccountId, out var account) ? account.Name : string.Empty);
        return new PlanRow(
            schedule.Id,
            occurrence.OriginalDate,
            schedule.Name,
            subtitle,
            text.Amount(occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount, occurrence.AmountMode, CurrencyOf(schedule.AccountId)),
            AmountColor(schedule.Kind),
            icon,
            color,
            color.WithAlpha(0.12f),
            badge,
            look.Text,
            look.Background) { BadgeStroke = look.Line }
            .WithDate(text, occurrence.DueDate, overdue, schedule.Kind);
    }

    private PlanRow ScheduleRow(Schedule schedule, PlanText text, DateOnly today)
    {
        var (icon, color) = Look(schedule);
        var next = schedule.State == ScheduleState.Active ? Occurrences.NextOpen(schedule, _states, today, today) : null;
        if (next is not null && !_work.AllowsOccurrence(next, today)) next = null;
        var subtitle = text.Rule(schedule.Rule);
        if (next is not null)
        {
            subtitle += Environment.NewLine + _translator.Format("Plan_NextDue", text.Date(next.DueDate));
        }

        var (badge, look) = schedule.State switch
        {
            ScheduleState.Paused => (_translator["Plan_Paused"], PlanLook.Warning),
            ScheduleState.Ended => (_translator["Plan_Ended"], PlanLook.Neutral),
            _ => ((string?)null, PlanLook.Neutral),
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
            look.Text,
            look.Background) { BadgeStroke = look.Line, TileStroke = color.WithAlpha(0.3f) };
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
        var hasAccounts = (await _store.GetAccountsAsync(includeArchived: false)).Count > 0;
        await Shell.Current.GoToAsync(hasAccounts ? AppShell.PlanEditorRoute : AppShell.AccountEditorRoute);
    }

    [RelayCommand]
    private async Task EnableRemindersAsync()
    {
        // The system asks while it still can; otherwise its notification settings open (D-38).
        RemindersOff = !await Presentation.PermissionPrompts.EnableNotificationsAsync(_reminders, _translator);
    }

    [RelayCommand]
    private Task ReviewEntriesAsync() => Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["unreviewed"] = true });
}
