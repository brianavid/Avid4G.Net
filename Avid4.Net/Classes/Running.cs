using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;

/// <summary>
/// Summary description for Programs
/// </summary>
public static class Running
{
    static string runningProgram = "";
    static string runningArgs = "";
    public static String RunningProgram { get { return runningProgram; } }

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
                case "Sky":
                    return "topBarSky";
                case "Spotify":
                    return "topBarSpotify";
                case "Video":
                    return "topBarVideo";
                case "Web":
                    return "topBarIplayer";
            }
        }
    }

    public static bool LaunchProgram(
        string name,
        string args)
    {
        SomethingRunning();
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
                Receiver.SelectRoomsOutput();
            }

            JRMC.SendCommand("Control/MCC?Command=10027");          //Maximize 
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
                Receiver.SelectRoomsOutput();
                return true;
        }
    }

    public static bool LaunchNewProgram(
        string name,
        string args)
    {
        SomethingRunning();
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
                JRMC.SendCommand("Control/MCC?Command=10027");          //Maximize 
                Thread.Sleep(200);
                JRMC.SendCommand("Control/MCC?Command=22009&Parameter=2");  // Display screen
                return true;
            }
        }

        return false;
    }

    public static bool RedisplayRunningProgram()
    {
        if (Screen.IsOn)
        {
            if (string.IsNullOrEmpty(runningArgs))
            {
                Screen.WaitForScreenOn();
                DesktopClient.ForegroundProgram(runningProgram);
            }
            else
            {
                LaunchProgram(runningProgram, runningArgs);
            }
        }

        return true;
    }

    public static bool ExitAllPrograms(bool keepScreen = false)
    {
        Zoom.Stop();
        Spotify.Stop();

        if (runningProgram == "Music" || runningProgram == "Photo")
        {
            ExitJRMC();
        }

        if (runningProgram == "Sky")
        {
            DesktopClient.SendIR(IRCodes.Codes["Sky.PowerSTB"], "Sky.PowerSTB");
            Receiver.SelectComputerInput();
        }

        NothingRunning();

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

        return DesktopClient.ExitAllPrograms();
    }

    private static void ExitJRMC()
    {
        if (runningProgram == "Photo")
        {
            JRMC.SendCommand("Control/MCC?Command=10049&Parameter=0");  // Clear
        }
        JRMC.SendCommand("Playback/Stop"); 
        JRMC.SendCommand("Control/MCC?Command=22000&Parameter=0");  // Normal screen
        JRMC.SendCommand("Control/MCC?Command=10014");              // Minimize
        //JRMC.SendCommand("Control/Key?key=Alt;F4");                 // exit
    }

    public static bool StartSky()
    {
        if (runningProgram != "Sky")
        {
            if (runningProgram == "Music" || runningProgram == "Photo")
            {
                ExitJRMC();
            }

            Zoom.Stop();
            Spotify.Stop();
            DesktopClient.ExitAllPrograms();
            DesktopClient.SendSpecialkey("ClearDesktop");
        }

        runningProgram = "Sky";
        return true;
    }

    static void NothingRunning()
    {
        Zoom.Stop();
        Spotify.Stop();
        runningProgram = "";
    }

    static void SomethingRunning()
    {
    }
}