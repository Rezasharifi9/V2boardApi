using Newtonsoft.Json.Linq;

namespace V2boardApi.Areas.api.Data.ApiModels
{
    /// <summary>
    /// ورودی سرویس ثبت خطای کلاینت (اپلیکیشن اندروید و سایر پلتفرم ها).
    /// نام فیلدها عمدا با camelCase سمت کلاینت است.
    /// </summary>
    public class ClientLogModel
    {
        /// <summary>
        /// سطح لاگ: Fatal / Error / Warn / Info / Debug / Trace.
        /// مقدار crash هم به Fatal نگاشت می شود. پیش فرض Error است.
        /// ستون NLog.Level حداکثر ۵ کاراکتر است.
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// تگ ماژول/صفحه برای رهگیری، مثل vpn-core یا http یا crash.
        /// در ستون actionName ذخیره می شود.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>متن کوتاه خطا. اگر خالی باشد و exception آمده باشد، یک پیام پیش فرض ساخته می شود.</summary>
        public string Message { get; set; }

        /// <summary>استک تریس یا متن exception سمت کلاینت</summary>
        public string Exception { get; set; }

        /// <summary>شناسه یکتای نصب، همان deviceId ثبت دستگاه</summary>
        public string DeviceId { get; set; }

        /// <summary>نام بسته اپ، مثل com.safenet.client — بخشی از نام Logger می شود</summary>
        public string PackageName { get; set; }

        public string AppVersion { get; set; }
        public int? VersionCode { get; set; }

        public string Manufacturer { get; set; }
        public string Model { get; set; }

        /// <summary>پلتفرم: android / ios / windows. پیش فرض android</summary>
        public string Device { get; set; }

        public string OsVersion { get; set; }
        public int? Sdk { get; set; }

        /// <summary>اکتیویتی / صفحه / کلاس محل خطا</summary>
        public string Screen { get; set; }

        /// <summary>داده اضافی آزاد (شیء JSON)</summary>
        public JToken Extra { get; set; }

        /// <summary>
        /// توکن نماینده. راه اصلی هدر Authorization است و این فیلد
        /// فقط برای کلاینت هایی است که نمی توانند هدر بفرستند.
        /// </summary>
        public string AgentToken { get; set; }
    }
}
