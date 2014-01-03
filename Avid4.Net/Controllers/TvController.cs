using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    [NoCache]
    public class TvController : Controller
    {
        // GET: /Tv/Watch
        public ActionResult Watch()
        {
            if (String.IsNullOrEmpty(RemotePotato.CurrentlySelectedChannelName))
            {
                return (View("Channels"));
            }
            return View();
        }

        // GET: /Tv/ControlPane
        public ActionResult ControlPane()
        {
            return PartialView();
        }

        // GET: /Tv/Channels
        public ActionResult Channels()
        {
            return View();
        }

        // GET: /Tv/ChannelsPane
        public ActionResult ChannelsPane()
        {
            return PartialView();
        }

        // GET: /Tv/Radio
        public ActionResult Radio()
        {
            return View();
        }

        // GET: /Tv/RadioPane
        public ActionResult RadioPane()
        {
            return PartialView();
        }

        // GET: /Tv/NowAndNext
        public ActionResult NowAndNext(
            string channelName)
        {
            ViewBag.ChannelName = channelName;
            return PartialView();
        }

        // GET: /Tv/ChangeChannel
        public ContentResult ChangeChannel(
            string channelName)
        {
            RemotePotato.SelectTvChannelName(channelName);
            return this.Content("");
        }

        // GET: /Tv/Action
        public ContentResult Action(
            string command)
        {
            RemotePotato.SendCommand(command);
            return this.Content("");
        }

        // GET: /Tv/Buttons
        public ActionResult Buttons()
        {
            return View();
        }

        // GET: /Tv/All
        public ActionResult All()
        {
            return View();
        }


    }
}
