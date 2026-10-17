using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data.Analysis;

/// <summary>Local-only account identity for explicit disclosure selection; names/ids never enter the provider DTO.</summary>
public sealed record AiAccount(Guid Id, string Name, string Currency, bool Archived);
/// <summary>One original file's actual selectable accounts; no service query or hidden selection.</summary>
public sealed record AiAccountScope(string File, IReadOnlyList<AiAccount> Accounts);
/// <summary>Exact read-only disclosure inputs, independent from UI language/calendar presentation.</summary>
public sealed record AiSelection(string File, IReadOnlyList<Guid> AccountIds, DateOnly From, DateOnly To,
    bool ConfirmedOnly, bool ComparePrevious, string Language);

/// <summary>Original file/rights/minimal source digest and immutable aggregate payload; no raw notes or blobs retained.</summary>
public sealed class AiReview
{
    private readonly string payload;
    internal AiReview(AiSelection selection, CommercialFileAccess access, string sourceDigest, AiPayload content)
    { Selection = selection; Access = access; SourceDigest = sourceDigest; payload = JsonSerializer.Serialize(content, AiJson.Default.AiPayload); Digest = AiProtocol.Digest(content); }
    /// <summary>Immutable original local scope; never sent to the provider.</summary>
    public AiSelection Selection { get; }
    /// <summary>Returns a fresh limited payload, so external draft mutation cannot replace original consent.</summary>
    public AiPayload Payload => JsonSerializer.Deserialize(payload, AiJson.Default.AiPayload)!;
    /// <summary>Digest of exactly the aggregate disclosure, separate from the private source fingerprint.</summary>
    public string Digest { get; }
    internal CommercialFileAccess Access { get; }
    internal string SourceDigest { get; }
    /// <summary>Rejects retired cached rights even when current test commercial enforcement is inactive.</summary>
    public void EnsureCurrent() { Access.EnsureSnapshotCurrent(); Access.DemandFeature(CommercialFeature.BasicExport); }
}

