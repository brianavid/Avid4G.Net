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

public class SkyData
{
    internal class JsonFormat
    {
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

    public class Channel
    {
        public string Title { get; internal set; }
        public int Id { get; internal set; }
        public int Code { get; internal set; }
        public string Lcn { get; internal set; }
        public bool IsRadio { get; internal set; }
        public bool IsFavourite { get; internal set; }

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

        public string Display
        {
            get
            {
                return String.Format("{0}",
                    Lcn ?? Title);
            }
        }
    }

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

    public class NowAndNext
    {
        public Programme Now { get; internal set; }
        public Programme Next { get; internal set; }
    }

    public class SkySsdpLocator
    {
        readonly IPAddress multicastAddress = IPAddress.Parse("239.255.255.250");
        const int multicastPort = 1900;
        const int unicastPort = 1901;
        const int searchTimeOutSeconds = 20;

        const string messageHeader = "M-SEARCH * HTTP/1.1";
        const string messageHost = "HOST: 239.255.255.250:1900";
        const string messageMan = "MAN: \"ssdp:discover\"";
        const string messageMx = "MX: 20";
        const string messageSt = "ST: ssdp:all";

        readonly byte[] broadcastMessage = Encoding.UTF8.GetBytes(
            string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{0}",
                          "\r\n",
                          messageHeader,
                          messageHost,
                          messageMan,
                          messageMx,
                          messageSt));

        HashSet<string> Devices = new HashSet<string>();

