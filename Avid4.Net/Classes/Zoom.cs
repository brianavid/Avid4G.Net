using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Net;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

/// <summary>
/// Summary description for Zoom
/// </summary>
public class Zoom
{
    static string host = null;
    public static string Host 
    {
        get { return "http://localhost:4768/"; }
    }

    public static string Url
    {
        get { return Host + "&zpfunc="; }
    }

    static bool socketReading = true;
    static Thread reader = null;
    static TcpClient client = null;
    static NetworkStream networkStream = null;

    static void ReadSynchronously()
    {
        client = new TcpClient();

        while (socketReading)
        {
            try
            {
                client.Connect("localhost", 4769);
            }
            catch (System.Exception ex)
            {
                Thread.Sleep(1000);
                continue;
            }

            if (client.Connected)
            {
                try
                {
	                using (NetworkStream ns = client.GetStream())
	                {
	                    networkStream = ns;
	                    using (StreamReader sr = new StreamReader(ns))
	                    {
	                        while (socketReading && client.Connected)
	                        {
	                            string line = sr.ReadLine();
	                            if (line == null)
	                            {
	                                break;
	                            }
	                            lock (typeof(Zoom))
	                            {
	                                ProcessReceivedLine(line);
	                            }
	                        }
	                    }
	                }
                }
                catch (System.Exception ex)
                {
                	
                }
                networkStream = null;
            }

            client.Close();
            client = new TcpClient();
        }
    }

    private static void ProcessReceivedLine(
        string line)
    {
        switch (line.Substring(0, 4))
        {
            case "1000":
                switch (Convert.ToInt32(line.Substring(5)))
                {
                    default:
                        state = "Unknown";
                        durationMs = 0;
                        positionMs = 0;
                        break;
                    case 0:
                        state = "Closed";
                        durationMs = 0;
                        positionMs = 0;
                        break;
                    case 1:
                        state = "Stopped";
                        durationMs = 0;
                        positionMs = 0;
                        break;
                    case 2:
                        state = "Paused";
                        SendRequest("1110");
                        SendRequest("1120");
                        break;
                    case 3:
                        state = "Playing";
                        SendRequest("1110");
                        SendRequest("1120");
                        break;
                }
                break;
            case "1100":
                SendRequest("1110");
                SendRequest("1120");
                break;
            case "1110":
                durationMs = Convert.ToInt32(line.Substring(5));
                break;
            case "1120":
                positionMs = Convert.ToInt32(line.Substring(5));
                break;
            case "1300":
                switch (Convert.ToInt32(line.Substring(5)))
                {
                    default:
                        mode = "Unknown";
                        break;
                    case 0:
                        mode = "DVD";
                        IsDvdMode = true;
                        SendRequest("1420");
                        break;
                    case 1:
                        mode = "Media";
                        IsDvdMode = false;
                        break;
                    case 2:
                        mode = "Audio";
                        IsDvdMode = false;
                        break;
                }
                break;
            case "1420":
                switch (Convert.ToInt32(line.Substring(5)))
                {
                    default:
                        if (IsDvdMode)
                        {
                            mode = "DVD";
                        }
                        break;
                    case 1:
                        mode = "Menu";
                        break;
                }
                break;
        }
    }

    public static void Start()
	{
        if (reader != null && reader.IsAlive)
        {
            reader.Abort();
        }

        reader = new Thread(ReadSynchronously);
        socketReading = true;
        reader.Start();
	}

	public static void Stop()
	{
        socketReading = false;
        for (int i = 0; i < 20; i++)
        {
            if (reader == null || !reader.IsAlive)
            {
                break;
            }
            Thread.Sleep(1000);
        }

        if (reader != null && reader.IsAlive)
        {
            reader.Abort();
        }

        reader = null;
    }

    public static void SendRequest(
        string code)
    {
        if (networkStream != null && networkStream.CanWrite)
        {
            byte[] myWriteBuffer = Encoding.ASCII.GetBytes(code + "\r\n");
            try
            {
	            networkStream.Write(myWriteBuffer, 0, myWriteBuffer.Length);
            }
            catch (System.Exception ex)
            {
            	
            }
        }
    }

    public static XElement GetInfo()
    {
        lock (typeof(Zoom))
        {
            var x = new XDocument(
                new XElement("Response",
                    new XAttribute("Status", "OK"),
                    new XElement("Item",
                        new XAttribute("Name", "PositionMS"),
                        positionMs.ToString()),
                    new XElement("Item",
                        new XAttribute("Name", "DurationMS"),
                        durationMs.ToString()),
                    new XElement("Item",
                        new XAttribute("Name", "ElapsedTimeDisplay"),
                        String.Format("{0}:{1:00}", positionMs/60000, (positionMs / 1000)%60)),
                    new XElement("Item",
                        new XAttribute("Name", "TotalTimeDisplay"),
                        String.Format("{0}:{1:00}", durationMs / 60000, (durationMs / 1000) % 60)),
                    new XElement("Item",
                        new XAttribute("Name", "State"),
                        state),
                    new XElement("Item",
                        new XAttribute("Name", "Mode"),
                        mode)));
            return x.Root;
        }
    }

    public static string Title { get; set; }

    static int positionMs = 0;
    static int durationMs = 0;
    static string state = "Unknown";
    static string mode = "Unknown";

    public static bool IsCurrentlyPlaying { get { return state == "Playing" || state == "Paused"; } }

    public static bool IsDvdMode { get; set; }
}