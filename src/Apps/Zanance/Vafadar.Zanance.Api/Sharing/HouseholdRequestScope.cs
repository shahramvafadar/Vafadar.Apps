using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Content-free sharing refusal; no ciphertext, private content, claims or native exception text enters HTTP/logs.</summary>
internal sealed class HouseholdProtocolRefusal(int status, string code) : Exception(code)
{
    /// <summary>Bounded public HTTP outcome.</summary>
    internal int Status { get; } = status;
}

/// <summary>Actual validated identity/session followed by one room lock, in the same order on every shared resource.</summary>
internal sealed class HouseholdRequestScope : IAsyncDisposable
{
    private readonly SyncRequestScope session;
    private readonly ServiceDbContext db;
    private readonly DateTimeOffset lockedAt;

    private HouseholdRequestScope(SyncRequestScope sessionScope, ServiceDbContext context, HouseholdSpaceRow space, DateTimeOffset now)
    { session = sessionScope; db = context; Space = space; lockedAt = now; }

    /// <summary>Actual authenticated identity, never taken from JSON or an invitation display name.</summary>
    internal string ActorId => session.Owner;
    /// <summary>Actual current nonrevoked session, rechecked after the identity lock.</summary>
    internal Guid SessionId => session.Session;
    /// <summary>Injected operational UTC instant, independent of ledger dates.</summary>
    internal DateTimeOffset Now => lockedAt;
    /// <summary>The one locked shared scope; another resource id cannot be substituted later.</summary>
    internal HouseholdSpaceRow Space { get; }
    /// <summary>Authoritative remote window; security revocation/device approval are still independently required.</summary>
    internal HouseholdRetentionState Retention => HouseholdAccessPolicy.Retention(Now, Space.HostExpiresAt, Space.Deleted);

    /// <summary>Locks session owner then room; no shared operation takes a second identity lock in reverse order.</summary>
    internal static async Task<HouseholdRequestScope?> OpenAsync(ClaimsPrincipal principal, ServiceDbContext db,
        TimeProvider time, Guid spaceId, CancellationToken ct)
    {
        if (spaceId == Guid.Empty) return null;
        var session = await SyncRequestScope.OpenAsync(principal, db, time, ct);
        if (session is null) return null;
        try
        {
            await LockRoomAsync(db, spaceId, ct);
            // Another member can hold the room lock past this session's expiry. Recheck actual time after
            // the final lock instead of carrying the earlier identity-lock timestamp into shared authorization.
            var now = time.GetUtcNow();
            if (!int.TryParse(principal.FindFirstValue(ServiceHost.GenerationClaim), out var generation) ||
                !await db.Sessions.AnyAsync(row => row.Id == session.Session && row.UserId == session.Owner &&
                    row.Generation == generation && !row.Revoked && row.ExpiresAt > now, ct))
            { await session.DisposeAsync(); return null; }
            var space = await db.Set<HouseholdSpaceRow>().SingleOrDefaultAsync(row => row.Id == spaceId, ct);
            if (space is null) { await session.DisposeAsync(); return null; }
            var scope = new HouseholdRequestScope(session, db, space, now);
            var deletion = await db.HouseholdDeletions.ReadAsync(spaceId, ct);
            if (deletion is not null) { HouseholdLifecycleStore.Apply(space, deletion); return scope; }
            await new HouseholdOwnershipStore(db).ExpirePendingAsync(scope, ct);
            return scope;
        }
        catch { await session.DisposeAsync(); throw; }
    }

