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

namespace Avid.Desktop
{
    class Program
    {
        static Logger logger = LogManager.GetLogger("Avid.Desktop");

        static bool exited = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            logger.Info("Avid Desktop Started");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Thread monitorThread;
            try
            {
                monitorThread = new Thread(DoDvbViewerMonitor);
                monitorThread.Start();

                var config = new HttpSelfHostConfiguration("http://localhost:89");

                config.Routes.MapHttpRoute(
                    "API Default", "api/{controller}/{action}/{id}",
                    new { id = RouteParameter.Optional });

                using (HttpSelfHostServer server = new HttpSelfHostServer(config))
                {
                    server.OpenAsync().Wait();
                    var applicationContext = new CustomApplicationContext();
                    Application.Run(applicationContext);
                    logger.Info("Avid Desktop Exit");
                    exited = true;
                    monitorTerminationEvent.Set();
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex);
            }
            catch
            {
                logger.Fatal("Non-.Net Exception");
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Fatal("Unhandled UI Exception: {0}", e.ExceptionObject);
        }

        static void SendToAvid(string url)
        {
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create("http://localhost:83/" + url);
            // Get the response.
            WebResponse response = request.GetResponse();
            response.Close();
        }

        static void Events_OnDvbvClose()
        {
            logger.Info("DVBViewer closed");
            monitorTerminationEvent.Set();
        }

        static void Events_OnChannelChange(
            int channelNumber)
        {
            logger.Info("Change Channel {0}", channelNumber);
            SendToAvid(string.Format("Tv/ChangeChannelNumber?channelNumber={0}", channelNumber));
        }

        static ManualResetEvent monitorTerminationEvent = new ManualResetEvent(false);

        static void DoDvbViewerMonitor()
        {
            while (!exited)
            {
                DVBViewerServer.IDVBViewer dvb;
                try
                {
                    dvb = (DVBViewerServer.DVBViewer)System.Runtime.InteropServices.Marshal.GetActiveObject("DVBViewerServer.DVBViewer");
                }
                catch (System.Exception ex)
                {
                    //logger.Error("Can't open running DVBViewerServer.DVBViewer: {0}", ex.Message);
                    dvb = null;
                }
                if (dvb != null)
                {
                    logger.Info("DVBViewer started");
                    monitorTerminationEvent.Reset();
                    try
                    {
                        var OnChannelChangeEvent = new IDVBViewerEvents_OnChannelChangeEventHandler(Events_OnChannelChange);
                        var onDVBVCloseEvent = new IDVBViewerEvents_onDVBVCloseEventHandler(Events_OnDvbvClose);
                        dvb.Events.OnChannelChange += OnChannelChangeEvent;
                        dvb.Events.onDVBVClose += onDVBVCloseEvent;
                    }
                    catch (System.Exception ex)
                    {

                    }
                    monitorTerminationEvent.WaitOne();
                }

                Thread.Sleep(5000);
            }
        }
    }
}
