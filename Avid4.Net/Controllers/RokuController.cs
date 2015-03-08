using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    public class RokuController : Controller
    {
        // GET: /Roku/Show
        public ActionResult Show()
        {
            ViewBag.LinkBack = true;
            return View();
        }

        // GET: /Roku/ShowWide
        public ActionResult ShowWide()
        {
            ViewBag.LinkBack = true;
            return View();
        }

    }
}
