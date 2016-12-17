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

                using (HttpSelfHostServer server = new HttpSelfHostServer(config))
                {
                    server.OpenAsync().Wait();
                    var applicationContext = new CustomApplicationContext();
                    Application.Run(applicationContext);
                    logger.Info("Avid Desktop Exit");
                    DvbViewerMonitor.NothingToMonitor();
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

    }
}
