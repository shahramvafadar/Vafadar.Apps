using System.Text.Json;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Keeps original linked financial rows together when a conflict requires an explicit version choice.</summary>
public static class SyncConflictGrouping
{
    /// <summary>Reads an original ledger group or the explicit parent cost identity; no financial relationship is inferred.</summary>
    public static Guid? Group(SyncRecordKey key, JsonElement? body) => body is not { } row ? null : key.Table switch
    {
        "HouseholdCosts" => row.GetProperty("Id").GetGuid(),
        "HouseholdAllocations" => row.GetProperty("CostId").GetGuid(),
        "Entries" or "AssetEvents" when row.GetProperty("GroupId").ValueKind != JsonValueKind.Null => row.GetProperty("GroupId").GetGuid(),
        _ => null,
    };
    /// <summary>Prevents a user-controlled ledger group id from colliding with an unrelated household cost id.</summary>
    public static string Family(SyncRecordKey key) => key.Table is "HouseholdCosts" or "HouseholdAllocations" ? "household-cost" : "ledger";
    /// <summary>Compares complete group scope without changing the delivered personal wire identity.</summary>
    public static bool InGroup(SyncRecordKey key, JsonElement? body, SyncRecordKey reference, Guid group) =>
        Family(key) == Family(reference) && Group(key, body) == group;

    /// <summary>Includes all captured group members, including unchanged members and explicit tombstones.</summary>
    public static IReadOnlyList<SyncConflict> Expand(IEnumerable<SyncConflict> conflicts, IReadOnlyDictionary<SyncRecordKey, SyncProposal> proposals,
        IReadOnlyDictionary<SyncRecordKey, SyncRecord> current, IReadOnlyDictionary<SyncRecordKey, SyncRecord>? baselines = null)
    {
        var result = conflicts.ToDictionary(row => row.Proposal.Record);
        var groups = result.Values.Select(row => (Family: Family(row.Proposal.Record), Id: row.LinkedGroup ?? Group(row.Proposal.Record, row.Proposal.Values) ??
            Group(row.Proposal.Record, row.Current?.Values) ?? Group(row.Proposal.Record, row.Baseline?.Values))).Where(group => group.Id is not null).Distinct().ToArray();
        foreach (var group in groups)
            foreach (var proposal in proposals.Values.Where(row => Family(row.Record) == group.Family && (Group(row.Record, row.Values) == group.Id ||
                Group(row.Record, current.GetValueOrDefault(row.Record)?.Values) == group.Id || Group(row.Record, baselines?.GetValueOrDefault(row.Record)?.Values) == group.Id)))
            {
                if (result.TryGetValue(proposal.Record, out var existing)) result[proposal.Record] = existing with { LinkedGroup = group.Id };
                else result.Add(proposal.Record, new(proposal, baselines?.GetValueOrDefault(proposal.Record), current.GetValueOrDefault(proposal.Record),
                    Family(proposal.Record) == "ledger" ? ["GroupId"] : [], "LinkedGroupReview", LinkedGroup: group.Id));
            }
        return result.Values.ToArray();
    }

    /// <summary>Bounds review without splitting a linked group across an incomplete consent surface.</summary>
    public static IReadOnlyList<SyncConflict> Page(IEnumerable<SyncConflict> conflicts)
    {
        var result = new List<SyncConflict>();
        foreach (var group in conflicts.GroupBy(row => row.LinkedGroup is { } id ? Family(row.Proposal.Record) + ":" + id.ToString("N") : row.Proposal.Record.Table + ":" + row.Proposal.Record.Key))
        {
            var rows = group.ToArray();
            if (rows.Length > 100) throw new InvalidDataException("The linked financial group exceeds the review bound.");
            if (result.Count + rows.Length > 100) break;
            result.AddRange(rows);
        }
        return result;
    }
}
