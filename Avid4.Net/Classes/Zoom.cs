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
/// Class to interface with the Zoom Player vis its TCP network or web service interfaces
/// </summary>
public class Zoom
{
    /// <summary>
    /// The default address of the Zoom Player web service interface
    /// </summary>
    static string Host 
    {
        get { return "http://localhost:4768/"; }      //  The default port number for the web service interface
    }

    /// <summary>
    /// The URL (with a function code appended) to send a Zoom Player function command
    /// </summary>
    /// <remarks>
    /// Functions are documented at http://www.inmatrix.com/zplayer/highlights/zpfunctions.shtml
    /// </remarks>
    public static string FuncUrl
    {
        get { return Host + "&zpfunc="; }
    }

    //  Data to control the asynchronous background thread that reads data sent from Zoom Player over its TCP network interface
    static bool socketReading = true;
    static Thread reader = null;
    static TcpClient client = null;
    static NetworkStream networkStream = null;

    /// <summary>
    /// Background thread method to repeatedly read and process data send as lines of text from Zoom Player over its TCP network interface
    /// </summary>
    static void ReadSynchronously()
    {
        client = new TcpClient();

        while (socketReading)
        {
            try
            {
                client.Connect("localhost", 4769);      //  The default port number for the TCP network interface
            }
            catch (System.Exception ex)
            {
                //  Wait for Zoon Player to start running
                Thread.Sleep(1000);
                continue;
            }

            if (client.Connected)
            {
                try
                {
	                using (NetworkStream ns = client.GetStream())
	                {
                        //  Store the network stream globally so that other threads can send commands while 
                        //  data is still being received in the backgound
	                    networkStream = ns;
	                    using (StreamReader sr = new StreamReader(ns))
	                    {
	                        while (socketReading && client.Connected)
	                        {
	                            string line = sr.ReadLine();
	                            if (line == null)
	                            {
                                    //  Exit reading and reconnect when Zoonm Player next starts
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

    /// <summary>
    /// Process the received line in the Zoom Player
    /// </summary>
    /// <remarks>
    /// The event codes in the line are documented in http://forum.inmatrix.com/index.php?showtopic=7051
    /// We are only currently interested in a very small subset of the available codes
    /// </remarks>
    /// <param name="line"></param>
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

    /// <summary>
    /// Start receiving and processing asynchronous data from Zoom Player
    /// </summary>
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

    /// <summary>
    /// Stop receiving and processing asynchronous data from Zoom Player
    /// </summary>
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

    /// <summary>
    /// Send a TCP network command to Zoom Player using the networkStream opened by the backgroundreading thread
    /// </summary>
    /// <param name="code"></param>
    static void SendRequest(
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

    /// <summary>
    /// Return the current state of the Zoom Player as an XML structure
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// The title of the video or DVD that is currently playing
    /// </summary>
    public static string Title { 
        get 
        { 
            return title; 
        }
        set
        {
            title = value;
            state = "Playing";
        }
    }
    static string title;

    /// <summary>
    /// The asynchronously maintain state of the video or DVD that is currently playing 
    /// </summary>
    static int positionMs = 0;
    static int durationMs = 0;
    static string state = "Unknown";
    static string mode = "Unknown";

    /// <summary>
    /// Is Zoom Player currently playing (including if paused)?
    /// </summary>
    public static bool IsCurrentlyPlaying { get { return state == "Playing" || state == "Paused"; } }

    /// <summary>
    /// Is Zoom Player currently in its "DVD" mode as opposed to its "Media" mode
    /// </summary>
    public static bool IsDvdMode { get; set; }
}