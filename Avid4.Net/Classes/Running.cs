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
    /// When was there last activity with the running program?
    /// </summary>
    static DateTime lastActive = DateTime.UtcNow;

    /// <summary>
    /// Initialize, detecting if Sky is running
    /// </summary>
    public static void Initialize()
    {
        if (Receiver.SelectedInput == "Sky")
        {
            runningProgram = "Sky";
        }

        if (Receiver.SelectedInput == "Roku")
        {
            runningProgram = "Roku";
        }

        if (Receiver.SelectedInput == "TV")
        {
            runningProgram = "SmartTv";
        }

        if (Receiver.SelectedInput == "Chromecast")
        {
            runningProgram = "Chromecast";
        }

        //  Start a background thread to poll for an inactive screen-off player and so turn it off after
        //  a short while
        var activityChecker = new Thread(ActivityChecker);
        activityChecker.Start();
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
                case "Roku":
                    return "topBarRoku";
                case "SmartTv":
                    return "topBarSmartTv";
                case "Chromecast":
                    return "topBarChromecast";
                case "Curzon":
                    return "topBarCurzon";
                case "Prime":
                    return "topBarPrime";
                case "LogFire":
                    return "topBarLogFire";
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
        logger.Info("LaunchProgram {0} -> {1} {2}", runningProgram, name, args ?? "");

        lastActive = DateTime.UtcNow; 
        
        runningArgs = args;

        if (name != "Sky")
        {
            StopSky();
        }
        Receiver.SelectComputerInput();

        if (name == "Music")
        {
            if (runningProgram != name)
            {
                if (runningProgram == "Photo")
                {
                    ExitJRMC();
                }
                Zoom.Stop();
                DvbViewer.Stop();
                Spotify.Stop();
                DesktopClient.ExitAllPrograms();
                runningProgram = name;
                Screen.SetScreenDisplayMode(0);
                Receiver.SelectRoomsOutput();
            }

            JRMC.SetDisplay(JRMC.DisplayMode.Standard, maximize: true);
            logger.Info("LaunchProgram OK {0}", runningProgram);
            return true;
        }

        if (runningProgram == name && String.IsNullOrEmpty(args))
        {
            if (runningProgram != "Spotify" && !DesktopClient.ForegroundProgram(name))
            {
                Zoom.Stop();
                DvbViewer.Stop();
                Spotify.Stop();
                DesktopClient.ExitAllPrograms();
                NothingRunning();
                return false;
            }
            logger.Info("LaunchProgram OK {0}", runningProgram);
            return true;
        }

        if (runningProgram == "Music" || runningProgram == "Photo")
        {
            ExitJRMC();
        }

        if (runningProgram == "TV")
        {
            DvbViewer.Stop();
        }

        runningProgram = name;

        switch (name)
        {
            default:
                return false;

            case "TV":
                if (args != null && args == "Radio")
                {
                    Screen.SetScreenDisplayMode(0);
                    Receiver.SelectRoomsOutput();
                }
                else
                {
                    Screen.EnsureScreenOn();
                    Receiver.SelectTVOutput();
                    Screen.WaitForScreenOn();
                }
                if (!DesktopClient.LaunchProgram("TV", args))
                {
                    NothingRunning();
                    return false;
                }
                logger.Info("LaunchProgram OK {0}", runningProgram);
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
                logger.Info("LaunchProgram OK {0}", runningProgram);
                return true;

            case "Video":
                Screen.EnsureScreenOn();
                Receiver.SelectTVOutput();
                //Screen.WaitForScreenOn();

                if (args != null ? 
                    !DesktopClient.LaunchNewProgram("Video", args) : 
                    !DesktopClient.LaunchProgram("Video", args))
                {
                    NothingRunning();
                    return false;
                }

                logger.Info("Zoom.Start");
                Zoom.Start();
                logger.Info("LaunchProgram OK {0}", runningProgram);
                return true;

            case "Spotify":
                Screen.SetScreenDisplayMode(0);
                Receiver.SelectRoomsOutput();
                DesktopClient.ExitAllPrograms();
                DesktopClient.EnsureSpotifyRunning();
                logger.Info("LaunchProgram OK {0}", runningProgram);
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

        lastActive = DateTime.UtcNow;

        runningArgs = "";

        if (name == "Photo")
        {
            if (runningProgram == "Photo")
            {
                ExitJRMC();
                Thread.Sleep(500);
            }

            Zoom.Stop();
            DvbViewer.Stop();
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
                logger.Info("LaunchProgram OK {0}", runningProgram);
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

        lastActive = DateTime.UtcNow;

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
            DvbViewer.Stop();
            Spotify.Stop();
        }

        runningProgram = "Sky";
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    private static void StopSky()
    {
        if (runningProgram == "Sky")
        {
            DesktopClient.SendIR(IRCodes.Codes["Sky.PowerSTB"], "Sky.PowerSTB");
        }
    }

    /// <summary>
    /// Note that we are starting streaming, and so stop all media PC player applications
    /// </summary>
    /// <returns></returns>
    public static bool StartStream(
        string streamSource)
    {
        logger.Info("StartStream: " + streamSource);

        if (runningProgram != streamSource)
        {
            if (runningProgram == "Music" || runningProgram == "Photo")
            {
                ExitJRMC();
            }
            StopSky();
            DesktopClient.ExitAllPrograms();
            DesktopClient.SendSpecialkey("ClearDesktop");
            NothingRunning();
        }

        runningProgram = streamSource;
        return true;
    }

    /// <summary>
    /// Assert (and ensure) that nothing is running
    /// </summary>
    static void NothingRunning()
    {
        Zoom.Stop();
        DvbViewer.Stop();
        Spotify.Stop();
        runningProgram = "";
        logger.Info("NothingRunning");
    }


    /// <summary>
    /// Is the currently running player showing signs of activity?
    /// </summary>
    /// <returns></returns>
    static Boolean IsActive()
    {
        //  If a music player is stopped or paused, it may have been forgotten
        switch (runningProgram)
        {
            default:
                //  If the screen is off and the volume is muted, it may have been forgotten
                //  So treat as inactive
                return Screen.IsOn || !Receiver.VolumeMuted;
            case "Music":
                return JRMC.IsActivelyPlaying();
            case "Spotify":
                return Spotify.GetPlaying() > 0;
            case "Video":
                return Zoom.IsCurrentlyActive;
        }

    }


    /// <summary>
    /// On a background thread, poll for a silent player and turn everything off after a short while.
    /// </summary>
    static void ActivityChecker()
    {
        for (;;)
        {
            Thread.Sleep(60 * 1000);   //  Every minute, check for activity
            if (IsActive())
            {
                lastActive = DateTime.UtcNow;
            }

            //  If the receiver is on and there has been no activity for 15 minutes,
            //  turn everything off
            if (Receiver.IsOn() && lastActive.AddMinutes(15) < DateTime.UtcNow)
            {
                logger.Info("No activity from {0} since {1} - Exiting", runningProgram, lastActive.ToShortTimeString());
                ExitAllPrograms(false);
            }
        }
    }
}