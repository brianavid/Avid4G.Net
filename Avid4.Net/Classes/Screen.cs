using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Management; // requires adding System.Management reference to project

/// <summary>
/// Class to control the screen, using HDMI-CEC commands issued through an HDMI-CEC HTTP service
/// </summary>
public static class Screen
{
    #region Win32 Native methods
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern SafeFileHandle CreateFile(
        string lpFileName,
        [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(SafeFileHandle hObject);

    [DllImport("Kernel32.DLL", CharSet = CharSet.Auto,
           SetLastError = true)]
    private extern static
        bool GetDevicePowerState(
            SafeFileHandle hDevice,
            out bool fOn);
    #endregion

    /// <summary>
    /// Send an HDMI-CEC command string encoded in an HTTP URL to the control service tray application
    /// </summary>
    /// <param name="command"></param>
    /// <returns>Success</returns>
    static bool SendHdmiCecCommand(
        string command)
    {
        try
        {
	        Uri requestUri = new Uri("http://localhost:12997/control/Send?RawCommand='" + command + "'");
	
	        HttpWebRequest request =
	            (HttpWebRequest)HttpWebRequest.Create(requestUri);
	        request.Method = WebRequestMethods.Http.Get;
	
	        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
	        return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception)
        {
            return false;
        }
    }

    //  Some time after turning the screen on through HDMI-CEC, the TV screen and receiver may decide that the 
    //  screen's own TV is to be displayed. This is not what we want.
    //  So for some period (30 seconds) after turning the screen on, we poll the receiver with the intention of
    //  triggering a side-effect of reseting its input to what we had set within Avid.
    //  This background thread is only to reset the unwanted side-effect of the HDMI behaviour of the receiver
    private static System.Threading.Timer tmrThreadingTimer = null;
    private static DateTime tmrStarted = DateTime.MinValue;
    private static void tmrTick(Object stateInfo)
    {
        if ((DateTime.Now - tmrStarted).TotalSeconds > 30)
        {
            tmrThreadingTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }
        Receiver.GetState();
    }

    /// <summary>
    /// Turn the screen on by issuing the appropriate HDMI-CEC command to device 0 (which is always the TV screen).
    /// Then catch any asynchronous unwanted consequential change of input on the receiver
    /// </summary>
    static void TurnOn()
    {
        SendHdmiCecCommand("!x0 04");
        isOn = true;

        tmrThreadingTimer = new System.Threading.Timer(tmrTick, null, 1000, 1000);
        tmrStarted = DateTime.Now;
    }

    /// <summary>
    /// Is the screen really on (irrespective of our state)?
    /// </summary>
    /// <returns></returns>
    public static bool TestScreenOn()
    {
        //  If we are watching Sky, it does not matter if the screen is on
        if (Running.RunningProgram == "Sky")
        {
            return isOn;
        }

        //  The only reliable way I can determine if the screen is on is if the display is other than the "Generic PnP Monitor"
        //  It appears that when there is no output screen switched on, the media PC's display reverts to "Generic PnP Monitor".
        //  When the screen is on, it is something more specific.
        //  We can get this information through ManagementObjectCollection
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM CIM_Display");
        ManagementObjectCollection collection = searcher.Get();

        bool fOn = false;
        foreach (ManagementObject obj in collection)
        {
            string caption = (string)obj["Caption"];
            if (caption != null && caption != "Generic PnP Monitor")
            {
                fOn = true;
            }
        }

        isOn = fOn;

        return fOn;
    }

    /// <summary>
    /// Wait for the screen to turn on before any further activity (such as starting a full-screen player
    /// application that needs to know the screen size).
    /// </summary>
    public static void WaitForScreenOn()
    {
        for (int i = 0; i < 30; i++)
        {
            if (TestScreenOn())
            {
                return;
            }

            System.Threading.Thread.Sleep(500);
        }
    }

    /// <summary>
    /// Ensure that the screen is on - we do this by turning it on!
    /// </summary>
    public static void EnsureScreenOn()
    {
        TurnOn();

        if (currentMode == 0)
        {
            currentMode = 1;
        }

    }

    /// <summary>
    /// Turn the screen off by issuing the appropriate HDMI-CEC command to device 0 (which is always the TV screen).
    /// </summary>
    static void TurnOff()
    {
        SendHdmiCecCommand("!x0 36");
        isOn = false;
    }

    /// <summary>
    /// Turn the screen on or off as requested. Also optionally turn on/off JRMC music visualization
    /// </summary>
    /// <param name="mode">0: Off; 1: On/Normal; 2: On/Visualize (JRMC only)</param>
    public static void SetScreenDisplayMode(
        int mode)
    {
        if (mode == 0)
        {
            TurnOff();

            if (Running.RunningProgram == "Music")
            {
                JRMC.SendCommand("Control/MCC?Command=22009&Parameter=1");
            }
        }
        else
        {
            if (!Receiver.IsOn())
            {
                Receiver.SelectTVOutput();
            }
            TurnOn();

            if (Running.RunningProgram == "Music")
            {
                JRMC.SendCommand("Control/MCC?Command=22009&Parameter=" + (mode == 2 ? "2" : "0"));
            }
        }

        currentMode = mode;
    }

    /// <summary>
    /// Is the screen currently believed to be on?
    /// </summary>
    public static bool IsOn
    {
        get { return isOn; }
    }
    static bool isOn = false;

    /// <summary>
    /// The current mode : 0: Off; 1: On/Normal; 2: On/Visualize (JRMC only)
    /// </summary>
    public static int CurrentMode { get { return !isOn || currentMode == 0 ? 0 : Running.RunningProgram == "Music" ? currentMode : 1;  } }
    static int currentMode;
}