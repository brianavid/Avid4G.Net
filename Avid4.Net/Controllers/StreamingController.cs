using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    public class StreamingController : Controller
    {
        void HideUnwantedUI()
        {
            switch (Running.RunningProgram)
            {
                case "Roku":
                case "SmartTV":
                case "Chromecast":
                case "LogFire":
                    ViewBag.Title = Running.RunningProgram;
                    break;
                default:
                    ViewBag.Title = "";
                    break;
            }
            if (Running.RunningProgram != "")
            {
                ViewBag.HideStreamGuidanceClass = "startHidden";
            }
            if (Running.RunningProgram != "Roku")
            {
                ViewBag.HideRokuClass = "startHidden";
            }
            if (Running.RunningProgram != "SmartTv")
            {
                ViewBag.HideSmartClass = "startHidden";
            }
        }

        // GET: /Streaming/Controls
        public ActionResult Controls()
        {
            HideUnwantedUI();
            return View();
        }

        // GET: /Streaming/Browser
        public ActionResult Browser()
        {
            HideUnwantedUI();
            return View();
        }

        // GET: /Streaming/All
        public ActionResult All()
        {
            HideUnwantedUI();
            return View();
        }

        public ContentResult RokuLaunch(
            string id)
        {
            Roku.RunApp(id);
            return this.Content("");
        }

        public ContentResult KeyDown(
            string id)
        {
            Roku.KeyDown(id);
            return this.Content("");
        }

        public ContentResult KeyUp(
            string id)
        {
            Roku.KeyUp(id);
            return this.Content("");
        }

        public ContentResult KeyPress(
            string id)
        {
            Roku.KeyPress(id);
            return this.Content("");
        }

        public ContentResult SendText(
            string text)
        {
            Roku.SendText(text);
            return this.Content("");
        }

        // GET: /Streaming/SendTvKey
        public ActionResult SendTvKey(
            string keyName)
        {
            Samsung.SendKey(keyName);
            return Content("");
        }

        static bool isPlaying = true;

        // GET: /Streaming/SmartTvPlayPause
        public ActionResult SmartTvPlayPause()
        {
            isPlaying = !isPlaying;
            Samsung.SendKey(isPlaying ? "PLAY" : "PAUSE");
            return Content("");
        }


    }
}
