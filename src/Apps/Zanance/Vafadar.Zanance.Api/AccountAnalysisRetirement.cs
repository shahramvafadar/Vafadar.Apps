using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api;

/// <summary>Result of local analysis retirement without provider traffic.</summary>
public enum AccountAnalysisRetirementResult
{
    UnknownReceipt,
    Pending,
    Retired,
}

/// <summary>Retires local analysis content for the account identified by the original receipt.</summary>
public static class AccountAnalysisRetirement
{
    /// <summary>Retires eligible content and preserves the original credit journal and provider receipts.</summary>
    public static async Task<AccountAnalysisRetirementResult> RunAsync(ServiceDbContext db, Guid receipt, TimeProvider time,
        CancellationToken ct, bool releaseUnclaimedReservations = true)
    {
        if (receipt == Guid.Empty) throw new ArgumentException("The original deletion receipt is required.", nameof(receipt));
        var owner = await db.AccountDeletionRequests.AsNoTracking().Where(row => row.ReceiptId == receipt)
            .Select(row => row.UserId).SingleOrDefaultAsync(ct);
        if (owner is null) return AccountAnalysisRetirementResult.UnknownReceipt;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await SyncRequestScope.LockOwnerAsync(db, owner, ct);
        if (!await db.AccountDeletionRequests.AnyAsync(row => row.UserId == owner && row.ReceiptId == receipt, ct))
            return AccountAnalysisRetirementResult.UnknownReceipt;
        var requests = db.Set<AiRequestRow>();
        var credits = db.Set<AiCreditRow>();
        var offers = db.Set<AiOfferRow>();
        var deletions = db.Set<AiPilotDeletionRow>();
        var lookups = db.Set<AiPilotStatusCheckRow>();
        var unclaimed = releaseUnclaimedReservations
            ? await requests.Where(row => row.UserId == owner && row.State == AiRequestState.Pending &&
                row.PilotDispatchStage == AiPilotDispatchStage.Queued && row.PilotDispatchClaimedAt == null &&
                row.VendorOperationId == null && row.PilotVendorStatus == null && row.PilotCreateRefusal == null &&
                row.PilotMeteredCostMicros == null && row.PilotCostCeilingMicros != null &&
                row.StatusCheckedAt == null && row.ChargedUnits == 0 &&
                offers.Any(offer => offer.Id == row.OfferId && offer.UserId == owner && offer.PilotRouteId != null) &&
                !deletions.Any(effect => effect.RequestId == row.Id) && !lookups.Any(check => check.RequestId == row.Id))
                .ToListAsync(ct)
            : [];
        foreach (var row in unclaimed)
        {
            var journal = await credits.AsNoTracking().Where(eventRow => eventRow.RequestId == row.Id).Take(2).ToListAsync(ct);
            if (journal.Count != 1 || journal[0] is not { Kind: AiCreditEvent.Reserve } original ||
                original.UserId != owner || original.PurchasedReserved != row.PurchasedReserved ||
                original.GiftedReserved != row.GiftedReserved || original.PurchasedAvailable != -row.PurchasedReserved ||
                original.GiftedAvailable != -row.GiftedReserved)
                continue;
            row.State = AiRequestState.Refunded;
            AiRequestStore.RecordRefund(db, row, row.PurchasedReserved, row.GiftedReserved, time.GetUtcNow());
        }
        await db.SaveChangesAsync(ct);
        // Provider confirmation is bound to this exact completed request. Keep its receipt,
        // usage and credit records while removing local copies of the disclosed content.
        // Empty text retains the original database invariant for a completed request.
        await requests.Where(row => row.UserId == owner && row.State == AiRequestState.Completed &&
                (row.ProtectedPayload != "" || row.ProtectedText != "") &&
                deletions.Any(effect => effect.RequestId == row.Id && effect.ConfirmedAt >= effect.StartedAt))
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.ProtectedPayload, "")
                .SetProperty(row => row.ProtectedText, ""), ct);
        await requests.Where(row => row.UserId == owner && row.State == AiRequestState.NotAccepted &&
                row.PurchasedReserved == 0 && row.GiftedReserved == 0 && row.ChargedUnits == 0 &&
                row.VendorOperationId == null && row.PilotDispatchStage == null && row.PilotDispatchClaimedAt == null &&
                row.PilotVendorStatus == null && row.StatusCheckedAt == null &&
                !credits.Any(credit => credit.RequestId == row.Id) && !deletions.Any(effect => effect.RequestId == row.Id) &&
                !lookups.Any(check => check.RequestId == row.Id))
            .ExecuteDeleteAsync(ct);
        await offers.Where(row => row.UserId == owner && !requests.Any(request => request.OfferId == row.Id))
            .ExecuteDeleteAsync(ct);
        var remaining = await AccountDeletionInventory.HasAnalysisAsync(db, owner, ct);
        await transaction.CommitAsync(ct);
        return remaining ? AccountAnalysisRetirementResult.Pending : AccountAnalysisRetirementResult.Retired;
    }
}
