namespace V2boardApi.Areas.App.Data.SettingsViewModels
{
    public class NLogListItemViewModel
    {
        public int Id { get; set; }
        public string Level { get; set; }
        public string Logged { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
        public string UserName { get; set; }
        public string IpAddress { get; set; }
        public string HttpMethod { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string ExecutionTime { get; set; }
        public bool HasException { get; set; }
    }
}
