using System.Globalization;
using System.Text.Json;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>A technical profile-size refusal, distinct from a financial conflict or a commercial resource quota.</summary>
public sealed class SyncProfileBoundException : ArgumentException
{
    /// <summary>Creates a content-free bound refusal.</summary>
    public SyncProfileBoundException() : base("Personal sync profile bound exceeded.") { }
}

/// <summary>A bounded structural problem identified without placing financial content in logs or exception text.</summary>
public sealed record SyncGraphIssue(SyncRecordKey Record, string Reason, IReadOnlyList<string> Fields, SyncRecordKey? RelatedRecord = null);

/// <summary>Checks complete proposed graph structure against the shipped model authority, never client SQL.</summary>
public static class SyncGraphValidation
{
    /// <summary>Maximum bytes of one protected profile; blob manifests count their original declared lengths.</summary>
    public const long MaximumProfileBytes = 256L * 1024 * 1024;
    /// <summary>Bounds untrusted graph cardinality independently of serialized byte size.</summary>
    public const int MaximumRows = 1_000_000;

    /// <summary>Validates every live scalar/key, dependency and original unique rule before any round can publish.</summary>
    public static IReadOnlyList<SyncGraphIssue> Validate(IReadOnlyCollection<SyncRecord> records, SyncSchemaManifest? contract = null)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (records.Count > MaximumRows) throw new SyncProfileBoundException();
        var tables = (contract ?? SyncSchema.Manifest).Tables.ToDictionary(table => table.Name, StringComparer.Ordinal);
        var issues = new List<SyncGraphIssue>();
        var live = new Dictionary<SyncRecordKey, JsonElement>();
        var semanticIdentities = new HashSet<SyncRecordKey>();
        long size = 0;
        foreach (var record in records)
        {
            if (!tables.TryGetValue(record.Record.Table, out var table))
            { Add(record.Record, "UnknownTable", []); continue; }
            if (record.Record.Key.Length > 2048 || record.Version < 0)
            { Add(record.Record, "InvalidIdentity", []); continue; }
            if (!ValidIdentity(table, record.Record))
            { Add(record.Record, "InvalidIdentity", table.PrimaryKey); continue; }
            if (record.Values is not { } body) continue;
            size = checked(size + System.Text.Encoding.UTF8.GetByteCount(body.GetRawText()));
            if (size > MaximumProfileBytes) throw new SyncProfileBoundException();
            var rowIssues = ValidateRow(table, record.Record, body);
            foreach (var issue in rowIssues) Add(issue.Record, issue.Reason, issue.Fields);
            if (rowIssues.Count != 0) continue;
            if (!semanticIdentities.Add(new(table.Name, ConstraintKeyOf(table, body, table.PrimaryKey))))
            { Add(record.Record, "DuplicateIdentity", table.PrimaryKey); continue; }
            if (!live.TryAdd(record.Record, body)) Add(record.Record, "DuplicateIdentity", table.PrimaryKey);
            foreach (var column in table.Columns.Where(column => column.Kind == "blob"))
            {
                var cell = body.GetProperty(column.Name);
                if (cell.ValueKind == JsonValueKind.Null) continue;
                size = checked(size + cell.GetProperty("length").GetInt64());
                if (size > MaximumProfileBytes) throw new SyncProfileBoundException();
            }
        }
        if (issues.Count != 0) return issues;
        var grouped = live.GroupBy(row => row.Key.Table, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        if (grouped.TryGetValue("Attachments", out var attachments))
            foreach (var attachment in attachments)
            {
                var parent = new SyncRecordKey("Entries", KeyOf(attachment.Value, ["EntryId"]));
                if (!live.ContainsKey(parent)) Add(attachment.Key, "RelatedRecordMissing", ["EntryId"], parent);
            }
        foreach (var table in tables.Values)
        {
            if (!grouped.TryGetValue(table.Name, out var rows)) continue;
            foreach (var dependency in table.ForeignKeys)
            {
                var targets = grouped.TryGetValue(dependency.PrincipalTable, out var parents)
                    ? parents.Select(parent => KeyOf(parent.Value, dependency.PrincipalColumns)).ToHashSet(StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal);
                foreach (var row in rows)
                {
                    if (dependency.Columns.Any(name => row.Value.GetProperty(name).ValueKind == JsonValueKind.Null)) continue;
                    if (!targets.Contains(KeyOf(row.Value, dependency.Columns)))
                        Add(row.Key, "RelatedRecordMissing", dependency.Columns,
                            new(dependency.PrincipalTable, KeyOf(row.Value, dependency.Columns)));
                }
            }
            foreach (var unique in table.UniqueKeys)
            {
                var seen = new Dictionary<string, SyncRecordKey>(StringComparer.Ordinal);
                foreach (var row in rows)
                {
                    // Match the real partial index; partial payments must remain independently postable.
                    if (unique.FullSettlementOnly && (row.Value.GetProperty("ScheduleId").ValueKind == JsonValueKind.Null ||
                        row.Value.GetProperty("IsPartialPayment").GetInt64() != 0)) continue;
                    if (unique.Columns.Any(name => row.Value.GetProperty(name).ValueKind == JsonValueKind.Null)) continue;
                    var uniqueValue = ConstraintKeyOf(table, row.Value, unique.Columns);
                    if (!seen.TryAdd(uniqueValue, row.Key))
                        Add(row.Key, unique.FullSettlementOnly ? "OccurrenceAlreadySettled" : "UniqueRelationship", unique.Columns, seen[uniqueValue]);
                }
            }
        }
        return issues;

        void Add(SyncRecordKey record, string reason, IReadOnlyList<string> fields, SyncRecordKey? related = null)
        {
            // Response bounds must not turn a large malformed profile into a large private-content response.
            if (issues.Count < 100) issues.Add(new(record, reason, fields, related));
        }
    }

