using System;
using System.Net;
using Microsoft.Win32;
using System.Net.Cache;
using System.Security.Principal;
using System.Security.AccessControl;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using NLog;
using System.IO;

namespace Avid.Desktop
{
    internal class SpotifyAuth
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Authenticate the currenr user and store a persistent OAUTH Refresh Key in the registry.
        /// This will be used by the Avid4 web app to allow it to browse and play music as that user.
        /// The mechanism uses an HTTP handshake involving Spotify and my own server which has the required secret
        /// </summary>
        public static void Auth()
        {
            try
            {
#pragma warning disable CA1416 // Validate platform compatibility
                //  Where will the Refresh URL me stored in the registry
                RegistryKey webKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("Software").OpenSubKey("Avid", true);
                const string SpotifyRefreshUrlRegistryValue = "SpotifyRefreshUrl";

                try
                {
                    //  Attempt to let "everyone" access that key as the web app runs as a different user
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
#pragma warning restore CA1416 // Validate platform compatibility
                }
                catch (System.Exception ex)
                {
                    logger.Error($"Unable to grant write access to the Avid registry subkey: ${ex.Message}");
                }

                //  My own web server has an authenticator with the client secret for my developer client ID
                const string RedirectUri = "http://brianavid.dnsalias.com/SpotifyAuth/Auth/";
                const string ClientId = "b2d4e764bb8c49f39f1211dfc6b71b34";

                var auth = new LoginRequest(new Uri(RedirectUri + "Authenticate"), ClientId, SpotifyAPI.Web.LoginRequest.ResponseType.Code)
                {
                    //How many permissions we need? Ask for the lot!
                    Scope = new[] {
                        Scopes.UgcImageUpload,
                        Scopes.UserReadPlaybackState,
                        Scopes.UserModifyPlaybackState,
                        Scopes.UserReadCurrentlyPlaying,
                        Scopes.Streaming,
                        Scopes.AppRemoteControl,
                        Scopes.UserReadEmail,
                        Scopes.UserReadPrivate,
                        Scopes.PlaylistReadCollaborative,
                        Scopes.PlaylistModifyPublic,
                        Scopes.PlaylistReadPrivate,
                        Scopes.PlaylistModifyPrivate,
                        Scopes.UserLibraryModify,
                        Scopes.UserLibraryRead,
                        Scopes.UserTopRead,
                        Scopes.UserReadPlaybackPosition,
                        Scopes.UserReadRecentlyPlayed,
                        Scopes.UserFollowRead,
                        Scopes.UserFollowModify
                    }
                };

                //  Open up a browser to authenticate
                BrowserUtil.Open(auth.ToUri());

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
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}

