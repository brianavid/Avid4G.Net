using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;
using System.Web.Http.SelfHost;
using System.Windows.Forms;
using NLog;
using Microsoft.Win32;
using System.IO;
using SpotifyAPI.SpotifyWebAPI;
using SpotifyAPI.SpotifyWebAPI.Models;
using System.Net.Cache;

namespace Avid.Spotify
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            Logger logger = LogManager.GetLogger("SpotifyPlayer");
            logger.Info("Spotify Player Started");
            try
            {
                if (!File.Exists(SpotifySession.SpotifyAppKeyFileName))
                {
                    MessageBox.Show(
                        string.Format("Spotify requires a key file named '{0}'", SpotifySession.SpotifyAppKeyFileName),
                        "Spotify Player", MessageBoxButtons.OK);
                    logger.Fatal("Spotify Player has no AppKey");
                    return;
                }
                var config = new HttpSelfHostConfiguration("http://localhost:8383");

                config.Routes.MapHttpRoute(
                    "API Default", "api/{controller}/{action}/{id}", 
                    new { id = RouteParameter.Optional });

                RegistryKey key = Registry.CurrentUser.CreateSubKey("Avid");

                string user = key.GetValue("SpotifyUser") as string;
                string pass = key.GetValue("SpotifyPass") as string;

                while (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                {
                    GetSpotifyCredentials gsc = new GetSpotifyCredentials(user ?? "", pass ?? "");
                    if (gsc.ShowDialog() == DialogResult.Cancel)
                    {
                        return;
                    }

                    user = gsc.SpotifyUser;
                    pass = gsc.SpotifyPass;

                    key.SetValue("SpotifyUser", user);
                    key.SetValue("SpotifyPass", pass);

                    key.DeleteValue("SpotifyAccessToken");
                    key.DeleteValue("SpotifyTokenType");
                }

                SpotifySession.SpotifyUser = user;
                SpotifySession.SpotifyPass = pass;

                //  If we have no saved SpotifyAccessToken, get it via an HTTP handshake and
                //  save it persistently in the registry).
                //  This will use a browser to ask for credentials, but only the one time it is needed.
                if (string.IsNullOrEmpty(key.GetValue("SpotifyToken") as string))
                {
                    var auth = new AutorizationCodeAuth()
                    {
                        ClientId = "b2d4e764bb8c49f39f1211dfc6b71b34",
                        RedirectUri = "http://www.brianavid.co.uk/Avid4SpotifyAuth/Auth/Authenticate",

                        //How many permissions we need?
                        Scope = Scope.USER_READ_PRIVATE | Scope.USER_READ_EMAIL | Scope.PLAYLIST_READ_PRIVATE | Scope.USER_LIBRARAY_READ | Scope.USER_LIBRARY_MODIFY | Scope.USER_READ_PRIVATE
                            | Scope.USER_FOLLOW_MODIFY | Scope.USER_FOLLOW_READ | Scope.PLAYLIST_MODIFY_PUBLIC | Scope.PLAYLIST_MODIFY_PRIVATE | Scope.USER_READ_BIRTHDATE
                    };

                    //  Open up a browser to authenticate
                    auth.DoAuth();  

                    //  Try for two minutes to get the RefreshToken constructed as part of the OAUTH exchange
                    for (int i = 0; i < 120; i++)
                    {
                        HttpWebRequest request =
                            (HttpWebRequest)HttpWebRequest.Create("http://www.brianavid.co.uk/Avid4SpotifyAuth/Auth/GetLastRefreshToken");
                        request.Method = WebRequestMethods.Http.Get;
                        request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        var lastRefreshToken = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        if (!string.IsNullOrEmpty(lastRefreshToken))
                        {
                            key.SetValue("SpotifyToken", lastRefreshToken);
                            break;
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                if (string.IsNullOrEmpty(key.GetValue("SpotifyToken") as string))
                {
                    MessageBox.Show(
                        string.Format("Failed to authenticate to Spotify"),
                        "Spotify Player", MessageBoxButtons.OK);
                    logger.Fatal("Failed to authenticate to Spotify");
                    return;
                }


                using (HttpSelfHostServer server = new HttpSelfHostServer(config))
                {
                    server.OpenAsync().Wait();
                    var applicationContext = new CustomApplicationContext();
                    Application.Run(applicationContext);
                    logger.Info("Spotify Player Exit");
                    SpotifySession.CloseSession(false);
                }            
            }
            catch (Exception ex)
            {
                logger.Fatal(ex);
            }
            //  SingleInstance.Stop();
        }
    }
}
