using System.Web.Http;
using System.Web.Mvc;
using System.Web.Http.Cors;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace V2boardApi.Areas.api
{
    public class apiAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "api";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "api_default",
                "api/v1/{controller}/{action}/{id}",
                new { id = UrlParameter.Optional }
            );

            GlobalConfiguration.Configure(WebApiConfig.Register);

        }

    }

    public class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // فعال‌سازی CORS
            config.EnableCors();

            // فعال‌سازی مسیردهی براساس Attribute
            config.MapHttpAttributeRoutes();

            // مسیردهی کلاسیک Web API
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // سایر تنظیمات...
        }
    }
}