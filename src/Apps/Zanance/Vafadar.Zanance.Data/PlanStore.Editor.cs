using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class PlanStore
{
    /// <summary>Explicitly refreshes cached authorization only after exact original-file/source comparison; raw fields never change.</summary>
    public async Task<PlanEditorReadSnapshot> RefreshEditorSnapshotAsync(PlanEditorReadSnapshot original, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original); original.EnsureInputsUnchanged();
        if (original.IsPending || original.Accepted) throw new InvalidOperationException("A pending or accepted plan cannot renew its writer.");
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        if (DatabaseFileIdentity.ConfiguredPath(db) != original.DatabasePath)
            throw new PlanEditorSourceChangedException();
        var access = new CommercialFileAccess(commercialAccess, original.DatabasePath); access.DemandFeature(CommercialFeature.History);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var inputs = await ReadEditorInputsAsync(db, access, original.Request, ct); original.ValidateInputs(inputs);
            var result = new PlanEditorReadSnapshot(original.DatabasePath, access, original.Request, inputs);
            result.EnsureCurrent(); ct.ThrowIfCancellationRequested(); return result;
        }, ct);
    }

    /// <summary>Saves only this original plan draft or its verified historical split, consuming acceptance before observers.</summary>
    public async Task<bool> SaveEditorAsync(PlanEditorReadSnapshot original, Schedule? previous, Schedule proposal,
        Func<bool> isCurrent, Action accepted, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original); ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(isCurrent); ArgumentNullException.ThrowIfNull(accepted);
        var before = previous?.CopyForEditing(); var target = proposal.CopyForEditing();
        original.BeginWrite(); var known = false;
        void Accept() { known = true; original.EndWrite(true); accepted(); }
        try
        {
            ValidateOriginalEditorTarget(original, target);
            if (original.Request.PlanId is { } id)
            {
                if (original.HasHistory)
                {
                    if (before is null || before.Id != id || target.Id == id || target.PreviousScheduleId != id
                        || before.ActiveUntil is not { } until || until == DateOnly.MaxValue)
                        throw new InvalidOperationException("An original historical edit requires its distinct linked continuation.");
                    var expected = original.Inputs.Schedule!.CopyForEditing();
                    var expectedNext = PlanActions.SplitFrom(expected, until.AddDays(1));
                    if (target.ActiveFrom != expectedNext.ActiveFrom
                        || System.Text.Json.JsonSerializer.Serialize(expected) != System.Text.Json.JsonSerializer.Serialize(before))
                        throw new InvalidOperationException("The original historical predecessor cannot be rewritten.");
                }
                else if (before is not null || target.Id != id)
                    throw new InvalidOperationException("The original plan edit cannot replace another identity.");
            }
            else if (before is not null || target.PreviousScheduleId is not null)
                throw new InvalidOperationException("A new original plan cannot impersonate a continuation.");
            if (before is not null) return await SaveSplitCoreAsync(before, target, ct, original, isCurrent, Accept);
            await SaveSchedulesCoreAsync([target], ct, original, isCurrent, Accept); return true;
        }
        finally { original.EndWrite(known); }
    }

    private static void ValidateOriginalEditorTarget(PlanEditorReadSnapshot original, Schedule target)
    {
        var account = original.Inputs.Accounts.SingleOrDefault(a => a.Id == target.AccountId);
        var destination = original.Inputs.Accounts.SingleOrDefault(a => a.Id == target.ToAccountId);
        if (string.IsNullOrWhiteSpace(target.Name) || target.Kind is not (EntryKind.Expense or EntryKind.Income or EntryKind.Transfer)
            || !Enum.IsDefined(target.State) || !Enum.IsDefined(target.AmountMode) || account is null
            || account.IsArchived && original.Inputs.Schedule?.AccountId != account.Id
            || target.AmountMode != AmountMode.Unknown && target.Amount is not > 0
            || target.AmountMode == AmountMode.Unknown && target.Amount is not null
            || target.Kind == EntryKind.Transfer && (destination is null || destination.Id == account.Id
                || destination.IsArchived && original.Inputs.Schedule?.ToAccountId != destination.Id
                || target.AmountMode != AmountMode.Unknown && account.CurrencyCode != destination.CurrencyCode && target.ToAmount is not > 0)
            || target.Kind != EntryKind.Transfer && (target.ToAccountId is not null || target.ToAmount is not null)
            || target.CategoryId is { } category && !original.Inputs.Categories.Any(c => c.Id == category
                && c.Kind == (target.Kind == EntryKind.Income ? CategoryKind.Income : CategoryKind.Expense))
            || target.Rule.Start == default || !Enum.IsDefined(target.Rule.Frequency) || !Enum.IsDefined(target.Rule.Calendar)
            || !Enum.IsDefined(target.Rule.End) || !Enum.IsDefined(target.Rule.DayRule) || Recurrence.Validate(target.Rule) is not null
            || target.AutoPost && (target.AmountMode != AmountMode.Fixed || target.AutoPostFrom is null))
            throw new InvalidOperationException("The original plan proposal has unavailable references or invalid monetary/rule facts.");
    }

    private static async Task ValidateSavedEditorRowsAsync(PlanEditorReadSnapshot? original, ZananceDbContext db,
        IReadOnlyList<Schedule> expected, Schedule? continuation, CancellationToken ct)
    {
        if (original is null) return;
        var ids = expected.Select(s => s.Id).ToArray();
        var saved = await db.Schedules.AsNoTracking().Where(s => ids.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
        foreach (var row in expected)
        {
            if (!saved.TryGetValue(row.Id, out var actual)) throw new PlanEditorSourceChangedException();
            // Audit metadata is not a financial proposal; every original monetary/rule/contract field still compares.
            actual.CreatedAt = row.CreatedAt; actual.UpdatedAt = row.UpdatedAt;
            if (System.Text.Json.JsonSerializer.Serialize(actual) != System.Text.Json.JsonSerializer.Serialize(row))
                throw new PlanEditorSourceChangedException();
        }
        var states = await db.OccurrenceStates.AsNoTracking().Where(s => ids.Contains(s.ScheduleId)).ToDictionaryAsync(s => s.Id, ct);
        var entries = await db.Entries.AsNoTracking().Where(e => e.ScheduleId != null && ids.Contains(e.ScheduleId.Value)).ToDictionaryAsync(e => e.Id, ct);
        if (states.Count != original.Inputs.States.Count || entries.Count != original.Inputs.Settlements.Count)
            throw new PlanEditorSourceChangedException();
        foreach (var row in original.Inputs.States)
        {
            var owner = continuation is not null && row.OriginalDate >= continuation.ActiveFrom ? continuation.Id : row.ScheduleId;
            if (!states.TryGetValue(row.Id, out var actual) || actual.ScheduleId != owner) throw new PlanEditorSourceChangedException();
            actual.ScheduleId = row.ScheduleId; actual.CreatedAt = row.CreatedAt; actual.UpdatedAt = row.UpdatedAt;
            if (System.Text.Json.JsonSerializer.Serialize(actual) != System.Text.Json.JsonSerializer.Serialize(row)) throw new PlanEditorSourceChangedException();
        }
        foreach (var row in original.Inputs.Settlements)
        {
            var owner = continuation is not null && row.OccurrenceDate >= continuation.ActiveFrom ? continuation.Id : row.ScheduleId;
            if (!entries.TryGetValue(row.Id, out var actual) || actual.ScheduleId != owner) throw new PlanEditorSourceChangedException();
            actual.ScheduleId = row.ScheduleId; actual.CreatedAt = row.CreatedAt; actual.UpdatedAt = row.UpdatedAt;
            if (System.Text.Json.JsonSerializer.Serialize(actual) != System.Text.Json.JsonSerializer.Serialize(row)) throw new PlanEditorSourceChangedException();
        }
    }

    private static void EnsureOriginalEditorOwner(PlanEditorReadSnapshot? original, ZananceDbContext db, Func<bool>? isCurrent)
    {
        if (original is null) return;
        original.RequireFile(db);
        if (isCurrent?.Invoke() != true || !original.IsPending || original.Accepted)
            throw new InvalidOperationException("The original plan form writer retired.");
    }

    /// <summary>Reads the complete original plan/debt/entry form on a worker without creating display settings.</summary>
    public async Task<PlanEditorReadSnapshot> ReadEditorSnapshotAsync(PlanEditorReadRequest request,
        Func<string, bool>? acceptsFile = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Today == default || new[] { request.PlanId, request.DebtId, request.EntryId }.Count(id => id is not null) > 1
            || new[] { request.PlanId, request.DebtId, request.EntryId }.Any(id => id == Guid.Empty)
            || request.EntryOrigin is not null && (request.PlanId is not null || request.DebtId is not null || request.EntryId is not null))
            throw new ArgumentException("The original plan route is invalid or ambiguous.", nameof(request));
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false || request.EntryOrigin is { } origin && origin.DatabasePath != path)
            throw new InvalidOperationException("The original plan form file changed.");
        var access = new CommercialFileAccess(commercialAccess, path); access.DemandFeature(CommercialFeature.History);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var result = new PlanEditorReadSnapshot(path, access, request, await ReadEditorInputsAsync(db, access, request, ct));
            ct.ThrowIfCancellationRequested(); result.EnsureCurrent(); return result;
        }, ct);
    }

    /// <summary>Revalidates only the original file/source, retaining already published raw plan/reminder input.</summary>
    public async Task ValidateEditorSnapshotAsync(PlanEditorReadSnapshot original, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        original.RequireFile(db);
        await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await ValidateEditorUnderContextAsync(original, db, ct);
        }, ct);
    }

    private async Task ValidateEditorUnderContextAsync(PlanEditorReadSnapshot original, ZananceDbContext db, CancellationToken ct)
    {
        original.RequireFile(db);
        original.Validate(await ReadEditorInputsAsync(db,
            new CommercialFileAccess(commercialAccess, original.DatabasePath), original.Request, ct));
        ct.ThrowIfCancellationRequested(); original.RequireFile(db);
    }

    private async Task<PlanEditorReadInputs> ReadEditorInputsAsync(ZananceDbContext db, CommercialFileAccess access,
        PlanEditorReadRequest request, CancellationToken ct)
    {
        if (request.EntryOrigin is { } origin) await store.ValidateEntryEditorUnderContextAsync(origin, db, ct);
        var accounts = await db.Accounts.AsNoTracking().OrderBy(a => a.Id).ToArrayAsync(ct);
        var categories = await db.Categories.AsNoTracking().OrderBy(c => c.Id).ToArrayAsync(ct);
        var settings = await SettingsRowReader.ReadAsync(db, ct);
        var schedule = request.PlanId is { } id ? await db.Schedules.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct) : null;
        var states = request.PlanId is { } stateId
            ? await db.OccurrenceStates.AsNoTracking().Where(s => s.ScheduleId == stateId).OrderBy(s => s.Id).ToArrayAsync(ct) : [];
        var settlements = request.PlanId is { } settledId
            ? await db.Entries.AsNoTracking().Where(e => e.ScheduleId == settledId).OrderBy(e => e.Id).ToArrayAsync(ct) : [];
        var entry = request.EntryId is { } entryId ? await db.Entries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == entryId, ct) : null;
        var work = await EditorWorkStore.ReadContextAsync(db, access, ct);
        return new(accounts, categories, settings, schedule, states, settlements, entry, work);
    }
}
