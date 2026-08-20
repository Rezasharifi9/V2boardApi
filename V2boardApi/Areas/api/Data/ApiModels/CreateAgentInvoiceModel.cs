namespace V2boardApi.Areas.api.Data.ApiModels
{
    /// <summary>
    /// ورودی سرویس ساخت فاکتور پرداخت مستقیم نماینده
    /// </summary>
    public class CreateAgentInvoiceModel
    {
        /// <summary>شناسه تعرفه در جدول tbLinkUserAndPlans (ستون Link_PU_ID)</summary>
        public int PlanId { get; set; }

        /// <summary>
        /// توکن اشتراکی که باید تمدید شود (ستون tbL_Token در جدول tbLinks).
        /// خالی بودن این مقدار یعنی یک اشتراک جدید ساخته شود.
        /// </summary>
        public string SubscriptionToken { get; set; }

        /// <summary>
        /// شناسه دستگاهی که فاکتور را می سازد ، همان deviceId که با MobileDevice/Register ثبت شده است.
        /// اگر ارسال شود ، فاکتور و سفارش به رکورد دستگاه در tbMobileUsers متصل می شوند و
        /// در بخش «کاربران موبایل» پنل قابل ردیابی خواهند بود.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>نام قدیمی deviceId. هنوز خوانده می شود تا کلاینت اندروید فعلی نشکند.</summary>
        public string AndroidId { get; set; }

        /// <summary>deviceId و در نبودش androidId.</summary>
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

            return null;
        }
    }
}
