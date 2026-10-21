using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>One original plan route, including explicit debt/entry origins and the captured form date.</summary>
public sealed record PlanEditorReadRequest(DateOnly Today, Guid? PlanId = null, Guid? DebtId = null,
    Guid? EntryId = null, EntryEditorReadSnapshot? EntryOrigin = null);

/// <summary>Detached plan references and original occurrence/settlement facts read through one actual-file transaction.</summary>
public sealed record PlanEditorReadInputs(IReadOnlyList<Account> Accounts, IReadOnlyList<Category> Categories,
    ZananceSettings? Settings, Schedule? Schedule, IReadOnlyList<OccurrenceState> States,
    IReadOnlyList<LedgerEntry> Settlements, LedgerEntry? Entry, EditorWorkReview Work);

/// <summary>The original plan's persisted financial or reference facts changed after form preparation.</summary>
public sealed class PlanEditorSourceChangedException : InvalidOperationException
{
    /// <summary>Identifies stale original facts without exposing financial values or connection details.</summary>
    public PlanEditorSourceChangedException() : base("The original plan editor source changed.") { }
}

/// <summary>Cached original authorization changed; only an explicit same-source review can acquire another writer.</summary>
public sealed class PlanEditorAccessChangedException : InvalidOperationException
{
    /// <summary>Identifies changed cached rights without exposing provider or financial values.</summary>
    public PlanEditorAccessChangedException() : base("The original plan editor authorization changed.") { }
}

/// <summary>Original file, complete source facts and one financial writer, independent of the mutable raw draft.</summary>
public sealed class PlanEditorReadSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    private int _write;
    /// <summary>Captures original immutable authority before any native form publication.</summary>
    internal PlanEditorReadSnapshot(string path, CommercialFileAccess access, PlanEditorReadRequest request, PlanEditorReadInputs inputs)
    { DatabasePath = path; _access = access; Request = request; Inputs = inputs; _stamp = Capture(inputs); }
    /// <summary>Gets the original actual file, including for an unavailable explicitly requested source.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets the original parsed route; an unavailable identity never becomes a new plan.</summary>
    public PlanEditorReadRequest Request { get; }
    /// <summary>Gets the complete detached packet; mutation invalidates this original authority.</summary>
    public PlanEditorReadInputs Inputs { get; }
    /// <summary>Gets the exact requested debt account when preparing an explicit repayment-reminder draft.</summary>
    public Account? Debt => Request.DebtId is { } id ? Inputs.Accounts.SingleOrDefault(a => a.Id == id) : null;
    /// <summary>Gets whether the explicit original sources and their monetary account references are available.</summary>
    public bool IsAvailable => (Request.PlanId is null || Inputs.Schedule is not null)
        && (Request.DebtId is null || Debt is { IsArchived: false } debt && debt.Type.IsDebt())
        && (Request.EntryId is null || Inputs.Entry is not null && Supported(Inputs.Entry.Kind)
            && (Inputs.Entry.Kind != EntryKind.Transfer || Inputs.Entry.ToAccountId is not null) && HasAccounts(Inputs.Entry.AccountId, Inputs.Entry.ToAccountId))
        && (Inputs.Schedule is null || Supported(Inputs.Schedule.Kind)
            && (Inputs.Schedule.Kind != EntryKind.Transfer || Inputs.Schedule.ToAccountId is not null) && HasAccounts(Inputs.Schedule.AccountId, Inputs.Schedule.ToAccountId));
    /// <summary>Gets actual recorded history, including an open moved/overridden occurrence that must not be orphaned.</summary>
    public bool HasHistory => Inputs.States.Count > 0 || Inputs.Settlements.Count > 0;
    /// <summary>Gets known financial acceptance before observers run.</summary>
    public bool Accepted => Volatile.Read(ref _write) == 2;
    /// <summary>Gets whether the original form still owns a pending writer.</summary>
    public bool IsPending => Volatile.Read(ref _write) == 1;
    private bool HasAccounts(Guid account, Guid? destination) => Inputs.Accounts.Any(a => a.Id == account)
        && (destination is null || Inputs.Accounts.Any(a => a.Id == destination));
    private static bool Supported(EntryKind kind) => kind is EntryKind.Expense or EntryKind.Income or EntryKind.Transfer;
    /// <summary>Checks cached original rights during synchronous form publication without another read.</summary>
    public void EnsureAccessCurrent()
    {
        try { _access.EnsureSnapshotCurrent(); }
        catch (InvalidOperationException) { throw new PlanEditorAccessChangedException(); }
    }
    /// <summary>Refuses changed cached rights or in-memory original financial/reference facts.</summary>
    public void EnsureCurrent()
    {
        EnsureAccessCurrent(); EnsureInputsUnchanged();
        try { Inputs.Work.EnsureCurrent(); }
        catch (InvalidOperationException) { throw new PlanEditorAccessChangedException(); }
    }
    /// <summary>Checks original facts for explicit same-file access review without implicitly renewing old rights.</summary>
    internal void EnsureInputsUnchanged()
    {
        Request.EntryOrigin?.EnsureCurrent();
        if (Request.EntryOrigin is { } origin && (!origin.IsAvailable || origin.Accepted || origin.IsPending))
            throw new PlanEditorSourceChangedException();
        if (_stamp != Capture(Inputs)) throw new PlanEditorSourceChangedException();
    }
    /// <summary>Rejects another configured file before any same-id plan/reference query.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        EnsureCurrent();
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original plan form cannot follow another file.");
    }
    /// <summary>Checks exact stored source/reference facts before an original action or financial write.</summary>
    internal void Validate(PlanEditorReadInputs current)
    {
        EnsureCurrent();
        ValidateInputs(current);
    }
    /// <summary>Requires identical original financial/reference facts during explicit authorization review.</summary>
    internal void ValidateInputs(PlanEditorReadInputs current)
    {
        EnsureInputsUnchanged();
        if (_stamp != Capture(current)) throw new PlanEditorSourceChangedException();
    }
    /// <summary>Admits one original writer; accepted plans cannot be submitted again.</summary>
    internal void BeginWrite()
    {
        EnsureCurrent();
        if (!IsAvailable || Interlocked.CompareExchange(ref _write, 1, 0) != 0)
            throw new InvalidOperationException("The original plan form is missing, pending or accepted.");
    }
    /// <summary>Consumes known acceptance or releases only an uncommitted attempt.</summary>
    internal void EndWrite(bool accepted) => Volatile.Write(ref _write, accepted ? 2 : 0);
    private static string Capture(PlanEditorReadInputs inputs) => JsonSerializer.Serialize(new
    { inputs.Accounts, inputs.Categories, inputs.Settings, inputs.Schedule, inputs.States, inputs.Settlements, inputs.Entry });
}
