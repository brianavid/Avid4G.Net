using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Web.Mvc;

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

            return View();
        }

        public ContentResult GetPlayingInfo()
        {
            StringWriter writer = new StringWriter();
            JRMC.GetPlaybackInfo().Save(writer);
            return this.Content(writer.ToString(), @"text/xml", writer.Encoding);
        }

        public ContentResult SendMCWS(
            string url)
        {
            XDocument doc = JRMC.GetXml(JRMC.Url + url);
            return this.Content(doc.ToString(), @"text/xml");
        }

        public ContentResult RemoveQueuedTrack(
            string id)
        {
            JRMC.RemoveQueuedTrack(id);
            return this.Content("");
        }

    }
}
