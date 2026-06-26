namespace V2boardApi.Areas.App.Data.UsersViewModels
{
    public class AgentHistorySummaryViewModel
    {
        public int CreatedCount { get; set; }
        public int RenewedCount { get; set; }
        public double TotalSalesAmount { get; set; }
        public string TotalSalesAmountFormatted { get; set; }
        public double PaidInvoicesAmount { get; set; }
        public string PaidInvoicesAmountFormatted { get; set; }
    }
}
