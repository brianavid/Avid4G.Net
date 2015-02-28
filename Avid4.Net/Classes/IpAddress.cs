using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Web;

/// <summary>
/// Utility class to determine if the client IP address is on the local LAN and therefore to allow full access
/// External clients are restricted to using the EPG only for remote recording
/// </summary>
public class IpAddress
{
    /// <summary>
    /// Is this a local LAN address
    /// </summary>
    /// <remarks>
    /// For simplicity, this is based on a textual representation of IPV4 or IPV6 addresses.
    /// In the future, this could perhaps be determined with more of an understanding of the address formats
    /// </remarks>
    /// <param name="address"></param>
    /// <returns></returns>
    public static bool IsLanIP(String address)
    {
        address = address.ToLower();

        return address == "127.0.0.1" ||        //  IPV4 local machine
               address == "::1" ||              //  IPV6 local machine
               address.StartsWith("192.168.") ||//  IPV4 local addresses as used in domestic routers
               address.StartsWith("fc") ||      //  IPV6 fc/7 Unique Local address range
               address.StartsWith("fd") ||      //  IPV6 fc/7 Unique Local address range
               address.StartsWith("fe");        //  IPV6 fe/7 Unique Local address range
    }

#if MORE_ALGORITHMIC_DETERMINATION
    static bool IsLanIP(IPAddress address)
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (var iface in interfaces)
        {
            var properties = iface.GetIPProperties();
            foreach (var ifAddr in properties.UnicastAddresses)
            {
                if (ifAddr.IPv4Mask != null &&
                    ifAddr.Address.AddressFamily == AddressFamily.InterNetwork &&
                    CheckMask(ifAddr.Address, ifAddr.IPv4Mask, address))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool CheckMask(IPAddress address, IPAddress mask, IPAddress target)
    {
        if (mask == null)
        {
            return false;
        }

        var ba = address.GetAddressBytes();
        var bm = mask.GetAddressBytes();
        var bb = target.GetAddressBytes();

        if (ba.Length != bm.Length || bm.Length != bb.Length)
            return false;

        for (var i = 0; i < ba.Length; i++)
        {
            int m = bm[i];

            int a = ba[i] & m;
            int b = bb[i] & m;

            if (a != b)
            {
                return false;
            }
        }

        return true;
    }
#endif
}
