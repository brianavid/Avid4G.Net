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
using System.Web.Http.Filters;
using NLog;

namespace Avid.Spotify
{
    /// <summary>
    /// Web API Controller, with public HttpGet web methods for managing the queu of playing tracks
    /// </summary>
    public class PlayQueueController : ApiController
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        class LoggingExceptionFilterAttribute : ExceptionFilterAttribute
        {
            public override void OnException(HttpActionExecutedContext context)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(context.Exception.Message),
                    ReasonPhrase = "ValidationException"
                };
                logger.Error(context.Exception);
                throw new HttpResponseException(resp);
            }
        }

        /// <summary>
        /// Play the identified track, either immediately or after the currently queued tracks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        [HttpGet]
        [LoggingExceptionFilter]
        public Boolean PlayTrack(
            string id,
            bool append = false)
        {
            Track track = SpotifySession.GetTrack(id);
            if (track != null && track.IsAvailable)
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
            return true;
        }

        async Task<Boolean> PlayAlbumAsync(
            Album album,
            bool append = false)
        {
            try
            {
                SpotifySession.EnqueueTracks((await album.Browse()).Tracks.Where(t => t.IsAvailable), append);
                return true;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                return false;
            }
        }


        /// <summary>
        /// Play all tracks of the identified album, either immediately or after the currently queued tracks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        [HttpGet]
        [LoggingExceptionFilter]
        public Boolean PlayAlbum(
            string id,
            bool append = false)
        {
            try
            {
                Album album = SpotifySession.GetAlbum(id);
                if (album == null)
                {
                    return false;
                }
                return PlayAlbumAsync(album, append).Result;

            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                return false;
            }
        }

        /// <summary>
        /// Get the currently playing track
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [LoggingExceptionFilter]
        public String GetCurrentTrack()
        {
            try
            {
                var track = SpotifySession.GetCurrentTrack();
                return track == null ? null : track.GetLink().ToString();
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Get the collection of all queued tracks
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [LoggingExceptionFilter]
        public IEnumerable<String> GetQueuedTracks()
        {
            try
            {
                var tracks = SpotifySession.GetQueuedTracks();
                return tracks == null ? new String[0] : tracks.Select(t => t.GetLink().ToString());
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }


        /// <summary>
        /// Skip to a specified queued track
        /// </summary>
        [HttpGet]
        [LoggingExceptionFilter]
        public String SkipToQueuedTrack(
            string id)
        {
            try
            {
                Track track = SpotifySession.GetTrack(id);
                if (track != null)
                {
                    SpotifySession.SkipToQueuedTrack(track);
                    return track.GetLink().ToString();
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }


        /// <summary>
        /// Remove the specified queued track from the queue
        /// </summary>
        [HttpGet]
        [LoggingExceptionFilter]
        public String RemoveQueuedTrack(
            string id)
        {
            try
            {
                Track track = SpotifySession.GetTrack(id);
                if (track != null)
                {
                    SpotifySession.RemoveQueuedTrack(track);
                    return track.GetLink().ToString();
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Skip playing forwards to the next queued track
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [LoggingExceptionFilter]
        public int Skip()
        {
            try
            {
                SpotifySession.Skip();
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
            return 0;
        }

        /// <summary>
        /// Skip playing backwards to the previous queued track
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [LoggingExceptionFilter]
        public int Back()
        {
            try
            {
                SpotifySession.Back();
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
            return 0;
        }

    }
}
