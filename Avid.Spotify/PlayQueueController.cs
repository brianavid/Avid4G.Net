using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;
using SpotiFire;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using NLog;

namespace Avid.Spotify
{
    public class PlayQueueController : ApiController
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        public SpotifyData.Track PlayTrack(
            int id,
            bool append = false)
        {
            Track track = Cache.Get(id) as Track;
            if (track != null)
            {
                try
                {
                    SpotifySession.EnqueueTrack(track, append);

                }
                catch (Exception ex)
                {
                    logger.Warn(ex);
                    throw new HttpResponseException(HttpStatusCode.InternalServerError);
                }
            }
            return MakeData.Track(track);
        }

        async Task<SpotifyData.Album> PlayAlbumAsync(
            Album album,
            bool append = false)
        {
            SpotifySession.EnqueueTracks((await (await album).Browse()).Tracks, append);
            return MakeData.Album(album);
        }

        [HttpGet]
        public SpotifyData.Album PlayAlbum(
            int id,
            bool append = false)
        {
            Album album = Cache.Get(id) as Album;
            if (album == null)
            {
                return null;
            }
            try
            {
                return PlayAlbumAsync(album, append).Result;

            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                return null;
            }
        }


        [HttpGet]
        public SpotifyData.Track GetCurrentTrack()
        {
            try
            {
                return MakeData.Track(SpotifySession.GetCurrentTrack());
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public IEnumerable<SpotifyData.Track> GetQueuedTracks()
        {
            try
            {
                var tracks = SpotifySession.GetQueuedTracks();
                return tracks == null ? new SpotifyData.Track[0] : tracks.Select(t => MakeData.Track(t));
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public SpotifyData.Track SkipToQueuedTrack(
            int id)
        {
            try
            {
                Track track = Cache.Get(id) as Track;
                if (track != null)
                {
                    SpotifySession.SkipToQueuedTrack(track);
                }
                return MakeData.Track(SpotifySession.GetCurrentTrack());
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }


        [HttpGet]
        public SpotifyData.Track RemoveQueuedTrack(
            int id)
        {
            try
            {
                Track track = Cache.Get(id) as Track;
                if (track != null)
                {
                    SpotifySession.RemoveQueuedTrack(track);
                }
                return MakeData.Track(track);
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

    }
}
