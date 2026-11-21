using System.Data;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api;

/// <summary>Service record groups requiring a policy or an erasure step for an original deletion request.</summary>
[Flags]
public enum AccountDeletionDependencies
{
    None = 0,
    Sessions = 1,
    IdentityMail = 2,
    PrivateObjects = 4,
    PersonalSync = 8,
    Household = 16,
    Billing = 32,
    Analysis = 64,
}

/// <summary>Read-only database inventory for an existing pending request. Values contain no account or object identifiers.</summary>
public static class AccountDeletionInventory
{
    /// <summary>Uses one database snapshot. A missing request returns null. The result is never proof of physical erasure.</summary>
    public static async Task<AccountDeletionDependencies?> InspectAsync(ServiceDbContext db, Guid receipt, CancellationToken ct)
    {
        if (receipt == Guid.Empty) throw new ArgumentException("The original deletion receipt is required.", nameof(receipt));
        await using var snapshot = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        await db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", ct);
        var owner = await db.AccountDeletionRequests.AsNoTracking().Where(row => row.ReceiptId == receipt)
            .Select(row => row.UserId).SingleOrDefaultAsync(ct);
        if (owner is null) return null;
        var dependencies = await ReadOwnerAsync(db, owner, receipt, ct);
        await snapshot.CommitAsync(ct);
        return dependencies;
    }

    internal static async Task<AccountDeletionDependencies> ReadOwnerAsync(ServiceDbContext db, string owner, Guid receipt, CancellationToken ct)
    {
        var dependencies = AccountDeletionDependencies.None;

        if (await db.Sessions.AnyAsync(row => row.UserId == owner, ct))
            dependencies |= AccountDeletionDependencies.Sessions;
        if (await db.Challenges.AnyAsync(row => row.UserId == owner, ct))
            dependencies |= AccountDeletionDependencies.IdentityMail;
        if (await db.Objects.AnyAsync(row => row.UserId == owner, ct))
            dependencies |= AccountDeletionDependencies.PrivateObjects;
        if (await db.Set<SyncSpaceRow>().AnyAsync(row => row.UserId == owner, ct) ||
            await db.Set<SyncDeviceRow>().AnyAsync(row => row.UserId == owner, ct) ||
            await db.Set<AccountSyncFileRetirement>().AnyAsync(row => row.ReceiptId == receipt, ct))
            dependencies |= AccountDeletionDependencies.PersonalSync;
        // Historical references can survive an ownership transfer and do not all have identity foreign keys.
        var deletionActorHash = HouseholdDeletionLedger.Actor(owner);
        if (await db.Set<HouseholdSpaceRow>().AnyAsync(row => row.OwnerId == owner || row.DeletionActorHash == deletionActorHash, ct) ||
            await db.Set<HouseholdMemberRow>().AnyAsync(row => row.UserId == owner, ct) ||
            await db.Set<HouseholdInvitationRow>().AnyAsync(row => row.ClaimedUserId == owner, ct) ||
            await db.Set<HouseholdMembershipEventRow>().AnyAsync(row => row.ActorId == owner, ct) ||
            await db.Set<HouseholdAuditRow>().AnyAsync(row => row.ActorId == owner, ct) ||
            await db.Set<HouseholdOwnershipRow>().AnyAsync(row => row.OwnerId == owner, ct) ||
            await db.Set<HouseholdRecoveryRow>().AnyAsync(row => row.OwnerId == owner, ct) ||
            await db.Set<HouseholdRecoverySeedRow>().AnyAsync(row => row.OwnerId == owner, ct))
            dependencies |= AccountDeletionDependencies.Household;
        var billingIdentityHash = BillingAuthority.IdentityHash(owner);
        if (await db.Set<BillingCustomerRow>().AnyAsync(row => row.UserId == owner || row.RegistrationIdentityHash == billingIdentityHash, ct) ||
            await db.Set<BillingIdentityRow>().AnyAsync(row => row.UserId == owner, ct) ||
            await db.Set<BillingPurchaseRow>().AnyAsync(row => row.UserId == owner, ct))
            dependencies |= AccountDeletionDependencies.Billing;
        if (await HasAnalysisAsync(db, owner, ct)) dependencies |= AccountDeletionDependencies.Analysis;
        return dependencies;
    }

    internal static async Task<bool> HasAnalysisAsync(ServiceDbContext db, string owner, CancellationToken ct) =>
            await db.Set<AiOfferRow>().AnyAsync(row => row.UserId == owner, ct) ||
            await db.Set<AiRequestRow>().AnyAsync(row => row.UserId == owner, ct) ||
            await db.Set<AiCreditRow>().AnyAsync(row => row.UserId == owner, ct) ||
            await db.Set<AiPilotDeletionRow>().AnyAsync(row => row.Actor == owner, ct) ||
            await db.Set<AiPilotRouteRow>().AnyAsync(row => row.Actor == owner, ct) ||
            await db.Set<AiPilotStatusCheckRow>().AnyAsync(row => row.Actor == owner, ct);
}
