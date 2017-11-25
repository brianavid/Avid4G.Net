using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    public class SecurityController : Controller
    {
        static DateTime NoTickBefore = DateTime.MinValue;

        // GET: Security/Tick (occurs every minute from the Desktop tray app service)
        public ActionResult Tick(
            string when = null)     //  For testing can explictly override the timestamp
        {
            if (when == null)
            {
                //  Tick with the current time unless there has been a (testing) Tick with an explicit time in the last 10 minutes
                if (DateTime.Now >= NoTickBefore)
                {
                    Security.Tick(DateTime.Now);
                }
            }
            else
            {
                var whenDate = DateTime.ParseExact(when, "HHmm", CultureInfo.InvariantCulture);
                Security.Tick(whenDate);

                //  Inhibit normal backgound ticks for the next 10 minutes
                NoTickBefore = DateTime.Now.AddMinutes(10);
            }
            return Content("OK");
        }

#if TestDirectDeviceOnOff
        // GET: Security/On?id=NNN
        public ActionResult On(
            string id,
            string socket = "no")
        {
            string ipAddr = "192.168.1." + id;
            TP_Link.TurnOn(ipAddr, ipAddr, socket != "no");
            return Content("OK");
        }

        // GET: Security/Off?id=NNN
        public ActionResult Off(
            string id,
            string socket = "no")
        {
            string ipAddr = "192.168.1." + id;
            TP_Link.TurnOff(ipAddr, ipAddr, socket != "no");
            return Content("OK");
        }
#endif

        // GET: Security/GetProfiles
        public ActionResult GetProfiles()
        {
            return View();
        }

        // GET: Security/LoadProfile?id=NNN
        public ActionResult LoadProfile(
            string id)
        {
            Security.LoadProfile(Int32.Parse(id));
            return Content("OK");
        }

        // GET: Security/GetSchedule
        public ActionResult GetSchedule()
        {
            return View();
        }
    }
}