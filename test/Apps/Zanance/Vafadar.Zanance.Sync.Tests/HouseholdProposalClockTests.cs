using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Sync.Tests;

/// <summary>Actual service temporal policy against client-generated deadlines; no signature, membership or financial acceptance is inferred.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdProposalClockTests
{
    /// <summary>The observed two-second positive Android offset made a full-budget declaration fail the original service policy.</summary>
    [Fact]
    public void Full_server_budget_from_a_two_seconds_ahead_client_is_rejected_by_actual_service_policy()
    {
        var server = new DateTimeOffset(2026, 10, 16, 11, 40, 0, TimeSpan.Zero);
        var original = Statement(server.AddSeconds(2).Add(HouseholdLimits.MembershipProposalLifetime).ToUnixTimeSeconds());
        var refused = Assert.Throws<HouseholdProtocolRefusal>(() => HouseholdMetadataProofs.RequireCurrentProposal(original, server));
        Assert.Equal(409, refused.Status);
    }

    /// <summary>Small clock differences work while excessive future clocks and already expired declarations remain refused.</summary>
    [Theory]
    [InlineData(-2, true)]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(59, true)]
    [InlineData(61, false)]
    [InlineData(-900, false)]
    public void Client_headroom_respects_actual_unchanged_service_expiry_and_future_bounds(int clientOffsetSeconds, bool accepted)
    {
        var server = new DateTimeOffset(2026, 10, 16, 11, 40, 0, TimeSpan.Zero);
        var proposal = Statement(HouseholdLimits.ClientDeadline(server.AddSeconds(clientOffsetSeconds), HouseholdLimits.MembershipProposalLifetime));
        if (accepted) HouseholdMetadataProofs.RequireCurrentProposal(proposal, server);
        else Assert.Throws<HouseholdProtocolRefusal>(() => HouseholdMetadataProofs.RequireCurrentProposal(proposal, server));
    }

    /// <summary>Only the deadline is interpreted by the isolated real temporal validator.</summary>
    private static HouseholdMembershipStatement Statement(long deadline) => new(1, Guid.NewGuid(), Guid.NewGuid(),
        HouseholdMembershipChange.Genesis, null, 0, 1, new('0', 64), "fictional-owner", "fictional-owner",
        new("unused-public-signing-key", "unused-public-wrapping-key"), 1, new('a', 64), new('b', 64), deadline, []);
}
