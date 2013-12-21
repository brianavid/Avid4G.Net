using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;
using SpotiFire;
using System.IO;
using NLog;

namespace Avid.Spotify
{
    /// <summary>
    /// Web API Controller, with public HttpGet web methods for Controlling the NAudio player
    /// </summary>
    public class PlayerController : ApiController
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Start or continue playing the current track
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public int Play()
        {
            try
            {
	            SpotifySession.Play();
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
            return 0;
        }

        /// <summary>
        /// Pause playing the current track
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public int Pause()
        {
            try
            {
	            SpotifySession.Pause();
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
            return 0;
        }

        /// <summary>
        /// Stop playing the current track
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public int Stop()
        {
            try
            {
	            SpotifySession.Stop();
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
            return 0;
        }

        /// <summary>
        /// Skip playing forwards to the next queued track
        /// </summary>
        /// <returns></returns>
        [HttpGet]
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

        /// <summary>
        /// Is the player playing a track?
        /// </summary>
        /// <returns>+ve: Playing; 0: Paused; -ve: Stolen by another session</returns>
        [HttpGet]
        public int GetPlaying()
        {
            try
            {
	            return SpotifySession.GetPlaying();
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Get the position at which the current track is playing
        /// </summary>
        /// <returns>Position in seconds</returns>
        [HttpGet]
        public int GetPosition()
        {
            try
            {
	            return SpotifySession.GetPosition();
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Seek to a particular position within the currently playing track
        /// </summary>
        /// <param name="pos">Position in seconds</param>
        /// <returns></returns>
        [HttpGet]
        public int SetPosition(
            int pos)
        {
            try
            {
	            return SpotifySession.SetPosition(pos);
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }
    }
    
}
