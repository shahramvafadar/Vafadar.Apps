using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete detached original inputs for outstanding reimbursements; estimates never replace the ledger.</summary>
public sealed record ReimbursementInputs(List<Account> Accounts, List<Category> Categories, List<LedgerEntry> Entries);

/// <summary>The small original tables whose metadata is independent of the financial ledger revision.</summary>
internal sealed record ReimbursementMetadata(List<Account> Accounts, List<Category> Categories);

/// <summary>One original file, access generation and coherent reimbursement source packet.</summary>
public sealed class ReimbursementReadSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    private readonly string _metadata;
    /// <summary>Gets the original transaction's complete-ledger revision for fresh publication validation.</summary>
    internal string Revision { get; }
    /// <summary>Gets the original actual file; a retained page and Retry cannot switch profiles silently.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets every original field; caller mutation refuses validation instead of granting new authority.</summary>
    public ReimbursementInputs Inputs { get; }
    /// <summary>Captures complete input identities on the original reader worker.</summary>
    internal ReimbursementReadSnapshot(string path, CommercialFileAccess access, string revision, ReimbursementInputs inputs)
    {
        DatabasePath = path; _access = access; Revision = revision; Inputs = inputs;
        _stamp = Capture(inputs); _metadata = Capture(new ReimbursementMetadata(inputs.Accounts, inputs.Categories));
    }
    /// <summary>Checks cached original rights immediately around native publication and retained row commands.</summary>
    public void EnsureCurrent() { _access.EnsureSnapshotCurrent(); _access.DemandFeature(CommercialFeature.History); }
    /// <summary>Refuses an actual file switch before opening another source database.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        EnsureCurrent();
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original reimbursements belong to another file.");
    }
    /// <summary>Validates immutable input authority only on the read worker, outside native collection callbacks.</summary>
    internal void RequireOriginal(ZananceDbContext db)
    {
        RequireFile(db);
        if (_stamp != Capture(Inputs)) throw new InvalidOperationException("The original reimbursement packet changed in memory.");
    }
    /// <summary>Refuses changed original account/category metadata independently of ledger revision triggers.</summary>
    internal void CompareMetadata(ReimbursementMetadata current)
    {
        if (_metadata != Capture(current)) throw new InvalidOperationException("The original reimbursement metadata changed.");
    }
    private static string Capture<T>(T value)
    {
        // D-631: hash all original fields without allocating another full-ledger JSON string.
        using var hash = SHA256.Create(); using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, value, typeof(T), ReimbursementJson.Default);
        output.FlushFinalBlock(); return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Explicit complete-field serialization for Android AOT; no financial payload is exported.</summary>
[JsonSerializable(typeof(ReimbursementInputs))]
[JsonSerializable(typeof(ReimbursementMetadata))]
internal sealed partial class ReimbursementJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Captures the original complete reimbursement inputs from one lease and deferred SQLite transaction.</summary>
    public async Task<ReimbursementReadSnapshot> ReadReimbursementsAsync(CancellationToken ct = default, string? originalFile = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
        if (originalFile is not null && originalFile != path)
            throw new InvalidOperationException("The original reimbursement page cannot read another file.");
        access.EnsureSnapshotCurrent(); access.DemandFeature(CommercialFeature.History);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var revision = await ReadHomeFinancialRevisionAsync(db, ct);
            var accounts = await ReadReimbursementAccountsAsync(db, ct);
            var categories = await ReadReimbursementCategoriesAsync(db, ct);
            var entries = await ReadCompleteHomeLedgerAsync(db, ct);
            ct.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent();
            return new ReimbursementReadSnapshot(path, access, revision, new(accounts, categories, entries));
        }, ct);
    }

    /// <summary>Refuses source edits, foreign files, changed inputs or access before publishing an original read.</summary>
    public async Task ValidateReimbursementsAsync(ReimbursementReadSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            snapshot.RequireOriginal(db);
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            if (snapshot.Revision != await ReadHomeFinancialRevisionAsync(db, ct))
                throw new InvalidOperationException("The original reimbursement ledger changed.");
            snapshot.CompareMetadata(new(await ReadReimbursementAccountsAsync(db, ct), await ReadReimbursementCategoriesAsync(db, ct)));
            ct.ThrowIfCancellationRequested(); snapshot.RequireOriginal(db);
        }, ct);
    }

    private static Task<List<Account>> ReadReimbursementAccountsAsync(ZananceDbContext db, CancellationToken ct) =>
        db.Accounts.AsNoTracking().OrderBy(a => a.IsArchived).ThenBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync(ct);
    private static Task<List<Category>> ReadReimbursementCategoriesAsync(ZananceDbContext db, CancellationToken ct) =>
        db.Categories.AsNoTracking().OrderBy(c => c.Kind).ThenBy(c => c.SortOrder).ToListAsync(ct);
}
