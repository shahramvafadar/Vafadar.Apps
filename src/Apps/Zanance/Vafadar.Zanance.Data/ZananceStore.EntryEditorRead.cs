using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads one original editor source and every required reference through a captured worker transaction.</summary>
    public async Task<EntryEditorReadSnapshot> ReadEntryEditorSnapshotAsync(EntryEditorReadRequest request,
        Func<string, bool>? acceptsFile = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (new[] { request.EditId, request.DuplicateId, request.RefundOfId, request.IncomeId }.Count(id => id is not null) > 1
            || request.SourceId == Guid.Empty || request.TemplateId is not null && request.SourceId is not null
            || request.IncomeId is not null && request.Kind != EntryKind.IncomeReversal)
            throw new ArgumentException("The original editor route is ambiguous.", nameof(request));
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        ct.ThrowIfCancellationRequested();
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original editor file changed.");
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        access.DemandFeature(CommercialFeature.History);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var inputs = await ReadEntryEditorInputsAsync(db, request, true, ct);
            var result = new EntryEditorReadSnapshot(path, access, request, inputs); ct.ThrowIfCancellationRequested();
            result.EnsureCurrent(); return result;
        }, ct);
    }

    /// <summary>Rechecks one exact original source and references without initializing preferences or replacing a raw draft.</summary>
    public async Task ValidateEntryEditorSnapshotAsync(EntryEditorReadSnapshot original, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            original.RequireFile(db); await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await ValidateEntryEditorUnderContextAsync(original, db, ct);
        }, ct);
    }

    internal async Task ValidateEntryEditorUnderContextAsync(EntryEditorReadSnapshot original, ZananceDbContext db, CancellationToken ct)
    {
        original.RequireFile(db);
        original.ValidateFinancial(await ReadEntryEditorInputsAsync(db, original.Request, false, ct));
        ct.ThrowIfCancellationRequested(); original.RequireFile(db);
    }

    private async Task<EntryEditorReadInputs> ReadEntryEditorInputsAsync(ZananceDbContext db,
        EntryEditorReadRequest request, bool suggestions, CancellationToken ct)
    {
        var exists = request.SourceId is { } id && await db.Entries.AsNoTracking().AnyAsync(e => e.Id == id, ct);
        var resources = await GetQuickResourceReviewAsync(db, exists ? request.SourceId : null, ct);
        var source = resources.SourceEntry;
        var related = source?.GroupId is { } group
            ? await db.Entries.AsNoTracking().Where(e => e.GroupId == group).OrderBy(e => e.Id).ToListAsync(ct)
            : source is null ? [] : new List<LedgerEntry> { source };
        var purchaseId = request.RefundOfId ?? source?.RefundOfId;
        var purchase = purchaseId is { } purchaseKey
            ? await db.Entries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == purchaseKey, ct) : null;
        var refundSources = related.Select(e => e.Id).Concat(purchase is null ? [] : new[] { purchase.Id }).Distinct().ToArray();
        var refunds = await db.Entries.AsNoTracking().Where(e => e.RefundOfId != null && refundSources.Contains(e.RefundOfId.Value))
            .OrderBy(e => e.Id).ToListAsync(ct);
        var holding = source?.GroupId is { } sourceGroup
            ? await db.AssetEvents.AsNoTracking().Where(e => e.GroupId == sourceGroup).OrderBy(e => e.Id)
                .Select(e => (Guid?)e.AssetTypeId).FirstOrDefaultAsync(ct) : null;
        var rules = suggestions ? await db.CategoryRules.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct) : [];
        IReadOnlyList<string> tags = [];
        if (suggestions)
        {
            var from = request.Today.AddYears(-2); var to = request.Today.AddYears(1);
            var lists = await db.Entries.AsNoTracking().Where(e => e.Date >= from && e.Date <= to).Select(e => e.Tags).ToListAsync(ct);
            tags = EntryTags.InUseTags(lists);
        }
        resources.TemplateChoice.Access.EnsureSnapshotCurrent(); resources.FilterChoice.Access.EnsureSnapshotCurrent();
        return new(resources, related, purchase, refunds, holding, rules, tags);
    }
}
