using System.Runtime.ExceptionServices;

namespace Vafadar.Zanance.App.Features.Sync;

public sealed partial class PersonalSyncViewModel
{
    private bool retiringRequestInput;
    private bool refusingLateRequestInput;
    private Exception? requestInputFeedbackFailure;
    private bool retiringPreview;
    private Exception? previewFeedbackFailure;

    /// <summary>Clears request-only text even when a departing native binding cannot receive its notification.</summary>
    private void ClearRetiringPassword()
    {
        retiringRequestInput = true;
        requestInputFeedbackFailure = null;
        try { Password = ""; }
        finally { retiringRequestInput = false; }
        var failure = requestInputFeedbackFailure;
        requestInputFeedbackFailure = null;
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    partial void OnPasswordChanging(string value)
    {
        // A reentrant old binding cannot repopulate input while its original owner is retiring.
        if (retiringRequestInput && value is not "")
            throw new OperationCanceledException("Retired request input cannot be repopulated.");
        if (!retiringRequestInput) refusingLateRequestInput = value.Length != 0 && !CanInteract;
    }

    partial void OnPasswordChanged(string value)
    {
        // D-1050: a late native TextChanged can arrive after the original form loses interaction authority.
        // Keep the original nonsecret drafts, but do not retain request text from that disabled binding.
        if (value is "" || CanInteract) return;
        try { ClearRetiringPassword(); }
        catch (Exception error) when (error is not OutOfMemoryException) { ReportFeedbackFailure(error); }
    }

    /// <summary>Clears the current frame reference before its original bytes are disposed.</summary>
    private void ClearRetiringPreviewValue()
    {
        retiringPreview = true;
        previewFeedbackFailure = null;
        try { AttachmentPreview = null; }
        finally { retiringPreview = false; }
        var failure = previewFeedbackFailure;
        previewFeedbackFailure = null;
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    partial void OnAttachmentPreviewChanging(PersonalSyncRenderedPage? value)
    {
        if (retiringPreview && value is not null)
            throw new OperationCanceledException("Retired preview cannot be repopulated.");
    }

    /// <summary>Limits fallible notification handling to the exact field currently being cleared.</summary>
    private bool IsRetiringField(string? property) =>
        retiringRequestInput && property == nameof(Password) || retiringPreview && property == nameof(AttachmentPreview);

    /// <summary>Retains the first callback failure without retaining request text or displayed bytes.</summary>
    private void RetainRetirementFailure(string? property, Exception error)
    {
        if (property == nameof(Password)) requestInputFeedbackFailure ??= error;
        else previewFeedbackFailure ??= error;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanging(System.ComponentModel.PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(Password) && !retiringRequestInput && refusingLateRequestInput) return;
        if (!IsRetiringField(e.PropertyName)) { base.OnPropertyChanging(e); return; }
        try { base.OnPropertyChanging(e); }
        catch (Exception error) when (error is not OutOfMemoryException) { RetainRetirementFailure(e.PropertyName, error); }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Password) && !retiringRequestInput && refusingLateRequestInput)
        {
            // Only this rejected nonempty assignment loses publication; ordinary empty cleanup still binds.
            refusingLateRequestInput = false;
            return;
        }
        if (!IsRetiringField(e.PropertyName)) { base.OnPropertyChanged(e); return; }
        try { base.OnPropertyChanged(e); }
        catch (Exception error) when (error is not OutOfMemoryException) { RetainRetirementFailure(e.PropertyName, error); }
    }
}
