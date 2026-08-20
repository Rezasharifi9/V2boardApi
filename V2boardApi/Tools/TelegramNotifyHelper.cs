using System;
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
            ReplyParameters replyParameters = null)
        {
            if (client == null || string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(message))
                return false;

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
            catch
            {
                return false;
            }
        }
    }
}
