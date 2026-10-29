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

    private readonly PlanStore _plans;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private readonly ReminderService _reminders;
    private PlanCenterReadSnapshot? _published;
    private PlanRow[] _publishedRows = [];
    private string? _originalFile;
    private bool _active = true;
    private long _revision;
    private CancellationTokenSource _lifetime = new();
    private TaskCompletionSource? _permissionIdle;

    /// <summary>Gets the original complete loading/error/Retry owner.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanInteract))]
    public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets translated original-source feedback without following a different file.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    /// <summary>Gets whether the original explicit native notification interaction is pending.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanInteract))]
    public partial bool IsPermissionBusy { get; private set; }
    /// <summary>Gets whether this current complete original publication admits native commands.</summary>
    public bool CanInteract => _active && Loading.IsReady && !IsPermissionBusy;

    public PlansViewModel(ZananceStore store, PlanStore plans, Translator translator, ILocalizationService localization, IDateFormatter dates, TimeProvider time, ReminderService reminders)
    {
        _reminders = reminders;
        ArgumentNullException.ThrowIfNull(store);
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

    /// <summary>Retires original source, rows and pending platform continuations on actual page/tab/root departure.</summary>
    public void SetActive(bool active)
    {
        if (_active != active) { _active = active; if (!active) RetireRead(); }
        _localization.Changed -= OnDisplayChanged;
        if (active) _localization.Changed += OnDisplayChanged;
    }
    private string DisplayIdentity() => $"{_localization.CurrentLanguage.CultureName}|{_localization.CurrentCulture.Name}|{_localization.CurrentCalendar}|{_localization.CurrentDigits}|{_localization.FormattingCultureName}";
    private void OnDisplayChanged(object? sender, EventArgs args) { RetireRead(); if (_active) _ = LoadAsync(); }
    async Task IThemeAware.RefreshThemeAsync() { RetireRead(); await LoadAsync(); }
    private void RetireRead()
    {
        _revision++; _published = null; _publishedRows = [];
        var retired = _lifetime; _lifetime = new(); Loading = new(); retired.Cancel(); retired.Dispose();
    }
    partial void OnSegmentIndexChanged(int value) { RetireRead(); if (_active) _ = LoadAsync(); }

    private sealed record PreparedRow(Schedule Schedule, Occurrence? Occurrence = null, Occurrence? Next = null);
    private sealed record PreparedCenter(PreparedRow[] Rows, string EmptyKey, bool HasNoPlans, bool WantsReminders);
    private sealed record RenderContext(PlanCenterReadSnapshot Source, Dictionary<Guid, Account> Accounts, CategoryLookup Categories);

    /// <summary>Reads/calculates privately and validates the complete original source after the native permission query.</summary>
    public async Task LoadAsync()
    {
        if (_permissionIdle is { } permission) await permission.Task;
        if (!_active) return;
        var owner = Loading; var revision = _revision; var segment = SegmentIndex; var day = Today;
        var display = DisplayIdentity(); var token = _lifetime.Token;
        bool Current() => _active && revision == _revision && segment == SegmentIndex && day == Today
            && display == DisplayIdentity() && ReferenceEquals(owner, Loading);
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original plan-center publication retired.", token); }
        PlanCenterReadSnapshot? source = null; PreparedCenter? prepared = null; PlanRow[] rows = []; var remindersOff = false;
        try
        {
            var pending = owner.RunAsync(async () =>
            {
                RequireCurrent(); _published = null;
                source = await _plans.ReadCenterSnapshotAsync(day, path =>
                { RequireCurrent(); _originalFile ??= path; return _originalFile == path; }, token);
                RequireCurrent(); prepared = await Task.Run(() => Prepare(source, segment, day), token); RequireCurrent();
                // Keep the actual platform query, but its late result cannot publish another page/profile's flags.
                remindersOff = _reminders.Scheduler.IsSupported && prepared.WantsReminders
                    && !await _reminders.Scheduler.AreEnabledAsync();
                RequireCurrent();
                var context = new RenderContext(source, source.Accounts.ToDictionary(a => a.Id), new(source.Categories, _translator));
                var text = new PlanText(_translator, _dates, _localization);
                rows = prepared.Rows.Select(row => row.Occurrence is { } occurrence
                    ? OccurrenceRow(occurrence, text, day, context) : ScheduleRow(row.Schedule, text, day, context, row.Next)).ToArray();
                await _plans.ValidateCenterSnapshotAsync(source, token); RequireCurrent(); source.EnsureCurrent();
            }, () =>
            {
                var result = prepared ?? throw new InvalidOperationException("The original plan center was not prepared.");
                void Publish(Action action) { RequireCurrent(); source!.EnsureCurrent(); action(); RequireCurrent(); source.EnsureCurrent(); }
                Publish(() => Rows = rows);
                if (!ReferenceEquals(Rows, rows)) throw new InvalidOperationException("The original published plan rows changed during presentation.");
                Publish(() => UnreviewedCount = source!.UnreviewedCount); Publish(() => HasNoPlans = result.HasNoPlans);
                Publish(() => RemindersOff = remindersOff); Publish(() => EmptyText = _translator[result.EmptyKey]);
                Publish(() => IsEmpty = rows.Length == 0); Publish(() => Feedback = null);
                _publishedRows = rows; _published = source;
            });
            OnPropertyChanged(nameof(CanInteract)); await pending;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) Feedback = CommercialFailureMessage.OrFallback(exception, _translator, "Plans_ReadFailed"); }
        finally { OnPropertyChanged(nameof(CanInteract)); }
    }
    private static PreparedCenter Prepare(PlanCenterReadSnapshot source, int segment, DateOnly today)
    {
        var schedules = source.Work.Schedules; var states = source.Work.States; var work = source.Work.Work;
        IEnumerable<PreparedRow> rows; var none = schedules.Count == 0;
        if (segment == 0)
            rows = PlanActions.InForce(schedules).SelectMany(s => Occurrences.OpenUpTo(s, states, today, s.ActiveFrom ?? s.Rule.Start))
                .Where(o => work.AllowsOccurrence(o, today)).OrderBy(o => o.DueDate).Select(o => new PreparedRow(o.Schedule, o));
        else if (segment == 1)
            rows = PlanActions.InForce(schedules).SelectMany(s => Occurrences.Between(s, states, today.AddDays(1), today.AddDays(UpcomingDays), today))
                .Where(o => o.IsOpen && work.AllowsOccurrence(o, today)).OrderBy(o => o.DueDate).Select(o => new PreparedRow(o.Schedule, o));
        else
            rows = schedules.Where(s => !schedules.Any(n => n.PreviousScheduleId == s.Id)).Select(schedule =>
            {
                var next = schedule.State == ScheduleState.Active ? Occurrences.NextOpen(schedule, states, today, today) : null;
                if (next is not null && !work.AllowsOccurrence(next, today)) next = null;
                return new PreparedRow(schedule, Next: next);
            });
        return new(rows.ToArray(), segment == 0 ? none ? "Plans_Empty" : "Plans_NothingDue"
            : segment == 1 ? none ? "Plans_Empty" : "Plans_NothingUpcoming" : "Plans_Empty", none,
            schedules.Any(s => s.ReminderEnabled && s.State == ScheduleState.Active));
    }

    private PlanRow OccurrenceRow(Occurrence occurrence, PlanText text, DateOnly today, RenderContext context)
    {
        var schedule = occurrence.Schedule;
        var (icon, color) = Look(schedule, context);
        var overdue = occurrence.Status == OccurrenceView.Overdue;
        var (badge, look) = occurrence.Status switch
        {
            OccurrenceView.Overdue => (PlanText.OverdueDays(_translator, today.DayNumber - occurrence.DueDate.DayNumber), PlanLook.Danger),
            OccurrenceView.Due => (_translator["Occurrence_Due"], PlanLook.Future),
            _ => ((string?)null, PlanLook.Neutral),
        };

        // The tile shows the date, so the second line names the weekday and the account.
        var subtitle = text.Weekday(occurrence.DueDate) + " · " + (context.Accounts.TryGetValue(schedule.AccountId, out var account) ? account.Name : string.Empty);
        return new PlanRow(
            schedule.Id,
            occurrence.OriginalDate,
            schedule.Name,
            subtitle,
            text.Amount(occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount, occurrence.AmountMode, CurrencyOf(schedule.AccountId, context)),
            AmountColor(schedule.Kind),
            icon,
            color,
            color.WithAlpha(0.12f),
            badge,
            look.Text,
            look.Background) { BadgeStroke = look.Line }
            .WithDate(text, occurrence.DueDate, overdue, schedule.Kind);
    }

    private PlanRow ScheduleRow(Schedule schedule, PlanText text, DateOnly today, RenderContext context, Occurrence? next)
    {
        var (icon, color) = Look(schedule, context);
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
            text.Amount(schedule.Amount, schedule.AmountMode, CurrencyOf(schedule.AccountId, context)),
            AmountColor(schedule.Kind),
            icon,
            color,
            color.WithAlpha(0.12f),
            badge,
            look.Text,
            look.Background) { BadgeStroke = look.Line, TileStroke = color.WithAlpha(0.3f) };
    }

    private static (Symbol Icon, Color Color) Look(Schedule schedule, RenderContext context) => schedule.Kind == EntryKind.Transfer
        ? (Symbol.ArrowSwap, EntryPresenter.NeutralColor)
        : (Icons.Parse(schedule.Icon, context.Categories.Icon(schedule.CategoryId)), context.Categories.Color(schedule.CategoryId));

    private static Color AmountColor(EntryKind kind) => kind switch
    {
        EntryKind.Income => EntryPresenter.IncomeColor,
        EntryKind.Expense => EntryPresenter.ExpenseColor,
        _ => EntryPresenter.NeutralColor,
    };

    private static string CurrencyOf(Guid accountId, RenderContext context) => context.Accounts.TryGetValue(accountId, out var account) ? account.CurrencyCode : "EUR";

    private bool Owns(PlanCenterReadSnapshot? source, SnapshotLoadState owner, long revision, int segment, DateOnly day,
        string display, bool requireIdle = true) => _active && source is not null && ReferenceEquals(source, _published)
        && ReferenceEquals(owner, Loading) && Loading.IsReady && revision == _revision && segment == SegmentIndex
        && day == Today && display == DisplayIdentity() && (!requireIdle || !IsPermissionBusy);

    private async Task FailCurrentAsync(Exception exception, SnapshotLoadState owner)
    {
        if (!_active || !ReferenceEquals(owner, Loading)) return;
        _published = null; _publishedRows = []; Feedback = CommercialFailureMessage.OrFallback(exception, _translator, "Plans_ReadFailed");
        try { await owner.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception failure) when (failure is not OutOfMemoryException) { }
        finally { OnPropertyChanged(nameof(CanInteract)); }
    }

    [RelayCommand]
    private async Task OpenAsync(PlanRow row)
    {
        var source = _published; var owner = Loading; var revision = _revision; var segment = SegmentIndex;
        var day = Today; var display = DisplayIdentity(); var token = _lifetime.Token;
        bool Current() => Owns(source, owner, revision, segment, day, display)
            && ReferenceEquals(Rows, _publishedRows) && _publishedRows.Any(item => ReferenceEquals(item, row));
        if (!Current()) return;
        try
        {
            await _plans.ValidateCenterSnapshotAsync(source!, token); if (!Current()) return; source!.EnsureCurrent();
            if (row.OriginalDate is { } date)
                await Shell.Current.GoToAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = row.ScheduleId, ["date"] = date });
            else await Shell.Current.GoToAsync(AppShell.PlanDetailRoute, new Dictionary<string, object> { ["id"] = row.ScheduleId });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (Current()) await FailCurrentAsync(exception, owner); }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var source = _published; var owner = Loading; var revision = _revision; var segment = SegmentIndex; var day = Today; var display = DisplayIdentity();
        bool Current() => Owns(source, owner, revision, segment, day, display);
        if (!Current()) return;
        try
        {
            await _plans.ValidateCenterSnapshotAsync(source!, _lifetime.Token); if (!Current()) return; source!.EnsureCurrent();
            await Shell.Current.GoToAsync(source.Accounts.Any(a => !a.IsArchived) ? AppShell.PlanEditorRoute : AppShell.AccountEditorRoute);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (Current()) await FailCurrentAsync(exception, owner); }
    }

    [RelayCommand]
    private async Task EnableRemindersAsync()
    {
        var source = _published; var owner = Loading; var revision = _revision; var segment = SegmentIndex; var day = Today; var display = DisplayIdentity();
        bool Current() => Owns(source, owner, revision, segment, day, display, requireIdle: false);
        if (!Current() || IsPermissionBusy || !RemindersOff) return;
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); _permissionIdle = idle;
        try
        {
            IsPermissionBusy = true; await _plans.ValidateCenterSnapshotAsync(source!, _lifetime.Token); if (!Current()) return;
            // Keep the original explicit native permission/settings flow. Only its original current page may use its result.
            var enabled = await PermissionPrompts.EnableNotificationsAsync(_reminders, _translator); if (!Current()) return;
            await _plans.ValidateCenterSnapshotAsync(source!, _lifetime.Token); if (!Current()) return; source!.EnsureCurrent();
            RemindersOff = !enabled;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (Current()) await FailCurrentAsync(exception, owner); }
        finally
        {
            try { IsPermissionBusy = false; }
            finally { if (ReferenceEquals(_permissionIdle, idle)) _permissionIdle = null; idle.TrySetResult(); }
        }
    }

    [RelayCommand]
    private async Task ReviewEntriesAsync()
    {
        var source = _published; var owner = Loading; var revision = _revision; var segment = SegmentIndex; var day = Today; var display = DisplayIdentity();
        bool Current() => Owns(source, owner, revision, segment, day, display);
        if (!Current()) return;
        try
        {
            await _plans.ValidateCenterSnapshotAsync(source!, _lifetime.Token); if (!Current()) return; source!.EnsureCurrent();
            await Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["unreviewed"] = true });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (Current()) await FailCurrentAsync(exception, owner); }
    }
}
