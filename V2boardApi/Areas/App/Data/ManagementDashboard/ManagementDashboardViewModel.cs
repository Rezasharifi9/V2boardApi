using System.Collections.Generic;

namespace V2boardApi.Areas.App.Data.ManagementDashboard
{
    public class NamedAmountViewModel
    {
        public string Name { get; set; }
        public double Amount { get; set; }
        public int Count { get; set; }
    }

    public class ManagementDashboardViewModel
    {
        public string PeriodTitle { get; set; }
        public string TodayLabel { get; set; }

        public double MonthSales { get; set; }
        public double MonthSalesChangePercent { get; set; }
        public double TodaySales { get; set; }
        public double TodaySalesChangePercent { get; set; }
        public double DailyAverage { get; set; }
        public double AverageOrderValue { get; set; }
        public double Arpu { get; set; }
        public int MonthOrderCount { get; set; }

        public double BotSales { get; set; }
        public double AgentSales { get; set; }
        public double MasterSales { get; set; }
        public double BotSalesPercent { get; set; }
        public double AgentSalesPercent { get; set; }
        public double MasterSalesPercent { get; set; }

        public double NewSalesAmount { get; set; }
        public double RenewSalesAmount { get; set; }
        public int NewSalesCount { get; set; }
        public int RenewSalesCount { get; set; }
        public double RenewRatePercent { get; set; }

        public double AgentReceivables { get; set; }
        public int PendingInvoiceCount { get; set; }
        public double PendingInvoiceAmount { get; set; }
        public int ActiveAgentCount { get; set; }
        public int BlockedAgentCount { get; set; }
        public int TelegramActiveCount { get; set; }

        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int ExpiredCustomers { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int TrafficExhaustedCount { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public double NewCustomersChangePercent { get; set; }
        public int OnlineNow { get; set; }
        public int BannedCustomers { get; set; }
        public double ChurnRatePercent { get; set; }

        public List<NamedAmountViewModel> TopAgents { get; set; }
        public List<NamedAmountViewModel> TopPlans { get; set; }
        public List<NamedAmountViewModel> TopDebtors { get; set; }
    }

    public class ManagementDashboardChartsViewModel
    {
        public List<string> TrendLabels { get; set; }
        public List<double> TrendSales { get; set; }
        public List<string> ChannelLabels { get; set; }
        public List<double> ChannelValues { get; set; }
        public List<string> MixLabels { get; set; }
        public List<double> MixValues { get; set; }
        public List<string> CustomerLabels { get; set; }
        public List<double> CustomerValues { get; set; }
        public List<string> AgentLabels { get; set; }
        public List<double> AgentValues { get; set; }
    }
}
