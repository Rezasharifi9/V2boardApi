using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    public class NotPayedUserModel
    {
        public string AccoutName { get; set; }
        public string PlanName { get; set; }
        public int Price { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderType { get; set; }
    }
}