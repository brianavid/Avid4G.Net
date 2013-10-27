﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;

/// <summary>
/// Summary description for RemotePotato
/// </summary>
public static class RemotePotato
{
    public class Programme
    {
        public String Id { get; private set; }
        public String Title { get; private set; }
        public String Description { get; private set; }
        public String Channel { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime StopTime { get; private set; }
        public TimeSpan Duration { get { return StopTime - StartTime; } }

        public Programme(
            String id,
            String title,
            String description,
            String channel,
            DateTime startTime,
            DateTime stopTime)
        {
            Id = id;
            Title = title;
            Description = description;
            Channel = channel;
            StartTime = startTime;
            StopTime = stopTime;
        }
    }

    public class Recording
    {
        public String Id { get; private set; }
        public String Title { get; private set; }
        public String Description { get; private set; }
        public String Channel { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime StopTime { get; private set; }
        public String EpisodeTitle { get; private set; }
        public String Filename { get; private set; }
        public TimeSpan Duration { get { return StopTime - StartTime; } }

        public Recording(
            String id,
            String title,
            String description,
            String channel,
            DateTime startTime,
            DateTime stopTime,
            String episodeTitle,
            String filename)
        {
            Id = id;
            Title = title;
            Description = description;
            Channel = channel;
            StartTime = startTime;
            StopTime = stopTime;
            EpisodeTitle = episodeTitle;
            Filename = filename;
        }
    }


    public class Channel
    {
        public string Name { get; internal set; }
        public int Id { get; internal set; }
        public bool IsRadio { get; internal set; }
        public bool IsFavourite { get; internal set; }

        internal Channel(
            string name,
            int id,
            bool isRadio,
            bool isFavourite)
        {
            Name = name;
            Id = id;
            IsRadio = isRadio;
            IsFavourite = isFavourite;
        }

        internal Channel(
            XElement ch,
            bool isRadio,
            bool isFavourite) : this(
                ch.Element("Callsign").Value,
                Convert.ToInt32(ch.Element("MCChannelNumber").Value),
                isRadio,
                isFavourite)
        {
        }

        public string Display
        {
            get
            {
                return String.Format("{0}", Name);
            }
        }
    }
    static public Dictionary<String, Recording> AllRecordings { get; private set; }

    static public IEnumerable<Recording> AllRecordingsInReverseTimeOrder
    {
        get { return AllRecordings.Values.OrderByDescending(r => r.StartTime); }
    }

    static public IEnumerable<Recording> AllRecordingsForTitle(
        string title)
    {
        return AllRecordingsInReverseTimeOrder.Where(r => r.Title == title);
    }

    static public IEnumerable<List<Recording>> AllRecordingsGroupedByTitle
    {
        get
        {
            Dictionary<string, List<Recording>> recordingsForTitle = new Dictionary<string, List<Recording>>();

            foreach (Recording r in AllRecordingsInReverseTimeOrder)
            {
                if (!recordingsForTitle.ContainsKey(r.Title))
                {
                    recordingsForTitle[r.Title] = new List<Recording>();
                }
                recordingsForTitle[r.Title].Add(r);
            }

            return recordingsForTitle.Values;
        }
    }

