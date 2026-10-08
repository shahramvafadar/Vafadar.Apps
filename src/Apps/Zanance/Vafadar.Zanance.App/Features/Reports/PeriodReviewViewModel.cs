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
    ZananceStore store,
    PlanStore plans,
    GoalStore goals,
    IBackupService backup,
    Translator translator,
    IDateFormatter dates,
    ILocalizationService localization,
    TimeProvider time) : ViewModelBase
{
    private PeriodReviewState? _state;
    private bool _loading;

    public ObservableCollection<ReviewStepRow> Steps { get; } = [];

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial bool NothingToReview { get; set; }

    [ObservableProperty]
    public partial string? ProgressText { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    private PeriodCalendar Calendar => Presentation.Calendars.ToPeriod(localization.CurrentCalendar);

    public async Task LoadAsync()
    {
        _loading = true;
        try
        {
            var settings = await store.GetSettingsAsync();
            var accounts = await store.GetAccountsAsync();
            var entries = await store.GetEntriesAsync();
            var firstData = accounts.Count == 0 ? (DateOnly?)null : accounts.Min(a => a.OpeningDate);
            _state = PeriodReview.Due(settings.ReviewProgress, Today, Calendar, settings.MonthStartDay, firstData);
            NothingToReview = _state is null;
            Steps.Clear();
            if (_state is not { } state)
            {
                Title = translator["Review_Title"];
                return;
            }

            var (from, to) = PeriodMath.MonthRange(state.Year, state.Month, Calendar, settings.MonthStartDay);
            Title = translator.Format("Review_TitleMonth", dates.Format(from, DateFormatStyle.MonthYear));

            // The current numbers of each step, so the user sees what is open before opening it.
            var unreviewed = entries.Count(e => e.Review == ReviewState.Unreviewed && e.Date <= to);
            var schedules = await plans.GetSchedulesAsync();
            var states = await plans.GetStatesAsync();
            var open = schedules.Sum(s => Occurrences.Between(s, states, s.ActiveFrom ?? s.Rule.Start, to, Today).Count(o => o.IsOpen));
            var notReconciled = accounts.Count(a => !a.IsArchived && (a.LastReconciledOn ?? a.OpeningDate) < to.AddDays(-DataStatus.ReconcileDays));
            var activeGoals = (await goals.GetGoalsAsync()).Count(g => g.State == Core.Goals.GoalState.Active);
            var lastBackup = backup.LastBackupAt is { } at ? dates.Format(DateOnly.FromDateTime(at.ToLocalTime().DateTime), DateFormatStyle.Short) : translator["Issue_NoBackup"];

            void Add(ReviewStep step, string detail, Func<Task> action) =>
                Steps.Add(new ReviewStepRow(step, translator[$"Review_{step}"], detail, action) { IsDone = state.Done.Contains(step) });
            Add(ReviewStep.Unreviewed, translator.Format("Review_UnreviewedDetail", unreviewed),
                () => Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["unreviewed"] = true }));
            Add(ReviewStep.Plans, translator.Format("Review_PlansDetail", open), () => Shell.Current.GoToAsync("//plans"));
            Add(ReviewStep.Reconcile, translator.Format("Review_ReconcileDetail", notReconciled), () => Shell.Current.GoToAsync(AppShell.AccountsRoute));
            Add(ReviewStep.Goals, translator.Format("Review_GoalsDetail", activeGoals), () => Shell.Current.GoToAsync(AppShell.GoalsRoute));
            Add(ReviewStep.Backup, translator.Format("Review_BackupDetail", lastBackup), () => Shell.Current.GoToAsync(AppShell.BackupRoute));
            foreach (var row in Steps)
            {
                row.PropertyChanged += async (_, e) =>
                {
                    if (e.PropertyName == nameof(ReviewStepRow.IsDone) && !_loading)
                    {
                        await Presentation.Failures.GuardAsync(() => SaveStepAsync(row));
                    }
                };
            }

            UpdateProgress();
        }
        finally
        {
            _loading = false;
        }
    }

    private void UpdateProgress() => ProgressText = translator.Format("Review_Progress", Steps.Count(s => s.IsDone), Steps.Count);

    private async Task SaveStepAsync(ReviewStepRow row)
    {
        if (_state is null)
        {
            return;
        }

        // Ticking several steps quickly: each toggle builds on the one before (one settings change at a time).
        var state = _state;
        string? progress = null;
        await store.UpdateSettingsAsync(settings =>
        {
            settings.ReviewProgress = PeriodReview.Toggle(PeriodReview.Parse(settings.ReviewProgress, state.Year, state.Month), row.Step, row.IsDone);
            progress = settings.ReviewProgress;
        });
        _state = PeriodReview.Parse(progress, state.Year, state.Month);
        UpdateProgress();
    }

    [RelayCommand]
    private Task OpenStepAsync(ReviewStepRow row) => row.Open();

    // Finishing hides the Home item until the next month; it changes no entry, plan or balance.
    [RelayCommand]
    private async Task FinishAsync()
    {
        if (_state is null)
        {
            return;
        }

        var state = _state;
        await store.UpdateSettingsAsync(settings => settings.ReviewProgress = PeriodReview.Finish(state));
        await Shell.Current.GoToAsync("..");
    }
}
