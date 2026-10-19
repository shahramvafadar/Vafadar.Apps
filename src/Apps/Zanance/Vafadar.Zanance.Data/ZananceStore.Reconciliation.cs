using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads an original balance comparison within one actual-file SQLite snapshot.</summary>
    public async Task<ReconciliationReadReview> ReadReconciliationAsync(Guid accountId, long observed, DateOnly date, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var review = new ReconciliationReadReview(path, access, await ReconciliationReadReview.ReadAsync(db, accountId, ct), observed, date);
        review.EnsureCurrent(); return review;
    }

    /// <summary>Atomically saves only an unchanged original comparison and its reconciliation metadata.</summary>
    public async Task<SaveResult> SaveReconciliationAsync(ReconciliationReadReview review, string? reason, CancellationToken ct = default,
        Func<bool>? isCurrent = null)
    {
        ArgumentNullException.ThrowIfNull(review);
        if (review.Result.Difference != 0 && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A balance adjustment requires a reason.", nameof(reason));
        review.BeginWrite();
        var committed = false;
        // The application may retire consent during an await, but cannot substitute a different reviewed packet.
        void ValidateCurrent()
        {
            review.EnsureCurrent();
            if (isCurrent?.Invoke() == false) throw new InvalidOperationException("The balance comparison draft changed before commit.");
        }
        try
        {
            ValidateCurrent();
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
            write.DemandFeature(CommercialFeature.Corrections);
            var current = await ReconciliationReadReview.ReadAsync(db, review.AccountId, ct);
            review.ValidateWriter(db, current);
            // Track the current stored account, not the editable form's potentially stale copy.
            var account = await db.Accounts.SingleAsync(a => a.Id == review.AccountId, ct);
            account.OpeningBalanceKnown = true;
            if (account.LastReconciledOn is null || account.LastReconciledOn < review.Date) account.LastReconciledOn = review.Date;
            var adjustment = Reconciliation.CreateAdjustment(current.Account, review.Result, review.Date, reason ?? string.Empty);
            SaveResult result;
            if (adjustment is not null)
                result = await SaveEntriesUnderWriterAsync(db, write, [adjustment], [], null, new HashSet<Guid>(), ct,
                    committedPublication: true, validateReview: ValidateCurrent);
            else
            {
                ValidateCurrent(); write.EnsureCurrent();
                await db.SaveChangesAsync(ct);
                ValidateCurrent();
                await write.CommitAsync(ct);
                result = CommittedWriteNotification.Publish(OnChanged, SaveResult.Success);
            }
            committed = result.Succeeded;
            return result;
        }
        catch (CommittedWriteNotificationException<SaveResult>) { committed = true; throw; }
        finally { review.EndWrite(committed); }
    }
}
