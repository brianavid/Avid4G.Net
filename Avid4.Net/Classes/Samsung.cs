using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Web;
using NLog;

/// <summary>
/// Class to support sending remote key presses to a Samsung TV
/// </summary>
public static class Samsung
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    const int TvPort = 55000;
    const string RemoteName = "Avid";
    const string AppName = "avid";

    /// <summary>
    /// The local machine's network mac address in the form "XX-XX-XX-XX-XX-XX-XX-XX"
    /// </summary>
    static string MacAddress
    {
        get
        {
            if (macAddress == null)
            {
                //  Get the address, which is returned in a format without hyphens
                var compactAddress =
                (
                    from nic in NetworkInterface.GetAllNetworkInterfaces()
                    where nic.OperationalStatus == OperationalStatus.Up
                    select nic.GetPhysicalAddress().ToString()
                ).FirstOrDefault();

                if (compactAddress != null)
                {
	                //  Add a hyphen between pairs of characters
	                var sb = new StringBuilder();
	                for (int i = 0; i < compactAddress.Length; i++)
	                {
	                    if (i != 0 && i % 2 == 0)
	                    {
	                        sb.Append('-');
	                    }
	                    sb.Append(compactAddress[i]);
	                }
	
	                macAddress = sb.ToString();
                }
            }
            return macAddress;
        }
    }
    static string macAddress = null;

    /// <summary>
    /// Encode a string in Base64
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    /// <summary>
    /// Append a byte array to a byte stream
    /// </summary>
    /// <param name="s"></param>
    /// <param name="bytes"></param>
    static void AppendBytes(
        this Stream s,
        byte[] bytes)
    {
        s.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Append a byte array to a byte stream preceded by a 
    /// two-byte (little endian) count of the array size
    /// </summary>
    /// <param name="s"></param>
    /// <param name="bytes"></param>
    static void AppendCountedBytes(
        this Stream s,
        byte[] bytes)
    {
        s.AppendBytes(new byte[] { (byte)bytes.Length, 0x00 });
        s.AppendBytes(bytes);
    }

    /// <summary>
    /// Append a (UTF8) string as to a byte stream with a count
    /// </summary>
    /// <param name="s"></param>
    /// <param name="text"></param>
    static void AppendString(
        this Stream s,
        string text)
    {
        s.AppendCountedBytes(System.Text.Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Append a string encoded as Base64 to a byte stream
    /// </summary>
    /// <param name="s"></param>
    /// <param name="text"></param>
    static void AppendBase64(
        this Stream s,
        string text)
    {
        s.AppendString(Base64Encode(text));
    }

    /// <summary>
    /// Send a named Key Press to the TV
    /// </summary>
    /// <param name="keyName"></param>
    public static void SendKey(
        string keyName)
    {
        try
        {
	        //  A special encoding to avoid affecting the TV during testing
	        if (Config.TvAddress != Config.IpAddress)
	        {
	            //  Open and close the TCP connection every time,
	            //  as we don't know when the TV has been turned off
	            using (TcpClient conn = new TcpClient(Config.TvAddress, TvPort))
	            {
	                //  First, authenticate the local IP Address and Mac address with the TV
	                MemoryStream msMsg = new MemoryStream();
	                var msPkt = conn.GetStream();
	
	                msMsg.AppendBytes(new byte[] { 0x64, 0x00 });
	                msMsg.AppendBase64(Config.IpAddress);
	                msMsg.AppendBase64(MacAddress);
	                msMsg.AppendBase64(RemoteName);
	
	                msPkt.AppendBytes(new byte[] { 0x00 });
	                msPkt.AppendString(AppName);
	                msPkt.AppendCountedBytes(msMsg.ToArray());
	
	                //  Then send the named key
	                msMsg = new MemoryStream();
	
	                msMsg.AppendBytes(new byte[] { 0x00, 0x00, 0x00 });
	                msMsg.AppendBase64("KEY_" + keyName);
	
	                msPkt.AppendBytes(new byte[] { 0x00 });
	                msPkt.AppendString(AppName);
	                msPkt.AppendCountedBytes(msMsg.ToArray());
	            }
	        }
        }
        catch (System.Exception ex)
        {
            logger.Error("Can't send key press '{0}': {1}", keyName, ex);
        }
    }
}