using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Explicit permanent remote deletion and content-free deadlines, independent of local profile deletion or purchased access.</summary>
internal sealed class HouseholdLifecycleStore(ServiceDbContext db)
{
    /// <summary>Only the actual current owner/device can review opaque privacy checkpoints during retention; no financial data, grants or membership authority is returned.</summary>
    internal async Task<HouseholdDeletionCheckpoint> CheckpointAsync(HouseholdRequestScope scope, Guid device, CancellationToken ct)
    {
        _ = await scope.RequireAsync(device, HouseholdRemoteOperation.DeleteSpace, ct);
        if (scope.Space.OwnerId != scope.ActorId) throw new HouseholdProtocolRefusal(404, "HouseholdDeletionUnavailable");
        var fingerprint = HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey)).Fingerprint;
        return new(scope.Space.Id, device, scope.ActorId, fingerprint, scope.Space.MembershipVersion, scope.Space.MembershipHead,
            scope.Space.Cursor, scope.Space.PublicationHead, scope.Now.ToUnixTimeSeconds());
    }

    /// <summary>Observes original acceptance or proves that the literal old request can no longer win this serialized room; it never cancels an accepted deletion.</summary>
    internal async Task<HouseholdDeletionReview> ReviewAsync(HouseholdRequestScope scope, HouseholdDeleteRequest input, CancellationToken ct)
    {
        if (input.SignedDeclaration is not { Length: > 0 and <= HouseholdLifecycleProtocol.DeclarationBytes * 2 } || input.DeviceId == Guid.Empty)
            throw new HouseholdProtocolRefusal(400, "InvalidHouseholdDeletion");
        var hash = HouseholdMetadataProofs.Digest(input.SignedDeclaration, HouseholdLifecycleProtocol.DeclarationBytes * 2);
        var marker = await db.HouseholdDeletions.ReadAsync(scope.Space.Id, ct);
        if (marker is not null)
        {
            if (marker.ActorHash != HouseholdDeletionLedger.Actor(scope.ActorId) || marker.RequestHash != hash || marker.Reason != HouseholdDeletionReason.Owner)
                throw new HouseholdProtocolRefusal(409, "HouseholdOriginalDeletionRequired");
            return new(scope.Space.Id, marker.OperationId, hash, false, Receipt(scope.Space, marker));
        }
        if (scope.Space.OwnerId != scope.ActorId || scope.Space.Deleted) throw new HouseholdProtocolRefusal(404, "HouseholdDeletionUnavailable");
        var value = HouseholdMetadataProofs.Read(input.SignedDeclaration, HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey)),
            HouseholdLifecycleProtocol.DeleteType, HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement);
        if (value.Format != 1 || value.SpaceId != scope.Space.Id || value.OperationId == Guid.Empty || value.OwnerId != scope.ActorId || value.DeviceId != input.DeviceId)
            throw new HouseholdProtocolRefusal(400, "InvalidHouseholdDeletion");
        // A checkpoint mismatch alone is not terminal across restoration of an older
        // database snapshot. Only expiry under the actual service UTC clock, after this
        // locked check found no permanent acceptance, permits explicit local withdrawal.
        var rejected = value.ValidUntilUnixSeconds <= scope.Now.ToUnixTimeSeconds();
        return new(scope.Space.Id, value.OperationId, hash, rejected, null);
    }

    /// <summary>Records original authorized privacy intent before database effects; its private immutable marker survives a database rollback or old snapshot.</summary>
    internal async Task<HouseholdDeletionReceipt> DeleteAsync(HouseholdRequestScope scope, HouseholdDeleteRequest input, CancellationToken ct)
    {
        if (input.SignedDeclaration is not { Length: > 0 and <= HouseholdLifecycleProtocol.DeclarationBytes * 2 } || input.DeviceId == Guid.Empty)
            throw new HouseholdProtocolRefusal(400, "InvalidHouseholdDeletion");
        var requestHash = HouseholdMetadataProofs.Digest(input.SignedDeclaration, HouseholdLifecycleProtocol.DeclarationBytes * 2);
        var existing = await db.HouseholdDeletions.ReadAsync(scope.Space.Id, ct);
        if (existing is not null)
        {
            if (existing.ActorHash != HouseholdDeletionLedger.Actor(scope.ActorId) || existing.RequestHash != requestHash || existing.Reason != HouseholdDeletionReason.Owner)
                throw new HouseholdProtocolRefusal(409, "HouseholdOriginalDeletionRequired");
            Apply(scope.Space, existing); return Receipt(scope.Space, existing);
        }
        _ = await scope.RequireAsync(input.DeviceId, HouseholdRemoteOperation.DeleteSpace, ct);
        if (scope.Space.OwnerId != scope.ActorId || scope.Space.Deleted) throw new HouseholdProtocolRefusal(404, "HouseholdDeletionUnavailable");
        var declaration = HouseholdMetadataProofs.Read(input.SignedDeclaration, HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey)),
            HouseholdLifecycleProtocol.DeleteType, HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement);
        if (declaration.Format != 1 || declaration.SpaceId != scope.Space.Id || declaration.OperationId == Guid.Empty || declaration.OwnerId != scope.ActorId || declaration.DeviceId != input.DeviceId ||
            declaration.MembershipVersion != scope.Space.MembershipVersion || declaration.MembershipHead != scope.Space.MembershipHead ||
            declaration.PublicationCursor != scope.Space.Cursor || declaration.PublicationHead != scope.Space.PublicationHead ||
            declaration.ValidUntilUnixSeconds <= scope.Now.ToUnixTimeSeconds() || declaration.ValidUntilUnixSeconds > scope.Now.Add(HouseholdLimits.MembershipProposalLifetime).ToUnixTimeSeconds())
            throw new HouseholdProtocolRefusal(409, "HouseholdDeletionReviewChanged");
        var marker = await db.HouseholdDeletions.RecordAsync(new(1, scope.Space.Id, declaration.OperationId, HouseholdDeletionLedger.Actor(scope.ActorId), requestHash,
            scope.Now.ToUnixTimeSeconds(), HouseholdDeletionReason.Owner), ct);
        Apply(scope.Space, marker); return Receipt(scope.Space, marker);
    }

    /// <summary>Only the actual originally declaring owning identity can observe its minimal original receipt after device/member data is retired.</summary>
    internal async Task<HouseholdDeletionReceipt> ObserveAsync(HouseholdRequestScope scope, Guid operation, CancellationToken ct)
    {
        var marker = await db.HouseholdDeletions.ReadAsync(scope.Space.Id, ct);
        if (marker is null || marker.OperationId != operation || marker.ActorHash != HouseholdDeletionLedger.Actor(scope.ActorId))
            throw new HouseholdProtocolRefusal(404, "HouseholdDeletionUnavailable");
        Apply(scope.Space, marker); return Receipt(scope.Space, marker);
    }

    /// <summary>Reports only this approved member's operational deadlines, including retained time without financial/key access.</summary>
    internal async Task<HouseholdLifecycleStatus> StatusAsync(HouseholdRequestScope scope, Guid device, CancellationToken ct)
    {
        var marker = await db.HouseholdDeletions.ReadAsync(scope.Space.Id, ct);
        if (marker is null || marker.ActorHash != HouseholdDeletionLedger.Actor(scope.ActorId))
            _ = await scope.RequireAsync(device, HouseholdRemoteOperation.ReadLifecycle, ct);
        var expires = scope.Space.HostExpiresAt;
        static long? Deadline(DateTimeOffset? value, TimeSpan interval) => value is { } instant && instant <= DateTimeOffset.MaxValue - interval ? (instant + interval).ToUnixTimeSeconds() : null;
        return new(scope.Space.Id, scope.Retention, scope.Now.ToUnixTimeSeconds(), expires?.ToUnixTimeSeconds(), Deadline(expires, HouseholdAccessPolicy.ReadOnlyInterval),
            Deadline(expires, HouseholdAccessPolicy.TotalRetentionInterval), scope.Space.StorageRetired);
    }

    /// <summary>Replays only the exact permanent marker; later renewal, root changes or an older database snapshot cannot resurrect the room.</summary>
    internal static void Apply(HouseholdSpaceRow space, HouseholdDeletionMarker marker)
    {
        if (marker.SpaceId != space.Id || space.DeletionOperationId is { } original &&
            (original != marker.OperationId || space.DeletionRequestHash != marker.RequestHash || space.DeletionActorHash != marker.ActorHash))
            throw new IOException("Private deletion marker differs from the original room.");
        space.Deleted = true; space.RotationRequired = true; space.DeletionOperationId = marker.OperationId; space.DeletionRequestHash = marker.RequestHash;
        space.DeletionActorHash = marker.ActorHash; space.DeletionReason = marker.Reason; space.DeletedAt = DateTimeOffset.FromUnixTimeSeconds(marker.DeclaredAtUnixSeconds);
    }

    /// <summary>No signature body, device key, grant or financial row survives in this minimal owner receipt.</summary>
    private static HouseholdDeletionReceipt Receipt(HouseholdSpaceRow space, HouseholdDeletionMarker marker) =>
        new(space.Id, marker.OperationId, marker.RequestHash, marker.Reason, marker.DeclaredAtUnixSeconds, space.StorageRetired);
}
