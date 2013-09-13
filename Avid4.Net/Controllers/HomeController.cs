using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            ViewBag.IsHome = true;
            return View("Home");
        }

        //
        // GET: /Home/Home

        public ActionResult Home()
        {
            ViewBag.IsHome = true;
            return View();
        }

        //
        // GET: /Home/Wide

        public ActionResult Wide()
        {
            ViewBag.IsHome = true;
            return View();
        }

        //
        // GET: /Home/MouseEtc

        public ActionResult MouseEtc()
        {
            ViewBag.LinkBack = true;
            return View();
        }

        //
        // GET: /Home/MouseEtcWide

        public ActionResult MouseEtcWide()
        {
            ViewBag.LinkBack = true;
            return View();
        }

    }
}
