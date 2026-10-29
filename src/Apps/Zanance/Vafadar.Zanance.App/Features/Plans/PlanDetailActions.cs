using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Original plan-detail consent, stable transition identity and known acceptance across failed follow-through.</summary>
public sealed partial class PlanDetailActions(PlanStore plans, Translator translator, ILocalizationService localization,
    TimeProvider time, IAppInteraction interaction) : ObservableObject
{
    private Guid _owner;
    private bool _active;
    private long _revision;
    private PlanDetailSnapshot? _preparedSource;
    private PlanDetailChange? _prepared;
    private DateOnly _preparedResume;
    private TaskCompletionSource? _idle;
    private bool _acceptedFailure;
    private bool _showAccepted;
    /// <summary>Gets the confirmation/write lifetime, independently of a page read.</summary>
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    /// <summary>Gets translated original refusal or known accepted follow-through feedback.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    /// <summary>Gets an actual accepted result before another view, notification or route can fail.</summary>
    [ObservableProperty] public partial string? Result { get; private set; }
    /// <summary>Gets the last actual accepted transition without renewing its original source authority.</summary>
    public PlanDetailChange? AcceptedChange { get; private set; }
    /// <summary>Gets a failure for the original page's covered reload; it never grants another writer.</summary>
    public Exception? Failure { get; private set; }
    /// <summary>Waits only for the actual pending original action before another source read.</summary>
    public Task WaitForIdleAsync() => _idle?.Task ?? Task.CompletedTask;
    /// <summary>Retires native consent when the actual page or query leaves, retaining known original acceptance.</summary>
    public void SetOwner(Guid id, bool active)
    {
        if (_owner == id && _active == active) return;
        _revision++; _owner = id; _active = active;
        if (AcceptedChange?.DestinationId != id) { Feedback = Result = null; _showAccepted = false; }
        else if (active && _showAccepted)
        {
            Result = translator["Plan_DetailChanged"];
            if (_acceptedFailure) Feedback = translator["Plan_DetailChangedFollowUpFailed"];
        }
    }
    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";
    /// <summary>Clears only obsolete unaccepted feedback after an actual successful original read.</summary>
    public void ClearReadFailure()
    {
        if (_acceptedFailure) return;
        Feedback = null; Failure = null;
    }
    /// <summary>Records a failed native return after an already known commit without renewing its writer.</summary>
    public void ReportFollowUpFailure(PlanDetailChange change)
    {
        if (!change.Accepted || !ReferenceEquals(AcceptedChange, change)) return;
        _acceptedFailure = _showAccepted = true; Result = translator["Plan_DetailChanged"];
        Feedback = translator["Plan_DetailChangedFollowUpFailed"];
    }

    /// <summary>Confirms only the original captured plan, then submits its prepared domain transition once.</summary>
    public async Task<PlanDetailChange?> RunAsync(PlanDetailSnapshot source, PlanDetailOperation operation,
        DateOnly resumeFrom, Func<bool> isCurrent)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(isCurrent);
        var revision = _revision; var display = DisplayIdentity(); var id = source.Source.Request.PlanId;
        bool OwnerCurrent() => _active && id == _owner && revision == _revision && display == DisplayIdentity();
        bool Current() => OwnerCurrent() && DateOnly.FromDateTime(time.GetLocalNow().DateTime) == source.Source.Request.Today && isCurrent();
        if (IsBusy || !Current() || source.Source.Accepted) return null;
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); _idle = idle;
        PlanDetailChange? change = null;
        try
        {
            IsBusy = true; Feedback = Result = null; Failure = null; _acceptedFailure = _showAccepted = false;
            await plans.ValidateDetailSnapshotAsync(source); if (!Current()) return null;
            if (!ReferenceEquals(_preparedSource, source) || _prepared?.Operation != operation
                || operation == PlanDetailOperation.Resume && _preparedResume != resumeFrom)
            {
                _prepared = plans.PrepareDetailChange(source, operation, resumeFrom);
                _preparedSource = source; _preparedResume = resumeFrom;
            }
            change = _prepared ?? throw new InvalidOperationException("The original plan transition was not prepared.");
            if (operation != PlanDetailOperation.Resume)
            {
                var key = operation switch
                {
                    PlanDetailOperation.Pause => "Plan_Pause",
                    PlanDetailOperation.End => "Plan_EndAction",
                    PlanDetailOperation.Delete => "Plan_Delete",
                    _ => throw new InvalidOperationException("Unsupported original plan confirmation."),
                };
                var message = operation switch
                { PlanDetailOperation.Pause => "Plan_PauseMessage", PlanDetailOperation.End => "Plan_EndMessage", _ => "Plan_DeleteMessage" };
                if (!await interaction.ConfirmAsync(translator[key], translator[message],
                        translator[operation == PlanDetailOperation.Delete ? "Common_Delete" : key], translator["Common_Cancel"]) || !Current()) return null;
            }
            await plans.ValidateDetailSnapshotAsync(source); if (!Current()) return null;
            var completed = await plans.ApplyDetailChangeAsync(change, Current);
            if (!completed && !change.Accepted)
                throw new InvalidOperationException("The original plan continuation cannot preserve recorded dates.");
            if (change.Accepted) { AcceptedChange = change; _showAccepted = true; }
            if (change.Accepted && OwnerCurrent()) Result = translator["Plan_DetailChanged"];
            return change.Accepted ? change : null;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            var accepted = change?.Accepted == true;
            _acceptedFailure = _showAccepted = accepted;
            if (accepted) AcceptedChange = change;
            if (accepted ? OwnerCurrent() : Current())
            {
                Failure = exception;
                if (accepted) Result = translator["Plan_DetailChanged"];
                Feedback = accepted ? translator["Plan_DetailChangedFollowUpFailed"]
                    : CommercialFailureMessage.OrFallback(exception, translator, "Plan_DetailActionFailed");
            }
            return accepted ? change : null;
        }
        finally
        {
            try { IsBusy = false; }
            finally
            {
                if (ReferenceEquals(_idle, idle)) _idle = null;
                idle.TrySetResult();
            }
        }
    }
}