    /// <summary>Requires a complete allowed row; missing fields never mean defaulting or permission to remove a value.</summary>
    public static IReadOnlyList<SyncGraphIssue> ValidateRow(SyncTable table, SyncRecordKey record, JsonElement row)
    {
        if (record.Table != table.Name) return [new(record, "UnknownTable", [])];
        if (!ValidIdentity(table, record)) return [new(record, "InvalidIdentity", table.PrimaryKey)];
        if (row.ValueKind != JsonValueKind.Object) return [new(record, "InvalidRow", [])];
        var properties = row.EnumerateObject().ToArray();
        var names = properties.Select(property => property.Name).ToArray();
        if (names.Length != table.Columns.Length || names.Distinct(StringComparer.Ordinal).Count() != names.Length ||
            !names.ToHashSet(StringComparer.Ordinal).SetEquals(table.Columns.Select(column => column.Name)))
            return [new(record, "UnknownOrMissingField", [])];
        var invalid = table.Columns.Where(column => !ValidCell(column, row.GetProperty(column.Name)))
            .Select(column => column.Name).ToArray();
        if (invalid.Length != 0) return [new(record, "InvalidScalar", invalid)];
        if (table.Name == "Attachments" && row.GetProperty("Data").GetProperty("length").GetInt64() is < 1 or > Ledger.EntryAttachment.MaxBytes)
            return [new(record, "InvalidAttachmentSize", ["Data"])];
        if (table.Columns.Where(column => column.Kind == "guid" && table.PrimaryKey.Contains(column.Name))
            .Any(column => row.GetProperty(column.Name).GetGuid() == Guid.Empty))
            return [new(record, "InvalidIdentity", table.PrimaryKey)];
        if (KeyOf(row, table.PrimaryKey) != record.Key) return [new(record, "PrimaryKeyMismatch", table.PrimaryKey)];
        return [];
    }

