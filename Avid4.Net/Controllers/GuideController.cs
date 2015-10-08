using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    public class GuideController : Controller
    {
        // GET: /Guide/Browser
        public ActionResult Browser(
            string mode)
        {
            ViewBag.Mode = mode;
            return View("Browser");
        }

        // GET: /Guide/BrowserWide
        public ActionResult BrowserWide(
            string mode)
        {
            ViewBag.Mode = mode;
            return View("BrowserWide");
        }

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

            return PartialView();
        }

        // GET: /Guide/SelectorPane
        public ActionResult SelectorPane(
            string mode)
        {
            ViewBag.Mode = mode;

            return PartialView();
        }

        // GET: /Guide/Listings
        public ActionResult ListingsPane(
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

            return PartialView();
        }

        // GET: /Guide/Description
        public ContentResult Description(
            string id,
            string channelName)
        {
            return this.Content(DvbViewer.EpgProgramme(id, channelName).Description);
        }

        // GET: /Guide/Record
        public ContentResult Record(
            string id,
            string channelName)
        {
            DvbViewer.AddTimer(id, channelName);
            return this.Content("");
        }

        // GET: /Guide/RecordSeries
        public ContentResult RecordSeries(
            string id,
            string channelName)
        {
            DvbViewer.AddTimer(id, channelName, true);
            return this.Content("");
        }

        // GET: /Guide/Cancel
        public ContentResult Cancel(
            string id)
        {
            DvbViewer.CancelTimer(id);
            return this.Content("");
        }

        // GET: /Guide/CancelSeries
        public ContentResult CancelSeries(
            string id)
        {
            DvbViewer.Series.Delete(id);
            return this.Content("");
        }

    }
}
