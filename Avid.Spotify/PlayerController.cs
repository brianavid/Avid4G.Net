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
    public class PlayerController : ApiController
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

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
