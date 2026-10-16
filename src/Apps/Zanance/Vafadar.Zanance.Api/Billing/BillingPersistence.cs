using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Installation-owned purchase principal, separate from identity and every financial profile.</summary>
public sealed class BillingCustomerRow
{
    /// <summary>Original signed registration identifier, retained before HTTP.</summary>
    public Guid Id { get; set; }
    /// <summary>Fixed supported distribution issuer, never an arbitrary web grant.</summary>
    public BillingIssuer Distribution { get; set; }
    /// <summary>Sandbox and production principals never share authority.</summary>
    public BillingEnvironment Environment { get; set; }
    /// <summary>Original public device verification identity; no private key.</summary>
    public string PublicIdentity { get; set; } = "";
    /// <summary>Exact independently possessed device public fingerprint.</summary>
    public string DeviceFingerprint { get; set; } = "";
    /// <summary>Original store-local account binding, allowing restoration without a Zanance account.</summary>
    public string AnonymousAccountBinding { get; set; } = "";
    /// <summary>Server-created anonymous store token, independent of a client-selected registration id.</summary>
    public Guid AccountToken { get; set; }
    /// <summary>Exact protected initial registration response; replay does not expose later purchase metadata.</summary>
    public string ProtectedRegistrationResponse { get; set; } = "";
    /// <summary>Immutable original optional identity commitment, independent of later explicit linking.</summary>
    public string? RegistrationIdentityHash { get; set; }
    /// <summary>Optional actual identity, bound only with its current validated session and original device proof.</summary>
    public string? UserId { get; set; }
    /// <summary>Server time of registration.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Original principal identity/retirement revision; purchase freshness uses the signed current nonce and server time.</summary>
    public long Revision { get; set; }
    /// <summary>Explicit device retirement; it never revokes a store purchase or destroys financial data.</summary>
    public bool Revoked { get; set; }
}

/// <summary>Stable provider account token for an actual identity, independent of app distribution or profile.</summary>
public sealed class BillingIdentityRow
{
    /// <summary>Actual validated service identity.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Explicit provider environment.</summary>
    public BillingEnvironment Environment { get; set; }
    /// <summary>Opaque provider app-account binding; never email or financial profile identity.</summary>
    public Guid AccountToken { get; set; }
    /// <summary>Indexed irreversible Google account binding; Apple uses the opaque UUID directly.</summary>
    public string GoogleAccountBinding { get; set; } = "";
}

/// <summary>Provider-verified purchase chain; tokens and receipt references are protected at rest.</summary>
public sealed class BillingPurchaseRow
{
    /// <summary>Domain-separated SHA256 of issuer/environment/provider purchase reference, never an order number.</summary>
    public string Id { get; set; } = "";
    /// <summary>Actual verification issuer.</summary>
    public BillingIssuer Issuer { get; set; }
    /// <summary>Verification environment; sandbox cannot grant production rights.</summary>
    public BillingEnvironment Environment { get; set; }
    /// <summary>Original registered customer identified by the verified provider account token.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Actual identity for Pro or explicitly linked store rights; never taken from a client claim.</summary>
    public string? UserId { get; set; }
    /// <summary>Data-Protection ciphertext of the minimum provider refresh reference.</summary>
    public string ProtectedReference { get; set; } = "";
    /// <summary>Actual provider account binding from the first accepted verification; never a client-selected owner.</summary>
    public string ProviderAccountBinding { get; set; } = "";
    /// <summary>An unverified query reservation never owns a purchase or enters a signed entitlement snapshot.</summary>
    public bool Verified { get; set; }
    /// <summary>Current accepted logical product, determined by the configured provider mapping.</summary>
    public BillingProduct Product { get; set; }
    /// <summary>Accepted provider lifecycle for finite current-snapshot selection.</summary>
    public BillingPurchaseState State { get; set; }
    /// <summary>Actual effective provider end, including its own explicit billing grace, or null for Lifetime.</summary>
    public DateTimeOffset? AccessUntil { get; set; }
    /// <summary>Bounded normalized provider facts, without credentials or financial data.</summary>
    public string FactsJson { get; set; } = "";
    /// <summary>Server time of the last actual provider verification.</summary>
    public DateTimeOffset VerifiedAt { get; set; }
    /// <summary>Monotonic accepted-observation revision.</summary>
    public long Revision { get; set; }
    /// <summary>Durable acknowledge work remains after grant persistence or a lost provider response.</summary>
    public bool AcknowledgePending { get; set; }
    /// <summary>Due time for bounded provider reconciliation, separate from the entitlement's paid end.</summary>
    public DateTimeOffset RecheckAt { get; set; }
    /// <summary>Authenticated event schedule revision; an event arriving during HTTP cannot be lost by its older result.</summary>
    public long RequestedRecheckRevision { get; set; }
    /// <summary>Consecutive content-free retry count, bounded for backoff.</summary>
    public int RecheckFailures { get; set; }
    /// <summary>One provider refresh lease, never a database reader held across HTTP.</summary>
    public Guid? LeaseOwner { get; set; }
    /// <summary>Bounded lease; an expired worker cannot publish over a newer observation.</summary>
    public DateTimeOffset? LeaseUntil { get; set; }
}

