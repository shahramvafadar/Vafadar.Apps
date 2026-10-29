using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>An occurrence in the plan details.</summary>
public sealed record OccurrenceRow(DateOnly OriginalDate, string DateText, string AmountText, string StatusText, Color StatusColor);

/// <summary>A plan with its next and past occurrences; pause, resume, end and delete (REC-16).</summary>
public sealed partial class PlanDetailViewModel(
    PlanStore plans,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    TimeProvider time,
    PlanDetailActions actions,
    IAppInteraction interaction) : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private Guid _id;
    private PlanDetailSnapshot? _review;
    private bool _active = true;
    private long _revision;
    private string? _originalFile;
    private CancellationTokenSource _lifetime = new();
    /// <summary>Gets original consent/write/accepted-result ownership independently of the covered reader.</summary>
    public PlanDetailActions Actions { get; } = actions;
    /// <summary>Gets the current complete source loader and local explicit Retry state.</summary>
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets original translated source/refusal feedback without changing the resume-date draft.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }

    public SnapshotCollection<DetailLine> Lines { get; } = [];

    public SnapshotCollection<OccurrenceRow> Upcoming { get; } = [];

    public SnapshotCollection<OccurrenceRow> History { get; } = [];

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? AmountText { get; set; }

    [ObservableProperty]
    public partial string? RuleText { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial bool IsPaused { get; set; }

    [ObservableProperty]
    public partial bool IsEnded { get; set; }

    [ObservableProperty]
    public partial bool CanDelete { get; set; }

    [ObservableProperty]
    public partial bool HasHistory { get; set; }

    [ObservableProperty]
    public partial DateOnly ResumeFrom { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    /// <summary>Retires the previous query without following a different actual profile file.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue("id", out var value)) return;
        if (value is not Guid id || id == Guid.Empty) throw new ArgumentException("An original plan identity is required.", nameof(query));
        if (_id == id) return;
        _id = id; RetireRead(); ResumeFrom = Today;
        Name = AmountText = RuleText = Feedback = null; NotFound = true;
        Lines.ReplaceAll([]); Upcoming.ReplaceAll([]); History.ReplaceAll([]);
    }
    /// <summary>Retires actual page/root/tab ownership, including an original pending native confirmation.</summary>
    public void SetActive(bool active)
    {
        if (_active != active) { _active = active; if (!active) RetireRead(); }
        Actions.SetOwner(_id, active);
        localization.Changed -= OnDisplayChanged;
        if (active) localization.Changed += OnDisplayChanged;
    }
    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";
    private void OnDisplayChanged(object? sender, EventArgs args) { RetireRead(); if (_active) _ = LoadAsync(); }
    async Task IThemeAware.RefreshThemeAsync() { RetireRead(); await LoadAsync(); }
    private void RetireRead()
    {
        _revision++; _review = null; var retired = _lifetime; _lifetime = new(); Loading = new();
        Actions.SetOwner(_id, false); if (_active) Actions.SetOwner(_id, true);
        retired.Cancel(); retired.Dispose();
    }

    /// <summary>Stages original detail/history privately and validates one actual-file snapshot before covered publication.</summary>
    public async Task LoadAsync()
    {
        await Actions.WaitForIdleAsync(); if (!_active) return;
        var owner = Loading; var revision = _revision; var id = _id; var day = Today; var display = DisplayIdentity(); var token = _lifetime.Token;
        PlanDetailSnapshot? source = null; Projection? prepared = null;
        bool Current() => _active && revision == _revision && id == _id && ReferenceEquals(owner, Loading) && day == Today && display == DisplayIdentity();
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original plan detail retired.", token); }
        try
        {
            await owner.RunAsync(async () =>
            {
                RequireCurrent(); _review = null;
                if (id == Guid.Empty) { prepared = Projection.Missing; return; }
                source = await plans.ReadDetailSnapshotAsync(id, day, path =>
                { RequireCurrent(); _originalFile ??= path; return _originalFile == path; }, token); RequireCurrent();
                var colors = (EntryPresenter.DangerColor, EntryPresenter.IncomeColor, EntryPresenter.NeutralColor);
                prepared = await Task.Run(() => Prepare(source, colors), token);
                await plans.ValidateDetailSnapshotAsync(source, token); RequireCurrent(); source.EnsureCurrent();
            }, () =>
            {
                var result = prepared ?? throw new InvalidOperationException("The original complete plan detail was not prepared.");
                void Publish(Action action) { RequireCurrent(); source?.EnsureCurrent(); action(); RequireCurrent(); source?.EnsureCurrent(); }
                Publish(() => Name = result.Name); Publish(() => AmountText = result.Amount); Publish(() => RuleText = result.Rule);
                Publish(() => IsActive = result.Active); Publish(() => IsPaused = result.Paused); Publish(() => IsEnded = result.Ended);
                Publish(() => CanSettle = result.CanSettle); Publish(() => CanDelete = result.CanDelete); Publish(() => HasHistory = result.History.Count != 0);
                Publish(() => Lines.ReplaceAll(result.Lines)); Publish(() => Upcoming.ReplaceAll(result.Upcoming)); Publish(() => History.ReplaceAll(result.History));
                Publish(() => NotFound = result.NotFound); Publish(() => Feedback = null);
                _review = source; Actions.ClearReadFailure();
            });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) Feedback = CommercialFailureMessage.OrFallback(exception, translator, "Plan_DetailReadFailed"); }
    }

    private sealed record Projection(string? Name, string? Amount, string? Rule, bool Active, bool Paused, bool Ended,
        bool CanSettle, bool CanDelete, bool NotFound, IReadOnlyList<DetailLine> Lines, IReadOnlyList<OccurrenceRow> Upcoming, IReadOnlyList<OccurrenceRow> History)
    {
        internal static Projection Missing { get; } = new(null, null, null, false, false, false, false, false, true, [], [], []);
    }

    private Projection Prepare(PlanDetailSnapshot snapshot, (Color Danger, Color Income, Color Neutral) colors)
    {
        var schedule = snapshot.Source.Inputs.Schedule;
        if (schedule is null) return Projection.Missing;
        if (!snapshot.Source.IsAvailable) throw new InvalidOperationException("The original plan's monetary references are unavailable.");
        var accounts = snapshot.Source.Inputs.Accounts.ToDictionary(a => a.Id);
        var categories = new CategoryLookup(snapshot.Source.Inputs.Categories, translator);
        var states = snapshot.Work.States;
        var text = new PlanText(translator, dates, localization);
        var account = accounts[schedule.AccountId]; var currency = account.CurrencyCode;
        var today = snapshot.Source.Request.Today;

        var name = schedule.Name;
        var amount = text.Amount(schedule.Amount, schedule.AmountMode, currency);
        var rule = text.Rule(schedule.Rule);
        var canSettle = schedule.Kind == Core.Ledger.EntryKind.Expense && schedule.Rule.Frequency != Frequency.Once;
        var active = schedule.State == ScheduleState.Active;
        var paused = schedule.State == ScheduleState.Paused;
        var ended = schedule.State == ScheduleState.Ended;

        var lines = new List<DetailLine>();
        lines.Add(new DetailLine(translator["Entry_Kind"], translator["EntryKind_" + schedule.Kind]));
        lines.Add(new DetailLine(translator[schedule.Kind == Core.Ledger.EntryKind.Transfer ? "Entry_FromAccount" : "Entry_Account"], account?.Name ?? "?"));
        if (schedule.ToAccountId is { } to && accounts.TryGetValue(to, out var destination))
        {
            lines.Add(new DetailLine(translator["Entry_ToAccount"], destination.Name));
        }

        if (schedule.CategoryId is not null)
        {
            lines.Add(new DetailLine(translator["Entry_Category"], categories.Name(schedule.CategoryId)));
        }

        lines.Add(new DetailLine(translator["Plan_AutoPost"], translator[schedule.AutoPost ? "Common_Yes" : "Common_No"]));
        if (schedule.ReminderEnabled)
        {
            lines.Add(new DetailLine(translator["Plan_Reminder"], PlanText.ReminderText(translator, schedule.ReminderDaysBefore, schedule.ReminderTime.ToString("t", localization.CurrentCulture))
                + (schedule.ReminderOnDueDate && schedule.ReminderDaysBefore > 0 ? " · " + translator["Plan_ReminderAlsoDue"] : string.Empty)));
        }

        // Contract details (F2-CON-01); the deadline is to cancel, not to pay.
        if (schedule.ContractProvider is { } provider)
        {
            lines.Add(new DetailLine(translator["Contract_Provider"], provider));
        }

        if (schedule.ContractReference is { } reference)
        {
            lines.Add(new DetailLine(translator["Contract_Reference"], reference));
        }

        if (schedule.ContractEnd is { } contractEnd)
        {
            lines.Add(new DetailLine(translator["Contract_End"], dates.Format(contractEnd, DateFormatStyle.Long) + (schedule.ContractRenews ? " · " + translator["Contract_RenewsShort"] : string.Empty)));
        }

        if (schedule.CancellationDeadline is { } deadline)
        {
            lines.Add(new DetailLine(translator["Contract_Deadline"], dates.Format(deadline, DateFormatStyle.Long)));
        }

        if (schedule.ReviewDate is { } review)
        {
            lines.Add(new DetailLine(translator["Contract_Review"], dates.Format(review, DateFormatStyle.Long)));
        }

        if (!string.IsNullOrEmpty(schedule.Note))
        {
            lines.Add(new DetailLine(translator["Entry_Note"], schedule.Note));
        }

        var upcoming = new List<OccurrenceRow>();
        if (schedule.State == ScheduleState.Active)
        {
            foreach (var occurrence in Occurrences.Between(schedule, states, today.AddDays(-400), today.AddYears(2), today)
                         .Where(o => o.IsOpen && snapshot.Work.Work.AllowsOccurrence(o, today)).Take(6))
            {
                upcoming.Add(Row(occurrence, text, currency, colors));
            }
        }

        var history = new List<OccurrenceRow>();
        foreach (var occurrence in Occurrences.Between(schedule, states, DateOnly.MinValue, today.AddYears(2), today).Where(o => !o.IsOpen).Reverse().Take(12))
        {
            history.Add(Row(occurrence, text, currency, colors));
        }

        return new(name, amount, rule, active, paused, ended, canSettle, snapshot.CanDelete, false, lines, upcoming, history);
    }

    private static OccurrenceRow Row(Occurrence occurrence, PlanText text, string currency, (Color Danger, Color Income, Color Neutral) colors)
    {
        var color = occurrence.Status switch
        {
            OccurrenceView.Overdue => colors.Danger,
            OccurrenceView.Settled => colors.Income,
            _ => colors.Neutral,
        };
        return new OccurrenceRow(occurrence.OriginalDate, text.Date(occurrence.DueDate), text.Amount(occurrence.IsOpen && occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount, occurrence.AmountMode, currency), text.Status(occurrence.Status), color);
    }

    private bool Current(PlanDetailSnapshot source, long revision, Guid id, SnapshotLoadState owner, string display) =>
        _active && revision == _revision && id == _id && ReferenceEquals(owner, Loading) && owner.IsReady
        && ReferenceEquals(source, _review) && source.Source.Request.Today == Today && display == DisplayIdentity();

    private async Task FailReadAsync(SnapshotLoadState owner, Exception exception)
    {
        _review = null; Feedback = CommercialFailureMessage.OrFallback(exception, translator, "Plan_DetailReadFailed");
        try { await owner.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception failure) when (failure is not OutOfMemoryException) { }
    }

    private async Task NavigateOriginalAsync(string route, OccurrenceRow? row = null)
    {
        var source = _review; if (source is null || source.Source.Inputs.Schedule is null) return;
        var original = source.Source.Inputs.Schedule;
        if (route == AppRoutes.PlanEditorRoute && original.State == ScheduleState.Ended
            || route == AppRoutes.SettlementRoute && (original.Kind != Core.Ledger.EntryKind.Expense || original.Rule.Frequency == Frequency.Once)) return;
        var revision = _revision; var id = _id; var owner = Loading; var display = DisplayIdentity(); var token = _lifetime.Token;
        bool IsCurrent() => Current(source, revision, id, owner, display)
            && (row is null || Upcoming.Concat(History).Any(item => ReferenceEquals(item, row)));
        if (!IsCurrent() || Actions.IsBusy) return;
        try
        {
            await plans.ValidateDetailSnapshotAsync(source, token); if (!IsCurrent()) return; source.EnsureCurrent();
            var query = row is null ? new Dictionary<string, object> { ["id"] = id }
                : new Dictionary<string, object> { ["plan"] = id, ["date"] = row.OriginalDate };
            await interaction.NavigateAsync(route, query);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (IsCurrent()) await FailReadAsync(owner, exception); }
    }

    [RelayCommand] private Task OpenOccurrenceAsync(OccurrenceRow row) => NavigateOriginalAsync(AppRoutes.OccurrenceRoute, row);
    [RelayCommand] private Task EditAsync() => NavigateOriginalAsync(AppRoutes.PlanEditorRoute);
    /// <summary>Gets the original recurring-expense final-settlement entry point independently of history ownership.</summary>
    [ObservableProperty] public partial bool CanSettle { get; set; }
    [RelayCommand] private Task SettleAsync() => NavigateOriginalAsync(AppRoutes.SettlementRoute);
    [RelayCommand] private Task PauseAsync() => ChangeAsync(PlanDetailOperation.Pause);
    [RelayCommand] private Task ResumeAsync() => ChangeAsync(PlanDetailOperation.Resume);
    [RelayCommand] private Task EndAsync() => ChangeAsync(PlanDetailOperation.End);
    [RelayCommand] private Task DeleteAsync() => ChangeAsync(PlanDetailOperation.Delete);

    private async Task ChangeAsync(PlanDetailOperation operation)
    {
        var source = _review; if (source is null || source.Source.Inputs.Schedule is null) return;
        var revision = _revision; var id = _id; var owner = Loading; var display = DisplayIdentity(); var resume = ResumeFrom;
        bool IsCurrent() => Current(source, revision, id, owner, display) && (operation != PlanDetailOperation.Resume || resume == ResumeFrom);
        if (!IsCurrent()) return;
        var changed = await Actions.RunAsync(source, operation, resume, IsCurrent);
        if (!IsCurrent()) return;
        if (changed?.Accepted != true)
        { if (Actions.Failure is { } failure) await FailReadAsync(owner, failure); return; }
        try
        {
            if (operation == PlanDetailOperation.Delete) { await interaction.NavigateAsync(".."); return; }
            if (operation == PlanDetailOperation.Resume)
            { _id = changed.DestinationId; ResumeFrom = Today; RetireRead(); }
            await LoadAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Actions.ReportFollowUpFailure(changed); }
    }
}
