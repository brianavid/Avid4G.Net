using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;

/// <summary>
/// A class of configuration values helds in a manually edited XML file
/// </summary>
public static class Config
{
    static XDocument doc = null;

    /// <summary>
    /// The XML document
    /// </summary>
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

    /// <summary>
    /// The Media PC's fixed IP address
    /// </summary>
    public static string IpAddress
    {
        get
        {
            XElement elAddr = Doc.Root.Element("IpAddress");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    /// <summary>
    /// The Receiver's IP address
    /// </summary>
    public static string ReceiverAddress
    {
        get
        {
            XElement elAddr = Doc.Root.Element("ReceiverAddress");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    /// <summary>
    /// The path to the directory in which recoded TV programmes are stored
    /// </summary>
    public static string VideoPath
    {
        get
        {
            XElement elAddr = Doc.Root.Element("Video");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    /// <summary>
    /// The path to the directory in which ripped DVDs are stored
    /// </summary>
    public static string DvdPath
    {
        get
        {
            XElement elAddr = Doc.Root.Element("DVD");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    /// <summary>
    /// The path to the directory in which ripped BluRays are stored
    /// </summary>
    public static string BluRayPath
    {
        get
        {
            XElement elAddr = Doc.Root.Element("BluRay");
            return elAddr == null ? null : elAddr.Value;
        }
    }

    /// <summary>
    /// A collection of favourite Sky channels to be displayed first in any lists
    /// </summary>
    public static List<string> SkyFavourites
    {
        get
        {
            XElement elSky = Doc.Root.Element("Sky");
            return elSky.Elements("Favourite").Select(el => el.Value).ToList();
        }
    }

    /// <summary>
    /// A collection of Sky Radio channels
    /// </summary>
    public static List<Tuple<string, int, int>> SkyRadio
    {
        get
        {
            XElement elSky = Doc.Root.Element("Sky");
            return elSky.Elements("Radio").Select(el => new Tuple<string, int, int>(el.Value, int.Parse(el.Attribute("id").Value), int.Parse(el.Attribute("code").Value))).ToList();
        }
    }

    /// <summary>
    /// A collection of Sky Package codes
    /// </summary>
    public static List<int> SkyPackages
    {
        get
        {
            XElement elSky = Doc.Root.Element("Sky");
            return elSky.Elements("Package").Select(el => int.Parse(el.Value)).ToList();
        }
    }

    /// <summary>
    /// A collection of favourite terrestrial TV channels to be displayed first in any lists
    /// </summary>
    public static List<string> TvFavourites
    {
        get
        {
            XElement elTv = Doc.Root.Element("TV");
            return elTv.Elements("Favourite").Select(el => el.Value).ToList();
        }
    }

    /// <summary>
    /// When true, indicates that Sky should be used for preference over the terrestrial tuners for live TV
    /// </summary>
    public static bool UseSkyLiveTV
    {
        get
        {
            XElement elLive = Doc.Root.Element("TV").Element("Live");
            return elLive == null || elLive.Value == "Sky";
        }
    }

    /// <summary>
    /// The collection of BBC TV channels to be made available for iPlayer
    /// </summary>
    public static Dictionary<string, string> BBCTVChannels
    {
        get
        {
            XElement elBBC = Doc.Root.Element("BBC");
            return elBBC.Elements("TV").ToDictionary(el => el.Value, el => el.Attribute("id").Value);
        }
    }

    /// <summary>
    /// The collection of BBC radio stations to be made available for iPlayer
    /// </summary>
    public static Dictionary<string, string> BBCRadioStations
    {
        get
        {
            XElement elBBC = Doc.Root.Element("BBC");
            return elBBC.Elements("Radio").ToDictionary(el => el.Value, el => el.Attribute("id").Value);
        }
    }

}