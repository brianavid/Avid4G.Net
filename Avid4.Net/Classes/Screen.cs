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
    static Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Turn the screen on by issuing the appropriate HDMI-CEC command to device 0 (which is always the TV screen).
    /// </summary>
    static void TurnOn()
    {
        DesktopClient.TvScreenOn();

        isOn = true;
    }

    /// <summary>
    /// Is the screen really on (irrespective of our state)?
    /// </summary>
    /// <returns></returns>
    static bool TestScreenOn()
    {
        //  If we are watching an external source, it does not matter if the screen is on
        if (Receiver.SelectedInput != "Computer")
        {
            return isOn;
        }

        return DesktopClient.TvScreenIsOn();
    }

    /// <summary>
    /// Wait for the screen to turn on before any further activity (such as starting a full-screen player
    /// application that needs to know the screen size).
    /// </summary>
    public static void WaitForScreenOn()
    {
        logger.Info("WaitForScreenOn");

        for (int i = 0; i < 30; i++)
        {
            if (TestScreenOn())
            {
                logger.Info("Screen is now on");

                break;
            }

            System.Threading.Thread.Sleep(500);
        }
    }

    /// <summary>
    /// Ensure that the screen is on - we do this by turning it on!
    /// </summary>
    /// <param name="exitSmart">True to exit the currently on screen from its "SmartTV" mode</param>
    public static void EnsureScreenOn(
        bool exitSmart = true)
    {
        logger.Info("EnsureScreenOn");

        //  If it wasn't previously on, it can't have been in its "SmartTV" mode 
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
    /// Turn the screen off by issuing the appropriate HDMI-CEC command to device 0 (which is always the TV screen).
    /// </summary>
    static void TurnOff()
    {
        DesktopClient.TvScreenOff();
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