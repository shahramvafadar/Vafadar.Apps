using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Minimal original active account identity for the CSV mapping selector.</summary>
public sealed record DataFileAccount(Guid Id, string Name, string CurrencyCode);

/// <summary>Original CSV choices, feature visibility and durable batch identities read from one actual file.</summary>
public sealed class DataFileWorkSnapshot
{
    private readonly CommercialFileAccess access;
    private readonly IReadOnlyDictionary<Guid, string> journals;
    /// <summary>Gets the private original file identity, never a portable disclosure field.</summary>
    internal string Path { get; }
    /// <summary>Gets the original preference-row identity across read-only retries.</summary>
    internal Guid SettingsId { get; }
    /// <summary>Captures immutable presentation facts and private original journal evidence, never a financial write grant.</summary>
    internal DataFileWorkSnapshot(string path, Guid settingsId, CommercialFileAccess rights, IEnumerable<DataFileAccount> accounts,
        bool holdings, IEnumerable<ImportBatchInfo> batches, IReadOnlyDictionary<Guid, string> journalPackets)
    { Path = path; SettingsId = settingsId; access = rights; Accounts = Array.AsReadOnly(accounts.ToArray()); CanExportHoldings = holdings;
        Batches = Array.AsReadOnly(batches.ToArray()); journals = journalPackets; }
    /// <summary>Gets complete original active account choices without balances or service metadata.</summary>
    public IReadOnlyList<DataFileAccount> Accounts { get; }
    /// <summary>Gets the original settings/type condition for the existing holdings-export control.</summary>
    public bool CanExportHoldings { get; }
    /// <summary>Gets original durable batches, including consumed aggregates represented by their journals.</summary>
    public IReadOnlyList<ImportBatchInfo> Batches { get; }
    /// <summary>Refuses retired original cached rights without requesting a new permission.</summary>
    public void EnsureCurrent() => access.EnsureSnapshotCurrent();
    /// <summary>Creates opaque original journal/file evidence for one explicit current native Undo.</summary>
    public DataFileUndoReview ReviewUndo(Guid batch)
    {
        EnsureCurrent(); if (!Batches.Any(item => item.BatchId == batch)) throw new InvalidOperationException("The original CSV batch retired.");
        return new(Path, access, batch, journals.GetValueOrDefault(batch));
    }
}

