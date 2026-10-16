using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Importing;

/// <summary>Coherent original financial inputs used by the bank preset's preview and writer guard.</summary>
public sealed record ImportReviewInputs(List<Account> Accounts, List<Category> Categories, List<LedgerEntry> Entries, List<ImportLinkBatch> Journals);

/// <summary>Original file/rights and financial state, never interchangeable with a subsequently selected profile.</summary>
public sealed class ImportReadReview
{
    private readonly CommercialFileAccess _access;
    private readonly string _path, _packet;
    private Dictionary<Guid, string>? _entries;
    /// <summary>Captures one actual opened file and coherent original read.</summary>
    internal ImportReadReview(string path, CommercialFileAccess access, ImportReviewInputs inputs)
    {
        _path = path; _access = access; Inputs = inputs; _packet = JsonSerializer.Serialize(inputs, ImportReviewJson.Default.ImportReviewInputs);
        ConsumedIds = inputs.Journals.Select(j => JsonSerializer.Deserialize(j.StateJson, ImportLinkJson.Default.ImportLinkState)
            is { Version: 1 } state ? state : throw new InvalidOperationException("Unsupported import journal."))
            .SelectMany(s => s.Adjustments.Where(a => a.After is null).Select(a => a.AggregateId)).ToFrozenSet();
    }
    /// <summary>Gets original financial sources; mutation invalidates the review.</summary>
    public ImportReviewInputs Inputs { get; }
    /// <summary>Gets ids consumed by the original coherent linked-import journal.</summary>
    public IReadOnlySet<Guid> ConsumedIds { get; }
    /// <summary>Retains the exact proposed immutable entries before native confirmation or duplicate choices.</summary>
    public void Bind(IEnumerable<LedgerEntry> entries)
    {
        if (_entries is not null) throw new InvalidOperationException("The original import review is already bound.");
        _entries = entries.ToDictionary(e => e.Id, e => JsonSerializer.Serialize(e, ImportLinkJson.Default.LedgerEntry));
    }
    /// <summary>Rejects retired rights or a changed in-memory original packet without HTTP.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent(); _access.DemandFeature(CommercialFeature.CsvImport);
        if (_packet != JsonSerializer.Serialize(Inputs, ImportReviewJson.Default.ImportReviewInputs)) throw new InvalidOperationException("The original import review was changed.");
    }
    /// <summary>Rechecks exact history and accepted original entries inside the same financial writer transaction.</summary>
    internal void ValidateWriter(ZananceDbContext db, IReadOnlyList<LedgerEntry> entries, ImportReviewInputs current)
    {
        EnsureCurrent();
        if (_entries is null || _path != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The reviewed import belongs to another file.");
        foreach (var entry in entries)
            if (!_entries.TryGetValue(entry.Id, out var packet) || packet != JsonSerializer.Serialize(entry, ImportLinkJson.Default.LedgerEntry))
                throw new InvalidOperationException("An accepted bank row differs from its original review.");
        if (_packet != JsonSerializer.Serialize(current, ImportReviewJson.Default.ImportReviewInputs)) throw new InvalidOperationException("The import history changed; review it again.");
        EnsureCurrent();
    }
    /// <summary>Reads original inputs under the caller's SQLite snapshot or writer.</summary>
    internal static async Task<ImportReviewInputs> ReadInputsAsync(ZananceDbContext db, CancellationToken ct) => new(
        await db.Accounts.AsNoTracking().OrderBy(a => a.Id).ToListAsync(ct),
        await db.Categories.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct),
        await db.Entries.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct),
        await db.ImportLinks.AsNoTracking().OrderBy(j => j.Id).ToListAsync(ct));
}

/// <summary>Explicit trim/AOT metadata for the reviewed financial input packet; no schema change.</summary>
[JsonSerializable(typeof(ImportReviewInputs))]
internal sealed partial class ImportReviewJson : JsonSerializerContext;
