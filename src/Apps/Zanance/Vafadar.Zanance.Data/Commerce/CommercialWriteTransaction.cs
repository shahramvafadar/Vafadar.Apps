using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>
/// Holds the SQLite write transaction across read/count/check/save. Independent services must compete for the same
/// database writer rather than each using an unrelated in-memory semaphore. Inactive commercial checks never
/// disable the actual household membership/expiry boundary; callers can retain their existing outer transaction.
/// </summary>
internal sealed class CommercialWriteTransaction : IAsyncDisposable
{
    private readonly CommercialFileAccess _access;
    private readonly IDbContextTransaction? _ownedTransaction;
    private readonly HouseholdLocalWritePermission? _household;
    private readonly IDisposable _runtimeScope;
    private ResourceSelection? _accountSelection;
    private ResourceSelection? _planSelection;
    private StoredResourceSelection? _storedAccounts;
    private StoredResourceSelection? _storedPlans;
    private ResourceSelection? _goalSelection;
    private StoredResourceSelection? _storedGoals;
    private StoredResourceSelection? _storedBudgets;
    private BudgetSelection? _budgetSelection;
    private readonly Dictionary<QuotaKind, StoredResourceSelection?> _storedQuick = [];

    /// <summary>Gets this writer's exact verified context, never a newly selected profile.</summary>
    internal CapabilityContext? Context => _access.Context;

    /// <summary>Retains this actual writer's immutable access only for its known committed readback receipt.</summary>
    internal CommercialFileAccess OriginalReadAccess => _access;

    private CommercialWriteTransaction(ZananceDbContext db, CommercialFileAccess access, IDbContextTransaction? transaction, HouseholdLocalWritePermission? household)
    {
        _access = access;
        _ownedTransaction = transaction;
        _household = household;
        _runtimeScope = db.ArmHouseholdEditor(household);
        _accountSelection = access.AccountSelection;
        _planSelection = access.PlanSelection;
        _goalSelection = access.GoalSelection;
    }

    /// <summary>Gets whether a write requires commercial checks.</summary>
    public bool Enforced => _access.Enforced;

