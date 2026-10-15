using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>Serializes personal authority and mutations across workers, including device revocation.</summary>
internal sealed class SyncRequestScope : IAsyncDisposable
{
    private readonly ServiceDbContext _db;
    private readonly IDbContextTransaction _transaction;

    private SyncRequestScope(ServiceDbContext db, IDbContextTransaction transaction, string owner, Guid session, DateTimeOffset now)
    { _db = db; _transaction = transaction; Owner = owner; Session = session; Now = now; }

    /// <summary>The validated identity, never supplied by request JSON.</summary>
    internal string Owner { get; }
    /// <summary>The actual current validated session.</summary>
    internal Guid Session { get; }
    /// <summary>Injected operational time, independent of financial dates.</summary>
    internal DateTimeOffset Now { get; }
    /// <summary>Only an actual device resolved under this locked session can authorize financial resource work.</summary>
    internal Guid? AuthorizedDeviceId { get; private set; }

    /// <summary>Rechecks session after taking the same owner lock used by personal revocation.</summary>
    internal static async Task<SyncRequestScope?> OpenAsync(ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, CancellationToken ct)
    {
        var owner = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(owner) || !Guid.TryParse(principal.FindFirstValue(ServiceHost.SessionClaim), out var session) ||
            !int.TryParse(principal.FindFirstValue(ServiceHost.GenerationClaim), out var generation)) return null;
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await LockOwnerAsync(db, owner, ct);
            var now = time.GetUtcNow();
            if (!await db.Sessions.AnyAsync(row => row.Id == session && row.UserId == owner && row.Generation == generation &&
                    !row.Revoked && row.ExpiresAt > now, ct))
            { await transaction.DisposeAsync(); return null; }
            return new(db, transaction, owner, session, now);
        }
        catch { await transaction.DisposeAsync(); throw; }
    }

    /// <summary>Uses the same transactional owner order for identity renewal/reset/revocation and financial publication.</summary>
    internal static Task LockOwnerAsync(ServiceDbContext db, string owner, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null || string.IsNullOrEmpty(owner))
            throw new InvalidOperationException("Personal authority serialization requires an actual owner transaction.");
        return db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({owner}, 0))", ct);
    }

    /// <summary>Requires this session's actual active device, not another owned device's guessed id.</summary>
    internal async Task<SyncDeviceRow?> DeviceAsync(Guid id, CancellationToken ct)
    {
        var device = await _db.Set<SyncDeviceRow>().SingleOrDefaultAsync(row => row.Id == id && row.UserId == Owner &&
            row.SessionId == Session && !row.Revoked, ct);
        AuthorizedDeviceId = device?.Id;
        if (device is not null) device.LastSeenAt = Now;
        return device;
    }

    /// <summary>Every resource resolves through its actual owner; bootstrap publication is restricted to its original device.</summary>
    internal Task<SyncSpaceRow?> SpaceAsync(Guid id, Guid device, CancellationToken ct) =>
        _db.Set<SyncSpaceRow>().SingleOrDefaultAsync(row => row.Id == id && row.UserId == Owner &&
            (!row.Initializing || row.BootstrapDeviceId == device), ct);

    /// <summary>Persists both authority observations and effects before returning a successful outcome.</summary>
    internal async Task CommitAsync(CancellationToken ct)
    { await _db.SaveChangesAsync(ct); await _transaction.CommitAsync(ct); }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
