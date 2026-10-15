using System.Security.Cryptography;
using System.Text.Json;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>An allowlisted stored scalar, with original model constraints rather than client-supplied SQL.</summary>
public sealed record SyncColumn(string Name, string Kind, bool Nullable, int? MaximumLength,
    long[]? EnumValues, bool Flags, long? Minimum = null, long? Maximum = null);

/// <summary>An original relational dependency; composite columns remain ordered.</summary>
public sealed record SyncForeignKey(string[] Columns, string PrincipalTable, string[] PrincipalColumns);

/// <summary>An original unique key; the only conditional rule is the explicit full-settlement predicate.</summary>
public sealed record SyncUniqueKey(string[] Columns, bool FullSettlementOnly);

/// <summary>An allowlisted financial table; Settings has a virtual singleton key and retains its local physical id.</summary>
public sealed record SyncTable(string Name, bool Singleton, SyncColumn[] Columns, string[] PrimaryKey,
    SyncForeignKey[] ForeignKeys, SyncUniqueKey[] UniqueKeys);

/// <summary>A versioned contract exported from the actual relational model and reviewed before use.</summary>
public sealed record SyncSchemaManifest(int Version, SyncTable[] Tables);

/// <summary>The immutable local/service schema authority; a peer cannot invent tables, columns or constraints.</summary>
public static class SyncSchema
{
    private static readonly byte[] Bytes = ReadBytes();
    /// <summary>The exact model contract; unknown versions fail before any financial mutation.</summary>
    public static SyncSchemaManifest Manifest { get; } = ReadManifest();
    /// <summary>Standard digest used for compatibility negotiation, not encryption or authentication.</summary>
    public static string Hash { get; } = SyncSchemaDigest.Compute(Manifest);

    private static byte[] ReadBytes()
    {
        using var source = typeof(SyncSchema).Assembly.GetManifestResourceStream("Vafadar.Zanance.Sync.schema-v1.json")
            ?? throw new InvalidOperationException("Financial sync model contract is missing.");
        using var bytes = new MemoryStream(); source.CopyTo(bytes); return bytes.ToArray();
    }

    private static SyncSchemaManifest ReadManifest()
    {
        var value = JsonSerializer.Deserialize(Bytes, PersonalSyncJsonContext.Default.SyncSchemaManifest)
            ?? throw new InvalidOperationException("Financial sync model contract is invalid.");
        if (value.Version != 1 || value.Tables.Length == 0) throw new InvalidOperationException("Financial sync model is not qualified.");
        return value;
    }
}

/// <summary>Canonical model hashing remains identical across Git line endings and JSON indentation.</summary>
public static class SyncSchemaDigest
{
    /// <summary>Hashes the typed manifest rather than platform-dependent source-file bytes.</summary>
    public static string Compute(SyncSchemaManifest manifest) => Convert.ToHexStringLower(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(manifest, PersonalSyncJsonContext.Default.SyncSchemaManifest)));
}
