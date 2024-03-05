using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace V2boardApi.Areas.App.Controllers
{
    public class ErrorController : Controller
    {
        // GET: App/Error
        public ActionResult Error404()
        {
            return View();
        }
    }
}