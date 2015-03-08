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
using System.Threading;
using System.Security.Principal;
using System.Security.AccessControl;

namespace Avid.Spotify
{
    static class Program
    {
        static Logger logger = LogManager.GetLogger("SpotifyPlayer");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            logger.Info("Spotify Player Started");

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

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
                }

                SpotifySession.SpotifyUser = user;
                SpotifySession.SpotifyPass = pass;

                //  If we have no saved SpotifyRefreshUrl, get it via an HTTP handshake and
                //  save it persistently in the registry).
                //  This will use a browser to ask for credentials, but only the one time it is needed.
                RegistryKey webKey = Registry.LocalMachine.OpenSubKey("Software",true).CreateSubKey("Avid");
                const string SpotifyRefreshUrlRegistryValue = "SpotifyRefreshUrl";

                try
                {
	                SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
	                NTAccount account = sid.Translate(typeof(NTAccount)) as NTAccount;
	
	                // Get ACL from Windows
	                RegistrySecurity rs = webKey.GetAccessControl();
	
	                // Creating registry access rule for 'Everyone' NT account
	                RegistryAccessRule rar = new RegistryAccessRule(
	                    account.ToString(),
	                    RegistryRights.FullControl,
	                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
	                    PropagationFlags.None,
	                    AccessControlType.Allow);
	
	                rs.AddAccessRule(rar);
	                webKey.SetAccessControl(rs);
                }
                catch (System.Exception ex)
                {
                    logger.Error("Unable to grant write access to the Avid registry subkey");
                }

                if (string.IsNullOrEmpty(webKey.GetValue(SpotifyRefreshUrlRegistryValue) as string))
                {
                    logger.Info("Must authenticate to Spotify Web API");
                    const String RedirectUri = "http://www.brianavid.co.uk/Avid4SpotifyAuth/Auth/";
                    var auth = new AutorizationCodeAuth()
                    {
                        ClientId = "b2d4e764bb8c49f39f1211dfc6b71b34",
                        RedirectUri = RedirectUri + "Authenticate",

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
                            (HttpWebRequest)HttpWebRequest.Create(RedirectUri + "GetLastRefreshToken");
                        request.Method = WebRequestMethods.Http.Get;
                        request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        var lastRefreshToken = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        if (!string.IsNullOrEmpty(lastRefreshToken))
                        {
                            //  Save the required authentication refresh URL into the registry so that the main
                            //  Avid4 web app can authenticate using the same credentials
                            webKey.SetValue(SpotifyRefreshUrlRegistryValue, RedirectUri + "Refresh?refresh_token=" + lastRefreshToken);
                            logger.Info("Authenticated to Spotify Web API");
                            break;
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                if (string.IsNullOrEmpty(webKey.GetValue(SpotifyRefreshUrlRegistryValue) as string))
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
            catch
            {
                logger.Fatal("Non-.Net Exception");
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            logger.Fatal("Unhandled Thread Exception: {0}", e.Exception);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Fatal("Unhandled UI Exception: {0}", e.ExceptionObject);
        }
    }
}
