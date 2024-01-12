using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
namespace V2boardBot.Models
{
    public static class Keyboards
    {
        /// <summary>
        /// کیبورد اصلی صفحه
        /// </summary>
        /// <returns></returns>
        public static InlineKeyboardMarkup GetHomeButton()
        {
            var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {

                                InlineKeyboardButton.WithCallbackData("💸 تمدید سرویس","RenewService"),
                                InlineKeyboardButton.WithCallbackData("💰 خرید سرویس","BuyService")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("🔗 اضافه کردن لینک","AddLink"),
                                InlineKeyboardButton.WithCallbackData("⚙️ سرویس ها","Services")

                            }
                            ,new[]
                            {
                               InlineKeyboardButton.WithCallbackData("📚 راهنمای اتصال","ConnectionHelp"),
                               InlineKeyboardButton.WithCallbackData("📊 تعرفه ها","PricePlans"),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("👜 کیف پول","Wallet")
                            }

                        });

            return keyboard;
        }



    }
}