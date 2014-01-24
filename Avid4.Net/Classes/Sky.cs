using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Net.Sockets;
using System.Threading;
using NLog;
using Microsoft.Win32;

/// <summary>
/// Class to control and access a Sky Set Top Box via its (undocumented) web service interfaces.
/// The singleton static property Sky is used to access all members. 
/// </summary>
/// <remarks>
/// Before accessing the Sky property, it is necessary to first call the Initialse() method
/// </remarks>
public class SkyData
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Classes into which JSON formats from the web can be deserialized.
    /// </summary>
    internal class JsonFormat
    {
        /// <summary>
        /// Sky channels listed via http://tv.sky.com/channel/index
        /// </summary>
        internal class SkyChannelInfo
        {
            public SkyChannelInit init { get; set; }
        }
        internal class SkyChannelInit
        {
            public SkyChannelChannel[] channels { get; set; }
        }
        internal class SkyChannelChannel
        {
            public int[] c { get; set; }
            public string lcn { get; set; }
            public string pt { get; set; }
            public string t { get; set; }

        }

        /// <summary>
        /// Now and Next listings for a channel via http://epgservices.sky.com/5.1.1/api/2.0/channel/json/{CHANNEL}/now/nn/4
        /// </summary>
        internal class SkyProgrammeInfo
        {
            public Dictionary<string, SkyListing[]> listings { get; set; }
        }
        internal class SkyListing
        {
            public string d { get; set; }
            public string img { get; set; }
            public string[] l { get; set; }
            public string rr { get; set; }
            public int s { get; set; }
            public int sid { get; set; }
            public string t { get; set; }
            public string url { get; set; }
            public object[] m { get; set; }
        }
    }

    /// <summary>
    /// Representation of a Recording stored on the STB
    /// </summary>
    public class Recording
    {
        public string Title { get; internal set; }
        public string Id { get; internal set; }
        public string Resource { get; internal set; }
        public DateTime WhenRecorded { get; internal set; }
        public TimeSpan Duration { get; internal set; }
        public TimeSpan PrePad { get; internal set; }
        public TimeSpan PostPad { get; internal set; }
        public TimeSpan LastViewed { get; internal set; }
        public string ChannelName { get; internal set; }
        public string Status { get; internal set; }
        public string Description { get; internal set; }
        public bool BeingRecorded { get; internal set; }
        public Int64 Size { get; internal set; }
        public bool BeenWatched { get { return LastViewed.TotalMinutes * 5 > Duration.TotalMinutes * 4; } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title"></param>
        /// <param name="id"></param>
        /// <param name="resource"></param>
        /// <param name="whenRecorded"></param>
        /// <param name="duration"></param>
        /// <param name="prePad"></param>
        /// <param name="postPad"></param>
        /// <param name="lastViewed"></param>
        /// <param name="channelName"></param>
        /// <param name="status"></param>
        /// <param name="description"></param>
        /// <param name="beingRecorded"></param>
        /// <param name="size"></param>
        internal Recording(
            string title,
            string id,
            string resource,
            DateTime whenRecorded,
            TimeSpan duration,
            TimeSpan prePad,
            TimeSpan postPad,
            TimeSpan lastViewed,
            string channelName,
            string status,
            string description,
            bool beingRecorded,
            Int64 size)
        {
            Title = title;
            Id = id;
            Resource = resource;
            WhenRecorded = whenRecorded;
            Duration = duration;
            PrePad = prePad;
            PostPad = postPad;
            LastViewed = lastViewed;
            ChannelName = channelName;
            Status = status;
            Description = description;
            BeingRecorded = beingRecorded;
            Size = size;
        }
    }

    /// <summary>
    /// Representation of a TV or Radio Channel
    /// </summary>
    public class Channel
    {
        public string Title { get; internal set; }
        public int Id { get; internal set; }
        public int Code { get; internal set; }
        public string Lcn { get; internal set; }
        public bool IsRadio { get; internal set; }
        public bool IsFavourite { get; internal set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title"></param>
        /// <param name="id"></param>
        /// <param name="code"></param>
        /// <param name="lcn"></param>
        /// <param name="isRadio"></param>
        /// <param name="isFavourite"></param>
        internal Channel(
            string title,
            int id,
            int code,
            string lcn,
            bool isRadio,
            bool isFavourite)
        {
            Title = title;
            Id = id;
            Code = code;
            Lcn = lcn;
            IsRadio = isRadio;
            IsFavourite = isFavourite;
        }

        /// <summary>
        /// Constructor from deserialized JSON format
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="isRadio"></param>
        /// <param name="isFavourite"></param>
        internal Channel(
            JsonFormat.SkyChannelChannel channel,
            bool isRadio,
            bool isFavourite) : 
                this(channel.t,
                channel.c[0],
                channel.c[1],
                channel.lcn,
                isRadio,
                isFavourite)
        {
        }

        /// <summary>
        /// Preferred 
        /// </summary>
        public string Display
        {
            get
            {
                return String.Format("{0}",
                    Lcn ?? Title);
            }
        }
    }

    /// <summary>
    /// Representation of a single Programme in the EPG or Now and Next
    /// </summary>
    public class Programme
    {
        public string Title { get; internal set; }
        public string Description { get; internal set; }
        public DateTime StartTime { get; internal set; }
        public TimeSpan Duration { get; internal set; }

        internal Programme(JsonFormat.SkyListing listing)
        {
            Title = listing.t;
            Description = listing.d;

            StartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(listing.s);
            Duration = new TimeSpan(0, 0, (int)listing.m[1]);
        }
    }

    /// <summary>
    /// THe Now and next pair of programmes for a channel
    /// </summary>
    public class NowAndNext
    {
        public Programme Now { get; internal set; }
        public Programme Next { get; internal set; }
    }

    //  All access to the Sky STB is through these two SOAP web service names.
    //  The URLs for these services will have been discovered by SkyLocator and stored in the registry
    const string SkyBoxPlayServiceType = "urn:schemas-nds-com:service:SkyPlay:2";
    const string SkyBoxBrowseServiceType = "urn:schemas-nds-com:service:SkyBrowse:2";

    /// <summary>
    /// The URL for the SkyPlay service for the STB discovered on the LAN
    /// </summary>
    static string SkyBoxPlayServiceAddress = null;

    /// <summary>
    /// The URL for the SkyBrowse service for the STB discovered on the LAN
    /// </summary>
    static string SkyBoxBrowseServiceAddress = null;

    //  When communicating with the Sky STB, retry a few times on failure
    const int MaxNetworkAttempts = 5;
    const int NetworkAttemptInterval = 200;

    /// <summary>
    /// All recordings keyed by Id
    /// </summary>
    public Dictionary<string, Recording> AllRecordings;

    /// <summary>
    /// All recordings keyed by recording resource URL to find the currently playing recording
    /// </summary>
    private Dictionary<string, Recording> AllRecordingsByResource;

    /// <summary>
    /// The currently playing recording - null if watching live TV
    /// </summary>
    public Recording CurrentRecording { get; private set; }

    /// <summary>
    /// All recordings in reverse time order
    /// </summary>
    public IEnumerable<Recording> AllRecordingsInReverseTimeOrder
    {
        get { return AllRecordings.Values.OrderByDescending(r => r.WhenRecorded); }
    }

    /// <summary>
    /// The capacity of the Sky box for recordings (in bytes)
    /// </summary>
    public static Int64 Capacity { get; private set; }

    /// <summary>
    /// The total size of all the recordings
    /// </summary>
    public Int64 TotalSize
    {
        get
        {
            return AllRecordings.Values.Sum(r => r.Size);
        }
    }

    /// <summary>
    /// All recordings sharing the same title, most recent first
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    public IEnumerable<Recording> AllRecordingsForTitle(
        string title)
    {
        return AllRecordingsInReverseTimeOrder.Where(r => r.Title == title);
    }

    /// <summary>
    /// All recordings as a collection of Lists, grouped with those sharing a title in the same List
    /// </summary>
    public IEnumerable<List<Recording>> AllRecordingsGroupedByTitle
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
    /// A display string of the total size of the specified recording as a percentage of the total size 
    /// </summary>
    /// <param name="recording"></param>
    /// <returns></returns>
    public string SizePercent(
        Recording recording)
    {
        return FormatPercentage(recording.Size, Capacity != 0 ? Capacity : TotalSize);
    }

    /// <summary>
    /// A display string of the total size of the specified collection of recordings as a percentage of the total size
    /// </summary>
    /// <param name="recordings"></param>
    /// <returns></returns>
    public string SizePercent(
        IEnumerable<Recording> recordings)
    {
        return FormatPercentage(recordings.Sum(r => r.Size), Capacity != 0 ? Capacity : TotalSize);
    }

    /// <summary>
    /// Format a size as a percentage (to one decimal place) of the total
    /// </summary>
    /// <param name="thisSize"></param>
    /// <param name="totalSize"></param>
    /// <returns></returns>
    static string FormatPercentage(
        Int64 thisSize,
        Int64 totalSize)
    {
        //return String.Format("{0:0.0}% ({1:0.0}GB / {2:0.}GB)", (double)thisSize * 100 / (double)totalSize, (double)thisSize / (1024 * 1024 * 1024), (double)totalSize / (1024 * 1024 * 1024));
        return String.Format("{0:0.0}%", (double)thisSize * 100 / (double)totalSize);
    }

    /// <summary>
    /// A dictionary of transmitted Sky TV channels keyed by channel number
    /// </summary>
    public Dictionary<int, Channel> AllChannels { get; private set; }

    /// <summary>
    /// A dictionary of transmitted Sky Radio channels keyed by channel number
    /// </summary>
    public static Dictionary<int, Channel> RadioChannels { get; private set; }

    /// <summary>
    /// The current Live TV or Radio channel - null if watching a Recording
    /// </summary>
    public Channel CurrentChannel { get; private set; }

    /// <summary>
    /// The favourite channels, displayed at the start of ant lists
    /// </summary>
    static string[] FavoriteChannels = null;

    /// <summary>
    /// The collection of package codes to which we are subscribed
    /// </summary>
    static List<int> Packages { get; set; }

    /// <summary>
    /// The mode in which the Sky class is operating (Planner/Live/Radio)
    /// </summary>
    public string CurrentMode { get; set; }
    
    /// <summary>
    /// The singleton instance of the SkyData class
    /// </summary>
    static SkyData skyData;

    /// <summary>
    /// Initialize the singleton singleton instance of the SkyData class
    /// </summary>
    /// <remarks>
    /// IMPORTANT : call this first before any use of the "Sky" property.
    /// </remarks>
    /// <param name="favoriteChannels">Collection of favourite TV channel names</param>
    /// <param name="radioChannels">Radio channels as tuples {title, id, code}</param>
    /// <param name="packages">Collection of subscribed package codes</param>
    /// <param name="capacityGB">The capacity of the Sky box for recordings (in GB)</param>
    public static void Initialize(
        IEnumerable<string> favoriteChannels = null,
        IEnumerable<Tuple<string, int, int>> radioChannels = null,
        List<int> packages = null,
        int capacityGB = 0)
    {
        //  Get the discovered web service URLs
        Dictionary<string, string> services = new Dictionary<string, string>();
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Avid").OpenSubKey("Sky"))
        {
            foreach (string serviceType in key.GetValueNames())
            {
                services[serviceType] = key.GetValue(serviceType) as string;
            }
        }

        //  We will use SkyBoxPlayServiceType and SkyBoxBrowseServiceType of the five that may have been discovered
        if (!services.ContainsKey(SkyBoxPlayServiceType) || !services.ContainsKey(SkyBoxBrowseServiceType))
        {
            throw new Exception("Sky services cannot be found");
        }
        SkyBoxPlayServiceAddress = services[SkyBoxPlayServiceType];
        SkyBoxBrowseServiceAddress = services[SkyBoxBrowseServiceType];

        //  Store any provided favourite channels
        if (favoriteChannels != null)
        {
            FavoriteChannels = favoriteChannels.ToArray();
        }

        //  Store any provided radio channels
        RadioChannels = new Dictionary<int, Channel>();
        if (radioChannels != null)
        {
            foreach (var channel in radioChannels)
            {
                RadioChannels[channel.Item2] = new Channel(channel.Item1, channel.Item2, channel.Item3, channel.Item1, true, false);
            }
        }

        Packages = packages;

        Capacity = (Int64)capacityGB * 1024 * 1024 * 1024;
    }

    /// <summary>
    /// The singleton instance property of the SkyData class
    /// </summary>
    public static SkyData Sky { get { return LoadSky(); } }

    /// <summary>
    /// Load or return the singleton instance of the SkyData class
    /// </summary>
    /// <returns></returns>
    static SkyData LoadSky()
    {
        if (skyData == null)
        {
            if (SkyBoxBrowseServiceAddress == null || SkyBoxPlayServiceAddress == null)
            {
                throw new Exception("Sky class not initialized");
            }
            skyData = new SkyData();
        }

        return skyData;
    }
     
    /// <summary>
    /// Constructor for the singleton instance, loading recordings from the STB and channels from thw web
    /// </summary>
    SkyData()
    {
        LoadAllRecordings();
        LoadChannelMappings();
        logger.Info("Found {0} recordings totalling {1}MB of {2}MB [{3}]",
            AllRecordings.Count,
            TotalSize / 1048576,
            Capacity / 1048576,
            FormatPercentage(TotalSize, Capacity));
    }

    /// <summary>
    /// Load the collection of all recordings from the STB
    /// </summary>
    public void LoadAllRecordings()
    {
        //  If we are watching a recording, stop it playing
        if (CurrentRecording != null)
        {
            Stop();
            CurrentRecording = null;
        }

        List<XElement> recordingsList = new List<XElement>();

        try
        {
            XNamespace nsRoot = null;
            XNamespace nsDC = null;
            XNamespace nsVX = null;
            XNamespace nsUPNP = null;

            //  Load in batches of 25
            const int batchSize = 25;
            for (int index = 0; ; index += batchSize)
            {
                //  Construct the (reverse engineered) SOAP XML to be posted to the SkyBrowse web service
                string postData = String.Format(
@"<?xml version='1.0' encoding='utf-8' standalone='yes'?>
	<s:Envelope s:encodingStyle='http://schemas.xmlsoap.org/soap/encoding/' xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
	    <s:Body>
	        <u:Browse xmlns:u='urn:schemas-nds-com:service:SkyBrowse:2'>
	            <ObjectID>3</ObjectID>
	            <BrowseFlag>BrowseDirectChildren</BrowseFlag>
	            <Filter>*</Filter>
	            <StartingIndex>{0}</StartingIndex>
	            <RequestedCount>{1}</RequestedCount>
	            <SortCriteria></SortCriteria>
	        </u:Browse>
	    </s:Body>
	</s:Envelope>", index, batchSize);

                //  Post the XML to the SkyBrowse web service. This returns the requested data
                XElement responseData = SkyBrowse("urn:schemas-nds-com:service:SkyBrowse:2#Browse", postData);
                if (responseData == null)
                {
                    break;
                }
                responseData = responseData.Elements().First().Elements().First();

                //  The Result is a complex XML using four different namespaces
                XElement recordingsBatch = XDocument.Parse(responseData.Element("Result").Value).Root;

                nsRoot = recordingsBatch.GetDefaultNamespace();
                nsDC = recordingsBatch.GetNamespaceOfPrefix("dc");
                nsVX = recordingsBatch.GetNamespaceOfPrefix("vx");
                nsUPNP = recordingsBatch.GetNamespaceOfPrefix("upnp");

                //  Get the XElement representation of the recordings
                foreach (XElement item in recordingsBatch.Elements(nsRoot + "item"))
                {
                    recordingsList.Add(item);
                }

                //  If we got less than we expected in the batch, this is the end of the collection
                if (int.Parse(responseData.Element("NumberReturned").Value) != batchSize)
                {
                    break;
                }
            }

            //  Process all the Recording XML elements to construct a Dictionary of Recording objects
            AllRecordings = new Dictionary<string,Recording>();
            foreach (XElement recording in recordingsList)
            {
                try
                {
	                XElement recStatus = recording.Element(nsVX + "X_recStatus");
	                if (recStatus != null && Convert.ToInt32(recStatus.Value) >= 3 &&   //  Empirically determined
	                    GetStringValue(recording, nsVX + "X_serviceType") != "5")       //  I think "5" is scheduled recording
	                {
                        XElement res = recording.Element(nsRoot + "res");
                        Int64 size = Convert.ToInt64(res.Attribute("size").Value);

	                    DateTime whenRecorded = GetDateTimeValue(recording, nsUPNP + "scheduledStartTime", DateTime.MinValue);
	                    DateTime recordedActualStart = GetDateTimeValue(recording, nsUPNP + "recordedStartDateTime", whenRecorded);
	                    TimeSpan duration = GetTimeSpanValue(recording, nsUPNP + "scheduledDuration", new TimeSpan(0));
	                    TimeSpan recordedActualDuration = GetTimeSpanValue(recording, nsUPNP + "recordedDuration", duration);
	                    TimeSpan prePad = whenRecorded - recordedActualStart;
	                    TimeSpan postPad = (recordedActualDuration - duration).Subtract(prePad);
	                    TimeSpan lastViewed = new TimeSpan(GetInt64Value(recording, nsVX + "X_lastPlaybackPosition", 0) * 10000);
	                    string channelName = GetStringValue(recording, nsUPNP + "channelName");
	                    string recordingId = recording.Attribute("id").Value;
                        bool beingRecorded = recStatus.Attribute("recState").Value == "4";
	                    AllRecordings[recordingId] = new Recording(
	                        GetStringValue(recording, nsDC + "title"),
	                        recordingId,
	                        GetStringValue(recording, nsRoot + "res"),
	                        whenRecorded > recordedActualStart ? whenRecorded : recordedActualStart,
	                        duration < recordedActualDuration ? duration : recordedActualDuration,
	                        prePad.Ticks > 0 ? prePad : new TimeSpan(0),
	                        postPad.Ticks > 0 ? postPad : new TimeSpan(0),
	                        lastViewed,
	                        channelName,
	                        recStatus.Attribute("contentStatus").Value != "3" ? " [Partial]" : "",
	                        GetStringValue(recording, nsDC + "description"),
                            beingRecorded,
                            size);
	                }
                }
                catch
                {
                	
                }
            }

            AllRecordingsByResource = AllRecordings.Values.ToDictionary(r => r.Resource);
        }
        catch (Exception)
        {
            AllRecordings = new Dictionary<string, Recording>();
            AllRecordingsByResource = new Dictionary<string, Recording>();
        }
    }

    /// <summary>
    /// Get the text value of a named sub-element of XML if it exists
    /// </summary>
    /// <param name="recording"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private string GetStringValue(
        XElement recording,
        XName name)
    {
        XElement valueElement = recording.Element(name);
        return (valueElement == null) ? null : valueElement.Value;
    }

    /// <summary>
    /// Get the integer value of a named sub-element of XML if it exists
    /// </summary>
    /// <param name="recording"></param>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    private Int64 GetInt64Value(
        XElement recording,
        XName name,
        Int64 defaultValue)
    {
        string valueString = GetStringValue(recording, name);
        return (valueString == null) ? defaultValue : Int64.Parse(valueString);
    }

    /// <summary>
    /// Get the DateTime value of a named sub-element of XML if it exists
    /// </summary>
    /// <param name="recording"></param>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    private DateTime GetDateTimeValue(
        XElement recording,
        XName name,
        DateTime defaultValue)
    {
        string valueString = GetStringValue(recording, name);
        return (valueString == null) ? defaultValue : DateTime.Parse(valueString);
    }

    /// <summary>
    /// Get the TimeSpan value of a named sub-element of XML if it exists
    /// </summary>
    /// <param name="recording"></param>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    private TimeSpan GetTimeSpanValue(
        XElement recording,
        XName name,
        TimeSpan defaultValue)
    {
        string valueString = GetStringValue(recording, name);
        return (valueString == null || valueString.Length < 3) ? defaultValue : TimeSpan.Parse(valueString.Substring(3));
    }

    /// <summary>
    /// Load the collection of TV channels from a Sky public web service
    /// </summary>
    public void LoadChannelMappings()
    {
        //  Load the channels as JSON and deserialize
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        string json = new WebClient().DownloadString("http://tv.sky.com/channel/index");
        JsonFormat.SkyChannelInfo channels = serializer.Deserialize<JsonFormat.SkyChannelInfo>(json);

        List<JsonFormat.SkyChannelChannel> channelList = new List<JsonFormat.SkyChannelChannel>();

        //  Add all the channels we are interested in
        foreach (JsonFormat.SkyChannelChannel channel in channels.init.channels)
        {
            //  Empirical determined values for C[2] encoding the subscription pack:
            //      12 => Entertainment
            //      13 => Lifestyle
            //      14 => Movies
            //      15 => Sport
            //      16 => News
            //      17 => Documentary
            //      18 => Childrens
            //      19 => Music
            //      21 => Shopping
            //      22 => Religious
            //      23 => Asian
            //      24 => Gambling & Sex
            //      25 => Specialist
            if ((Packages != null && Packages.Contains(channel.c[2]) ||      //  Included in configured packages
                 channel.pt == null ||      //  Free to view
                 channel.pt == "F" ) &&     //  Free to air
                channel.c[1] < 900)         //  Empirically determined - c[1] >= 900 => Local regional
            {
                channelList.Add(channel);
            }
        }

        //  Build a list of channels with favourites first, then HD, then SD
        AllChannels = new Dictionary<int,Channel>();
        var favoriteChannels = FavoriteChannels;

        foreach (var favouriteChannel in favoriteChannels)
        {
            foreach (var channel in channelList)
            {
                if (channel.t == favouriteChannel || channel.lcn == favouriteChannel)
                {
                    AllChannels[channel.c[0]] = new Channel(channel, false, true);
                }
            }
        }

        foreach (var channel in channelList)
        {
            if (!favoriteChannels.Contains(channel.t) && channel.c[3] != 0) //  c[3] != 0 => HD
            {
                AllChannels[channel.c[0]] = new Channel(channel, false, false);
            }
        }

        foreach (var channel in channelList)
        {
            if (!favoriteChannels.Contains(channel.t) && channel.c[3] == 0) //  c[3] == 0 => SD
            {
                AllChannels[channel.c[0]] = new Channel(channel, false, false);
            }
        }

        //  What are we currently watching?
        GetCurrentChannelInfo();
    }

    /// <summary>
    /// What are we currently watching?
    /// </summary>
    public void GetCurrentChannelInfo()
    {
        //  Post the (reverse engineered) SOAP XML to the SkyPlay web service.
        //  This will return a "recource URL"
        string currentUrl = SkyPlay("CurrentURI", "urn:schemas-nds-com:service:SkyPlay:2#GetMediaInfo",
@"<s:Envelope s:encodingStyle='http://schemas.xmlsoap.org/soap/encoding/' xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
    <s:Body>
        <u:GetMediaInfo xmlns:u='urn:schemas-nds-com:service:SkyPlay:2'>
            <InstanceID>0</InstanceID>
        </u:GetMediaInfo>
    </s:Body>
</s:Envelope>", 1000);

        CurrentChannel = null;
        CurrentRecording = null;
        if (!String.IsNullOrEmpty(currentUrl))
        {
            //  Is it a live channel? If so, which?
            if (currentUrl.StartsWith("xsi://"))
            {
                int currentInternalChannelNumber = Convert.ToInt32(currentUrl.Remove(0, currentUrl.IndexOf("://") + 3), 16);
                CurrentChannel = GetChannelByInternalNumber(currentInternalChannelNumber);
            }
                //  Or is it a recording? If so, which?
            else if (currentUrl.StartsWith("file://pvr/"))
            {
                if (AllRecordingsByResource.ContainsKey(currentUrl))
                {
                    CurrentRecording = AllRecordingsByResource[currentUrl];
                }
            }
        }
    }

    /// <summary>
    /// Given a (shy internal) channel number, get the Channel
    /// </summary>
    /// <param name="internalChannelNumber"></param>
    /// <returns></returns>
    private Channel GetChannelByInternalNumber(
        int internalChannelNumber)
    {
        if (AllChannels.ContainsKey(internalChannelNumber))
        {
            return AllChannels[internalChannelNumber];
        }

        if (RadioChannels.ContainsKey(internalChannelNumber))
        {
            return RadioChannels[internalChannelNumber];
        }

        return null;
    }

    /// <summary>
    /// Get the Now and Next information from the web for the specified channel
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public NowAndNext GetNowAndNext(
        Channel channel)
    {
        try
        {
            //  Load the Now and Next information as JSON and deserialize
            JavaScriptSerializer serializer = new JavaScriptSerializer();
	        string json = new WebClient().DownloadString(string.Format("http://epgservices.sky.com/5.1.1/api/2.0/channel/json/{0}/now/nn/4", channel.Id));
	        JsonFormat.SkyProgrammeInfo programmes = serializer.Deserialize<JsonFormat.SkyProgrammeInfo>(json);
	        var listings = programmes.listings[channel.Id.ToString()];
	
	        NowAndNext nowAndNext = new NowAndNext();
	
	        if (listings.Length > 0)
	        {
	            nowAndNext.Now = new Programme(listings[0]);
	        }
	
	        if (listings.Length > 1)
	        {
	            nowAndNext.Next = new Programme(listings[1]);
	        }
	
	        return nowAndNext;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Post SOAP data to the SkyPlay service, returning the response data as a text value of 
    /// the first sub-element of name specified as resultValueReturned (if provided)
    /// </summary>
    /// <param name="resultValueReturned"></param>
    /// <param name="soapAction"></param>
    /// <param name="postData"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    private string SkyPlay(
        string resultValueReturned,
        string soapAction,
        string postData,
        int timeout)
    {
        lock (this)
        {
	        for (int i = 0; i < MaxNetworkAttempts; i++)
	        {
	            try
	            {
		            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

                    WebRequest request = WebRequest.Create(SkyBoxPlayServiceAddress);
		            ((HttpWebRequest)request).UserAgent = "SKY_skyplus";
		            request.Method = "POST";
		            request.ContentLength = postBytes.Length;
		            request.ContentType = "text/xml; charset=utf-8";
		            request.Headers.Add("SOAPACTION", "\"" + soapAction + "\"");
                    request.Timeout = timeout  + i*1000;    //  Increase the timeout each retry
                    if (i != 0)
                    {
                        ((HttpWebRequest)request).KeepAlive = false;
                    }
		
		            Stream dataStream = request.GetRequestStream();
		            dataStream.Write(postBytes, 0, postBytes.Length);
		            dataStream.Close();
		
		            using (WebResponse response = request.GetResponse())
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            XElement responseData = XDocument.Load(response.GetResponseStream()).Root;

                            if (resultValueReturned != null)
                            {
                                return responseData.Elements().First().Elements().First().Element(resultValueReturned).Value;
                            }
                        }
                    }
		
		            return i.ToString();
	            }
	            catch (Exception ex)
	            {
	                if (i < MaxNetworkAttempts-1)
	                {
	                    System.Threading.Thread.Sleep(NetworkAttemptInterval);
	                    continue;
	                }
                    logger.WarnException("SkyPlay Exception for " + soapAction, ex);
	                break;
	            }
	        }
        }

        return null;
    }

    /// <summary>
    /// Post SOAP data to the SkyBrowse service, returning the response data as XML
    /// </summary>
    /// <param name="soapAction"></param>
    /// <param name="postData"></param>
    /// <returns></returns>
    private XElement SkyBrowse(
        string soapAction,
        string postData)
    {
        for (int i = 0; i < MaxNetworkAttempts; i++)
        {
            try
            {
                byte[] postBytes = Encoding.UTF8.GetBytes(postData);

                WebRequest request = WebRequest.Create(SkyBoxBrowseServiceAddress);
                ((HttpWebRequest)request).UserAgent = "SKY_skyplus";
                request.Method = "POST";
                request.ContentLength = postBytes.Length;
                request.ContentType = "text/xml; charset=utf-8";
                request.Headers.Add("SOAPACTION", "\"" + soapAction + "\"");
                if (i != 0)
                {
                    ((HttpWebRequest)request).KeepAlive = false;
                }

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(postBytes, 0, postBytes.Length);
                dataStream.Close();

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        return XDocument.Load(responseStream).Root;
                    }
                }
            }
            catch (Exception ex)
            {
                if (i < MaxNetworkAttempts-1)
                {
                    System.Threading.Thread.Sleep(NetworkAttemptInterval);
                    continue;
                }
                logger.WarnException("SkyBrowse Exception for " + soapAction, ex);
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Watch the live channel on the specified number
    /// </summary>
    /// <param name="internalNumber"></param>
    public void ChangeChannel(
        int internalNumber)
    {
        if (internalNumber == 0)
        {
            internalNumber = AllChannels.First().Key;
        }

        //  Post to SkyPlay
        string postData = String.Format(
@"<?xml version='1.0' encoding='utf-8'?>
<s:Envelope s:encodingStyle='http://schemas.xmlsoap.org/soap/encoding/' xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
    <s:Body>
        <u:SetAVTransportURI xmlns:u='urn:schemas-nds-com:service:SkyPlay:2'>
            <InstanceID>0</InstanceID>
            <CurrentURI>xsi://{0:X}</CurrentURI>
            <CurrentURIMetaData>NOT_IMPLEMENTED</CurrentURIMetaData>
        </u:SetAVTransportURI>
    </s:Body>
</s:Envelope>", internalNumber);
        try
        {
            SkyPlay(null, "urn:schemas-nds-com:service:SkyPlay:2#SetAVTransportURI", postData, 1000);
            System.Threading.Thread.Sleep(1000);
            GetCurrentChannelInfo();
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Pause the current recording or live channel
    /// </summary>
    /// <returns></returns>
    public string Pause()
    {
        //  Post to SkyPlay
        return SkyPlay(null, "urn:schemas-nds-com:service:SkyPlay:2#Pause",
@"<?xml version='1.0' encoding='utf-8'?>
<s:Envelope s:encodingStyle='http://schemas.xmlsoap.org/soap/encoding/' xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
    <s:Body>
        <u:Pause xmlns:u='urn:schemas-nds-com:service:SkyPlay:2'> 
        <InstanceID>0</InstanceID> 
        </u:Pause> 
    </s:Body>
</s:Envelope>", 50);
    }

    /// <summary>
    /// Stop the current recording
    /// </summary>
    public void Stop()
    {
        //  Post to SkyPlay
        SkyPlay(null, "urn:schemas-nds-com:service:SkyPlay:2#Stop",
@"<?xml version='1.0' encoding='utf-8'?>
<s:Envelope s:encodingStyle='http://schemas.xmlsoap.org/soap/encoding/' xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
    <s:Body>
        <u:Stop xmlns:u='urn:schemas-nds-com:service:SkyPlay:2'> 
        <InstanceID>0</InstanceID> 
        </u:Stop> 
    </s:Body>
</s:Envelope>", 1000);

        if (CurrentRecording != null)
        {
            ChangeChannel(0);
        }
    }

    /// <summary>
    /// Play or resume the current recording. FF or Rewind is speed != 1
    /// </summary>
    /// <param name="speed">-30, -12, -6, -2, 1, 2, 6, 12, 30</param>
    /// <returns></returns>
    public string PlayAtSpeed(
        int speed)
    {
        //  Post to SkyPlay
        string postData = String.Format(
@"<?xml version='1.0' encoding='utf-8'?>
<s:Envelope s:encodingStyle='http://schemas.xmlsoap.org/soap/encoding/' xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
    <s:Body>
        <u:Play xmlns:u='urn:schemas-nds-com:service:SkyPlay:2'>
            <InstanceID>0</InstanceID>
            <Speed>{0}</Speed>
        </u:Play>
    </s:Body>
</s:Envelope>", speed);
        return SkyPlay(null, "urn:schemas-nds-com:service:SkyPlay:2#Play", postData, 50);
    }

    /// <summary>
    /// Watch the specified recording, starting at the specified number of minutes from the start of the recording
    /// </summary>
    /// <param name="recording"></param>
    /// <param name="startTimeMinutes"></param>
    public void PlayRecording(
        Recording recording,
        int startTimeMinutes)
    {
        //  Post to SkyPlay
        string postData = String.Format(
@"<?xml version='1.0' encoding='utf-8'?>
<s:Envelope s:encodingStyle='http://schemas.xmlsoap.org/soap/encoding/' xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
    <s:Body>
        <u:SetAVTransportURI xmlns:u='urn:schemas-nds-com:service:SkyPlay:2'>
            <InstanceID>0</InstanceID>
            <CurrentURI>{0}?position={1}&amp;speed=1</CurrentURI>
            <CurrentURIMetaData>NOT_IMPLEMENTED</CurrentURIMetaData>
        </u:SetAVTransportURI>
    </s:Body>
</s:Envelope>",
          recording.Resource,
          startTimeMinutes * 60000);
        SkyPlay(null, "urn:schemas-nds-com:service:SkyPlay:2#SetAVTransportURI", postData, 1000);
        CurrentRecording = recording;
    }

    /// <summary>
    /// Delete the specified recording
    /// </summary>
    /// <param name="recording"></param>
    public void DeleteRecording(
        Recording recording)
    {
        //  Post to SkyBrowse
        string postData = String.Format(
@"<?xml version='1.0' encoding='utf-8'?>
<s:Envelope s:encodingStyle='http://schemas.xmlsoap.org/soap/encoding/' xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
    <s:Body>
        <u:DestroyObject xmlns:u='urn:schemas-nds-com:service:SkyBrowse:2'>
            <ObjectID>{0}</ObjectID>
        </u:DestroyObject>
    </s:Body>
</s:Envelope>",
          recording.Id);
        SkyBrowse("urn:schemas-nds-com:service:SkyBrowse:2#DestroyObject", postData);

        LoadAllRecordings();
    }
}