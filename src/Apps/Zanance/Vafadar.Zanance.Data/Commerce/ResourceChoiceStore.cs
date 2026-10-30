using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Explicit actual-file financial resource choices with optimistic review and a serialized SQLite writer.</summary>
public sealed class ResourceChoiceStore(IDbContextFactory<ZananceDbContext> contextFactory,
    ICommercialWriteAccessSource commercialAccess)
{
    /// <summary>Raised only after a changed choice commits; notifications can rebuild from the current file.</summary>
    public event EventHandler? Changed;

    /// <summary>Reads original items and current usable choices without writing or activating commercial enforcement.</summary>
    public async Task<ResourceChoiceSnapshot> ReadAsync(QuotaKind kind, CancellationToken cancellationToken = default)
    {
        DemandKind(kind);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        // Capture the original context before dispatch; SQLite async open/queries can run synchronously.
        return await Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var result = await ReadAsync(kind, db, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested(); return result;
        }, cancellationToken);
    }

    /// <summary>Reads and rechecks the complete choice in an existing original operation's context.</summary>
    /// <remarks>Retains exact-file and current-access checks without reopening an encrypted connection per choice.</remarks>
    internal async Task<ResourceChoiceSnapshot> ReadAsync(QuotaKind kind, ZananceDbContext db, CancellationToken cancellationToken)
    {
        DemandKind(kind);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        var access = new CommercialFileAccess(commercialAccess, path);
        access.DemandFeature(CommercialFeature.History);
        var stored = await StoredResourceSelection.ReadAsync(db, access, kind, cancellationToken);
        var items = await ReadItemsAsync(db, kind, cancellationToken);
        ResourceAvailability? availability = null;
        if (access.Context is { } context)
        {
            var scope = StoredResourceSelection.Scope(context);
            var selection = stored?.ForNewWork() ?? kind switch
            {
                QuotaKind.FinancialAccounts => access.AccountSelection,
                QuotaKind.RecurringPlans => access.PlanSelection,
                QuotaKind.Goals => access.GoalSelection,
                QuotaKind.QuickTemplates or QuotaKind.SavedFilters => null,
                _ => throw new ArgumentOutOfRangeException(nameof(kind)),
            };
            availability = ResourceSelectionPolicy.Resolve(kind, scope, context,
                items.Select(item => new QuotaItem(kind, scope, item.Id, item.State)), selection);
        }
        // A racing chooser save must not publish a mixture of two choices and the reviewed original item list.
        var current = await StoredResourceSelection.ReadAsync(db, access, kind, cancellationToken);
        if (current?.Revision != stored?.Revision || current?.IdentitySet != stored?.IdentitySet
            || !(await ReadItemsAsync(db, kind, cancellationToken)).SequenceEqual(items))
            throw new InvalidOperationException("The resource choice changed during loading; reload it.");
        access.EnsureSnapshotCurrent();
        return new(kind, items, availability, path, access, stored);
    }

    /// <summary>Saves only a current explicit reviewed subset; history, entity states and money are never rewritten.</summary>
    /// <returns>Whether the canonical choice changed and was committed.</returns>
    public async Task<bool> SaveAsync(ResourceChoiceSnapshot reviewed, IEnumerable<Guid> selectedIds,
        CancellationToken cancellationToken = default, SelectionWriteReceipt? receipt = null, Action? validateOriginal = null)
    {
        ArgumentNullException.ThrowIfNull(reviewed);
        ArgumentNullException.ThrowIfNull(selectedIds);
        if (!reviewed.CanChoose) throw new InvalidOperationException("No bounded active-item choice is required.");
        validateOriginal?.Invoke(); receipt?.Begin();
        var choice = new ResourceSelection(reviewed.Kind, StoredResourceSelection.Scope(reviewed.Access.Context!), selectedIds);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (!string.Equals(path, reviewed.DatabasePath, StringComparison.Ordinal))
            throw new InvalidOperationException("The reviewed choice belongs to another opened file.");
        reviewed.Access.EnsureSnapshotCurrent();
        reviewed.Access.DemandFeature(CommercialFeature.Corrections);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var row = await StoredResourceSelection.ReadAsync(db, reviewed.Access, reviewed.Kind, cancellationToken, tracking: true);
        var items = await ReadItemsAsync(db, reviewed.Kind, cancellationToken);
        if (row?.Revision != reviewed.Revision || row?.IdentitySet != reviewed.StoredValue || !items.SequenceEqual(reviewed.Items))
            throw new InvalidOperationException("The reviewed resource list or choice changed; reload it before saving.");
        var availability = ResourceSelectionPolicy.Resolve(reviewed.Kind, choice.Scope, reviewed.Access.Context!,
            items.Select(item => new QuotaItem(reviewed.Kind, choice.Scope, item.Id, item.State)), choice);
        if (availability.RequiresSelection || choice.SelectedIds.Count != availability.SelectedCount)
            throw new ArgumentException("Choose only current eligible items within the available capacity.", nameof(selectedIds));
        var encoded = StoredResourceSelection.Encode(choice);
        if (row is not null && row.HasValidValue() && row.IdentitySet == encoded)
        {
            reviewed.Access.EnsureSnapshotCurrent();
            validateOriginal?.Invoke(); receipt?.Accept(false);
            return false;
        }
        if (row is null)
        {
            row = new() { Kind = choice.Kind, ScopeKind = choice.Scope.Kind, ScopeId = choice.Scope.Id };
            db.ResourceSelections.Add(row);
        }
        row.IdentitySet = encoded;
        row.Revision = Guid.NewGuid();
        reviewed.Access.EnsureSnapshotCurrent();
        validateOriginal?.Invoke();
        await db.SaveChangesAsync(cancellationToken);
        reviewed.Access.EnsureSnapshotCurrent();
        validateOriginal?.Invoke();
        await transaction.CommitAsync(cancellationToken);
        receipt?.Accept(true);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Rechecks an original reviewed list without modifying its entities or persisted choice.</summary>
    public async Task ValidateAsync(ResourceChoiceSnapshot reviewed, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            await ValidateAsync(reviewed, db, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }

    /// <summary>Shares validation with the actual writer, never an unrelated read connection.</summary>
    internal async Task ValidateAsync(ResourceChoiceSnapshot reviewed, ZananceDbContext db, CancellationToken cancellationToken, bool homeRefresh = false)
    {
        ArgumentNullException.ThrowIfNull(reviewed); DemandKind(reviewed.Kind);
        if (DatabaseFileIdentity.ConfiguredPath(db) != reviewed.DatabasePath)
            throw new InvalidOperationException("The reviewed resource list belongs to another opened file.");
        reviewed.Access.EnsureSnapshotCurrent();
        var current = await StoredResourceSelection.ReadAsync(db, reviewed.Access, reviewed.Kind, cancellationToken);
        if (current?.Revision != reviewed.Revision || current?.IdentitySet != reviewed.StoredValue
            || !(await ReadItemsAsync(db, reviewed.Kind, cancellationToken)).SequenceEqual(reviewed.Items))
        {
            reviewed.Access.EnsureSnapshotCurrent();
            throw HomeSnapshotSupersededException.SourceChanged(homeRefresh, "The original resource list or choice changed; reload it.");
        }
        reviewed.Access.EnsureSnapshotCurrent();
    }

    private static async Task<List<ResourceChoiceItem>> ReadItemsAsync(ZananceDbContext db, QuotaKind kind, CancellationToken cancellationToken)
    {
        var items = kind == QuotaKind.QuickTemplates
            ? (await db.Templates.AsNoTracking().ToListAsync(cancellationToken)).Select(template =>
                new ResourceChoiceItem(template.Id, template.Name, QuotaItemState.Active)
                { EntryKind = template.Kind, OriginalPacket = System.Text.Json.JsonSerializer.Serialize(template) })
            : kind == QuotaKind.SavedFilters
            ? (await db.SavedFilters.AsNoTracking().ToListAsync(cancellationToken)).Select(filter =>
                new ResourceChoiceItem(filter.Id, filter.Name, QuotaItemState.Active)
                { OriginalPacket = System.Text.Json.JsonSerializer.Serialize(filter) })
            : kind == QuotaKind.FinancialAccounts
            ? (await db.Accounts.AsNoTracking().ToListAsync(cancellationToken)).Select(account =>
                new ResourceChoiceItem(account.Id, account.Name, account.IsArchived ? QuotaItemState.Archived : QuotaItemState.Active)
                { AccountType = account.Type, CurrencyCode = account.CurrencyCode })
            : kind == QuotaKind.Goals
            ? (await db.Goals.AsNoTracking().ToListAsync(cancellationToken)).Select(goal =>
                new ResourceChoiceItem(goal.Id, goal.Name, goal.State switch
                {
                    Core.Goals.GoalState.Active => QuotaItemState.Active,
                    Core.Goals.GoalState.Paused => QuotaItemState.Paused,
                    Core.Goals.GoalState.Completed => QuotaItemState.Ended,
                    Core.Goals.GoalState.Archived => QuotaItemState.Archived,
                    _ => throw new InvalidOperationException("The original goal state is invalid."),
                })
                { GoalType = goal.Type, CurrencyCode = goal.CurrencyCode })
            : (await db.Schedules.AsNoTracking().ToListAsync(cancellationToken)).Select(plan =>
                new ResourceChoiceItem(plan.Id, plan.Name, plan.State switch
                {
                    ScheduleState.Active => QuotaItemState.Active,
                    ScheduleState.Paused => QuotaItemState.Paused,
                    ScheduleState.Ended => QuotaItemState.Ended,
                    _ => throw new InvalidOperationException("The original plan state is invalid."),
                }) { EntryKind = plan.Kind });
        return items.OrderBy(item => item.Id).ToList();
    }

    private static void DemandKind(QuotaKind kind)
    {
        if (kind is not (QuotaKind.FinancialAccounts or QuotaKind.RecurringPlans or QuotaKind.Goals
            or QuotaKind.QuickTemplates or QuotaKind.SavedFilters))
            throw new ArgumentOutOfRangeException(nameof(kind), "This resource uses its separate selection boundary.");
    }
}
