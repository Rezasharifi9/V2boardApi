using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.DashbordModel
{
    public class WeeklyDataReportViewModel
    {
        /// <summary>
        /// مقدار فروش به ریال
        /// </summary>
        public long Sale { get; set; }
        /// <summary>
        /// مقدار درصد سود یا زیان فروش
        /// </summary>
        public double ProfitSale { get; set; }

        /// <summary>
        /// مقدار اشتراک
        /// </summary>
        public int Subscriptions { get; set; }
        /// <summary>
        /// مقدار درصد سود یا ضرر اشتراک ها
        /// </summary>
        public double ProfitSub { get; set; }

        /// <summary>
        /// مقدار مصرف ترافیک  
        /// </summary>
        public double SubscriptionUseage { get; set; }
        /// <summary>
        /// مقدار درصد مصرف ترافیک
        /// </summary>
        public double ProfitUseage { get; set; }

        /// <summary>
        /// مقدار اشتراک حفظ شده
        /// </summary>
        public int MaintainSubscription { get; set;}
        /// <summary>
        /// درصد حفظ اشتراک
        /// </summary>
        public double ProfitMaintainSub { get;set;}
    }
}