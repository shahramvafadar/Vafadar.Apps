using System.Text.Json;

namespace Vafadar.Zanance.Testing;

/// <summary>Compare complete restored data while retaining the shape of Home's opaque revision row.</summary>
internal static class FinancialSnapshotAssert
{
    /// <summary>Allow only a well-formed revision change after explicitly accepted financial writes.</summary>
    internal static void EqualAfterWrites(string expected, string actual) =>
        Assert.Equal(NormalizeAfterWrites(expected), NormalizeAfterWrites(actual));

    /// <summary>Normalize only the validated opaque token, preserving every table and financial value.</summary>
    internal static string NormalizeAfterWrites(string snapshot)
    {
        var tables = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(snapshot)!;
        Assert.True(tables.TryGetValue("HomeFinancialRevision", out var revisions));
        var row = JsonSerializer.Deserialize<JsonElement[]>(Assert.Single(revisions!))!;
        Assert.Equal(2, row.Length); Assert.Equal(1L, row[0].GetInt64());
        Assert.True(Guid.TryParseExact(row[1].GetString(), "N", out _));
        // Restoring money is itself a new write; retaining the old token would permit stale Home reads.
        tables["HomeFinancialRevision"] = [JsonSerializer.Serialize(new object[] { 1L, "<restored-financial-revision>" })];
        return JsonSerializer.Serialize(tables);
    }
}
