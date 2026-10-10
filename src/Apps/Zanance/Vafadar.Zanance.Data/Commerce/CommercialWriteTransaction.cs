using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>
/// Holds the SQLite write transaction across read/count/check/save. Independent services must compete for the same
/// database writer rather than each using an unrelated in-memory semaphore. Inactive builds add no transaction.
/// </summary>
internal sealed class CommercialWriteTransaction : IAsyncDisposable
{
    private readonly ICommercialWriteAccessSource _source;
    private readonly string _databasePath;
    private readonly CommercialWriteAccess _access;
    private readonly IDbContextTransaction? _ownedTransaction;

    private CommercialWriteTransaction(ICommercialWriteAccessSource source, string databasePath,
        CommercialWriteAccess access, IDbContextTransaction? transaction)
    {
        _source = source;
        _databasePath = databasePath;
        _access = access;
        _ownedTransaction = transaction;
    }

    /// <summary>Gets whether a write requires commercial checks.</summary>
    public bool Enforced => _access.Enforced;

    /// <summary>Captures the actual file and acquires its write transaction before any quota-sensitive reads.</summary>
    public static async Task<CommercialWriteTransaction> OpenAsync(ZananceDbContext db,
        ICommercialWriteAccessSource source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(source);
        var path = Path.GetFullPath(db.Database.GetDbConnection().DataSource);
        var access = source.Capture(path) ?? throw new InvalidOperationException("A commercial access source returned no snapshot.");
        if (!access.Enforced) return new(source, path, access, null);
        if (!string.Equals(path, access.DatabasePath, StringComparison.Ordinal))
            throw new InvalidOperationException("The commercial snapshot belongs to another database.");
        // Microsoft.Data.Sqlite's serializable writer transaction protects count and save across factories/processes.
        // The independent-provider contention test verifies this boundary with the actual SQLite implementation.
        var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var result = new CommercialWriteTransaction(source, path, access, transaction);
        try
        {
            result.EnsureCurrent();
            return result;
        }
        catch
        {
            await result.DisposeAsync();
            throw;
        }
    }

    /// <summary>Demands the feature and returns its maximum, without reading counts for an unlimited quota.</summary>
    public int? DemandCapacity(CommercialFeature feature, QuotaKind kind)
    {
        DemandFeature(feature);
        if (!Enforced) return null;
        return QuotaPolicy.Get(kind, _access.Context!).Maximum;
    }

    /// <summary>Checks the operation even when it adds no quota item; a paid right never substitutes for membership.</summary>
    public void DemandFeature(CommercialFeature feature)
    {
        EnsureCurrent();
        if (!Enforced) return;
        var permission = PlanPolicy.Check(feature, _access.Context!);
        if (permission != FeaturePermission.Allowed) throw new CommercialWriteRejectedException(feature, permission);
    }

    /// <summary>Rejects an addition against the actual transaction-local count.</summary>
    public void DemandCount(CommercialFeature feature, QuotaKind kind, int maximum, int current, int requested = 1)
    {
        EnsureCurrent();
        if (!new Quota(maximum).CanAdd(current, requested))
            throw new CommercialWriteRejectedException(feature, kind, maximum, current, requested);
    }

    /// <summary>Rejects a changed entitlement/scope snapshot before saving rather than using retired profile rights.</summary>
    public void EnsureCurrent()
    {
        if (_access.Enforced && _source.Capture(_databasePath) != _access)
            throw new InvalidOperationException("Commercial access changed during the write; retry with current rights.");
    }

    /// <summary>Commits only the transaction this guard owns; existing callers keep their outer atomic operation.</summary>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        EnsureCurrent();
        if (_ownedTransaction is not null) await _ownedTransaction.CommitAsync(cancellationToken);
    }

    /// <summary>Disposes an owned uncommitted transaction, rolling back rejected or failed writes.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_ownedTransaction is not null) await _ownedTransaction.DisposeAsync();
    }
}