/// <summary>Aggregate-only analysis sources from one original coherent protected reader; no financial write or raw-data upload.</summary>
public sealed class AiSnapshotStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource access)
{
    /// <summary>Lists original account choices off the UI thread; all selections remain explicit.</summary>
    public async Task<AiAccountScope> AccountsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var path = DatabaseFileIdentity.ConfiguredPath(db);
        return await Task.Run(async () =>
        {
            var rows = await db.Accounts.AsNoTracking().OrderBy(a => a.Name).ThenBy(a => a.Id)
                .Select(a => new AiAccount(a.Id, a.Name, a.CurrencyCode, a.IsArchived)).Take(10001).ToListAsync(ct);
            if (rows.Count > 10000) throw new InvalidOperationException("Analysis account scope exceeds its technical bound.");
            return new AiAccountScope(path, rows);
        }, ct);
    }
    /// <summary>Captures only selected original financial fields and produces the exact preview before any native consent.</summary>
    public async Task<AiReview> ReviewAsync(AiSelection selection, CancellationToken ct = default)
    {
        var frozen = Freeze(selection); await using var db = await factory.CreateDbContextAsync(ct);
        var rights = new CommercialFileAccess(access, DatabaseFileIdentity.ConfiguredPath(db)); rights.EnsureSnapshotCurrent(); rights.DemandFeature(CommercialFeature.BasicExport);
        return await Task.Run(async () =>
        {
            var frame = await ReadAsync(db, frozen, ct); var payload = Build(frozen, frame); rights.EnsureSnapshotCurrent();
            return new AiReview(frozen, rights, SourceDigest(frame), payload);
        }, ct);
    }
    /// <summary>Rechecks the exact original file/minimal source/rights after native awaits, without disclosing another profile.</summary>
    public async Task CheckAsync(AiReview review, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review); review.EnsureCurrent(); await using var db = await factory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            var frame = await ReadAsync(db, review.Selection, ct);
            if (SourceDigest(frame) != review.SourceDigest || AiProtocol.Digest(Build(review.Selection, frame)) != review.Digest)
                throw new InvalidOperationException("The original analysis source changed.");
            review.EnsureCurrent();
        }, ct);
    }
    private static AiSelection Freeze(AiSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (!Path.IsPathFullyQualified(selection.File) || selection.AccountIds is null || selection.AccountIds.Count is < 1 or > 10000 ||
            selection.AccountIds.Any(id => id == Guid.Empty) || selection.AccountIds.Distinct().Count() != selection.AccountIds.Count ||
            selection.From == DateOnly.MinValue || selection.To < selection.From || selection.To.DayNumber - selection.From.DayNumber > 365)
            throw new ArgumentException("An explicit bounded analysis selection is required.");
        if (selection.ComparePrevious && selection.From.DayNumber <= selection.To.DayNumber - selection.From.DayNumber + 1)
            throw new ArgumentException("The comparison period is outside supported dates.");
        return selection with { AccountIds = Array.AsReadOnly(selection.AccountIds.Order().ToArray()) };
    }
    private static async Task<AiSourceFrame> ReadAsync(ZananceDbContext db, AiSelection selection, CancellationToken ct)
    {
        if (DatabaseFileIdentity.ConfiguredPath(db) != selection.File) throw new InvalidOperationException("Original analysis file required.");
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        // A personal export permission is never separate household-provider consent, including restored shared data.
        if (await db.Set<HouseholdLocalBinding>().AnyAsync(ct) || await db.HouseholdContacts.AnyAsync(ct) || await db.HouseholdCosts.AnyAsync(ct))
            throw new InvalidOperationException("Separate shared-data analysis permission is not qualified.");
        var accounts = await db.Accounts.AsNoTracking().Where(a => selection.AccountIds.Contains(a.Id)).OrderBy(a => a.Id)
            .Select(a => new AiSourceAccount(a.Id, a.Name, a.CurrencyCode, a.OpeningDate, a.IsArchived)).ToListAsync(ct);
        if (accounts.Count != selection.AccountIds.Count) throw new InvalidOperationException("The original selected accounts changed.");
        var from = selection.ComparePrevious ? selection.From.AddDays(-(selection.To.DayNumber - selection.From.DayNumber + 1)) : selection.From;
        var rows = await db.Entries.AsNoTracking().Where(e => selection.AccountIds.Contains(e.AccountId) && e.Date >= from && e.Date <= selection.To)
            .OrderBy(e => e.Id).Select(e => new AiSourceEntry(e.Id, e.AccountId, e.Date, e.Kind, e.Amount, e.Review)).Take(100001).ToListAsync(ct);
        if (rows.Count > 100000) throw new InvalidOperationException("Analysis source exceeds its technical row bound.");
        return new(accounts, rows);
    }
    private static AiPayload Build(AiSelection selection, AiSourceFrame frame)
    {
        var accounts = frame.Accounts.Select(a => new Account(a.Id) { Name = a.Name, CurrencyCode = a.Currency,
            OpeningDate = a.OpeningDate, IsArchived = a.Archived }).ToArray();
        var byId = accounts.ToDictionary(a => a.Id);
        var entries = frame.Entries.Select(e => new LedgerEntry(e.Id) { AccountId = e.AccountId, Date = e.Date, Kind = e.Kind, Amount = e.Amount, Review = e.Review }).ToArray();
        AiPeriod Period(DateOnly from, DateOnly to)
        {
            var filter = new LedgerFilter(from, to, selection.AccountIds, selection.ConfirmedOnly);
            var recorded = LedgerCalculator.Totals(accounts, entries, filter).ToDictionary(t => t.CurrencyCode, StringComparer.Ordinal);
            var totals = accounts.Select(a => a.CurrencyCode).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
                .Select(code => recorded.GetValueOrDefault(code) ?? new PeriodTotals(code, 0, 0, 0, 0)).ToArray();
            var unreviewed = LedgerCalculator.Matching(entries, new LedgerFilter(from, to, selection.AccountIds), selection.AccountIds)
                .Count(e => e.Review == ReviewState.Unreviewed && EntryClassification.CountsInResult(e.Kind) && !LedgerCalculator.IsBeforeOpening(e, byId[e.AccountId]));
            return new(from, to, accounts.Length, selection.ConfirmedOnly, totals, unreviewed);
        }
        var current = Period(selection.From, selection.To);
        var previous = selection.ComparePrevious ? Period(selection.From.AddDays(-(selection.To.DayNumber - selection.From.DayNumber + 1)), selection.From.AddDays(-1)) : null;
        var result = new AiPayload(1, selection.ComparePrevious ? AiPurpose.PeriodComparison : AiPurpose.PeriodExplanation, selection.Language, current, previous);
        AiProtocol.Validate(result); return result;
    }
    private static string SourceDigest(AiSourceFrame frame) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(frame, AiSourceJson.Default.AiSourceFrame)));
}

/// <summary>Minimal private source account; its identity/name never enter outgoing aggregate DTOs.</summary>
internal sealed record AiSourceAccount(Guid Id, string Name, string Currency, DateOnly OpeningDate, bool Archived);
/// <summary>Only original calculation/source fingerprint fields, never payees/notes/documents.</summary>
internal sealed record AiSourceEntry(Guid Id, Guid AccountId, DateOnly Date, EntryKind Kind, long Amount, ReviewState Review);
/// <summary>Coherent bounded original sources used for stale-consent refusal only.</summary>
internal sealed record AiSourceFrame(List<AiSourceAccount> Accounts, List<AiSourceEntry> Entries);
/// <summary>Explicit AOT metadata for private local source fingerprints, not network DTOs.</summary>
[JsonSerializable(typeof(AiSourceFrame))]
internal sealed partial class AiSourceJson : JsonSerializerContext;
