using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>
/// Holds the SQLite write transaction across read/count/check/save. Independent services must compete for the same
/// database writer rather than each using an unrelated in-memory semaphore. Inactive checks add no quota
/// transaction; callers may retain their existing atomic operation by explicitly requesting its transaction.
/// </summary>
internal sealed class CommercialWriteTransaction : IAsyncDisposable
{
    private readonly CommercialFileAccess _access;
    private readonly IDbContextTransaction? _ownedTransaction;

    private CommercialWriteTransaction(CommercialFileAccess access, IDbContextTransaction? transaction)
    {
        _access = access;
        _ownedTransaction = transaction;
    }

    /// <summary>Gets whether a write requires commercial checks.</summary>
    public bool Enforced => _access.Enforced;

    /// <summary>Gets the exact financial counting scope from this write's already-bound access snapshot.</summary>
    public QuotaScope FinancialScope => _access.Context is { } context
        ? new(context.Scope.Kind == EntitlementScopeKind.PersonalProfile ? QuotaScopeKind.PersonalProfile : QuotaScopeKind.SharedSpace, context.Scope.Id)
        : throw new InvalidOperationException("Inactive writes have no commercial counting scope.");

    /// <summary>Captures the actual file and acquires its write transaction before any quota-sensitive reads.</summary>
    public static async Task<CommercialWriteTransaction> OpenAsync(ZananceDbContext db,
        ICommercialWriteAccessSource source, CancellationToken cancellationToken, bool requireTransaction = false)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(source);
        var path = Path.GetFullPath(db.Database.GetDbConnection().DataSource);
        var access = new CommercialFileAccess(source, path);
        if (!access.Enforced && !requireTransaction) return new(access, null);
        // Microsoft.Data.Sqlite's serializable writer transaction protects count and save across factories/processes.
        // The independent-provider contention test verifies this boundary with the actual SQLite implementation.
        var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var result = new CommercialWriteTransaction(access, transaction);
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

    /// <summary>Gets capacity after the caller checks its specific operation, so retained corrections keep their rights.</summary>
    public int? GetMaximum(QuotaKind kind)
    {
        EnsureCurrent();
        return Enforced ? QuotaPolicy.Get(kind, _access.Context!).Maximum : null;
    }

    /// <summary>Checks the operation even when it adds no quota item; a paid right never substitutes for membership.</summary>
    public void DemandFeature(CommercialFeature feature)
    {
        _access.DemandFeature(feature);
    }

    /// <summary>Rejects an addition against the actual transaction-local count.</summary>
    public void DemandCount(CommercialFeature feature, QuotaKind kind, int maximum, int current, int requested = 1)
    {
        EnsureCurrent();
        if (!new Quota(maximum).CanAdd(current, requested))
            throw new CommercialWriteRejectedException(feature, kind, maximum, current, requested);
    }

    /// <summary>Checks new money against original account states and the same writer's explicit scoped choice.</summary>
    public void DemandSelectedAccounts(CommercialFeature feature, IReadOnlyDictionary<Guid, Account> accounts, IEnumerable<Guid> requestedIds)
    {
        DemandFeature(feature);
        if (!Enforced) return;
        var scope = FinancialScope;
        var availability = ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, scope, _access.Context!,
            accounts.Values.Select(account => QuotaItem.From(scope, account)), _access.AccountSelection);
        foreach (var id in requestedIds.Distinct())
        {
            // Missing/archived accounts retain their established ledger validation, instead of a misleading tier error.
            if (accounts.TryGetValue(id, out var account) && !account.IsArchived && !availability.IsSelected(id))
                throw new CommercialWriteRejectedException(feature, QuotaKind.FinancialAccounts, id, availability.RequiresSelection);
        }
    }

    /// <summary>Checks new automated plan work against original states and the explicit exact-scope choice.</summary>
    public void DemandSelectedPlan(CommercialFeature feature, IEnumerable<Schedule> plans, Guid requestedId)
    {
        DemandFeature(feature);
        if (!Enforced) return;
        var scope = FinancialScope;
        var availability = ResourceSelectionPolicy.Resolve(QuotaKind.RecurringPlans, scope, _access.Context!,
            plans.Select(plan => QuotaItem.From(scope, plan)), _access.PlanSelection);
        if (!availability.IsSelected(requestedId))
            throw new CommercialWriteRejectedException(feature, QuotaKind.RecurringPlans, requestedId, availability.RequiresSelection);
    }

    /// <summary>Checks new occurrence work, resolving original slices through their actual unique selected continuation.</summary>
    public void DemandPlanOccurrence(IReadOnlyList<Schedule> plans, Guid requestedId)
    {
        DemandFeature(CommercialFeature.BasicPlans);
        if (!Enforced) return;
        var plan = plans.FirstOrDefault(item => item.Id == requestedId)
            ?? throw new InvalidOperationException("The plan no longer exists; review the payment again.");
        if (PlanWorkPolicy.RequiresAdvancedRule(plan)) DemandFeature(CommercialFeature.AdvancedPlans);
        var scope = FinancialScope;
        var work = PlanWorkPolicy.Resolve(_access.Context!, scope, plans, _access.PlanSelection);
        if (!work.CanGenerate(requestedId))
        {
            var availability = ResourceSelectionPolicy.Resolve(QuotaKind.RecurringPlans, scope, _access.Context!,
                plans.Select(item => QuotaItem.From(scope, item)), _access.PlanSelection);
            throw new CommercialWriteRejectedException(CommercialFeature.BasicPlans, QuotaKind.RecurringPlans,
                requestedId, availability.RequiresSelection);
        }
    }

    /// <summary>Rejects a changed entitlement/scope snapshot before saving rather than using retired profile rights.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureCurrent();
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
