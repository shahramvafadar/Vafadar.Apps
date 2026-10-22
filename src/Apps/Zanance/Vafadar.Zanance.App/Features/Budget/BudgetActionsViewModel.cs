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
    private long _revision, _retirement;
    private readonly Dictionary<string, IAsyncRelayCommand> _commands = [];

    /// <summary>Gets commands bound to the exact published original period and page lifetime.</summary>
    public IAsyncRelayCommand EditCommand => Command(nameof(EditCommand), EditAsync, () => CanEdit);
    /// <summary>Gets a previous-copy command that cannot migrate to another displayed period.</summary>
    public IAsyncRelayCommand CopyPreviousCommand => Command(nameof(CopyPreviousCommand), CopyPreviousAsync, () => CanCopyPrevious);
    /// <summary>Gets a next-copy command owned by this original confirmation and period.</summary>
    public IAsyncRelayCommand CopyToNextCommand => Command(nameof(CopyToNextCommand), CopyToNextAsync, () => CanCopyNext);
    /// <summary>Gets deletion owned by the original displayed budget, including while consent is pending.</summary>
    public IAsyncRelayCommand DeleteCommand => Command(nameof(DeleteCommand), DeleteAsync, () => CanDelete);
    /// <summary>Gets a read-only reload command for the current page lifetime.</summary>
    public IAsyncRelayCommand ReloadCommand => Command(nameof(ReloadCommand), ReloadAsync, () => CanReload);

    private IAsyncRelayCommand Command(string name, Func<Task> execute, Func<bool> enabled)
    {
        if (_commands.TryGetValue(name, out var existing)) return existing;
        var revision = _revision;
        var command = new AsyncRelayCommand(() => revision == _revision && enabled() ? execute() : Task.CompletedTask,
            () => revision == _revision && enabled());
        _commands.Add(name, command); return command;
    }

    private void RetireCommands()
    {
        _revision++;
        foreach (var command in _commands.Values.ToArray()) command.NotifyCanExecuteChanged();
        _commands.Clear();
        foreach (var name in new[] { nameof(EditCommand), nameof(CopyPreviousCommand), nameof(CopyToNextCommand), nameof(DeleteCommand), nameof(ReloadCommand) })
            OnPropertyChanged(name);
    }

    /// <summary>Invalidates retained native commands and pending consent when the original page disappears.</summary>
    public void Retire()
    {
        _retirement++;
        _ready = false; _loading = false; _needsReload = false; _review = null;
        RetireCommands(); RefreshActions();
    }

    private bool Owns(BudgetEditorReview review, long revision) => revision == _revision
        && ReferenceEquals(_review, review) && _ready && !_loading && !_needsReload;

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
    public bool CanReload => (_ready || _needsReload) && !_loading && IsNotBusy;
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
    public void BeginLoad() { _loading = true; RetireCommands(); RefreshActions(); }
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
        _ready = true; _loading = false; _needsReload = false; Feedback = null; RetireCommands(); RefreshActions();
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

    private async Task EditAsync()
    {
        if (!CanEdit || _review is not { } review || _visit is not { } visit) return;
        var revision = _revision; var originalId = _original?.Id;
        await RunAsync(async accepted =>
        {
            await ValidateAsync(review, revision);
            EditorOpening?.Invoke(this, EventArgs.Empty);
            if (!Owns(review, revision)) return false;
            await interaction.NavigateAsync(AppRoutes.BudgetEditorRoute, new Dictionary<string, object>
            {
                ["id"] = (object?)originalId ?? null!, ["scope"] = review.Choices.ReminderScope,
                ["year"] = visit.Year, ["month"] = visit.Month, ["calendar"] = visit.Calendar,
                ["currency"] = visit.Currency, ["period"] = visit.Period, ["start"] = visit.Start,
            });
            return false;
        });
    }
    private async Task CopyPreviousAsync()
    {
        if (!CanCopyPrevious || _review is not { } review || _previousCopy is not { } copy) return;
        var revision = _revision;
        await RunAsync(async accepted =>
        {
            await ValidateAsync(review, revision);
            await store.SaveBudgetAsync(copy, review: review, inputCurrent: () => Owns(review, revision), accepted: accepted); return true;
        });
    }
    private async Task CopyToNextAsync()
    {
        if (!CanCopyNext || _review is not { } review || _nextCopy is not { } copy || _nextVisit is not { } next) return;
        var replaced = _nextOriginal?.Id;
        var revision = _revision;
        await RunAsync(async accepted =>
        {
            await ValidateAsync(review, revision);
            var range = next.Period == BudgetPeriod.Month
                ? PeriodMath.MonthRange(next.Year, next.Month, next.Calendar, review.Choices.MonthStartDay)
                : BudgetPeriods.Range(next.Period, next.Start);
            var target = next.Period == BudgetPeriod.Month ? dates.Format(range.Item1, DateFormatStyle.MonthYear)
                : $"{dates.Format(range.Item1, DateFormatStyle.Short)} – {dates.Format(range.Item2, DateFormatStyle.Short)}";
            if (!await interaction.ConfirmAsync(translator["Budget_CopyNext"],
                translator.Format(replaced is null ? "Budget_CopyMessage" : "Budget_CopyReplaceMessage", target),
                translator["Budget_Copy"], translator["Common_Cancel"])) return false;
            await ValidateAsync(review, revision);
            if (replaced is { } id) await store.ReplaceBudgetAsync(id, copy, review: review, inputCurrent: () => Owns(review, revision), accepted: accepted);
            else await store.SaveBudgetAsync(copy, review: review, inputCurrent: () => Owns(review, revision), accepted: accepted);
            return true;
        }, next);
    }
    private async Task DeleteAsync()
    {
        if (!CanDelete || _review is not { } review || _original is not { } original) return;
        var revision = _revision;
        await RunAsync(async accepted =>
        {
            await ValidateAsync(review, revision);
            if (!await interaction.ConfirmAsync(translator["Budget_Delete"], translator["Budget_DeleteMessage"],
                translator["Common_Delete"], translator["Common_Cancel"])) return false;
            await ValidateAsync(review, revision);
            await store.DeleteBudgetAsync(original.Id, review: review, inputCurrent: () => Owns(review, revision), accepted: accepted); return true;
        });
    }
    private async Task ValidateAsync(BudgetEditorReview review, long revision)
    {
        if (!Owns(review, revision))
            throw new InvalidOperationException("The displayed budget changed; reload before acting.");
        await store.ValidateBudgetEditorReviewAsync(review);
        if (!Owns(review, revision))
            throw new InvalidOperationException("The budget view was retired while its review was read.");
    }
    private async Task RunAsync(Func<Action, Task<bool>> operation, BudgetVisit? next = null)
    {
        if (!CanInteract) return;
        var revision = _revision; var originalReview = _review;
        IsBusy = true; Feedback = null; var committed = false;
        try
        {
            committed |= await operation(() => committed = true);
            if (revision != _revision) return;
            if (committed) { _needsReload = true; RefreshActions(); await reload(next); }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // D-461: an old operation cannot replace the newer page's ready/feedback state.
            if (revision != _revision && (!committed || !ReferenceEquals(_review, originalReview))) return;
            _needsReload = true; _loading = false;
            Feedback = committed ? translator["Occurrence_SavedRefreshFailed"]
                : Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Error_Unexpected", draftKept: true);
            if (!committed) await interaction.ShowFailureAsync(exception);
        }
        finally { IsBusy = false; RefreshActions(); }
    }
    private async Task ReloadAsync()
    {
        if (!CanReload) return;
        var retirement = _retirement; var originalReview = _review;
        IsBusy = true;
        try { await reload(null); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (retirement != _retirement || !ReferenceEquals(_review, originalReview)) return;
            FailLoad(); await interaction.ShowFailureAsync(exception);
        }
        finally { IsBusy = false; }
    }
}
