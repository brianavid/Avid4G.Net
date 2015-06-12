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
using NLog;

/// <summary>
/// Class to control the screen, using unofficially documented discrete power on/off IR Codes
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

    static Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Turn the screen on by issuing the appropriate discrete power on IR Code
    /// </summary>
    static void TurnOn()
    {
        DesktopClient.SendIR(IRCodes.Codes["TV.PowerOn"], "TV.PowerOn");

        isOn = true;
    }

    /// <summary>
    /// Is the screen really on (irrespective of our state)?
    /// </summary>
    /// <returns></returns>
    static bool TestScreenOn()
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
            if (caption != null)
            {
                if (caption != "Generic PnP Monitor")
                {
                    fOn = true;
                }
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
        logger.Info("WaitForScreenOn");

        LogDisplayStatus();

        for (int i = 0; i < 30; i++)
        {
            if (TestScreenOn())
            {
                logger.Info("Screen is now on");

                break;
            }

            System.Threading.Thread.Sleep(500);
        }

        LogDisplayStatus();
    }

    /// <summary>
    /// Log the screen state for diagnosis purposes
    /// </summary>
    [Conditional("LOG_SCREEN_STATE")]
    private static void LogDisplayStatus()
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM CIM_Display");
        ManagementObjectCollection collection = searcher.Get();

        foreach (ManagementObject obj in collection)
        {
            foreach (var prop in obj.Properties)
            {
                logger.Info("{0} -> {1}", prop.Name, prop.Value);
            }
        }
    }

    /// <summary>
    /// Ensure that the screen is on - we do this by turning it on!
    /// </summary>
    public static void EnsureScreenOn(
        bool exitSmart = true)
    {
        logger.Info("EnsureScreenOn");
        exitSmart &= isOn;

        TurnOn();

        if (currentMode == 0)
        {
            currentMode = 1;
        }

        if (exitSmart)
        {
            Samsung.SendKey("EXIT");
        }

    }

    /// <summary>
    /// Turn the screen off by issuing the appropriate  discrete power off IR Code
    /// </summary>
    static void TurnOff()
    {
        DesktopClient.SendIR(IRCodes.Codes["TV.PowerOff"], "TV.PowerOff");

        isOn = false;
    }

    /// <summary>
    /// Turn the screen on or off as requested. Also optionally turn on/off JRMC music visualization
    /// </summary>
    /// <param name="mode">0: Off; 1: On/Normal; 2: On/Visualize (JRMC only)</param>
    public static void SetScreenDisplayMode(
        int mode)
    {
        logger.Info("SetScreenDisplayMode {0}", mode);

        if (mode == 0)
        {
            TurnOff();

            if (Running.RunningProgram == "Music")
            {
                JRMC.SetDisplay(JRMC.DisplayMode.Mini);
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
                JRMC.SetDisplay(mode == 2 ? JRMC.DisplayMode.Display : JRMC.DisplayMode.Standard);
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