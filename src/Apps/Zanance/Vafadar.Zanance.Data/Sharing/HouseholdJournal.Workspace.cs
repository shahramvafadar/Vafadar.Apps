using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>One protected local household presentation snapshot; it grants no reusable key or write authority.</summary>
public sealed record HouseholdLocalWorkspaceSnapshot(PersonalSyncReview Connection, PersonalSyncStatus Status,
    HouseholdJournalState? State, HouseholdAccessSnapshot? Access, HouseholdMembershipStatement? Membership,
    bool PendingAdministration, HouseholdInvitationNotice? PendingInvitation, HouseholdOwnershipObservation? Ownership,
    HouseholdOwnershipOffer? OwnershipOffer, HouseholdDeletionIntent? Deletion, HouseholdProjectionReceipt? SourceReceipt,
    bool OnboardingCompleted);

public sealed partial class HouseholdJournal
{
    /// <summary>Reads the original local workspace on a worker under one captured file and deferred source transaction.</summary>
    public async Task<HouseholdLocalWorkspaceSnapshot> ReadWorkspaceAsync(PersonalSyncStore originalStore, LocalSyncPeer? peer, string? actualIdentity,
        CancellationToken ct = default)
    {
        // Capture before dispatch: a later profile switch cannot select a different factory context.
        await using var db = await factory.CreateDbContextAsync(ct);
        if (db.Database.GetDbConnection() is not SqliteConnection connection) throw Refusal();
        var sourceAccess = originalStore.CaptureWorkspaceAccess(db);
        // The original pre-sign-in page reads only connection/status metadata. Private
        // household material retains the original protected-factory requirement.
        if (actualIdentity is not null) ProtectedSqliteAccess.Require(connection);
        var path = Path.GetFullPath(DatabaseFileIdentity.ConfiguredPath(db));
        return await Task.Run(async () =>
        {
            ct.ThrowIfCancellationRequested(); await db.Database.OpenConnectionAsync(ct);
            await using var transaction = connection.BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct) ?? throw Refusal();
            var original = await PersonalSyncStore.ReadConnectionInContextAsync(db, ct);
            var status = await PersonalSyncStore.ReadStatusInContextAsync(db, ct);
            var onboardingCompleted = await db.Settings.OrderBy(row => row.CreatedAt).Select(row => (bool?)row.OnboardingCompleted).FirstOrDefaultAsync(ct) ?? false;
            if (actualIdentity is null)
            {
                sourceAccess.EnsureSnapshotCurrent(); ct.ThrowIfCancellationRequested();
                return new HouseholdLocalWorkspaceSnapshot(original, status, null, null, null, false, null, null, null, null, null, onboardingCompleted);
            }
            await using var context = new JournalContext(db, joined, path, connection, ownsDb: false);
            var current = await CaptureInContextAsync(context, peer ?? throw Refusal(), actualIdentity, ct);
            HouseholdAccessSnapshot? access = null;
            HouseholdMembershipStatement? membership = null;
            HouseholdInvitationNotice? invitation = null;
            HouseholdOwnershipObservation? ownership = null;
            HouseholdOwnershipOffer? offer = null;
            HouseholdDeletionIntent? deletion = null;
            HouseholdProjectionReceipt? sourceReceipt = null;
            var pending = false;
            if (current is not null)
            {
                var authority = current.Authority;
                access = await ReadAccessInContextAsync(context, authority, ct);
                if (current.MembershipVersion > 0)
                    membership = (await ReadCurrentMembershipInContextAsync(context, authority, ct)).Snapshot();
                pending = await ReadMembershipProposalInContextAsync(context, authority, ct) is not null;
                if (access.Role == HouseholdRole.Owner)
                    invitation = await ReadInvitationNoticeInContextAsync(context, authority, ct);
                ownership = await ReadOwnershipInContextAsync(context, authority, ct);
                if (ownership is not null)
                {
                    var anchor = await ReadMembershipVersionInContextAsync(context, authority, ownership.AnchorVersion, ct);
                    offer = StandardJoseSignatures.VerifyJson(ownership.SignedOffer!, anchor.Root,
                        HouseholdOwnershipProtocol.OfferType, 8192, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer);
                }
                var binding = await RequireBindingAsync(context, authority, ct);
                deletion = await ReadDeletionCoreAsync(db, binding, ct);
                sourceReceipt = await ReadProjectionReceiptInContextAsync(context, authority, ct);
            }
            context.Check(); sourceAccess.EnsureSnapshotCurrent(); ct.ThrowIfCancellationRequested();
            return new HouseholdLocalWorkspaceSnapshot(original, status, current, access, membership,
                pending, invitation, ownership, offer, deletion, sourceReceipt, onboardingCompleted);
        }, ct);
    }
}
