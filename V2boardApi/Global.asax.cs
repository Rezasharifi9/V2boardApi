using DataLayer.DomainModel;
using DataLayer.Repository;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Timers;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Http;
using System.Threading.Tasks;
using System.Web.Http.Results;
using Newtonsoft.Json;
using V2boardApi.Models.V2boardModel;
using V2boardApi.Tools;

namespace V2boardApi
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbUseages> RepositoryUseage { get; set; }
        private System.Timers.Timer Timer { get; set; }

        private System.Timers.Timer GetUseageTimer { get; set; }
        protected async void Application_Start()
        {

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            GlobalConfiguration.Configure(WebApiConfig.Register);


            //GetUseageTimer = new System.Timers.Timer();
            //GetUseageTimer.Elapsed += Timer_Elapsed;
            //GetUseageTimer.Interval = 86400000;
            //GetUseageTimer.Start();
            //var r = await GetUseages(null, null);
        }


    }
}
