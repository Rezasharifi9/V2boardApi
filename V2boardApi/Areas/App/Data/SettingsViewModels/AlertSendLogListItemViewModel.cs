namespace V2boardApi.Areas.App.Data.SettingsViewModels
{
    public class AlertSendLogListItemViewModel
    {
        public int Id { get; set; }
        public string Recipient { get; set; }
        public string ChatId { get; set; }
        public string SentAt { get; set; }
        public string AlertType { get; set; }
        public string Message { get; set; }
        public string MessageFull { get; set; }
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
    }
}
