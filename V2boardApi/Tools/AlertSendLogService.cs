using DataLayer.DomainModel;
using NLog;
using System;

namespace V2boardApi.Tools
{
    /// <summary>
    /// ثبت جزئیات ارسال هشدار تلگرام در جدول tbAlertSendLogs.
    /// خطای ثبت لاگ هرگز مانع ارسال پیام نمی‌شود.
    /// </summary>
    public static class AlertSendLogService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Write(
            tbUsers recipient,
            string chatId,
            string alertType,
            string message,
            bool success,
            string error = null)
        {
            if (recipient == null)
            {
                Write(null, "—", chatId, alertType, message, success, error);
                return;
            }

            Write(recipient.User_ID, FormatRecipientName(recipient), chatId, alertType, message, success, error);
        }

        public static void Write(
            int? userId,
            string recipientName,
            string chatId,
            string alertType,
            string message,
            bool success,
            string error = null)
        {
            try
            {
                using (var db = new Entities())
                {
                    db.tbAlertSendLogs.Add(new tbAlertSendLogs
                    {
                        FK_User_ID = userId,
                        tbAsl_RecipientName = Truncate(string.IsNullOrWhiteSpace(recipientName) ? "—" : recipientName.Trim(), 200),
                        tbAsl_ChatId = Truncate(chatId, 50),
                        tbAsl_AlertType = Truncate(string.IsNullOrWhiteSpace(alertType) ? PanelNotificationService.TitleGeneral : alertType.Trim(), 200),
                        tbAsl_Message = string.IsNullOrWhiteSpace(message) ? "—" : message,
                        tbAsl_SentAt = DateTime.Now,
                        tbAsl_IsSuccess = success,
                        tbAsl_Error = Truncate(error, 500)
                    });
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "خطا در ثبت لاگ ارسال هشدار");
            }
        }

        public static string FormatRecipientName(tbUsers user)
        {
            if (user == null)
                return "—";

            var display = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName.Trim()
                : !string.IsNullOrWhiteSpace(user.BussinesTitle) ? user.BussinesTitle.Trim()
                : null;

            if (string.IsNullOrWhiteSpace(user.Username))
                return display ?? "—";

            return string.IsNullOrWhiteSpace(display) || display == user.Username
                ? user.Username
                : display + " (" + user.Username + ")";
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
