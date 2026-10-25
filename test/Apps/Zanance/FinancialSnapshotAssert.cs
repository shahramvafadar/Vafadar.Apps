using System.Text.Json;
using System.Security.Cryptography;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.Testing;

/// <summary>Compares authoritative restored rows and validates internal derived bookkeeping separately.</summary>
internal static class FinancialSnapshotAssert
{
    /// <summary>Allow only a well-formed revision change after explicitly accepted financial writes.</summary>
    internal static void EqualAfterWrites(string expected, string actual) =>
        Assert.Equal(NormalizeAfterWrites(expected), NormalizeAfterWrites(actual));

    /// <summary>Normalizes validated revision/derived transport after accepted writes, preserving all authoritative rows.</summary>
    internal static string NormalizeAfterWrites(string snapshot)
    {
        var tables = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(snapshot)!;
        Assert.True(tables.TryGetValue("HomeFinancialRevision", out var revisions));
        var row = JsonSerializer.Deserialize<JsonElement[]>(Assert.Single(revisions!))!;
        Assert.Equal(2, row.Length); Assert.Equal(1L, row[0].GetInt64());
        Assert.True(Guid.TryParseExact(row[1].GetString(), "N", out _));
        // Restoring money is itself a new write; retaining the old token would permit stale Home reads.
        tables["HomeFinancialRevision"] = [JsonSerializer.Serialize(new object[] { 1L, "<restored-financial-revision>" })];
        NormalizeLedgerTransport(tables);
        return JsonSerializer.Serialize(tables);
    }

    /// <summary>
    /// Derived rowids, pending invalidation and block grouping can differ after an accepted Undo or omitted-entry edit.
    /// Validate their shape/integrity/readiness, then exclude only this internal transport from financial equality.
    /// Ordinary no-write snapshots never call this normalization and continue comparing every stored byte.
    /// </summary>
    private static void NormalizeLedgerTransport(Dictionary<string, List<string>> tables)
    {
        string[] names = ["HomeLedgerReadState", "HomeLedgerReadBlocks", "HomeLedgerDirtyBlocks"];
        if (names.All(name => !tables.ContainsKey(name))) return; // Historical first-schema snapshots remain valid.
        Assert.All(names, name => Assert.True(tables.ContainsKey(name)));
        var state = JsonSerializer.Deserialize<JsonElement[]>(Assert.Single(tables[names[0]]))!;
        Assert.Equal(6, state.Length); Assert.Equal(1, state[0].GetInt64());
        Assert.True(Guid.TryParseExact(state[1].GetString(), "N", out _));
        if (state[2].ValueKind != JsonValueKind.Null) Assert.True(Guid.TryParseExact(state[2].GetString(), "N", out _));
        for (var index = 3; index < 6; index++)
            Assert.True(state[index].ValueKind == JsonValueKind.Null || state[index].ValueKind == JsonValueKind.Number && state[index].GetInt64() >= 0);
        var ready = state[2].ValueKind == JsonValueKind.String && state[1].GetString() == state[2].GetString();
        var dirty = new HashSet<long>();
        foreach (var value in tables[names[2]])
        {
            var row = JsonSerializer.Deserialize<JsonElement[]>(value)!; Assert.Single(row);
            var id = row[0].GetInt64(); Assert.True(unchecked(id << 9) >> 9 == id); Assert.True(dirty.Add(id));
        }
        long count = 0; var blocks = new HashSet<long>(); var incomplete = false;
        foreach (var value in tables[names[1]])
        {
            var row = JsonSerializer.Deserialize<JsonElement[]>(value)!; Assert.Equal(4, row.Length);
            var id = row[0].GetInt64(); Assert.True(blocks.Add(id));
            var rows = row[1].GetInt64(); Assert.InRange(rows, 1, HomeLedgerBlockCodec.MaxRows); count += rows;
            if (row[2].ValueKind == JsonValueKind.Null)
            { Assert.Equal(JsonValueKind.Null, row[3].ValueKind); incomplete = true; continue; }
            var payload = row[2].GetBytesFromBase64(); var digest = row[3].GetBytesFromBase64();
            Assert.Equal(32, digest.Length); Assert.Equal(SHA256.HashData(payload), digest);
            Assert.Equal(rows, HomeLedgerBlockCodec.Decode(payload, id, TestContext.Current.CancellationToken).Count);
        }
        if (ready)
        {
            Assert.False(incomplete); Assert.Empty(dirty); Assert.Equal(1, state[4].GetInt64());
            Assert.Equal(count, state[5].GetInt64());
            if (tables.TryGetValue("Entries", out var entries)) Assert.Equal(entries.Count, count);
        }
        // The complete Entries/accounts/plans/settings/etc snapshots remain untouched and compared exactly.
        foreach (var name in names) tables[name] = ["<validated-derived-ledger-transport>"];
    }
}
