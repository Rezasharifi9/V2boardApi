namespace V2boardApi.Areas.api.Data.ApiModels
{
    /// <summary>
    /// ورودی سرویس دریافت موجودی کیف پول ربات تلگرام مشتری
    /// </summary>
    public class GetTelegramWalletModel
    {
        /// <summary>
        /// توکن لینک ساب مشتری (ستون tbL_Token در جدول tbLinks).
        /// از روی همین توکن کاربر تلگرام صاحب اشتراک پیدا می شود.
        /// </summary>
        public string SubscriptionToken { get; set; }
    }
}
