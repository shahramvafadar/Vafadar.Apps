using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original category definitions, actual file and cached rights for the original overview/editor.</summary>
public sealed class CategoryReadSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    /// <summary>Gets the first actual category file, independently of a later profile choice.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets every original category field, including archived/default/parent/order identities.</summary>
    public IReadOnlyList<Category> Categories { get; }
    /// <summary>Captures complete detached source identity on its original read worker.</summary>
    internal CategoryReadSnapshot(string path, CommercialFileAccess access, IReadOnlyList<Category> categories)
    { DatabasePath = path; _access = access; Categories = categories; _stamp = Stamp(categories); }
    /// <summary>Checks original file, cached access and unmodified detached definitions.</summary>
    internal void RequireOriginal(ZananceDbContext db)
    {
        _access.RequireFile(DatabaseFileIdentity.ConfiguredPath(db));
        if (_stamp != Stamp(Categories)) throw new InvalidOperationException("The original category definitions changed.");
    }
    /// <summary>Checks the complete persisted definition packet without weakening order or field comparisons.</summary>
    internal bool Matches(IReadOnlyList<Category> categories) => _stamp == Stamp(categories);
    /// <summary>Creates a receipt only from the current original writer's own complete post-save definition packet.</summary>
    internal CategoryReadSnapshot AfterOwnWrite(IReadOnlyList<Category> categories) => new(DatabasePath, _access, categories);
    private static string Stamp(IReadOnlyList<Category> categories) => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(categories, CategoryReadJson.Default.IReadOnlyListCategory)));
}

/// <summary>Original completed move and its same-writer category publication, never a newly selected file.</summary>
public sealed record CategoryMoveReceipt(int SortOrder, CategoryReadSnapshot Source, bool Changed);

/// <summary>Exact accepted category identity and the same writer's complete post-save definitions.</summary>
public sealed record CategoryWriteReceipt(Guid CategoryId, CategoryReadSnapshot Source);

/// <summary>Explicit AOT serialization retains every original category field.</summary>
[JsonSerializable(typeof(IReadOnlyList<Category>))]
internal sealed partial class CategoryReadJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Reads only complete original category definitions on one captured worker lease/deferred transaction.</summary>
    public async Task<CategoryReadSnapshot> ReadCategorySnapshotAsync(Func<string, bool>? acceptsFile = null,
        CancellationToken cancellationToken = default, CategoryReadSnapshot? expected = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original category file changed.");
        var access = new CommercialFileAccess(commercialAccess, path);
        return await Task.Run(async () =>
        {
            access.EnsureSnapshotCurrent(); expected?.RequireOriginal(db); await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var categories = await ReadOriginalCategoriesAsync(db, cancellationToken);
            if (expected is not null && !expected.Matches(categories))
                throw new InvalidOperationException("The original category navigation source changed.");
            cancellationToken.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent();
            expected?.RequireOriginal(db);
            return new CategoryReadSnapshot(path, access, categories);
        }, cancellationToken);
    }

    /// <summary>Checks complete fresh definitions and original file/access before publication or native continuation.</summary>
    public async Task ValidateCategorySnapshotAsync(CategoryReadSnapshot source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            source.RequireOriginal(db); await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            await ValidateOriginalCategoriesAsync(source, db, cancellationToken);
        }, cancellationToken);
    }

    private static async Task<List<Category>> ReadOriginalCategoriesAsync(ZananceDbContext db, CancellationToken ct) =>
        await db.Categories.AsNoTracking().OrderBy(category => category.Kind).ThenBy(category => category.SortOrder).ToListAsync(ct);

    private static async Task ValidateOriginalCategoriesAsync(CategoryReadSnapshot source, ZananceDbContext db, CancellationToken ct)
    {
        source.RequireOriginal(db);
        if (!source.Matches(await ReadOriginalCategoriesAsync(db, ct)))
            throw new InvalidOperationException("The original category source changed.");
        ct.ThrowIfCancellationRequested(); source.RequireOriginal(db);
    }

    private static void RequireCategoryOwner(CategoryReadSnapshot source, ZananceDbContext db, Func<bool> current)
    {
        if (!current()) throw new OperationCanceledException("The original category owner retired.");
        source.RequireOriginal(db);
    }

    /// <summary>Saves a private original category draft through the original writer without mutating rejected input.</summary>
    public async Task<CategoryWriteReceipt> SaveReviewedCategoryAsync(CategoryReadSnapshot source, Category input, Guid? originalId,
        Func<bool> current, CancellationToken cancellationToken = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(current);
        var submitted = CopyCategory(input);
        if (originalId is { } id)
        {
            var original = source.Categories.SingleOrDefault(category => category.Id == id);
            if (original is null || submitted.Id != id || submitted.Kind != original.Kind || submitted.SystemKey != original.SystemKey
                || submitted.SortOrder != original.SortOrder || !submitted.CreatedAt.EqualsExact(original.CreatedAt))
                throw new InvalidOperationException("Choose the original category before changing it.");
        }
        else if (source.Categories.Any(category => category.Id == submitted.Id))
            throw new InvalidOperationException("A new category cannot replace an original category.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            RequireCategoryOwner(source, db, current);
            return (await SaveCategoryCoreAsync(submitted, cancellationToken, source, current, accepted, db))!;
        }, cancellationToken);
    }

    /// <summary>Moves the original category and returns its order from the same actual writer and file.</summary>
    public async Task<CategoryMoveReceipt> MoveReviewedCategoryAsync(CategoryReadSnapshot source, Guid id, int step,
        Func<bool> current, CancellationToken cancellationToken = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(current);
        if (!source.Categories.Any(category => category.Id == id)) throw new InvalidOperationException("Choose the original category before moving it.");
        if (step is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(step));
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () => (await MoveCategoryCoreAsync(id, step, cancellationToken, source, current, accepted, db))!, cancellationToken);
    }

    private static Category CopyCategory(Category category) => new(category.Id)
    {
        Kind = category.Kind, SystemKey = category.SystemKey, Name = category.Name, ParentId = category.ParentId,
        Color = category.Color, Icon = category.Icon, SpendingType = category.SpendingType, IsEssential = category.IsEssential,
        IsArchived = category.IsArchived, SortOrder = category.SortOrder, CreatedAt = category.CreatedAt, UpdatedAt = category.UpdatedAt,
    };
}