/// <summary>Exact original CSV journal and file, independent of a later native selection or profile.</summary>
public sealed class DataFileUndoReview
{
    private readonly string path;
    private readonly CommercialFileAccess access;
    private readonly Guid batch;
    private readonly string? journal;
    /// <summary>Captures one exact durable original batch journal and cached file rights.</summary>
    internal DataFileUndoReview(string file, CommercialFileAccess rights, Guid id, string? packet)
    { path = file; access = rights; batch = id; journal = packet; }
    /// <summary>Refuses replacement files or retired cached source rights inside the writer.</summary>
    internal void RequireFile(ZananceDbContext db)
    { access.EnsureSnapshotCurrent(); if (path != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The original CSV Undo file changed."); }
    /// <summary>Refuses a replaced original batch or journal rather than manufacturing new consent.</summary>
    internal void RequireBatch(Guid id, string? current)
    { if (batch != id || journal != current) throw new InvalidOperationException("The original CSV Undo journal changed; reload it."); }
}

/// <summary>Complete original bounded-date CSV sources under one read transaction; no temporary output is created.</summary>
public sealed class DataFileLedgerSnapshot
{
    private readonly CommercialFileAccess access;
    /// <summary>Captures the complete original deferred read for the unchanged CSV formatter.</summary>
    internal DataFileLedgerSnapshot(CommercialFileAccess rights, IReadOnlyList<Account> accounts, IReadOnlyList<Category> categories, IReadOnlyList<LedgerEntry> entries)
    { access = rights; Accounts = accounts; Categories = categories; Entries = entries; }
    /// <summary>Gets original active and archived account names/currencies for both sides of transfers.</summary>
    public IReadOnlyList<Account> Accounts { get; }
    /// <summary>Gets complete original category labels.</summary>
    public IReadOnlyList<Category> Categories { get; }
    /// <summary>Gets every mapped original entry field in the unchanged date/creation order.</summary>
    public IReadOnlyList<LedgerEntry> Entries { get; }
    /// <summary>Refuses retired original cached rights before producing or offering output.</summary>
    public void EnsureCurrent() => access.EnsureSnapshotCurrent();
}

/// <summary>Original holdings CSV source, read coherently without changing price, quantity or ledger.</summary>
public sealed class DataFileHoldingSnapshot
{
    private readonly CommercialFileAccess access;
    /// <summary>Captures complete original holding records under one deferred read.</summary>
    internal DataFileHoldingSnapshot(CommercialFileAccess rights, IReadOnlyList<AssetType> types, IReadOnlyList<AssetLocation> locations,
        IReadOnlyList<AssetEvent> events, IReadOnlyList<AssetValuation> values)
    { access = rights; Types = types; Locations = locations; Events = events; Valuations = values; }
    /// <summary>Gets original complete type definitions.</summary>
    public IReadOnlyList<AssetType> Types { get; }
    /// <summary>Gets original complete locations.</summary>
    public IReadOnlyList<AssetLocation> Locations { get; }
    /// <summary>Gets original complete events in their existing date order.</summary>
    public IReadOnlyList<AssetEvent> Events { get; }
    /// <summary>Gets original complete valuation rows.</summary>
    public IReadOnlyList<AssetValuation> Valuations { get; }
    /// <summary>Refuses retired original cached rights without creating new consent.</summary>
    public void EnsureCurrent() => access.EnsureSnapshotCurrent();
}

public sealed partial class ZananceStore
{
    /// <summary>Captures original CSV metadata before optional initial preference creation, then reads its full workspace coherently.</summary>
    public async Task<DataFileWorkSnapshot> ReadDataFileWorkAsync(CancellationToken ct = default, Func<string, bool>? acceptsFile = null,
        DataFileWorkSnapshot? previous = null)
    {
        string? initialPath = previous?.Path; Guid? settingsId = previous?.SettingsId; previous?.EnsureCurrent();
        if (previous is null)
            await GetSettingsCoreAsync(null, ct, (db, settings) => { initialPath = DatabaseFileIdentity.ConfiguredPath(db); settingsId = settings.Id; }, acceptsFile);
        if (initialPath is null || settingsId is null) throw new InvalidOperationException("The original CSV settings source was not captured.");
        await using var db = await contextFactory.CreateDbContextAsync(ct); var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (path != initialPath || acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original CSV source file changed.");
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var settings = await SettingsRowReader.ReadAsync(db, ct) ?? throw new InvalidOperationException("The original CSV settings were removed.");
            if (settings.Id != settingsId) throw new InvalidOperationException("The original CSV settings identity changed.");
            var accounts = await db.Accounts.AsNoTracking().Where(item => !item.IsArchived).OrderBy(item => item.SortOrder).ThenBy(item => item.Name)
                .Select(item => new DataFileAccount(item.Id, item.Name, item.CurrencyCode)).ToArrayAsync(ct);
            var rows = await db.Entries.AsNoTracking().Where(item => item.ImportBatchId != null).ToListAsync(ct);
            var journals = await ReadImportLinksAsync(db, ct); var batches = ProjectImportBatches(rows, journals);
            var holdings = settings.Shows(Feature.HoldingsExport) && await db.AssetTypes.AnyAsync(ct);
            ct.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent();
            if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original CSV source retired.");
            return new DataFileWorkSnapshot(path, settings.Id, access, accounts, holdings, batches,
                journals.ToDictionary(item => item.Batch.Id, item => item.Batch.StateJson));
        }, ct);
    }
    /// <summary>Reads every original CSV source together from the captured file, retaining all32 entry fields and original bounded-range order.</summary>
    public async Task<DataFileLedgerSnapshot> ReadDataFileLedgerAsync(string expectedFile, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct); var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (path != expectedFile) throw new InvalidOperationException("The original CSV export file changed.");
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var accounts = await db.Accounts.AsNoTracking().OrderBy(item => item.IsArchived).ThenBy(item => item.SortOrder).ThenBy(item => item.Name).ToArrayAsync(ct);
            var categories = await db.Categories.AsNoTracking().OrderBy(item => item.Kind).ThenBy(item => item.SortOrder).ToArrayAsync(ct);
            var entries = await db.Entries.AsNoTracking().Where(item => item.Date >= from && item.Date <= to)
                .OrderByDescending(item => item.Date).ThenByDescending(item => item.CreatedAt).ToArrayAsync(ct);
            ct.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent(); return new DataFileLedgerSnapshot(access, accounts, categories, entries);
        }, ct);
    }
    /// <summary>Reads the complete original holdings CSV packet from one actual file without opening a financial writer.</summary>
    public async Task<DataFileHoldingSnapshot> ReadDataFileHoldingsAsync(string expectedFile, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct); var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (path != expectedFile) throw new InvalidOperationException("The original holdings CSV file changed.");
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var types = await db.AssetTypes.AsNoTracking().OrderBy(item => item.IsArchived).ThenBy(item => item.SortOrder).ThenBy(item => item.Name).ToArrayAsync(ct);
            var locations = await db.AssetLocations.AsNoTracking().OrderBy(item => item.IsArchived).ThenBy(item => item.SortOrder).ThenBy(item => item.Name).ToArrayAsync(ct);
            var events = (await db.AssetEvents.AsNoTracking().ToArrayAsync(ct)).OrderBy(item => item.Date).ThenBy(item => item.CreatedAt).ToArray();
            var values = (await db.AssetValuations.AsNoTracking().ToArrayAsync(ct)).OrderByDescending(item => item.Date).ThenByDescending(item => item.CreatedAt).ToArray();
            ct.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent(); return new DataFileHoldingSnapshot(access, types, locations, events, values);
        }, ct);
    }
}
