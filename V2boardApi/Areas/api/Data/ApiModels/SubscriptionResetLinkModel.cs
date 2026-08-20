using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ApiModels
{
    /// <summary>
    /// خروجی سرویس تغییر لینک اشتراک — معادل گزینه «تغییر لینک» در ربات
    /// </summary>
    public class SubscriptionResetLinkModel
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>توکن جدید لینک ساب. لینک قبلی از این لحظه قطع است</summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>لینک کامل اشتراک با توکن جدید</summary>
        [JsonProperty("subscriptionLink")]
        public string SubscriptionLink { get; set; }

        /// <summary>لینک پشتیبان با توکن جدید؛ اگر BackupSubAddr روی سرور نباشد null است</summary>
        [JsonProperty("backupSubscriptionLink")]
        public string BackupSubscriptionLink { get; set; }
    }
}
