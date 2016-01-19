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
using System.Globalization;


/// <summary>
/// Control class for DvbViewer service that provides a web service interface to DvbViewer and
/// its Recording Service for terrestrial TV and Radio, both live and recorded
/// </summary>
public class DvbViewer
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    //  All EPG timers use a date which is the number of days after this strange base date
    static readonly DateTime EpgBaseDate = new DateTime(1899, 12, 30);

    /// <summary>
    /// A terrestrial TV or Radio channel
    /// </summary>
    public class Channel
    {
        public int Number { get; private set; }
        public string Name { get; internal set; }
        public string Id { get; internal set; }
        public string EpgId { get; internal set; }
        public string LogoUrl { get; internal set; }
        public bool IsRadio { get; internal set; }
        public bool IsHD { get; internal set; }
        public bool IsFavourite { get; internal set; }
        public bool InError { get; internal set; }

        internal Channel(
            XElement xChan,
            bool isFavourite)
        {
            try
            {
                Number = int.Parse(xChan.Attribute("nr").Value);
                Name = xChan.Attribute("name").Value;
                Id = xChan.Attribute("ID").Value;
                EpgId = xChan.Attribute("EPGID").Value;
                IsRadio = xChan.Attribute("flags").Value == "16";
                IsHD = xChan.Attribute("flags").Value == "24" &&
                    Name.EndsWith(" HD");       //  Empirical
                if (xChan.Element("logo") != null)
                {
                    LogoUrl = xChan.Element("logo").Value;
                }
                IsFavourite = isFavourite;
                InError = false;
            }
            catch (System.Exception ex)
            {
                logger.Error("Error parsing Channel XML", ex);
                InError = true;
            }
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
    /// A TV or Radio programme in the EPG
    /// </summary>
    public class Programme
    {
        public String Id { get; private set; }
        public String Title { get; private set; }
        public String Description { get; private set; }
        public Channel Channel { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime StopTime { get; private set; }
        public TimeSpan Duration { get { return StopTime - StartTime; } }
        public bool InError { get; internal set; }
        public bool IsScheduled { get { return DvbViewer.IsScheduled(Id); } }

        public Programme(
            XElement xProg)
        {
            try
            {
                Id = xProg.Element("eventid").Value;
                Title = xProg.Element("titles").Element("title").Value;
                Description = xProg.Element("events").Element("event").Value;
                Channel = AllChannels.FirstOrDefault(c => c.EpgId == xProg.Attribute("channel").Value);
                StartTime = DateTime.ParseExact(
                    xProg.Attribute("start").Value,
                    new[] { "yyyyMMddHHmmss" },
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                StopTime = DateTime.ParseExact(
                    xProg.Attribute("stop").Value,
                    new[] { "yyyyMMddHHmmss" },
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                InError = false;
            }
            catch (System.Exception ex)
            {
                logger.Error("Error parsing Programme XML", ex);
                InError = true;
            }
        }
    }

    /// <summary>
    /// A scheduled timer to record a TV or Radio programme
    /// </summary>
    public class Timer
    {
        public String Id { get; private set; }
        public String Name { get; private set; }
        public Channel Channel { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime StopTime { get; private set; }
        public TimeSpan PrePad { get; private set; }
        public TimeSpan PostPad { get; private set; }
        public TimeSpan Duration { get { return StopTime - StartTime; } }
        public String EventId { get; private set; }
        public bool InSeries { get { return Series.Find(Name, Channel, StartTime) != null; } }
        public bool IsRecording { get; private set; }
        public bool InError { get; internal set; }

        public Timer(
            XElement xTimer)
        {
            try
            {
                Id = xTimer.Element("ID").Value;
                Name = xTimer.Element("Descr").Value;
                PrePad = xTimer.Attribute("PreEPG") == null ? TimeSpan.Zero : TimeSpan.FromMinutes(int.Parse(xTimer.Attribute("PreEPG").Value));
                PostPad = xTimer.Attribute("PostEPG") == null ? TimeSpan.Zero : TimeSpan.FromMinutes(int.Parse(xTimer.Attribute("PostEPG").Value));
                Channel = AllChannels.FirstOrDefault(c => c.Id == xTimer.Element("Channel").Attribute("ID").Value.Split('|')[0]);
                StartTime = DateTime.ParseExact(
                    xTimer.Attribute("Date").Value + " " + xTimer.Attribute("Start").Value,
                    new[] { "dd.MM.yyyy HH:mm:ss" },
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal) + PrePad;
                StopTime = StartTime.AddMinutes(int.Parse(xTimer.Attribute("Dur").Value)) - PrePad - PostPad;
                EventId = xTimer.Attribute("EPGEventID") == null ? "" : xTimer.Attribute("EPGEventID").Value;
                IsRecording = xTimer.Element("Recording").Value != "0";
                InError = false;
            }
            catch (System.Exception ex)
            {
                logger.Error("Error parsing Timer XML", ex);
                InError = true;
            }
        }
    }

    /// <summary>
    /// A Series definition
    /// </summary>
    public class Series
    {
        public String Id { get; private set; }
        public String Name { get; private set; }
        public Channel Channel { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime StartTimeLow { get; private set; }
        public DateTime StartTimeHigh { get; private set; }
        bool isDeleted = false;

        const string XmlFilename = @"C:\Avid.Net\Series.xml";
        const string Format = "dd-MM-yyyy HH:mm";
        const int PreWindowMinutes = 90;
        const int PostWindowMinutes = 180;

        Series(
            XElement xSeries)
        {
            try
            {
                Id = xSeries.Attribute("Id").Value;
                Name = xSeries.Attribute("Name").Value;
                Channel = NamedChannel(xSeries.Attribute("Channel").Value);
                StartTime = DateTime.ParseExact(xSeries.Attribute("StartTime").Value,
                    Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                StartTimeLow = DateTime.ParseExact(xSeries.Attribute("StartTimeLow").Value,
                    Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                StartTimeHigh = DateTime.ParseExact(xSeries.Attribute("StartTimeHigh").Value,
                    Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            }
            catch (System.Exception ex)
            {
                logger.Error("Error parsing Series XML", ex);
            }
        }

        Series(
            string id,
            string name,
            Channel channel,
            DateTime startTime)
        {
            Id = id;
            Name = name;
            Channel = channel;
            StartTime = startTime;
            var prePad = Math.Min(startTime.Hour * 60 + startTime.Minute, PreWindowMinutes);
            var postPad = Math.Min(23 * 60 + 59 - startTime.Hour * 60 + startTime.Minute, PostWindowMinutes);
            StartTimeLow = startTime.AddMinutes(-prePad);
            StartTimeHigh = startTime.AddMinutes(postPad);
        }

        static List<Series> seriesDefinitions = null;

        public static IEnumerable<Series> All { get { Load(); return seriesDefinitions.Where(s => !s.isDeleted); } }

        /// <summary>
        /// Load the ...
        /// </summary>
        static void Load()
        {
            if (seriesDefinitions == null)
            {
                if (File.Exists(Series.XmlFilename))
                {
                    XElement seriesDoc = XDocument.Load(Series.XmlFilename, LoadOptions.None).Root;
                    seriesDefinitions = seriesDoc.Elements("Series")
                        .Select(s => new Series(s))
                        .ToList();
                }
                else
                {
                    seriesDefinitions = new List<Series>();
                }
            }
        }

        static void Save()
        {
            XElement root = new XElement("SeriesDefinitions", 
                All.Select(s => s.ToXml));
            root.Save(Series.XmlFilename);
        }

        XElement ToXml
        {
            get
            {
                return new XElement("Series",
                    new XAttribute("Id", Id),
                    new XAttribute("Name", Name),
                    new XAttribute("Channel", Channel.Name),
                    new XAttribute("StartTime", StartTime.ToString(Format)),
                    new XAttribute("StartTimeLow", StartTimeLow.ToString(Format)),
                    new XAttribute("StartTimeHigh", StartTimeHigh.ToString(Format)));
            }
        }
        
        public static void Add(
            string id,
            string name,
            Channel channel,
            DateTime startTime)
        {
            Load();
            if (Find(name, channel, startTime) == null)
            {
                seriesDefinitions.Add(new Series(id, name, channel, startTime));
                Save();
            }
        }

        public static Series Find(
            string name,
            Channel channel,
            DateTime startTime)
        {
            Load();
            foreach (Series series in seriesDefinitions)
            {
                if (!series.isDeleted &&
                    series.Name == name &&
                    series.Channel == channel &&
                    series.StartTimeLow.DayOfWeek == startTime.DayOfWeek &&
                    series.StartTimeLow.TimeOfDay <= startTime.TimeOfDay &&
                    series.StartTimeHigh.TimeOfDay >= startTime.TimeOfDay)
                {
                    return series;
                }
            }
            return null;
        }

        public static void Delete(
            string id)
        {
            foreach (Series series in All.Where(s => s.Id == id))
            {
                series.isDeleted = true;
                Save();
            }
        }

    }

    /// <summary>
    /// A recorded TV or Radio programme stored in a file
    /// </summary>
    public class Recording
    {
        public String Id { get; private set; }
        public String Title { get; private set; }
        public String Description { get; private set; }
        public Channel Channel { get; private set; }
        public DateTime StartTime { get; private set; }
        public TimeSpan Duration { get; private set; }
        public DateTime StopTime { get { return StartTime + Duration; } }
        public String Filename { get; private set; }
        public bool InError { get; internal set; }

        public Recording(
            XElement xRecording)
        {
            try
            {
                Id = xRecording.Attribute("id").Value;
                Title = xRecording.Element("title").Value;
                Description = xRecording.Element("info").Value;
                Channel = AllChannels.FirstOrDefault(c => c.Name.Equals(xRecording.Element("channel").Value, StringComparison.CurrentCultureIgnoreCase));
                StartTime = DateTime.ParseExact(
                    xRecording.Attribute("start").Value,
                    new[] { "yyyyMMddHHmmss" },
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                Duration = TimeSpan.ParseExact(xRecording.Attribute("duration").Value,
                    "hhmmss",
                    CultureInfo.InvariantCulture);
                Filename = xRecording.Element("file").Value;
                InError = false;
            }
            catch (System.Exception ex)
            {
                logger.Error("Error parsing Recording XML", ex);
                InError = true;
            }
        }

        public bool IsRecording 
        { 
            get 
            {
                return Schedule.Where(s => s.Channel.Id == Channel.Id && s.StartTime == StartTime).Any(s => s.IsRecording);
            } 
        }

    }

    /// <summary>
    /// The host address of the DvbViewer Recording Service, which is the "real" address of the localhost
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

            if (host == null)
            {
                host = "127.0.0.1";
            }

            return host;
        }
    }
    static string host = null;

    /// <summary>
    /// The HTTP Url of the DvbViewer Recording Service
    /// </summary>
    public static string Url
    {
        get { return "http://" + Host + ":8089"; }
    }


    /// <summary>
    /// Send an HTTP GET request to the DvbViewer Recording Service, expecting an XML response, which is returned
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    static XDocument GetXml(
        string url,
        bool noResponseExpected = false)
    {
        try
        {
            if (!url.StartsWith("/"))
            {
                url = "/api/" + url;
            }
            Uri requestUri = new Uri(Url + url);

            HttpWebRequest request =
                (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.Method = WebRequestMethods.Http.Get;
            request.ContentType = "text/xml";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (noResponseExpected)
            {
                return null;
            }

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
    /// Commands intended for a running foreground DvbViewer rather than the background Recording Service
    /// are sent with a parameter of a "target" name. dvbTarget is the first (and only) such name found.
    /// </summary>
    static string DvbTarget
    {
        get 
        {
            if (dvbTarget == null) 
            {
                var targetXml = GetXml("dvbcommand.html").Element("targets").Element("target");
                if (targetXml != null)
                {
                    dvbTarget = targetXml.Value;
                }
            }

            return dvbTarget;
        }
    }
    static string dvbTarget = null;

    /// <summary>
    /// Commands for a running foreground DvbViewer are encoded as integers, but for usability, 
    /// a name mapping is read from the "actions.ini" file and stored in a Dictionary
    /// </summary>
    static Dictionary<string, int> definedCommands;

    static int recordingPrePadMinutes;
    static int recordingPostPadMinutes;

    /// <summary>
    /// Initialize, by populating the definedCommands Dictionary
    /// </summary>
    public static void Initialize()
    {
        definedCommands = new Dictionary<string, int>();
        using (var sr = new StreamReader(Config.DvbViewerActionsPath))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                var parts = line.Split('=');
                definedCommands[parts[0].ToLower()] = int.Parse(parts[1]);
            }
        }

        var status = GetXml("status.html").Element("status");
        recordingPrePadMinutes = int.Parse(status.Element("epgbefore").Value);
        recordingPostPadMinutes = int.Parse(status.Element("epgafter").Value);

        LoadSchedule();
        foreach (Series s in Series.All)
        {
            foreach (Programme p in GetEpgProgrammesInSeries(s))
            {
                if (!p.IsScheduled)
                {
                    AddTimer(p);
                }
            }
        }

        LastChannelChangeTime = DateTime.UtcNow;
    }

    /// <summary>
    /// The live channel name currently selected to watch
    /// </summary>
    public static Channel CurrentlySelectedChannel { get; private set; }
    public static String CurrentlySelectedChannelName { get { return CurrentlySelectedChannel == null ? "" : CurrentlySelectedChannel.Name; } }

    /// <summary>
    /// Get once only and cache the current set of channels
    /// </summary>
    static XDocument ChannelsXml
    {
        get
        {
            if (channelsXml == null)
            {
                channelsXml = GetXml("getchannelsxml.html?logo=1");
                CurrentlySelectedChannel = null;
            }

            return channelsXml;
        }
    }
    static XDocument channelsXml = null;

    /// <summary>
    /// The favourite channel names as configured
    /// </summary>
    static List<String> FavouriteChannelNames = Config.TvFavourites;

    /// <summary>
    /// Get once only and cache all the TV and Radio channels
    /// </summary>
    /// <returns>A collection of Channel objects</returns>
    static IEnumerable<Channel> AllChannels
    {
        get
        {
            if (allChannels == null)
            {
                var favouriteChannelNames = FavouriteChannelNames.Select(n => n.ToLower());
                allChannels = ChannelsXml.Element("channels").Element("root").Element("group").Elements("channel")
                    .Where(c => !String.IsNullOrEmpty(c.Attribute("name").Value))
                    .Select(c => new Channel(c, favouriteChannelNames.Contains(c.Attribute("name").Value.ToLower())))
                    .Where(c => !c.InError);
            }

            return allChannels;
        }
    }
    static IEnumerable<Channel> allChannels = null;

    /// <summary>
    /// A named channel
    /// </summary>
    public static Channel NamedChannel(
        String channelName)
    {
        return AllChannels.FirstOrDefault(ch => ch.Name.Equals(channelName, StringComparison.CurrentCultureIgnoreCase));
    }

    /// <summary>
    /// A numbered channel
    /// </summary>
    public static Channel NumberedChannel(
        int channelNumber)
    {
        return AllChannels.FirstOrDefault(ch => ch.Number == channelNumber);
    }

    public static IEnumerable<Channel> AllTvChannels
    {
        get
        {
            return AllChannels.Where(ch => !ch.IsRadio);
        }
    }

    /// <summary>
    /// A collection of all Radio channels
    /// </summary>
    public static IEnumerable<Channel> AllRadioChannels
    {
        get
        {
            return AllChannels.Where(ch => ch.IsRadio);
        }
    }

    /// <summary>
    /// A collection of all favourite TV channels
    /// </summary>
    public static IEnumerable<Channel> AllFavouriteChannels
    {
        get
        {
            var favourites = AllTvChannels.Where(ch => ch.IsFavourite).ToDictionary(c => c.Name.ToLower());
            foreach (var name in FavouriteChannelNames)
            {
                if (favourites.ContainsKey(name.ToLower()))
                {
                    yield return favourites[name.ToLower()];
                }
            }
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
                if (!favourites.ContainsKey(c.Name) && c.IsHD)
                {
                    yield return c;
                }
            }
            foreach (var c in AllTvChannels)
            {
                if (!favourites.ContainsKey(c.Name) && !c.IsHD)
                {
                    yield return c;
                }
            }
        }
    }

    /// <summary>
    /// A collection of all TV channel names
    /// </summary>
    public static IEnumerable<string> AllTvChannelNames
    {
        get
        {
            return AllTvChannels.Select(c => c.Name);
        }
    }

    /// <summary>
    /// A collection of all Radio channel names
    /// </summary>
    public static IEnumerable<string> AllRadioChannelNames
    {
        get
        {
            return AllRadioChannels.Select(c => c.Name);
        }
    }

    /// <summary>
    /// A collection of all favourite TV channel names
    /// </summary>
    public static IEnumerable<string> AllFavouriteChannelNames
    {
        get
        {
            return AllFavouriteChannels.Select(c => c.Name);
        }
    }

    /// <summary>
    /// A sorted collection of all TV channel names
    /// </summary>
    public static IEnumerable<string> AllTvChannelNamesFavouriteFirst
    {
        get
        {
            return AllTvChannelsFavouriteFirst.Select(c => c.Name);
        }
    }

    /// <summary>
    /// The title of the programme currently being broadcast on the channel currently selected to watch
    /// </summary>
    public static string CurrentProgrammeTitle
    {
        get
        {
            if (CurrentlySelectedChannel != null)
            {
                Programme[] nowAndNext = GetNowAndNext(CurrentlySelectedChannel).ToArray();
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
            if (CurrentlySelectedChannel != null)
            {
                Programme[] nowAndNext = GetNowAndNext(CurrentlySelectedChannel).ToArray();
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
            if (CurrentlySelectedChannel != null)
            {
                Programme[] nowAndNext = GetNowAndNext(CurrentlySelectedChannel).ToArray();
                if (nowAndNext.Length > 1)
                {
                    return nowAndNext[1].Title;
                }
            }
            return "";
        }
    }

    /// <summary>
    /// Note that DVBViewer has stopped
    /// </summary>
    public static void Stop()
    {
        CurrentlySelectedChannel = null;
        LastStatus = null;
    }

    /// <summary>
    /// Select to watch a channel specified by channel number
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="isStatus">Is this change as a result of a status notification tracking external changes?</param>
    public static void SelectChannel(
        Channel channel,
        bool isStatus = false)
    {
        //  Do not change channel as a result of a status notification if it comes within
        //  ten seconds of a "real" channel change. Otherwise, the messages could 
        //  "cross in the post" and the channel change may be reverted.
        if (channel != null && DvbTarget != null && 
            CurrentlySelectedChannel != channel &&
            (!isStatus || DateTime.UtcNow > LastChannelChangeTime.AddSeconds(10)))
        {
            GetXml(String.Format("dvbcommand.html?target={0}&cmd=-c{1}", DvbTarget, channel.Number));
            CurrentlySelectedChannel = channel;
            LastStatus = null;
        }

        if (!isStatus)
        {
            LastChannelChangeTime = DateTime.UtcNow;
        }
    }

    public static string GetChannelLogoUrl(
        Channel channel)
    {
        return channel.LogoUrl == null ? null : "http://" + Host + ":8089/" + channel.LogoUrl;
    }

    /// <summary>
    /// Get the collection of programmes from the EPG scheduled to be broadcast on a specified date for a specified channel name
    /// </summary>
    /// <param name="day"></param>
    /// <param name="channelName"></param>
    /// <returns></returns>
    public static IEnumerable<Programme> GetEpgProgrammesForDay(
        DateTime day,
        Channel channel)
    {
        DateTime nextDay = day + new TimeSpan(1, 0, 0, 0);
        return GetEpgProgrammesInRange(day, nextDay, channel);
    }

    /// <summary>
    /// Get once and cache the collection of programmes from the EPG scheduled to be broadcast on a specified date for a specified channel name
    /// </summary>
    /// <param name="day"></param>
    /// <param name="channelName"></param>
    /// <returns></returns>
    public static IEnumerable<Programme> GetEpgProgrammesForChannel(
        Channel channel)
    {
        if (channel == null)
        {
            return null;
        }

        if (!epgProgrammesByChannel.ContainsKey(channel.Name))
        {
            var xProg = GetXml("epg.html?lvl=2&channel=" + channel.EpgId);

            epgProgrammesByChannel[channel.Name] =
                xProg.Element("epg").Elements("programme")
                .Select(p => new Programme(p))
                .Where(p => !p.InError);

            foreach (var programme in epgProgrammesByChannel[channel.Name])
            {
                epgProgrammesByIdAndChannel[MakeIdAndChannelKey(programme.Id, channel.Name)] = programme;
            }
        }

        return epgProgrammesByChannel[channel.Name];
    }
    static Dictionary<String, IEnumerable<Programme>> epgProgrammesByChannel = new Dictionary<String, IEnumerable<Programme>>();
    static String MakeIdAndChannelKey(
        String Id,
        String channelName)
    {
        return Id + ";" + channelName; 
    }

    static Dictionary<String, Programme> epgProgrammesByIdAndChannel = new Dictionary<String, Programme>();

    public static Programme EpgProgramme(
        String Id,
        String channelName)
    {
        return epgProgrammesByIdAndChannel[MakeIdAndChannelKey(Id, channelName)];
    }

    /// <summary>
    /// Get the (at most two) "Now and Next" programmes for a specified channel name
    /// </summary>
    /// <param name="channelName"></param>
    /// <returns></returns>
    public static IEnumerable<Programme> GetNowAndNext(
        Channel channel)
    {
        var epgProgrammes = GetEpgProgrammesForChannel(channel);
        return epgProgrammes == null ?
            new Programme[0] :
            epgProgrammes.SkipWhile(p => p.StopTime <= DateTime.Now).Take(2);
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
        Channel channel)
    {
        var epgProgrammes = GetEpgProgrammesForChannel(channel);
        return epgProgrammes == null ?
            new Programme[0] :
            epgProgrammes.SkipWhile(p => p.StartTime <= startTime).TakeWhile(p => p.StartTime <= endTime);
    }


    static bool ProgrammeInSeries(
        Programme programme,
        Series series)
    {
        return
            programme.Title == series.Name &&
            programme.StartTime.DayOfWeek == series.StartTime.DayOfWeek &&
            programme.StartTime.TimeOfDay >= series.StartTimeLow.TimeOfDay &&
            programme.StartTime.TimeOfDay <= series.StartTimeHigh.TimeOfDay;
    }
     
    static IEnumerable<Programme> GetEpgProgrammesInSeries(
        Series series)
    {
        var epgProgrammes = GetEpgProgrammesForChannel(series.Channel);
        return epgProgrammes == null ?
            new Programme[0] :
            epgProgrammes.Where(p => ProgrammeInSeries(p, series));
    }


    /// <summary>
    /// Send an emulated remote control command (by number) to the running DvbViewer instance
    /// </summary>
    /// <param name="command"></param>
    public static void SendCommand(
        int command)
    {
        if (DvbTarget != null)
        {
            GetXml(String.Format("dvbcommand.html?target={0}&cmd=-x{1}", DvbTarget, command));
        }
    }

    /// <summary>
    /// Send an emulated remote control command (by name) to the running DvbViewer instance
    /// </summary>
    /// <param name="command"></param>
    public static bool SendCommand(
        string command)
    {
        command = command.ToLower();
        if (definedCommands.ContainsKey(command))
        {
            SendCommand(definedCommands[command]);
            return true;
        }

        return false;
    }

    /// <summary>
    /// The schedule of recordings
    /// </summary>
    public static IEnumerable<Timer> Schedule
    {
        get
        {
            if (schedule == null)
            {
                LoadSchedule();
            }
            return schedule;
        }
    }
    static Timer[] schedule = null;

    /// <summary>
    /// Load the schedule of recordings from the DvbViewer Recording Service
    /// </summary>
    public static void LoadSchedule()
    {
        XDocument scheduleDoc = GetXml("timerlist.html");
        schedule = scheduleDoc.Element("Timers").Elements("Timer")
            .Select(t => new Timer(t))
            .Where(t => !t.InError)
            .ToArray();
        Array.Sort(schedule, (s1, s2) => s1.StartTime.CompareTo(s2.StartTime));
;
    }

    /// <summary>
    /// Is the identified programme scheduled to record?
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
    static bool IsScheduled(
        string programmeId)
    {
        return schedule.Any(t => t.EventId == programmeId);
    }

    /// <summary>
    /// Schedule a recording for an identified programme from the EPG
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
    public static Timer AddTimer(
        String id,
        String channelName,
        bool isSeries = false)
    {
        string key = MakeIdAndChannelKey(id, channelName);
        return epgProgrammesByIdAndChannel.ContainsKey(key) ? AddTimer(epgProgrammesByIdAndChannel[key], isSeries) : null;
    }

    /// <summary>
    /// Schedule a recording for an identified programme from the EPG
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
    public static Timer AddTimer(
        Programme programme,
        bool isSeries = false)
    {
        if (programme == null)
        {
            return null;
        }

        try
        {
#if UseApiToRecord
            GetXml(String.Format("timeradd.html?ch={0}&dor={1}&enable=1&start={2}&stop={3}&title={4}",
                programme.Channel.Id,
                (programme.StartTime - EpgBaseDate).Days,
                programme.StartTime.Hour * 60 + programme.StartTime.Minute,
                programme.StopTime.Hour * 60 + programme.StopTime.Minute,
                System.Uri.EscapeDataString(programme.Title)), true);
#else
            try
            {
                String requestUrl = String.Format(
                    "http://{0}:8089/timer_new.html?active=active&prio=50" +
                    "&channel={1}" +
                    "&title={2}" +
                    "&dor={3}&epgbefore={4}&starttime={5}&endtime={6}&epgafter={7}" +
                    "&Exitaktion=0&Aufnahmeaktion=0&folder=Auto&Series=&Format=2" +
                    "&scheme=%25year-%25date_%25time_%25station_%25event&RecAllAudio=checkbox" +
                    "&PATPMTAdjust=checkbox&searchaction=none&aktion=timer_add&source=timer_add" +
                    "&referer={8}" +
                    "&timer_id=&do=&timertype=0&save=Speichern&pdc=0&epgevent={9}&_={10}",
                    Host,
                    programme.Channel.Number,
                    System.Uri.EscapeDataString(programme.Title),
                    programme.StartTime.ToString("dd.MM.yyyy"),
                    recordingPrePadMinutes,
                    System.Uri.EscapeDataString(programme.StartTime.ToString("HH:mm")),
                    System.Uri.EscapeDataString(programme.StopTime.ToString("HH:mm")),
                    recordingPostPadMinutes,
                    System.Uri.EscapeDataString("http://localhost:83/Guide/Home"),
                    programme.Id,
                    DateTime.UtcNow.Ticks);

                HttpWebRequest request =
                    (HttpWebRequest)HttpWebRequest.Create(requestUrl);
                request.Method = WebRequestMethods.Http.Get;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            }
            catch (System.Exception ex)
            {
                logger.Error("Can't set timer", ex);
            }
#endif
            LoadSchedule();
            var timer = Schedule.FirstOrDefault(t => t.EventId == programme.Id);

            if (isSeries && timer != null)
            {
                Series series = Series.Find(timer.Name, timer.Channel, timer.StartTime);
                if (series == null)
                {
                    Series.Add(timer.EventId, timer.Name, timer.Channel, timer.StartTime);
                }
            }

            return timer;
        }
        catch (System.Exception ex)
        {
            logger.Error("Cannot add timer", ex);
            return null;
        }
    }

    /// <summary>
    /// Cancel the scheduled recording for an identified programme
    /// </summary>
    /// <param name="programmeId"></param>
    /// <returns></returns>
    public static string CancelTimer(
        String timerId)
    {
        GetXml(String.Format("timerdelete.html?id={0}&delfile=1", timerId));
        Series.Delete(timerId);

        LoadSchedule();

        return null;
    }


    /// <summary>
    /// All stored recordings, keyed by Id
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
    /// Load the collection of recordings from DVBViewer
    /// </summary>
    public static void LoadAllRecordings()
    {
        AllRecordings = new Dictionary<String, Recording>();

        try
        {
            XDocument recordingsDoc = GetXml("recordings.html");
            var recordings = recordingsDoc.Element("recordings").Elements("recording")
                .Select(r => new Recording(r))
                .Where(r => !r.InError);

            foreach (var r in recordings)
            {
                if (File.Exists(r.Filename))
                {
                    AllRecordings[r.Id] = r;
                }
                else
                {
                    logger.Info("Non-existent recording: {0}", r.Filename);
                    DeleteRecording(r);
                }
            }
        }
        catch (Exception)
        {

        }
    }

    /// <summary>
    /// Delete and identified recording
    /// </summary>
    /// <param name="recordingId"></param>
    public static void DeleteRecording(
        Recording recording)
    {
        if (recording != null)
        {
            logger.Info("Delete recording file: {0}", recording.Filename);
            GetXml(String.Format("recdelete.html?recid={0}&delfile=1", recording.Id));
        }
    }

    /// <summary>
    /// Run a background task to "Cleanup" (remove DB entries that refer to non-existent recording files) 
    /// and "Refresh" (add DB entries for TS recording files that were previously unknown)
    /// </summary>
    public static void CleanupRefreshDB()
    {
        try
        {
            logger.Info("Cleanup/Refresh Recording DB");
            //  Note use of absolute path to avoid prepending "/api/"
            GetXml("/tasks.html?task=CleanupDB&aktion=tasks", true);
            GetXml("/tasks.html?task=RefreshDB&aktion=tasks", true);
        }
        catch (System.Exception ex)
        {
            logger.Error("Can't run task Cleanup/Refresh DB: ", ex);
        }
    }

    /// <summary>
    /// The last recorded status posted from the DVB monitor
    /// </summary>
    public static XDocument LastStatus { private get; set; }

    /// <summary>
    /// The last time the channel was changed
    /// </summary>
    public static DateTime LastChannelChangeTime { private get; set; }

    /// <summary>
    /// The title of the currently watched programme as reported by the DVB monitor
    /// </summary>
    public static string NowTitle { get { return LastStatus == null ? "" : LastStatus.Root.Element("Now").Attribute("title").Value; } }

    /// <summary>
    /// The description of the currently watched programme as reported by the DVB monitor
    /// </summary>
    public static string NowDescription { get { return LastStatus == null ? "" : LastStatus.Root.Element("Now").Attribute("description").Value; } }

    /// <summary>
    /// The title of the next programme on the current channel as reported by the DVB monitor
    /// </summary>
    public static string NextTitle { get { return LastStatus == null ? "" : LastStatus.Root.Element("Next").Attribute("title").Value; } }

    /// <summary>
    /// The description of the next programme on the current channel as reported by the DVB monitor
    /// </summary>
    public static string NextDescription { get { return LastStatus == null ? "" : LastStatus.Root.Element("Next").Attribute("description").Value; } }

    /// <summary>
    /// The start time of the next programme on the current channel as reported by the DVB monitor
    /// </summary>
    public static string NextStart { get { return LastStatus == null ? "" : LastStatus.Root.Element("Next").Attribute("start").Value; } }
    
    /// <summary>
    /// The "playstate" for display as reported by the DVB monitor, taking into account time-shifting
    /// </summary>
    public static string PlayState 
    { 
        get 
        {
            if (LastStatus == null)
            {
                return "";
            }
            else
            {
                var timeShifted = LastStatus.Root.Element("TimeShift") != null;
                var playState = LastStatus.Root.Element("PlayState").Attribute("state").Value;
                return playState != "Playing" ? playState : timeShifted ? "Delayed" : "";
            }
        } 
    }


    /// <summary>
    /// The time-shift (if any) for display as reported by the DVB monitor
    /// </summary>
    public static string TimeShift 
    { 
        get 
        {
            try
            {
	            if (LastStatus == null || LastStatus.Root.Element("TimeShift") == null)
	            {
	                return "";
	            }
	            else
	            {
	                TimeSpan delay = TimeSpan.ParseExact(LastStatus.Root.Element("TimeShift").Attribute("remain").Value, @"hh\:mm\:ss", CultureInfo.InvariantCulture);
                    
                    //  If the player remains paused, advance the time-shoft delay since the pause was last reported
	                if (LastStatus.Root.Element("PlayState").Attribute("state").Value == "Paused")
	                {
	                    DateTime reportTime = DateTime.Parse(LastStatus.Root.Attribute("when").Value);
	                    delay += DateTime.Now - reportTime;
	                }
	
	                return delay.ToString(@"mm\:ss");
	            }
            }
            catch (System.Exception ex)
            {
                logger.Error("Can't get TimeShift", ex);
                return "";
            }
        } 
    }

    /// <summary>
    /// Id the program on the current channel being recorded?
    /// </summary>
    public static bool IsRecordingNow
    {
        get
        {
            if (CurrentlySelectedChannel == null)
            {
                return false;
            }
            var now = GetNowAndNext(CurrentlySelectedChannel).FirstOrDefault();
            return now == null ? false : now.IsScheduled;

        }
    }
}
