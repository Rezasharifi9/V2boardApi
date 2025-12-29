using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.AdminDashboard
{
    public class SalesSummaryViewModel
    {
        public double TotalSales { get; set; }          // فروش کل
        public int TotalCustomers { get; set; }          // کل مشتریان
        public double AgentSales { get; set; }           // کل فروش نمایندگان
        public double BotSales { get; set; }             // کل فروش ربات
    }
}