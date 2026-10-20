using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Confirms only the original current entry's review flag without resubmitting its financial fields.</summary>
    public Task MarkEntryDetailReviewedAsync(EntryDetailSnapshot original, Func<bool> isCurrent, CancellationToken ct = default) =>
        RunEntryDetailWriteAsync(original, async accepted =>
        {
            if (original.Entry?.Review != ReviewState.Unreviewed)
                throw new InvalidOperationException("The original entry no longer needs review.");
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            RequireEntryDetailOwner(original, db, isCurrent);
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
            write.DemandFeature(CommercialFeature.Corrections);
            await ValidateEntryDetailUnderContextAsync(original, db, ct);
            var entry = await db.Entries.SingleAsync(e => e.Id == original.EntryId, ct);
            entry.Review = ReviewState.Confirmed;
            RequireEntryDetailOwner(original, db, isCurrent); await db.SaveChangesAsync(ct);
            RequireEntryDetailOwner(original, db, isCurrent); write.EnsureCurrent();
            await write.CommitAsync(ct); accepted(); CommittedWriteNotification.Publish(OnChanged); return true;
        });

    /// <summary>Deletes only the complete original confirmed detail/group and returns its file-bound short-Undo receipt.</summary>
    public Task<IReadOnlyList<LedgerEntry>> DeleteEntryDetailAsync(EntryDetailSnapshot original, Func<bool> isCurrent,
        CancellationToken ct = default) => RunEntryDetailWriteAsync(original, async accepted =>
        {
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            RequireEntryDetailOwner(original, db, isCurrent);
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
            await ValidateEntryDetailUnderContextAsync(original, db, ct);
            return await DeleteEntriesUnderWriterAsync(db, write, [original.EntryId], ct, committedPublication: true,
                validateReview: () => RequireEntryDetailOwner(original, db, isCurrent), accepted: accepted);
        });

    /// <summary>Creates an explicit rule only from the original retained category and match shown by this detail.</summary>
    public Task SaveEntryDetailRuleAsync(EntryDetailSnapshot original, Func<bool> isCurrent, CancellationToken ct = default) =>
        RunEntryDetailWriteAsync(original, async accepted =>
        {
            var entry = original.Entry ?? throw new InvalidOperationException("The original rule source is missing.");
            var match = (entry.Payee ?? entry.Title)?.Trim();
            if (entry.Kind is not (EntryKind.Income or EntryKind.Expense) || entry.CategoryId is not { } category
                || original.Inputs.Resources.Categories.SingleOrDefault(value => value.Id == category) is not
                    { SystemKey: not Core.Categories.DefaultCategories.Uncategorized }
                || match is null || match.Length < Core.Categories.CategoryRules.MinLength || original.Inputs.HoldingTypeId is not null)
                throw new InvalidOperationException("The original entry cannot supply a category rule.");
            var kind = entry.Kind == EntryKind.Income ? Core.Categories.CategoryKind.Income : Core.Categories.CategoryKind.Expense;
            if (original.Inputs.Rules.Any(rule => rule.Kind == kind && rule.CategoryId == category
                && string.Equals(rule.Match, match, StringComparison.CurrentCultureIgnoreCase)))
                throw new InvalidOperationException("The original category rule already has this meaning.");
            await SaveCategoryRuleCoreAsync(new() { Match = match, CategoryId = category,
                Kind = kind },
                ct, original, isCurrent, accepted);
            return true;
        });

    private static void RequireEntryDetailOwner(EntryDetailSnapshot original, ZananceDbContext db, Func<bool> isCurrent)
    {
        ArgumentNullException.ThrowIfNull(isCurrent); original.RequireFile(db);
        if (!isCurrent()) throw new InvalidOperationException("The original detail action owner retired.");
    }

    private static async Task<T> RunEntryDetailWriteAsync<T>(EntryDetailSnapshot original, Func<Action, Task<T>> write)
    {
        ArgumentNullException.ThrowIfNull(original);
        if (original.Entry is null) throw new InvalidOperationException("The original entry is unavailable.");
        original.BeginWrite(); var accepted = false;
        try
        {
            return await write(() => { accepted = true; original.EndWrite(true); });
        }
        finally { original.EndWrite(accepted); }
    }

    /// <summary>Reads source, references, group, refunds and previews in one captured actual-file worker transaction.</summary>
    public async Task<EntryDetailSnapshot> ReadEntryDetailSnapshotAsync(Guid id, CancellationToken ct = default, string? originalPath = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (originalPath is not null && path != originalPath)
            throw new InvalidOperationException("The original transaction detail belongs to another file.");
        var access = new CommercialFileAccess(commercialAccess, path);
        access.EnsureSnapshotCurrent(); access.DemandFeature(CommercialFeature.History);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var inputs = await ReadEntryDetailInputsAsync(db, id, access, ct);
            var result = new EntryDetailSnapshot(path, access, id, inputs);
            ct.ThrowIfCancellationRequested(); result.EnsureCurrent(); return result;
        }, ct);
    }

    /// <summary>Rechecks one complete original detail in a worker transaction before publication or native action.</summary>
    public async Task ValidateEntryDetailSnapshotAsync(EntryDetailSnapshot original, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            original.RequireFile(db);
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await ValidateEntryDetailUnderContextAsync(original, db, ct);
        }, ct);
    }

    private async Task ValidateEntryDetailUnderContextAsync(EntryDetailSnapshot original, ZananceDbContext db, CancellationToken ct)
    {
        original.RequireFile(db);
        var access = new CommercialFileAccess(commercialAccess, DatabaseFileIdentity.ConfiguredPath(db));
        original.ValidateFacts(await ReadEntryDetailInputsAsync(db, original.EntryId, access, ct));
        ct.ThrowIfCancellationRequested(); original.RequireFile(db);
    }

    private async Task<EntryDetailInputs> ReadEntryDetailInputsAsync(ZananceDbContext db, Guid id,
        CommercialFileAccess access, CancellationToken ct)
    {
        var exists = await db.Entries.AsNoTracking().AnyAsync(e => e.Id == id, ct);
        var resources = await GetQuickResourceReviewAsync(db, exists ? id : null, ct);
        var entry = resources.SourceEntry;
        var related = entry?.GroupId is { } group
            ? await db.Entries.AsNoTracking().Where(e => e.GroupId == group).OrderBy(e => e.Id).ToListAsync(ct)
            : entry is null ? [] : new List<LedgerEntry> { entry };
        var ids = related.Select(e => e.Id).ToArray();
        var paybacks = await db.Entries.AsNoTracking().Where(e => e.RefundOfId != null && ids.Contains(e.RefundOfId.Value))
            .OrderBy(e => e.Date).ThenBy(e => e.Id).ToListAsync(ct);
        var purchase = entry?.RefundOfId is { } purchaseId
            ? await db.Entries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == purchaseId, ct) : null;
        var holding = entry?.GroupId is { } holdingGroup
            ? await db.AssetEvents.AsNoTracking().Where(e => e.GroupId == holdingGroup).OrderBy(e => e.Id)
                .Select(e => (Guid?)e.AssetTypeId).FirstOrDefaultAsync(ct) : null;
        EntryAttachmentReview? attachments = entry is null ? null : new(DatabaseFileIdentity.ConfiguredPath(db),
            access, await EntryAttachmentReview.ReadAsync(db, id, ct));
        var images = new List<EntryDetailImage>();
        if (attachments is not null)
            foreach (var info in attachments.Inputs.Attachments.Where(a => a.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
            {
                var bytes = await db.Attachments.AsNoTracking().Where(a => a.Id == info.Id && a.EntryId == id)
                    .Select(a => a.Data).SingleAsync(ct);
                images.Add(new(info.Id, bytes));
            }
        var rules = await db.CategoryRules.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
        access.EnsureSnapshotCurrent();
        return new(resources, related, paybacks, purchase, holding, attachments, images, rules);
    }
}
