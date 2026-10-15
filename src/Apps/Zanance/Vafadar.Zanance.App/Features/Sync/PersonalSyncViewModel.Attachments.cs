using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sync;

public sealed partial class PersonalSyncViewModel
{
    private PersonalSyncConflictReview? previewReview;
    private bool previewLocal;
    private int previewIndex;
    private string previewName = "";
    private double previewViewportWidth = 360;
    private double previewZoom = 1;

    /// <summary>Gets the one currently displayed bounded native image; never the original file bytes.</summary>
    [ObservableProperty] public partial PersonalSyncRenderedPage? AttachmentPreview { get; set; }
    /// <summary>Gets whether the original inline preview has a current image.</summary>
    public bool HasAttachmentPreview => AttachmentPreview is not null;
    /// <summary>Gets the reviewed file name and version rather than a local path.</summary>
    public string AttachmentPreviewTitle => previewName + " · " + translator[previewLocal ? "Sync_ReviewSubmitted" : "Sync_Remote"];
    /// <summary>Gets the actual page/count with the configured display digits.</summary>
    public string AttachmentPreviewPosition => AttachmentPreview is { } frame
        ? NativeDigits.Apply(translator.Format("Sync_AttachmentPage", previewIndex + 1, frame.PageCount))! : "";
    /// <summary>Gets whether a preceding page exists in the same immutable file.</summary>
    public bool CanPreviewPrevious => CanInteract && AttachmentPreview is not null && previewIndex > 0;
    /// <summary>Gets whether a following page exists in the same immutable file.</summary>
    public bool CanPreviewNext => CanInteract && AttachmentPreview is { } frame && previewIndex + 1 < frame.PageCount;
    /// <summary>Gets the bounded displayed width, preserving the real image aspect ratio while zooming.</summary>
    public double AttachmentImageWidth => AttachmentPreview is { } frame
        ? Math.Min(previewViewportWidth * previewZoom, 16384d * frame.Width / frame.Height) : 1;
    /// <summary>Gets the displayed height from the real bounded image dimensions.</summary>
    public double AttachmentImageHeight => AttachmentPreview is { } frame ? AttachmentImageWidth * frame.Height / frame.Width : 1;
    /// <summary>Gets whether the current page can be enlarged for reading.</summary>
    public bool CanPreviewZoomIn => CanInteract && HasAttachmentPreview && previewZoom < 4;
    /// <summary>Gets whether the current page can be reduced towards its viewport width.</summary>
    public bool CanPreviewZoomOut => CanInteract && HasAttachmentPreview && previewZoom > 1;
    /// <summary>Accepts only the actual inline viewport measurement, never a device setting.</summary>
    public void SetAttachmentViewport(double width)
    {
        if (!double.IsFinite(width) || width <= 0) return;
        previewViewportWidth = Math.Min(width, 4096); RefreshAttachmentProperties();
    }

    [RelayCommand] private Task PreviewLocalAsync(PersonalSyncConflictReview selected) => ShowAttachmentAsync(selected, true, 0);
    [RelayCommand] private Task PreviewRemoteAsync(PersonalSyncConflictReview selected) => ShowAttachmentAsync(selected, false, 0);
    [RelayCommand] private Task PreviewPreviousAsync() => CanPreviewPrevious && previewReview is { } selected
        ? ShowAttachmentAsync(selected, previewLocal, previewIndex - 1) : Task.CompletedTask;
    [RelayCommand] private Task PreviewNextAsync() => CanPreviewNext && previewReview is { } selected
        ? ShowAttachmentAsync(selected, previewLocal, previewIndex + 1) : Task.CompletedTask;
    [RelayCommand] private void CloseAttachmentPreview() => ClearAttachmentPreview();
    [RelayCommand] private void PreviewZoomIn() { if (CanPreviewZoomIn) { previewZoom = Math.Min(4, previewZoom + 0.5); RefreshAttachmentProperties(); } }
    [RelayCommand] private void PreviewZoomOut() { if (CanPreviewZoomOut) { previewZoom = Math.Max(1, previewZoom - 0.5); RefreshAttachmentProperties(); } }

    private Task ShowAttachmentAsync(PersonalSyncConflictReview selected, bool local, int page) => RunAsync(async (access, ct) =>
    {
        var current = Conflicts.SingleOrDefault(item => AttachmentReviewKey(item) == AttachmentReviewKey(selected))
            ?? throw new InvalidOperationException("The attachment review was retired.");
        using var content = await store.ReadAttachmentReviewAsync(current, local, ct);
        var rendered = await environment.RenderAttachmentPageAsync(content, page, ct); var published = false;
        try
        {
            ct.ThrowIfCancellationRequested(); access.Validate();
            if (rendered.PageCount <= 0 || page < 0 || page >= rendered.PageCount || rendered.Data.Length == 0 || rendered.Width <= 0 || rendered.Height <= 0)
                throw new InvalidDataException("The native attachment page is invalid.");
            // Recheck the exact journal and original data after native rendering, before publishing a preview or consent.
            using var rechecked = await store.ReadAttachmentReviewAsync(current, local, ct);
            access.Validate();
            if (previewReview is { } oldReview && AttachmentReviewKey(oldReview) == AttachmentReviewKey(current) && previewLocal == local &&
                AttachmentPreview is { } oldFrame && oldFrame.PageCount != rendered.PageCount)
                throw new InvalidDataException("The immutable attachment page count changed.");
            previewReview = current; previewLocal = local; previewIndex = page; previewName = content.FileName; previewZoom = 1;
            var old = AttachmentPreview; AttachmentPreview = rendered; old?.Dispose(); published = true;
            RefreshAttachmentProperties(); RefreshReviewItems();
        }
        finally { if (!published) rendered.Dispose(); }
    });

    private static string AttachmentReviewKey(PersonalSyncConflictReview item) =>
        item.Connection.GenerationPath + "|" + item.Connection.RemoteProfile + "|" + item.Operation + "|" + item.Phase + "|" + item.Digest;

    private void ClearAttachmentPreview()
    {
        var old = AttachmentPreview; AttachmentPreview = null; previewReview = null; previewName = "";
        old?.Dispose(); RefreshAttachmentProperties();
    }

    private void RefreshAttachmentProperties()
    {
        foreach (var property in new[] { nameof(HasAttachmentPreview), nameof(AttachmentPreviewTitle), nameof(AttachmentPreviewPosition),
            nameof(CanPreviewPrevious), nameof(CanPreviewNext), nameof(AttachmentImageWidth), nameof(AttachmentImageHeight),
            nameof(CanPreviewZoomIn), nameof(CanPreviewZoomOut) }) OnPropertyChanged(property);
    }
}
