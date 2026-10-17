using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api;

/// <summary>Server identity; never a local financial profile or purchase entitlement.</summary>
public sealed class ServiceUser : IdentityUser
{
}

/// <summary>Bounded device grant, rechecked for every authenticated request.</summary>
public sealed class DeviceSession
{
    /// <summary>Unpredictable server-created session identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Actual identity owner.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Refresh generation; concurrent replay cannot create another grant.</summary>
    public int Generation { get; set; }
    /// <summary>Absolute expiry, never extended by refresh.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>Explicit revocation.</summary>
    public bool Revoked { get; set; }
}

/// <summary>One-use challenge and transactional mail outbox, encrypted by framework Data Protection.</summary>
public sealed class IdentityChallenge
{
    /// <summary>Public opaque reference.</summary>
    public Guid Id { get; set; }
    /// <summary>Actual identity owner.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Purpose, separate confirmation and password recovery.</summary>
    public string Purpose { get; set; } = "";
    /// <summary>Protected token and recipient; never logged or returned by HTTP.</summary>
    public string ProtectedMail { get; set; } = "";
    /// <summary>Absolute validity limit.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>Consumption commits with its identity effect.</summary>
    public bool Consumed { get; set; }
    /// <summary>Lease owner for idempotent local delivery.</summary>
    public Guid? LeaseOwner { get; set; }
    /// <summary>Bounded lease expiry for worker recovery.</summary>
    public DateTimeOffset? LeaseUntil { get; set; }
    /// <summary>Delivery completion; rows remain until expiry maintenance.</summary>
    public bool Delivered { get; set; }
}

/// <summary>Private opaque object manifest; no financial ledger is interpreted by this service.</summary>
public sealed class PrivateObject
{
    /// <summary>Server-created immutable object identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Actual authenticated owner.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Bounded byte length.</summary>
    public long Length { get; set; }
    /// <summary>Integrity digest of the uploaded bytes.</summary>
    public string Sha256 { get; set; } = "";
    /// <summary>Pending durable deletion; reads stop immediately.</summary>
    public bool Deleted { get; set; }
    /// <summary>Committed physical retirement; pending deletes remain resumable.</summary>
    public bool StorageRetired { get; set; }
    /// <summary>Creation time for operations and consistent retention.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Durable per-window request counter shared by all workers.</summary>
public sealed class RequestWindow
{
    /// <summary>Hashed request partition, without stored raw network address.</summary>
    public string Partition { get; set; } = "";
    /// <summary>UTC minute boundary.</summary>
    public DateTimeOffset StartsAt { get; set; }
    /// <summary>Atomic request count.</summary>
    public int Count { get; set; }
}

/// <summary>Independent PostgreSQL service schema and maintained Identity persistence.</summary>
public sealed class ServiceDbContext(DbContextOptions<ServiceDbContext> options, Sharing.IHouseholdDeletionLedger? householdDeletions = null) : IdentityDbContext<ServiceUser>(options)
{
    /// <summary>Runtime-only immutable privacy intent port; absent design-time contexts cannot authorize household requests.</summary>
    internal Sharing.IHouseholdDeletionLedger HouseholdDeletions => householdDeletions ?? throw new InvalidOperationException("Household privacy storage is not composed.");
    /// <summary>Device grants.</summary>
    public DbSet<DeviceSession> Sessions => Set<DeviceSession>();
    /// <summary>Challenges and mail delivery journal.</summary>
    public DbSet<IdentityChallenge> Challenges => Set<IdentityChallenge>();
    /// <summary>Private manifests.</summary>
    public DbSet<PrivateObject> Objects => Set<PrivateObject>();
    /// <summary>Shared request windows.</summary>
    public DbSet<RequestWindow> RequestWindows => Set<RequestWindow>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        Sync.SyncServiceModel.Configure(builder);
        Sharing.HouseholdServiceModel.Configure(builder);
        Sharing.HouseholdOwnershipModel.Configure(builder);
        Sharing.HouseholdLifecycleModel.Configure(builder);
        Billing.BillingServiceModel.Configure(builder);
        Analysis.AiServiceModel.Configure(builder);
        builder.Entity<DeviceSession>().HasOne<ServiceUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<IdentityChallenge>().HasOne<ServiceUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PrivateObject>().HasOne<ServiceUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RequestWindow>().HasKey(x => new { x.Partition, x.StartsAt });
        builder.Entity<RequestWindow>().Property(x => x.Partition).HasMaxLength(64);
        builder.Entity<DeviceSession>().HasIndex(x => new { x.UserId, x.Revoked });
        builder.Entity<IdentityChallenge>().HasIndex(x => new { x.Delivered, x.ExpiresAt });
        builder.Entity<PrivateObject>().HasIndex(x => new { x.UserId, x.Deleted });
        builder.Entity<PrivateObject>().Property(x => x.Sha256).HasMaxLength(64);
        builder.Entity<IdentityChallenge>().Property(x => x.Purpose).HasMaxLength(16);
    }
}
