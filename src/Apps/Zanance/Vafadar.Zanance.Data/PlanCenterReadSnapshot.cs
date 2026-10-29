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

/// <summary>Complete small original reference tables; the review badge never requires materializing the ledger.</summary>
internal sealed record PlanCenterMetadata(List<Account> Accounts, List<Category> Categories);

/// <summary>One original plan-center file, complete work/reference inputs and original aggregate review count.</summary>
public sealed class PlanCenterReadSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _metadataStamp;
    internal string FinancialRevision { get; }
    /// <summary>Gets the captured actual file; explicit Retry cannot silently switch profiles.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets the original local day used for every row and native continuation.</summary>
    public DateOnly Today { get; }
    /// <summary>Gets original ordered accounts including archived references.</summary>
    public IReadOnlyList<Account> Accounts { get; }
    /// <summary>Gets original ordered categories including archived references.</summary>
    public IReadOnlyList<Category> Categories { get; }
    /// <summary>Gets original schedules, occurrence states, resource selection and commercial policy.</summary>
    public PlanWorkSnapshot Work { get; }
    /// <summary>Gets the original SQL count, from the same transaction as the displayed plans.</summary>
    public int UnreviewedCount { get; }
    internal PlanCenterReadSnapshot(string path, DateOnly today, CommercialFileAccess access, PlanWorkSnapshot work,
        PlanCenterMetadata metadata, string revision, int count)
    {
        DatabasePath = path; Today = today; _access = access; Work = work; Accounts = metadata.Accounts;
        Categories = metadata.Categories; FinancialRevision = revision; UnreviewedCount = count;
        _metadataStamp = Capture(metadata);
    }
    /// <summary>Checks cached actual-file access cheaply around native publication and command callbacks.</summary>
    public void EnsureCurrent() { _access.EnsureSnapshotCurrent(); _access.DemandFeature(CommercialFeature.History); }
    internal void RequireFile(ZananceDbContext db)
    {
        EnsureCurrent();
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original plan center cannot follow another file.");
    }
    internal void EnsureInputsUnchanged()
    {
        EnsureCurrent();
        CompareMetadata(new(Accounts.ToList(), Categories.ToList()));
        if (PlanWorkSnapshot.CaptureInputs(Work.Schedules, Work.States) != Work.InputStamp)
            throw new InvalidOperationException("The original plan-center packet changed in memory.");
    }
    internal void CompareMetadata(PlanCenterMetadata metadata)
    {
        if (_metadataStamp != Capture(metadata)) throw new InvalidOperationException("The original plan-center references changed.");
    }
    private static string Capture(PlanCenterMetadata metadata)
    {
        using var hash = SHA256.Create(); using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, metadata, PlanCenterJson.Default.PlanCenterMetadata);
        output.FlushFinalBlock(); return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Explicit complete reference fields for trimmed Android AOT, without serializing financial history.</summary>
[JsonSerializable(typeof(PlanCenterMetadata))]
internal sealed partial class PlanCenterJson : JsonSerializerContext;

public sealed partial class PlanStore
{
    /// <summary>Reads original work, references and review aggregate under one original-file worker transaction.</summary>
    public async Task<PlanCenterReadSnapshot> ReadCenterSnapshotAsync(DateOnly today,
        Func<string, bool>? acceptsFile = null, CancellationToken ct = default)
    {
        if (today == default) throw new ArgumentException("The original plan-center day is required.", nameof(today));
        await using var db = await contextFactory.CreateDbContextAsync(ct); var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original plan center cannot read another file.");
        var access = new CommercialFileAccess(commercialAccess, path); access.DemandFeature(CommercialFeature.History);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var revision = await ZananceStore.ReadPlanCenterFinancialRevisionAsync(db, ct);
            var work = await GetWorkSnapshotAsync(db, ct);
            var metadata = await ReadCenterMetadataAsync(db, ct);
            var count = await db.Entries.CountAsync(entry => entry.Review == ReviewState.Unreviewed, ct);
            var result = new PlanCenterReadSnapshot(path, today, access, work, metadata, revision, count);
            ct.ThrowIfCancellationRequested(); result.EnsureInputsUnchanged(); return result;
        }, ct);
    }
    /// <summary>Refuses changed file, complete original work/reference facts or ledger before publication or navigation.</summary>
    public async Task ValidateCenterSnapshotAsync(PlanCenterReadSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot); await using var db = await contextFactory.CreateDbContextAsync(ct);
        snapshot.RequireFile(db);
        await Task.Run(async () =>
        {
            snapshot.EnsureInputsUnchanged();
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            if (snapshot.FinancialRevision != await ZananceStore.ReadPlanCenterFinancialRevisionAsync(db, ct)
                || snapshot.UnreviewedCount != await db.Entries.CountAsync(entry => entry.Review == ReviewState.Unreviewed, ct))
                throw new InvalidOperationException("The original plan-center review source changed.");
            await ValidateWorkSnapshotAsync(snapshot.Work, db, ct);
            snapshot.CompareMetadata(await ReadCenterMetadataAsync(db, ct));
            ct.ThrowIfCancellationRequested(); snapshot.EnsureInputsUnchanged();
        }, ct);
    }
    private static async Task<PlanCenterMetadata> ReadCenterMetadataAsync(ZananceDbContext db, CancellationToken ct) => new(
        await db.Accounts.AsNoTracking().OrderBy(a => a.IsArchived).ThenBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync(ct),
        await db.Categories.AsNoTracking().OrderBy(c => c.Kind).ThenBy(c => c.SortOrder).ToListAsync(ct));
}

public sealed partial class ZananceStore
{
    /// <summary>Shares the existing complete-ledger revision reader; the plan center does not copy or export ledger rows.</summary>
    internal static Task<string> ReadPlanCenterFinancialRevisionAsync(ZananceDbContext db, CancellationToken ct) =>
        ReadHomeFinancialRevisionAsync(db, ct);
}
