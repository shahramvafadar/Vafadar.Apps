using System.Net;
using System.Net.Sockets;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// Who a request comes from, for the limits per hour of the site (accounts, reports, notices): the network of
/// the address, not the address itself. A connection over IPv6 is usually given a whole network of
/// 2<sup>64</sup> addresses and may change among them at will, so each of its addresses would have a budget of
/// its own; the first 64 bits name the connection. An address of IPv4 names it as it is. The key lives in the
/// memory of the process for the hour it counts and is written nowhere.
/// </summary>
public static class Sender
{
    /// <summary>Returns the key of who sent a request.</summary>
    public static string Of(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Of(context.Connection.RemoteIpAddress);
    }

    /// <summary>Returns the key of an address; a request without one – from inside the process – is <c>unknown</c>.</summary>
    public static string Of(IPAddress? address)
    {
        if (address is null)
        {
            return "unknown";
        }

        // An address of IPv4 that came over a socket of both families is that address of IPv4.
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }
}
