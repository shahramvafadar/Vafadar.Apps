using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads an original split/group and its choices/dependents in one actual-file SQLite snapshot.</summary>
    public async Task<SplitReadReview> ReadSplitReviewAsync(Guid entryId, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var review = new SplitReadReview(entryId, path, access, await SplitReadReview.ReadAsync(db, entryId, ct));
        review.EnsureCurrent(); return review;
    }

    /// <summary>Splits or reallocates only the unchanged original group under the existing atomic financial writer.</summary>
    public Task<SaveResult> SaveReviewedSplitAsync(SplitReadReview review, IReadOnlyList<(Guid? CategoryId, long Amount)> shares,
        CancellationToken ct = default, Func<bool>? isCurrent = null) => SaveSplitReviewAsync(review, shares, false, ct, isCurrent);

    /// <summary>Combines only an unchanged original split, preserving its selected first part and attachment ownership.</summary>
    public Task<SaveResult> JoinReviewedSplitAsync(SplitReadReview review, CancellationToken ct = default,
        Func<bool>? isCurrent = null) => SaveSplitReviewAsync(review, [], true, ct, isCurrent);

    private async Task<SaveResult> SaveSplitReviewAsync(SplitReadReview review, IReadOnlyList<(Guid? CategoryId, long Amount)> shares,
        bool join, CancellationToken ct, Func<bool>? isCurrent)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(shares);
        var acceptedShares = shares.ToArray(); review.BeginWrite(); var committed = false;
        void ValidateCurrent()
        {
            review.EnsureCurrent();
            if (isCurrent?.Invoke() == false || !acceptedShares.SequenceEqual(shares))
                throw new SplitReviewChangedException("The original split draft changed before commit.");
        }
        try
        {
            ValidateCurrent();
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
            write.DemandFeature(CommercialFeature.Corrections);
            var current = await SplitReadReview.ReadAsync(db, review.EntryId, ct); review.ValidateWriter(db, current);
            if (!EntryActions.CanSplit(current.Parts) || EntryActions.HasPaybacks(current.Parts, current.Paybacks))
                throw new SplitReviewChangedException("The original entry cannot be split or joined.");
            // D-407: EntryActions mutates its inputs. Never mutate the retained reviewed sources on failure.
            var parts = current.Parts.OrderBy(e => e.Id == review.EntryId ? 0 : 1).ThenBy(e => e.CreatedAt).ThenBy(e => e.Id).Select(e => e.Copy()).ToList();
            IReadOnlyList<LedgerEntry> save; IReadOnlyList<Guid> delete;
            if (join) { var result = EntryActions.Join(parts); save = [result.Save]; delete = result.Delete; }
            else (save, delete) = EntryActions.Split(parts, acceptedShares);
            var saved = await SaveEntriesUnderWriterAsync(db, write, save, delete, save[0].Id, new HashSet<Guid>(), ct,
                committedPublication: true, validateReview: ValidateCurrent);
            committed = saved.Succeeded; return saved;
        }
        catch (CommittedWriteNotificationException<SaveResult>) { committed = true; throw; }
        finally { review.EndWrite(committed); }
    }
}