    /// <summary>Cost writers require the actual verified household, independently of inactive test-build commercial limits.</summary>
    internal void DemandHousehold()
    {
        EnsureCurrent();
        if (_household is null) throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
        _household.Demand();
    }

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
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        var access = new CommercialFileAccess(source, path);
        // Microsoft.Data.Sqlite's serializable writer transaction protects count and save across factories/processes.
        // The independent-provider contention test verifies this boundary with the actual SQLite implementation.
        var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        CommercialWriteTransaction? result = null;
        try
        {
            var household = await HouseholdJournal.CaptureLocalWritePermissionAsync(db, db.WorkClock, cancellationToken);
            household?.Demand();
            result = new CommercialWriteTransaction(db, access, transaction, household);
            result.EnsureCurrent();
            // D-141: read the explicit choices only after acquiring this actual file's writer. Stored rows override
            // cached fallback choices; a selection never modifies or grants the source's verified paid facts.
            result._storedAccounts = await StoredResourceSelection.ReadAsync(db, access, QuotaKind.FinancialAccounts, cancellationToken, tracking: true);
            result._storedPlans = await StoredResourceSelection.ReadAsync(db, access, QuotaKind.RecurringPlans, cancellationToken, tracking: true);
            result._storedGoals = await StoredResourceSelection.ReadAsync(db, access, QuotaKind.Goals, cancellationToken, tracking: true);
            result._storedBudgets = await StoredResourceSelection.ReadAsync(db, access, QuotaKind.BudgetDefinitions, cancellationToken, tracking: true);
            result._budgetSelection = result._storedBudgets is null ? access.BudgetSelection : StoredBudgetSelection.ForNewWork(result._storedBudgets);
            result._goalSelection = result._storedGoals?.ForNewWork() ?? access.GoalSelection;
            result._accountSelection = result._storedAccounts?.ForNewWork() ?? access.AccountSelection;
            result._planSelection = result._storedPlans?.ForNewWork() ?? access.PlanSelection;
            foreach (var kind in new[] { QuotaKind.QuickTemplates, QuotaKind.SavedFilters })
                result._storedQuick[kind] = await StoredResourceSelection.ReadAsync(db, access, kind, cancellationToken, tracking: true);
            result.EnsureCurrent();
            return result;
        }
        catch
        {
            if (result is not null) await result.DisposeAsync();
            else if (transaction is not null) await transaction.DisposeAsync();
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
            accounts.Values.Select(account => QuotaItem.From(scope, account)), _accountSelection);
        foreach (var id in requestedIds.Distinct())
        {
            // Missing/archived accounts retain their established ledger validation, instead of a misleading tier error.
            if (accounts.TryGetValue(id, out var account) && !account.IsArchived && !availability.IsSelected(id))
                throw new CommercialWriteRejectedException(feature, QuotaKind.FinancialAccounts, id, availability.RequiresSelection || (_storedAccounts is not null && !_storedAccounts.HasValidValue()));
        }
    }

    /// <summary>Checks new automated plan work against original states and the explicit exact-scope choice.</summary>
    public void DemandSelectedPlan(CommercialFeature feature, IEnumerable<Schedule> plans, Guid requestedId)
    {
        DemandFeature(feature);
        if (!Enforced) return;
        var scope = FinancialScope;
        var availability = ResourceSelectionPolicy.Resolve(QuotaKind.RecurringPlans, scope, _access.Context!,
            plans.Select(plan => QuotaItem.From(scope, plan)), _planSelection);
        if (!availability.IsSelected(requestedId))
            throw new CommercialWriteRejectedException(feature, QuotaKind.RecurringPlans, requestedId, availability.RequiresSelection || (_storedPlans is not null && !_storedPlans.HasValidValue()));
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
        var work = PlanWorkPolicy.Resolve(_access.Context!, scope, plans, _planSelection);
        if (!work.CanGenerate(requestedId))
        {
            var availability = ResourceSelectionPolicy.Resolve(QuotaKind.RecurringPlans, scope, _access.Context!,
                plans.Select(item => QuotaItem.From(scope, item)), _planSelection);
            throw new CommercialWriteRejectedException(CommercialFeature.BasicPlans, QuotaKind.RecurringPlans,
                requestedId, availability.RequiresSelection || (_storedPlans is not null && !_storedPlans.HasValidValue()));
        }
    }

    /// <summary>Checks and joins an explicit account creation/unarchive to an existing bounded choice atomically.</summary>
    public async Task PrepareAccountChangeAsync(ZananceDbContext db, Account target, Account? original, CancellationToken cancellationToken)
    {
        if (!Enforced || _storedAccounts is null) return;
        var adds = !target.IsArchived && (original is null || original.IsArchived);
        var ids = _accountSelection!.SelectedIds.ToHashSet();
        if (!_storedAccounts.HasValidValue())
        {
            if (adds && GetMaximum(QuotaKind.FinancialAccounts) is not null)
                throw new CommercialWriteRejectedException(CommercialFeature.FinancialAccounts, QuotaKind.FinancialAccounts, target.Id, true);
            return;
        }
        if (adds && GetMaximum(QuotaKind.FinancialAccounts) is { } maximum)
        {
            var accounts = await db.Accounts.AsNoTracking().ToListAsync(cancellationToken);
            var availability = ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, FinancialScope, _access.Context!,
                accounts.Select(account => QuotaItem.From(FinancialScope, account)), _accountSelection);
            if (availability.RequiresSelection)
                throw new CommercialWriteRejectedException(CommercialFeature.FinancialAccounts, QuotaKind.FinancialAccounts, target.Id, true);
            DemandCount(CommercialFeature.FinancialAccounts, QuotaKind.FinancialAccounts, maximum, availability.SelectedCount);
            ids.Add(target.Id);
        }
        if (target.IsArchived) ids.Remove(target.Id);
        ReplaceChoice(_storedAccounts, new(QuotaKind.FinancialAccounts, FinancialScope, ids));
    }

    /// <summary>Gets whether an existing persisted account choice owns the transaction-local addition check.</summary>
    public bool HasStoredAccountChoice => _storedAccounts is not null;

    /// <summary>Checks net chosen quick-resource creation/replacement/deletion under this same actual-file writer.</summary>
    internal void PrepareQuickResourceChange(QuotaKind kind, IEnumerable<Guid> originalIds, Guid target,
        IReadOnlyList<Guid>? replacedIds = null, bool deleting = false)
    {
        if (!Enforced) return;
        if (kind is not (QuotaKind.QuickTemplates or QuotaKind.SavedFilters)) throw new ArgumentOutOfRangeException(nameof(kind));
        var row = _storedQuick.GetValueOrDefault(kind);
        var originals = originalIds.ToHashSet(); var replaced = replacedIds ?? [];
        if (deleting)
        {
            // Malformed choices retain their exact value until explicit reviewed repair; deletion grants no new work.
            if (row is not null && row.HasValidValue())
                ReplaceChoice(row, new(kind, FinancialScope, row.ToSelection().SelectedIds.Where(id => id != target)));
            return;
        }
        var feature = kind == QuotaKind.QuickTemplates ? CommercialFeature.QuickTemplates : CommercialFeature.SavedFilters;
        var creating = !originals.Contains(target) && replaced.Count == 0;
        DemandFeature(creating ? feature : CommercialFeature.Corrections);
        var choice = row?.ForNewWork();
        var ids = choice?.SelectedIds.ToHashSet();
        if (creating && GetMaximum(kind) is { } maximum)
        {
            var availability = ResourceSelectionPolicy.Resolve(kind, FinancialScope, Context!,
                originals.Select(id => new QuotaItem(kind, FinancialScope, id, QuotaItemState.Active)), choice);
            if (availability.RequiresSelection || row is not null && !row.HasValidValue())
                throw new CommercialWriteRejectedException(feature, kind, target, true);
            DemandCount(feature, kind, maximum, availability.SelectedCount);
        }
        if (row is null || !row.HasValidValue()) return;
        if (creating) ids!.Add(target);
        else if (replaced.Any(id => ids!.Contains(id)))
        {
            ids!.ExceptWith(replaced); ids.Add(target);
        }
        ReplaceChoice(row, new(kind, FinancialScope, ids!));
    }

    /// <summary>Updates a persisted plan choice for explicit creation, ending or a validated slot-preserving split.</summary>
    public async Task PreparePlanChangesAsync(ZananceDbContext db, IReadOnlyList<Schedule> batch,
        Guid? continuationOf, CancellationToken cancellationToken)
    {
        if (!Enforced || _storedPlans is null) return;
        var originals = await db.Schedules.AsNoTracking().ToListAsync(cancellationToken);
        var ids = _planSelection!.SelectedIds.ToHashSet();
        foreach (var plan in batch.Where(plan => plan.State == ScheduleState.Ended)) ids.Remove(plan.Id);
        if (continuationOf is { } prior && _planSelection.SelectedIds.Contains(prior))
        {
            // The caller already verified this exact predecessor/new continuation relationship under this writer.
            var next = batch.Single(plan => plan.PreviousScheduleId == prior);
            if (next.State is ScheduleState.Active or ScheduleState.Paused) ids.Add(next.Id);
        }
        var additions = batch.Where(plan => plan.State is ScheduleState.Active or ScheduleState.Paused)
            .Where(plan => continuationOf is null || plan.PreviousScheduleId != continuationOf
                || originals.FirstOrDefault(original => original.Id == continuationOf) is not { State: ScheduleState.Active or ScheduleState.Paused })
            .Where(plan => originals.FirstOrDefault(original => original.Id == plan.Id) is not { State: ScheduleState.Active or ScheduleState.Paused })
            .Select(plan => plan.Id).ToArray();
        if (additions.Length > 0 && GetMaximum(QuotaKind.RecurringPlans) is { } maximum)
        {
            var availability = ResourceSelectionPolicy.Resolve(QuotaKind.RecurringPlans, FinancialScope, _access.Context!,
                originals.Select(plan => QuotaItem.From(FinancialScope, plan)), _planSelection);
            if (availability.RequiresSelection)
                throw new CommercialWriteRejectedException(CommercialFeature.BasicPlans, QuotaKind.RecurringPlans, additions[0], true);
            DemandCount(CommercialFeature.BasicPlans, QuotaKind.RecurringPlans, maximum, ids.Count, additions.Length);
            ids.UnionWith(additions);
        }
        if (!_storedPlans.HasValidValue())
        {
            if (additions.Length > 0 && GetMaximum(QuotaKind.RecurringPlans) is not null)
                throw new CommercialWriteRejectedException(CommercialFeature.BasicPlans, QuotaKind.RecurringPlans, additions[0], true);
            return;
        }
        ReplaceChoice(_storedPlans, new(QuotaKind.RecurringPlans, FinancialScope, ids));
    }

    /// <summary>Gets whether persisted plan choices own the transaction-local net capacity check.</summary>
    public bool HasStoredPlanChoice => _storedPlans is not null;

    /// <summary>Checks new goal use against original states and this writer's exact-scope choice.</summary>
    public void DemandSelectedGoal(CommercialFeature feature, IEnumerable<Goal> goals, Guid requestedId)
    {
        DemandFeature(feature);
        if (!Enforced) return;
        var availability = ResourceSelectionPolicy.Resolve(QuotaKind.Goals, FinancialScope, _access.Context!,
            goals.Select(goal => QuotaItem.From(FinancialScope, goal)), _goalSelection);
        if (!availability.IsSelected(requestedId))
            throw new CommercialWriteRejectedException(feature, QuotaKind.Goals, requestedId,
                availability.RequiresSelection || (_storedGoals is not null && !_storedGoals.HasValidValue()));
    }

    /// <summary>Joins explicit creation/reopening to a free chosen slot and releases completed/archived identities atomically.</summary>
    public async Task PrepareGoalChangeAsync(ZananceDbContext db, Goal target, Goal? original, CancellationToken cancellationToken)
    {
        if (!Enforced || _storedGoals is null) return;
        var counted = target.State is GoalState.Active or GoalState.Paused;
        var adds = counted && original?.State is not (GoalState.Active or GoalState.Paused);
        if (!_storedGoals.HasValidValue())
        {
            if (adds && GetMaximum(QuotaKind.Goals) is not null)
                throw new CommercialWriteRejectedException(CommercialFeature.BasicGoals, QuotaKind.Goals, target.Id, true);
            return;
        }
        var ids = _goalSelection!.SelectedIds.ToHashSet();
        if (adds && GetMaximum(QuotaKind.Goals) is { } maximum)
        {
            var goals = await db.Goals.AsNoTracking().ToListAsync(cancellationToken);
            var availability = ResourceSelectionPolicy.Resolve(QuotaKind.Goals, FinancialScope, _access.Context!,
                goals.Select(goal => QuotaItem.From(FinancialScope, goal)), _goalSelection);
            if (availability.RequiresSelection)
                throw new CommercialWriteRejectedException(CommercialFeature.BasicGoals, QuotaKind.Goals, target.Id, true);
            DemandCount(CommercialFeature.BasicGoals, QuotaKind.Goals, maximum, availability.SelectedCount);
            ids.Add(target.Id);
        }
        if (!counted) ids.Remove(target.Id);
        ReplaceChoice(_storedGoals, new(QuotaKind.Goals, FinancialScope, ids));
    }

    /// <summary>Releases only the explicitly deleted goal identity, preserving malformed input for reviewed repair.</summary>
    public void PrepareGoalDeletion(Guid id)
    {
        if (!Enforced || _storedGoals is null || !_storedGoals.HasValidValue()) return;
        ReplaceChoice(_storedGoals, new(QuotaKind.Goals, FinancialScope, _goalSelection!.SelectedIds.Where(selected => selected != id)));
    }

    /// <summary>Gets whether a persisted goal choice owns the transaction-local addition check.</summary>
    public bool HasStoredGoalChoice => _storedGoals is not null;

    /// <summary>Checks a new current definition and atomically transfers only a retired chosen definition slot.</summary>
    internal void PrepareBudgetChange(IReadOnlyList<Core.Budgets.Budget> originals, Core.Budgets.Budget target,
        Core.Budgets.Budget? original, DateOnly today, int monthStartDay)
    {
        if (!Enforced || !BudgetSelectionPolicy.IsCurrent(target, today, monthStartDay)) return;
        var scope = FinancialScope;
        var key = BudgetDefinitionKey.From(scope, target);
        var availability = BudgetSelectionPolicy.Resolve(Context!, scope, originals, today, monthStartDay, _budgetSelection);
        // Existing same-definition edits are corrections; malformed or obsolete choices do not bar them.
        if (original is not null && BudgetDefinitionKey.From(scope, original) == key
            && BudgetSelectionPolicy.IsCurrent(original, today, monthStartDay)) return;
        if (availability.RequiresSelection || _storedBudgets is not null && !StoredBudgetSelection.IsValid(_storedBudgets)
            && availability.Maximum is not null)
            throw new CommercialWriteRejectedException(CommercialFeature.BasicBudgets, QuotaKind.BudgetDefinitions, target.Id, true);
        var selected = availability.SelectedDefinitions.ToHashSet();
        if (original is not null)
        {
            var previous = BudgetDefinitionKey.From(scope, original);
            if (previous != key && !originals.Any(b => b.Id != original.Id
                && BudgetSelectionPolicy.IsCurrent(b, today, monthStartDay) && BudgetDefinitionKey.From(scope, b) == previous))
                selected.Remove(previous);
        }
        if (!selected.Contains(key) && availability.Maximum is { } maximum)
            DemandCount(CommercialFeature.BasicBudgets, QuotaKind.BudgetDefinitions, maximum, selected.Count);
        selected.Add(key);
        if (_storedBudgets is not null && StoredBudgetSelection.IsValid(_storedBudgets))
            StoredBudgetSelection.Replace(_storedBudgets, new(scope, selected));
    }

    /// <summary>Releases a deleted definition only when its last current period row is removed.</summary>
    internal void PrepareBudgetDeletion(IReadOnlyList<Core.Budgets.Budget> originals, Core.Budgets.Budget removed,
        DateOnly today, int monthStartDay)
    {
        if (!Enforced || _storedBudgets is null || !StoredBudgetSelection.IsValid(_storedBudgets)) return;
        var key = BudgetDefinitionKey.From(FinancialScope, removed);
        if (originals.Any(b => b.Id != removed.Id && BudgetSelectionPolicy.IsCurrent(b, today, monthStartDay)
            && BudgetDefinitionKey.From(FinancialScope, b) == key)) return;
        StoredBudgetSelection.Replace(_storedBudgets, new(FinancialScope, _budgetSelection!.Definitions.Where(k => k != key)));
    }

    private static void ReplaceChoice(StoredResourceSelection row, ResourceSelection choice)
    {
        var encoded = StoredResourceSelection.Encode(choice);
        if (row.IdentitySet == encoded) return;
        row.IdentitySet = encoded;
        row.Revision = Guid.NewGuid();
    }

    /// <summary>Rejects a changed entitlement/scope snapshot before saving rather than using retired profile rights.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureCurrent();
        _household?.Demand();
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
        _runtimeScope.Dispose();
        if (_ownedTransaction is not null) await _ownedTransaction.DisposeAsync();
    }
}
