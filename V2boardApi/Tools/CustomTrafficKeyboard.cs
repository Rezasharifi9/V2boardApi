using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using V2boardApi.Tools;

namespace V2boardBotApp.Models
{
    public class CustomTrafficKeyboard
    {
        tbBotSettings tbBotSettings;
        InlineKeyboardMarkup keyboardButtons;
        int Traffic;
        int Month;
        public CustomTrafficKeyboard(tbBotSettings botSettings, int? TrafficUser = null, int? MonthUser = null)
        {
            if (TrafficUser == null)
            {
                Traffic = 10;
            }
            else
            {
                Traffic = TrafficUser.Value;
            }
            if (MonthUser == null)
            {
                Month = 1;
            }
            else
            {
                Month = MonthUser.Value;
            }
            tbBotSettings = botSettings;
            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();

            List<InlineKeyboardButton> StaticRow1 = new List<InlineKeyboardButton>();
            StaticRow1.Add(InlineKeyboardButton.WithCallbackData("📊 ترافیک", "ترافیک"));

            inlineKeyboards.Add(StaticRow1);

            List<InlineKeyboardButton> DynamicRow2 = new List<InlineKeyboardButton>();
            DynamicRow2.Add(InlineKeyboardButton.WithCallbackData("➖", "MinsTraffic"));
            DynamicRow2.Add(InlineKeyboardButton.WithCallbackData(Traffic + " گیگ", "Gig"));
            DynamicRow2.Add(InlineKeyboardButton.WithCallbackData("➕", "PlusTraffic"));

            inlineKeyboards.Add(DynamicRow2);

            List<InlineKeyboardButton> StaticRow3 = new List<InlineKeyboardButton>();
            StaticRow3.Add(InlineKeyboardButton.WithCallbackData("⏳ زمان سرویس", "زمان سرویس"));

            inlineKeyboards.Add(StaticRow3);

            List<InlineKeyboardButton> DynamicRow4 = new List<InlineKeyboardButton>();
            DynamicRow4.Add(InlineKeyboardButton.WithCallbackData("➖", "MinusMonth"));
            DynamicRow4.Add(InlineKeyboardButton.WithCallbackData(Month + " ماه", "Month"));
            DynamicRow4.Add(InlineKeyboardButton.WithCallbackData("➕", "PlusMonth"));

            inlineKeyboards.Add(DynamicRow4);

            var Price = tbBotSettings.PricePerMonth_Major * Month;
            Price += tbBotSettings.PricePerGig_Major * Traffic;

            if (botSettings.Present_Discount != null && botSettings.Present_Discount != 0)
            {
                List<InlineKeyboardButton> DiscountRow = new List<InlineKeyboardButton>();

                var DiscountPrice = Price * botSettings.Present_Discount;
                DiscountRow.Add(InlineKeyboardButton.WithCallbackData(Convert.ToInt32(DiscountPrice).ConvertToMony() + " تومان", "**"));
                DiscountRow.Add(InlineKeyboardButton.WithCallbackData("🌻 تخفیف :", "🌻 تخفیف"));

                inlineKeyboards.Add(DiscountRow);

            }

            


            List<InlineKeyboardButton> PriceRow = new List<InlineKeyboardButton>();

            if (botSettings.Present_Discount != null)
            {
                var DiscountPrice = Price * botSettings.Present_Discount;
                PriceRow.Add(InlineKeyboardButton.WithCallbackData(Convert.ToInt32((Price - DiscountPrice)).ConvertToMony() + " تومان", "40,000 تومان"));
            }
            else
            {
                PriceRow.Add(InlineKeyboardButton.WithCallbackData(Price.ConvertToMony() + " تومان", "40,000 تومان"));
            }
            
            PriceRow.Add(InlineKeyboardButton.WithCallbackData("💸 قیمت نهایی :", "💸 قیمت نهایی :"));

            inlineKeyboards.Add(PriceRow);

            List<InlineKeyboardButton> FinishRow = new List<InlineKeyboardButton>();
            //FinishRow.Add(InlineKeyboardButton.WithCallbackData("بازگشت 🔙", "بازگشت"));
            FinishRow.Add(InlineKeyboardButton.WithCallbackData("🔜 مرحله بعد", "NextLevel"));


            inlineKeyboards.Add(FinishRow);

            keyboardButtons = new InlineKeyboardMarkup(inlineKeyboards);

        }

        public void AddTraffic()
        {

        }

        public InlineKeyboardMarkup GetKeyboard()
        {
            var s = keyboardButtons.InlineKeyboard.ToList()[1].ToList()[1];
            return keyboardButtons;
        }
    }
}
