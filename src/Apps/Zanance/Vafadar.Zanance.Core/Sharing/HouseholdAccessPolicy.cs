namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Server-enforced household roles; personal purchases and UI filters never substitute for membership.</summary>
public enum HouseholdRole
{
    /// <summary>Accepted read/export access, without financial writes or membership administration.</summary>
    Viewer = 1,
    /// <summary>Accepted financial create/edit/delete access, without invitations or ownership administration.</summary>
    Editor = 2,
    /// <summary>The single current owning identity, independently of its number of approved devices.</summary>
    Owner = 3,
}

/// <summary>Explicit remote operations, including permissions that must be assessed separately from simple viewing.</summary>
public enum HouseholdRemoteOperation
{
    /// <summary>Read encrypted financial records or their bounded manifests.</summary>
    Read,
    /// <summary>Export encrypted remote content for local client decryption.</summary>
    Export,
    /// <summary>Read only this actual approved device's encrypted epoch grant.</summary>
    ReadDeviceGrant,
    /// <summary>Create or edit financial content under current signed membership and epoch.</summary>
    WriteFinancial,
    /// <summary>Delete financial records with explicit signed tombstones.</summary>
    DeleteFinancial,
    /// <summary>Create or withdraw an invitation; its recipient accepts through a separate one-use authority.</summary>
    Invite,
    /// <summary>Revoke a member/device; security controls remain available during subscription retention.</summary>
    Revoke,
    /// <summary>Complete a two-sided confirmed owner transfer.</summary>
    TransferOwnership,
    /// <summary>Read the scoped membership/security audit, without other spaces or private records.</summary>
    ReadAudit,
    /// <summary>Leave this space without claiming remote deletion of existing local copies.</summary>
    Leave,
    /// <summary>Delete the entire space through its current owner, independently of a commercial subscription.</summary>
    DeleteSpace,
    /// <summary>Read only this actual member's service deadlines/state, including retained time; no financial/key access follows.</summary>
    ReadLifecycle,
}

/// <summary>Remote service retention state; it makes no claim that downloaded/exported copies can be recalled.</summary>
public enum HouseholdRetentionState
{
    /// <summary>Current host service authority permits ordinary authorized work.</summary>
    Active,
    /// <summary>The first30 days after verified service expiry allow read/export but no new financial changes.</summary>
    ReadOnlyGrace,
    /// <summary>The following90 days retain server bytes without serving financial reads/exports.</summary>
    Retained,
    /// <summary>The120-day deadline passed; no reactivation/read may race delayed physical retirement.</summary>
    DeletionDue,
    /// <summary>Explicit permanent deletion already revoked every grant.</summary>
    Deleted,
}

/// <summary>Exact authenticated membership facts rechecked under the service's transaction lock.</summary>
public sealed record HouseholdMembershipAuthority
{
    /// <summary>Creates the current actual space/identity/device authority, not a claim of paid purchase verification.</summary>
    public HouseholdMembershipAuthority(Guid spaceId, string identityId, HouseholdRole role, bool accepted,
        bool revoked, bool deviceApproved, long membershipVersion, long keyEpoch)
    {
        if (spaceId == Guid.Empty || string.IsNullOrWhiteSpace(identityId) || identityId.Length > 128 ||
            !Enum.IsDefined(role) || membershipVersion < 1 || keyEpoch < 1)
            throw new ArgumentException("Exact household membership authority required.");
        SpaceId = spaceId; IdentityId = identityId; Role = role; Accepted = accepted; Revoked = revoked;
        DeviceApproved = deviceApproved; MembershipVersion = membershipVersion; KeyEpoch = keyEpoch;
    }

    /// <summary>Actual scope; an authority cannot be reused against another guessed space id.</summary>
    public Guid SpaceId { get; }
    /// <summary>Actual validated identity, never an email address supplied in a request body.</summary>
    public string IdentityId { get; }
    /// <summary>Current role, not a stale role on an offline pending change.</summary>
    public HouseholdRole Role { get; }
    /// <summary>Explicit accepted invitation; a pending reserved seat cannot read data.</summary>
    public bool Accepted { get; }
    /// <summary>Immediate security revocation, taking precedence over grace/retention and all purchased plans.</summary>
    public bool Revoked { get; }
    /// <summary>Current exact device-key approval, separate from the account/session login.</summary>
    public bool DeviceApproved { get; }
    /// <summary>Current signed membership generation; clients must also verify its pinned continuity.</summary>
    public long MembershipVersion { get; }
    /// <summary>Current encryption epoch required for writes after revocation/rotation.</summary>
    public long KeyEpoch { get; }
}

/// <summary>Pure scope-bound service policy; it never reads a clock, hides personal data or manufactures a Pro grant.</summary>
public static class HouseholdAccessPolicy
{
    /// <summary>Approved read/export interval; uses elapsed operational UTC time, never a financial calendar month.</summary>
    public static readonly TimeSpan ReadOnlyInterval = TimeSpan.FromDays(30);
    /// <summary>Total remote retention ends120 days after expiry, including the initial30-day read-only interval.</summary>
    public static readonly TimeSpan TotalRetentionInterval = TimeSpan.FromDays(120);

    /// <summary>Evaluates exclusive exact deadlines with an injected instant and verified host expiry facts.</summary>
    public static HouseholdRetentionState Retention(DateTimeOffset now, DateTimeOffset? verifiedHostExpiry, bool deleted)
    {
        if (deleted) return HouseholdRetentionState.Deleted;
        if (verifiedHostExpiry is not { } expiry || now < expiry) return HouseholdRetentionState.Active;
        var elapsed = now - expiry;
        if (elapsed < ReadOnlyInterval) return HouseholdRetentionState.ReadOnlyGrace;
        return elapsed < TotalRetentionInterval ? HouseholdRetentionState.Retained : HouseholdRetentionState.DeletionDue;
    }

    /// <summary>Checks each remote resource under accepted, active, exact-device membership and current service state.</summary>
    public static bool Allows(HouseholdMembershipAuthority? membership, Guid requestedSpace,
        HouseholdRemoteOperation operation, HouseholdRetentionState retention)
    {
        if (!Enum.IsDefined(operation) || !Enum.IsDefined(retention) || membership is not
            { Accepted: true, Revoked: false, DeviceApproved: true } || requestedSpace != membership.SpaceId ||
            (retention is HouseholdRetentionState.Deleted or HouseholdRetentionState.DeletionDue) && operation != HouseholdRemoteOperation.ReadLifecycle) return false;
        if (operation == HouseholdRemoteOperation.ReadLifecycle) return true;
        if (operation == HouseholdRemoteOperation.Leave) return membership.Role != HouseholdRole.Owner;
        if (operation is HouseholdRemoteOperation.Revoke or HouseholdRemoteOperation.DeleteSpace)
            return membership.Role == HouseholdRole.Owner;
        if (retention == HouseholdRetentionState.Retained) return false;
        if (operation is HouseholdRemoteOperation.Read or HouseholdRemoteOperation.Export or
            HouseholdRemoteOperation.ReadDeviceGrant or HouseholdRemoteOperation.ReadAudit) return true;
        if (retention == HouseholdRetentionState.ReadOnlyGrace) return false;
        return operation switch
        {
            HouseholdRemoteOperation.WriteFinancial or HouseholdRemoteOperation.DeleteFinancial =>
                membership.Role is HouseholdRole.Editor or HouseholdRole.Owner,
            HouseholdRemoteOperation.Invite or HouseholdRemoteOperation.TransferOwnership => membership.Role == HouseholdRole.Owner,
            _ => false,
        };
    }
}
