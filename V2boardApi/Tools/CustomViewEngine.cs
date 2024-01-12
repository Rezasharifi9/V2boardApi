using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace V2boardApi.Tools
{
    public class CustomRazorViewEngine : RazorViewEngine
    {
        public CustomRazorViewEngine()
        {
            // مسیرهای جدید برای ویوهای کنترلرهای اصلی
            ViewLocationFormats = new string[]
            {
            "~/Areas/App/Views/{1}/{0}.cshtml",
            "~/Areas/App/Views/Shared/{0}.cshtml",
            // اضافه کردن مسیر سفارشی
            "~/Areas/App/Views/{1}/{0}.cshtml"
            };

            // مسیرهای جدید برای ویوهای اشتراکی
            PartialViewLocationFormats = new string[]
            {
            "~/Areas/App/Views/{1}/{0}.cshtml",
            "~/Areas/App/Views/Shared/{0}.cshtml",
            // اضافه کردن مسیر سفارشی
            "~/Areas/App/Views/Shared/{0}.cshtml"
            };

            // مسیرهای جدید برای ویوهای _Layout و _ViewStart
            MasterLocationFormats = new string[]
            {
            "~/Areas/App/Views/{1}/{0}.cshtml",
            "~/Areas/App/Views/Shared/{0}.cshtml",
            // اضافه کردن مسیر Layout سفارشی
            "~/Areas/App/Views/Shared/{0}.cshtml"
            };
        }
    }
}