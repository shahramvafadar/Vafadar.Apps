using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Content-free local financial-write refusal; a test-build commercial allowance cannot override it.</summary>
public enum HouseholdWriteBlockReason
{
    /// <summary>Current complete membership/device/key/host authority has not been verified.</summary>
    Unverified,
    /// <summary>The dedicated draft has not completed approved setup.</summary>
    Unpublished,
    /// <summary>The current signed role is view/export only.</summary>
    Viewer,
    /// <summary>The bound device/member or authenticated remote scope is no longer authorized.</summary>
    Revoked,
    /// <summary>The authenticated service refused this scope; do not guess whether the cause is retention, deletion or revocation.</summary>
    RemoteRefused,
    /// <summary>An explicit local pause has not been resumed.</summary>
    Paused,
    /// <summary>Host expiry/grace/retention does not permit financial mutations.</summary>
    Expired,
    /// <summary>The signed epoch/host rotation is not ready for new encrypted financial work.</summary>
    Rotation,
    /// <summary>The clock predates its actual recorded host observation beyond normal skew.</summary>
    Clock,
}

/// <summary>Membership/privacy write authority is independent of purchase limits and contains no financial value or credential.</summary>
public sealed class HouseholdWriteRejectedException(HouseholdWriteBlockReason reason) : InvalidOperationException("Household financial write is not currently authorized.")
{
    /// <summary>Stable reason for translated current-state UI; never a guessed replacement role.</summary>
    public HouseholdWriteBlockReason Reason { get; } = reason;
}

/// <summary>Display-only last verified local role/deadline state; every later writer still captures its own exact transaction authority.</summary>
public sealed record HouseholdAccessSnapshot(HouseholdRole? Role, bool CanWrite, HouseholdWriteBlockReason? BlockedBy,
    long? ObservedAtUnixSeconds, long? HostExpiresAtUnixSeconds, HouseholdRetentionState Retention);

/// <summary>Transaction-local verified authority. Public/private key handles are disposed before this object is returned.</summary>
internal sealed class HouseholdLocalWritePermission(ZananceDbContext db, TimeProvider clock, HouseholdWriteBlockReason? blocked,
    DateTimeOffset? observedAt = null, DateTimeOffset? expiresAt = null)
{
    private readonly Guid transaction = db.Database.CurrentTransaction?.TransactionId ?? throw new InvalidOperationException("Household write authority requires the actual writer transaction.");
    internal void Demand()
    {
        if (db.Database.CurrentTransaction?.TransactionId != transaction) throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
        if (db.Database.GetDbConnection() is not SqliteConnection connection) throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
        ProtectedSqliteAccess.Require(connection);
        if (blocked is { } reason) throw new HouseholdWriteRejectedException(reason);
        var now = clock.GetUtcNow();
        if (observedAt is { } observed && now < observed.AddMinutes(-5)) throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Clock);
        if (expiresAt is { } expires && now >= expires) throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Expired);
    }
}

