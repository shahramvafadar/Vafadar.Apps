using System.Net;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// Who the limits per hour count by (step 19.3, found in review 18.7): the network of a connection, so that a
/// visitor with a whole network of IPv6 addresses has one budget and not one for each of them.
/// </summary>
public sealed class SenderTests
{
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    [InlineData("2001:db8:12:34:aaaa:bbbb:cccc:dddd", "2001:db8:12:34::/64")]
    [InlineData("2001:db8:12:34::1", "2001:db8:12:34::/64")]
    [InlineData("::1", "::/64")]
    public void An_address_is_counted_by_its_network(string address, string key)
    {
        Assert.Equal(key, Sender.Of(IPAddress.Parse(address)));
    }

    [Fact]
    public void Two_addresses_of_one_network_share_a_budget_and_two_networks_do_not()
    {
        Assert.Equal(Sender.Of(IPAddress.Parse("2001:db8:12:34::1")), Sender.Of(IPAddress.Parse("2001:db8:12:34:ffff:ffff:ffff:ffff")));
        Assert.NotEqual(Sender.Of(IPAddress.Parse("2001:db8:12:34::1")), Sender.Of(IPAddress.Parse("2001:db8:12:35::1")));
        Assert.NotEqual(Sender.Of(IPAddress.Parse("203.0.113.7")), Sender.Of(IPAddress.Parse("203.0.113.8")));
        Assert.Equal("unknown", Sender.Of((IPAddress?)null));
    }
}
