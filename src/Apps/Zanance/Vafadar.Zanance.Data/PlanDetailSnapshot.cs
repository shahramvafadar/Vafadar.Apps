using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>One complete original plan-detail read, including original new-work policy and immutable writer facts.</summary>
public sealed class PlanDetailSnapshot
{
    /// <summary>Captures both original packets inside the same actual-file read transaction.</summary>
    internal PlanDetailSnapshot(PlanEditorReadSnapshot source, PlanWorkSnapshot work) { Source = source; Work = work; }
    /// <summary>Gets the original complete account/category/plan/history packet; mutation invalidates its authority.</summary>
    public PlanEditorReadSnapshot Source { get; }
    /// <summary>Gets the original occurrence policy, without changing schedules to represent access limitations.</summary>
    public PlanWorkSnapshot Work { get; }
    /// <summary>Gets whether the original deletion rule permits removing this plan rather than ending it.</summary>
    public bool CanDelete => Source.Inputs.Schedule is not null && Source.Inputs.Settlements.Count == 0
        && !Source.Inputs.States.Any(s => s.Status != OccurrenceStatus.Open);
    /// <summary>Checks cached original access cheaply around native publication, without serializing history on the UI thread.</summary>
    public void EnsureCurrent()
    {
        Source.EnsureAccessCurrent();
        (Work.Access ?? throw new InvalidOperationException("The original plan policy is unavailable.")).EnsureSnapshotCurrent();
    }
    /// <summary>Checks complete caller mutation on a worker before any actual database validation.</summary>
    internal void EnsureInputsUnchanged()
    {
        Source.EnsureCurrent();
        if (PlanWorkSnapshot.CaptureInputs(Work.Schedules, Work.States) != Work.InputStamp)
            throw new PlanEditorSourceChangedException();
        EnsureCurrent();
    }
}

/// <summary>Only the original plan-detail lifecycle operations; a caller cannot supply an arbitrary replacement row.</summary>
public enum PlanDetailOperation
{
    /// <summary>Pauses the original active schedule from its captured local day.</summary>
    Pause,
    /// <summary>Resumes the original paused schedule with the existing linked-continuation policy.</summary>
    Resume,
    /// <summary>Ends the original active schedule, retaining every recorded occurrence.</summary>
    End,
    /// <summary>Removes only the original history-free schedule after explicit original confirmation.</summary>
    Delete,
}

/// <summary>A private prepared original transition with a stable continuation identity and durable known-commit state.</summary>
public sealed class PlanDetailChange
{
    private int _accepted;
    internal PlanDetailChange(PlanDetailSnapshot source, PlanDetailOperation operation, Schedule? previous, Schedule proposal)
    { Source = source; Operation = operation; Previous = previous; Proposal = proposal; }
    internal PlanDetailSnapshot Source { get; }
    internal Schedule? Previous { get; }
    internal Schedule Proposal { get; }
    /// <summary>Gets the original selected lifecycle operation.</summary>
    public PlanDetailOperation Operation { get; }
    /// <summary>Gets the stable actual destination, including an uncommitted resume continuation.</summary>
    public Guid DestinationId => Proposal.Id;
    /// <summary>Gets a known actual commit independently of notification, refresh or native return failures.</summary>
    public bool Accepted => Volatile.Read(ref _accepted) == 1;
    internal void MarkAccepted() => Volatile.Write(ref _accepted, 1);
}

