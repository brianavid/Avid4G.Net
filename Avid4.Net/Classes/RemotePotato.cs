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

/// <summary>
/// Summary description for RemotePotato
/// </summary>
public static class RemotePotato
{
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

    static public Dictionary<String, Recording> AllRecordings { get; private set; }

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

    public static void OutputHost(System.Web.HttpResponse Response)
    {
        Response.Write(Host);
    }

    public static void OutputUrl(System.Web.HttpResponse Response)
    {
        Response.Write(Url);
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
        return GetAllChannels().Where(ch => Convert.ToInt32(ch.Element("MCChannelNumber").Value) < 100);
    }

    static IEnumerable<XElement> GetAllFavouriteChannels()
    {
        return GetAllTvChannels().Where(ch => Convert.ToBoolean(ch.Element("IsFavorite").Value));
    }

    static IEnumerable<XElement> GetAllRadioChannels()
    {
        return GetAllChannels().Where(ch => Convert.ToInt32(ch.Element("MCChannelNumber").Value) >= 700);
    }

    public static void GenerateTvChannelDropdown(
        HttpResponse Response)
    {
        EnsureServiceRunning(false);

        Response.Write("<table style='width: 100%'><tr><td><select id='TvSelect' onchange='return OnTvChannelSelected()'>\n");
        Response.Write(String.Format("<option></option>\n"));
        try
        {
	        foreach (var channel in RemotePotato.GetAllTvChannels()
	                .OrderBy(ch => ch.Element("Callsign").Value))
	        {
	            Response.Write(String.Format("<option>{0}</option>\n", channel.Element("Callsign").Value));
	        }
        }
        catch (System.Exception ex)
        {
        	
        }
        Response.Write("</select></td></tr></table>\n");
    }

    public static void GenerateRadioChannelDropdown(
        HttpResponse Response)
    {
        EnsureServiceRunning(false);

        Response.Write("<table style='width: 100%'><tr><td><select id='RadioSelect' onchange='return OnRadioChannelSelected()'>\n");
        Response.Write(String.Format("<option></option>\n"));
        try
        {
	        foreach (var channel in RemotePotato.GetAllRadioChannels()
	                .OrderBy(ch => ch.Element("Callsign").Value))
	        {
	            Response.Write(String.Format("<option>{0}</option>\n", channel.Element("Callsign").Value));
	        }
        }
        catch (System.Exception ex)
        {
        	
        }
        Response.Write("</select></td></tr></table>\n");
    }

    public static void SelectTvChannelName(
        string channelName)
    {
        if (channelName == "BBC ONE HD")
        {
            SelectTvChannelNumber("101");   //  BBC ONE HD is faked up as it has no EPG
            return;
        }
        XElement channel = GetAllChannels().Where(ch => ch.Element("Callsign").Value == channelName).First();
        SelectTvChannelNumber(channel.Element("MCChannelNumber").Value);
    }

    public static void SelectTvChannelNumber(
        string channelNumber)
    {
        GetXml(Url + "sendremotekey/GotoLiveTV");
        foreach (var c in channelNumber)
        {
            GetXml(Url + "sendremotekey/Num" + c);
        }
        GetXml(Url + "sendremotekey/Enter");
    }

    public static void GenerateEpgChannelDropdown(
        HttpResponse Response,
        string selectedCallSign)
    {
        EnsureServiceRunning(false);

        Response.Write("<table style='width: 100%'><tr><td><select id='EpgChannelSelect' onchange='return RefreshEpgProgrammes()'>\n");
        Response.Write(String.Format("<option></option>\n"));
        try
        {
	        foreach (var channel in RemotePotato.GetAllFavouriteChannels()
	                .OrderBy(ch => ch.Element("Callsign").Value))
	        {
	            string callSign = channel.Element("Callsign").Value;
	            Response.Write(String.Format("<option {1}>{0}</option>\n",
	                callSign,
	                callSign == selectedCallSign ? "selected='yes'" : ""));
	        }
        }
        catch (System.Exception ex)
        {
        	
        }
        Response.Write("</select></td></tr></table>\n");
    }

