using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api;

/// <summary>Result of deleting local analysis records with no provider or credit effects.</summary>
public enum AccountAnalysisRetirementResult
{
    UnknownReceipt,
    Pending,
    Retired,
}

/// <summary>Retires only local offers and refused requests for the account identified by the original receipt.</summary>
public static class AccountAnalysisRetirement
{
    /// <summary>Never sends vendor work or modifies credit journal rows. An unresolved request keeps erasure pending.</summary>
    public static async Task<AccountAnalysisRetirementResult> RunAsync(ServiceDbContext db, Guid receipt, CancellationToken ct)
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
        var deletions = db.Set<AiPilotDeletionRow>();
        var lookups = db.Set<AiPilotStatusCheckRow>();
        await requests.Where(row => row.UserId == owner && row.State == AiRequestState.NotAccepted &&
                row.PurchasedReserved == 0 && row.GiftedReserved == 0 && row.ChargedUnits == 0 &&
                row.VendorOperationId == null && row.PilotDispatchStage == null && row.PilotDispatchClaimedAt == null &&
                row.PilotVendorStatus == null && row.StatusCheckedAt == null &&
                !credits.Any(credit => credit.RequestId == row.Id) && !deletions.Any(effect => effect.RequestId == row.Id) &&
                !lookups.Any(check => check.RequestId == row.Id))
            .ExecuteDeleteAsync(ct);
        await db.Set<AiOfferRow>().Where(row => row.UserId == owner && !requests.Any(request => request.OfferId == row.Id))
            .ExecuteDeleteAsync(ct);
        var remaining = await AccountDeletionInventory.HasAnalysisAsync(db, owner, ct);
        await transaction.CommitAsync(ct);
        return remaining ? AccountAnalysisRetirementResult.Pending : AccountAnalysisRetirementResult.Retired;
    }
}
