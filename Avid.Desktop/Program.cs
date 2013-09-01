using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.ServiceModel;
using System.Configuration;

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
            SkyLocator.GetSkyServices(ConfigurationManager.AppSettings["IpAddress"]);

            //  if (!SingleInstance.Start()) { return; }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                ServiceHost selfHost = new ServiceHost(typeof(DesktopService), baseAddress);

                try
                {
                    selfHost.AddServiceEndpoint(typeof(IDesktopService), new WSHttpBinding(), "DesktopService");

                    selfHost.Open();

                    var applicationContext = new CustomApplicationContext();
                    Application.Run(applicationContext);

                    // Close the ServiceHostBase to shutdown the service.
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
                Trace.WriteLine(String.Format("Exception : {0}", ex.ToString()));
            }
            //  SingleInstance.Stop();
        }
    }
}
