using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ApiModels
{
    /// <summary>
    /// خروجی سرویس تشخیص نماینده بر اساس توکن لینک ساب
    /// نماینده از بخش بعد از @ در نام اشتراک (ستون email جدول v2_user) پیدا می شود
    /// </summary>
    public class SubscriptionAgentModel
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>نام اشتراک (بخش قبل از $ و @ در ایمیل کاربر)</summary>
        [JsonProperty("subscriptionName")]
        public string SubscriptionName { get; set; }

        /// <summary>نام کاربری نماینده (بخش بعد از @ در ایمیل کاربر)</summary>
        [JsonProperty("agentUsername")]
        public string AgentUsername { get; set; }

        /// <summary>نام تجاری نماینده برای نمایش در برنامه</summary>
        [JsonProperty("businessTitle")]
        public string BusinessTitle { get; set; }

        /// <summary>توکن نماینده ؛ همان مقداری که در هدر Authorization اندپوینت های /User/* فرستاده می شود</summary>
        [JsonProperty("agentToken")]
        public string AgentToken { get; set; }

        /// <summary>آیدی ربات تلگرام نماینده (بدون @)</summary>
        [JsonProperty("botUsername")]
        public string BotUsername { get; set; }

        /// <summary>آیدی پشتیبانی نماینده (بدون @)</summary>
        [JsonProperty("supportUsername")]
        public string SupportUsername { get; set; }

        /// <summary>اگر false باشد فروش نماینده موقتاً متوقف است و نباید دکمه خرید نمایش داده شود</summary>
        [JsonProperty("sellEnabled")]
        public bool SellEnabled { get; set; }
    }
}