        IEnumerable<string> GetSkyLocations(string localIpAddress)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                IPAddress ipAddress = IPAddress.Parse(localIpAddress);
                socket.Bind(new IPEndPoint(/*IPAddress.Any*/ipAddress, unicastPort));
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastAddress, IPAddress.Any));

                var thd = new Thread(() => GetSocketResponse(socket));
                thd.Start();

                socket.SendTo(broadcastMessage, 0, broadcastMessage.Length, SocketFlags.None, new IPEndPoint(multicastAddress, multicastPort));

                for (int i = 0; i < searchTimeOutSeconds; i++)
                {
                    Thread.Sleep(1000);
                    lock (Devices)
                    {
                        if (Devices.Count == 2)
                        {
                            break;
                        }
                    }
                }

                socket.Close();
                return Devices;
            }
        }

        string GetLocation(string str)
        {
            if (str.StartsWith("HTTP/1.1 200 OK"))
            {
                var reader = new StringReader(str);
                var lines = new List<string>();
                for (; ; )
                {
                    var line = reader.ReadLine();
                    if (line == null) break;
                    if (line != "") lines.Add(line);
                }

                var server = lines.Where(lin => lin.ToLower().StartsWith("server:")).First();
                if (server.Contains(" SKY "))
                {
                    var location = lines.Where(lin => lin.ToLower().StartsWith("location:")).First();
                    return location.Substring("location:".Length).Trim();
                }
            }

            return "";
        }

        public void GetSocketResponse(Socket socket)
        {
            try
            {
                while (true)
                {
                    var response = new byte[8000];
                    EndPoint ep = new IPEndPoint(IPAddress.Any, multicastPort);
                    socket.ReceiveFrom(response, ref ep);
                    var str = Encoding.UTF8.GetString(response);

                    var location = GetLocation(str);
                    if (!string.IsNullOrEmpty(location))
                    {
                        lock (Devices)
                        {
                            Devices.Add(location);
                        }
                    }
                }
            }
            catch
            {
                //TODO handle exception for when connection closes
            }

        }

        public static Dictionary<string, string> GetSkyServices(string localIpAddress)
        {
            SkySsdpLocator locator = new SkySsdpLocator();
            IEnumerable<string> locations = locator.GetSkyLocations(localIpAddress);

            Dictionary<string, string> services = new Dictionary<string, string>();

            foreach (string location in locations)
            {
                int lastSlash = location.LastIndexOf("/");
                string host = location.Substring(0, lastSlash);

                HttpWebRequest request = WebRequest.Create(location) as HttpWebRequest;
                request.UserAgent = "SKY_skyplus";
                request.Accept = "text/xml";

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        var doc = XDocument.Load(responseStream);
                        var nsRoot = doc.Root.GetDefaultNamespace();
                        foreach (var service in doc.Descendants(nsRoot + "service"))
                        {
                            services[service.Element(nsRoot + "serviceType").Value] = host + service.Element(nsRoot + "controlURL").Value;
                        }
                    }
                }
            }

            return services;

        }
    }

    const string SkyBoxPlayServiceType = "urn:schemas-nds-com:service:SkyPlay:2";
    const string SkyBoxBrowseServiceType = "urn:schemas-nds-com:service:SkyBrowse:2";

    static string SkyBoxPlayServiceAddress = null;
    static string SkyBoxBrowseServiceAddress = null;
    const int MaxNetworkAttempts = 10;
    const int NetworkAttemptInterval = 200;

    XNamespace nsRoot;
    XNamespace nsDC;
    XNamespace nsVX;
    XNamespace nsUPNP;

    public Dictionary<string, Recording> AllRecordings;
    public Dictionary<string, Recording> AllRecordingsByResource;
    public Recording CurrentRecording { get; private set; }

    public IEnumerable<Recording> AllRecordingsInReverseTimeOrder
    {
        get { return AllRecordings.Values.OrderByDescending(r => r.WhenRecorded); }
    }

    public Int64 TotalSize
    {
        get
        {
            return AllRecordings.Values.Sum(r => r.Size);
        }
    }

    public IEnumerable<Recording> AllRecordingsForTitle(
        string title)
    {
        return AllRecordingsInReverseTimeOrder.Where(r => r.Title == title);
    }

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

    public string SizePercent(
        Recording recording)
    {
        return FormatPercentage(recording.Size, TotalSize);
    }

    public string SizePercent(
        IEnumerable<Recording> recordings)
    {
        return FormatPercentage(recordings.Sum(r => r.Size), TotalSize);
    }

    static string FormatPercentage(
        Int64 thisSize,
        Int64 totalSize)
    {
        //return String.Format("{0:0.0}% ({1:0.0}GB / {2:0.}GB)", (double)thisSize * 100 / (double)totalSize, (double)thisSize / (1024 * 1024 * 1024), (double)totalSize / (1024 * 1024 * 1024));
        return String.Format("{0:0.0}%", (double)thisSize * 100 / (double)totalSize);
    }

    public string CurrentMode { get; set; }

    //        SkyChannelInfo channels;
    public Dictionary<int, Channel> AllChannels { get; private set; }
    public Dictionary<int, Channel> RadioChannels { get; private set; }
    public Channel CurrentChannel { get; private set; }

    static string[] FavoriteChannels = null;
    
    static SkyData skyData;

    //  IMPORTANT : call this first before any use of the "Sky" property.
    public static void Initialize(
        string localIpAddress,
        IEnumerable<string> favoriteChannels = null)
    {
        Dictionary<string, string> services = SkySsdpLocator.GetSkyServices(localIpAddress);
        SkyBoxPlayServiceAddress = services[SkyBoxPlayServiceType];
        SkyBoxBrowseServiceAddress = services[SkyBoxBrowseServiceType];

        if (favoriteChannels != null)
        {
            FavoriteChannels = favoriteChannels.ToArray();
        }
    }

    public static SkyData Sky { get { return LoadSky(); } }

    public static SkyData LoadSky()
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
        
    SkyData()
    {
        LoadAllRecordings();
        LoadChannelMappings();
    }

    public void LoadAllRecordings()
    {
        if (CurrentRecording != null)
        {
            Stop();
            CurrentRecording = null;
        }

        List<XElement> recordingsList = new List<XElement>();

        const int batchSize = 25;

        try
        {
            for (int index = 0; ; index += 25)
            {
                string postData = String.Format(
@"<?xml version='1.0' encoding='utf-8' standalone='yes'?>
	<s:Envelope s:encodingStyle='http://schemas.xmlsoap.org/soap/encoding/' xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
	    <s:Body>
	        <u:Browse xmlns:u='urn:schemas-nds-com:service:SkyBrowse:2'>
	            <ObjectID>3</ObjectID>
	            <BrowseFlag>BrowseDirectChildren</BrowseFlag>
	            <Filter>*</Filter>
	            <StartingIndex>{0}</StartingIndex>
	            <RequestedCount>25</RequestedCount>
	            <SortCriteria></SortCriteria>
	        </u:Browse>
	    </s:Body>
	</s:Envelope>", index);

                XElement responseData = SkyBrowse("urn:schemas-nds-com:service:SkyBrowse:2#Browse", postData);
                if (responseData == null)
                {
                    break;
                }
                responseData = responseData.Elements().First().Elements().First();

                XElement recordingsBatch = XDocument.Parse(responseData.Element("Result").Value).Root;
                nsRoot = recordingsBatch.GetDefaultNamespace();
                nsDC = recordingsBatch.GetNamespaceOfPrefix("dc");
                nsVX = recordingsBatch.GetNamespaceOfPrefix("vx");
                nsUPNP = recordingsBatch.GetNamespaceOfPrefix("upnp");

                foreach (XElement item in recordingsBatch.Elements(nsRoot + "item"))
                {
                    recordingsList.Add(item);
                }

                if (int.Parse(responseData.Element("NumberReturned").Value) != batchSize)
                {
                    break;
                }
            }

            AllRecordings = new Dictionary<string,Recording>();
            foreach (XElement recording in recordingsList)
            {
                try
                {
	                XElement recStatus = recording.Element(nsVX + "X_recStatus");
	                if (recStatus != null && Convert.ToInt32(recStatus.Value) >= 3 &&
	                    GetStringValue(recording, nsVX + "X_serviceType") != "5")
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
        catch (System.Exception ex)
        {
            AllRecordings = new Dictionary<string, Recording>();
            AllRecordingsByResource = new Dictionary<string, Recording>();
        }
    }

    private string GetStringValue(
        XElement recording,
        XName name)
    {
        XElement valueElement = recording.Element(name);
        return (valueElement == null) ? null : valueElement.Value;
    }

    private Int64 GetInt64Value(
        XElement recording,
        XName name,
        Int64 defaultValue)
    {
        string valueString = GetStringValue(recording, name);
        return (valueString == null) ? defaultValue : Int64.Parse(valueString);
    }

    private DateTime GetDateTimeValue(
        XElement recording,
        XName name,
        DateTime defaultValue)
    {
        string valueString = GetStringValue(recording, name);
        return (valueString == null) ? defaultValue : DateTime.Parse(valueString);
    }

    private TimeSpan GetTimeSpanValue(
        XElement recording,
        XName name,
        TimeSpan defaultValue)
    {
        string valueString = GetStringValue(recording, name);
        return (valueString == null || valueString.Length < 3) ? defaultValue : TimeSpan.Parse(valueString.Substring(3));
    }

    void AddRadioChannel(
            string title,
            int id,
            int code)
    {
        RadioChannels[id] = new Channel(title, id, code, title, true, false);
    }

    public void LoadChannelMappings()
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        string json = new WebClient().DownloadString("http://tv.sky.com/channel/index");
        JsonFormat.SkyChannelInfo channels = serializer.Deserialize<JsonFormat.SkyChannelInfo>(json);

        List<JsonFormat.SkyChannelChannel> channelList = new List<JsonFormat.SkyChannelChannel>();

        foreach (JsonFormat.SkyChannelChannel channel in channels.init.channels)
        {
            if (channel.c[2] <= 12 && channel.c[1] < 900)
            {
                channelList.Add(channel);
            }
        }

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
            if (!favoriteChannels.Contains(channel.t) && channel.c[3] != 0)
            {
                AllChannels[channel.c[0]] = new Channel(channel, false, false);
            }
        }

        foreach (var channel in channelList)
        {
            if (!favoriteChannels.Contains(channel.t) && channel.c[3] == 0)
            {
                AllChannels[channel.c[0]] = new Channel(channel, false, false);
            }
        }

        RadioChannels = new Dictionary<int,Channel>();

        AddRadioChannel("BBC Radio 2", 0x840, 102);
        AddRadioChannel("BBC Radio 3", 0x841, 103);
        AddRadioChannel("BBC Radio 4", 0x842, 104);
        AddRadioChannel("BBC Radio 4 Extra", 0x850, 131);
        AddRadioChannel("Classic FM", 0xD8E, 106);
        AddRadioChannel("Planet Rock", 0xD93, 110);

        GetCurrentChannelInfo();
    }

    public void GetCurrentChannelInfo()
    {
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
            if (currentUrl.StartsWith("xsi://"))
            {
                int currentInternalChannelNumber = Convert.ToInt32(currentUrl.Remove(0, currentUrl.IndexOf("://") + 3), 16);
                CurrentChannel = GetChannelByInternalNumber(currentInternalChannelNumber);
            }
            else if (currentUrl.StartsWith("file://pvr/"))
            {
                if (AllRecordingsByResource.ContainsKey(currentUrl))
                {
                    CurrentRecording = AllRecordingsByResource[currentUrl];
                }
            }
        }
    }

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

    public NowAndNext GetNowAndNext(
        Channel channel)
    {
        try
        {
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
                    request.Timeout = timeout;
		
		            Stream dataStream = request.GetRequestStream();
		            dataStream.Write(postBytes, 0, postBytes.Length);
		            dataStream.Close();
		
		            WebResponse response = request.GetResponse();
	                using (Stream responseStream = response.GetResponseStream())
	                {
		                XElement responseData = XDocument.Load(response.GetResponseStream()).Root;
	
	                    if (resultValueReturned != null)
	                    {
	                        return responseData.Elements().First().Elements().First().Element(resultValueReturned).Value;
	                    }
	                }
		
		            return i.ToString();
	            }
	            catch
	            {
	                if (i < MaxNetworkAttempts-1)
	                {
	                    System.Threading.Thread.Sleep(NetworkAttemptInterval);
	                    continue;
	                }
	                break;
	            }
	        }
        }

        return null;
    }

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

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(postBytes, 0, postBytes.Length);
                dataStream.Close();

                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    return XDocument.Load(responseStream).Root;
                }
            }
            catch
            {
                if (i < MaxNetworkAttempts-1)
                {
                    System.Threading.Thread.Sleep(NetworkAttemptInterval);
                    continue;
                }
                break;
            }
        }

        return null;
    }


    public void ChangeChannel(
        int internalNumber)
    {
        if (internalNumber == 0)
        {
            internalNumber = AllChannels.First().Key;
        }

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
        catch (System.Exception ex)
        {
        }
    }

    public string Pause()
    {
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

    public void Stop()
    {
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

    public string PlayAtSpeed(
        int speed)
    {
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

    public void PlayRecording(
        Recording recording,
        int startTimeMinutes)
    {
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

    public void DeleteRecording(
        Recording recording)
    {
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