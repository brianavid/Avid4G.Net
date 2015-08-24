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
        // GET: /Video/Watch
        public ActionResult Watch()
        {
            return View();
        }

        // GET: /Video/WatchPane
        public ActionResult WatchPane()
        {
            return PartialView();
        }

        // GET: /Video/All
        public ActionResult All()
        {
            return View();
        }

        // GET: /Video/Recordings
        public ActionResult Recordings()
        {
            return View();
        }

        // GET: /Video/Recording
        public ActionResult Recording(
            string id)
        {
            ViewBag.Id = id;
            return View();
        }

        // GET: /Video/Videos
        public ActionResult Videos()
        {
            return View();
        }

        // GET: /Video/DVDs
        public ActionResult DVDs()
        {
            return View();
        }

        // GET: /Video/RecordingsPane
        public ActionResult RecordingsPane(
            string title)
        {
            if (title != null)
            {
                ViewBag.GroupTitle = title;
            }
            return PartialView();
        }

        // GET: /Video/VideosPane
        public ActionResult VideosPane()
        {
            return PartialView();
        }

        // GET: /Video/DVDsPane
        public ActionResult DVDsPane()
        {
            return PartialView();
        }

        // GET: /Video/PlayRecording
        public ContentResult PlayRecording(
            string id)
        {
            if (DvbViewer.AllRecordings.ContainsKey(id))
            {
                var recording = DvbViewer.AllRecordings[id];
                Running.LaunchProgram("Video", "/Media /F /ExFunc:exSetVolume,100 /ExFunc:exSetMode,0 /Play \"" + recording.Filename + "\"");
                Zoom.IsDvdMode = false;
                Zoom.Title = recording.Title;
            }
            return this.Content("");
        }

        // GET: /Video/DeleteRecording
        public ContentResult DeleteRecording(
            string id)
        {
            if (DvbViewer.AllRecordings.ContainsKey(id))
            {
                var recording = DvbViewer.AllRecordings[id];
                DvbViewer.DeleteRecording(recording);
            }
            return this.Content("");
        }

        // GET: /Video/PlayDvdDisk
        public ContentResult PlayDvdDisk(
            string drive,
            string title)
        {
            Running.LaunchProgram("Video", "/DVD /F /ExFunc:exSetVolume,100 /ExFunc:exSetMode,1 /Opendrive:" + drive);
            Zoom.IsDvdMode = true;
            if (!string.IsNullOrEmpty(title))
            {
                Zoom.Title = title;
            }
            return this.Content("");
        }

        // GET: /Video/PlayDvdDirectory
        public ContentResult PlayDvdDirectory(
            string path,
            string title)
        {
            Running.LaunchProgram("Video", "/DVD /F /ExFunc:exSetVolume,100 /ExFunc:exSetMode,1 /Play \"" + path + "\\VIDEO_TS\\VIDEO_TS.IFO\"");
            Zoom.IsDvdMode = true;
            if (!string.IsNullOrEmpty(title))
            {
                Zoom.Title = title;
            }
            return this.Content("");
        }

        // GET: /Video/PlayVideoFile
        public ContentResult PlayVideoFile(
            string path,
            string title)
        {
            Running.LaunchProgram("Video", "/Media /F /ExFunc:exSetVolume,100 /ExFunc:exSetMode,0 /Play \"" + path + "\"");
            if (!string.IsNullOrEmpty(title))
            {
                Zoom.Title = title;
            }
            return this.Content("");
        }

        // GET: /Video/GetPlayingInfo
        public ContentResult GetPlayingInfo()
        {
            StringWriter writer = new StringWriter();
            Zoom.GetInfo().Save(writer);
            return this.Content(writer.ToString(), @"text/xml", writer.Encoding);
        }

        // GET: /Video/SendZoom
        public ContentResult SendZoom(
            string cmd)
        {
            Uri requestUri = new Uri(Zoom.FuncUrl + cmd);

            for (int i = 1; i < 11; i++)
            {
                try
                {
                    HttpWebRequest request =
                        (HttpWebRequest)HttpWebRequest.Create(requestUri);
                    request.Method = WebRequestMethods.Http.Get;

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    return this.Content("");
                }
                catch (Exception)
                {
                    System.Threading.Thread.Sleep(200 * i);
                }
            }

            return this.Content("");
        }
    }
}
