using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Captured original writer inputs used only to explain editor readiness, never to grant a write.</summary>
public sealed class EditorWorkReview
{
    private readonly CommercialFileAccess _access;
    private readonly Account[] _accounts;
    private readonly Schedule[] _plans;
    private readonly StoredResourceSelection? _accountChoice;
    private readonly StoredResourceSelection? _planChoice;
    /// <summary>Captures independently retained original rows and choices in one reader.</summary>
    internal EditorWorkReview(CommercialFileAccess access, Account[] accounts, Schedule[] plans,
        StoredResourceSelection? accountChoice, StoredResourceSelection? planChoice)
    { _access = access; _accounts = accounts; _plans = plans; _accountChoice = accountChoice; _planChoice = planChoice; }
    /// <summary>Rejects retired cached facts before the form is uncovered.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();
    /// <summary>Explains the original account create/unarchive/correction decision without applying a form.</summary>
    public WorkRestriction? AccountRestriction(Guid id, bool archived)
    {
        if (_access.Context is not { } context) return null;
        var original = _accounts.SingleOrDefault(row => row.Id == id);
        var feature = original is null ? CommercialFeature.FinancialAccounts : CommercialFeature.Corrections;
        var permission = PlanPolicy.Check(feature, context);
        if (permission != FeaturePermission.Allowed) return new(feature, permission);
        if (archived || original is { IsArchived: false }) return null;
        permission = PlanPolicy.Check(CommercialFeature.FinancialAccounts, context);
        if (permission != FeaturePermission.Allowed) return new(CommercialFeature.FinancialAccounts, permission);
        return Addition(QuotaKind.FinancialAccounts, CommercialFeature.FinancialAccounts,
            _accounts.Select(row => QuotaItem.From(Scope(context), row)), _accountChoice, 1);
    }
    /// <summary>Explains the same original Plan feature and net slot decision, including a verified historical split.</summary>
    public WorkRestriction? PlanRestriction(Schedule target, Guid? continuationOf = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (_access.Context is not { } context) return null;
        var original = _plans.SingleOrDefault(row => row.Id == target.Id);
        var continued = continuationOf is { } id ? _plans.SingleOrDefault(row => row.Id == id) : null;
        if (original is null && continued?.State is ScheduleState.Active or ScheduleState.Paused
            && target.PreviousScheduleId == continued.Id) original = continued;
        var feature = PlanEditPolicy.Feature(target, original); var permission = PlanPolicy.Check(feature, context);
        if (permission != FeaturePermission.Allowed) return new(feature, permission);
        var previous = continued ?? _plans.SingleOrDefault(row => row.Id == target.Id);
        var oldSlot = previous?.State is ScheduleState.Active or ScheduleState.Paused ? 1 : 0;
        var newSlot = target.State is ScheduleState.Active or ScheduleState.Paused ? 1 : 0;
        if (newSlot <= oldSlot) return null;
        return Addition(QuotaKind.RecurringPlans, CommercialFeature.BasicPlans,
            _plans.Select(row => QuotaItem.From(Scope(context), row)), _planChoice, newSlot - oldSlot);
    }
    private WorkRestriction? Addition(QuotaKind kind, CommercialFeature feature, IEnumerable<QuotaItem> originals,
        StoredResourceSelection? stored, int requested)
    {
        var context = _access.Context!; var maximum = QuotaPolicy.Get(kind, context).Maximum;
        if (maximum is null) return null;
        if (stored is not null)
        {
            if (!stored.HasValidValue()) return new(feature, FeaturePermission.Allowed, kind, RequiresSelection: true);
            var availability = ResourceSelectionPolicy.Resolve(kind, Scope(context), context, originals, stored.ForNewWork());
            if (availability.RequiresSelection) return new(feature, FeaturePermission.Allowed, kind, RequiresSelection: true);
            return new Quota(maximum).CanAdd(availability.SelectedCount, requested) ? null
                : new(feature, FeaturePermission.Allowed, kind, maximum, availability.SelectedCount, requested);
        }
        // Without a persisted choice the original writer counts every eligible row, not a cached fallback subset.
        var count = originals.Count(row => row.State is QuotaItemState.Active or QuotaItemState.Paused);
        return new Quota(maximum).CanAdd(count, requested) ? null : new(feature, FeaturePermission.Allowed, kind, maximum, count, requested);
    }
    private static QuotaScope Scope(CapabilityContext context) => new(context.Scope.Kind == EntitlementScopeKind.PersonalProfile
        ? QuotaScopeKind.PersonalProfile : QuotaScopeKind.SharedSpace, context.Scope.Id);
}

/// <summary>Reads coherent original writer inputs; inactive test access performs no financial row or choice read.</summary>
public sealed class EditorWorkStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource source)
{
    /// <summary>Captures actual-file readiness without settings creation, write locks, data mutation or paid activation.</summary>
    public async Task<EditorWorkReview> ReadAsync(CancellationToken cancellationToken = default, Func<string, bool>? acceptsFile = null)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original editor work file changed.");
        var access = new CommercialFileAccess(source, path);
        if (!access.Enforced) { access.EnsureSnapshotCurrent(); return new(access, [], [], null, null); }
        access.DemandFeature(CommercialFeature.History);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
        var accounts = await db.Accounts.AsNoTracking().ToArrayAsync(cancellationToken);
        var plans = await db.Schedules.AsNoTracking().ToArrayAsync(cancellationToken);
        var accountChoice = await StoredResourceSelection.ReadAsync(db, access, QuotaKind.FinancialAccounts, cancellationToken);
        var planChoice = await StoredResourceSelection.ReadAsync(db, access, QuotaKind.RecurringPlans, cancellationToken);
        access.EnsureSnapshotCurrent(); return new(access, accounts, plans, accountChoice, planChoice);
    }
}
