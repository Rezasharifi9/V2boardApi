using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ApiModels
{
    /// <summary>
    /// خروجی سرویس اطلاعات اشتراک بر اساس توکن لینک ساب
    /// </summary>
    public class SubscriptionTokenInfoModel
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>نام اشتراک (بخش قبل از $ و @ در ایمیل کاربر)</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>حجم کل اشتراک به گیگابایت</summary>
        [JsonProperty("totalVolumeGb")]
        public double TotalVolumeGb { get; set; }

        /// <summary>حجم مصرف شده (آپلود + دانلود) به گیگابایت</summary>
        [JsonProperty("usedVolumeGb")]
        public double UsedVolumeGb { get; set; }

        /// <summary>مدت زمان باقی مانده به روز. صفر یعنی منقضی شده و 1- یعنی نامحدود</summary>
        [JsonProperty("remainingDays")]
        public int RemainingDays { get; set; }

        /// <summary>تاریخ انقضا به شمسی (yyyy/MM/dd). در اشتراک نامحدود null است</summary>
        [JsonProperty("expireDate")]
        public string ExpireDate { get; set; }
    }
}
