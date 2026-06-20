using System.Collections.Generic;

namespace V2boardApi.Areas.App.Data.SubscriptionsViewModels
{
    public class SubUsageHistorySummaryViewModel
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string TotalDownload { get; set; }
        public string TotalUpload { get; set; }
        public string Total { get; set; }
    }

    public class SubUsageHistoryResultViewModel
    {
        public string SubName { get; set; }
        public List<SubUsageHistoryItemViewModel> Items { get; set; }
        public SubUsageHistorySummaryViewModel Summary { get; set; }
    }
}
