using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original rule definitions, category references, file and cached access from one actual reader.</summary>
public sealed class CategoryRulesReadSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    /// <summary>Gets the first original file; explicit Retry may not adopt another profile.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets all original categories, including archived references used by retained definitions.</summary>
    public IReadOnlyList<Category> Categories => Inputs.Categories;
    /// <summary>Gets every original definition without filtering stored user data by paid capability.</summary>
    public IReadOnlyList<CategoryRule> Rules => Inputs.Rules;
    /// <summary>Gets the complete original detached identity packet.</summary>
    internal CategoryRulesReadInputs Inputs { get; }
    /// <summary>Captures original source identity on the read worker without exporting its values.</summary>
    internal CategoryRulesReadSnapshot(string path, CommercialFileAccess access, CategoryRulesReadInputs inputs)
    { DatabasePath = path; _access = access; Inputs = inputs; _stamp = Stamp(inputs); }
    /// <summary>Rejects retired cached access before original native work becomes interactive.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();
    /// <summary>Rejects a foreign actual file or changed detached original payload.</summary>
    internal void RequireOriginal(ZananceDbContext db)
    {
        _access.RequireFile(DatabaseFileIdentity.ConfiguredPath(db));
        if (_stamp != Stamp(Inputs)) throw new InvalidOperationException("The original categorization rule inputs changed.");
    }
    /// <summary>Checks fresh complete persisted categories and definitions, independently of UI ordering.</summary>
    internal bool Matches(CategoryRulesReadInputs fresh) => _stamp == Stamp(fresh);
    private static string Stamp(CategoryRulesReadInputs inputs)
    {
        using var hash = SHA256.Create();
        using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, inputs, CategoryRulesReadJson.Default.CategoryRulesReadInputs);
        output.FlushFinalBlock(); return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Only original inputs needed by the rule form; reading does not create settings or touch saved entries.</summary>
internal sealed record CategoryRulesReadInputs(List<Category> Categories, List<CategoryRule> Rules);
/// <summary>Explicit AOT metadata preserves all original category and rule fields.</summary>
[JsonSerializable(typeof(CategoryRulesReadInputs))]
internal sealed partial class CategoryRulesReadJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Reads original categories and definitions on one captured worker lease and deferred transaction.</summary>
    public async Task<CategoryRulesReadSnapshot> ReadCategoryRulesSnapshotAsync(Func<string, bool>? acceptsFile = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original categorization rule file changed.");
        var access = new CommercialFileAccess(commercialAccess, path);
        return await Task.Run(async () =>
        {
            access.EnsureSnapshotCurrent();
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var inputs = await ReadCategoryRulesInputsAsync(db, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent();
            return new CategoryRulesReadSnapshot(path, access, inputs);
        }, cancellationToken);
    }

    /// <summary>Validates complete original references and definitions before publication or a native continuation.</summary>
    public async Task ValidateCategoryRulesSnapshotAsync(CategoryRulesReadSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            snapshot.RequireOriginal(db);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            if (!snapshot.Matches(await ReadCategoryRulesInputsAsync(db, cancellationToken)))
                throw new InvalidOperationException("The original categorization rule source changed.");
            cancellationToken.ThrowIfCancellationRequested(); snapshot.RequireOriginal(db);
        }, cancellationToken);
    }

    private static async Task<CategoryRulesReadInputs> ReadCategoryRulesInputsAsync(ZananceDbContext db, CancellationToken ct) => new(
        await db.Categories.AsNoTracking().OrderBy(category => category.Kind).ThenBy(category => category.SortOrder).ToListAsync(ct),
        await db.CategoryRules.AsNoTracking().ToListAsync(ct));

    private static void RequireCategoryRulesOwner(CategoryRulesReadSnapshot source, ZananceDbContext db, Func<bool> isCurrent)
    {
        if (!isCurrent()) throw new OperationCanceledException("The original categorization rule owner retired.");
        source.RequireOriginal(db);
    }

    private static async Task ValidateCategoryRulesWriterAsync(CategoryRulesReadSnapshot source, ZananceDbContext db, CancellationToken ct)
    {
        source.RequireOriginal(db);
        if (!source.Matches(await ReadCategoryRulesInputsAsync(db, ct)))
            throw new InvalidOperationException("The original categorization rule source changed before writing.");
    }

    /// <summary>Submits an owned detached rule through the original replacement/correction writer on a worker.</summary>
    public async Task<Guid> SaveReviewedCategoryRuleAsync(CategoryRulesReadSnapshot source, CategoryRule rule,
        Func<bool> isCurrent, CancellationToken cancellationToken = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(rule); ArgumentNullException.ThrowIfNull(isCurrent);
        if (rule.Match.Trim().Length < CategoryRules.MinLength)
            throw new ArgumentException("The text of a rule is too short.", nameof(rule));
        var input = (rule.Id, rule.Match, rule.CategoryId, rule.Kind, rule.CreatedAt, rule.UpdatedAt);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            RequireCategoryRulesOwner(source, db, isCurrent);
            if (!source.Categories.Any(category => category.Id == input.CategoryId && category.Kind == input.Kind
                    && !category.IsArchived && category.SystemKey != DefaultCategories.Uncategorized))
                throw new InvalidOperationException("Choose an original available rule category.");
            // The original writer may trim/audit its own copy; rejected work never edits the caller's draft.
            var target = new CategoryRule { Match = input.Match, CategoryId = input.CategoryId, Kind = input.Kind,
                CreatedAt = input.CreatedAt, UpdatedAt = input.UpdatedAt };
            db.Entry(target).Property(item => item.Id).CurrentValue = input.Id;
            await SaveCategoryRuleCoreAsync(target, cancellationToken, isCurrent: isCurrent, accepted: accepted,
                rulesOriginal: source, capturedContext: db);
            return target.Id;
        }, cancellationToken);
    }

    /// <summary>Deletes an original reviewed definition through the same rights policy and actual rollback-capable writer.</summary>
    public async Task DeleteReviewedCategoryRuleAsync(CategoryRulesReadSnapshot source, Guid id,
        Func<bool> isCurrent, CancellationToken cancellationToken = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(isCurrent);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(() => DeleteCategoryRuleCoreAsync(id, cancellationToken, source, isCurrent, accepted, db), cancellationToken);
    }
}
