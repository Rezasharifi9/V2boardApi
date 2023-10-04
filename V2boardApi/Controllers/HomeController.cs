using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace V2boardApi.Controllers
{
    public class HomeController : Controller
    {
        [Route("/index")]
        public ActionResult Index()
        {
            var Cookie = Request.Cookies["url"];
            if (Cookie != null)
            {
                return Redirect(Cookie.Value);
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}