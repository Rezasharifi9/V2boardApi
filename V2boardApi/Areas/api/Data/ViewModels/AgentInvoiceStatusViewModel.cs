using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    /// <summary>
    /// خروجی سرویس بررسی وضعیت فاکتور پرداخت مستقیم نماینده
    /// </summary>
    public class AgentInvoiceStatusViewModel
    {
        /// <summary>کد پیگیری فاکتور (همان dw_TaxId)</summary>
        [JsonProperty("trackingCode")]
        public string TrackingCode { get; set; }

        /// <summary>true یعنی پرداخت این فاکتور تائید شده است</summary>
        [JsonProperty("isConfirmed")]
        public bool IsConfirmed { get; set; }

        /// <summary>وضعیت خام فاکتور : FOR_PAY یا FINISH یا FOR_RESERVE</summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>نوع سفارش : جدید یا تمدید</summary>
        [JsonProperty("orderType")]
        public string OrderType { get; set; }

        /// <summary>نام اشتراک بدون بخش های $ و @</summary>
        [JsonProperty("subscriptionName")]
        public string SubscriptionName { get; set; }

        /// <summary>مبلغ فاکتور به ریال به همراه سه رقم یونیک انتهایی</summary>
        [JsonProperty("amount")]
        public long Amount { get; set; }

        /// <summary>
        /// لینک اشتراک . فقط برای سفارش جدید تائید شده مقدار دارد
        /// در سفارش تمدید و در فاکتور تائید نشده null است.
        /// </summary>
        [JsonProperty("subscriptionLink")]
        public string SubscriptionLink { get; set; }

        /// <summary>لینک پشتیبان اشتراک . در صورت تعریف نشدن آدرس پشتیبان روی سرور null است</summary>
        [JsonProperty("backupSubscriptionLink")]
        public string BackupSubscriptionLink { get; set; }

        /// <summary>true یعنی مشتری برای این فاکتور رسید عکس آپلود کرده است</summary>
        [JsonProperty("hasReceipt")]
        public bool HasReceipt { get; set; }

        /// <summary>حجم کل اشتراک به گیگابایت . فقط بعد از تائید پر می‌شود</summary>
        [JsonProperty("totalVolumeGb")]
        public double? TotalVolumeGb { get; set; }

        /// <summary>حجم مصرف‌شده به گیگابایت . فقط بعد از تائید پر می‌شود</summary>
        [JsonProperty("usedVolumeGb")]
        public double? UsedVolumeGb { get; set; }

        /// <summary>روز باقی‌مانده . صفر یعنی منقضی ، 1- یعنی نامحدود . فقط بعد از تائید</summary>
        [JsonProperty("remainingDays")]
        public int? RemainingDays { get; set; }

        /// <summary>تاریخ انقضا به شمسی yyyy/MM/dd . فقط بعد از تائید</summary>
        [JsonProperty("expireDate")]
        public string ExpireDate { get; set; }

        /// <summary>پیام قابل نمایش به مشتری</summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
