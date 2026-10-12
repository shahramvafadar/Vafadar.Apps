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
    public Task PickAsync() => RunAsync(null, null);
    /// <summary>Reads only the exact retained expense attachment and opens its original editor for explicit review.</summary>
    public Task ReadAttachmentAsync(Guid entryId, Guid attachmentId) => RunAsync(entryId, attachmentId);
    private async Task RunAsync(Guid? entryId, Guid? attachmentId)
    {
        if (IsBusy) return;
        IsBusy = true; Error = null;
        try
        {
            var reviewed = await reader.CaptureAsync(entryId, attachmentId);
            var file = reviewed.Attachment is { } original
                ? new ReceiptPickedFile(original.FileName, original.ContentType, original.Data) : await runtime.PickAsync();
            if (file is null) return;
            await reader.ValidateAsync(reviewed);
            if (file.Data.Length == 0 || file.Data.Length > EntryAttachment.MaxBytes)
                throw new ReceiptPreparationException(file.Data.Length == 0 ? "Attachment_Failed" : "Attachment_TooLarge", new InvalidDataException());
            if (entryId is null) await runtime.RunWhenUnlockedAsync(ReadAndOpenAsync);
            else await ReadAndOpenAsync();
            async Task ReadAndOpenAsync()
            {
                await reader.ValidateAsync(reviewed);
                var document = await runtime.ReadAsync(file.RecognitionData ?? file.Data, file.ContentType);
                await reader.ValidateAsync(reviewed);
                var suggestion = ReceiptParser.Parse(document.Text, DateOnly.FromDateTime(time.GetLocalNow().DateTime), document.IsAmbiguous);
                if (suggestion.IsEmpty)
                {
                    await runtime.AlertAsync(translator["Receipt_Title"], translator[entryId is null ? "Receipt_NothingNew" : "Receipt_Nothing"], translator["Common_Ok"]);
                    await reader.ValidateAsync(reviewed);
                    if (entryId is not null) return;
                }
                var draft = new ReceiptDraft(reviewed, suggestion, entryId is null ? file with { RecognitionData = null } : null);
                await draft.ValidateAsync(reader, entryId);
                await runtime.OpenEditorAsync(draft);
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Error = exception is ReceiptPreparationException preparation ? translator[preparation.ResourceKey]
                : CommercialFailureMessage.OrFallback(exception, translator, "Receipt_ReadFailed");
            await runtime.AlertAsync(translator["Receipt_Title"], Error, translator["Common_Ok"]);
        }
        finally { IsBusy = false; }
    }
}
