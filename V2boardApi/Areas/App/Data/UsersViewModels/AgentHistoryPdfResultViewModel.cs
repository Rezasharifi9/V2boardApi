using System.Collections.Generic;
using V2boardApi.Areas.App.Data.RequestModels;

namespace V2boardApi.Areas.App.Data.UsersViewModels
{
    public class AgentHistoryPdfResultViewModel
    {
        public string AgentName { get; set; }
        public string AgentUsername { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string GeneratedAt { get; set; }
        public AgentHistorySummaryViewModel Summary { get; set; }
        public List<UserLogResponseModel> Items { get; set; }
    }
}
