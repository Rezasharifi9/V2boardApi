using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace V2boardApi.Tools
{
    public class Admin
    {
        private TelegramBotClient bot;
        private string AdminID;
        public Admin(TelegramBotClient bt,string AdminUniqID)
        {
            bot = bt;
            AdminID = AdminUniqID;  
        }

        /// <summary>
        /// تابع پیام به ادمین
        /// </summary>
        /// <param name="message"></param>
        /// <param name="parseMode"></param>
        /// <returns></returns>
        public async Task<bool> SendMessage(string message,ParseMode parseMode)
        {
            await bot.SendTextMessageAsync(message, AdminID, parseMode: parseMode);
            return true;
        }
    }
}