    static string host = null;
    public static string Host
    {
        get
        {
            if (host == null)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (var addr in addresses)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        host = "http://" + addr.ToString() + ":9080/";
                        break;
                    }
                }
            }
            return host;
        }
    }

    public static string Url
    {
        get { return Host + "xml/"; }
    }

    public static bool EnsureServiceRunning(
        bool recycle)
    {
        return DesktopClient.EnsureRemotePotatoRunning(recycle);
    }

    static XDocument GetXml(
        string url)
    {
        try
        {
	        Uri requestUri = new Uri(url);
	
	        HttpWebRequest request =
	            (HttpWebRequest)HttpWebRequest.Create(requestUri);
	        request.Method = WebRequestMethods.Http.Get;
	        request.ContentType = "text/xml";
	
	        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
	        XDocument xDoc =
	            XDocument.Load(new StreamReader(response.GetResponseStream()));
	
	        return xDoc;
        }
        catch
        {
            return null;
        }
    }

    static string GetText(
        string url)
    {
        try
        {
	        Uri requestUri = new Uri(url);
	
	        HttpWebRequest request =
	            (HttpWebRequest)HttpWebRequest.Create(requestUri);
	        request.Method = WebRequestMethods.Http.Get;
	
	        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
	        var responseStream = new StreamReader(response.GetResponseStream());
	
	        return responseStream.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }

    static XDocument GetXml(
        string url,
        XDocument requestDoc)
    {
        try
        {
	        Uri requestUri = new Uri(url);
	
	        HttpWebRequest request =
	            (HttpWebRequest)HttpWebRequest.Create(requestUri);
	        request.Method = WebRequestMethods.Http.Post;
	        StreamWriter requestWriter = new StreamWriter(request.GetRequestStream(), Encoding.UTF8);
	        requestWriter.Write(requestDoc.ToString());
	        requestWriter.Close();
	
	        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
	        XDocument xDoc =
	            XDocument.Load(new StreamReader(response.GetResponseStream()));
	
	        return xDoc;
        }
        catch
        {
            return null;
        }
    }

    static XDocument channelsXml = null;

    static XDocument ChannelsXml
    {
        get
        {
            if (channelsXml == null)
            {
                channelsXml = GetXml(Url + "channels/all");
                CurrentlySelectedChannelName = "";
                CurrentlySelectedChannelNumber = "";
            }

            return channelsXml;
        }
    }

    static IEnumerable<XElement> GetAllChannels()
    {
        return ChannelsXml.Element("ArrayOfTVService").Elements("TVService");
    }

    static IEnumerable<XElement> GetAllTvChannels()
    {
        return GetAllChannels().Where(ch => Convert.ToInt32(ch.Element("MCChannelNumber").Value) < 200);
    }

    static IEnumerable<XElement> GetAllFavouriteChannels()
    {
        Dictionary<string, XElement> tvChannels = new Dictionary<string,XElement>();
        foreach (var ch in GetAllTvChannels())
        {
            string callsign = ch.Element("Callsign").Value.ToLower();
            if (!tvChannels.ContainsKey(callsign))
            {
                tvChannels[callsign] = ch;
            }
        }

        return (Config.TvFavourites).Select(fav => tvChannels[fav.ToLower()]);
    }

    static IEnumerable<XElement> GetAllRadioChannels()
    {
        return GetAllChannels().Where(ch => Convert.ToInt32(ch.Element("MCChannelNumber").Value) >= 700);
    }

    public static IEnumerable<string> AllTvChannelNames
    {
        get
        {
            return GetAllTvChannels().Select(c => c.Element("Callsign").Value);
        }
    }

    public static IEnumerable<string> AllRadioChannelNames
    {
        get
        {
            return GetAllRadioChannels().Select(c => c.Element("Callsign").Value);
        }
    }

    public static IEnumerable<string> AllFavouriteChannelNames
    {
        get
        {
            return GetAllFavouriteChannels().Select(c => c.Element("Callsign").Value);
        }
    }

    public static IEnumerable<Channel> AllTvChannels
    {
        get
        {
            return GetAllTvChannels().Select(c => new Channel(c, false, false));
        }
    }

    public static IEnumerable<Channel> AllRadioChannels
    {
        get
        {
            return GetAllRadioChannels().Select(c => new Channel(c, true, false));
        }
    }

    public static IEnumerable<Channel> AllFavouriteChannels
    {
        get
        {
            return GetAllFavouriteChannels().Select(c => new Channel(c, false, true));
        }
    }

    public static IEnumerable<Channel> AllTvChannelsFavouriteFirst
    {
        get
        {
            Dictionary<string, Channel> favourites = AllFavouriteChannels.ToDictionary(c => c.Name);
            foreach (var c in favourites.Values)
            {
                yield return c;
            }
            foreach (var c in AllTvChannels)
            {
                if (!favourites.ContainsKey(c.Name))
                {
                    yield return c;
                }
            }
        }
    }

    public static string CurrentlySelectedChannelName { get; private set; }
    public static string CurrentlySelectedChannelNumber { get; private set; }

    public static string CurrentProgrammeTitle { 
        get 
        {
            if (CurrentlySelectedChannelName != null)
            {
                Programme[] nowAndNext = GetNowAndNext(CurrentlySelectedChannelName).ToArray();
                if (nowAndNext.Length > 0)
                {
                    return nowAndNext[0].Title;
                }
            }
            return "";
        }
    }

    public static string NextProgrameTime
    {
        get
        {
            if (CurrentlySelectedChannelName != null)
            {
                Programme[] nowAndNext = GetNowAndNext(CurrentlySelectedChannelName).ToArray();
                if (nowAndNext.Length > 1)
                {
                    return nowAndNext[1].StartTime.ToLocalTime().ToString("HH:mm");
                }
            }
            return "";
        }
    }

    public static string NextProgrameTitle
    {
        get
        {
            if (CurrentlySelectedChannelName != null)
            {
                Programme[] nowAndNext = GetNowAndNext(CurrentlySelectedChannelName).ToArray();
                if (nowAndNext.Length > 1)
                {
                    return nowAndNext[1].Title;
                }
            }
            return "";
        }
    }

    public static void SelectTvChannelName(
        string channelName)
    {
        CurrentlySelectedChannelName = channelName;

        if (channelName == "BBC ONE HD")
        {
            SelectTvChannelNumber("101");   //  BBC ONE HD is faked up as it has no EPG
            return;
        }
        XElement channel = GetAllChannels().Where(ch => ch.Element("Callsign").Value == channelName).First();
        SelectTvChannelNumber(channel.Element("MCChannelNumber").Value);
    }

    static void SelectTvChannelNumber(
        string channelNumber)
    {
        CurrentlySelectedChannelNumber = channelNumber;

        GetXml(Url + "sendremotekey/GotoLiveTV");
        foreach (var c in channelNumber)
        {
            GetXml(Url + "sendremotekey/Num" + c);
        }
        GetXml(Url + "sendremotekey/Enter");
    }

    public static IEnumerable<Programme> GetEpgProgrammes(
        DateTime day,
        string channelName)
    {
        DateTime nextDay = day + new TimeSpan(1, 0, 0, 0);
        return GetEpgProgrammesInRange(day, nextDay, channelName);
    }

    public static IEnumerable<Programme> GetNowAndNext(
        string channelName)
    {
        DateTime startTine = DateTime.UtcNow - new TimeSpan(0, 6, 0, 0); ;
        DateTime endTine = startTine + new TimeSpan(0, 24, 0, 0);
        return GetEpgProgrammesInRange(startTine, endTine, channelName).Take(2);
    }

    public static IEnumerable<Programme> GetEpgProgrammesInRange(
        DateTime startTime,
        DateTime endTine,
        string channelName)
    {
        EnsureServiceRunning(false);

        if (schedule == null)
        {
            LoadSchedule();
        }

        if (!String.IsNullOrEmpty(channelName))
        {
            if (channelName == "BBC ONE HD")
            {
                channelName = "BBC ONE";   //  BBC ONE HD is faked up as it has no EPG
            }

            channelName = channelName.ToLower();

            try
            {
                XElement channel = GetAllChannels().Where(ch => ch.Element("Callsign").Value.ToLower() == channelName).First();
                XDocument requestDoc = new XDocument(
                    new XElement("ArrayOfEPGRequest",
                        new XElement("EPGRequest",
                            new XElement("TVServiceID", channel.Element("UniqueId").Value),
                            new XElement("StartTime", startTime.Ticks),
                            new XElement("StopTime", endTine.Ticks))));

                XDocument programsDoc = GetXml(Url + "programmes/nodescription/byepgrequest", requestDoc);

                if (programsDoc != null)
                {
                    return programsDoc.Element("ArrayOfTVProgramme").Elements("TVProgramme")
                        .Select(
                            p => new Programme(
                                p.Element("Id").Value,
                                p.Element("Title").Value,
                                p.Element("Description").Value,
                                channelName,
                                new DateTime(Int64.Parse(p.Element("StartTime").Value)),
                                new DateTime(Int64.Parse(p.Element("StopTime").Value))))
                        .Where(
                                p => p.StopTime > DateTime.UtcNow);
                }
            }
            catch (System.Exception ex)
            {
            }
        }

        return new List<Programme>();
    }

    public static IEnumerable<Programme> GetScheduledRecordings()
    {
        EnsureServiceRunning(false);

        if (schedule == null)
        {
            LoadSchedule();
        }

        List<Programme> result = new List<Programme>();

	    foreach (var recording in schedule.Recordings
	        .OrderBy(rec => Convert.ToInt64(schedule.GetProgramme(rec).Element("StartTime").Value)))
	    {
	        var p = schedule.GetProgramme(recording);
	        var startTime = new DateTime(Convert.ToInt64(p.Element("StartTime").Value)).ToLocalTime();
            XElement channel = GetAllChannels().Where(ch => ch.Element("UniqueId").Value == p.Element("ServiceID").Value).First();

            result.Add (new Programme(
                        p.Element("Id").Value,
                        p.Element("Title").Value,
                        p.Element("Description").Value,
                        channel.Element("Callsign").Value,
                        new DateTime(Int64.Parse(p.Element("StartTime").Value)),
                        new DateTime(Int64.Parse(p.Element("StopTime").Value))));
        }

        return result;
    }

    public static void LoadAllRecordings()
    {
        EnsureServiceRunning(false);

        AllRecordings = new Dictionary<String, Recording>();

        try
        {
	        recordedDescriptions = new Dictionary<string, string>();
	        
	        XDocument recordingsDoc = null;

            for (int i = 0; i < 20; i++)
            {
                recordingsDoc = GetXml(Url + "recordedtv");
                if (recordingsDoc != null)
                {
                    break;
                }

                System.Threading.Thread.Sleep(500);
            }

            if (recordingsDoc != null)
            {
                foreach (var recording in recordingsDoc.Element("ArrayOfTVProgramme").Elements("TVProgramme")
                    .OrderBy(rec => -Convert.ToInt64(rec.Element("StartTime").Value)))
                {
                    string id = recording.Element("Id").Value;
                    DateTime startTime = new DateTime(Convert.ToInt64(recording.Element("StartTime").Value)).ToLocalTime();
                    DateTime stopTime = new DateTime(Convert.ToInt64(recording.Element("StopTime").Value)).ToLocalTime();

                    AllRecordings[id] = new Recording(
                        id,
                        recording.Element("Title").Value,
                        recording.Element("Description").Value,
                        recording.Element("WTVCallsign").Value,
                        startTime,
                        stopTime,
                        recording.Element("EpisodeTitle").Value,
                        recording.Element("Filename").Value);
                }
            }
        }
        catch (System.Exception ex)
        {
        	
        }
    }

    public static string GetDescription(
        string id)
    {
        if (recordedDescriptions != null && recordedDescriptions.ContainsKey(id))
        {
            return recordedDescriptions[id];
        }

        XDocument infoDoc = GetXml(Url + "programme/getinfo/" + id);
        return infoDoc == null ? "" : infoDoc.Element("TVProgrammeInfoBlob").Element("Description").Value;
    }

    public static void SendCommand(
        string command)
    {
        GetXml(Url + "sendremotekey/" + command);
    }

    public static string GetChannelNameById(
        string channelId)
    {
        XElement channel = GetAllChannels().Where(ch => ch.Element("UniqueId").Value == channelId).First();
        return channel.Element("Callsign").Value;
    }

    static ScheduledRecordings schedule = null;

    static void LoadSchedule()
    {
        XDocument scheduleDoc = GetXml(Url + "recordings ");
        schedule = new ScheduledRecordings(scheduleDoc);
    }

    public static bool IsScheduled(
        string programmeId)
    {
        return schedule.IsScheduled(programmeId);
    }

    public static bool IsSeries(
        string programmeId)
    {
        if (schedule != null)
        {
            foreach (var recording in schedule.Recordings)
            {
                if (recording.Element("TVProgrammeID").Value == programmeId)
                {
                    return recording.Element("SeriesID").Value != "0";
                }
            }
        }

        return false;
    }

    public static string RecordShow(
        string programmeId)
    {
        XDocument requestDoc = new XDocument(
            new XElement("RecordingRequest",
                new XElement("TVProgrammeID", programmeId),
                new XElement("RequestType", "OneTime"),
                new XElement("KeepUntil", "NotSet"),
                new XElement("Prepadding", "300"),
                new XElement("Postpadding", "300")));

        XDocument recordDoc = GetXml(Url + "record/byrecordingrequest ", requestDoc);
        LoadSchedule();

        if (recordDoc != null && !Convert.ToBoolean(recordDoc.Element("RecordingResult").Element("Success").Value))
        {
            return recordDoc.Element("RecordingResult").Element("ErrorMessage").Value;
        }

        return null;
    }

    public static string RecordSeries(
        string programmeId)
    {
        XDocument requestDoc = new XDocument(
            new XElement("RecordingRequest",
                new XElement("TVProgrammeID", programmeId),
                new XElement("RequestType", "Series"),
                new XElement("SeriesRequestSubType", "ThisChannelThisTime"),
                new XElement("KeepUntil", "NotSet"),
                new XElement("Prepadding", "120"),
                new XElement("Postpadding", "180")));

        XDocument recordDoc = GetXml(Url + "record/byrecordingrequest ", requestDoc);
        LoadSchedule();

        if (recordDoc != null && !Convert.ToBoolean(recordDoc.Element("RecordingResult").Element("Success").Value))
        {
            return recordDoc.Element("RecordingResult").Element("ErrorMessage").Value;
        }

        return null;
    }

    public static string CancelRecording(
        string programmeId)
    {
        if (schedule != null)
        {
            foreach (var recording in schedule.Recordings.Where(rec => rec.Element("TVProgrammeID").Value == programmeId))
            {
                GetText(Url + "cancelrequest/" + recording.Element("RPRequestID").Value);
            }
        }

        LoadSchedule();

        return null;
    }

    static Dictionary<string, string> recordedDescriptions;

    public static void DeleteRecording(
        Recording recording)
    {
        string path = recording.Filename;
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            LoadAllRecordings();
        }
    }
}