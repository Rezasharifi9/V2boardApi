using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    /// <summary>
    /// خروجی سرویس آپلود رسید فاکتور اپلیکیشن
    /// </summary>
    public class AgentInvoiceReceiptViewModel
    {
        [JsonProperty("trackingCode")]
        public string TrackingCode { get; set; }

        /// <summary>true یعنی فایل رسید روی سرور ذخیره شده است</summary>
        [JsonProperty("receiptUploaded")]
        public bool ReceiptUploaded { get; set; }

        /// <summary>true یعنی عکس برای ادمین ربات تلگرام ارسال شده است</summary>
        [JsonProperty("sentToAdmin")]
        public bool SentToAdmin { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
