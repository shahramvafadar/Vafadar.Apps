using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>One original occurrence and all display/suggestion inputs from one actual-file read transaction.</summary>
public sealed class OccurrenceReadSnapshot
{
    private readonly string _candidateStamp;
    /// <summary>Captures detached original inputs and their immutable candidate stamp.</summary>
    internal OccurrenceReadSnapshot(PlanEditorReadSnapshot source, DateOnly originalDate, IReadOnlyList<LedgerEntry> candidates)
    {
        Source = source; OriginalDate = originalDate; Candidates = candidates;
        Occurrence = source.Inputs.Schedule is { } plan
            ? Occurrences.Find(plan, source.Inputs.States, originalDate, source.Request.Today) : null;
        _candidateStamp = JsonSerializer.Serialize(candidates);
    }
    /// <summary>Gets the exact original file, complete references, plan/history and cached access.</summary>
    public PlanEditorReadSnapshot Source { get; }
    /// <summary>Gets the route's original date independently of any moved due date.</summary>
    public DateOnly OriginalDate { get; }
    /// <summary>Gets the original resolved occurrence, or null for an unavailable original identity.</summary>
    public Occurrence? Occurrence { get; }
    /// <summary>Gets complete unchanged ledger rows in the original bounded suggestion window.</summary>
    public IReadOnlyList<LedgerEntry> Candidates { get; }
    /// <summary>Checks cached authorization without serializing financial rows on the presentation thread.</summary>
    public void EnsureAccessCurrent() => Source.EnsureAccessCurrent();
    /// <summary>Rejects mutable caller rows before database validation or a bound writer.</summary>
    internal void EnsureUnchanged()
    {
        Source.EnsureCurrent();
        if (_candidateStamp != JsonSerializer.Serialize(Candidates)) throw new PlanEditorSourceChangedException();
    }
    /// <summary>Rejects another configured file or a substituted occurrence record before correction.</summary>
    internal void RequireOriginal(Occurrence occurrence, ZananceDbContext db)
    {
        Source.RequireFile(db);
        if (!ReferenceEquals(Occurrence, occurrence)) throw new InvalidOperationException("The original occurrence review was replaced.");
        EnsureUnchanged();
    }
    /// <summary>Requires the complete original candidate rows and order, including independent tag values.</summary>
    internal void ValidateCandidates(IReadOnlyList<LedgerEntry> candidates)
    {
        EnsureUnchanged();
        if (_candidateStamp != JsonSerializer.Serialize(candidates)) throw new PlanEditorSourceChangedException();
    }
}

public sealed partial class PlanStore
{
    /// <summary>Reads the original occurrence, all references/history and complete candidate rows on one worker/file/transaction.</summary>
    public async Task<OccurrenceReadSnapshot> ReadOccurrenceSnapshotAsync(Guid planId, DateOnly originalDate, DateOnly today,
        Func<string, bool>? acceptsFile = null, CancellationToken ct = default)
    {
        if (planId == Guid.Empty) throw new ArgumentException("An original plan identity is required.", nameof(planId));
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original occurrence cannot follow another file.");
        var access = new CommercialFileAccess(commercialAccess, path); access.DemandFeature(CommercialFeature.History);
        var request = new PlanEditorReadRequest(today, PlanId: planId);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var source = new PlanEditorReadSnapshot(path, access, request, await ReadEditorInputsAsync(db, access, request, ct));
            var occurrence = source.Inputs.Schedule is { } plan ? Occurrences.Find(plan, source.Inputs.States, originalDate, today) : null;
            var snapshot = new OccurrenceReadSnapshot(source, originalDate, await ReadOccurrenceCandidatesAsync(db, occurrence, ct));
            ct.ThrowIfCancellationRequested(); snapshot.EnsureUnchanged(); return snapshot;
        }, ct);
    }

    /// <summary>Refuses current-file, reference, occurrence, candidate or cached-right changes before covered publication.</summary>
    public async Task ValidateOccurrenceSnapshotAsync(OccurrenceReadSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(ct); snapshot.Source.RequireFile(db);
        await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await ValidateOccurrenceUnderContextAsync(snapshot, db, ct);
        }, ct);
    }

    /// <summary>Revalidates the complete original read under the caller's same actual-file transaction.</summary>
    internal async Task ValidateOccurrenceUnderContextAsync(OccurrenceReadSnapshot snapshot, ZananceDbContext db, CancellationToken ct)
    {
        snapshot.Source.RequireFile(db); snapshot.EnsureUnchanged();
        var access = new CommercialFileAccess(commercialAccess, snapshot.Source.DatabasePath);
        snapshot.Source.Validate(await ReadEditorInputsAsync(db, access, snapshot.Source.Request, ct));
        snapshot.ValidateCandidates(await ReadOccurrenceCandidatesAsync(db, snapshot.Occurrence, ct));
        ct.ThrowIfCancellationRequested(); snapshot.EnsureUnchanged();
    }

    private static async Task<IReadOnlyList<LedgerEntry>> ReadOccurrenceCandidatesAsync(ZananceDbContext db, Occurrence? occurrence, CancellationToken ct)
    {
        if (occurrence is not { IsOpen: true }) return [];
        var due = occurrence.DueDate;
        var from = due.AddDays(-Math.Min(10, due.DayNumber));
        var through = due.AddDays(Math.Min(10, DateOnly.MaxValue.DayNumber - due.DayNumber));
        return await db.Entries.AsNoTracking().Where(e => e.Date >= from && e.Date <= through).OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToArrayAsync(ct);
    }
}
