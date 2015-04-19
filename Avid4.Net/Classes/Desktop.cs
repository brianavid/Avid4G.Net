using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using NLog;

/// <summary>
/// Client access wrapper for the Avid.Desktop WCF service
/// </summary>
public static class DesktopClient
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    static HttpClient trayAppClient = new HttpClient();

    /// <summary>
    /// Initialize the WebAPI HTTP client, setting cache control to prevent caching
    /// </summary>
    public static void Initialize()
    {
        trayAppClient.BaseAddress = new Uri("http://localhost:89");
        trayAppClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue();
        trayAppClient.DefaultRequestHeaders.CacheControl.NoCache = true;
        trayAppClient.DefaultRequestHeaders.CacheControl.MaxAge = new TimeSpan(0);

        EnsureSpotifyRunning();
    }

    /// <summary>
    /// Launch the named application at the path defined in AvidConfig, 
    /// either with provided arguments or those defined in AvidConfig
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    static public bool LaunchProgram(string name, string args)
    {
        lock (trayAppClient)
        {
            try
            {
                logger.Info("LaunchProgram '{0}' '{1}'", name, args ?? "");
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/LaunchProgram?name={0}&args={1}",
                    name, HttpUtility.UrlEncode(args ?? ""))).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Launch a new instance of the named program with specified arguments
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    static public bool LaunchNewProgram(string name, string args)
    {
        lock (trayAppClient)
        {
            try
            {
                logger.Info("LaunchNewProgram '{0}' '{1}'", name, args ?? "");
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/LaunchNewProgram?name={0}&args={1}",
                    name, HttpUtility.UrlEncode(args ?? ""))).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Exit a named program we have launched if it is still running
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    static public bool ExitProgram(string name)
    {
        lock (trayAppClient)
        {
            try
            {
                logger.Info("ExitProgram '{0}'", name);
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/ExitProgram?name={0}",
                    name)).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Bring the named program to the foreground if it is running
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    static public bool ForegroundProgram(string name)
    {
        lock (trayAppClient)
        {
            try
            {
                logger.Info("ForegroundProgram '{0}'", name);
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/ForegroundProgram?name={0}",
                    name)).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Exits all running programs we have launched
    /// </summary>
    /// <returns></returns>
    static public bool ExitAllPrograms()
    {
        lock (trayAppClient)
        {
            try
            {
                logger.Info("ExitAllPrograms");
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/ExitAllPrograms")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Send an emulated keyboard sequence of key presses to the foreground application
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    static public bool SendKeys(string keys)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/SendKeys?keys={0}",
                    HttpUtility.UrlEncode(keys ?? ""))).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Send an IR code through the USB IIRT transmitter
    /// </summary>
    /// <param name="irCode"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    static public bool SendIR(string irCode, string description)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/SendIR?irCode={0}&description={1}",
                    irCode, HttpUtility.UrlEncode(description ?? ""))).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Move the mouse cursor on screen by a relative amount
    /// </summary>
    /// <param name="dx"></param>
    /// <param name="dy"></param>
    /// <returns></returns>
    static public bool MouseMoveRelative(int dx, int dy)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/MouseMoveRelative?dx={0}&dy={1}",
                    dx, dy)).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Send an emulated mouse click at the current cursor location
    /// </summary>
    /// <param name="rightButton">True if an emulated right mouse click; otherwise a left mouse click</param>
    /// <returns></returns>
    static public bool MouseClick(bool rightButton)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/MouseClick?rightButton={0}",
                    rightButton)).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Send special keys to the desktop
    /// </summary>
    /// <param name="keyName"></param>
    /// <returns></returns>
    static public bool SendSpecialkey(string keyName)
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/SendSpecialkey?keyName={0}",
                    keyName)).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Fetch CPU and GPU temperature and load statistics as XML
    /// </summary>
    /// <returns></returns>
    static public string FetchCoreTempInfoXml()
    {
        lock (trayAppClient)
        {
            try
            {
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/FetchCoreTempInfoXml")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<string>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Ensure that the RemotePotato service is running and has not died, starting the service if it is not running
    /// </summary>
    /// <param name="recycle">If true; unconditionally stops and restarts the service</param>
    /// <returns>True if the service is now running</returns>
    static public bool EnsureRemotePotatoRunning(
            bool recycle)
    {
        lock (trayAppClient)
        {
            try
            {
                logger.Info("EnsureRemotePotatoRunning");
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/EnsureRemotePotatoRunning?recycle={0}", recycle)).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Ensure that the Spotify Player is running and has not died
    /// </summary>
    /// <returns>True if the player is now running</returns>
    static public bool EnsureSpotifyRunning()
    {
        lock (trayAppClient)
        {
            try
            {
                logger.Info("EnsureSpotifyRunning");
                HttpResponseMessage resp = trayAppClient.GetAsync(string.Format("api/Desktop/EnsureSpotifyRunning")).Result;
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<bool>().Result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }
}