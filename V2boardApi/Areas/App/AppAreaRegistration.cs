using System.Web.Mvc;
using System.Web.Routing;

namespace V2boardApi.Areas.App
{
    public class AppAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "App";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "App_default",
                "App/{controller}/{action}/{id}",
                new { Controller = "Dashboard", action = "Index", id = UrlParameter.Optional }
            );

            context.MapRoute(
            name: "Bot",
            url: "App/Bot/{action}/{botName}",
            defaults: new { controller = "Bot", action = "Update" }
        );
        }
    }
}