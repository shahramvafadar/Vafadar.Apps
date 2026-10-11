using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Budget;

/// <summary>Original displayed budget coordinates, retained independently of a later tab or period change.</summary>
public sealed record BudgetVisit(int Year, int Month, PeriodCalendar Calendar, BudgetPeriod Period,
    DateOnly Start, string Currency);

/// <summary>
/// Actual budget view actions over the complete displayed review. History never grants new work; confirmations,
/// paired writes and post-commit publication share one busy guard (D-153).
/// </summary>
public sealed partial class BudgetActionsViewModel(ZananceStore store, Translator translator, IDateFormatter dates,
    IAppInteraction interaction, Func<BudgetVisit?, Task> reload) : ViewModelBase
{
    private BudgetEditorReview? _review;
    private BudgetVisit? _visit, _nextVisit;
    private Core.Budgets.Budget? _original, _previousCopy, _nextCopy, _nextOriginal;
    private bool _ready, _loading = true, _needsReload;

    /// <summary>Announces the validated editor transition without granting a write or storing navigation preferences.</summary>
    public event EventHandler? EditorOpening;

    /// <summary>Gets translated failure feedback; a committed write has its own refresh-only message.</summary>
    [ObservableProperty]
    public partial string? Feedback { get; set; }

    /// <summary>Gets whether the original loaded page can accept navigation or actions.</summary>
    public bool CanInteract => _ready && !_loading && !_needsReload && IsNotBusy;
    /// <summary>Gets whether a successful explicit reload is required before another action.</summary>
    public bool NeedsReload => _needsReload;
    /// <summary>Gets whether complete parent publication is still in progress.</summary>
    public bool IsLoading => _loading;
    /// <summary>Gets whether Reload can read current data without repeating the previous write.</summary>
    public bool CanReload => !_loading && IsNotBusy;
    /// <summary>Gets the independent visible previous-period alternative, even if new work is unavailable.</summary>
    public bool HasPrevious => _original is null && _previousCopy is not null;
    /// <summary>Gets original correction or new-definition editor availability.</summary>
    public bool CanEdit => CanInteract && _review is { } review && (_original is not null
        ? review.Choices.Work.CanCorrect : review.Choices.CanSave(EmptyDefinition()));
    /// <summary>Gets the exact previous-copy feature/capacity projection; its writer rechecks the same review.</summary>
    public bool CanCopyPrevious => CanInteract && HasPrevious && _review!.Choices.CanSave(_previousCopy!);
    /// <summary>Gets next-period copying/replacement independently of original financial history.</summary>
    public bool CanCopyNext => CanInteract && _nextCopy is not null && _review!.Choices.CanSave(_nextCopy, _nextOriginal?.Id);
    /// <summary>Gets deletion independently of paid tools, selection and definition capacity.</summary>
    public bool CanDelete => CanInteract && _original is not null && _review!.Choices.CanDelete;
    /// <summary>Gets explanatory work feedback without hiding the original amounts or correction actions.</summary>
    public string? WorkText => _ready && _review is { } review && (_original is { } original
        ? !review.Choices.Work.CanUse(original.Id) && Core.Commerce.BudgetSelectionPolicy.IsCurrent(original, review.Choices.Today, review.Choices.MonthStartDay)
        : !review.Choices.CanSave(EmptyDefinition())) ? translator["Budget_WorkUnavailable"] : null;

    /// <summary>Retires action availability while the parent reads all original financial presentation inputs.</summary>
    public void BeginLoad() { _loading = true; RefreshActions(); }
    /// <summary>Keeps failed or retired publication unavailable until an explicit successful read.</summary>
    public void FailLoad()
    {
        _loading = false; _needsReload = true;
        Feedback ??= translator["Error_Unexpected"]; RefreshActions();
    }
    /// <summary>Publishes only complete original periods and references, with no financial or preference write.</summary>
    public void Publish(BudgetEditorReview review, BudgetVisit visit, Guid? originalId)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(visit);
        var originals = review.Choices.Items.Select(item => item.ToBudget()).ToArray();
        var original = originalId is { } id ? originals.Single(budget => budget.Id == id) : null;
        var previousVisit = Step(visit, -1); var nextVisit = Step(visit, 1);
        var previous = Find(originals, previousVisit); var next = Find(originals, nextVisit);
        // Resolve every candidate before replacing the published packet; ambiguous periods never guess a source.
        _review = review; _visit = visit; _original = original; _nextVisit = nextVisit; _nextOriginal = next;
        _previousCopy = previous is null ? null : Copy(previous, visit);
        _nextCopy = original is null ? null : Copy(original, nextVisit);
        _ready = true; _loading = false; _needsReload = false; Feedback = null; RefreshActions();
    }

    private Core.Budgets.Budget EmptyDefinition() => new()
    {
        Year = _visit!.Period == BudgetPeriod.Month ? _visit.Year : 0,
        Month = _visit.Period == BudgetPeriod.Month ? _visit.Month : 0,
        Calendar = _visit.Calendar, Period = _visit.Period, PeriodStart = _visit.Start, CurrencyCode = _visit.Currency,
    };
    private static Core.Budgets.Budget Copy(Core.Budgets.Budget source, BudgetVisit visit) => visit.Period == BudgetPeriod.Month
        ? BudgetPlanning.CopyTo(source, visit.Year, visit.Month) : BudgetPlanning.CopyTo(source, visit.Start);
    private static BudgetVisit Step(BudgetVisit visit, int step)
    {
        if (visit.Period != BudgetPeriod.Month) return visit with { Start = BudgetPeriods.Step(visit.Period, visit.Start, step) };
        var (year, month) = step < 0 ? PeriodMath.Previous(visit.Year, visit.Month) : PeriodMath.Next(visit.Year, visit.Month);
        return visit with { Year = year, Month = month };
    }
    /// <summary>Resolves an original period from the complete review, preserving its named calendar and explicit week dates.</summary>
    public static Core.Budgets.Budget? Find(IEnumerable<Core.Budgets.Budget> originals, BudgetVisit visit) => originals
        .Where(budget => budget.Period == visit.Period && budget.Calendar == visit.Calendar && budget.CurrencyCode == visit.Currency
            && (visit.Period == BudgetPeriod.Month ? budget.Year == visit.Year && budget.Month == visit.Month
                : visit.Start >= budget.PeriodStart && visit.Start <= BudgetPeriods.Range(budget.Period, budget.PeriodStart).Last)).SingleOrDefault();

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy)) RefreshActions();
    }
    /// <summary>Refreshes live explanatory captions without writing or reconstructing a confirmed operation.</summary>
    public void RefreshDisplay() => RefreshActions();
    private void RefreshActions()
    {
        foreach (var property in new[] { nameof(CanInteract), nameof(NeedsReload), nameof(IsLoading), nameof(CanReload), nameof(HasPrevious),
            nameof(CanEdit), nameof(CanCopyPrevious), nameof(CanCopyNext), nameof(CanDelete), nameof(WorkText) }) OnPropertyChanged(property);
        EditCommand.NotifyCanExecuteChanged(); CopyPreviousCommand.NotifyCanExecuteChanged();
        CopyToNextCommand.NotifyCanExecuteChanged(); DeleteCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task EditAsync()
    {
        if (!CanEdit || _review is not { } review || _visit is not { } visit) return;
        await RunAsync(async () =>
        {
            await ValidateAsync(review);
            EditorOpening?.Invoke(this, EventArgs.Empty);
            await interaction.NavigateAsync(AppRoutes.BudgetEditorRoute, new Dictionary<string, object>
            {
                ["id"] = (object?)_original?.Id ?? null!, ["scope"] = review.Choices.ReminderScope,
                ["year"] = visit.Year, ["month"] = visit.Month, ["calendar"] = visit.Calendar,
                ["currency"] = visit.Currency, ["period"] = visit.Period, ["start"] = visit.Start,
            });
            return false;
        });
    }
    [RelayCommand(CanExecute = nameof(CanCopyPrevious))]
    private async Task CopyPreviousAsync()
    {
        if (!CanCopyPrevious || _review is not { } review || _previousCopy is not { } copy) return;
        await RunAsync(async () =>
        {
            await ValidateAsync(review);
            await store.SaveBudgetAsync(copy, review: review); return true;
        });
    }
    [RelayCommand(CanExecute = nameof(CanCopyNext))]
    private async Task CopyToNextAsync()
    {
        if (!CanCopyNext || _review is not { } review || _nextCopy is not { } copy || _nextVisit is not { } next) return;
        var replaced = _nextOriginal?.Id;
        await RunAsync(async () =>
        {
            await ValidateAsync(review);
            var range = next.Period == BudgetPeriod.Month
                ? PeriodMath.MonthRange(next.Year, next.Month, next.Calendar, review.Choices.MonthStartDay)
                : BudgetPeriods.Range(next.Period, next.Start);
            var target = next.Period == BudgetPeriod.Month ? dates.Format(range.Item1, DateFormatStyle.MonthYear)
                : $"{dates.Format(range.Item1, DateFormatStyle.Short)} – {dates.Format(range.Item2, DateFormatStyle.Short)}";
            if (!await interaction.ConfirmAsync(translator["Budget_CopyNext"],
                translator.Format(replaced is null ? "Budget_CopyMessage" : "Budget_CopyReplaceMessage", target),
                translator["Budget_Copy"], translator["Common_Cancel"])) return false;
            await ValidateAsync(review);
            if (replaced is { } id) await store.ReplaceBudgetAsync(id, copy, review: review);
            else await store.SaveBudgetAsync(copy, review: review);
            return true;
        }, next);
    }
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (!CanDelete || _review is not { } review || _original is not { } original) return;
        await RunAsync(async () =>
        {
            await ValidateAsync(review);
            if (!await interaction.ConfirmAsync(translator["Budget_Delete"], translator["Budget_DeleteMessage"],
                translator["Common_Delete"], translator["Common_Cancel"])) return false;
            await ValidateAsync(review);
            await store.DeleteBudgetAsync(original.Id, review: review); return true;
        });
    }
    private async Task ValidateAsync(BudgetEditorReview review)
    {
        if (!ReferenceEquals(_review, review) || _loading || _needsReload)
            throw new InvalidOperationException("The displayed budget changed; reload before acting.");
        await store.ValidateBudgetEditorReviewAsync(review);
        if (!ReferenceEquals(_review, review) || _loading || _needsReload)
            throw new InvalidOperationException("The budget view was retired while its review was read.");
    }
    private async Task RunAsync(Func<Task<bool>> operation, BudgetVisit? next = null)
    {
        if (!CanInteract) return;
        IsBusy = true; Feedback = null; var committed = false;
        try
        {
            committed = await operation();
            if (committed) { _needsReload = true; RefreshActions(); await reload(next); }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _needsReload = true; _loading = false;
            Feedback = translator[committed ? "Occurrence_SavedRefreshFailed" : "Error_Unexpected"];
            if (!committed) await interaction.ShowFailureAsync(exception);
        }
        finally { IsBusy = false; RefreshActions(); }
    }
    [RelayCommand(CanExecute = nameof(CanReload))]
    private async Task ReloadAsync()
    {
        if (!CanReload) return;
        IsBusy = true;
        try { await reload(null); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { FailLoad(); await interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; }
    }
}