public sealed partial class HouseholdJournal
{
    /// <summary>Reads a presentation hint without retaining a native connection, private key or reusable write permission.</summary>
    public async Task<HouseholdAccessSnapshot> ReadAccessAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        HouseholdRole? role = null;
        if (binding.MembershipVersion != 0)
        {
            using var device = await ReadIdentityAsync(context.Db, "device", ct);
            role = (await ReadMembershipAsync(context.Db, binding, device, ct)).Current.Snapshot().Members
                .SingleOrDefault(member => member.IdentityId == binding.IdentityId)?.Role;
        }
        var permission = await CaptureLocalWritePermissionAsync(context.Db, context.Db.WorkClock, ct) ?? throw Refusal();
        HouseholdWriteBlockReason? blocked = null;
        try { permission.Demand(); } catch (HouseholdWriteRejectedException error) { blocked = error.Reason; }
        context.Check(); return new(role, blocked is null, blocked, binding.AccessObservedAtUnixSeconds, binding.HostExpiresAtUnixSeconds, binding.AccessRetention);
    }
    /// <summary>Only the original pinned authenticated transport coordinator records host metadata after signed chain/grant checks; no paid fact is manufactured.</summary>
    internal async Task RecordAccessCheckpointAsync(HouseholdJournalAuthority authority, HouseholdCheckpoint checkpoint, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (checkpoint.SpaceId != binding.SpaceId || checkpoint.MembershipVersion != binding.MembershipVersion || checkpoint.MembershipHead != binding.MembershipHead ||
            checkpoint.KeyEpoch != membership.Current.Epoch || checkpoint.OwnerId != membership.Current.Snapshot().OwnerId ||
            checkpoint.ObservedAtUnixSeconds <= 0 || checkpoint.ObservedAtUnixSeconds < (binding.AccessObservedAtUnixSeconds ?? 0) || !Enum.IsDefined(checkpoint.Retention)) throw Refusal();
        var observed = FromUnix(checkpoint.ObservedAtUnixSeconds); var expires = checkpoint.HostExpiresAtUnixSeconds is { } value ? FromUnix(value) : (DateTimeOffset?)null;
        if (checkpoint.Retention != HouseholdAccessPolicy.Retention(observed, expires, checkpoint.Retention == HouseholdRetentionState.Deleted)) throw Refusal();
        binding.AccessObservedAtUnixSeconds = checkpoint.ObservedAtUnixSeconds; binding.HostExpiresAtUnixSeconds = checkpoint.HostExpiresAtUnixSeconds;
        binding.AccessRetention = checkpoint.Retention; binding.RotationRequired = checkpoint.RotationRequired; binding.RemoteAccessRefused = false;
        await context.CommitAsync(ct);
    }

    /// <summary>An actual authenticated scoped refusal stops local mutation without discarding pending ciphertext, finance or user pause state.</summary>
    internal async Task RecordRemoteAccessRefusalAsync(HouseholdJournalAuthority authority, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        binding.RemoteAccessRefused = true; await context.CommitAsync(ct);
    }

    /// <summary>Reads exact local signed authority under the caller's existing SQLite writer; null means no household at all, never unverified household permission.</summary>
    internal static async Task<HouseholdLocalWritePermission?> CaptureLocalWritePermissionAsync(ZananceDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Capture household authority inside the actual financial writer.");
        var binding = await db.Set<HouseholdLocalBinding>().AsNoTracking().SingleOrDefaultAsync(ct);
        if (binding is null)
        {
            if (!await db.Set<Sync.SyncBindingRow>().AnyAsync(row => row.TransportKind == Sync.ProfileSynchronizationKind.Household, ct)) return null;
            HouseholdWriteBoundary.Require(db); return new(db, clock, HouseholdWriteBlockReason.Unverified);
        }
        HouseholdWriteBoundary.Require(db);
        HouseholdLocalWritePermission Denied(HouseholdWriteBlockReason reason) => new(db, clock, reason);
        if (db.Database.GetDbConnection() is not SqliteConnection connection) return Denied(HouseholdWriteBlockReason.Unverified);
        ProtectedSqliteAccess.Require(connection);
        if (binding.GenerationPath != Path.GetFullPath(DatabaseFileIdentity.ConfiguredPath(db))) return Denied(HouseholdWriteBlockReason.Unverified);
        if (await db.Set<HouseholdLocalSecret>().AnyAsync(row => row.Slot == DeletionSlot, ct)) return Denied(HouseholdWriteBlockReason.RemoteRefused);
        if (binding.SpaceId == Guid.Empty || binding.DeviceId == Guid.Empty || binding.MembershipVersion == 0) return Denied(HouseholdWriteBlockReason.Unpublished);
        var financial = await db.Set<Sync.SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
        if (financial is null || !binding.OwnerCreation && financial.LastSuccessAt is null)
            return Denied(HouseholdWriteBlockReason.Unpublished);
        if (financial.TransportKind != Sync.ProfileSynchronizationKind.Household || financial.ProfileId != binding.SpaceId ||
            financial.DeviceId != binding.DeviceId || financial.IdentityId != binding.IdentityId || financial.PeerKey != binding.PeerKey ||
            financial.GenerationPath != binding.GenerationPath || financial.SchemaHash != HouseholdFinancialSchema.Hash)
            return Denied(HouseholdWriteBlockReason.Unverified);
        // Verified membership/keys alone do not mean this receiving ledger has adopted its
        // original financial baseline. Incoming apply uses its exact verified maintenance scope;
        // ordinary writes require the first atomic successful receive, including after a crash.
        if (await db.Set<HouseholdLocalOwnership>().AnyAsync(row => row.Stage >= HouseholdOwnershipStage.Offered && row.Stage <= HouseholdOwnershipStage.Accepted, ct))
            return Denied(HouseholdWriteBlockReason.Rotation);
        if (binding.SendingDisabled) return Denied(HouseholdWriteBlockReason.Paused);
        if (binding.RemoteAccessRefused) return Denied(HouseholdWriteBlockReason.RemoteRefused);
        if (binding.AccessObservedAtUnixSeconds is not { } observedSeconds) return Denied(HouseholdWriteBlockReason.Unverified);
        var observed = FromUnix(observedSeconds); var expires = binding.HostExpiresAtUnixSeconds is { } seconds ? FromUnix(seconds) : (DateTimeOffset?)null;
        var now = clock.GetUtcNow();
        if (now < observed.AddMinutes(-5)) return Denied(HouseholdWriteBlockReason.Clock);
        if (binding.AccessRetention != HouseholdRetentionState.Active || HouseholdAccessPolicy.Retention(now > observed ? now : observed, expires, false) != HouseholdRetentionState.Active)
            return Denied(HouseholdWriteBlockReason.Expired);
        try
        {
            using var device = await ReadIdentityAsync(db, "device", ct); var state = await ReadMembershipAsync(db, binding, device, ct);
            var member = state.Current.Snapshot().Members.SingleOrDefault(item => item.IdentityId == binding.IdentityId);
            if (member is null || !member.Devices.Any(item => item.Id == binding.DeviceId && item.Keys == new HouseholdPublicKeys(device.Public.SigningKey, device.Public.WrappingKey)))
                return Denied(HouseholdWriteBlockReason.Revoked);
            if (member.Role == HouseholdRole.Viewer) return Denied(HouseholdWriteBlockReason.Viewer);
            if (member.Role is not (HouseholdRole.Owner or HouseholdRole.Editor)) return Denied(HouseholdWriteBlockReason.Unverified);
            if (binding.RotationRequired) return Denied(HouseholdWriteBlockReason.Rotation);
            using var epoch = await ReadEpochCoreAsync(db, state, state.Current.Epoch, ct);
            return new(db, clock, null, observed, expires);
        }
        catch (Exception error) when (error is CryptographicException or InvalidDataException) { return Denied(HouseholdWriteBlockReason.Unverified); }
    }
    /// <summary>Checks current original role/clock before sending retained ciphertext as well as before creating a new seal.</summary>
    internal async Task DemandLocalFinancialWriteAsync(HouseholdJournalAuthority authority, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var permission = await CaptureLocalWritePermissionAsync(context.Db, context.Db.WorkClock, ct) ?? throw Refusal();
        permission.Demand(); context.Check();
    }

    /// <summary>Connects only the exact accepted protected device/room; initial viewers consume a separate empty destination, never a readable personal profile.</summary>
    internal static async Task RequireLocalHouseholdConnectionAsync(ZananceDbContext db, string actualPath, string identity, string peerKey,
        Guid space, Guid deviceId, bool receiving, CancellationToken ct)
    {
        if (db.Database.GetDbConnection() is not SqliteConnection connection) throw Refusal();
        ProtectedSqliteAccess.Require(connection); HouseholdWriteBoundary.Require(db);
        if (await db.Set<HouseholdLocalSecret>().AnyAsync(row => row.Slot == DeletionSlot, ct)) throw Refusal();
        var binding = await db.Set<HouseholdLocalBinding>().AsNoTracking().SingleOrDefaultAsync(ct) ?? throw Refusal();
        if (binding.GenerationPath != actualPath || binding.IdentityId != identity || binding.PeerKey != peerKey || binding.SpaceId != space || binding.DeviceId != deviceId ||
            receiving && (binding.OwnerCreation || binding.ProjectionReviewId is not null)) throw Refusal();
        using var device = await ReadIdentityAsync(db, "device", ct); var membership = await ReadMembershipAsync(db, binding, device, ct);
        if (!AllowsLocalReception(binding, membership.Current, db.WorkClock) || membership.Current.Device(binding.DeviceId, requireWrite: false) != device.Public) throw Refusal();
        using var epoch = await ReadEpochCoreAsync(db, membership, membership.Current.Epoch, ct);
    }

    /// <summary>Only accepted read/export access in active/grace time can consume verified feed; no commercial switch grants it.</summary>
    private static bool AllowsLocalReception(HouseholdLocalBinding binding, HouseholdVerifiedMembership membership, TimeProvider clock, bool ownershipRotation = false)
    {
        if (binding.SendingDisabled || binding.RemoteAccessRefused || binding.AccessObservedAtUnixSeconds is not { } seconds || binding.RotationRequired && !ownershipRotation ||
            binding.AccessRetention is not (HouseholdRetentionState.Active or HouseholdRetentionState.ReadOnlyGrace)) return false;
        var observed = FromUnix(seconds); var now = clock.GetUtcNow(); if (now < observed.AddMinutes(-5)) return false;
        var expires = binding.HostExpiresAtUnixSeconds is { } expiry ? FromUnix(expiry) : (DateTimeOffset?)null;
        var member = membership.Snapshot().Members.SingleOrDefault(item => item.IdentityId == binding.IdentityId);
        return member is not null && member.Devices.Any(item => item.Id == binding.DeviceId) && HouseholdAccessPolicy.Allows(
            new(binding.SpaceId, binding.IdentityId, member.Role, true, false, true, membership.Version, membership.Epoch), binding.SpaceId,
            HouseholdRemoteOperation.Read, HouseholdAccessPolicy.Retention(now > observed ? now : observed, expires, false));
    }
    private static DateTimeOffset FromUnix(long seconds)
    {
        try { return DateTimeOffset.FromUnixTimeSeconds(seconds); }
        catch (ArgumentOutOfRangeException) { throw Refusal(); }
    }
}
