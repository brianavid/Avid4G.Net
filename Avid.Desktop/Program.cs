using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.ServiceModel;
using System.Configuration;
using NLog;

namespace Avid.Desktop
{
    static class Program
    {
        static Uri baseAddress = new Uri("http://localhost:89/Avid/DesktopService");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Logger logger = LogManager.GetLogger("Desktop");
            logger.Info("Avid Desktop Started");

            try
            {
                //  The Desktop tray app is also responsible for discovering the Sky STB service locations and recording them in the registry
                //  This is simply a convenient place to do this
                logger.Info("SkyLocator.GetSkyServices");
                SkyLocator.GetSkyServices(ConfigurationManager.AppSettings["IpAddress"], logger);

                //  if (!SingleInstance.Start()) { return; }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                //  The desktop service is a self-hosted WCF service implementing IDesktopService
                ServiceHost selfHost = new ServiceHost(typeof(DesktopService), baseAddress);

                try
                {
                    selfHost.AddServiceEndpoint(typeof(IDesktopService), new WSHttpBinding(), "DesktopService");

                    logger.Info("ServiceHost.Open");
                    selfHost.Open();

                    //  Now the IDesktopService implementation is running start a tray UI which can be used to exit 
                    logger.Info("Running in tray");
                    var applicationContext = new CustomApplicationContext();
                    Application.Run(applicationContext);

                    // Close the ServiceHostBase to shutdown the service.
                    logger.Info("ServiceHost.Close");
                    selfHost.Close();
                }
                catch (CommunicationException ce)
                {
                    Trace.WriteLine(String.Format("CommunicationException : {0}", ce.ToString()));
                    selfHost.Abort();
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex);
            }
            //  SingleInstance.Stop();
        }
    }
}