    /// <summary>Uses the model's ordered scalar keys, including the Settings virtual singleton.</summary>
    public static string KeyOf(JsonElement row, IReadOnlyList<string> columns)
    {
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes))
        {
            writer.WriteStartArray();
            foreach (var name in columns) row.GetProperty(name).WriteTo(writer);
            writer.WriteEndArray();
        }
        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static string ConstraintKeyOf(SyncTable table, JsonElement row, IReadOnlyList<string> columns)
    {
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes))
        {
            writer.WriteStartArray();
            foreach (var name in columns)
            {
                var value = row.GetProperty(name);
                if (value.ValueKind != JsonValueKind.Null && table.Columns.Single(column => column.Name == name).Kind == "guid")
                    writer.WriteStringValue(value.GetGuid().ToString("D"));
                else value.WriteTo(writer);
            }
            writer.WriteEndArray();
        }
        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>Checks tombstone keys too; no arbitrary text becomes an opaque deletion identity.</summary>
    public static bool ValidIdentity(SyncTable table, SyncRecordKey record)
    {
        if (record.Table != table.Name || record.Key is null || record.Key.Length > 2048) return false;
        try
        {
            using var document = JsonDocument.Parse(record.Key, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() != table.PrimaryKey.Length) return false;
            for (var index = 0; index < table.PrimaryKey.Length; index++)
            {
                var column = table.Columns.Single(value => value.Name == table.PrimaryKey[index]);
                var value = root[index];
                if (value.ValueKind == JsonValueKind.Null || !ValidCell(column, value) ||
                    column.Kind == "guid" && value.GetGuid() == Guid.Empty) return false;
            }
            using var bytes = new MemoryStream();
            using (var writer = new Utf8JsonWriter(bytes)) root.WriteTo(writer);
            return System.Text.Encoding.UTF8.GetString(bytes.ToArray()) == record.Key;
        }
        catch (JsonException) { return false; }
    }

    private static bool ValidCell(SyncColumn column, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null) return column.Nullable;
        if (column.Kind is "integer" or "ticks")
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var number)) return false;
            if (column.Minimum is { } minimum && number < minimum || column.Maximum is { } maximum && number > maximum) return false;
            if (column.Kind == "ticks" && (number < DateTime.MinValue.Ticks || number > DateTime.MaxValue.Ticks)) return false;
            if (column.EnumValues is not { } values) return true;
            return column.Flags ? (number & ~values.Aggregate(0L, (mask, item) => mask | item)) == 0 : values.Contains(number);
        }
        if (column.Kind == "real") return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number);
        if (column.Kind == "blob")
        {
            if (value.ValueKind != JsonValueKind.Object) return false;
            var fields = value.EnumerateObject().ToArray();
            return fields.Length == 2 && value.TryGetProperty("sha256", out var hash) && hash.ValueKind == JsonValueKind.String &&
                hash.GetString() is { Length: 64 } digest && digest.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f') &&
                value.TryGetProperty("length", out var length) && length.ValueKind == JsonValueKind.Number &&
                length.TryGetInt64(out var count) && count is >= 0 and <= MaximumProfileBytes;
        }
        if (value.ValueKind != JsonValueKind.String) return false;
        var text = value.GetString()!;
        if (text.Length > (column.MaximumLength ?? 1_000_000)) return false;
        return column.Kind switch
        {
            // Microsoft.Data.Sqlite binds Guid parameters as uppercase text. Requiring that actual
            // representation keeps received keys queryable by the original EF/native callers.
            "guid" => Guid.TryParseExact(text, "D", out var guid) && text == guid.ToString("D").ToUpperInvariant(),
            "date" => DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "time" => TimeOnly.TryParseExact(text, ["HH:mm:ss", "HH:mm:ss.FFFFFFF"], CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "decimal" => decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
            "json-guid" or "json-string" or "json-integer" => ValidCollection(text, column),
            "text" => true,
            _ => false,
        };
    }

    private static bool ValidCollection(string text, SyncColumn column)
    {
        var kind = column.Kind;
        try
        {
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 4 });
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() > 65536) return false;
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (kind == "json-guid" && (item.ValueKind != JsonValueKind.String || !Guid.TryParseExact(item.GetString(), "D", out _))) return false;
                if (kind == "json-string" && item.ValueKind != JsonValueKind.String) return false;
                if (kind == "json-integer" && (item.ValueKind != JsonValueKind.Number || !item.TryGetInt64(out var number) ||
                    column.Minimum is { } minimum && number < minimum || column.Maximum is { } maximum && number > maximum)) return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
    }
}
