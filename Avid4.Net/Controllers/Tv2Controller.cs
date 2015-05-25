using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    [NoCache]
    public class Tv2Controller : Controller
    {
        // GET: /Tv2/Watch
        public ActionResult Watch()
        {
            if (DvbViewer.CurrentlySelectedChannel == null)
            {
                return (View("Channels"));
            }
            return View();
        }

        // GET: /Tv2/ControlPane
        public ActionResult ControlPane()
        {
            return PartialView();
        }

        // GET: /Tv2/Channels
        public ActionResult Channels()
        {
            return View();
        }

        // GET: /Tv2/ChannelsPane
        public ActionResult ChannelsPane()
        {
            return PartialView();
        }

        // GET: /Tv2/Radio
        public ActionResult Radio()
        {
            return View();
        }

        // GET: /Tv2/RadioPane
        public ActionResult RadioPane()
        {
            return PartialView();
        }

        // GET: /Tv2/NowAndNext
        public ActionResult NowAndNext(
            string channelName)
        {
            ViewBag.ChannelName = channelName;
            return PartialView();
        }

        // GET: /Tv2/ChangeChannel
        public ContentResult ChangeChannel(
            string channelName)
        {
            DvbViewer.SelectChannel(DvbViewer.NamedChannel(channelName));
            return this.Content("");
        }

        // GET: /Tv2/Action
        public ContentResult Action(
            string command)
        {
            DvbViewer.SendCommand(command);
            return this.Content("");
        }

        // GET: /Tv2/Buttons
        public ActionResult Buttons()
        {
            return View();
        }

        // GET: /Tv2/All
        public ActionResult All()
        {
            return View();
        }


    }
}
