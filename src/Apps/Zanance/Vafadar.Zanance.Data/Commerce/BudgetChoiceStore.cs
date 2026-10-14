using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Reviewed canonical budget choices under the exact SQLite writer; selecting never posts money or changes limits.</summary>
public sealed class BudgetChoiceStore(IDbContextFactory<ZananceDbContext> contextFactory,
    ICommercialWriteAccessSource commercialAccess, TimeProvider time)
{
    /// <summary>Raised only after a changed explicit choice commits.</summary>
    public event EventHandler? Changed;

    /// <summary>Reads all original periods and separate live eligibility without creating preferences or paid facts.</summary>
    public async Task<BudgetChoiceSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);
        return await ReadAsync(db, cancellationToken);
    }

    /// <summary>Reads and rechecks complete original budget choices in the caller's captured context.</summary>
    internal async Task<BudgetChoiceSnapshot> ReadAsync(ZananceDbContext db, CancellationToken cancellationToken)
    {
        var path = FilePath(db);
        var access = new CommercialFileAccess(commercialAccess, path);
        access.DemandFeature(CommercialFeature.History);
        var stored = await StoredResourceSelection.ReadAsync(db, access, QuotaKind.BudgetDefinitions, cancellationToken);
        var budgets = await db.Budgets.AsNoTracking().OrderBy(b => b.Id).ToListAsync(cancellationToken);
        var accounts = await ReadAccountsAsync(db, cancellationToken);
        var startDay = await ReadStartDayAsync(db, cancellationToken);
        var today = Today();
        BudgetAvailability? availability = null;
        var work = BudgetWorkPolicy.Inactive;
        if (access.Context is { } context)
        {
            var scope = StoredResourceSelection.Scope(context);
            var selection = stored is null ? access.BudgetSelection : StoredBudgetSelection.ForNewWork(stored);
            availability = BudgetSelectionPolicy.Resolve(context, scope, budgets, today, startDay, selection);
            work = BudgetWorkPolicy.Resolve(context, scope, budgets, today, startDay, selection);
        }
        // A review must never combine an old choice with newly loaded financial inputs or a changed period.
        var current = await StoredResourceSelection.ReadAsync(db, access, QuotaKind.BudgetDefinitions, cancellationToken);
        var reread = await db.Budgets.AsNoTracking().OrderBy(b => b.Id).ToListAsync(cancellationToken);
        var items = budgets.Select(BudgetChoiceItem.From).ToArray();
        if (current?.Revision != stored?.Revision || current?.IdentitySet != stored?.IdentitySet
            || !items.SequenceEqual(reread.Select(BudgetChoiceItem.From))
            || !accounts.SequenceEqual(await ReadAccountsAsync(db, cancellationToken))
            || startDay != await ReadStartDayAsync(db, cancellationToken) || today != Today())
            throw new InvalidOperationException("Budget inputs changed during loading; reload them.");
        access.EnsureSnapshotCurrent();
        return new(items, path, access, stored, today, startDay, availability, work, accounts,
            budgets.Where(budget => BudgetSelectionPolicy.IsCurrent(budget, today, startDay)).Select(budget => budget.Id).ToHashSet(), budgets);
    }

    /// <summary>Rejects a retired read, including same-revision edits, date changes and inactive-to-active transitions, without writes.</summary>
    public async Task ValidateAsync(BudgetChoiceSnapshot reviewed, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await ValidateAsync(reviewed, db, cancellationToken);
    }

    /// <summary>Rechecks original choices and metadata inside the caller's exact SQLite writer.</summary>
    internal async Task ValidateAsync(BudgetChoiceSnapshot reviewed, ZananceDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reviewed);
        if (FilePath(db) != reviewed.DatabasePath) throw new InvalidOperationException("The reviewed budgets belong to another file.");
        reviewed.Access.DemandFeature(CommercialFeature.History);
        reviewed.Access.EnsureSnapshotCurrent();
        var stored = await StoredResourceSelection.ReadAsync(db, reviewed.Access, QuotaKind.BudgetDefinitions, cancellationToken);
        var budgets = await db.Budgets.AsNoTracking().OrderBy(b => b.Id).ToListAsync(cancellationToken);
        if (stored?.Revision != reviewed.Revision || stored?.IdentitySet != reviewed.StoredValue
            || !reviewed.Items.SequenceEqual(budgets.Select(BudgetChoiceItem.From))
            || !reviewed.Items.SequenceEqual(reviewed.Budgets.Select(BudgetChoiceItem.From))
            || !reviewed.Accounts.SequenceEqual(await ReadAccountsAsync(db, cancellationToken))
            || reviewed.MonthStartDay != await ReadStartDayAsync(db, cancellationToken) || reviewed.Today != Today())
            throw new InvalidOperationException("Budget inputs changed during preparation; reload them.");
        reviewed.Access.EnsureSnapshotCurrent();
    }

    // D-146: an opened Android SQLite connection can report an alias rather than its configured absolute file.
    private static string FilePath(ZananceDbContext db) => DatabaseFileIdentity.ConfiguredPath(db);

    /// <summary>Saves only the reviewed current definitions; no financial row or paid capability is rewritten.</summary>
    public async Task<bool> SaveAsync(BudgetChoiceSnapshot reviewed, IEnumerable<BudgetDefinitionKey> selected,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed);
        if (!reviewed.CanChoose) throw new InvalidOperationException("No bounded budget choice is required.");
        var scope = StoredResourceSelection.Scope(reviewed.Access.Context!);
        var choice = new BudgetSelection(scope, selected);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (FilePath(db) != reviewed.DatabasePath)
            throw new InvalidOperationException("The reviewed budgets belong to another file.");
        reviewed.Access.DemandFeature(CommercialFeature.Corrections);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var stored = await StoredResourceSelection.ReadAsync(db, reviewed.Access, QuotaKind.BudgetDefinitions, cancellationToken, tracking: true);
        var budgets = await db.Budgets.AsNoTracking().OrderBy(b => b.Id).ToListAsync(cancellationToken);
        if (stored?.Revision != reviewed.Revision || stored?.IdentitySet != reviewed.StoredValue || !reviewed.Items.SequenceEqual(budgets.Select(BudgetChoiceItem.From))
            || !reviewed.Accounts.SequenceEqual(await ReadAccountsAsync(db, cancellationToken))
            || reviewed.MonthStartDay != await ReadStartDayAsync(db, cancellationToken) || reviewed.Today != Today())
            throw new InvalidOperationException("The reviewed budgets or period changed; reload them before saving.");
        var availability = BudgetSelectionPolicy.Resolve(reviewed.Access.Context!, scope, budgets, reviewed.Today, reviewed.MonthStartDay, choice);
        if (availability.RequiresSelection || choice.Definitions.Count != availability.SelectedDefinitions.Count)
            throw new ArgumentException("Choose only current definitions within capacity.", nameof(selected));
        if (stored is not null && StoredBudgetSelection.IsValid(stored) && stored.IdentitySet == choice.Value)
        { reviewed.Access.EnsureSnapshotCurrent(); return false; }
        if (stored is null)
        {
            stored = new() { Kind = QuotaKind.BudgetDefinitions, ScopeKind = scope.Kind, ScopeId = scope.Id };
            db.ResourceSelections.Add(stored);
        }
        StoredBudgetSelection.Replace(stored, choice);
        reviewed.Access.EnsureSnapshotCurrent();
        await db.SaveChangesAsync(cancellationToken);
        reviewed.Access.EnsureSnapshotCurrent();
        await transaction.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Relays explicit native row choices to their reviewed definitions; row ids never become stored definition identity.</summary>
    public Task<bool> SaveGroupsAsync(BudgetChoiceSnapshot reviewed, IEnumerable<Guid> selectedGroupIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed);
        ArgumentNullException.ThrowIfNull(selectedGroupIds);
        if (!reviewed.CanChoose) throw new InvalidOperationException("No bounded budget choice is required.");
        var ids = selectedGroupIds.Distinct().ToArray();
        var groups = reviewed.Groups.ToDictionary(group => group.RepresentativeId);
        if (ids.Any(id => !groups.ContainsKey(id))) throw new ArgumentException("A choice is not in the reviewed definition list.", nameof(selectedGroupIds));
        var scope = StoredResourceSelection.Scope(reviewed.Access.Context!);
        return SaveAsync(reviewed, ids.Select(id => groups[id].Definition(scope)), cancellationToken);
    }

    private static async Task<List<BudgetChoiceAccount>> ReadAccountsAsync(ZananceDbContext db, CancellationToken cancellationToken) =>
        (await db.Accounts.AsNoTracking().OrderBy(account => account.Id).ToListAsync(cancellationToken))
            .Select(BudgetChoiceAccount.From).ToList();

    private DateOnly Today() => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
    /// <summary>Reads the current financial-month boundary without creating or updating a settings row.</summary>
    internal static async Task<int> ReadStartDayAsync(ZananceDbContext db, CancellationToken cancellationToken) =>
        await db.Settings.Select(s => (int?)s.MonthStartDay).FirstOrDefaultAsync(cancellationToken) ?? 1;
}
