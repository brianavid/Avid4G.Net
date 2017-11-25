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
    class Program
    {
        static Logger logger = LogManager.GetLogger("Avid.Desktop");

        private static System.Timers.Timer securityPollTimer;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            logger.Info("Avid Desktop Started");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            try
            {
#if USE_SKY_STB
                //  The Desktop tray app is also responsible for discovering the Sky STB service locations and recording them in the registry
                //  This is simply a convenient place to do this work with the necessary access rights
                logger.Info("SkyLocator.GetSkyServices");
                SkyLocator.GetSkyServices(ConfigurationManager.AppSettings["IpAddress"], logger);
#endif

                //  Run a background thread to monitor any DVBViewer player through its COM interface
                DvbViewerMonitor.StartMonitoring();

                var config = new HttpSelfHostConfiguration("http://localhost:89");

                config.Routes.MapHttpRoute(
                    "API Default", "api/{controller}/{action}/{id}",
                    new { id = RouteParameter.Optional });


                // Create a timer with a one minute interval.
                securityPollTimer = new System.Timers.Timer(60000);
                // Hook up the Elapsed event for the timer. 
                securityPollTimer.Elapsed += OnSecurityPollTimerEvent;
                securityPollTimer.Enabled = true;

                using (HttpSelfHostServer server = new HttpSelfHostServer(config))
                {
                    server.OpenAsync().Wait();
                    var applicationContext = new CustomApplicationContext();
                    Application.Run(applicationContext);
                    logger.Info("Avid Desktop Exit");
                    DvbViewerMonitor.NothingToMonitor();
                    securityPollTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex);
                DvbViewerMonitor.NothingToMonitor();
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Fatal("Unhandled UI Exception: {0}", e.ExceptionObject);
        }

        /// <summary>
        /// Every minute, send a Security/Tick request to the web app, to check ant required changes to lighting etc
        /// </summary>
        /// <remarks>
        /// Setting the timer here makes it long lived and so survive any recycling of the wb app
        /// </remarks>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void OnSecurityPollTimerEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            //logger.Info("OnSecurityPollTimerEvent");
            Exception lastEx = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    HttpWebRequest request =
                        (HttpWebRequest)HttpWebRequest.Create("http://localhost:83/Security/Tick");
                    request.Method = WebRequestMethods.Http.Get;
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    System.Threading.Thread.Sleep(2000);
                }
            }

            //logger.Error(lastEx, "OnSecurityPollTimerEvent failed");
        }
    }
}
