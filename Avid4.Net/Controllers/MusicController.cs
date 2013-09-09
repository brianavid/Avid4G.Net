using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;

namespace Avid4.Net.Controllers
{
    public class MusicController : Controller
    {
        //
        // GET: /Music/

        public ActionResult All()
        {
            ViewBag.Mode = "Library";

            return View();
        }

        public ActionResult Playing()
        {
            return View();
        }

        public ActionResult Queue()
        {
            return View();
        }

        public ActionResult QueuePane()
        {
            return View();
        }

        public ActionResult Browser(
            string mode,
            string id,
            string query)
        {
            ViewBag.Mode = mode;
            if (id != null)
            {
                ViewBag.Id = id;
            }
            if (query != null)
            {
                ViewBag.Query = query;
            }

            return View();
        }

        public ActionResult BrowserPane(
            string mode,
            string id,
            string query,
            string date,
            string station,
            string append)
        {
            ViewBag.Mode = mode;
            if (id != null)
            {
                ViewBag.Id = id;
            }
            if (query != null)
            {
                ViewBag.Query = query;
            }
            if (append != null)
            {
                ViewBag.Append = append;
            }
            if (date != null)
            {
                ViewBag.Date = date;
            }
            if (station != null)
            {
                ViewBag.Station = station;
            }

            return View();
        }

        public ContentResult GetPlayingInfo()
        {
            StringWriter writer = new StringWriter();
            JRMC.GetPlaybackInfo().Save(writer);
            return this.Content(writer.ToString(), @"text/xml", writer.Encoding);
        }

        public ContentResult SendMCWS(
            string url,
            string noStream)
        {
            if (url.StartsWith("Playback/PlayByKey?"))
            {
                JRMC.ClearStreaming();
            }

            if (!string.IsNullOrEmpty(noStream) && JRMC.IsStreaming())
            {
                return this.Content("");
            }

            XDocument doc = JRMC.GetXml(JRMC.Url + url);
            return this.Content(doc.ToString(), @"text/xml");
        }

        public ContentResult RemoveQueuedTrack(
            string id)
        {
            JRMC.RemoveQueuedTrack(id);
            return this.Content("");
        }

        public ContentResult PlayListenAgain(
            string pid)
        {
            string name;
            string station;
            DateTime startTime;
            string streamUrl = BBC.GetStreamUrl(pid, out name, out station, out startTime);
            JRMC.SetStreaming(name, station, startTime);
            JRMC.GetXml(JRMC.Url + "Control/CommandLine?Arguments=/Play " + HttpUtility.UrlEncode(streamUrl));
            return this.Content("");
        }


        public ActionResult GetListenAgainIcon()
        {
            var dir = Server.MapPath("/Content");
            var path = Path.Combine(dir, "BBC Radio.jpg");
            return base.File(path, "image/jpeg");
        }
    }
}
