using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>One detached image preview read in the same transaction as its original entry and document metadata.</summary>
public sealed record EntryDetailImage(Guid AttachmentId, byte[] Data);

/// <summary>Complete original detail facts, without initializing settings or querying unrelated ledger history.</summary>
public sealed record EntryDetailInputs(QuickResourceReview Resources, IReadOnlyList<LedgerEntry> Related,
    IReadOnlyList<LedgerEntry> Paybacks, LedgerEntry? Purchase, Guid? HoldingTypeId,
    EntryAttachmentReview? AttachmentReview, IReadOnlyList<EntryDetailImage> Images, IReadOnlyList<CategoryRule> Rules);

/// <summary>A persistent original detail changed after reading; only an unchanged same-file reader may restart preparation.</summary>
public sealed class EntryDetailSupersededException : InvalidOperationException
{
    /// <summary>Describes a changed persistent packet, never permission or caller-input retirement.</summary>
    public EntryDetailSupersededException() : base("The original entry detail changed during preparation.") { }
}

/// <summary>The original actual file, view facts and cached rights for one detail publication or explicit action.</summary>
public sealed class EntryDetailSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _packet;
    private int _write;

    /// <summary>Captures complete original facts before any caller can publish or mutate them.</summary>
    internal EntryDetailSnapshot(string path, CommercialFileAccess access, Guid id, EntryDetailInputs inputs)
    { DatabasePath = path; _access = access; EntryId = id; Inputs = inputs; _packet = Packet(inputs); }

    /// <summary>Gets the original configured file, independent of any later selected profile.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets the requested original identity, including verified absence.</summary>
    public Guid EntryId { get; }
    /// <summary>Gets complete detached facts; changing them retires original authority.</summary>
    public EntryDetailInputs Inputs { get; }
    /// <summary>Gets the original entry, or null for a coherently verified missing row.</summary>
    public LedgerEntry? Entry => Inputs.Resources.SourceEntry;

    /// <summary>Gets known action acceptance, even when its following notification fails.</summary>
    public bool Accepted => Volatile.Read(ref _write) == 2;

    /// <summary>Checks cached ownership cheaply during synchronous native field publication.</summary>
    public void EnsureAccessCurrent() => _access.EnsureSnapshotCurrent();

    /// <summary>Rejects changed cached rights and mutated original fields before synchronous publication.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent();
        var resources = Inputs.Resources;
        if (resources.InputStamp != QuickResourceReview.Capture(resources.Templates, resources.Filters,
            resources.Accounts, resources.Categories, resources.Settings, resources.SourceEntry) || _packet != Packet(Inputs))
            throw new InvalidOperationException("The original entry detail inputs changed in memory.");
    }

    /// <summary>Refuses foreign actual files and mutated original facts before a row query.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        EnsureCurrent();
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original entry detail cannot follow another profile.");
    }

    /// <summary>Distinguishes persistent supersession from caller-input or rights retirement.</summary>
    internal void ValidateFacts(EntryDetailInputs current)
    {
        EnsureCurrent();
        if (_packet != Packet(current)) throw new EntryDetailSupersededException();
    }

    /// <summary>Admits only one pending writer from this exact original detail packet.</summary>
    internal void BeginWrite()
    {
        EnsureCurrent();
        if (Interlocked.CompareExchange(ref _write, 1, 0) != 0)
            throw new InvalidOperationException("The original detail action is pending or already accepted.");
    }

    /// <summary>Consumes a known commit before notification; an uncommitted attempt retains only its original authority.</summary>
    internal void EndWrite(bool accepted) => Volatile.Write(ref _write, accepted ? 2 : 0);

    private static string Packet(EntryDetailInputs value) => JsonSerializer.Serialize(new
    {
        value.Resources.InputStamp, value.Related, value.Paybacks, value.Purchase, value.HoldingTypeId,
        Attachments = value.AttachmentReview?.Inputs,
        Images = value.Images.Select(image => new { image.AttachmentId, Size = image.Data.Length,
            Hash = Convert.ToHexString(SHA256.HashData(image.Data)) }), value.Rules,
    });
}
