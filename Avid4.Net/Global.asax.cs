using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using NLog;

namespace Avid4.Net
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        Logger logger = LogManager.GetLogger("AvidApplication");


        protected void Application_Start()
        {
            logger.Info("Avid 4 Started");
            AreaRegistration.RegisterAllAreas();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            JRMC.LoadAndIndexAllAlbums(new string[] { "1", "2" }, DateTime.Now.Hour < 5);   //  Reload album data from JRMC when restarting between midnight and five (i.e. in the overnight restart)
            DesktopClient.Initialize();
            Receiver.Initialize();
            Running.Initialize();
            Spotify.Initialize();
            DvbViewer.Initialize();
            Security.Initialize();

#if USE_SKY_STB
            SkyData.Initialize(Config.SkyFavourites, Config.SkyRadio, Config.SkyPackages, Config.SkyCapacityGB);
            var sky = SkyData.Sky;
#endif
        }

        protected void Application_Error()
        {
            Exception lastException = Server.GetLastError();
            logger.Fatal(lastException);
        }
    }
}