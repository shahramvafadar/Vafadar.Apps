using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Saves an editor batch in its original file and captures its attachment destination before commit publication.</summary>
    public async Task<SaveResult> SaveEditorEntriesAsync(QuickResourceReview original,
        IReadOnlyList<LedgerEntry> entries, IReadOnlyCollection<Guid> deleteIds, Guid attachmentEntryId,
        Func<bool> isCurrent, Action<EntryAttachmentReview?> accepted, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original); ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(deleteIds); ArgumentNullException.ThrowIfNull(isCurrent);
        ArgumentNullException.ThrowIfNull(accepted);
        var batch = entries.Select(e => e.Copy()).ToArray(); var removed = deleteIds.ToArray();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        void Current()
        {
            original.TemplateChoice.Access.EnsureSnapshotCurrent();
            original.FilterChoice.Access.EnsureSnapshotCurrent();
            if (!isCurrent() || original.TemplateChoice.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
                throw new InvalidOperationException("The original entry editor file or owner changed.");
        }
        // Refuse another configured profile before reading any same-id financial or document rows.
        Current();
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
        write.DemandFeature(CommercialFeature.Corrections);
        await ValidateQuickResourceReviewAsync(original, db, ct);
        EntryAttachmentReview? destination = null;
        return await SaveEntriesUnderWriterAsync(db, write, batch, removed, null, new HashSet<Guid>(), ct,
            committedPublication: true, validateReview: Current, prepareCommit: async () =>
            {
                // A replaced aggregate may legitimately disappear. Its document cannot be guessed onto another row.
                if (await db.Entries.AsNoTracking().AnyAsync(e => e.Id == attachmentEntryId, ct))
                    destination = new EntryAttachmentReview(DatabaseFileIdentity.ConfiguredPath(db),
                        new CommercialFileAccess(commercialAccess, DatabaseFileIdentity.ConfiguredPath(db)),
                        await EntryAttachmentReview.ReadAsync(db, attachmentEntryId, ct));
            }, accepted: () => accepted(destination));
    }
}
