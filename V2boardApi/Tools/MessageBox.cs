using DeviceDetectorNET.Class.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace V2boardApi.Tools
{
    public static class MessageBox
    {
        public static JsonResult Success(string Title, string Text, ButtonText ConfirmButtonText = ButtonText.باشه, icon icon = icon.success)
        {
            // ساختار داده‌های JSON
            var data = new
            {
                title = Title,
                text = Text,
                confirmButtonText = ConfirmButtonText.ToString(),
                icon = icon.ToString(),
                customClass = new
                {
                    confirmButton = "btn btn-primary waves-effect waves-light"
                },
                buttonsStyling = false
            };

            // تبدیل داده‌های JSON به رشته جاوااسکریپت
            string script = $"Swal.fire({Json.Encode(data)});";
            var Js = new JsonResult();
            Js.Data = new { status = "success", data = script };
            Js.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return Js;
        }

        public static JsonResult Warning(string Title, string Text, ButtonText ConfirmButtonText = ButtonText.باشه, icon icon = icon.warning)
        {
            // ساختار داده‌های JSON
            var data = new
            {
                title = Title,
                text = Text,
                confirmButtonText = ConfirmButtonText.ToString(),
                icon = icon.ToString(),
                customClass = new
                {
                    confirmButton = "btn btn-primary waves-effect waves-light"
                },
                buttonsStyling = false
            };

            // تبدیل داده‌های JSON به رشته جاوااسکریپت
            string script = $"Swal.fire({Json.Encode(data)});";
            var Js = new JsonResult();
            Js.Data = new { status = "warning", data = script };
            Js.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return Js;
        }


        public static JsonResult Error(string Title, string Text, ButtonText ConfirmButtonText = ButtonText.باشه, icon icon = icon.error)
        {
            // ساختار داده‌های JSON
            var data = new
            {
                title = Title,
                text = Text,
                confirmButtonText = ConfirmButtonText.ToString(),
                icon = icon.ToString(),
                customClass = new
                {
                    confirmButton = "btn btn-primary waves-effect waves-light"
                },
                buttonsStyling = false
            };

            // تبدیل داده‌های JSON به رشته جاوااسکریپت
            string script = $"Swal.fire({Json.Encode(data)});";
            var Js = new JsonResult();
            Js.Data = new { status = "error", data = script };
            Js.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return Js;
        }
    }
    public enum icon
    {
        success,
        warning,
        error,
        info
    }

    public enum ButtonText
    {
        باشه
    }

    public static class Toaster
    {

        public static JsonResult Success(string Title, string Text, text_icons icon = text_icons.success)
        {
            return CreateToast(Title, Text, icon);
        }

        public static JsonResult Warning(string Title, string Text, text_icons icon = text_icons.warning)
        {
            return CreateToast(Title, Text, icon);
        }

        public static JsonResult Error(string Title, string Text, text_icons icon = text_icons.danger)
        {
            return CreateToast(Title, Text, icon);
        }

        private static JsonResult CreateToast(string Title, string Text, text_icons icon)
        {
            string script = $"showToast('"+ Title + "','"+ Text + "','"+ "text-" + icon.ToString().ToLower() + "');";
            var Js = new JsonResult
            {
                Data = new { status = "success", data = script },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };

            return Js;
        }

        public enum text_icons
        {
            success,
            warning,
            danger,
            info
        }

    }
}



