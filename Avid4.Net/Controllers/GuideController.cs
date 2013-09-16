using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    public class GuideController : Controller
    {
        //
        // GET: /Guide/Browser

        public ActionResult Browser(
            string mode)
        {
            ViewBag.Mode = mode;
            return View();
        }

        //
        // GET: /Guide/BrowserWide

        public ActionResult BrowserWide(
            string mode)
        {
            ViewBag.Mode = mode;
            return View();
        }

        //
        // GET: /Guide/BrowserPane

        public ActionResult BrowserPane(
            string mode,
            string id,
            string date,
            string channel)
        {
            ViewBag.Mode = mode;
            if (id != null)
            {
                ViewBag.Id = id;
            }
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

        //
        // GET: /Guide/Description

        public ContentResult Description(
            string id)
        {
            return this.Content(RemotePotato.GetDescription(id));
        }

        //
        // GET: /Guide/Record

        public ContentResult Record(
            string id)
        {
            return this.Content(RemotePotato.RecordShow(id));
        }

        //
        // GET: /Guide/Cancel

        public ContentResult Cancel(
            string id)
        {
            return this.Content(RemotePotato.CancelRecording(id));
        }

    }
}
