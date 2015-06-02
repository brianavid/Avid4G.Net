using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace Avid4.Net.Controllers
{
    [NoCache]
    public class TvController : Controller
    {
        // GET: /Tv/Watch
        public ActionResult Watch()
        {
            if (DvbViewer.CurrentlySelectedChannel == null)
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
            DvbViewer.SelectChannel(DvbViewer.NamedChannel(channelName));
            return this.Content("");
        }

        // GET: /Tv/ChangeChannelNumer
        public ContentResult ChangeChannelNumber(
            int channelNumber)
        {
            DvbViewer.SelectChannel(DvbViewer.NumberedChannel(channelNumber));
            return this.Content("");
        }

        // GET: /Tv/Action
        public ContentResult Action(
            string command)
        {
            DvbViewer.SendCommand(command);
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

        // GET: /Tv/All
        public ContentResult RecordNow()
        {
            var now = DvbViewer.GetNowAndNext(DvbViewer.CurrentlySelectedChannel).FirstOrDefault();
            if (now != null)
            {
                DvbViewer.AddTimer(now);
            }
            return this.Content("");
        }


        // POST: /Tv/UpdateStatus
        [HttpPost]
        public ContentResult UpdateStatus()
        {
            try
            {
	            var xStatus = XDocument.Load(Request.InputStream);
	
	            DvbViewer.LastStatus = xStatus;
	
	            foreach (var channel in xStatus.Root.Elements("Channel"))
	            {
	                var channelNumber = int.Parse(channel.Attribute(("number")).Value);
	                if (DvbViewer.CurrentlySelectedChannel == null || channelNumber != DvbViewer.CurrentlySelectedChannel.Number)
	                {
	                    DvbViewer.SelectChannel(DvbViewer.NumberedChannel(channelNumber));
	                }
	            }
            }
            catch (System.Exception)
            {
            }

            return this.Content("");
        }

    }
}
