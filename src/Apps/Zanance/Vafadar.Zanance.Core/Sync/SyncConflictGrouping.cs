using System.Text.Json;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Keeps original linked financial rows together when a conflict requires an explicit version choice.</summary>
public static class SyncConflictGrouping
{
    /// <summary>Reads only the original entry/holding group identity, never an import batch or an inferred relationship.</summary>
    public static Guid? Group(SyncRecordKey key, JsonElement? body) => key.Table is "Entries" or "AssetEvents" && body is { } row &&
        row.GetProperty("GroupId").ValueKind != JsonValueKind.Null ? row.GetProperty("GroupId").GetGuid() : null;

    /// <summary>Includes all captured group members, including unchanged members and explicit tombstones.</summary>
    public static IReadOnlyList<SyncConflict> Expand(IEnumerable<SyncConflict> conflicts, IReadOnlyDictionary<SyncRecordKey, SyncProposal> proposals,
        IReadOnlyDictionary<SyncRecordKey, SyncRecord> current, IReadOnlyDictionary<SyncRecordKey, SyncRecord>? baselines = null)
    {
        var result = conflicts.ToDictionary(row => row.Proposal.Record);
        var groups = result.Values.Select(row => row.LinkedGroup ?? Group(row.Proposal.Record, row.Proposal.Values) ??
            Group(row.Proposal.Record, row.Current?.Values) ?? Group(row.Proposal.Record, row.Baseline?.Values)).OfType<Guid>().Distinct().ToArray();
        foreach (var group in groups)
            foreach (var proposal in proposals.Values.Where(row => Group(row.Record, row.Values) == group ||
                Group(row.Record, current.GetValueOrDefault(row.Record)?.Values) == group || Group(row.Record, baselines?.GetValueOrDefault(row.Record)?.Values) == group))
            {
                if (result.TryGetValue(proposal.Record, out var existing)) result[proposal.Record] = existing with { LinkedGroup = group };
                else result.Add(proposal.Record, new(proposal, baselines?.GetValueOrDefault(proposal.Record), current.GetValueOrDefault(proposal.Record),
                    ["GroupId"], "LinkedGroupReview", LinkedGroup: group));
            }
        return result.Values.ToArray();
    }

    /// <summary>Bounds review without splitting a linked group across an incomplete consent surface.</summary>
    public static IReadOnlyList<SyncConflict> Page(IEnumerable<SyncConflict> conflicts)
    {
        var result = new List<SyncConflict>();
        foreach (var group in conflicts.GroupBy(row => row.LinkedGroup?.ToString("N") ?? row.Proposal.Record.Table + ":" + row.Proposal.Record.Key))
        {
            var rows = group.ToArray();
            if (rows.Length > 100) throw new InvalidDataException("The linked financial group exceeds the review bound.");
            if (result.Count + rows.Length > 100) break;
            result.AddRange(rows);
        }
        return result;
    }
}
