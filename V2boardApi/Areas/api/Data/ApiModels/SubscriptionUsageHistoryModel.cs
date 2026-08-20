using Newtonsoft.Json;
using System.Collections.Generic;

namespace V2boardApi.Areas.api.Data.ApiModels
{
    /// <summary>
    /// خروجی سرویس تاریخچه مصرف ۳۰ روز گذشته بر اساس توکن لینک ساب
    /// </summary>
    public class SubscriptionUsageHistoryModel
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>نام اشتراک (بخش قبل از $ و @ در ایمیل کاربر)</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>شروع بازه به شمسی (yyyy/MM/dd)</summary>
        [JsonProperty("fromDate")]
        public string FromDate { get; set; }

        /// <summary>پایان بازه به شمسی (yyyy/MM/dd) — معمولاً امروز</summary>
        [JsonProperty("toDate")]
        public string ToDate { get; set; }

        /// <summary>مجموع دانلود بازه به گیگابایت</summary>
        [JsonProperty("totalDownloadGb")]
        public double TotalDownloadGb { get; set; }

        /// <summary>مجموع آپلود بازه به گیگابایت</summary>
        [JsonProperty("totalUploadGb")]
        public double TotalUploadGb { get; set; }

        /// <summary>مجموع مصرف بازه (دانلود + آپلود) به گیگابایت</summary>
        [JsonProperty("totalGb")]
        public double TotalGb { get; set; }

        /// <summary>مصرف روزانه؛ روزهای بدون ترافیک در خروجی نیستند. جدیدترین روز اول است</summary>
        [JsonProperty("items")]
        public List<SubscriptionUsageDayModel> Items { get; set; }
    }

    public class SubscriptionUsageDayModel
    {
        /// <summary>تاریخ روز به شمسی (yyyy/MM/dd)</summary>
        [JsonProperty("date")]
        public string Date { get; set; }

        /// <summary>دانلود آن روز به گیگابایت</summary>
        [JsonProperty("downloadGb")]
        public double DownloadGb { get; set; }

        /// <summary>آپلود آن روز به گیگابایت</summary>
        [JsonProperty("uploadGb")]
        public double UploadGb { get; set; }

        /// <summary>مجموع آن روز به گیگابایت</summary>
        [JsonProperty("totalGb")]
        public double TotalGb { get; set; }
    }
}
