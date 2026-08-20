using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    /// <summary>
    /// خروجی سرویس ساخت فاکتور پرداخت مستقیم نماینده
    /// </summary>
    public class AgentInvoiceViewModel
    {
        /// <summary>کد پیگیری فاکتور (همان dw_TaxId)</summary>
        [JsonProperty("trackingCode")]
        public string TrackingCode { get; set; }

        /// <summary>مبلغ فاکتور به ریال به همراه سه رقم یونیک انتهایی</summary>
        [JsonProperty("amount")]
        public long Amount { get; set; }

        /// <summary>شماره کارت فعال نماینده</summary>
        [JsonProperty("cardNumber")]
        public string CardNumber { get; set; }

        /// <summary>نام دارنده کارت</summary>
        [JsonProperty("cardHolderName")]
        public string CardHolderName { get; set; }

        /// <summary>نام اشتراک بدون بخش های $ و @ ( در حالت اشتراک جدید ، نامی که تولید شده است )</summary>
        [JsonProperty("subscriptionName")]
        public string SubscriptionName { get; set; }

        /// <summary>مبلغ تعرفه به تومان بدون اعمال تخفیف</summary>
        [JsonProperty("planPrice")]
        public double PlanPrice { get; set; }

        /// <summary>حجم تعرفه به گیگابایت</summary>
        [JsonProperty("planVolume")]
        public double PlanVolume { get; set; }

        /// <summary>مدت زمان تعرفه به ماه</summary>
        [JsonProperty("planMonth")]
        public double PlanMonth { get; set; }

        /// <summary>تعداد کاربر مجاز تعرفه . null یعنی محدودیتی تعریف نشده است</summary>
        [JsonProperty("deviceLimit")]
        public int? DeviceLimit { get; set; }
    }
}
