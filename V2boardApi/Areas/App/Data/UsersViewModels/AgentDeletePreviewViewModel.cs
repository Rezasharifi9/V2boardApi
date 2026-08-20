namespace V2boardApi.Areas.App.Data.UsersViewModels
{
    public class AgentDeletePreviewViewModel
    {
        public string Username { get; set; }
        public int FactorCount { get; set; }
        public int LogCount { get; set; }
        public int TelegramUserCount { get; set; }
        public int OrderCount { get; set; }
        public int DepositLogCount { get; set; }
        public int PlanCount { get; set; }
        public int ChildAgentCount { get; set; }
        public int NotificationCount { get; set; }
        public int LinkCount { get; set; }
        public string Message { get; set; }
    }
}
