namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Current-request cache publication context, independent of a replayable operation completion receipt.</summary>
public sealed record BillingSnapshotContext(Guid CustomerId, Guid RequestId, string DeviceFingerprint,
    BillingIssuer Distribution, BillingEnvironment Environment, string? IdentityHash, long MinimumRevision,
    DateTimeOffset MinimumServerTime);

/// <summary>Exact authority/scope/freshness checks after standard signature verification and before atomic private cache publication.</summary>
public static class BillingSnapshotPolicy
{
    /// <summary>Requires the still-owned fresh request, complete current state and nonregressing signed floors.</summary>
    public static void RequireCurrent(BillingSnapshot snapshot, BillingSnapshotContext current)
    {
        if (snapshot.Version != 1 || snapshot.Kind != BillingSnapshotKind.Current || current.CustomerId == Guid.Empty ||
            current.RequestId == Guid.Empty || snapshot.CustomerId != current.CustomerId || snapshot.RequestId != current.RequestId ||
            snapshot.DeviceFingerprint != current.DeviceFingerprint || snapshot.Distribution != current.Distribution ||
            snapshot.Environment != current.Environment || snapshot.IdentityHash != current.IdentityHash || snapshot.AccountToken == Guid.Empty ||
            snapshot.Revision < current.MinimumRevision || snapshot.ServerTime < current.MinimumServerTime ||
            snapshot.Grants is null || snapshot.Grants.Count > 32 || snapshot.Retired && snapshot.Grants.Count != 0)
            throw new ArgumentException("Current billing observation was refused.");
        foreach (var grant in snapshot.Grants)
        {
            grant.Facts.Validate();
            if (grant.Environment != current.Environment || grant.VerifiedAt > snapshot.ServerTime ||
                grant.PurchaseId is not null && (grant.PurchaseId.Length != 64 || !grant.PurchaseId.All(char.IsAsciiHexDigit)) ||
                !BillingProducts.AppliesTo(grant.Facts.Product, grant.Issuer, current.Distribution, grant.MatchingIdentity) ||
                grant.MatchingIdentity && current.IdentityHash is null)
                throw new ArgumentException("Current billing grant scope was refused.");
        }
    }
}
