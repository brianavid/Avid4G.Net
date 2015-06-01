using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;
using System.Web.Http.SelfHost;
using NLog;
using System.Windows.Forms;
using DVBViewerServer;
using System.Net.Http;
using System.Threading;
using System.IO;
using System.Xml.Linq;


namespace Avid.Desktop
{
    internal class DvbViewerMonitor
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        static Thread monitorThread = null;

        static bool exited = false;

        /// <summary>
        /// The DVBViewer player COM interface
        /// </summary>
        static DVBViewerServer.IDVBViewer dvb = null;

        /// <summary>
        /// The last reported play state for DVBViewer player
        /// </summary>
        static string playState = "";

        /// <summary>
        /// Send data to the Avid IIS process via an HTTP GET or POST asynchronously
        /// </summary>
        /// <param name="url">The HTTP URL</param>
        /// <param name="data">If non-null, POST this data; Otherwise GET the URL</param>
        static void SendToAvid(string url, string data = null)
        {
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create("http://localhost:83/" + url);
            if (data != null)
            {
                request.Method = WebRequestMethods.Http.Post;
                using (StreamWriter requestWriter = new StreamWriter(request.GetRequestStream(), Encoding.UTF8))
                {
                    requestWriter.Write(data);
                    requestWriter.Close();
                }
            }

            //  Send the request asynchronously
            request.BeginGetResponse(new AsyncCallback(FinishWebRequest), request);
        }

        /// <summary>
        /// Completion routine for the asynchronous HTTP request
        /// </summary>
        /// <param name="result"></param>
        static void FinishWebRequest(IAsyncResult result)
        {
            // Get the response and ignore it
            HttpWebResponse response = (result.AsyncState as HttpWebRequest).EndGetResponse(result) as HttpWebResponse;
            response.Close();
        }

        /// <summary>
        /// Send the current DVBViewer player status to Avid as XML data
        /// </summary>
        static void SendStatusToAvid()
        {
            var remain = dvb.DataManager.Value["#TV.Timeshift.remain"] ?? "";
            var xStatus = new XElement("Status",
                new XAttribute("when", DateTime.Now.ToString()),
                new XElement("Channel",
                    new XAttribute("number", dvb.DataManager.Value["#channelnr"] ?? ""),
                    new XAttribute("name", dvb.DataManager.Value["#channelname"] ?? "")),
                new XElement("Now",
                    new XAttribute("title", dvb.DataManager.Value["#TV.Now.title"] ?? ""),
                    new XAttribute("description", dvb.DataManager.Value["#TV.Now.event"] ?? "")),
                new XElement("Next",
                    new XAttribute("title", dvb.DataManager.Value["#TV.Next.title"] ?? ""),
                    new XAttribute("description", dvb.DataManager.Value["#TV.Next.event"] ?? ""),
                    new XAttribute("start", dvb.DataManager.Value["#TV.Next.start"] ?? "")),
                new XElement("PlayState",
                    new XAttribute("state", playState)),
                dvb.IsTimeshift() && remain != "" && remain != "00:00:00" ?
                    new XElement("TimeShift",
                        new XAttribute("remain", remain)) :
                    null);

            SendToAvid("Tv/UpdateStatus", xStatus.ToString());
        }

        /// <summary>
        /// Event handler for DVBViewer player closing
        /// </summary>
        static void Events_OnDvbvClose()
        {
            logger.Info("DVBViewer closed");
            monitorTerminationEvent.Set();
        }

        /// <summary>
        /// Event handler for DVBViewer player changing channel
        /// </summary>
        static void Events_OnChannelChange(
            int channelNumber)
        {
            //logger.Info("Change Channel {0}", channelNumber);
            playState = "Playing";
            SendStatusToAvid();
        }

        /// <summary>
        /// Event handler for DVBViewer player changing playstate
        /// </summary>
        static void Events_OnPlaystateChange(
            TRendererTyp typ,
            TPlaystates state)
        {
            switch (state)
            {
                case TPlaystates.cpsPlay:
                    playState = "Playing";
                    break;
                case TPlaystates.cpsPause:
                    playState = "Paused";
                    break;
            }

            //logger.Info("OnPlaystateChange: {0}", state);
            SendStatusToAvid();
        }

        /// <summary>
        /// Event fired when the monitor must terminate monitoring the current DVBViewer
        /// </summary>
        static ManualResetEvent monitorTerminationEvent = new ManualResetEvent(false);

        /// <summary>
        /// Background thread to monitor DVBViewer instances
        /// </summary>
        static void DoDvbViewerMonitor()
        {
            while (!exited)
            {
                try
                {
                    //  Connect the the DVBViewer COM interface
                    dvb = (DVBViewerServer.DVBViewer)System.Runtime.InteropServices.Marshal.GetActiveObject("DVBViewerServer.DVBViewer");
                }
                catch (System.Exception ex)
                {
                    //logger.Error("Can't open running DVBViewerServer.DVBViewer: {0}", ex.Message);
                    dvb = null;
                }

                //  If there is a DVBViewer COM interface
                if (dvb != null)
                {
                    logger.Info("DVBViewer started");
                    var lastTimeShift = "";
                    var lastNextStartTime = "";
                    monitorTerminationEvent.Reset();
                    try
                    {
                        //  Set Event handlers
                        var OnChannelChangeEvent = new IDVBViewerEvents_OnChannelChangeEventHandler(Events_OnChannelChange);
                        var onDVBVCloseEvent = new IDVBViewerEvents_onDVBVCloseEventHandler(Events_OnDvbvClose);
                        dvb.Events.OnChannelChange += OnChannelChangeEvent;
                        dvb.Events.OnPlaystatechange += new IDVBViewerEvents_OnPlaystatechangeEventHandler(Events_OnPlaystateChange);
                        dvb.Events.onDVBVClose += onDVBVCloseEvent;

                        //  Repeatedly wait for termination or 1 second tick
                        while (!monitorTerminationEvent.WaitOne(1000))
                        {
                            //  If not terminated
                            try
                            {
                                //  Have any key values changed?
                                bool needSendToAvid = false;
                                var timeshift = dvb.DataManager.Value["#TV.Timeshift.remain"] ?? "";
                                if (timeshift != lastTimeShift)
                                {
                                    lastTimeShift = timeshift;
                                    needSendToAvid = true;
                                }
                                var nextStartTime = dvb.DataManager.Value["#TV.Next.start"] ?? "";
                                if (nextStartTime != lastNextStartTime)
                                {
                                    lastNextStartTime = nextStartTime;
                                    needSendToAvid = true;
                                }

                                //  If so, send the current state to Avid
                                if (needSendToAvid)
                                {
                                    SendStatusToAvid();
                                }
                            }
                            catch (System.Exception ex)
                            {
                                //logger.Error(String.Format("DVB COM error 1 : {0}", ex));
                                break;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        //logger.Error(String.Format("DVB COM error 2 : {0}", ex));
                    }

                    //  Terminated or DVBViewer not found
                    dvb = null;
                    break;
                }

                Thread.Sleep(1000);
            }

            monitorThread = null;
        }

        /// <summary>
        /// Run a background thread to monitor any DVBViewer player through its COM interface
        /// </summary>
        static internal void StartMonitoring()
        {
            exited = false;
            if (monitorThread == null)
            {
                monitorThread = new Thread(DoDvbViewerMonitor);
                monitorThread.Start();
            }
        }

        /// <summary>
        /// When exiting the current player program, DVB Monitoring must now be terminated
        /// </summary>
        static internal void NothingToMonitor()
        {
            exited = true;
            monitorTerminationEvent.Set();
        }
    }
}
