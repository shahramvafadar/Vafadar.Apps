using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Backup;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>One step of the review: what to look at, its current state, done or not, and the screen that does it.</summary>
public sealed partial class ReviewStepRow(ReviewStep step, string title, string detail, Func<Task> open) : ObservableObject
{
    public ReviewStep Step { get; } = step;

    public string Title { get; } = title;

    public string Detail { get; } = detail;

    public Func<Task> Open { get; } = open;

    [ObservableProperty]
    public partial bool IsDone { get; set; }
}

/// <summary>
/// The period-end review (ZEX-S0610, ZEX-UI16): a short, finishable routine for the financial month that just ended –
/// review entries, settle plans, compare balances, look at goals, make a backup. Each step opens its existing screen;
/// the progress is kept in the profile's settings, and finishing hides the Home item until the next month.
/// </summary>
public sealed partial class PeriodReviewViewModel(
    ZananceStore store, IBackupService backup, Translator translator, IDateFormatter dates,
    ILocalizationService localization, TimeProvider time, Interaction.IAppInteraction interaction)
    : ViewModelBase, Presentation.IThemeAware
{
    private PeriodReviewState? _state;
    private PeriodReviewReadSnapshot? _published;
    private string? _originalFile;
    private DateOnly _publishedDay;
    private string? _publishedDisplay;
    private volatile bool _active;
    private int _revision;
    private int _accepted;
    private bool _saving;
    private bool _publishing;
    private CancellationTokenSource _lifetime = new();

    /// <summary>Gets all original checklist rows, published together under the original page cover.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<ReviewStepRow> Steps { get; private set; } = [];
    [ObservableProperty] public partial string? Title { get; set; }
    [ObservableProperty] public partial bool NothingToReview { get; set; }
    [ObservableProperty] public partial string? ProgressText { get; set; }
    /// <summary>Gets one local read/write failure, preserving known committed progress.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    /// <summary>Gets the current whole-page loading/error cover and explicit Retry state.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanInteract)), NotifyPropertyChangedFor(nameof(CanReload))]
    public partial Presentation.SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets whether original current rows may navigate or change checklist progress.</summary>
    public bool CanInteract => _active && !_saving && Loading.IsReady && _published is not null
        && _publishedDay == Today && _publishedDisplay == DisplayIdentity();
    /// <summary>Gets whether the original file may be explicitly reread outside a pending command.</summary>
    public bool CanReload => _active && !_saving && !Loading.IsLoading;
    /// <summary>Gets known acceptance independently of later read/notification failure.</summary>
    public bool Accepted => Volatile.Read(ref _accepted) != 0;
    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
    private PeriodCalendar Calendar => Presentation.Calendars.ToPeriod(localization.CurrentCalendar);
    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";

    /// <summary>Retires actual row/read/write owners across departure, retained tabs and root replacement.</summary>
    public void SetActive(bool active)
    {
        localization.Changed -= OnDisplayChanged;
        if (active) localization.Changed += OnDisplayChanged;
        if (_active != active) { _active = active; if (!active) RetireRead(); }
        NotifyAvailability();
    }
    private void RetireRead()
    {
        Interlocked.Increment(ref _revision); _published = null;
        var retired = _lifetime; _lifetime = new(); Loading = new(); retired.Cancel(); retired.Dispose();
    }
    private void OnDisplayChanged(object? sender, EventArgs args) { RetireRead(); if (_active) _ = LoadAsync(); }
    async Task Presentation.IThemeAware.RefreshThemeAsync() { RetireRead(); if (_active) await LoadAsync(); }
    private void NotifyAvailability() { OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload)); OnPropertyChanged(nameof(Accepted)); }

    /// <summary>Stages complete original calculations privately and publishes only a freshly validated one-file source.</summary>
    public async Task LoadAsync()
    {
        if (!_active) return;
        var owner = Loading; var revision = Volatile.Read(ref _revision); var display = DisplayIdentity();
        var day = Today; var calendar = Calendar; var token = _lifetime.Token;
        bool Current() => _active && revision == Volatile.Read(ref _revision) && display == DisplayIdentity()
            && Today == day && ReferenceEquals(owner, Loading);
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original period-review publication retired.", token); }
        PeriodReviewReadSnapshot? source = null; Prepared? prepared = null;
        try
        {
            var pending = owner.RunAsync(async () =>
            {
                _published = null; RequireCurrent();
                source = await store.ReadPeriodReviewAsync(path => { RequireCurrent(); _originalFile ??= path; return _originalFile == path; }, token);
                RequireCurrent();
                prepared = await Task.Run(() => Prepare(source, day, calendar), token); RequireCurrent();
                await store.ValidatePeriodReviewAsync(source, token); RequireCurrent();
            }, () =>
            {
                RequireCurrent(); var result = prepared!;
                var state = result.State;
                var rows = state is null ? [] : CreateRows(source!, state, result, revision, day, display);
                _publishing = true;
                try
                {
                    Title = state is null ? translator["Review_Title"] : translator.Format("Review_TitleMonth", dates.Format(result.From, DateFormatStyle.MonthYear)); RequireCurrent();
                    NothingToReview = state is null; RequireCurrent();
                    Steps = rows; RequireCurrent(); _state = state;
                    ProgressText = state is null ? null : translator.Format("Review_Progress", rows.Count(row => row.IsDone), rows.Length); RequireCurrent();
                    Feedback = null; RequireCurrent(); _publishedDay = day; _publishedDisplay = display; _published = source;
                }
                finally { _publishing = false; }
            });
            NotifyAvailability(); await pending;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) Feedback = Accepted ? translator["Occurrence_SavedRefreshFailed"] : Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Error_Unexpected"); }
        finally { NotifyAvailability(); }
    }

    private sealed record Prepared(PeriodReviewState? State, DateOnly From, int Unreviewed, int OpenPlans, int NotReconciled, int ActiveGoals);
    private static Prepared Prepare(PeriodReviewReadSnapshot source, DateOnly day, PeriodCalendar calendar)
    {
        var start = source.Settings?.MonthStartDay ?? 1;
        var first = source.Accounts.Count == 0 ? (DateOnly?)null : source.Accounts.Min(account => account.OpeningDate);
        var state = PeriodReview.Due(source.Settings?.ReviewProgress, day, calendar, start, first);
        if (state is null) return new(null, default, 0, 0, 0, 0);
        var (from, to) = PeriodMath.MonthRange(state.Year, state.Month, calendar, start);
        return new(state, from, source.Entries.Count(entry => entry.Review == ReviewState.Unreviewed && entry.Date <= to),
            source.Schedules.Sum(schedule => Occurrences.Between(schedule, source.States, schedule.ActiveFrom ?? schedule.Rule.Start, to, day).Count(occurrence => occurrence.IsOpen)),
            source.Accounts.Count(account => !account.IsArchived && (account.LastReconciledOn ?? account.OpeningDate) < to.AddDays(-DataStatus.ReconcileDays)),
            source.Goals.Count(goal => goal.State == Core.Goals.GoalState.Active));
    }
    private ReviewStepRow[] CreateRows(PeriodReviewReadSnapshot source, PeriodReviewState state, Prepared prepared, int revision, DateOnly day, string display)
    {
        var lastBackup = backup.LastBackupAt is { } at ? dates.Format(DateOnly.FromDateTime(at.ToLocalTime().DateTime), DateFormatStyle.Short) : translator["Issue_NoBackup"];
        var rows = new List<ReviewStepRow>();
        void Add(ReviewStep step, string detail, string route, IDictionary<string, object>? query = null)
        {
            var row = new ReviewStepRow(step, translator[$"Review_{step}"], detail,
                () => NavigateOriginalAsync(source, revision, day, display, route, query)) { IsDone = state.Done.Contains(step) };
            row.PropertyChanged += async (_, args) =>
            { if (args.PropertyName == nameof(ReviewStepRow.IsDone) && !_publishing) await SaveStepAsync(row); };
            rows.Add(row);
        }
        Add(ReviewStep.Unreviewed, translator.Format("Review_UnreviewedDetail", prepared.Unreviewed), "//transactions", new Dictionary<string, object> { ["unreviewed"] = true });
        Add(ReviewStep.Plans, translator.Format("Review_PlansDetail", prepared.OpenPlans), "//plans");
        Add(ReviewStep.Reconcile, translator.Format("Review_ReconcileDetail", prepared.NotReconciled), AppRoutes.AccountsRoute);
        Add(ReviewStep.Goals, translator.Format("Review_GoalsDetail", prepared.ActiveGoals), AppRoutes.GoalsRoute);
        Add(ReviewStep.Backup, translator.Format("Review_BackupDetail", lastBackup), AppRoutes.BackupRoute);
        return [.. rows];
    }
    private bool Owns(PeriodReviewReadSnapshot source, int revision, DateOnly day, string display) =>
        _active && revision == Volatile.Read(ref _revision) && day == Today && display == DisplayIdentity() && ReferenceEquals(source, _published);
    private async Task NavigateOriginalAsync(PeriodReviewReadSnapshot source, int revision, DateOnly day, string display, string route, IDictionary<string, object>? query)
    {
        if (!await EnsureOriginalDayAsync()) return;
        if (!CanInteract || !Owns(source, revision, day, display)) return;
        try { await store.ValidatePeriodReviewAsync(source); if (CanInteract && Owns(source, revision, day, display)) await interaction.NavigateAsync(route, query); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Owns(source, revision, day, display)) await CoverFailureAsync(exception); }
    }
    private async Task SaveStepAsync(ReviewStepRow row)
    {
        if (await EnsureOriginalDayAsync() && CanInteract && Steps.Any(original => ReferenceEquals(original, row)))
            await SaveProgressAsync(row, row.IsDone);
    }

    private async Task<bool> EnsureOriginalDayAsync()
    {
        if (_active && _published is not null && _publishedDay != Today)
        {
            // D-636: a new command cannot silently adopt today's date for yesterday's complete publication.
            await CoverFailureAsync(new InvalidOperationException("Reload the original period review after its local day changes."));
            return false;
        }
        return true;
    }

    private async Task SaveProgressAsync(ReviewStepRow? row, bool done)
    {
        if (!await EnsureOriginalDayAsync()) return;
        if (!CanInteract || _state is not { } state || _published is not { } source) return;
        var revision = Volatile.Read(ref _revision); var day = Today; var display = DisplayIdentity(); var calendar = Calendar;
        bool Current() => Owns(source, revision, day, display);
        _saving = true; Volatile.Write(ref _accepted, 0); NotifyAvailability();
        try
        {
            await store.SavePeriodReviewProgressAsync(source, calendar, day, state.Key, row?.Step, done, Current,
                accepted: () => Volatile.Write(ref _accepted, 1));
            if (Current())
            {
                await LoadAsync();
                if (row is null && _active && revision == Volatile.Read(ref _revision) && display == DisplayIdentity() && day == Today && Loading.IsReady)
                    await interaction.NavigateAsync("..");
            }
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) await CoverFailureAsync(exception); }
        finally { _saving = false; NotifyAvailability(); }
    }
    private async Task CoverFailureAsync(Exception exception)
    {
        _published = null; var failed = Loading = new();
        Feedback = Accepted ? translator["Occurrence_SavedRefreshFailed"] : Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Error_Unexpected");
        try { await failed.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception original) when (ReferenceEquals(original, exception)) { }
        NotifyAvailability();
    }

    [RelayCommand] private Task ReloadAsync() => CanReload ? LoadAsync() : Task.CompletedTask;
    [RelayCommand] private Task OpenStepAsync(ReviewStepRow row) => CanInteract && Steps.Any(original => ReferenceEquals(original, row)) ? row.Open() : Task.CompletedTask;
    // Finishing preserves the original settings-only policy; it never settles a plan or posts money.
    [RelayCommand] private Task FinishAsync() => SaveProgressAsync(null, false);
}
