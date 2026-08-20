using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    /// <summary>
    /// خروجی سرویس ثبت دستگاه. کلاینت با گرفتن این پاسخ ثبت را موفق تلقی می کند
    /// و دیگر در اجراهای بعدی درخواست را تکرار نمی کند.
    /// </summary>
    public class MobileDeviceViewModel
    {
        /// <summary>
        /// شناسه رکورد در جدول (tbMobileUsers.tbMu_ID) — با deviceId رشته ای درخواست فرق دارد.
        /// </summary>
        [JsonProperty("deviceId")]
        public int DeviceId { get; set; }

        /// <summary>نماینده ای که این دستگاه زیرمجموعه اوست</summary>
        [JsonProperty("agentUsername")]
        public string AgentUsername { get; set; }

        /// <summary>نام تجاری نماینده برای نمایش در برنامه</summary>
        [JsonProperty("businessName")]
        public string BusinessName { get; set; }

        /// <summary>true یعنی این deviceId قبلا ثبت شده بود و رکورد فقط به روز شد</summary>
        [JsonProperty("isExisting")]
        public bool IsExisting { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
