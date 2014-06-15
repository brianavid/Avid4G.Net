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
                }

                key.SetValue("SpotifyUser", user);
                key.SetValue("SpotifyPass", pass);

                SpotifySession.SpotifyUser = user;
                SpotifySession.SpotifyPass = pass;

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
