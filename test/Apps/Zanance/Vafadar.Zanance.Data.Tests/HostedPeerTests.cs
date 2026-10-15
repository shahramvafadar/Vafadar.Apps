using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Approved public-origin authority remains distinct from explicit local certificate pins.</summary>
[Trait("AT", "AT-293")]
public sealed class HostedPeerTests
{
    /// <summary>Only the approved standard HTTPS origin can use system trust and stable identity binding.</summary>
    [Fact]
    public void Approved_public_origin_uses_system_trust_and_canonical_identity_binding()
    {
        var peer = new LocalSyncPeer("https://api.zanance.vafadar.pro", "");
        var canonical = new LocalSyncPeer("https://API.ZANANCE.VAFADAR.PRO:443/", " ");
        Assert.True(peer.UsesSystemTrust); Assert.Equal("", peer.Fingerprint);
        Assert.Equal(peer.BindingKey("fictitious-owner-a"), canonical.BindingKey("fictitious-owner-a"));
        Assert.NotEqual(peer.BindingKey("fictitious-owner-a"), peer.BindingKey("fictitious-owner-b"));
    }

    /// <summary>A hostname, redirect shape or local-pin override cannot widen the approved hosted authority.</summary>
    [Theory]
    [InlineData("http://api.zanance.vafadar.pro/", "")]
    [InlineData("https://other.vafadar.pro/", "")]
    [InlineData("https://api.zanance.vafadar.pro.other.invalid/", "")]
    [InlineData("https://api.zanance.vafadar.pro./", "")]
    [InlineData("https://api.zanance.vafadar.pro:8443/", "")]
    [InlineData("https://api.zanance.vafadar.pro/identity", "")]
    [InlineData("https://api.zanance.vafadar.pro/?redirect=local", "")]
    [InlineData("https://api.zanance.vafadar.pro/#local", "")]
    [InlineData("https://fictional@api.zanance.vafadar.pro/", "")]
    [InlineData("https://api.zanance.vafadar.pro/", "1111111111111111111111111111111111111111111111111111111111111111")]
    [InlineData("https://localhost:14543/", "")]
    public void Unapproved_authority_or_ambiguous_trust_is_refused(string address, string fingerprint) =>
        Assert.Throws<ArgumentException>(() => new LocalSyncPeer(address, fingerprint));
}
