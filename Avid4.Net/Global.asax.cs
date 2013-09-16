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

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            JRMC.LoadAndIndexAllAlbums(new string[] { "1", "2" }, false);
            Receiver.Initialize();
            Running.Initialize();
            SkyData.Initialize(Config.IpAddress, Config.SkyFavourites);
            Spotify.Initialize();

            var sky = SkyData.Sky;
        }

        protected void Application_Error()
        {
            Exception lastException = Server.GetLastError();
            logger.Fatal(lastException);
        }
    }
}