public sealed partial class PlanStore
{
    /// <summary>Reads original detail, history and occurrence-work policy under one actual-file deferred worker transaction.</summary>
    public async Task<PlanDetailSnapshot> ReadDetailSnapshotAsync(Guid planId, DateOnly today,
        Func<string, bool>? acceptsFile = null, CancellationToken ct = default)
    {
        if (planId == Guid.Empty || today == default) throw new ArgumentException("The original plan identity and day are required.");
        await using var db = await contextFactory.CreateDbContextAsync(ct); var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original plan detail cannot follow another file.");
        var access = new CommercialFileAccess(commercialAccess, path); access.DemandFeature(CommercialFeature.History);
        var request = new PlanEditorReadRequest(today, PlanId: planId);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var source = new PlanEditorReadSnapshot(path, access, request, await ReadEditorInputsAsync(db, access, request, ct));
            var result = new PlanDetailSnapshot(source, await GetWorkSnapshotAsync(db, ct));
            ct.ThrowIfCancellationRequested(); result.EnsureInputsUnchanged(); return result;
        }, ct);
    }

    /// <summary>Refuses changed original persisted facts, selection, rights or file before native publication or consent.</summary>
    public async Task ValidateDetailSnapshotAsync(PlanDetailSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot); await using var db = await contextFactory.CreateDbContextAsync(ct);
        snapshot.Source.RequireFile(db);
        await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await ValidateDetailUnderContextAsync(snapshot, db, ct);
        }, ct);
    }

    private async Task ValidateDetailUnderContextAsync(PlanDetailSnapshot snapshot, ZananceDbContext db, CancellationToken ct)
    {
        snapshot.EnsureInputsUnchanged();
        await ValidateEditorUnderContextAsync(snapshot.Source, db, ct);
        await ValidateWorkSnapshotAsync(snapshot.Work, db, ct);
        ct.ThrowIfCancellationRequested(); snapshot.EnsureInputsUnchanged();
    }

    /// <summary>Prepares only an original domain transition, without mutating the reviewed plan or writing any source.</summary>
    public PlanDetailChange PrepareDetailChange(PlanDetailSnapshot snapshot, PlanDetailOperation operation, DateOnly resumeFrom)
    {
        ArgumentNullException.ThrowIfNull(snapshot); snapshot.EnsureInputsUnchanged();
        if (!Enum.IsDefined(operation) || !snapshot.Source.IsAvailable || snapshot.Source.Accepted || snapshot.Source.IsPending
            || snapshot.Source.Request.PlanId is null || snapshot.Source.Inputs.Schedule is not { } original)
            throw new InvalidOperationException("The original plan change is unavailable, pending or accepted.");
        var target = original.CopyForEditing(); Schedule? previous = null;
        switch (operation)
        {
            case PlanDetailOperation.Pause when original.State == ScheduleState.Active:
                PlanActions.Pause(target, snapshot.Source.Request.Today); break;
            case PlanDetailOperation.End when original.State == ScheduleState.Active:
                PlanActions.End(target, snapshot.Source.Request.Today); break;
            case PlanDetailOperation.Resume when original.State == ScheduleState.Paused && resumeFrom != default:
                previous = target; target = PlanActions.Resume(previous, resumeFrom)
                    ?? throw new InvalidOperationException("The original paused plan has no continuation."); break;
            case PlanDetailOperation.Delete when snapshot.CanDelete: break;
            default: throw new InvalidOperationException("The original plan does not permit this lifecycle operation.");
        }
        return new(snapshot, operation, previous, target);
    }

    /// <summary>Commits one prepared original transition through the existing atomic writers and consumes acceptance before observers.</summary>
    public async Task<bool> ApplyDetailChangeAsync(PlanDetailChange change, Func<bool> isCurrent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(change); ArgumentNullException.ThrowIfNull(isCurrent);
        var source = change.Source.Source;
        if (change.Accepted) throw new InvalidOperationException("The original plan change was already accepted.");
        source.BeginWrite(); var known = false;
        void Accept() { known = true; source.EndWrite(true); change.MarkAccepted(); }
        try
        {
            if (!isCurrent()) throw new InvalidOperationException("The original plan-detail owner retired.");
            if (change.Operation == PlanDetailOperation.Delete)
                return await DeleteScheduleCoreAsync(source.Request.PlanId!.Value, ct, change.Source, isCurrent, Accept);
            if (change.Previous is { } previous)
                return await SaveSplitCoreAsync(previous.CopyForEditing(), change.Proposal.CopyForEditing(), ct, source, isCurrent, Accept, change.Source);
            await SaveSchedulesCoreAsync([change.Proposal.CopyForEditing()], ct, source, isCurrent, Accept, change.Source); return true;
        }
        finally { source.EndWrite(known); }
    }
}
