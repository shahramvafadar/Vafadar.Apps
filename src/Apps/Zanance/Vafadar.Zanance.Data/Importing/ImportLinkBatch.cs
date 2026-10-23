using System.Text.Json.Serialization;
using Vafadar.Core.Domain;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Importing;

/// <summary>
/// A durable, profile-local journal for one ledger import or explicit editor aggregate adjustment (D-72/D-392/D-428). Snapshots travel with
/// database backups; attachments stay in their existing table. There is no foreign key to a consumed aggregate.
/// </summary>
public sealed class ImportLinkBatch : Entity, IAuditableEntity
{
    /// <summary>Creates an import journal.</summary>
    public ImportLinkBatch() { }

    /// <summary>Creates a journal whose identity is the import batch id.</summary>
    public ImportLinkBatch(Guid batchId) : base(batchId) { }

    /// <summary>Gets or sets the versioned, source-generated financial snapshot.</summary>
    public string StateJson { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>An aggregate's pre-import state, applied state and the exact details used for this reduction.</summary>
internal sealed record ImportAggregateState(Guid AggregateId, LedgerEntry? Before, LedgerEntry? After, List<LedgerEntry> Detailed);

/// <summary>The imported rows and aggregate changes needed to reject unsafe or out-of-order Undo.</summary>
internal sealed record ImportLinkState(int Version, List<LedgerEntry> Imported, List<ImportAggregateState> Adjustments,
    string? GroupDependencyDigest = null);

/// <summary>Explicit metadata keeps the journal functional in trimmed Android and iOS builds.</summary>
[JsonSerializable(typeof(ImportLinkState))]
[JsonSerializable(typeof(LedgerEntry))]
[JsonSerializable(typeof(AssetEvent))]
internal sealed partial class ImportLinkJson : JsonSerializerContext;
