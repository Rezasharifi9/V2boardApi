using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.api.Data.AppModels
{
    public class SubscriptionInfo
    {
        public string SubscriptionName { get; set; }

        public double TotalVolume { get; set; }

        public double UsedVolume { get; set; }

        public int RemainingDays { get; set; }

        public string BusinessName { get; set; }
        public string BusinessUserName { get; set; }

        public byte BusinessProfile { get; set; }
    }

}