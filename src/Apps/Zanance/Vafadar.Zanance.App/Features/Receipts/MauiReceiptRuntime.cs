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
            using var origin = CaptureOrigin();
            return await ReceiptSourceChooser.RunAsync<ReceiptPickedFile?>(translator, interaction, PermissionPrompts.CanTakePhoto,
#if ANDROID
                showLatinImageHelp: true,
#else
                showLatinImageHelp: false,
#endif
                origin, appLock.RunWhenUnlockedAsync, appLock.RetainCurrentDestinationIfLocked,
                async () => Convert(await PermissionPrompts.TakePhotoAsync(translator) is { } photo
                    ? await AttachmentFiles.FromCameraAsync(translator["Camera_PhotoName"], photo) : null),
                async () => Convert(await AttachmentFiles.PickAsync(translator["Attachment_Pick"], forRecognition: true)), null);
        }
        catch (InvalidDataException exception) { throw new ReceiptPreparationException("Attachment_PhotoFailed", exception); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PermissionException)
        { throw new ReceiptPreparationException("Attachment_Failed", exception); }
        static ReceiptPickedFile? Convert(PreparedAttachment? file) => file is null ? null : new(file.Name, file.ContentType, file.Data, file.RecognitionData);
    }
    /// <inheritdoc />
    public Task RunWhenUnlockedAsync(Func<Task> action) => appLock.RunWhenUnlockedAsync(action);
    /// <inheritdoc />
    public async Task<bool> QueueWhenUnlockedAsync(Func<Task> action)
    {
        await appLock.RunWhenUnlockedAsync(action);
        return true; // The original gate owns both immediate and deferred work; it does not decline a queued callback.
    }
    /// <inheritdoc />
    public bool RetainPublicationIfLocked(Func<Task> continuation) => appLock.RetainCurrentDestinationIfLocked(continuation);
    /// <inheritdoc />
    public ReceiptReadOrigin CaptureOrigin()
    {
        var app = Application.Current;
        if (app?.Windows.FirstOrDefault() is not { Page: AppShell root } window || root.CurrentPage is not { } page)
            return new(() => false);
        var model = page.BindingContext;
        var section = root.CurrentItem?.CurrentItem;
        var route = section?.Navigation.NavigationStack.ToArray() ?? [];
        var modals = root.Navigation.ModalStack.ToArray();
        var retired = new CancellationTokenSource();
        bool Current()
        {
            var currentModals = root.Navigation.ModalStack;
            return ReferenceEquals(app.Windows.FirstOrDefault(), window) && ReferenceEquals(window.Page, root)
                && ReferenceEquals(root.CurrentItem?.CurrentItem, section) && ReferenceEquals(page.BindingContext, model)
                && (section?.Navigation.NavigationStack ?? []).SequenceEqual(route)
                && currentModals.Count >= modals.Length && currentModals.Take(modals.Length).SequenceEqual(modals)
                && currentModals.Skip(modals.Length).All(modal => modal is LockPage)
                // Shell.CurrentPage can expose the temporary modal cover instead of its original page.
                && (ReferenceEquals(root.CurrentPage, page) || root.CurrentPage is LockPage);
        }
        void Check()
        {
            if (!Current() && !retired.IsCancellationRequested) retired.Cancel();
        }
        void Navigated(object? sender, ShellNavigatedEventArgs args) => Check();
        void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName is nameof(Window.Page) or nameof(Page.BindingContext)) Check();
        }
        void ModalPushed(object? sender, ModalPushedEventArgs args) => Check();
        void Destroying(object? sender, EventArgs args) { if (!retired.IsCancellationRequested) retired.Cancel(); }
        root.Navigated += Navigated; window.PropertyChanged += Changed; page.PropertyChanged += Changed;
        app.ModalPushed += ModalPushed; window.Destroying += Destroying;
        // A file picker and the original lock cover keep the same page; successful tab/push/root retirement does not.
        return new(Current, retired.Token, () =>
        {
            root.Navigated -= Navigated; window.PropertyChanged -= Changed; page.PropertyChanged -= Changed;
            app.ModalPushed -= ModalPushed; window.Destroying -= Destroying; retired.Dispose();
        });
    }
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
