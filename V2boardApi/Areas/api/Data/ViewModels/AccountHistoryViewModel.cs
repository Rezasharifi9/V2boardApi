using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    public class AccountHistoryViewModel
    {
        public string CreateTime { get; set; }
        public int SalePrice { get; set; }
        public string PlanName { get; set; }
    }
}