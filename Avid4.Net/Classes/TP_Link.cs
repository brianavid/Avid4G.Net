using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Web;
using NLog;
using System.Web.Hosting;

static class MemoryStreamExtensions
{
    public static void Append(this MemoryStream stream, byte value)
    {
        stream.Append(new[] { value });
    }

    public static void Append(this MemoryStream stream, byte[] values)
    {
        stream.Write(values, 0, values.Length);
    }
}

public class TP_Link
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    static int Port = 9999;
    static string SockOnCommand = "{\"system\":{\"set_relay_state\":{\"state\":1}}}";
    static string SockOffCommand = "{\"system\":{\"set_relay_state\":{\"state\":0}}}";
    static string BulbOnCommand = "{\"smartlife.iot.smartbulb.lightingservice\":{\"transition_light_state\":{\"on_off\":1, \"transition_period\": 0}}}";
    static string BulbOffCommand = "{\"smartlife.iot.smartbulb.lightingservice\":{\"transition_light_state\":{\"on_off\":0, \"transition_period\": 0}}}";

    /// <summary>
    /// TP_Link TCP commands are pseudo-encrypted (obfuscated)
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    static byte[] Encrypt(byte[] s)
    {
        MemoryStream stream = new MemoryStream();
        stream.Append(0);
        stream.Append(0);
        stream.Append(0);
        stream.Append((byte)s.Length);
        var key = 171;
        foreach (var c1 in s)
        {
            var c2 = c1 ^ key;
            key = c2;
            stream.Append((byte)c2);
        }
        return stream.ToArray();
    }

    /// <summary>
    /// TP_Link TCP responses are pseudo-encrypted (obfuscated)
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    static byte[] Decrypt(byte[] s)
    {
        MemoryStream stream = new MemoryStream();
        var key = 171;
        foreach (var c1 in s.Skip(4))
        {
            var c2 = c1 ^ key;
            key = c1;
            stream.Append((byte)c2);
        }
        return stream.ToArray();
    }

    /// <summary>
    /// Send a command (on a worker thread) to the specified TP_Link device
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="command"></param>
    static void Send(
        string ipAddress,
        string command)
    {
        HostingEnvironment.QueueBackgroundWorkItem(ct => {
            try
            {
                var client = new TcpClient();
                client.Connect(ipAddress, Port);
                NetworkStream ns = client.GetStream();
                //logger.Info(command);
                byte[] commandBytes = Encoding.ASCII.GetBytes(command);
                byte[] myWriteBuffer = Encrypt(commandBytes);
                ns.Write(myWriteBuffer, 0, myWriteBuffer.Length);
                byte[] responseBuf = new byte[2048];
                var responseLen = ns.Read(responseBuf, 0, 2048);
                var response = responseBuf.Take(responseLen).ToArray();
                //logger.Info(Encoding.ASCII.GetString(Decrypt(response)));
            }
            catch (Exception)
            {
                //logger.Error(ex, "Failed to control device at {0}", ipAddress);
            }
        });
    }

    /// <summary>
    /// Turn on a TP_Link device
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="deviceName"></param>
    /// <param name="isSocket"></param>
    public static void TurnOn(
        string ipAddress,
        string deviceName,
        bool isSocket)
    {
        logger.Info("Turn on {0}", deviceName);
        Send(ipAddress, isSocket ? SockOnCommand : BulbOnCommand);
    }

    /// <summary>
    /// Turn off a TP_Link device
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="deviceName"></param>
    /// <param name="isSocket"></param>
    public static void TurnOff(
        string ipAddress,
        string deviceName,
        bool isSocket)
    {
        logger.Info("Turn off {0}", deviceName);
        Send(ipAddress, isSocket ? SockOffCommand : BulbOffCommand);
    }

}
