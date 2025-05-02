using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.SubscriptionsViewModels
{
    public class GetUserDataModel
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string UsedVolume { get; set; }
        public int DaysLeft { get; set; }
        public string ExpireDate { get; set; }
        public string PlanName { get; set; }
        public string SubLink { get; set; }
        public string RemainingVolume { get; set; }
        public bool CanEdit { get; set; }
        public bool IsBanned { get; set; }
        public string TotalVolume { get; set; }
        public int IsActive { get; set;}
        public bool IsOnline { get; set; }
        public string LastTimeOnline { get; set; }
        public int OnlineUsers { get; set; }
        public int LimitUsers { get; set; }
        public bool Exceeded { get; set; }

    }
}