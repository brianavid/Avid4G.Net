using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;

using Avid.Desktop;

/// <summary>
/// Summary description for Desktop
/// </summary>
public static class DesktopClient
{
    static IDesktopService desktopClientChannel = null;
    static ChannelFactory<IDesktopService> serviceFactory = null;

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
}