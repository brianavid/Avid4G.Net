using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Avid4.Net.Controllers
{
    public class VideoController : Controller
    {
        //
        // GET: /Video/Watch

        public ActionResult Watch()
        {
            return View();
        }

        //
        // GET: /Video/WatchPane

        public ActionResult WatchPane()
        {
            return PartialView();
        }

        //
        // GET: /Video/All

        public ActionResult All()
        {
            return View();
        }

        //
        // GET: /Video/Recordings

        public ActionResult Recordings()
        {
            return View();
        }

        //
        // GET: /Video/Recordings

        public ActionResult Recording(
            string id)
        {
            ViewBag.Id = id;
            return View();
        }

        //
        // GET: /Video/DVDs

        public ActionResult DVDs()
        {
            return View();
        }

        //
        // GET: /Video/RecordingsPane

        public ActionResult RecordingsPane()
        {
            return PartialView();
        }

        //
        // GET: /Video/DVDsPane

        public ActionResult DVDsPane()
        {
            return PartialView();
        }

        // GET: /Sky/PlayRecording

        public ContentResult PlayRecording(
            string id)
        {
            if (RemotePotato.AllRecordings.ContainsKey(id))
            {
                var recording = RemotePotato.AllRecordings[id];
                Running.LaunchProgram("Video", "/Media /F /Play \"" + recording.Filename + "\"");
                Zoom.IsDvdMode = false;
                Zoom.Title = recording.Title;
            }
            return this.Content("");
        }

        // GET: /Sky/DeleteRecording

        public ContentResult DeleteRecording(
            string id)
        {
            if (RemotePotato.AllRecordings.ContainsKey(id))
            {
                var recording = RemotePotato.AllRecordings[id];
                RemotePotato.DeleteRecording(recording);
            }
            return this.Content("");
        }

        // GET: /Sky/PlayDvdDisk

        public ContentResult PlayDvdDisk(
            string drive,
            string title)
        {
            Running.LaunchProgram("Video", "/DVD /F /Opendrive:" + drive);
            Zoom.IsDvdMode = true;
            if (!string.IsNullOrEmpty(title))
            {
                Zoom.Title = title;
            }
            return this.Content("");
        }

        // GET: /Sky/PlayDvdDirectory

        public ContentResult PlayDvdDirectory(
            string path,
            string title)
        {
            Running.LaunchProgram("Video", "/DVD /F /Play \"" + path + "\\VIDEO_TS\\VIDEO_TS.IFO\"");
            Zoom.IsDvdMode = true;
            if (!string.IsNullOrEmpty(title))
            {
                Zoom.Title = title;
            }
            return this.Content("");
        }

        // GET: /Sky/PlayBluRayFile

        public ContentResult PlayBluRayFile(
            string path,
            string title)
        {
            Running.LaunchProgram("Video", "/DVD /F /Play \"" + path + "\"");
            Zoom.IsDvdMode = true;
            if (!string.IsNullOrEmpty(title))
            {
                Zoom.Title = title;
            }
            return this.Content("");
        }

        public ContentResult GetPlayingInfo()
        {
            StringWriter writer = new StringWriter();
            Zoom.GetInfo().Save(writer);
            return this.Content(writer.ToString(), @"text/xml", writer.Encoding);
        }


        public ContentResult SendZoom(
            string cmd)
        {
            Uri requestUri = new Uri(Zoom.Url + cmd);

            for (int i = 0; i < 5; i++)
            {
                HttpWebRequest request =
                    (HttpWebRequest)HttpWebRequest.Create(requestUri);
                request.Method = WebRequestMethods.Http.Get;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                try
                {
                    return this.Content("");
                }
                catch (System.Exception ex)
                {
                    System.Threading.Thread.Sleep(2000);
                }
            }

            return this.Content("");
        }
    }
}
