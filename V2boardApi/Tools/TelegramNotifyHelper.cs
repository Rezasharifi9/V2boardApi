using DataLayer.DomainModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace V2boardApi.Tools
{
    /// <summary>
    /// ارسال پیام‌های اطلاع‌رسانی تلگرام بدون ثبت لاگ برای خطاهای بلاک/عدم دسترسی.
    /// </summary>
    public static class TelegramNotifyHelper
    {
        public static bool IsDeliveryBlockedException(Exception ex)
        {
            if (ex == null)
                return false;

            var apiEx = ex as ApiRequestException;
            if (apiEx != null && (apiEx.ErrorCode == 403 || apiEx.ErrorCode == 400))
                return true;

            var message = (ex.Message + " " + (ex.InnerException?.Message ?? string.Empty)).ToLowerInvariant();
            return message.Contains("bot was blocked")
                || message.Contains("blocked by the user")
                || message.Contains("user is deactivated")
                || message.Contains("chat not found")
                || message.Contains("peer_id_invalid")
                || message.Contains("forbidden")
                || message.Contains("have no rights")
                || message.Contains("need administrator");
        }

        public static async Task<bool> TrySendMessageAsync(
            TelegramBotClient client,
            string chatId,
            string message,
            ParseMode? parseMode = null,
            ReplyMarkup replyMarkup = null,
            ReplyParameters replyParameters = null,
            Action<string> onFailure = null)
        {
            if (client == null || string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(message))
            {
                onFailure?.Invoke("پارامتر ارسال ناقص است");
                return false;
            }

            try
            {
                if (parseMode.HasValue)
                {
                    await client.SendMessage(
                        chatId,
                        message,
                        parseMode: parseMode.Value,
                        replyMarkup: replyMarkup,
                        replyParameters: replyParameters);
                }
                else
                {
                    await client.SendMessage(
                        chatId,
                        message,
                        replyMarkup: replyMarkup,
                        replyParameters: replyParameters);
                }
                return true;
            }
            catch (Exception ex)
            {
                onFailure?.Invoke(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// یوزرنیم یا شناسه عددی تلگرام را از ورودی پروفایل تمیز می‌کند.
        /// @، فاصله و لینک t.me حذف می‌شود تا Hesam_Sadeghi10 و @Hesam_Sadeghi10 یکی شوند.
        /// </summary>
        public static string NormalizeTelegramIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var s = value.Trim();
            const string httpsPrefix = "https://t.me/";
            const string httpPrefix = "http://t.me/";
            const string shortPrefix = "t.me/";

            if (s.StartsWith(httpsPrefix, StringComparison.OrdinalIgnoreCase))
                s = s.Substring(httpsPrefix.Length);
            else if (s.StartsWith(httpPrefix, StringComparison.OrdinalIgnoreCase))
                s = s.Substring(httpPrefix.Length);
            else if (s.StartsWith(shortPrefix, StringComparison.OrdinalIgnoreCase))
                s = s.Substring(shortPrefix.Length);

            s = s.Trim().TrimStart('@');

            var cut = s.IndexOfAny(new[] { '/', '?', '#' });
            if (cut >= 0)
                s = s.Substring(0, cut);

            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        /// <summary>
        /// مقصد پیام نماینده را از آیدی تلگرام پروفایل می‌سازد.
        /// هرگز به AdminBot_ID برنمی‌گردد تا پیام اشتباهاً برای ادمین نرود.
        /// </summary>
        public static string ResolveChatIdFromProfile(Entities db, string telegramId, string preferredRobotId)
        {
            var identity = NormalizeTelegramIdentity(telegramId);
            if (identity == null || db == null)
                return null;

            long numericId;
            var isNumeric = long.TryParse(identity, out numericId) && numericId > 0;
            var withAt = "@" + identity;

            tbTelegramUsers telUser = null;
            if (!string.IsNullOrEmpty(preferredRobotId))
            {
                telUser = FindTelegramUser(db.tbTelegramUsers.Where(t => t.Tel_RobotID == preferredRobotId), identity, withAt, isNumeric);
            }

            if (telUser == null)
                telUser = FindTelegramUser(db.tbTelegramUsers, identity, withAt, isNumeric);

            if (telUser != null && !string.IsNullOrEmpty(telUser.Tel_UniqUserID))
                return telUser.Tel_UniqUserID;

            if (isNumeric)
                return identity;

            return withAt;
        }

        private static tbTelegramUsers FindTelegramUser(
            IQueryable<tbTelegramUsers> source,
            string identity,
            string withAt,
            bool isNumeric)
        {
            return source.FirstOrDefault(t =>
                (t.Tel_Username != null && (t.Tel_Username == identity || t.Tel_Username == withAt))
                || (isNumeric && t.Tel_UniqUserID == identity));
        }
    }
}
