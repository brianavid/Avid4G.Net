using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;

using Avid.Desktop;

/// <summary>
/// Client access wrapper for the Avid.Desktop WCF service
/// </summary>
public static class DesktopClient
{
    static IDesktopService desktopClientChannel = null;
    static ChannelFactory<IDesktopService> serviceFactory = null;

    /// <summary>
    /// Instantiate a singleton instance of IDesktop by opening a WCF communication channel.
    /// </summary>
    static IDesktopService Desktop
    {
        get
        {
            if (serviceFactory == null)
            {
                serviceFactory = new ChannelFactory<IDesktopService>("WSHttpBinding_IDesktopService");
            }

            ICommunicationObject conn = desktopClientChannel as ICommunicationObject;
            if (conn != null && conn.State != CommunicationState.Opened)
            {
                conn.Abort();
                desktopClientChannel = null;
            }
            if (desktopClientChannel == null)
            {
                desktopClientChannel = serviceFactory.CreateChannel();
            }
            return desktopClientChannel;
        }
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
        try
        {
            return Desktop.LaunchProgram(name, args);
        }
        catch
        {
            return Desktop.LaunchProgram(name, args);
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
        try
        {
            return Desktop.LaunchNewProgram(name, args);
        }
        catch
        {
            return Desktop.LaunchNewProgram(name, args);
        }
    }

    /// <summary>
    /// Exit a named program we have launched if it is still running
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    static public bool ExitProgram(string name)
    {
        try
        {
            return Desktop.ExitProgram(name);
        }
        catch
        {
            return Desktop.ExitProgram(name);
        }
    }

    /// <summary>
    /// Bring the named program to the foreground if it is running
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    static public bool ForegroundProgram(string name)
    {
        try
        {
            return Desktop.ForegroundProgram(name);
        }
        catch
        {
            return Desktop.ForegroundProgram(name);
        }
    }

    /// <summary>
    /// Exits all running programs we have launched
    /// </summary>
    /// <returns></returns>
    static public bool ExitAllPrograms()
    {
        try
        {
	        return Desktop.ExitAllPrograms();
        }
        catch
        {
            return Desktop.ExitAllPrograms();
        }
    }

    /// <summary>
    /// Send an emulated keyboard sequence of key presses to the foreground application
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    static public bool SendKeys(string keys)
    {
        try
        {
            return Desktop.SendKeys(keys);
        }
        catch
        {
            return Desktop.SendKeys(keys);
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
        try
        {
            return Desktop.SendIR(irCode, description);
        }
        catch
        {
            return Desktop.SendIR(irCode, description);
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
        try
        {
            return Desktop.MouseMoveRelative(dx, dy);
        }
        catch
        {
            return Desktop.MouseMoveRelative(dx, dy);
        }
    }

    /// <summary>
    /// Send an emulated mouse click at the current cursor location
    /// </summary>
    /// <param name="rightButton">True if an emulated right mouse click; otherwise a left mouse click</param>
    /// <returns></returns>
    static public bool MouseClick(bool rightButton)
    {
        try
        {
            return Desktop.MouseClick(rightButton);
        }
        catch
        {
            return Desktop.MouseClick(rightButton);
        }
    }

    /// <summary>
    /// Send special keys to the desktop
    /// </summary>
    /// <param name="keyName"></param>
    /// <returns></returns>
    static public bool SendSpecialkey(string keyName)
    {
        try
        {
            return Desktop.SendSpecialkey(keyName);
        }
        catch
        {
            return Desktop.SendSpecialkey(keyName);
        }
    }

    /// <summary>
    /// Fetch CPU and GPU temperature and load statistics as XML
    /// </summary>
    /// <returns></returns>
    static public string FetchCoreTempInfoXml()
    {
        try
        {
            return Desktop.FetchCoreTempInfoXml();
        }
        catch
        {
            return Desktop.FetchCoreTempInfoXml();
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
        try
        {
            return Desktop.EnsureRemotePotatoRunning(recycle);
        }
        catch
        {
            return Desktop.EnsureRemotePotatoRunning(recycle);
        }
    }

    /// <summary>
    /// Ensure that the Spotify Player is running and has not died
    /// </summary>
    /// <returns>True if the player is now running</returns>
    static public bool EnsureSpotifyRunning()
    {
        try
        {
            return Desktop.EnsureSpotifyRunning();
        }
        catch
        {
            return Desktop.EnsureSpotifyRunning();
        }
    }
}