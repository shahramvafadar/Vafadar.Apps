using System.Text.Json;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Explicit restore authorization and logical receipt ownership, independent of clocks or guessed money.</summary>
public sealed class PersonalSyncRestoreTests
{
    [Fact]
    public void A_deletion_missing_from_the_authentic_baseline_requires_review()
    {
        var restored = JsonSerializer.SerializeToElement(new { Amount = 1250L });
        var result = SyncRowMerge.Merge(null, null, restored, currentIsTombstone: true);
        Assert.Null(result.Values); Assert.Contains("$deleted", result.Conflicts);
    }

    [Fact]
    public void Exact_current_deleted_baseline_allows_the_owners_explicit_local_undo()
    {
        var restored = JsonSerializer.SerializeToElement(new { Amount = 1250L });
        var result = SyncRowMerge.Merge(null, null, restored, currentIsTombstone: true, knownCurrentDeletion: true);
        Assert.Empty(result.Conflicts); Assert.Equal(1250, result.Values!.Value.GetProperty("Amount").GetInt64());
    }

    [Fact]
    public void A_live_historical_baseline_cannot_be_used_as_current_deleted_authority()
    {
        var baseline = JsonSerializer.SerializeToElement(new { Amount = 1000L });
        var restored = JsonSerializer.SerializeToElement(new { Amount = 1250L });
        var result = SyncRowMerge.Merge(baseline, null, restored, currentIsTombstone: true, knownCurrentDeletion: true);
        Assert.Null(result.Values); Assert.NotEmpty(result.Conflicts);
    }

    [Fact]
    public void A_wire_receipt_needs_a_live_parent_even_though_local_undo_can_retain_orphan_bytes()
    {
        var id = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var missing = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var key = new SyncRecordKey("Attachments", JsonSerializer.Serialize(new[] { id }));
        var values = JsonSerializer.SerializeToElement(new { Id = id, EntryId = missing, FileName = "fictitious.pdf",
            ContentType = "application/pdf", CreatedAt = 0L, Data = new { sha256 = new string('1', 64), length = 1L } });
        var problem = Assert.Single(SyncGraphValidation.Validate([new(key, 1, values)]));
        Assert.Equal("RelatedRecordMissing", problem.Reason); Assert.Equal(new[] { "EntryId" }, problem.Fields);
        Assert.Equal(new SyncRecordKey("Entries", JsonSerializer.Serialize(new[] { missing })), problem.RelatedRecord);
    }
}
