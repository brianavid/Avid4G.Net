using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using Avid.Spotify;
using System.Text;

namespace Avid4.Net.Controllers
{
    public class SpotifyController : Controller
    {
        static bool isPaused = false;

        //
        // GET: /Spotify/Mouse

        public ActionResult Mouse()
        {
            return View();
        }

        //
        // GET: /Spotify/WideMouse

        public ActionResult WideMouse()
        {
            return View();
        }

        public ActionResult All()
        {
            ViewBag.Mode = "Library";

            return View();
        }

        public ActionResult Playing()
        {
            return View();
        }

        public ActionResult Queue()
        {
            return View();
        }

        public ActionResult QueuePane()
        {
            return View();
        }

        public ActionResult Browser(
            string mode,
            string id,
            string name,
            string query)
        {
            ViewBag.Mode = mode;
            if (id != null)
            {
                ViewBag.Id = id;
            }
            if (name != null)
            {
                ViewBag.Name = name;
            }
            if (query != null)
            {
                ViewBag.Query = query;
            }

            return View();
        }

        public ActionResult BrowserPane(
            string mode,
            string id,
            string name,
            string query,
            string trackInfoId,
            string albumInfoId,
            string artistInfoId,
            string append)
        {
            ViewBag.Mode = mode;
            if (id != null)
            {
                ViewBag.Id = id;
            }
            if (name != null)
            {
                ViewBag.Name = name;
            }
            if (query != null)
            {
                ViewBag.Query = query;
            }
            if (trackInfoId != null)
            {
                ViewBag.TrackId = trackInfoId;
            }
            if (albumInfoId != null)
            {
                ViewBag.AlbumId = albumInfoId;
            }
            if (artistInfoId != null)
            {
                ViewBag.ArtistId = artistInfoId;
            }
            if (append != null)
            {
                ViewBag.Append = append;
            }

            return View();
        }

        public ContentResult GetPlayingInfo()
        {
            SpotifyData.Track currentTrack = Spotify.GetCurrentTrack();
            IEnumerable<SpotifyData.Track> queuedTracks = Spotify.GetQueuedTracks();
            if (currentTrack == null)
            {
                XElement stoppedInfo = new XElement("Track",
                    new XAttribute("id", ""),
                    new XAttribute("name", ""),
                    new XAttribute("album", ""),
                    new XAttribute("albumid", ""),
                    new XAttribute("albumArtist", ""),
                    new XAttribute("trackArtists", ""),
                    new XAttribute("duration", 0),
                    new XAttribute("position", 0),
                    new XAttribute("status", "Stopped"),
                    new XAttribute("postionDisplay", ""),
                    new XAttribute("indexDisplay", ""));

                return this.Content(stoppedInfo.ToString(), @"text/xml", Encoding.UTF8);
            }

            int pos = Spotify.GetPosition();
            int trackCount = 0;
            int trackIndex = 0;
            foreach (SpotifyData.Track track in queuedTracks)
            {
                trackCount++;
                if (track.Id == currentTrack.Id)
                {
                    trackIndex = trackCount;
                }
            }

            int playStatus = Spotify.GetPlaying();

            XElement info = new XElement("Track",
                new XAttribute("id", currentTrack.Id),
                new XAttribute("name", currentTrack.Name),
                new XAttribute("album", currentTrack.AlbumName),
                new XAttribute("albumid", currentTrack.AlbumId),
                new XAttribute("albumArtist", currentTrack.AlbumArtistName),
                new XAttribute("trackArtists", currentTrack.TrackArtistNames),
                new XAttribute("duration", currentTrack.Duration),
                new XAttribute("position", pos),
                new XAttribute("status", playStatus == -1 ? "Stolen" : playStatus == 0 ? "Paused" : "Playing"),
                new XAttribute("postionDisplay", Spotify.FormatDuration(pos) + "/" + Spotify.FormatDuration(currentTrack.Duration)),
                new XAttribute("indexDisplay", trackIndex + "/" + trackCount));

            return this.Content(info.ToString(), @"text/xml", Encoding.UTF8);
        }

        public ContentResult PlayAlbum(
            int id,
            bool append = false)
        {
            Spotify.PlayAlbum(id, append);
            isPaused = false;
            return this.Content("");
        }

        public ContentResult PlayTrack(
            int id,
            bool append = false)
        {
            Spotify.PlayTrack(id, append);
            isPaused = false;
            return this.Content("");
        }

        public ContentResult SkipToQueuedTrack(
            int id)
        {
            Spotify.SkipToQueuedTrack(id);
            isPaused = false;
            return this.Content("");
        }

        public ContentResult RemoveQueuedTrack(
            int id)
        {
            Spotify.RemoveQueuedTrack(id);
            return this.Content("");
        }

        public ContentResult PlayPause()
        {
            if (isPaused)
            {
                Spotify.Play();
                isPaused = false;
            }
            else
            {
                Spotify.Pause();
                isPaused = true;
            }
            return this.Content("");
        }

        public ContentResult Skip()
        {
            Spotify.Skip();
            return this.Content("");
        }

        public ContentResult Back()
        {
            Spotify.Back();
            return this.Content("");
        }

        public ContentResult Plus10()
        {
            Spotify.SetPosition(Spotify.GetPosition() + 10);
            return this.Content("");
        }

        public ContentResult Minus10()
        {
            int pos = Spotify.GetPosition();
            Spotify.SetPosition(pos < 10 ? 0 : pos - 10);
            return this.Content("");
        }

        public ContentResult SetPosition(
            int pos)
        {
            Spotify.SetPosition(pos);
            return this.Content("");
        }

        public ActionResult GetAlbumImage(
            int id)
        {
            return File(Spotify.GetAlbumImage(id), "image/png");
        }

        public ContentResult AddTrackToPlaylist(
            int id,
            string name)
        {
            Spotify.AddTrackToPlayList(name, id);
            return this.Content("");
        }

        public ContentResult AddAlbumToPlayList(
            int id,
            string name)
        {
            Spotify.AddAlbumToPlayList(name, id);
            return this.Content("");
        }


        public ContentResult RemoveTrackFromPlayList(
            int id,
            string name)
        {
            Spotify.RemoveTrackFromPlayList(name, id);
            return this.Content("");
        }

        public ContentResult RemoveAlbumFromPlayList(
            int id,
            string name)
        {
            Spotify.RemoveAlbumFromPlayList(name, id);
            return this.Content("");
        }
    }
}
