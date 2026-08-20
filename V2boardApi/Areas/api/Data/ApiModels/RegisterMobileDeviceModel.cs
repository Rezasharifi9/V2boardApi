namespace V2boardApi.Areas.api.Data.ApiModels
{
    /// <summary>
    /// ورودی سرویس ثبت دستگاه در اولین اجرای اپلیکیشن.
    /// نام فیلدها عمدا با همان camelCase سمت کلاینت است تا نگاشت JSON بدون تنظیم اضافه انجام شود.
    /// </summary>
    public class RegisterMobileDeviceModel
    {
        /// <summary>
        /// شناسه یکتای نصب — مستقل از سیستم عامل.
        /// Android: ANDROID_ID ، iOS: identifierForVendor ، Windows: GUID محلی اپ ، Flutter: همان مقدار پایدار پلتفرم.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>نام قدیمی deviceId. هنوز خوانده می شود تا کلاینت اندروید فعلی نشکند.</summary>
        public string AndroidId { get; set; }

        /// <summary>توکن FCM / APNs / WNS برای ارسال Push Notification</summary>
        public string FirebaseToken { get; set; }

        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Device { get; set; }
        public string Product { get; set; }
        public string Hardware { get; set; }

        /// <summary>نسخه سیستم عامل به صورت رشته ، مثل 15 یا 18.5 یا 10.0.26100</summary>
        public string OsVersion { get; set; }

        /// <summary>نام قدیمی osVersion. هنوز خوانده می شود تا کلاینت اندروید فعلی نشکند.</summary>
        public string AndroidVersion { get; set; }

        /// <summary>سطح API / عدد بیلد ، مثل 35 روی اندروید</summary>
        public int? Sdk { get; set; }

        public string AppVersion { get; set; }
        public int? VersionCode { get; set; }
        public string PackageName { get; set; }

        public string Language { get; set; }
        public string Country { get; set; }

        /// <summary>منطقه زمانی دستگاه ، برای ارسال نوتیفیکیشن در ساعت درست</summary>
        public string Timezone { get; set; }

        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        public int? Density { get; set; }

        /// <summary>اجازه نمایش نوتیفیکیشن در دستگاه</summary>
        public bool? NotificationEnabled { get; set; }

        public bool? Rooted { get; set; }

        /// <summary>
        /// توکن نماینده. راه اصلی ارسال توکن هدر Authorization است و این فیلد
        /// فقط برای کلاینت هایی است که نمی توانند هدر بفرستند.
        /// </summary>
        public string AgentToken { get; set; }

        /// <summary>deviceId و در نبودش androidId. خالی یعنی هیچ کدام نیامده.</summary>
        public string ResolveDeviceId()
        {
            if (!string.IsNullOrWhiteSpace(DeviceId))
            {
                return DeviceId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(AndroidId))
            {
                return AndroidId.Trim();
            }

            return "";
        }

        /// <summary>osVersion و در نبودش androidVersion.</summary>
        public string ResolveOsVersion()
        {
            if (!string.IsNullOrWhiteSpace(OsVersion))
            {
                return OsVersion;
            }

            return AndroidVersion;
        }
    }
}
