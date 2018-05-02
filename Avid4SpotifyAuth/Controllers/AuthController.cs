using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using NLog;

namespace Avid4SpotifyAuth.Controllers
{
    public class Token
    {
        [JsonProperty("access_token")]
        public String AccessToken { get; set; }

        [JsonProperty("token_type")]
        public String TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public String RefreshToken { get; set; }

        [JsonProperty("error")]
        public String Error { get; set; }
        [JsonProperty("error_description")]
        public String ErrorDescription { get; set; }

        public DateTime CreateDate { get; set; }
        public Token()
        {
            CreateDate = DateTime.Now;
        }
        public Boolean IsExpired()
        {
            return CreateDate.Add(TimeSpan.FromSeconds(ExpiresIn)) >= DateTime.Now;
        }
    }

    public class AuthController : Controller
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        //Your client Id
        private const string ClientId = "b2d4e764bb8c49f39f1211dfc6b71b34";
        private const string ClientSecret = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"; //  Replace with real secret before deploying
        private const string RedirectUri = "http://brianavid.dnsalias.com/SpotifyAuth/Auth/Authenticate";

        private static string lastRefreshToken = "";
        private static DateTime refreshTokenFetchExpiry = DateTime.MinValue;

        //
        // GET: /Auth/

        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /ShowLog/

        public ActionResult ShowLog()
        {
            return View();
        }

        //
        // GET: /Authenticate/

        ContentResult DoAuthentication(
            string code,
            string refresh_token)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Proxy = null;

                    NameValueCollection col = new NameValueCollection();
                    col.Add("grant_type", code == null ? "refresh_token" : "authorization_code");
                    if (code != null)
                    {
                        col.Add("code", code);
                    }
                    else
                    {
                        col.Add("refresh_token", refresh_token);
                    }
                    col.Add("redirect_uri", RedirectUri);
                    col.Add("client_id", ClientId);
                    col.Add("client_secret", ClientSecret);

                    String response = "";
                    try
                    {
                        byte[] data = wc.UploadValues("https://accounts.spotify.com/api/token", "POST", col);
                        response = Encoding.UTF8.GetString(data);
                    }
                    catch (WebException e)
                    {
                        response = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                    }
                    var token = JsonConvert.DeserializeObject<Token>(response);
                    lastRefreshToken = token.RefreshToken;
                    refreshTokenFetchExpiry = DateTime.UtcNow.AddSeconds(120);
                    logger.Info("Last Refresh Token {0}", lastRefreshToken);
                    logger.Info("Fetch Refresh Token before {0}", refreshTokenFetchExpiry.ToShortTimeString());
                    logger.Info("New Token {0}", token.AccessToken);
                    return this.Content(response, "application/json");
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return this.Content("");
            }
        }

        public ContentResult Probe()
        {
            logger.Info("Probe");
            return this.Content("OK");
        }

        public ContentResult Authenticate(
            string code,
            string error,
            string state)
        {
            logger.Info("Authenticate {0}", code);
            return DoAuthentication(code, null);
        }

        public ContentResult GetLastRefreshToken()
        {
            if (DateTime.UtcNow > refreshTokenFetchExpiry)
            {
                lastRefreshToken = "";
            }
            logger.Info("GetLastRefreshToken {0}", lastRefreshToken);
            return this.Content(lastRefreshToken);
        }

        public ContentResult Refresh(
            string refresh_token)
        {
            logger.Info("Refresh {0}", refresh_token);
            return DoAuthentication(null, refresh_token);
        }

    }
}
