using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Owns one editor's financial acceptance and optional document handoff, independently of later navigation.</summary>
public sealed class EntryEditorSaveSession(ZananceStore store, QuickResourceReview original)
{
    private bool _busy;
    /// <summary>Gets known financial acceptance; neither notification nor attachment failure can rearm Save.</summary>
    public bool Accepted { get; private set; }
    /// <summary>Gets whether the original document could not be attached after the financial commit.</summary>
    public bool AttachmentFailed { get; private set; }
    /// <summary>Gets a postcommit notification failure without classifying the write as uncommitted.</summary>
    public bool NotificationFailed { get; private set; }

    /// <summary>Submits detached original inputs once and keeps a document failure separate from the saved money.</summary>
    public async Task<SaveResult> SaveAsync(IReadOnlyList<LedgerEntry> entries, IReadOnlyCollection<Guid> deleteIds,
        Guid entryId, EntryAttachment? attachment, Func<bool> isCurrent)
    {
        if (_busy || Accepted) throw new InvalidOperationException("The original editor save is pending or already accepted.");
        _busy = true; AttachmentFailed = NotificationFailed = false;
        var document = attachment?.Copy(); EntryAttachmentReview? destination = null;
        try
        {
            SaveResult result;
            try
            {
                result = await store.SaveEditorEntriesAsync(original, entries, deleteIds, entryId, isCurrent,
                    review => { destination = review; Accepted = true; });
            }
            catch (CommittedWriteNotificationException<SaveResult> exception) when (Accepted)
            { result = exception.Result; NotificationFailed = true; }
            if (!Accepted || document is null) return result;
            try
            {
                if (destination is null || !isCurrent()) throw new InvalidOperationException("The original document destination retired.");
                document.EntryId = entryId;
                await store.AddReviewedAttachmentAsync(destination, document, isCurrent);
            }
            catch (CommittedWriteNotificationException) { NotificationFailed = true; }
            catch (Exception exception) when (exception is not OutOfMemoryException) { AttachmentFailed = true; }
            return result;
        }
        finally { _busy = false; }
    }
}
