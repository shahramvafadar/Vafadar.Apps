using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Receipts;

/// <summary>Actual shared Home/attachment recognition flow; never saves an entry or overwrites an existing amount.</summary>
public sealed partial class ReceiptReadingSession(ReceiptWorkStore reader, IReceiptRuntime runtime, Translator translator,
    TimeProvider time) : ObservableObject
{
    /// <summary>Gets one busy lifetime spanning access, native source, unlock, reading, feedback and destination.</summary>
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    /// <summary>Gets full translated failure feedback without exposing recognition content or native exception details.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Starts the original camera/picker route; declined access never opens a native source.</summary>
    public Task PickAsync(Func<bool>? isCurrent = null) => RunAsync(null, null, isCurrent);
    /// <summary>Reads only the exact retained expense attachment and opens its original editor for explicit review.</summary>
    public Task ReadAttachmentAsync(Guid entryId, Guid attachmentId, Func<bool>? isCurrent = null) => RunAsync(entryId, attachmentId, isCurrent);
    private async Task RunAsync(Guid? entryId, Guid? attachmentId, Func<bool>? isCurrent)
    {
        if (IsBusy) return;
        ReceiptReadOrigin? origin = null;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration retirement = default;
        var publishing = false;
        bool Current() => origin?.IsCurrent == true && isCurrent?.Invoke() != false;
        void RequireCurrent()
        {
            if (!Current()) throw new OperationCanceledException("The original receipt presentation retired.");
        }
        try
        {
            origin = runtime.CaptureOrigin();
            retirement = origin.Invalidated.Register(() =>
            {
                // Own navigation may retire the source page before its original native Task completes.
                if (!Volatile.Read(ref publishing)) completed.TrySetResult();
            });
            RequireCurrent();
            // D-952: capture the original page before startup notifications can replace it.
            // Every publication stays inside cleanup and rechecks that captured owner before effects.
            IsBusy = true; RequireCurrent();
            Error = null; RequireCurrent();
            var reviewed = await reader.CaptureAsync(entryId, attachmentId);
            RequireCurrent();
            var file = reviewed.Attachment is { } original
                ? new ReceiptPickedFile(original.FileName, original.ContentType, original.Data) : await runtime.PickAsync();
            if (file is null) return;
            RequireCurrent();
            await reader.ValidateAsync(reviewed);
            if (file.Data.Length == 0 || file.Data.Length > EntryAttachment.MaxBytes)
                throw new ReceiptPreparationException(file.Data.Length == 0 ? "Attachment_Failed" : "Attachment_TooLarge", new InvalidDataException());

            await QueueAsync(ReadAndPrepareAsync);

            async Task<bool> ReadAndPrepareAsync()
            {
                RequireCurrent();
                await reader.ValidateAsync(reviewed);
                RequireCurrent();
                var document = await runtime.ReadAsync(file.RecognitionData ?? file.Data, file.ContentType);
                RequireCurrent();
                await reader.ValidateAsync(reviewed);
                RequireCurrent();
                var suggestion = ReceiptParser.Parse(document.Text, DateOnly.FromDateTime(time.GetLocalNow().DateTime), document.IsAmbiguous);
                var draft = new ReceiptDraft(reviewed, suggestion, entryId is null ? file with { RecognitionData = null } : null);
                var noticeShown = false;
                return await PublishAsync();

                async Task<bool> PublishAsync()
                {
                    RequireCurrent();
                    await reader.ValidateAsync(reviewed);
                    RequireCurrent();
                    // D-709: retain only the completed result at the original access drain's head.
                    // Unlock revalidates this exact source; it never repeats OCR or an answered notice.
                    if (runtime.RetainPublicationIfLocked(() => GuardedAsync(PublishAsync))) return true;
                    if (suggestion.IsEmpty && !noticeShown)
                    {
                        await runtime.AlertAsync(translator["Receipt_Title"], translator[entryId is null ? "Receipt_NothingNew" : "Receipt_Nothing"], translator["Common_Ok"]);
                        noticeShown = true;
                        RequireCurrent();
                        await reader.ValidateAsync(reviewed);
                        RequireCurrent();
                    }
                    if (suggestion.IsEmpty && entryId is not null) return false;
                    await draft.ValidateAsync(reader, entryId);
                    RequireCurrent();
                    if (runtime.RetainPublicationIfLocked(() => GuardedAsync(PublishAsync))) return true;
                    Volatile.Write(ref publishing, true);
                    await runtime.OpenEditorAsync(draft);
                    return false;
                }
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (Current()) await QueueAsync(() => ShowFailureAsync(exception));
        }
        finally
        {
            retirement.Dispose(); origin?.Dispose(); IsBusy = false;
        }

        async Task QueueAsync(Func<Task<bool>> stage)
        {
            if (!Current()) return;
            var accepted = await runtime.QueueWhenUnlockedAsync(() => GuardedAsync(stage));
            if (!accepted) completed.TrySetResult();
            // A queued access callback returns immediately; busy ownership ends only at its real terminal stage.
            await completed.Task;
        }
        async Task GuardedAsync(Func<Task<bool>> stage)
        {
            var deferred = false;
            try { if (Current()) deferred = await stage(); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                if (Current())
                {
                    try { deferred = await ShowFailureAsync(exception); }
                    catch (Exception feedback) when (feedback is not OutOfMemoryException)
                    { /* An uncertain native failure acknowledgement must not replay feedback into a new view. */ }
                }
            }
            finally { if (!deferred) completed.TrySetResult(); }
        }
        async Task<bool> ShowFailureAsync(Exception exception)
        {
            if (!Current()) return false;
            if (runtime.RetainPublicationIfLocked(() => GuardedAsync(() => ShowFailureAsync(exception)))) return true;
            Error = exception is ReceiptPreparationException preparation ? translator[preparation.ResourceKey]
                : CommercialFailureMessage.OrFallback(exception, translator, "Receipt_ReadFailed");
            if (!Current()) return false;
            await runtime.AlertAsync(translator["Receipt_Title"], Error, translator["Common_Ok"]);
            return false;
        }
    }
}
