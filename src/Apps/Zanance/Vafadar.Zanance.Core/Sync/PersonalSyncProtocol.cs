using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Identifies one allowed relational row by its canonical ordered primary-key JSON.</summary>
public sealed record SyncRecordKey([property: JsonRequired] string Table, [property: JsonRequired] string Key);

/// <summary>An immutable server event; a null body is a retained tombstone, never an empty replacement record.</summary>
public sealed record SyncRecord([property: JsonRequired] SyncRecordKey Record, [property: JsonRequired] long Version,
    [property: JsonRequired] JsonElement? Values);

/// <summary>A complete proposed row against an actual previously received server version.</summary>
public sealed record SyncProposal([property: JsonRequired] SyncRecordKey Record, [property: JsonRequired] long BaseVersion,
    [property: JsonRequired] JsonElement? Values);

/// <summary>A separately reviewed personal space, independent of a device's local profile name or credentials.</summary>
public sealed record SyncProfile(Guid Id, string Name, string SchemaHash, long Cursor, bool Initializing, Guid BootstrapDeviceId = default);

/// <summary>Explicitly transfers only an unpublished initialization, bound to its observed current device.</summary>
public sealed record SyncClaimInitialization([property: JsonRequired] Guid DeviceId, [property: JsonRequired] Guid ExpectedBootstrapDeviceId);

/// <summary>A revocable device owned by the authenticated identity, with no hardware identifier.</summary>
public sealed record SyncDevice(Guid Id, string Name, DateTimeOffset LastSeenAt, bool Revoked);

/// <summary>The authenticated owner identifier, without an email address, password or bearer credential.</summary>
public sealed record SyncIdentity(string Id);

/// <summary>An explicit bounded owner-visible label, without machine identity or credentials.</summary>
public sealed record SyncNameInput([property: JsonRequired] string Name);

/// <summary>Idempotently creates a private space; altered retry content is refused.</summary>
public sealed record SyncCreateProfile([property: JsonRequired] Guid DeviceId, [property: JsonRequired] Guid OperationId,
    [property: JsonRequired] string Name, [property: JsonRequired] string SchemaHash);

/// <summary>Creates a durable bounded staging round; the operation identity survives connection loss.</summary>
public sealed record SyncOpenRound([property: JsonRequired] Guid DeviceId, [property: JsonRequired] Guid OperationId,
    [property: JsonRequired] string SchemaHash);

/// <summary>Durable replay observation, distinct from a successful financial acknowledgement.</summary>
public sealed record SyncRoundHandle(Guid Id, bool Committed, long Cursor, int Parts, long Rows);

/// <summary>An ordered immutable part; replay with different content must be refused.</summary>
public sealed record SyncRoundPart([property: JsonRequired] int Index, [property: JsonRequired] IReadOnlyList<SyncProposal> Rows);

/// <summary>Binds the owner's whole-row choice to the reviewed current row and any related counterpart version.</summary>
public sealed record SyncResolution([property: JsonRequired] SyncRecordKey Record, [property: JsonRequired] long ReviewedVersion,
    [property: JsonRequired] bool UseProposal, SyncRecordKey? RelatedRecord = null, long? RelatedVersion = null,
    IReadOnlyList<SyncRecordVersion>? ContextVersions = null);

/// <summary>An exact authenticated relationship version included in an explicit financial decision.</summary>
public sealed record SyncRecordVersion(SyncRecordKey Record, long Version);

/// <summary>Publishes only a complete checked round; no staged financial row is visible before this commit.</summary>
public sealed record SyncCommitRound([property: JsonRequired] int Parts, [property: JsonRequired] int Rows,
    [property: JsonRequired] string ContentHash, [property: JsonRequired] IReadOnlyList<SyncResolution> Resolutions);

/// <summary>Explains a conflict without choosing an amount, deletion or relationship for the owner.</summary>
public sealed record SyncConflict(SyncProposal Proposal, SyncRecord? Baseline, SyncRecord? Current,
    IReadOnlyList<string> Fields, string Reason, SyncRecordKey? ExistingSettlement = null, SyncRecord? RelatedRecord = null,
    IReadOnlyList<SyncRecord>? Context = null, Guid? LinkedGroup = null);

/// <summary>Returns the atomic outcome; a conflict outcome publishes none of the proposed financial changes.</summary>
public sealed record SyncCommitResult(bool Committed, long Cursor, IReadOnlyList<SyncConflict> Conflicts);

/// <summary>An ordered immutable feed page bounded by a captured server cursor.</summary>
public sealed record SyncFeedPage(IReadOnlyList<SyncRecord> Records, long NextCursor, long ThroughCursor, bool Complete);

/// <summary>Describes bounded private object parts; the standard SHA-256 digest covers their concatenated bytes.</summary>
public sealed record SyncBlob(string Sha256, long Length, IReadOnlyList<Guid> Parts);

/// <summary>Declares an exact immutable byte length before bounded piece uploads.</summary>
public sealed record SyncBlobDeclaration([property: JsonRequired] long Length);

/// <summary>Resumes only confirmed immutable pieces; incomplete bytes never become a readable attachment.</summary>
public sealed record SyncBlobStatus(string Sha256, long Length, int Parts, IReadOnlyList<int> ReceivedParts, bool Complete);

/// <summary>Provides AOT-safe serialization for the same personal protocol on the client and service.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SyncRecord))]
[JsonSerializable(typeof(SyncProposal))]
[JsonSerializable(typeof(SyncProfile[]))]
[JsonSerializable(typeof(SyncDevice[]))]
[JsonSerializable(typeof(SyncIdentity))]
[JsonSerializable(typeof(SyncConflict))]
[JsonSerializable(typeof(SyncNameInput))]
[JsonSerializable(typeof(SyncCreateProfile))]
[JsonSerializable(typeof(SyncClaimInitialization))]
[JsonSerializable(typeof(SyncOpenRound))]
[JsonSerializable(typeof(SyncRoundHandle))]
[JsonSerializable(typeof(SyncRoundPart))]
[JsonSerializable(typeof(SyncCommitRound))]
[JsonSerializable(typeof(SyncCommitResult))]
[JsonSerializable(typeof(SyncFeedPage))]
[JsonSerializable(typeof(SyncBlob))]
[JsonSerializable(typeof(SyncBlobDeclaration))]
[JsonSerializable(typeof(SyncBlobStatus))]
[JsonSerializable(typeof(SyncSchemaManifest))]
public partial class PersonalSyncJsonContext : JsonSerializerContext
{
}
