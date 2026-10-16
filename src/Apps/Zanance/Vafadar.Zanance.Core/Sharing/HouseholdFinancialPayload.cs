using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Exact inner encrypted content type; never an HTTP financial-row/blob request to the service.</summary>
public enum HouseholdFinancialContentKind
{
    /// <summary>One complete original relational row or explicit tombstone.</summary>
    Record = 1,
    /// <summary>One bounded immutable attachment piece, authenticated with the same complete publication manifest.</summary>
    Attachment = 2,
    /// <summary>One bounded piece of a completely framed original record stream; records may cross envelope boundaries.</summary>
    RecordStream = 3,
    /// <summary>One bounded group of immutable small attachment pieces; cardinality does not force one ciphertext per attachment.</summary>
    AttachmentBatch = 4,
}

/// <summary>Protected attachment piece; its plain digest remains inside client encryption and never becomes a service address.</summary>
public sealed record HouseholdAttachmentPiece([property: JsonRequired] string Sha256, [property: JsonRequired] long Length,
    [property: JsonRequired] int Index, [property: JsonRequired] byte[] Bytes)
{
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdAttachmentPiece);
}

/// <summary>Bounded signed-then-encrypted original financial content with exact attempt/index/schema binding.</summary>
public sealed record HouseholdFinancialPayload([property: JsonRequired] int Format, [property: JsonRequired] string SchemaHash,
    [property: JsonRequired] Guid AttemptId, [property: JsonRequired] int Index, [property: JsonRequired] HouseholdFinancialContentKind Kind,
    [property: JsonRequired] SyncRecord? Record, [property: JsonRequired] HouseholdAttachmentPiece? Attachment,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] HouseholdRecordBlock? Stream = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] HouseholdAttachmentPiece[]? Blobs = null)
{
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdFinancialPayload);
}

/// <summary>Original length-prefixed UTF8 financial record stream, inside standard signed encryption; final totals bind complete decoding without a whole-profile allocation.</summary>
public sealed record HouseholdRecordBlock([property: JsonRequired] int Index, [property: JsonRequired] bool Final,
    [property: JsonRequired] long Length, [property: JsonRequired] int Records, [property: JsonRequired] byte[] Bytes)
{
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdRecordBlock);
}

/// <summary>Financial wire framing bounds preserve the original profile/row cardinality while accounting for serialized identity/escaping overhead.</summary>
public static class HouseholdFinancialFraming
{
    /// <summary>Base64 plus metadata remains below the256KiB clear signed-message limit.</summary>
    public const int BlockBytes = 160 * 1024;
    /// <summary>Covers current stored text/JSON column maxima after worst-case escaping; only household framing/apply uses this larger local row bound.</summary>
    public const int MaximumRecordBytes = 16 * 1024 * 1024;
    /// <summary>Finite wire allowance covers up to6x JSON escaping of original256MiB values plus1,000,000 fixed canonical GUID-key headers.</summary>
    public const long MaximumStreamBytes = 2L * 1024 * 1024 * 1024;
}

/// <summary>Strict original encrypted payload serialization, independent of public service metadata and runtime CLR type discovery.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(HouseholdFinancialPayload))]
public partial class HouseholdFinancialJsonContext : JsonSerializerContext;
