#define USE_IR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Web;
using NLog;

public static class Samsung
{
    static Logger logger = LogManager.GetCurrentClassLogger();

#if USE_IR

    static Dictionary<string, string> irMap = null;

    static Dictionary<string, string> IrMap
    {
        get
        {
            if (irMap == null)
            {
                irMap = new Dictionary<string, string>();

                irMap["0"] = "TV.0";
                irMap["1"] = "TV.1";
                irMap["2"] = "TV.2";
                irMap["3"] = "TV.3";
                irMap["4"] = "TV.4";
                irMap["5"] = "TV.5";
                irMap["6"] = "TV.6";
                irMap["7"] = "TV.7";
                irMap["8"] = "TV.8";
                irMap["9"] = "TV.9";
                irMap["CONTENTS"] = "TV.Smart";
                irMap["UP"] = "TV.Up";
                irMap["LEFT"] = "TV.Left";
                irMap["ENTER"] = "TV.Select";
                irMap["RIGHT"] = "TV.Right";
                irMap["DOWN"] = "TV.Down";
                irMap["RETURN"] = "TV.Return";
                irMap["EXIT"] = "TV.Exit";
                irMap["RED"] = "TV.Red";
                irMap["GREEN"] = "TV.Green";
                irMap["YELLOW"] = "TV.Yellow";
                irMap["BLUE"] = "TV.Blue";
                irMap["PLAY"] = "TV.Play";
                irMap["PAUSE"] = "TV.Pause";
                irMap["FWD"] = "TV.Fwd";
                irMap["REWIND"] = "TV.Rewind";
                irMap["INFO"] = "TV.Info";
            }
            return irMap;
        }
    }

    public static void SendKey(
        string keyName)
    {
        if (IrMap.ContainsKey(keyName))
        {
            DesktopClient.SendIR(IRCodes.Codes[IrMap[keyName]], keyName);
        }
    }
#else

    const int TvPort = 55000;
    const string RemoteName = "Avid";
    const string AppName = "avid";
    const string TvName = "UE46D6000";

    static string MacAddress
    {
        get
        {
            if (macAddress == null)
            {
                var compactAddress =
                (
                    from nic in NetworkInterface.GetAllNetworkInterfaces()
                    where nic.OperationalStatus == OperationalStatus.Up
                    select nic.GetPhysicalAddress().ToString()
                ).FirstOrDefault();
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
            return macAddress;
        }
    }
    static string macAddress = null;

    static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    static void AppendBytes(
        this MemoryStream ms,
        byte[] bytes)
    {
        ms.Write(bytes, 0, bytes.Length);
    }

    static void AppendCountedBytes(
        this MemoryStream ms,
        byte[] bytes)
    {
        ms.AppendBytes(new byte[] { (byte)bytes.Length, 0x00 });
        ms.AppendBytes(bytes);
    }

    static void AppendString(
        this MemoryStream ms,
        string text)
    {
        ms.AppendCountedBytes(System.Text.Encoding.UTF8.GetBytes(text));
    }

    static void AppendBase64(
        this MemoryStream ms,
        string text)
    {
        ms.AppendString(Base64Encode(text));
    }

    public static void SendKey(
        string keyName)
    {
        using (UdpClient udp = new UdpClient(Config.TvAddress, TvPort))
        {
            MemoryStream msMsg = new MemoryStream();
            MemoryStream msPkt = new MemoryStream();

            msMsg.AppendBytes(new byte[] { 0x64, 0x00 });
            msMsg.AppendBase64(Config.IpAddress);
            msMsg.AppendBase64(MacAddress);
            msMsg.AppendBase64(RemoteName);

            msPkt.AppendBytes(new byte[] { 0x00 });
            msPkt.AppendString(AppName);
            msPkt.AppendCountedBytes(msMsg.ToArray());

            if (Config.TvAddress != Config.IpAddress)
            {
                var result = udp.Send(msPkt.ToArray(), (int)msPkt.Length);
                if (result != msPkt.Length)
                {
                    logger.Error("Failed to send 1st packet: {0}", result);
                }
            }

            msMsg = new MemoryStream();
            msPkt = new MemoryStream();

            msMsg.AppendBytes(new byte[] { 0x00, 0x00, 0x00 });
            msMsg.AppendBase64("KEY_" + keyName);

            msPkt.AppendBytes(new byte[] { 0x00 });
            msPkt.AppendString(TvName);
            msPkt.AppendCountedBytes(msMsg.ToArray());

            if (Config.TvAddress != Config.IpAddress)
            {
                var result = udp.Send(msPkt.ToArray(), (int)msPkt.Length);
                if (result != msPkt.Length)
                {
                    logger.Error("Failed to send 2nd packet: {0}", result);
                }
                else
                {
                    logger.Info("Sent key: {0}", keyName);
                }
            }
        }
    }
#endif
}