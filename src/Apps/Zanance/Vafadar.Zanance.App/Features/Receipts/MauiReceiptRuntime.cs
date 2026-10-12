using Vafadar.Documents;
using Vafadar.Documents.Maui;
using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Features.Receipts;

/// <summary>Original contextual camera/file picker, bounded metadata-free preparation and native receipt effects.</summary>
public sealed class MauiReceiptRuntime(Translator translator, AppLockService appLock, IAppInteraction interaction) : IReceiptRuntime
{
    /// <inheritdoc />
    public async Task<ReceiptPickedFile?> PickAsync()
    {
        try
        {
            PreparedAttachment? picked;
            if (PermissionPrompts.CanTakePhoto)
            {
                var camera = translator["Receipt_TakePhoto"];
                var choice = await interaction.ChooseAsync(translator["Receipt_SourceTitle"], translator["Common_Cancel"], camera, translator["Receipt_ChooseFile"]);
                if (choice == camera)
                {
                    picked = await PermissionPrompts.TakePhotoAsync(translator) is { } photo
                        ? await AttachmentFiles.FromCameraAsync(translator["Camera_PhotoName"], photo) : null;
                    return Convert(picked);
                }
                if (choice != translator["Receipt_ChooseFile"]) return null;
            }
            return Convert(await AttachmentFiles.PickAsync(translator["Attachment_Pick"], forRecognition: true));
        }
        catch (InvalidDataException exception) { throw new ReceiptPreparationException("Attachment_PhotoFailed", exception); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PermissionException)
        { throw new ReceiptPreparationException("Attachment_Failed", exception); }
        static ReceiptPickedFile? Convert(PreparedAttachment? file) => file is null ? null : new(file.Name, file.ContentType, file.Data, file.RecognitionData);
    }
    /// <inheritdoc />
    public Task RunWhenUnlockedAsync(Func<Task> action) => appLock.RunWhenUnlockedAsync(action);
    /// <inheritdoc />
    public Task<TextLayoutResult> ReadAsync(byte[] data, string contentType) => DocumentReader.ReadDocumentAsync(data, contentType);
    /// <inheritdoc />
    public Task AlertAsync(string title, string message, string cancel) => interaction.AlertAsync(title, message, cancel);
    /// <inheritdoc />
    public Task OpenEditorAsync(ReceiptDraft draft)
    {
        var query = new Dictionary<string, object> { ["receiptVisit"] = draft };
        if (draft.Review.EntryId is { } id) query["id"] = id;
        else query["kind"] = nameof(EntryKind.Expense);
        return interaction.NavigateAsync(AppShell.EntryEditorRoute, query);
    }
}
