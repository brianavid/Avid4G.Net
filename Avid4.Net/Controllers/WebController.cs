using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    public class WebController : Controller
    {
        //
        // GET: /Web/Mouse

        public ActionResult Mouse()
        {
            return View();
        }

        //
        // GET: /Web/All

        public ActionResult All()
        {
            ViewBag.Mode = "iPlayerSelect";

            return View();
        }

        public ActionResult Browser(
            string mode)
        {
            ViewBag.Mode = mode;

            return View();
        }

        public ActionResult BrowserPane(
            string mode,
            string date,
            string channel)
        {
            ViewBag.Mode = mode;
            if (date != null)
            {
                ViewBag.Date = date;
            }
            if (channel != null)
            {
                ViewBag.Channel = channel;
            }

            return View();
        }

        public ContentResult PlayBBC(
            string pid)
        {
            if (String.IsNullOrEmpty(Running.RunningProgram))
            {
                DesktopClient.SendSpecialkey("ClearDesktop");
            }

            Running.LaunchProgram("Web", "-k -nomerge http://www.bbc.co.uk/iplayer/bigscreen/tv/episode/" + pid);

            return Content("OK");
        }

    }
}