/// <summary>Explicit verified-store restoration or original purchase grants to an actual registered device.</summary>
public sealed class BillingDevicePurchaseRow
{
    /// <summary>Actual signed device principal.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>One verified purchase, unique globally across customer claims.</summary>
    public string PurchaseId { get; set; } = "";
}

/// <summary>Verified provider transaction aliases converge on one stable purchase chain.</summary>
public sealed class BillingPurchaseAliasRow
{
    /// <summary>Issuer/environment/query-reference digest.</summary>
    public string Id { get; set; } = "";
    /// <summary>Original canonical provider-chain digest, never chosen by client input.</summary>
    public string PurchaseId { get; set; } = "";
}

/// <summary>Idempotent original signed command; repeated ids with changed bytes are refused.</summary>
public sealed class BillingCommandRow
{
    /// <summary>Actual signed principal.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Original operation retained before HTTP.</summary>
    public Guid Id { get; set; }
    /// <summary>Digest of exact original signed request bytes.</summary>
    public string RequestHash { get; set; } = "";
    /// <summary>Exact protected signed command, preserved before the first provider query.</summary>
    public string ProtectedRequest { get; set; } = "";
    /// <summary>Original finite refresh queue, captured before provider HTTP.</summary>
    public string ProtectedWork { get; set; } = "";
    /// <summary>Accepted original queue prefix; a retry does not requery its completed steps.</summary>
    public int CompletedWork { get; set; }
    /// <summary>Original last accepted provider outcome for the single native operation's completion receipt.</summary>
    public BillingPurchaseState? LastState { get; set; }
    /// <summary>Protected exact response for lost-acknowledgement continuation.</summary>
    public string ProtectedResponse { get; set; } = "";
    /// <summary>Finite retention; purchase uniqueness remains independent of command expiry.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>Authenticated notification trigger, deduplicated separately from provider purchase authority.</summary>
public sealed class BillingNotificationRow
{
    /// <summary>Issuer/environment/event-id digest; no raw token or account metadata is logged.</summary>
    public string Id { get; set; } = "";
    /// <summary>Actual authenticated provider.</summary>
    public BillingIssuer Issuer { get; set; }
    /// <summary>Authenticated provider environment.</summary>
    public BillingEnvironment Environment { get; set; }
    /// <summary>Minimum protected provider reference for the subsequent authoritative query.</summary>
    public string ProtectedReference { get; set; } = "";
    /// <summary>Server receipt time, never treated as provider event ordering.</summary>
    public DateTimeOffset ReceivedAt { get; set; }
    /// <summary>Set only after the authoritative-query schedule commits atomically; it is not a verified paid outcome.</summary>
    public bool Completed { get; set; }
    /// <summary>Current durable worker lease.</summary>
    public Guid? LeaseOwner { get; set; }
    /// <summary>Bounded worker lease expiry.</summary>
    public DateTimeOffset? LeaseUntil { get; set; }
    /// <summary>Earliest retry, without raw exception diagnostics.</summary>
    public DateTimeOffset RetryAt { get; set; }
    /// <summary>Bounded retry count for operational backoff.</summary>
    public int Attempts { get; set; }
}

/// <summary>Purchase metadata lives only in the independent service schema, never a portable financial backup.</summary>
internal static class BillingServiceModel
{
    /// <summary>Configures finite columns, ownership uniqueness and explicit identity-retention barriers.</summary>
    internal static void Configure(ModelBuilder builder)
    {
        var customers = builder.Entity<BillingCustomerRow>();
        customers.HasKey(x => x.Id); customers.Property(x => x.PublicIdentity).HasMaxLength(16384);
        customers.Property(x => x.DeviceFingerprint).HasMaxLength(64);
        customers.Property(x => x.AnonymousAccountBinding).HasMaxLength(128);
        customers.HasIndex(x => x.AccountToken).IsUnique();
        customers.HasIndex(x => new { x.Distribution, x.Environment, x.AnonymousAccountBinding }).IsUnique();
        customers.Property(x => x.ProtectedRegistrationResponse).HasMaxLength(65536);
        customers.Property(x => x.RegistrationIdentityHash).HasMaxLength(64);
        customers.HasIndex(x => new { x.UserId, x.Environment, x.Revoked });
        customers.HasOne<ServiceUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        var identities = builder.Entity<BillingIdentityRow>();
        identities.HasKey(x => new { x.UserId, x.Environment }); identities.HasIndex(x => x.AccountToken).IsUnique();
        identities.Property(x => x.GoogleAccountBinding).HasMaxLength(64);
        identities.HasIndex(x => x.GoogleAccountBinding).IsUnique();
        identities.HasOne<ServiceUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        var purchases = builder.Entity<BillingPurchaseRow>();
        purchases.HasKey(x => x.Id); purchases.Property(x => x.Id).HasMaxLength(64);
        purchases.Property(x => x.ProtectedReference).HasMaxLength(32768); purchases.Property(x => x.FactsJson).HasMaxLength(8192);
        purchases.Property(x => x.ProviderAccountBinding).HasMaxLength(128);
        purchases.HasIndex(x => new { x.UserId, x.Environment }); purchases.HasIndex(x => new { x.AcknowledgePending, x.LeaseUntil });
        purchases.HasIndex(x => new { x.State, x.AccessUntil });
        purchases.HasIndex(x => new { x.Verified, x.RecheckAt, x.LeaseUntil });
        purchases.HasOne<BillingCustomerRow>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        purchases.HasOne<ServiceUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        var devices = builder.Entity<BillingDevicePurchaseRow>();
        devices.HasKey(x => new { x.CustomerId, x.PurchaseId }); devices.Property(x => x.PurchaseId).HasMaxLength(64);
        devices.HasOne<BillingCustomerRow>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        devices.HasOne<BillingPurchaseRow>().WithMany().HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        var aliases = builder.Entity<BillingPurchaseAliasRow>(); aliases.HasKey(x => x.Id);
        aliases.Property(x => x.Id).HasMaxLength(64); aliases.Property(x => x.PurchaseId).HasMaxLength(64);
        aliases.HasOne<BillingPurchaseRow>().WithMany().HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        var commands = builder.Entity<BillingCommandRow>();
        commands.HasKey(x => new { x.CustomerId, x.Id }); commands.Property(x => x.RequestHash).HasMaxLength(64);
        commands.Property(x => x.ProtectedRequest).HasMaxLength(65536);
        commands.Property(x => x.ProtectedWork).HasMaxLength(65536);
        commands.Property(x => x.ProtectedResponse).HasMaxLength(262144); commands.HasIndex(x => x.ExpiresAt);
        commands.HasOne<BillingCustomerRow>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        var notifications = builder.Entity<BillingNotificationRow>();
        notifications.HasKey(x => x.Id); notifications.Property(x => x.Id).HasMaxLength(64);
        notifications.Property(x => x.ProtectedReference).HasMaxLength(32768);
        notifications.HasIndex(x => new { x.Completed, x.RetryAt, x.LeaseUntil });
    }
}
