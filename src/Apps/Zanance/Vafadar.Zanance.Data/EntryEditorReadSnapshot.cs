using System.Text.Json;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>One parsed original editor route; explicit unavailable sources never become a blank replacement.</summary>
public sealed record EntryEditorReadRequest(DateOnly Today, Guid? EditId = null, Guid? DuplicateId = null,
    Guid? RefundOfId = null, Guid? IncomeId = null, EntryKind Kind = EntryKind.Expense, Guid? TemplateId = null)
{
    /// <summary>Gets the exact original financial source, independent of later draft changes.</summary>
    public Guid? SourceId => EditId ?? DuplicateId ?? RefundOfId ?? IncomeId;
}

/// <summary>Complete editor source/references, original group and refund facts, and detached display suggestions.</summary>
public sealed record EntryEditorReadInputs(QuickResourceReview Resources, IReadOnlyList<LedgerEntry> Related,
    LedgerEntry? Purchase, IReadOnlyList<LedgerEntry> Paybacks, Guid? HoldingTypeId,
    IReadOnlyList<CategoryRule> Rules, IReadOnlyList<string> Tags);

/// <summary>Persistent original editor facts changed while preparing or reviewing the form.</summary>
public sealed class EntryEditorSourceChangedException : InvalidOperationException
{
    /// <summary>Identifies changed original facts without exposing financial or native connection values.</summary>
    public EntryEditorSourceChangedException() : base("The original transaction editor source changed.") { }
}

/// <summary>Original file and complete source packet, retained independently of the mutable editor draft.</summary>
public sealed class EntryEditorReadSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _financial;
    private readonly string _presentation;
    private int _write;
    /// <summary>Captures original facts before a caller can publish or modify them.</summary>
    internal EntryEditorReadSnapshot(string path, CommercialFileAccess access, EntryEditorReadRequest request, EntryEditorReadInputs inputs)
    {
        DatabasePath = path; _access = access; Request = request; Inputs = inputs;
        _financial = Financial(inputs); _presentation = JsonSerializer.Serialize(new { inputs.Rules, inputs.Tags });
    }
    /// <summary>Gets the first actual original file, including a missing source.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets the original parsed route and its captured local date.</summary>
    public EntryEditorReadRequest Request { get; }
    /// <summary>Gets the complete detached facts; mutation retires original authority.</summary>
    public EntryEditorReadInputs Inputs { get; }
    /// <summary>Gets the exact original entry, never a later same-id entry from another profile.</summary>
    public LedgerEntry? Source => Inputs.Resources.SourceEntry;
    /// <summary>Gets whether an explicit original source exists with complete monetary account references.</summary>
    public bool IsAvailable => (Request.SourceId is null || Source is not null)
        && (Source is null || Inputs.Resources.Accounts.Any(a => a.Id == Source.AccountId)
            && (Source.ToAccountId is null || Inputs.Resources.Accounts.Any(a => a.Id == Source.ToAccountId)))
        && (Request.RefundOfId is null || Source?.Kind == EntryKind.Expense)
        && (Request.IncomeId is null || Source?.Kind == EntryKind.Income)
        && (Request.EditId is null && Request.DuplicateId is null || Inputs.HoldingTypeId is null
            && Source?.Kind is not (EntryKind.AssetPurchase or EntryKind.AssetSale))
        && (Request.TemplateId is null || Inputs.Resources.Templates.Any(t => t.Id == Request.TemplateId)
            && Inputs.Resources.Work.CanApplyTemplate(Request.TemplateId.Value));
    /// <summary>Gets known financial acceptance before any fallible callback or notification.</summary>
    public bool Accepted => Volatile.Read(ref _write) == 2;
    /// <summary>Gets whether an original writer still owns its pending commit boundary.</summary>
    public bool IsPending => Volatile.Read(ref _write) == 1;
    /// <summary>Admits one pending writer from this exact original form packet.</summary>
    internal void BeginWrite()
    {
        EnsureCurrent();
        if (Interlocked.CompareExchange(ref _write, 1, 0) != 0)
            throw new InvalidOperationException("The original editor write is pending or already accepted.");
    }
    /// <summary>Consumes a known commit; an uncommitted attempt retains only original authority.</summary>
    internal void EndWrite(bool accepted) => Volatile.Write(ref _write, accepted ? 2 : 0);
    /// <summary>Checks cached rights cheaply during synchronous field publication.</summary>
    public void EnsureAccessCurrent() => _access.EnsureSnapshotCurrent();
    /// <summary>Refuses changed cached rights or in-memory original source/suggestion packets.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent(); var resources = Inputs.Resources;
        if (resources.InputStamp != QuickResourceReview.Capture(resources.Templates, resources.Filters, resources.Accounts,
            resources.Categories, resources.Settings, resources.SourceEntry) || _financial != Financial(Inputs)
            || _presentation != JsonSerializer.Serialize(new { Inputs.Rules, Inputs.Tags }))
            throw new InvalidOperationException("The original transaction editor inputs changed in memory.");
    }
    /// <summary>Rejects foreign actual files before any original financial row query.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        EnsureCurrent();
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The transaction editor cannot follow another file.");
    }
    /// <summary>Checks persisted financial facts; unrelated tag suggestions never become writer authority.</summary>
    internal void ValidateFinancial(EntryEditorReadInputs current)
    {
        EnsureCurrent();
        if (_financial != Financial(current)) throw new EntryEditorSourceChangedException();
    }
    private static string Financial(EntryEditorReadInputs inputs) => JsonSerializer.Serialize(new
    { inputs.Resources.InputStamp, inputs.Related, inputs.Purchase, inputs.Paybacks, inputs.HoldingTypeId });
}