    /// <summary>One exact original room partition shared by authenticated requests and privacy retirement, always after any identity lock.</summary>
    internal static Task LockRoomAsync(ServiceDbContext db, Guid space, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null || space == Guid.Empty) throw new InvalidOperationException("Room maintenance requires its original transaction.");
        var partition = "zanance.household:" + space.ToString("N");
        return db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({partition}, 1))", ct);
    }

    /// <summary>Resolves only the actual caller's member; pending invitation access is opt-in and never financial authority.</summary>
    internal Task<HouseholdMemberRow?> MemberAsync(bool includePending, CancellationToken ct) => db.Set<HouseholdMemberRow>()
        .SingleOrDefaultAsync(row => row.SpaceId == Space.Id && row.UserId == ActorId && !row.Revoked && (includePending || row.Accepted), ct);

    /// <summary>Requires this exact account/session/device and actual room; another member's guessed device never qualifies.</summary>
    internal async Task<HouseholdDeviceRow?> DeviceAsync(Guid id, bool includePending, CancellationToken ct)
    {
        if (id == Guid.Empty) return null;
        var device = await db.Set<HouseholdDeviceRow>().SingleOrDefaultAsync(row => row.Id == id && row.SpaceId == Space.Id &&
            row.UserId == ActorId && row.SessionId == SessionId && !row.Revoked && (includePending || row.Approved), ct);
        if (device is not null) device.LastSeenAt = Now;
        return device;
    }

    /// <summary>Checks every remote financial/export/key/audit/administrative resource under current membership and retention.</summary>
    internal async Task<HouseholdDeviceRow> RequireAsync(Guid deviceId, HouseholdRemoteOperation operation, CancellationToken ct)
    {
        var member = await MemberAsync(includePending: false, ct);
        var device = await DeviceAsync(deviceId, includePending: false, ct);
        if (member is null || device is null || Space.MembershipVersion < 1 || Space.KeyEpoch < 1)
            throw new HouseholdProtocolRefusal(404, "HouseholdResourceUnavailable");
        var authority = new HouseholdMembershipAuthority(Space.Id, ActorId, member.Role, member.Accepted,
            member.Revoked, device.Approved && !device.Revoked, Space.MembershipVersion, Space.KeyEpoch);
        if (!HouseholdAccessPolicy.Allows(authority, Space.Id, operation, Retention))
            throw new HouseholdProtocolRefusal(Retention is HouseholdRetentionState.Deleted or HouseholdRetentionState.DeletionDue ? 410 : 403,
                "HouseholdOperationUnavailable");
        if (Space.RotationRequired && operation is HouseholdRemoteOperation.WriteFinancial or HouseholdRemoteOperation.DeleteFinancial)
            throw new HouseholdProtocolRefusal(409, "HouseholdKeyRotationRequired");
        return device;
    }

    /// <summary>Only exact actual owner initialization/recovery can inspect its own pending device; no financial read follows.</summary>
    internal async Task<HouseholdDeviceRow> RequireOwnerPendingDeviceAsync(Guid deviceId, CancellationToken ct)
    {
        if (Space.OwnerId != ActorId || Space.Deleted || Retention is HouseholdRetentionState.DeletionDue or HouseholdRetentionState.Deleted)
            throw new HouseholdProtocolRefusal(404, "HouseholdResourceUnavailable");
        var member = await MemberAsync(includePending: true, ct);
        var device = await DeviceAsync(deviceId, includePending: true, ct);
        if (member?.Role != HouseholdRole.Owner || device is null)
            throw new HouseholdProtocolRefusal(404, "HouseholdResourceUnavailable");
        return device;
    }

    /// <summary>Permits only observing this exact session's own previous attempt metadata after role/device revocation; no finance, grants or new mutation follows.</summary>
    internal async Task RequireOwnAttemptObservationAsync(Guid deviceId, CancellationToken ct)
    {
        if (!await db.Set<HouseholdDeviceRow>().AnyAsync(row => row.Id == deviceId && row.SpaceId == Space.Id &&
            row.UserId == ActorId && row.SessionId == SessionId, ct))
            throw new HouseholdProtocolRefusal(404, "HouseholdResourceUnavailable");
    }

    /// <summary>Persists authority observations and effects under the actual original session/room transaction.</summary>
    internal Task CommitAsync(CancellationToken ct) => session.CommitAsync(ct);
    /// <inheritdoc />
    public ValueTask DisposeAsync() => session.DisposeAsync();
}
