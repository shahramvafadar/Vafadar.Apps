using System.Text;
using System.Text.Json;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Bounded original relational context for human review; never a source of financial authority by itself.</summary>
public static class SyncReviewRelationships
{
    /// <summary>Returns physical and explicit logical references without interpreting opaque metadata as identities.</summary>
    public static IEnumerable<SyncRecordKey> Keys(string tableName, JsonElement values)
    {
        var table = SyncSchema.Manifest.Tables.Single(row => row.Name == tableName);
        foreach (var relation in table.ForeignKeys)
            if (!relation.Columns.Any(column => values.GetProperty(column).ValueKind == JsonValueKind.Null))
                yield return new(relation.PrincipalTable, SyncGraphValidation.KeyOf(values, relation.Columns));
        foreach (var (column, target) in new[] { ("ScheduleId", "Schedules"), ("EntryId", "Entries"), ("PreviousScheduleId", "Schedules"), ("CategoryId", "Categories"),
            ("AssetTypeId", "AssetTypes"), ("LocationId", "AssetLocations"), ("ToLocationId", "AssetLocations"), ("GoalId", "Goals"), ("AccountId", "Accounts"), ("ToAccountId", "Accounts"),
            ("DefaultAccountId", "Accounts"), ("ParentId", "Categories") })
            if (values.TryGetProperty(column, out var id) && id.ValueKind == JsonValueKind.String)
                yield return new(target, JsonSerializer.Serialize(new[] { id.GetString()! }));
        foreach (var (column, target) in new[] { ("AccountIds", "Accounts"), ("CategoryIds", "Categories") })
            if (tableName != "ForecastSnapshots" && values.TryGetProperty(column, out var collection) && collection.ValueKind == JsonValueKind.String)
            {
                using var ids = JsonDocument.Parse(collection.GetString()!);
                foreach (var id in ids.RootElement.EnumerateArray()) yield return new(target, JsonSerializer.Serialize(new[] { id.GetString()! }));
            }
        if (tableName == "ForecastSnapshots")
            foreach (var id in values.GetProperty("AccountIds").GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!Guid.TryParse(id, out var identity)) throw new InvalidDataException("The saved forecast scope is not reviewable.");
                yield return new("Accounts", JsonSerializer.Serialize(new[] { identity.ToString("D").ToUpperInvariant() }));
            }
    }

    /// <summary>Captures relationships from the same authenticated graph as the conflict, including explicit missing rows.</summary>
    public static IReadOnlyList<SyncRecord> Capture(SyncConflict conflict, IReadOnlyDictionary<SyncRecordKey, SyncRecord> graph)
    {
        var queue = new Queue<SyncRecord>(new[] { new SyncRecord(conflict.Proposal.Record, 0, conflict.Proposal.Values), conflict.Current, conflict.RelatedRecord }.OfType<SyncRecord>());
        var seen = new HashSet<SyncRecordKey>(); var result = new Dictionary<SyncRecordKey, SyncRecord>(); long bytes = 0;
        while (queue.TryDequeue(out var source))
        {
            if (source.Values is not { } body) continue;
            var group = SyncConflictGrouping.Group(source.Record, body);
            var linked = group is null ? [] : graph.Values.Where(row => SyncConflictGrouping.Group(row.Record, row.Values) == group).Select(row => row.Record).ToArray();
            foreach (var key in Keys(source.Record.Table, body).Concat(linked))
            {
                if (!seen.Add(key) || key == conflict.Proposal.Record) continue;
                var record = graph.GetValueOrDefault(key) ?? new(key, 0, null);
                bytes += record.Values is { } value ? Encoding.UTF8.GetByteCount(value.GetRawText()) : 0;
                if (result.Count >= 128 || bytes > 64 * 1024) throw new InvalidDataException("The personal review relationship context exceeds its bound.");
                result.Add(key, record); queue.Enqueue(record);
            }
        }
        return result.Values.OrderBy(row => row.Record.Table, StringComparer.Ordinal).ThenBy(row => row.Record.Key, StringComparer.Ordinal).ToArray();
    }
}
