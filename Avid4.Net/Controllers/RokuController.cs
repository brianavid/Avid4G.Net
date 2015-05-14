﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    public class RokuController : Controller
    {
        // GET: /Roku/Controls
        public ActionResult Controls()
        {
            return View();
        }

        // GET: /Roku/Browser
        public ActionResult Browser()
        {
            switch (Running.RunningProgram)
            {
                case "Roku":
                case "Chromecast":
                case "LogFire":
                    ViewBag.Title = Running.RunningProgram;
                    break;
                default:
                    ViewBag.Title = "";
                    break;
            }
            if (Running.RunningProgram != "Roku")
            {
                ViewBag.HideRokuClass = "startHidden";
            }
            return View();
        }

        // GET: /Roku/All
        public ActionResult All()
        {
            switch (Running.RunningProgram)
            {
                case "Roku":
                case "Chromecast":
                case "LogFire":
                    ViewBag.Title = Running.RunningProgram;
                    break;
                default:
                    ViewBag.Title = "";
                    break;
            }
            if (Running.RunningProgram != "Roku")
            {
                ViewBag.HideRokuClass = "startHidden";
            }
            return View();
        }

        public ContentResult Launch(
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

    }
}
