using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

/// <summary>
/// The BBC class can obtain the (future or historical) schedule of BBC Radio or TV programmes and
/// iPlayer URLS from which radio can be streamed or TV can be watched.
/// </summary>
public class BBC
{
    /// <summary>
    /// Class representing a single program in the BBC schedule of TV and radio programmes
    /// </summary>
    public class Programme
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Pid { get; private set; }
        public DateTime StartTime { get; private set; }
        public TimeSpan Duration { get; private set; }

        internal Programme(XElement broadcast)
        {
            Title = broadcast.Element("programme").Element("display_titles").Element("title").Value;
            Description = broadcast.Element("programme").Element("short_synopsis").Value;
            Pid = broadcast.Element("programme").Element("pid").Value;
            StartTime = DateTime.Parse(broadcast.Element("start").Value);
            Duration = TimeSpan.FromSeconds(double.Parse(broadcast.Element("duration").Value));
        }
    }

    /// <summary>
    /// Get the schedule of all TV or radio programmes for a single day and a single TV channel or radio station.
    /// </summary>
    /// <param name="station"></param>
    /// <param name="dateString"></param>
    /// <param name="stationSpecifier"></param>
    /// <returns></returns>
    public static IEnumerable<Programme> GetSchedule(
        string station,
        string dateString,
        string stationSpecifier)
    {
        string url = string.Format("http://www.bbc.co.uk/{0}/programmes/schedules/{1}{2}.xml",
            station, stationSpecifier == null ? "" : stationSpecifier + "/", dateString);
        XElement schedule = XDocument.Load(url).Root;

        return schedule.Descendants("broadcast").Select(broadcast => new Programme(broadcast));
    }

    /// <summary>
    /// Get the URL from which a (medium quality, non-seekable) WMA stream for a radio programme can be played in a media player
    /// </summary>
    /// <remarks>
    /// Other (higher quality) media variants can only be played with in a browser
    /// </remarks>
    /// <param name="pid"></param>
    /// <param name="name"></param>
    /// <param name="station"></param>
    /// <param name="startTime"></param>
    /// <returns></returns>
    public static string GetStreamUrl(
        string pid,
        out string name,
        out string station,
        out DateTime startTime)
    {
        string url1 = string.Format("http://www.bbc.co.uk/iplayer/playlist/{0}", pid);
        XElement mediaPlaylist = XDocument.Load(url1).Root;
        XNamespace ns1 = mediaPlaylist.GetDefaultNamespace();

        string id = mediaPlaylist.Element(ns1+"item").Attribute("identifier").Value;
        name = mediaPlaylist.Element(ns1 + "item").Element(ns1 + "title").Value;
        station = mediaPlaylist.Element(ns1 + "item").Element(ns1 + "service").Value;
        startTime = DateTime.Parse(mediaPlaylist.Element(ns1 + "item").Element(ns1 + "broadcast").Value);

        string url2 = string.Format("http://www.bbc.co.uk/mediaselector/4/mtis/stream/{0}", id);
        XElement mediaSelection = XDocument.Load(url2).Root;
        XNamespace ns2 = mediaSelection.GetDefaultNamespace();

        return mediaSelection.Elements(ns2+"media").Where(m => m.Attribute("encoding").Value == "wma9").First().Element(ns2+"connection").Attribute("href").Value;
    }

    /// <summary>
    /// Get the URL from which a TV programme can be watched in iPlayer in a browser
    /// </summary>
    /// <param name="pid"></param>
    /// <returns></returns>
    public static string GetTvPlayerUrl(
        string pid)
    {
        return "http://www.bbc.co.uk/iplayer/episode/" + pid;
    }
}
