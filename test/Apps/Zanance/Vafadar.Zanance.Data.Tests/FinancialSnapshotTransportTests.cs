using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Testing;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Accepted-write normalization cannot conceal changed financial rows or damaged derived transport.</summary>
public sealed class FinancialSnapshotTransportTests
{
    [Fact]
    public void Equivalent_typed_transport_can_regroup_physical_rows_after_an_accepted_round_trip()
        => FinancialSnapshotAssert.EqualAfterWrites(Snapshot(1), Snapshot(512));

    [Theory]
    [InlineData("Entries"), InlineData("Accounts"), InlineData("Settings")]
    public void Every_authoritative_table_still_exposes_changed_values(string table)
    {
        var original = Snapshot(1);
        var changed = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(original)!;
        changed[table] = ["changed authoritative value"];
        Assert.NotEqual(FinancialSnapshotAssert.NormalizeAfterWrites(original),
            FinancialSnapshotAssert.NormalizeAfterWrites(JsonSerializer.Serialize(changed)));
    }

    [Fact]
    public void A_damaged_digest_is_not_discarded_as_harmless_bookkeeping()
    {
        var changed = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(Snapshot(1))!;
        var row = JsonSerializer.Deserialize<JsonElement[]>(Assert.Single(changed["HomeLedgerReadBlocks"]))!;
        changed["HomeLedgerReadBlocks"] = [JsonSerializer.Serialize(new object[] { 0L, 1L, row[2].GetBytesFromBase64(), new byte[32] })];
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => FinancialSnapshotAssert.NormalizeAfterWrites(JsonSerializer.Serialize(changed)));
    }

    [Fact]
    public void A_missing_internal_table_cannot_be_normalized_into_a_complete_schema()
    {
        var changed = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(Snapshot(1))!;
        changed.Remove("HomeLedgerDirtyBlocks");
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => FinancialSnapshotAssert.NormalizeAfterWrites(JsonSerializer.Serialize(changed)));
    }

    private static string Snapshot(long rowId)
    {
        var token = Guid.NewGuid().ToString("N");
        var entry = new LedgerEntry(new("20000000-0000-0000-0000-000000000001"))
        { AccountId = new("10000000-0000-0000-0000-000000000001"), Amount = 123, Date = new(2026, 10, 25), Title = "Original" };
        var payload = HomeLedgerBlockCodec.Encode([new(rowId, entry)], TestContext.Current.CancellationToken);
        return JsonSerializer.Serialize(new Dictionary<string, List<string>>
        {
            ["HomeFinancialRevision"] = [JsonSerializer.Serialize(new object[] { 1L, token })],
            ["HomeLedgerReadState"] = [JsonSerializer.Serialize(new object[] { 1L, token, token, 123L, 1L, 1L })],
            ["HomeLedgerReadBlocks"] = [JsonSerializer.Serialize(new object[] { rowId >> 9, 1L, payload, SHA256.HashData(payload) })],
            ["HomeLedgerDirtyBlocks"] = [], ["Entries"] = [JsonSerializer.Serialize(entry)],
            ["Accounts"] = ["original account"], ["Settings"] = ["original settings"]
        });
    }
}
