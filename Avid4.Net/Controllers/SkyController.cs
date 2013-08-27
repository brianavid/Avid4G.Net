using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    [NoCache]
    public class SkyController : Controller
    {
        //
        // GET: /Sky/Watch

        public ActionResult Watch()
        {
            return View();
        }

        //
        // GET: /Sky/ControlPane

        public ActionResult ControlPane()
        {
            return PartialView();
        }

        //
        // GET: /Sky/Live

        public ActionResult Live()
        {
            SkyData.Sky.LoadChannelMappings();
            return View();
        }

        //
        // GET: /Sky/ChannelsPane

        public ActionResult ChannelsPane()
        {
            SkyData.Sky.LoadChannelMappings();
            return PartialView();
        }

        // GET: /Sky/Radio

        public ActionResult Radio()
        {
            return View();
        }

        //
        // GET: /Sky/RadioPane

        public ActionResult RadioPane()
        {
            SkyData.Sky.LoadChannelMappings();
            return PartialView();
        }

        //
        //
        // GET: /Sky/NowAndNext

        public ActionResult NowAndNext(
            string id)
        {
            if (id != null && SkyData.Sky.AllChannels.ContainsKey(int.Parse(id)))
            {
                ViewBag.Id = id;
                return PartialView();
            }
            return null;
        }

        // GET: /Sky/Recordings

        public ActionResult Recordings(
            string refresh)
        {
            if (!String.IsNullOrEmpty(refresh))
            {
                SkyData.Sky.LoadAllRecordings();
            }
            return View();
        }

        // GET: /Sky/RecordingsPane

        public ActionResult RecordingsPane(
            string title,
            string refresh)
        {
            if (title != null)
            {
                ViewBag.GroupTitle = title;
            }
            else if (!String.IsNullOrEmpty(refresh))
            {
                SkyData.Sky.LoadAllRecordings();
            }
            return PartialView();
        }

        // GET: /Sky/Recordings

        public ActionResult Recording(
            string id)
        {
            ViewBag.Id = id;
            return View();
        }

        //
        // GET: /Sky/Recordings

        public ContentResult RecordingDescription(
            string id)
        {
            if (id != null && SkyData.Sky.AllRecordings.ContainsKey(id))
            {
                return this.Content(SkyData.Sky.AllRecordings[id].Description);
            }
            return this.Content("");
        }

        //
        // GET: /Sky/Buttons

        public ActionResult Buttons()
        {
            return View();
        }

        //
        // GET: /Sky/All

        public ActionResult All()
        {
            return View();
        }

        //
        // GET: /Sky/ChangeChannel

        public ContentResult ChangeChannel(
            string id)
        {
            int channelNumber = int.Parse(id);
            SkyData.Sky.ChangeChannel(channelNumber);
            Receiver.SetMute(false);

            return this.Content("");
        }

        //
        // GET: /Sky/PlayRecording

        public ContentResult PlayRecording(
            string id,
            string start)
        {
            if (SkyData.Sky.AllRecordings.ContainsKey(id))
            {
                SkyData.Recording recording = SkyData.Sky.AllRecordings[id];
                SkyData.Sky.PlayRecording(recording, int.Parse(start));
                Receiver.SetMute(false);
            }
            return this.Content("");
        }

        // GET: /Sky/DeleteRecording

        public ContentResult DeleteRecording(
            string id)
        {
            if (SkyData.Sky.AllRecordings.ContainsKey(id))
            {
                SkyData.Recording recording = SkyData.Sky.AllRecordings[id];
                SkyData.Sky.DeleteRecording(recording);
            }
            return this.Content("");
        }

        //
        // GET: /Sky/play?speed=NNN

        public ContentResult Play(
            string speed)
        {
            DateTime start = DateTime.UtcNow;
            int speedValue = int.Parse(speed);
            string result = SkyData.Sky.PlayAtSpeed(speedValue);
            DateTime finish = DateTime.UtcNow;
            return this.Content(String.Format("Play {2} {0}..{1} == {3}", start.ToString("u"), finish.ToString("u"), speed, result));
        }

        //
        // GET: /Sky/Pause

        public ContentResult Pause()
        {
            DateTime start = DateTime.UtcNow;
            string result = SkyData.Sky.Pause();
            DateTime finish = DateTime.UtcNow;
            return this.Content(String.Format("Pause {0}..{1} == {2}", start.ToString("u"), finish.ToString("u"), result));
        }

        //
        // GET: /Sky/Stop

        public ContentResult Stop()
        {
            DateTime start = DateTime.UtcNow;
            Receiver.SetMute(true);
            SkyData.Sky.Stop();
            DateTime finish = DateTime.UtcNow;
            return this.Content(String.Format("Stop {0}..{1}", start.ToString("u"), finish.ToString("u")));
        }

    }
}
