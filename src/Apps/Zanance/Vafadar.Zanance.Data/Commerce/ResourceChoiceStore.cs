using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Explicit, actual-file account and plan choices with optimistic review and a serialized SQLite writer.</summary>
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
        var path = Path.GetFullPath(db.Database.GetDbConnection().DataSource);
        var access = new CommercialFileAccess(commercialAccess, path);
        access.DemandFeature(CommercialFeature.History);
        var stored = await StoredResourceSelection.ReadAsync(db, access, kind, cancellationToken);
        var items = await ReadItemsAsync(db, kind, cancellationToken);
        ResourceAvailability? availability = null;
        if (access.Context is { } context)
        {
            var scope = StoredResourceSelection.Scope(context);
            var selection = stored?.ForNewWork() ?? (kind == QuotaKind.FinancialAccounts ? access.AccountSelection : access.PlanSelection);
            availability = ResourceSelectionPolicy.Resolve(kind, scope, context,
                items.Select(item => new QuotaItem(kind, scope, item.Id, item.State)), selection);
        }
        // A racing chooser save must not publish a mixture of two choices and the reviewed original item list.
        if ((await StoredResourceSelection.ReadAsync(db, access, kind, cancellationToken))?.Revision != stored?.Revision)
            throw new InvalidOperationException("The resource choice changed during loading; reload it.");
        access.EnsureSnapshotCurrent();
        return new(kind, items, availability, path, access, stored);
    }

    /// <summary>Saves only a current explicit reviewed subset; history, entity states and money are never rewritten.</summary>
    /// <returns>Whether the canonical choice changed and was committed.</returns>
    public async Task<bool> SaveAsync(ResourceChoiceSnapshot reviewed, IEnumerable<Guid> selectedIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed);
        ArgumentNullException.ThrowIfNull(selectedIds);
        if (!reviewed.CanChoose) throw new InvalidOperationException("No bounded active-item choice is required.");
        var choice = new ResourceSelection(reviewed.Kind, StoredResourceSelection.Scope(reviewed.Access.Context!), selectedIds);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = Path.GetFullPath(db.Database.GetDbConnection().DataSource);
        if (!string.Equals(path, reviewed.DatabasePath, StringComparison.Ordinal))
            throw new InvalidOperationException("The reviewed choice belongs to another opened file.");
        reviewed.Access.EnsureSnapshotCurrent();
        reviewed.Access.DemandFeature(CommercialFeature.Corrections);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var row = await StoredResourceSelection.ReadAsync(db, reviewed.Access, reviewed.Kind, cancellationToken, tracking: true);
        var items = await ReadItemsAsync(db, reviewed.Kind, cancellationToken);
        if (row?.Revision != reviewed.Revision || !items.SequenceEqual(reviewed.Items))
            throw new InvalidOperationException("The reviewed resource list or choice changed; reload it before saving.");
        var availability = ResourceSelectionPolicy.Resolve(reviewed.Kind, choice.Scope, reviewed.Access.Context!,
            items.Select(item => new QuotaItem(reviewed.Kind, choice.Scope, item.Id, item.State)), choice);
        if (availability.RequiresSelection || choice.SelectedIds.Count != availability.SelectedCount)
            throw new ArgumentException("Choose only current eligible items within the available capacity.", nameof(selectedIds));
        var encoded = StoredResourceSelection.Encode(choice);
        if (row is not null && row.HasValidValue() && row.IdentitySet == encoded)
        {
            reviewed.Access.EnsureSnapshotCurrent();
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
        await db.SaveChangesAsync(cancellationToken);
        reviewed.Access.EnsureSnapshotCurrent();
        await transaction.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private static async Task<List<ResourceChoiceItem>> ReadItemsAsync(ZananceDbContext db, QuotaKind kind, CancellationToken cancellationToken)
    {
        var items = kind == QuotaKind.FinancialAccounts
            ? (await db.Accounts.AsNoTracking().ToListAsync(cancellationToken)).Select(account =>
                new ResourceChoiceItem(account.Id, account.Name, account.IsArchived ? QuotaItemState.Archived : QuotaItemState.Active))
            : (await db.Schedules.AsNoTracking().ToListAsync(cancellationToken)).Select(plan =>
                new ResourceChoiceItem(plan.Id, plan.Name, plan.State switch
                {
                    ScheduleState.Active => QuotaItemState.Active,
                    ScheduleState.Paused => QuotaItemState.Paused,
                    ScheduleState.Ended => QuotaItemState.Ended,
                    _ => throw new InvalidOperationException("The original plan state is invalid."),
                }));
        return items.OrderBy(item => item.Id).ToList();
    }

    private static void DemandKind(QuotaKind kind)
    {
        if (kind is not (QuotaKind.FinancialAccounts or QuotaKind.RecurringPlans))
            throw new ArgumentOutOfRangeException(nameof(kind), "This resource uses its separate selection boundary.");
    }
}
