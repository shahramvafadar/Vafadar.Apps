using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original merge references captured only when the actual original editor requests a merge.</summary>
public sealed class CategoryMergeReview
{
    /// <summary>Gets the original reviewed category definitions, actual file and cached access.</summary>
    public CategoryReadSnapshot Source { get; }
    /// <summary>Gets the exact original source definition selected for merging.</summary>
    public Guid SourceId { get; }
    /// <summary>Gets the exact original target selected before native consent.</summary>
    public Guid TargetId { get; }
    private readonly string _stamp;
    /// <summary>Captures original complete typed references without exporting or normalizing financial values.</summary>
    internal CategoryMergeReview(CategoryReadSnapshot source, Guid sourceId, Guid targetId, CategoryMergeInputs inputs)
    { Source = source; SourceId = sourceId; TargetId = targetId; _stamp = Stamp(inputs); }
    /// <summary>Rejects foreign files, retired access and changed public category definitions.</summary>
    internal void RequireOriginal(ZananceDbContext db)
    {
        // Only the immutable full typed stamp escapes the read worker; no caller can edit or retain the large graph.
        Source.RequireOriginal(db);
    }
    /// <summary>Checks the complete fresh merge packet while the original actual writer remains held.</summary>
    internal bool Matches(CategoryMergeInputs inputs) => _stamp == Stamp(inputs);
    private static string Stamp(CategoryMergeInputs inputs)
    {
        using var hash = SHA256.Create(); using var sink = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(sink, inputs, CategoryMergeJson.Default.CategoryMergeInputs);
        sink.FlushFinalBlock(); return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>All original fields in every source table the original merge can change, including complete ledger payloads.</summary>
internal sealed record CategoryMergeInputs(List<Category> Categories, List<LedgerEntry> Entries,
    List<Schedule> Schedules, List<EntryTemplate> Templates, List<CategoryRule> Rules,
    List<ContributionPlan> Contributions, List<SavedFilter> Filters, List<Budget> Budgets);
/// <summary>Explicit AOT metadata for complete original merge reference packets.</summary>
[JsonSerializable(typeof(CategoryMergeInputs))]
internal sealed partial class CategoryMergeJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Reads complete original merge references on demand from one file and deferred worker transaction.</summary>
    public async Task<CategoryMergeReview> ReadCategoryMergeReviewAsync(CategoryReadSnapshot source, Guid sourceId, Guid targetId,
        Func<bool> current, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(current);
        var original = source.Categories.SingleOrDefault(category => category.Id == sourceId);
        var target = source.Categories.SingleOrDefault(category => category.Id == targetId);
        if (original is null || target is null || sourceId == targetId || original.Kind != target.Kind || target.IsArchived)
            throw new InvalidOperationException("Choose an original available category merge target.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            RequireCategoryOwner(source, db, current); await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var inputs = await ReadCategoryMergeInputsAsync(db, cancellationToken);
            if (!source.Matches(inputs.Categories)) throw new InvalidOperationException("The original category definitions changed before merge consent.");
            cancellationToken.ThrowIfCancellationRequested(); RequireCategoryOwner(source, db, current);
            return new CategoryMergeReview(source, sourceId, targetId, inputs);
        }, cancellationToken);
    }

    /// <summary>Runs the same original merge under its exact reviewed file, full references and actual editor owner.</summary>
    public async Task<CategoryWriteReceipt> MergeReviewedCategoryAsync(CategoryMergeReview review, Func<bool> current,
        CancellationToken cancellationToken = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(current);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () => (await MergeCategoryCoreAsync(review.SourceId, review.TargetId, cancellationToken,
            review, current, accepted, db))!, cancellationToken);
    }

    private static async Task<CategoryMergeInputs> ReadCategoryMergeInputsAsync(ZananceDbContext db, CancellationToken ct) => new(
        await ReadOriginalCategoriesAsync(db, ct), await db.Entries.AsNoTracking().OrderBy(item => item.Id).ToListAsync(ct),
        await db.Schedules.AsNoTracking().OrderBy(item => item.Id).ToListAsync(ct),
        await db.Templates.AsNoTracking().OrderBy(item => item.Id).ToListAsync(ct),
        await db.CategoryRules.AsNoTracking().OrderBy(item => item.Id).ToListAsync(ct),
        await db.ContributionPlans.AsNoTracking().OrderBy(item => item.Id).ToListAsync(ct),
        await db.SavedFilters.AsNoTracking().OrderBy(item => item.Id).ToListAsync(ct),
        await db.Budgets.AsNoTracking().OrderBy(item => item.Id).ToListAsync(ct));

    private static async Task ValidateOriginalCategoryMergeAsync(CategoryMergeReview review, ZananceDbContext db, CancellationToken ct)
    {
        review.RequireOriginal(db);
        if (!review.Matches(await ReadCategoryMergeInputsAsync(db, ct)))
            throw new InvalidOperationException("The original category merge references changed after consent.");
        ct.ThrowIfCancellationRequested(); review.RequireOriginal(db);
    }
}
