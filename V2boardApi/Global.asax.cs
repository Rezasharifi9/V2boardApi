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
using V2boardApi.Areas.api;

namespace V2boardApi
{
    public class MvcApplication : System.Web.HttpApplication
    {
        // بدون نگه داشتن ارجاع، GC شیء و تایمرهای داخلش را جمع می کند و تایمرها بی صدا از کار می افتند
        private static TimerService _backgroundTimers;

        protected void Application_Start()
        {

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            ViewEngines.Engines.Add(new CustomRazorViewEngine());
            Repository<tbUsers> Rep = new Repository<tbUsers>();
            var Res = Rep.Where(p => p.tbBotSettings.Count > 0 && p.tbBotSettings.Where(s => s.Bot_Token != null).Any()).ToList();
            foreach (var item in Res)
            {
                BotManager.AddBot(item.Username, item.tbBotSettings.First().Bot_Token);
            }

            var User = Res.FirstOrDefault();

            if (User != null && User.tbServers != null)
            {
                ServerCacheHelper.Set(User.tbServers);
            }
            else
            {
                // اگر استارت با دیتابیس همزمان نشود، Get در اولین درخواست دوباره از DB پر می‌کند
                ServerCacheHelper.Get();
            }

            // در محیط توسعه می توان با EnableBackgroundTimers=false در Web.config
            // تایمرها را خاموش کرد تا به سرورهای ریموت وصل نشوند
            if (BackgroundTimersEnabled())
                _backgroundTimers = new TimerService();

            StimulsoftBootstrap.Initialize();
        }

        private static bool BackgroundTimersEnabled()
        {
            var setting = System.Configuration.ConfigurationManager.AppSettings["EnableBackgroundTimers"];
            bool enabled;
            return string.IsNullOrWhiteSpace(setting) || !bool.TryParse(setting, out enabled) || enabled;
        }

        void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();

            // بررسی اینکه آیا استثنای رخ داده یک HttpException است
            if (exception is HttpException httpException)
            {
                int httpCode = httpException.GetHttpCode();

                if(Response.IsClientConnected && !Response.IsRequestBeingRedirected)
                {
                    // اگر کد خطای HTTP برابر 404 باشد
                    if (httpCode == 404)
                    {
                        Server.ClearError();
                        Response.Redirect("~/App/Error/Error404"); // انتقال به اکشن Error404
                    }
                    else if (httpCode == 401)
                    {
                        // برای سایر خطاها
                        Server.ClearError();
                        Response.Redirect("~/App/Error/Error401"); // انتقال به اکشن Error401
                    }
                    else if (httpCode == 500)
                    {
                        // برای سایر خطاها
                        Server.ClearError();
                        Response.Redirect("~/App/Error/Error500"); // انتقال به اکشن Error500
                    }
                }
                
            }
            else
            {
                // برای سایر خطاها
                Server.ClearError();
                Response.Redirect("~/Home/Error"); // انتقال به اکشن Error در کنترلر Home
            }
        }
    }
}
