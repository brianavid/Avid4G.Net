﻿using System;
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
            return View(Config.UseDvbViewer ? "Browser2" : "Browser");
        }

        // GET: /Guide/BrowserWide
        public ActionResult BrowserWide(
            string mode)
        {
            ViewBag.Mode = mode;
            return View(Config.UseDvbViewer ? "BrowserWide2" : "BrowserWide");
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

        // GET: /Guide/BrowserPane
        public ActionResult BrowserPane2(
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
            string id)
        {
            if (Config.UseDvbViewer)
            {
                return this.Content(DvbViewer.EpgProgramme(id).Description);
            }
            else
            {
                return this.Content(RemotePotato.GetDescription(id));
            }
        }

        // GET: /Guide/Record
        public ContentResult Record(
            string id)
        {
            if (Config.UseDvbViewer)
            {
                DvbViewer.AddTimer(id);
                return this.Content("");
            }
            else
            {
                return this.Content( RemotePotato.RecordShow(id));
            }
        }

        // GET: /Guide/RecordSeries
        public ContentResult RecordSeries(
            string id)
        {
            if (Config.UseDvbViewer)
            {
                DvbViewer.AddTimer(id, true);
                return this.Content("");
            }
            else
            {
                return this.Content(RemotePotato.RecordSeries(id));
            }
        }

        // GET: /Guide/Cancel
        public ContentResult Cancel(
            string id)
        {
            if (Config.UseDvbViewer)
            {
                DvbViewer.CancelTimer(id);
                return this.Content("");
            }
            else
            {
                return this.Content(RemotePotato.CancelRecording(id));
            }
        }

        // GET: /Guide/CancelSeries
        public ContentResult CancelSeries(
            string id)
        {
            if (Config.UseDvbViewer)
            {
                DvbViewer.Series.Delete(id);
            }
            return this.Content("");
        }

    }
}
