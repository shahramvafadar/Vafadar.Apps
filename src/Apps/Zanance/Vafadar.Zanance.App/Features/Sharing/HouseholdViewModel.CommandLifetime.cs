using Vafadar.Zanance.App.Features.Sync;

namespace Vafadar.Zanance.App.Features.Sharing;

public sealed partial class HouseholdViewModel
{
    private Func<bool>? ownsManualFeedback;
    private bool acceptedOperation;
    private bool primarySubmitted;

    /// <summary>A missing acknowledgement is an unknown outcome, never permission to repeat the original primary command.</summary>
    private async Task<T> AwaitPrimaryAsync<T>(Func<Task<T>> effect)
    {
        operationOwner?.Invoke(); primarySubmitted = true;
        var result = await effect(); RecordAcceptedOperation(); return result;
    }

    /// <summary>Preserves the same unknown/accepted boundary for original operations without a return payload.</summary>
    private async Task AwaitPrimaryAsync(Func<Task> effect)
    {
        operationOwner?.Invoke(); primarySubmitted = true;
        await effect(); RecordAcceptedOperation();
    }

    /// <summary>Allows only this original native frame across an explicitly owned profile maintenance transition.</summary>
    private Action CaptureProfileTransitionOwner()
    {
        var originalVisible = visible; var disclosure = disclosureVersion;
        var inputs = (Address, Fingerprint, Email, Name, InvitationText, ConfirmedRoot);
        return () =>
        {
            if (!OwnsPresentation || !ReferenceEquals(visible, originalVisible) || disclosure != disclosureVersion
                || inputs != (Address, Fingerprint, Email, Name, InvitationText, ConfirmedRoot))
                throw new OperationCanceledException("The original household profile transition retired.");
        };
    }

    /// <summary>Captures the original native disclosure and literal inputs around the actual platform prompt.</summary>
    private async Task<T> AwaitNativeAsync<T>(Func<Task<T>> effect)
    {
        var validate = operationOwner ?? throw new OperationCanceledException("The original household native frame retired.");
        validate(); var inputs = (Address, Fingerprint, Email, Name, InvitationText, ConfirmedRoot);
        var answer = await effect(); validate();
        if (inputs != (Address, Fingerprint, Email, Name, InvitationText, ConfirmedRoot))
            throw new OperationCanceledException("The original household native inputs changed.");
        return answer;
    }

    /// <summary>Records only a returned original acknowledgement, before fallible refresh or native follow-through.</summary>
    private void RecordAcceptedOperation() { primarySubmitted = true; acceptedOperation = true; }

    /// <summary>A failed follow-through cannot turn an acknowledged primary operation into an editable retry.</summary>
    private void RetainFailure(string key)
    {
        if (acceptedOperation) { ready = false; errorKey = "Sync_AcceptedFollowUpFailed"; }
        else { if (primarySubmitted) ready = false; errorKey = key; }
    }
    /// <summary>Owns the original picker/file/profile continuation independently of shared busy feedback.</summary>
    private void BeginManualCommand()
    {
        if (!OwnsPresentation || HasPendingOperation) throw new OperationCanceledException("The original household command retired.");
        var originalVisible = visible; var disclosure = disclosureVersion; var inputs = (Address, Fingerprint, Email);
        ownsManualFeedback = () => OwnsPresentation && ReferenceEquals(visible, originalVisible)
            && disclosureVersion == disclosure && inputs == (Address, Fingerprint, Email);
        acceptedOperation = false; primarySubmitted = false; commandActive = true; IsBusy = true;
        commandCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Combines actual profile access with the original native page and disclosure.</summary>
    private PersonalSyncAccess CaptureManualAccess()
    {
        var originalVisible = visible ?? throw new OperationCanceledException("The original household page retired.");
        var disclosure = disclosureVersion; var inputs = (Address, Fingerprint, Email);
        var captured = environment.Capture();
        operation = CancellationTokenSource.CreateLinkedTokenSource(captured.Cancellation, originalVisible.Token);
        var lifetime = operation;
        var access = new PersonalSyncAccess(() =>
        {
            captured.Validate(); lifetime.Token.ThrowIfCancellationRequested();
            if (!OwnsPresentation || !ReferenceEquals(visible, originalVisible) || disclosure != disclosureVersion
                || inputs != (Address, Fingerprint, Email))
                throw new OperationCanceledException("The original household native continuation retired.");
        }, lifetime.Token, captured.Scope);
        operationOwner = access.Validate; access.Validate(); return access;
    }

    /// <summary>Releases only the serialized original native command after its last follow-through.</summary>
    private void EndManualCommand()
    {
        var completion = commandCompletion;
        operation?.Dispose(); operation = null; operationOwner = null;
        commandActive = false; IsBusy = false; commandCompletion = null; ownsManualFeedback = null;
        completion?.TrySetResult(); Refresh();
    }
    /// <summary>A retired picker or profile continuation cannot publish its error into another frame.</summary>
    private bool CanPublishManualFeedback => ownsManualFeedback?.Invoke() == true;
}
