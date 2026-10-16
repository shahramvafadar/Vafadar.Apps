using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

public sealed partial class PersonalSyncStore
{
    /// <summary>Retains a whole original linked-group snapshot when any member changes, without financial writes.</summary>
    private static async Task ReconcileGroupCaptureAsync(ZananceDbContext db, long after, CancellationToken ct)
    {
        var pending = await db.Set<SyncOutboxRow>().AsNoTracking().Where(row => row.Sequence > after &&
            (row.TableName == "Entries" || row.TableName == "AssetEvents" || row.TableName == "HouseholdCosts" || row.TableName == "HouseholdAllocations")).OrderBy(row => row.Sequence).ToArrayAsync(ct);
        var groups = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        { ["ledger"] = new(StringComparer.Ordinal), ["household-cost"] = new(StringComparer.Ordinal) };
        var latest = pending.GroupBy(row => new SyncRecordKey(row.TableName, row.RecordKey)).ToDictionary(group => group.Key, group => group.Last());
        foreach (var row in pending) AddGroup(new(row.TableName, row.RecordKey), row.Body);
        foreach (var key in latest.Keys)
        {
            var known = await db.Set<SyncKnownRow>().AsNoTracking().SingleOrDefaultAsync(row => row.TableName == key.Table && row.RecordKey == key.Key, ct);
            AddGroup(key, known?.Body);
        }
        if (groups.Values.All(group => group.Count == 0)) return;
        var current = new Dictionary<SyncRecordKey, JsonElement?>();
        foreach (var table in Core.Sharing.HouseholdFinancialSchema.Manifest.Tables.Where(table => table.Name is "Entries" or "AssetEvents" or "HouseholdCosts" or "HouseholdAllocations"))
        {
            var family = SyncConflictGrouping.Family(new(table.Name, "")); if (groups[family].Count == 0) continue;
            var column = table.Name == "HouseholdCosts" ? "Id" : table.Name == "HouseholdAllocations" ? "CostId" : "GroupId";
            await using var query = Command(db, SyncTriggerSql.ReadLiveExistingAliased(table) +
                " WHERE source.\"" + column + "\" IN (SELECT value FROM json_each($groups));");
            Parameter(query, "$groups", JsonSerializer.Serialize(groups[family].ToArray()));
            await using var reader = await query.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var key = new SyncRecordKey(table.Name, reader.GetString(0)); current.Add(key, SyncRowCodec.ToWire(key, reader.GetString(1)));
            }
        }
        foreach (var table in new[] { "Entries", "AssetEvents", "HouseholdCosts", "HouseholdAllocations" })
        {
            var family = SyncConflictGrouping.Family(new(table, "")); if (groups[family].Count == 0) continue;
            var column = table == "HouseholdCosts" ? "Id" : table == "HouseholdAllocations" ? "CostId" : "GroupId";
            await using var query = Command(db, "SELECT TableName,RecordKey FROM SyncKnown WHERE TableName=$table AND " +
                "json_extract(Body,'$." + column + "') IN (SELECT value FROM json_each($groups));");
            Parameter(query, "$table", table); Parameter(query, "$groups", JsonSerializer.Serialize(groups[family].ToArray()));
            await using var reader = await query.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) current.TryAdd(new(reader.GetString(0), reader.GetString(1)), null);
        }
        foreach (var (key, body) in current)
        {
            if (latest.TryGetValue(key, out var row) && Same(SyncRowCodec.Parse(row.Body), body)) continue;
            await CaptureOneAsync(db, key, body is null, ct);
        }
        // Tombstones are tracked additions; persist them before the caller freezes its sequence cutoff.
        await db.SaveChangesAsync(ct);
        void AddGroup(SyncRecordKey key, string? body)
        {
            if (body is null) return;
            using var document = JsonDocument.Parse(body);
            if (SyncConflictGrouping.Group(key, document.RootElement) is { } group)
                groups[SyncConflictGrouping.Family(key)].Add(group.ToString("D").ToUpperInvariant());
        }
    }
}
