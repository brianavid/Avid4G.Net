using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using NLog;

/// <summary>
/// Control class for RemotePotato service that provides a web service interface to Windows Media Center for terrestrial TV and Radio, both live and recorded
/// </summary>
public static class RemotePotato
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// A terrestrial programme, either in the EPG or scheduled to be recorded
    /// </summary>
    public class Programme
    {
        public String Id { get; private set; }
        public String Title { get; private set; }
        public String Description { get; private set; }
        public String Channel { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime StopTime { get; private set; }
        public TimeSpan Duration { get { return StopTime - StartTime; } }
        public String SeriesId { get; private set; }

        public Programme(
            String id,
            String title,
            String description,
            String channel,
            DateTime startTime,
            DateTime stopTime,
            String seriesId)
        {
            Id = id;
            Title = title;
            Description = description;
            Channel = channel;
            StartTime = startTime;
            StopTime = stopTime;
            SeriesId = seriesId;
        }
    }

    /// <summary>
    /// A terrestrial recording
    /// </summary>
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

    /// <summary>
    /// A terrestrial TV or Radio channel
    /// </summary>
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

        /// <summary>
        /// Constructor from XML representation
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="isRadio"></param>
        /// <param name="isFavourite"></param>
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

        /// <summary>
        /// Format the channel for display
        /// </summary>
        public string Display
        {
            get
            {
                return String.Format("{0}", Name);
            }
        }
    }

    /// <summary>
    /// A collection of scheduled and recorded recordings
    /// </summary>
    public class ScheduledRecordings
    {
        /// <summary>
        /// Constructor from a RPRecordingsBlob XML element
        /// </summary>
        /// <param name="recordingsBlob"></param>
        public ScheduledRecordings(
            XDocument recordingsBlob)
        {
            requests = new Dictionary<string, XElement>();
            recordings = new Dictionary<string, XElement>();
            programmes = new Dictionary<string, XElement>();

            foreach (var request in recordingsBlob.Element("RPRecordingsBlob").Element("RPRequests").Elements("RPRequest"))
            {
                string id = request.Element("ID").Value;
                requests[id] = request;
            }

            foreach (var recording in recordingsBlob.Element("RPRecordingsBlob").Element("RPRecordings").Elements("RPRecording"))
            {
                string id = recording.Element("Id").Value;
                recordings[id] = recording;
            }

            foreach (var programme in recordingsBlob.Element("RPRecordingsBlob").Element("TVProgrammes").Elements("TVProgramme"))
            {
                string id = programme.Element("Id").Value;
                programmes[id] = programme;
            }
        }

        /// <summary>
        /// All recordings, whether scheduled or recorded, keyed by recording ID
        /// </summary>
        Dictionary<string, XElement> recordings;

        /// <summary>
        /// All scheduled recording requests
        /// </summary>
        Dictionary<string, XElement> requests;

        /// <summary>
        /// All recorded programmes
        /// </summary>
        Dictionary<string, XElement> programmes;

        /// <summary>
        /// All recordings, whether scheduled or recorded
        /// </summary>
        public IEnumerable<XElement> Recordings { get { return recordings.Values; } }

        /// <summary>
        /// Get the scheduled recording request for a particular recording 
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        public XElement GetRequest(XElement recording)
        {
            return requests[recording.Element("RPRequestID").Value];
        }

        /// <summary>
        /// Get the programme for a particular recording
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        public XElement GetProgramme(XElement recording)
        {
            return programmes[recording.Element("TVProgrammeID").Value];
        }

        /// <summary>
        /// Is there a scheduled recording request for a particular programme?
        /// </summary>
        /// <param name="programmeId"></param>
        /// <returns></returns>
        public bool IsScheduled(string programmeId)
        {
            return programmes.ContainsKey(programmeId);
        }
    }   
    
    /// <summary>
    /// All recordings, keyed by Id
    /// </summary>
    static public Dictionary<String, Recording> AllRecordings { get; private set; }

    /// <summary>
    /// All recordings, most recent first
    /// </summary>
    static public IEnumerable<Recording> AllRecordingsInReverseTimeOrder
    {
        get { return AllRecordings.Values.OrderByDescending(r => r.StartTime); }
    }

    /// <summary>
    /// All recordings sharing the same title, most recent first
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    static public IEnumerable<Recording> AllRecordingsForTitle(
        string title)
    {
        return AllRecordingsInReverseTimeOrder.Where(r => r.Title == title);
    }

    /// <summary>
    /// All recordings as a collection of Lists, grouped with those sharing a title in the same List
    /// </summary>
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

    /// <summary>
    /// The host address of the RemotePotato service, which is the "real" address of the localhost
    /// </summary>
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
                        host = addr.ToString();
                        break;
                    }
                }
            }
            return host;
        }
    }
    static string host = null;

    /// <summary>
    /// The HTTP Url of the RemotePotato service
    /// </summary>
    public static string Url
    {
        get { return "http://" + Host + ":9080/xml/"; }
    }

    /// <summary>
    /// Ensure that the RemotePotato service is running and has not died, starting the service if it is not running
    /// </summary>
    /// <param name="recycle">If true; unconditionally stops and restarts the service</param>
    /// <returns>True if the service is now running</returns>
    public static bool EnsureServiceRunning(
        bool recycle)
    {
        return DesktopClient.EnsureRemotePotatoRunning(recycle);
    }

    /// <summary>
    /// Send an HTTP GET request to the RemotePotato service, expecting an XML response, which is returned
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Send an HTTP GET request to the RemotePotato service, expecting an unformated string response, which is returned
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Send an HTTP POST request to the RemotePotato service with an XML body, expecting an XML response, which is returned
    /// </summary>
    /// <param name="url"></param>
    /// <param name="requestDoc"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Get exactly once the collection of known TV and Radio channels
    /// </summary>
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
    static XDocument channelsXml = null;

    /// <summary>
    /// Get all TV and Radio channels
    /// </summary>
    /// <returns>A collection of TVService XML elements</returns>
    static IEnumerable<XElement> GetAllChannels()
    {
        return ChannelsXml.Element("ArrayOfTVService").Elements("TVService");
    }

    /// <summary>
    /// Get all TV channels
    /// </summary>
    /// <remarks>
    /// This expects the convention that TV channels have a channel number less than 200
    /// </remarks>
    /// <returns>A collection of TVService XML elements</returns>
    static IEnumerable<XElement> GetAllTvChannels()
    {
        return GetAllChannels().Where(ch => Convert.ToInt32(ch.Element("MCChannelNumber").Value) < 200);
    }

    /// <summary>
    /// Get all TV channels which are configured as favourite channel names
    /// </summary>
    /// <returns>A collection of TVService XML elements</returns>
    static IEnumerable<XElement> GetAllFavouriteChannels()
    {
        //  Build a temporary Dctionary keyed by channel name (Callsign)
        Dictionary<string, XElement> tvChannels = new Dictionary<string,XElement>();
        foreach (var ch in GetAllTvChannels())
        {
            string callsign = ch.Element("Callsign").Value.ToLower();
            if (!tvChannels.ContainsKey(callsign))
            {
                tvChannels[callsign] = ch;
            }
        }

        //  Select the channels which are configured as favourite channel names
        return (Config.TvFavourites)
            .Select(fav => fav.ToLower())
            .Where(fav => tvChannels.ContainsKey(fav))
            .Select(fav => tvChannels[fav]);
    }

    /// <summary>
    /// Get all Radio channels
    /// </summary>
    /// This expects the convention that Radio channels have a channel number no less than 700
    /// </remarks>
    /// <returns>A collection of TVService XML elements</returns>
    static IEnumerable<XElement> GetAllRadioChannels()
    {
        return GetAllChannels().Where(ch => Convert.ToInt32(ch.Element("MCChannelNumber").Value) >= 700);
    }

    /// <summary>
    /// A collection of all TV channel names
    /// </summary>
    public static IEnumerable<string> AllTvChannelNames
    {
        get
        {
            return GetAllTvChannels().Select(c => c.Element("Callsign").Value);
        }
    }

    /// <summary>
    /// A collection of all Radio channel names
    /// </summary>
    public static IEnumerable<string> AllRadioChannelNames
    {
        get
        {
            return GetAllRadioChannels().Select(c => c.Element("Callsign").Value);
        }
    }

    /// <summary>
    /// A collection of all favourite TV channel names
    /// </summary>
    public static IEnumerable<string> AllFavouriteChannelNames
    {
        get
        {
            return GetAllFavouriteChannels().Select(c => c.Element("Callsign").Value);
        }
    }

    /// <summary>
    /// A collection of all TV channels
    /// </summary>
    public static IEnumerable<Channel> AllTvChannels
    {
        get
        {
            return GetAllTvChannels().Select(c => new Channel(c, false, false));
        }
    }

    /// <summary>
    /// A collection of all Radio channels
    /// </summary>
    public static IEnumerable<Channel> AllRadioChannels
    {
        get
        {
            return GetAllRadioChannels().Select(c => new Channel(c, true, false));
        }
    }

    /// <summary>
    /// A collection of all favourite TV channels
    /// </summary>
    public static IEnumerable<Channel> AllFavouriteChannels
    {
        get
        {
            return GetAllFavouriteChannels().Select(c => new Channel(c, false, true));
        }
    }

    /// <summary>
    /// A collection of all TV channels, ordered with the favourites at the start
    /// </summary>
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

    /// <summary>
    /// The live channel name currently selected to watch
    /// </summary>
    public static string CurrentlySelectedChannelName { get; private set; }

    /// <summary>
    /// The live channel number currently selected to watch
    /// </summary>
    public static string CurrentlySelectedChannelNumber { get; private set; }

    /// <summary>
    /// The title of the programme currently being broadcast on the channel currently selected to watch
    /// </summary>
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

    /// <summary>
    /// The formatted start time of the next programme to be broadcast on the channel currently selected to watch
    /// </summary>
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

    /// <summary>
    /// The title of the next programme to be broadcast on the channel currently selected to watch
    /// </summary>
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

    /// <summary>
    /// Select to watch a  channel specified by channel name
    /// </summary>
    /// <param name="channelName"></param>
    public static void SelectTvChannelName(
        string channelName)
    {
        CurrentlySelectedChannelName = channelName;

        XElement channel = GetAllChannels().Where(ch => ch.Element("Callsign").Value == channelName).First();
        SelectTvChannelNumber(channel.Element("MCChannelNumber").Value);
    }

    /// <summary>
    /// Select to watch a  channel specified by channel number
    /// </summary>
    /// <param name="channelNumber"></param>
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

    /// <summary>
    /// Get the collection of programmes from the EPG scheduled to be broadcast on a specified date for a specified channel name
    /// </summary>
    /// <param name="day"></param>
    /// <param name="channelName"></param>
    /// <returns></returns>
    public static IEnumerable<Programme> GetEpgProgrammes(
        DateTime day,
        string channelName)
    {
        DateTime nextDay = day + new TimeSpan(1, 0, 0, 0);
        return GetEpgProgrammesInRange(day, nextDay, channelName);
    }

    /// <summary>
    /// Get the (at most two) "Now and Next" programmes for a specified channel name
    /// </summary>
    /// <param name="channelName"></param>
    /// <returns></returns>
    public static IEnumerable<Programme> GetNowAndNext(
        string channelName)
    {
        DateTime startTime = DateTime.UtcNow - new TimeSpan(0, 6, 0, 0); ;
        DateTime endTime = startTime + new TimeSpan(0, 24, 0, 0);
        return GetEpgProgrammesInRange(startTime, endTime, channelName).Take(2);
    }

    /// <summary>
    /// Get the collection of programmes from the EPG within a specified date range for the specified channel
    /// </summary>
    /// <remarks>
    /// Only programmes that have not yet stopped will be returned
    /// </remarks>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="channelName"></param>
    /// <returns></returns>
    public static IEnumerable<Programme> GetEpgProgrammesInRange(
        DateTime startTime,
        DateTime endTime,
        string channelName)
    {
        EnsureServiceRunning(false);

        if (schedule == null)
        {
            LoadSchedule();
        }

        if (!String.IsNullOrEmpty(channelName))
        {
            channelName = channelName.ToLower();

            try
            {
                XElement channel = GetAllChannels().Where(ch => ch.Element("Callsign").Value.ToLower() == channelName).First();
                XDocument requestDoc = new XDocument(
                    new XElement("ArrayOfEPGRequest",
                        new XElement("EPGRequest",
                            new XElement("TVServiceID", channel.Element("UniqueId").Value),
                            new XElement("StartTime", startTime.Ticks),
                            new XElement("StopTime", endTime.Ticks))));

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
                                new DateTime(Int64.Parse(p.Element("StopTime").Value)),
                                p.Element("SeriesID").Value))
                        .Where(
                                p => p.StopTime > DateTime.UtcNow);
                }
            }
            catch (Exception)
            {
            }
        }

        return new List<Programme>();
    }

    /// <summary>
    /// Get the collection of programmes scheduled to record
    /// </summary>
    /// <returns></returns>
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
                        new DateTime(Int64.Parse(p.Element("StopTime").Value)),
                        p.Element("SeriesID").Value));
        }

        return result;
    }

    /// <summary>
    /// Load the collection of recordings from Windows Media Center
    /// </summary>
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
        catch (Exception)
        {
        	
        }
    }

    /// <summary>
    /// Get the full description for an identified programme
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
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

    /// <summary>
    /// DEscriptions (fetched only once) for identified programmes
    /// </summary>
    static Dictionary<string, string> recordedDescriptions;

    /// <summary>
    /// Send an emulated remote control command (by name) to the running Windows Media Center
    /// </summary>
    /// <param name="command"></param>
    public static void SendCommand(
        string command)
    {
        GetXml(Url + "sendremotekey/" + command);
    }

    /// <summary>
    /// The schedule of recordings
    /// </summary>
    static ScheduledRecordings schedule = null;

    /// <summary>
    /// Load the schedule of recordings from Windows Media Center
    /// </summary>
    public static void LoadSchedule()
    {
        XDocument scheduleDoc = GetXml(Url + "recordings ");
        schedule = new ScheduledRecordings(scheduleDoc);
    }

    /// <summary>
    /// Is the identified programme scheduled to record?
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
    public static bool IsScheduled(
        string programmeId)
    {
        return schedule.IsScheduled(programmeId);
    }

    /// <summary>
    /// Is the identified programme scheduled to record as part of a series?
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Schedule a recording for an identified programme from the EPG
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Schedule a recording for the complete series for an identified programme from the EPG
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Cancel the scheduled recording for an identified programme
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Delete the file containing an particular recording
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
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