using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using NLog;

/// <summary>
/// Class to keep track of what player application is currently running
/// </summary>
public static class Running
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Avid name for the currently running player application
    /// </summary>
    static string runningProgram = "";

    /// <summary>
    /// Avid name for the currently running player application
    /// Arguments to the 
    /// </summary>
    static string runningArgs = "";

    /// <summary>
    /// Avid name for the currently running player application
    /// </summary>
    public static String RunningProgram { get { return runningProgram; } }

    /// <summary>
    /// Initialize, detecting if Sky is running
    /// </summary>
    public static void Initialize()
    {
        if (Receiver.SelectedInput == "Sky")
        {
            runningProgram = "Sky";
        }
    }

    /// <summary>
    /// Return a CSS class name which can be used to style (colour) the UI top bar based on the running player application 
    /// </summary>
    public static string RunningProgramTopBarClass
    {
        get
        {
            switch (runningProgram)
            {
                default:
                    return "topBarNone";
                case "Music":
                    return "topBarMusic";
                case "TV":
                case "Radio":
                    return "topBarTv";
                case "Sky":
                    return "topBarSky";
                case "Spotify":
                    return "topBarSpotify";
                case "Video":
                    return "topBarVideo";
                case "Web":
                    return "topBarIplayer";
                case "Photo":
                    return "topBarPhotos";
            }
        }
    }

    /// <summary>
    /// Launch a specified player application, closing any others as appropriate, and configuring
    /// the screen and receiver to suit the preferred outputs fr that player
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static bool LaunchProgram(
        string name,
        string args)
    {
        logger.Info("LaunchProgram {0} -> {1}", runningProgram, name);

        runningArgs = args;

        if (name != "Sky")
        {
            if (runningProgram == "Sky")
            {
                DesktopClient.SendIR(IRCodes.Codes["Sky.PowerSTB"], "Sky.PowerSTB");
            }
            Receiver.SelectComputerInput();
        }

        if (name == "Music")
        {
            if (runningProgram != name)
            {
                if (runningProgram == "Photo")
                {
                    ExitJRMC();
                }
                Zoom.Stop();
                Spotify.Stop();
                DesktopClient.ExitAllPrograms();
                runningProgram = name;
                Screen.SetScreenDisplayMode(0);
                Receiver.SelectRoomsOutput();
            }

            JRMC.SetDisplay(JRMC.DisplayMode.Standard, maximize: true);
            return true;
        }

        if (runningProgram == name && String.IsNullOrEmpty(args))
        {
            if (runningProgram != "Spotify" && !DesktopClient.ForegroundProgram(name))
            {
                Zoom.Stop();
                Spotify.Stop();
                DesktopClient.ExitAllPrograms();
                NothingRunning();
                return false;
            }
            return true;
        }

        if (runningProgram == "Music" || runningProgram == "Photo")
        {
            ExitJRMC();
        }

        runningProgram = name;

        switch (name)
        {
            default:
                return false;

            case "TV":
                Screen.EnsureScreenOn();
                Receiver.SelectTVOutput();
                Screen.WaitForScreenOn();
                if (!DesktopClient.LaunchProgram("TV", args))
                {
                    NothingRunning();
                    return false;
                }
                return true;

            case "Radio":
                Screen.SetScreenDisplayMode(0);
                Receiver.SelectRoomsOutput();
                if (!DesktopClient.LaunchProgram("TV", args))
                {
                    NothingRunning();
                    return false;
                }
                return true;

            case "Web":
                Screen.EnsureScreenOn();
                Receiver.SelectTVOutput();
                Screen.WaitForScreenOn();
                if (!DesktopClient.LaunchProgram("Web", args))
                {
                    NothingRunning();
                    return false;
                }
                return true;

            case "Video":
                Screen.EnsureScreenOn();
                Receiver.SelectTVOutput();
                Screen.WaitForScreenOn();

                if (!DesktopClient.LaunchProgram("Video", args))
                {
                    NothingRunning();
                    return false;
                }

                Zoom.Start();
                return true;

            case "Spotify":
                Screen.SetScreenDisplayMode(0);
                Receiver.SelectRoomsOutput();
                DesktopClient.ExitAllPrograms();
                return true;
        }
    }

    /// <summary>
    /// Launch the player applictaion (which will only be the Photo viewer) leaving any JRMC music still playing
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static bool LaunchNewProgram(
        string name,
        string args)
    {
        logger.Info("LaunchNewProgram {0} -> {1}", runningProgram, name);

        runningArgs = "";

        if (name == "Photo")
        {
            if (runningProgram == "Photo")
            {
                ExitJRMC();
                Thread.Sleep(500);
            }

            Zoom.Stop();
            Spotify.Stop();
            DesktopClient.ExitAllPrograms();
            Receiver.SelectComputerInput();
            Receiver.ReselectInput();
            Screen.EnsureScreenOn();
            Receiver.SelectRoomsOutput();
            Screen.WaitForScreenOn();
            runningProgram = "Photo";

            if (DesktopClient.LaunchNewProgram(name, args))
            {
                JRMC.SetDisplay(JRMC.DisplayMode.Display, maximize: true);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Exit all running programmes
    /// </summary>
    /// <param name="keepScreen"></param>
    /// <returns></returns>
    public static bool ExitAllPrograms(
        bool keepScreen = false)
    {
        logger.Info("ExitAllPrograms");

        if (runningProgram == "Music" || runningProgram == "Photo")
        {
            ExitJRMC();
        }

        if (runningProgram == "Sky")
        {
            DesktopClient.SendIR(IRCodes.Codes["Sky.PowerSTB"], "Sky.PowerSTB");
            Receiver.SelectComputerInput();
        }

        if (!keepScreen)
        {
            Receiver.TurnOff();
	        Screen.SetScreenDisplayMode(0);
        }
        else
        {
            Screen.EnsureScreenOn();
            Receiver.SelectTVOutput();
            Screen.WaitForScreenOn();
        }

        bool ok = DesktopClient.ExitAllPrograms();

        NothingRunning();

        return ok;
    }

    /// <summary>
    /// Command the JRMC player to stop and and hide itself
    /// </summary>
    private static void ExitJRMC()
    {
        JRMC.ExitDisplay(runningProgram == "Photo");
    }

    /// <summary>
    /// Note that we are starting the Sky box, and so stop all media PC player applications
    /// </summary>
    /// <returns></returns>
    public static bool StartSky()
    {
        logger.Info("StartSky");

        if (runningProgram != "Sky")
        {
            if (runningProgram == "Music" || runningProgram == "Photo")
            {
                ExitJRMC();
            }

            DesktopClient.ExitAllPrograms();
            DesktopClient.SendSpecialkey("ClearDesktop");
            Zoom.Stop();
            Spotify.Stop();
        }

        runningProgram = "Sky";
        return true;
    }

    /// <summary>
    /// Assert (and ensure) that nothing is running
    /// </summary>
    static void NothingRunning()
    {
        Zoom.Stop();
        Spotify.Stop();
        runningProgram = "";
    }
}