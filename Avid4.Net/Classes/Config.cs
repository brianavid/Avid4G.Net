﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;

/// <summary>
/// Summary description for Config
/// </summary>
public static class Config
{
    static XDocument doc = null;

    static XDocument Doc
    {
        get
        {
            if (doc == null)
            {
                doc = XDocument.Load(@"C:\Avid.Net\AvidConfig.xml");
            }
            return doc;
        }
    }

    public static string IpAddress
    {
        get
        {
            XElement elAddr = Doc.Root.Element("IpAddress");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    public static string VideoPath
    {
        get
        {
            XElement elAddr = Doc.Root.Element("Video");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    public static string DvdPath
    {
        get
        {
            XElement elAddr = Doc.Root.Element("DVD");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    public static string BluRayPath
    {
        get
        {
            XElement elAddr = Doc.Root.Element("BluRay");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    public static Dictionary<string, string> SkyChannels
    {
        get
        {
            XElement elSky = Doc.Root.Element("Sky");
            return elSky.Elements("Channel").ToDictionary(el => el.Attribute("name").Value, el => el.Attribute("code").Value);
        }
    }

    public static string ReceiverAddress
    {
        get
        {
            XElement elAddr = Doc.Root.Element("ReceiverAddress");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    public static string SkyAddress
    {
        get
        {
            XElement elAddr = Doc.Root.Element("SkyAddress");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    public static List<string> SkyFavourites
    {
        get
        {
            XElement elSky = Doc.Root.Element("Sky");
            return elSky.Elements("Favourite").Select(el => el.Value).ToList();
        }
    }

    public static List<string> TvFavourites
    {
        get
        {
            XElement elTv = Doc.Root.Element("TV");
            return elTv.Elements("Favourite").Select(el => el.Value).ToList();
        }
    }

    public static bool useSkyLiveTV
    {
        get
        {
            XElement elLive = Doc.Root.Element("TV").Element("Live");
            return elLive == null || elLive.Value == "Sky";
        }
    }

    public static Dictionary<string, string> BBCTVChannels
    {
        get
        {
            XElement elBBC = Doc.Root.Element("BBC");
            return elBBC.Elements("TV").ToDictionary(el => el.Value, el => el.Attribute("id").Value);
        }
    }

    public static Dictionary<string, string> BBCRadioStations
    {
        get
        {
            XElement elBBC = Doc.Root.Element("BBC");
            return elBBC.Elements("Radio").ToDictionary(el => el.Value, el => el.Attribute("id").Value);
        }
    }

}