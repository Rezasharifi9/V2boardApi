using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    /// <summary>
    /// خروجی سرویس موجودی کیف پول ربات تلگرام مشتری
    /// </summary>
    public class TelegramWalletViewModel
    {
        /// <summary>موجودی کیف پول به تومان . اگر حساب تلگرام متصل نباشد صفر است</summary>
        [JsonProperty("balance")]
        public long Balance { get; set; }

        /// <summary>true یعنی این اشتراک به یک کاربر ربات تلگرام وصل است و پرداخت از کیف پول ممکن است</summary>
        [JsonProperty("hasWallet")]
        public bool HasWallet { get; set; }

        /// <summary>نام اشتراک بدون بخش های $ و @</summary>
        [JsonProperty("subscriptionName")]
        public string SubscriptionName { get; set; }

        /// <summary>پیام فارسی قابل نمایش</summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
