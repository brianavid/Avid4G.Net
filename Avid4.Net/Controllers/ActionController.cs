using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Avid4.Net;

namespace Avid4.Net.Controllers
{
    public class ActionController : Controller
    {
        public ContentResult GetRunning()
        {
            return this.Content(Running.RunningProgram);
        }

        //
        // GET: /Action/

        public ActionResult VolumeUp()
        {
            Receiver.IncreaseVolume();
            return Content(Receiver.VolumeDisplay);
        }

        public ActionResult VolumeDown()
        {
            Receiver.DecreaseVolume();
            return Content(Receiver.VolumeDisplay);
        }

        public ActionResult VolumeMute()
        {
            if (Running.RunningProgram == "TV")
            {
                RemotePotato.SendCommand("VolMute");
            }
            else
            {
                Receiver.ToggleMute();
            }
            return Content(Receiver.VolumeDisplay);
        }

        public ActionResult VolumeGet()
        {
            return Content(Receiver.VolumeDisplay);
        }

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

        public ActionResult StartSky(
            string mode)
        {
            bool isAlreadyRunningSky = Running.RunningProgram == "Sky";

            if (mode != "radio")
            {
                Screen.EnsureScreenOn();
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
                System.Threading.Thread.Sleep(2000);
                Receiver.SetMute(true);

                DesktopClient.SendIR(IRCodes.Codes["Sky.Watch"], "Sky.Watch");
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

        public ActionResult AllOff(
            string keep)
        {
            Running.ExitAllPrograms(!string.IsNullOrEmpty(keep));
            DesktopClient.SendSpecialkey("ClearDesktop");
            return Content(Receiver.VolumeDisplay);
        }

        public ActionResult MouseMove(
            string dx,
            string dy)
        {
            DesktopClient.MouseMoveRelative(Convert.ToInt32(dx), Convert.ToInt32((dy)));
            return Content("");
        }

        public ActionResult MouseClick(
            string right)
        {
            DesktopClient.MouseClick(!String.IsNullOrEmpty(right));
            return Content("");
        }

        public ActionResult SendIR(
            string id)
        {
            System.Diagnostics.Trace.WriteLine(id);
            DesktopClient.SendIR(IRCodes.Codes[id], id);
            return Content("");
        }

        public ActionResult ScreenOff()
        {
            Screen.SetScreenDisplayMode(0);
            return Content("");
        }

        public ActionResult ScreenOn()
        {
            Screen.SetScreenDisplayMode(1);
            return Content("");
        }

        public ActionResult SoundTV()
        {
            Receiver.SelectTVOutput(null);
            return Content("");
        }

        public ActionResult SoundRooms()
        {
            Receiver.SelectRoomsOutput();
            return Content("");
        }

        public ActionResult RebuildMediaDb()
        {
            JRMC.LoadAndIndexAllAlbums(new string[] { "1", "2" }, true);
            DesktopClient.EnsureRemotePotatoRunning(true);
            return Content("");
        }

        public ActionResult RecycleApp()
        {
            HttpRuntime.UnloadAppDomain();
            return Content("");
        }

    }
}
