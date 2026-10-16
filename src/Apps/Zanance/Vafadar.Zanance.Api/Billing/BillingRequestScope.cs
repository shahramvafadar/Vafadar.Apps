using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Actual optional session plus short-lived serialized billing mutations; no HTTP within this scope.</summary>
internal sealed class BillingRequestScope : IAsyncDisposable
{
    private readonly SyncRequestScope? _session;
    private readonly IDbContextTransaction? _anonymous;
    private readonly ServiceDbContext _db;
    private BillingRequestScope(ServiceDbContext db, SyncRequestScope? session, IDbContextTransaction? anonymous)
    { _db = db; _session = session; _anonymous = anonymous; }
    /// <summary>Actual confirmed identity or null for account-free local Plus.</summary>
    internal string? UserId => _session?.Owner;

    /// <summary>Revalidates an actual session under the existing owner lock; anonymous never creates an identity.</summary>
    internal static async Task<BillingRequestScope> OpenAsync(ServiceDbContext db, ClaimsPrincipal principal, TimeProvider time, CancellationToken ct)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            var session = await SyncRequestScope.OpenAsync(principal, db, time, ct)
                ?? throw new BillingRefusal(401, "CurrentBillingIdentityRequired");
            return new(db, session, null);
        }
        return new(db, null, await db.Database.BeginTransactionAsync(ct));
    }

    /// <summary>Fixed lock order: actual identity, customer, then purchase; workers never reverse these partitions.</summary>
    internal static Task LockCustomerAsync(ServiceDbContext db, Guid customer, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null || customer == Guid.Empty) throw new BillingRefusal(422, "BillingCustomerRequired");
        var partition = "zanance.billing.customer:" + customer.ToString("N");
        return db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({partition}, 5))", ct);
    }

    /// <summary>Serializes exact provider purchase ownership across devices and workers.</summary>
    internal static Task LockPurchaseAsync(ServiceDbContext db, string id, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null || id.Length != 64) throw new BillingRefusal(422, "StorePurchaseRefused");
        var partition = "zanance.billing.purchase:" + id;
        return db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({partition}, 6))", ct);
    }

    /// <summary>Commits the current validated authority and its effects atomically.</summary>
    internal async Task CommitAsync(CancellationToken ct)
    {
        if (_session is not null) await _session.CommitAsync(ct);
        else { await _db.SaveChangesAsync(ct); await _anonymous!.CommitAsync(ct); }
    }
    /// <inheritdoc />
    public ValueTask DisposeAsync() => _session is not null ? _session.DisposeAsync() : _anonymous!.DisposeAsync();
}
