using Vafadar.Documents;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Receipts;

/// <summary>Prepared compact attachment and optional transient bounded recognition image.</summary>
public sealed record ReceiptPickedFile(string Name, string ContentType, byte[] Data, byte[]? RecognitionData = null);

/// <summary>Typed process-local destination; only the compact copy reaches the unsaved entry editor.</summary>
public sealed record ReceiptDraft(ReceiptReadReview Review, ReceiptSuggestion Suggestion, ReceiptPickedFile? File)
{
    /// <summary>Revalidates the exact original destination before applying a late suggestion to an editor.</summary>
    public async Task ValidateAsync(ReceiptWorkStore reader, Guid? entryId)
    {
        if (entryId != Review.EntryId || File?.RecognitionData is not null)
            throw new InvalidOperationException("The receipt draft destination changed.");
        await reader.ValidateAsync(Review);
    }
}

/// <summary>Original camera/picker, app lock, document extraction and UI effects; application policy remains testable.</summary>
public interface IReceiptRuntime
{
    /// <summary>Picks or captures with the original contextual permission and bounded preparation.</summary>
    Task<ReceiptPickedFile?> PickAsync();
    /// <summary>Runs recognition/publication only through the original app-lock gate after external picking.</summary>
    Task RunWhenUnlockedAsync(Func<Task> action);
    /// <summary>Captures only the creating native page/root, independently of a receipt's original file review.</summary>
    ReceiptReadOrigin CaptureOrigin() => new(() => true);
    /// <summary>Queues the original read and reports whether it remains owned, including a deferred unlock.</summary>
    async Task<bool> QueueWhenUnlockedAsync(Func<Task> action)
    {
        var invoked = false;
        await RunWhenUnlockedAsync(async () => { invoked = true; await action(); });
        return invoked;
    }
    /// <summary>Retains only unfinished presentation at the current drain's head when access closed during a read.</summary>
    bool RetainPublicationIfLocked(Func<Task> continuation) => false;
    /// <summary>Uses the original native OCR/PDF reader, preserving source layout ambiguity.</summary>
    Task<TextLayoutResult> ReadAsync(byte[] data, string contentType);
    /// <summary>Shows translated feedback in the actual application window.</summary>
    Task AlertAsync(string title, string message, string cancel);
    /// <summary>Opens the original new or existing entry editor without saving financial data.</summary>
    Task OpenEditorAsync(ReceiptDraft draft);
}

/// <summary>A native preparation failure with an existing translated attachment explanation.</summary>
public sealed class ReceiptPreparationException(string resourceKey, Exception inner) : Exception("Receipt preparation failed.", inner)
{
    /// <summary>Gets the established preparation feedback key, never native exception text.</summary>
    public string ResourceKey { get; } = resourceKey;
}

/// <summary>Owns one original native receipt page/root without granting access or retaining it after disposal.</summary>
public sealed class ReceiptReadOrigin(Func<bool> isCurrent, CancellationToken invalidated = default, Action? release = null) : IDisposable
{
    private Func<bool>? _isCurrent = isCurrent;
    private Action? _release = release;
    /// <summary>Gets whether the original presentation is still current; it never substitutes for file validation.</summary>
    public bool IsCurrent => !Invalidated.IsCancellationRequested && _isCurrent?.Invoke() == true;
    /// <summary>Signals actual retirement while work is waiting for access, without replaying the native source.</summary>
    public CancellationToken Invalidated { get; } = invalidated;
    /// <inheritdoc />
    public void Dispose() { _isCurrent = null; var release = _release; _release = null; release?.Invoke(); }
}
