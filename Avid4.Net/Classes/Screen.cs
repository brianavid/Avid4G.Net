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
/// Summary description for Screen
/// </summary>
public static class Screen
{
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
        catch (System.Exception ex)
        {
            return false;
        }
    }

    static System.Threading.Timer tmrThreadingTimer = null;
    static DateTime tmrStarted = DateTime.MinValue;
    private static void tmrTick(Object stateInfo)
    {
        if ((DateTime.Now - tmrStarted).TotalSeconds > 30)
        {
            tmrThreadingTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }
        Receiver.GetState();
    }

    static void TurnOn()
    {
        SendHdmiCecCommand("!x0 04");
        isOn = true;

        tmrThreadingTimer = new System.Threading.Timer(tmrTick, null, 2000, 2000);
        tmrStarted = DateTime.Now;
    }

    public static bool TestScreenOn()
    {
        if (Running.RunningProgram == "Sky")
        {
            return isOn;
        }

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

    public static void EnsureScreenOn()
    {
        TurnOn();

        if (currentMode == 0)
        {
            currentMode = 1;
        }

    }

    static void TurnOff()
    {
        SendHdmiCecCommand("!x0 36");
        isOn = false;
    }

    public static void SetScreenDisplayMode(
        int mode) // 0: Off; 1: On/Normal; 2: On/Visualize
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

    public static int GetDisplayMode()
    {
        var displayMode = JRMC.GetDisplayMode();

        if (currentMode != 0)
        {
            if (displayMode == 2 && Running.RunningProgram == "Music")
            {
                currentMode = 2;
            }
            else
            {
                currentMode = 1;
            }
        }

        return displayMode;
    }

    static bool isOn = false;
    public static bool IsOn
    {
        get { return isOn; }
    }
    static int currentMode;
    public static int CurrentMode { get { return !isOn || currentMode == 0 ? 0 : Running.RunningProgram == "Music" ? currentMode : 1;  } }
}