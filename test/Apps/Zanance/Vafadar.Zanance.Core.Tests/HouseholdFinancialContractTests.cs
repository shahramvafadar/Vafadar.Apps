using System.Text.Json;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Financial scope and conflict-family isolation; household annotations must never become readable personal finance.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdFinancialContractTests
{
    /// <summary>The independent household contract adds reviewed facts without changing already delivered personal compatibility.</summary>
    [Fact]
    public void Household_contact_cannot_enter_the_personal_graph_even_after_its_descriptor_is_known()
    {
        Assert.Equal("7a1ec01b41bddf51d896b49ea050a5c7bf85f3293140797ba608b4eee9549a1e", SyncSchema.Hash);
        var id = Guid.CreateVersion7(); var key = new SyncRecordKey("HouseholdContacts", JsonSerializer.Serialize(new[] { id.ToString("D").ToUpperInvariant() }));
        var body = JsonSerializer.SerializeToElement(new { Id = id.ToString("D").ToUpperInvariant(), Name = "Fictitious financial contact",
            IdentityId = (string?)null, IsArchived = 0, CreatedAt = 0 });
        Assert.Equal("HouseholdContacts", SyncSchema.Describe(key.Table).Name);
        Assert.Contains(SyncGraphValidation.Validate([new(key, 0, body)]), issue => issue.Reason == "UnknownTable");
        Assert.Empty(SyncGraphValidation.Validate([new(key, 0, body)], HouseholdFinancialSchema.Manifest));
        Assert.NotEqual(SyncSchema.Hash, HouseholdFinancialSchema.Hash);
    }

    /// <summary>Attacker-chosen equal GUIDs cannot drag an unrelated cash group into a cost-share consent.</summary>
    [Fact]
    public void Cost_group_expansion_does_not_include_a_ledger_group_with_the_same_guid()
    {
        var group = Guid.CreateVersion7(); var contact = Guid.CreateVersion7();
        var cost = new SyncProposal(new("HouseholdCosts", "cost"), 0, JsonSerializer.SerializeToElement(new { Id = group }));
        var share = new SyncProposal(new("HouseholdAllocations", "share"), 0, JsonSerializer.SerializeToElement(new { CostId = group, ContactId = contact }));
        var entry = new SyncProposal(new("Entries", "entry"), 0, JsonSerializer.SerializeToElement(new { GroupId = group }));
        var result = SyncConflictGrouping.Expand([new(cost, null, null, [], "ConflictingCost", LinkedGroup: group)],
            new[] { cost, share, entry }.ToDictionary(row => row.Record), new Dictionary<SyncRecordKey, SyncRecord>());
        Assert.Equal(2, result.Count);
        Assert.Contains(result, row => row.Proposal.Record == share.Record);
        Assert.DoesNotContain(result, row => row.Proposal.Record == entry.Record);
    }
}