    public static void GenerateProgrammeListing(
        HttpResponse Response,
        DateTime day,
        string channelName)
    {
        EnsureServiceRunning(false);

        if (schedule == null)
        {
            LoadSchedule();
        }

        DateTime nextDay = day + new TimeSpan(1, 0, 0, 0);
        if (!String.IsNullOrEmpty(channelName))
        {
            try
            {
	            XElement channel = GetAllChannels().Where(ch => ch.Element("Callsign").Value == channelName).First();
	            XDocument requestDoc = new XDocument(
	                new XElement("ArrayOfEPGRequest",
	                    new XElement("EPGRequest",
	                        new XElement("TVServiceID", channel.Element("UniqueId").Value),
	                        new XElement("StartTime", day.Ticks),
	                        new XElement("StopTime", nextDay.Ticks))));
	
	            XDocument programsDoc = GetXml(Url + "programmes/nodescription/byepgrequest", requestDoc);
	
	            if (programsDoc != null)
	            {
	                foreach (var programme in programsDoc.Element("ArrayOfTVProgramme").Elements("TVProgramme"))
	                {
	                    var startTime = new DateTime(Convert.ToInt64(programme.Element("StartTime").Value)).ToLocalTime();
	                    var programmeId = programme.Element("Id").Value;
	
	                    Response.Write(String.Format(
	                        "<div onclick=\"LinkTo('EPGProgramme.aspx?id={0}&start={1}&stop={2}&channel={3}&title={4}&series={5}')\" class='programtitle' " +
	                                "style=\"width: 100%;border-width: thin; border-color: #3a5069; border-top-style: none; border-bottom-style: solid; {6}\">",
	                        programmeId,
	                        programme.Element("StartTime").Value,
	                        programme.Element("StopTime").Value,
	                        programme.Element("ServiceID").Value,
	                        HttpUtility.UrlEncode(programme.Element("Title").Value),
	                        programme.Element("IsSeries").Value != "0" ? programme.Element("SeriesID").Value : "0",
	                        schedule.IsScheduled(programmeId) ? "background-color: #DDDDDD" : ""));
	
	                    Response.Write(String.Format("<div style=\"font-size: small;\">{0:HH:mm}</div>", startTime));
	
	                    Response.Write(String.Format("{0}", programme.Element("Title").Value));
	
	                    Response.Write("</div>");
	                }
	            }
            }
            catch (System.Exception ex)
            {
            	
            }
        }
    }

    public static void GenerateScheduledListing(
        HttpResponse Response)
    {
        EnsureServiceRunning(false);

        try
        {
	        if (schedule == null)
	        {
	            LoadSchedule();
	        }
	
	        foreach (var recording in schedule.Recordings
	            .OrderBy(rec => Convert.ToInt64(schedule.GetProgramme(rec).Element("StartTime").Value)))
	        {
	            var programme = schedule.GetProgramme(recording);
	            var startTime = new DateTime(Convert.ToInt64(programme.Element("StartTime").Value)).ToLocalTime();
	            XElement channel = GetAllChannels().Where(ch => ch.Element("UniqueId").Value == programme.Element("ServiceID").Value).First();
	            Response.Write(String.Format(
	                "<div onclick=\"LinkTo('EPGProgramme.aspx?id={0}&start={1}&stop={2}&channel={3}&title={4}&series={5}')\" class='programtitle' " +
	                        "style=\"width: 100%;border-width: thin; border-color: #3a5069; border-top-style: none; border-bottom-style: solid;\">",
	                programme.Element("Id").Value,
	                programme.Element("StartTime").Value,
	                programme.Element("StopTime").Value,
	                programme.Element("ServiceID").Value,
	                HttpUtility.UrlEncode(programme.Element("Title").Value),
	                recording.Element("SeriesID").Value));
	            Response.Write(String.Format("<div style=\"font-size: small;\"><table width='100%'><tr><td>{0:ddd dd HH:mm}</td><td align='right'>{1}</td></tr></table></div>",
	                startTime, channel.Element("Callsign").Value));
	
	            Response.Write(String.Format("{0}", recording.Element("Title").Value));
	
	            Response.Write("</div>");
	        }
        }
        catch (System.Exception ex)
        {
        	
        }
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

    public static void GenerateRecordingsListing(
        HttpResponse Response)
    {
        EnsureServiceRunning(false);

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
                    var startTime = new DateTime(Convert.ToInt64(recording.Element("StartTime").Value)).ToLocalTime();
                    Response.Write(String.Format(
                        "<div onclick=\"LinkTo('EPGRecording.aspx?id={0}&start={1}&stop={2}&callsign={3}&title={4}&episode={5}&path={6}')\" class='programtitle' " +
                                "style=\"width: 100%;border-width: thin; border-color: #3a5069; border-top-style: none; border-bottom-style: solid;\">\r\n",
                        recording.Element("Id").Value,
                        recording.Element("StartTime").Value,
                        recording.Element("StopTime").Value,
                        HttpUtility.UrlEncode(recording.Element("WTVCallsign").Value),
                        HttpUtility.UrlEncode(recording.Element("Title").Value),
                        HttpUtility.UrlEncode(recording.Element("EpisodeTitle").Value),
                        HttpUtility.UrlEncode(recording.Element("Filename").Value)));
                    Response.Write(String.Format("\t<div style=\"font-size: small;\"><table width='100%'><tr><td>{0:ddd dd MMM}</td><td align='right'>{1}</td></tr></table></div>\r\n",
                        startTime, recording.Element("WTVCallsign").Value));

                    Response.Write(String.Format("\t<div>{0}</div>\r\n", recording.Element("Title").Value));
                    Response.Write(String.Format("\t<div style=\"font-size: small\"><table width='100%'><tr><td align='right'>{0}</td></tr></table></div>\r\n",
                        recording.Element("EpisodeTitle").Value));

                    Response.Write("</div>\r\n");

                    recordedDescriptions[recording.Element("Id").Value] = recording.Element("Description").Value;
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

    public static bool IsScheduled(string programmeId)
    {
        return schedule.IsScheduled(programmeId);
    }

    public static bool IsSeries(string programmeId)
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

    public static void DeleteRecording(Recording recording)
    {
        string path = recording.Filename;
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            LoadAllRecordings();
        }
    }
}