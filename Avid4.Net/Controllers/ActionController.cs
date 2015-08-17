using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Avid4.Net;
using NLog;

namespace Avid4.Net.Controllers
{
    public class ActionController : Controller
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        // GET: /Action/GetRunning
        public ContentResult GetRunning()
        {
            return this.Content(Running.RunningProgram);
        }

        // GET: /Action/VolumeUp
        public ActionResult VolumeUp()
        {
            Receiver.IncreaseVolume();
            return Content(Receiver.VolumeDisplay);
        }

        // GET: /Action/VolumeDown
        public ActionResult VolumeDown()
        {
            Receiver.DecreaseVolume();
            return Content(Receiver.VolumeDisplay);
        }

        // GET: /Action/VolumeMute
        public ActionResult VolumeMute()
        {
            Receiver.ToggleMute();
            return Content(Receiver.VolumeDisplay);
        }

        // GET: /Action/VolumeGet
        public ActionResult VolumeGet()
        {
            return Content(Receiver.VolumeDisplay);
        }

        // GET: /Action/Launch
        public ActionResult Launch(
            string name,
            string args,
            string title,
            string detach)
        {
            if (String.IsNullOrEmpty(Running.RunningProgram))
            {
                DesktopClient.SendSpecialkey("ClearDesktop");
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (!string.IsNullOrEmpty(args))
                {
                    args = HttpUtility.UrlDecode(args);
                }
                if (!string.IsNullOrEmpty(detach))
                {
                    Running.LaunchNewProgram(name, args);
                }
                else
                {
                    Running.LaunchProgram(name, args);
                }
            }

            return Content("OK");
        }

        // GET: /Action/StartSky
        public ActionResult StartSky(
            string mode)
        {
            bool isAlreadyRunningSky = Running.RunningProgram == "Sky";

            if (mode != "radio")
            {
                Screen.EnsureScreenOn();
            }
            else if (!isAlreadyRunningSky)
            {
                Screen.SetScreenDisplayMode(0);
            }

            Running.StartSky();

            Receiver.SelectSkyInput();
            if (mode == "radio")
            {
                Receiver.SelectRoomsOutput();
            }
            else
            {
                Receiver.SelectTVOutput(null, false);
            }

            if (!isAlreadyRunningSky)
            {
                Receiver.SetMute(true);
            }

            DesktopClient.SendIR(IRCodes.Codes["Sky.Watch"], "Sky.Watch");

            if (!isAlreadyRunningSky)
            {
                System.Threading.Thread.Sleep(2000);
                SkyData.Sky.ChangeChannel(0);
            }

            switch (mode)
            {
                case "planner":
                    return Content("/Sky/Recordings");
                case "live":
                    return Content("/Sky/Live");
                case "radio":
                    return Content("/Sky/Radio");
            }
            return Content("/Sky/Watch");
        }

        // GET: /Action/AllOff
        public ActionResult AllOff(
            string keep)
        {
            try
            {
	            Running.ExitAllPrograms(!string.IsNullOrEmpty(keep));
	            DesktopClient.SendSpecialkey("ClearDesktop");
	            return Content(Receiver.VolumeDisplay);
            }
            catch (System.Exception ex)
            {
                logger.Error("Error in AllOff: {0}", ex);
                return Content("Error");
            }
        }

        // GET: /Action/MouseMove
        public ActionResult MouseMove(
            string dx,
            string dy)
        {
            DesktopClient.MouseMoveRelative(Convert.ToInt32(dx), Convert.ToInt32((dy)));
            return Content("");
        }

        // GET: /Action/MouseClick
        public ActionResult MouseClick(
            string right)
        {
            DesktopClient.MouseClick(!String.IsNullOrEmpty(right));
            return Content("");
        }

        // GET: /Action/SendKeys
        public ActionResult SendKeys(
            string keys)
        {
            DesktopClient.SendKeys(keys);
            return Content("");
        }

        // GET: /Action/SendIR
        public ActionResult SendIR(
            string id)
        {
            DesktopClient.SendIR(IRCodes.Codes[id], id);
            return Content("");
        }

