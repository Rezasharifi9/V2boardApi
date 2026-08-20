using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    /// <summary>
    /// یک تعرفه از لیست تعرفه های نماینده
    /// </summary>
    public class AgentPlanViewModel
    {
        /// <summary>شناسه تعرفه ( Link_PU_ID جدول tbLinkUserAndPlans )</summary>
        [JsonProperty("planId")]
        public int PlanId { get; set; }

        /// <summary>حجم تعرفه به گیگابایت . در تعرفه نامحدود معنی ندارد</summary>
        [JsonProperty("planVolume")]
        public double PlanVolume { get; set; }

        /// <summary>مدت زمان تعرفه به ماه</summary>
        [JsonProperty("planMonth")]
        public double PlanMonth { get; set; }

        /// <summary>مبلغ فروش نماینده به تومان</summary>
        [JsonProperty("planPrice")]
        public double PlanPrice { get; set; }

        /// <summary>تعداد کاربر مجاز تعرفه . null یعنی محدودیتی تعریف نشده است</summary>
        [JsonProperty("deviceLimit")]
        public int? DeviceLimit { get; set; }

        /// <summary>true یعنی تعرفه نامحدود است و planVolume برای آن معنی ندارد</summary>
        [JsonProperty("isUnlimited")]
        public bool IsUnlimited { get; set; }
    }
}
