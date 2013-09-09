using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

public class BBC
{
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
}