        // GET: /Action/ScreenOff
        public ActionResult ScreenOff()
        {
            Screen.SetScreenDisplayMode(0);
            return Content("");
        }

        // GET: /Action/ScreenOn
        public ActionResult ScreenOn()
        {
            Screen.SetScreenDisplayMode(1);
            return Content("");
        }

        // GET: /Action/VisualOn
        public ActionResult VisualOn()
        {
            Screen.SetScreenDisplayMode(Running.RunningProgram == "Music" ? 2 : 1);
            if (Running.RunningProgram == "Spotify")
            {
                DesktopClient.LaunchProgram("GForce", "");
            }
            return Content("");
        }

        // GET: /Action/StartStream
        public ActionResult StartStream()
        {
            Screen.EnsureScreenOn(Running.RunningProgram != "SmartTv");
            var streamProgram = "";
            switch (Running.RunningProgram)
            {
                case "LogFire":
                case "Chromecast":
                case "Roku":
                case "SmartTv":
                case "Curzon":
                case "Music":
                case "Spotify":
                    streamProgram = Running.RunningProgram;
                    break;
            }
            Running.StartStream(streamProgram);
            Receiver.TurnOn();
            return Content("");
        }

        // GET: /Action/GoLogFire
        public ActionResult GoLogFire()
        {
            Screen.EnsureScreenOn();
            if (Running.RunningProgram != "Music" && Running.RunningProgram != "Spotify")
            {
                Running.StartStream("LogFire");
            }
            Receiver.SelectComputerInput();
            DesktopClient.LaunchProgram("LogFire", null);
            return Content("");
        }

        // GET: /Action/GoCurzon
        public ActionResult GoCurzon()
        {
            Screen.EnsureScreenOn();
            Running.StartStream("Curzon");
            Receiver.SelectComputerInput();
            DesktopClient.LaunchProgram("Curzon", null);
            return Content("");
        }

        // GET: /Action/GoChromecast
        public ActionResult GoChromecast()
        {
            Screen.EnsureScreenOn();
            Running.StartStream("Chromecast");
            Receiver.SelectChromecastInput();
            Receiver.SelectTVOutput();
            return Content("");
        }

        // GET: /Action/GoChromecastAudio
        public ActionResult GoChromecastAudio()
        {
            Running.StartStream("Chromecast");
            Receiver.SelectChromecastInput();
            Receiver.SelectRoomsOutput();
            Screen.SetScreenDisplayMode(0);
            return Content("");
        }

        // GET: /Action/GoRoku
        public ActionResult GoRoku()
        {
            Roku.KeyPress("Home");
            Screen.EnsureScreenOn();
            Running.StartStream("Roku");
            Receiver.SelectRokuInput();
            Receiver.SelectTVOutput();
            return Content("");
        }

        // GET: /Action/GoSmart
        public ActionResult GoSmart()
        {
            Screen.EnsureScreenOn(false);
            Running.StartStream("SmartTv");
            Screen.WaitForScreenOn();
            Receiver.SelectTvInput();
            Receiver.SelectTVOutput();
            Samsung.SendKey("CONTENTS");
            return Content("");
        }

        // GET: /Action/SoundTV
        public ActionResult SoundTV(
            string mode)
        {
            Receiver.SelectTVOutput(mode);
            return Content("");
        }

        // GET: /Action/SoundRooms
        public ActionResult SoundRooms()
        {
            Receiver.SelectRoomsOutput();
            return Content("");
        }

        // GET: /Action/RebuildMediaDb
        public ActionResult RebuildMediaDb()
        {
            if (Running.RunningProgram != "Video" && Running.RunningProgram != "Spotify")
            {
                JRMC.LoadAndIndexAllAlbums(new string[] { "1", "2" }, true);
            }
            DvbViewer.CleanupRefreshDB();
            Spotify.LoadAndIndexAllSavedTracks();
            return Content("");
        }

        // GET: /Action/RecycleApp
        public ActionResult RecycleApp()
        {
            Spotify.ExitPlayer();

            HttpRuntime.UnloadAppDomain();
            return Content("");
        }

    }
}
