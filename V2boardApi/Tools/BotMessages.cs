using DataLayer.DomainModel;
using DeviceDetectorNET.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using V2boardApi.Models;
using V2boardBot.Functions;
using V2boardBot.Models;

namespace V2boardApi.Tools
{
    public static class BotMessages
    {
        /// <summary>
        /// متن شرایط سرویس پریمیوم و گُلد رو می دهد
        /// </summary>
        /// <param name="BotSettings"></param>
        /// <returns></returns>
        public static MessageModel SendAccpetPolicySub(tbBotSettings BotSettings)
        {
            StringBuilder str2 = new StringBuilder();
            str2.AppendLine("");
            str2.AppendLine("✨ <b> دو نوع اشتراک برای کاربران عزیز داریم </b> ✨");
            str2.AppendLine("");
            str2.AppendLine("");
            str2.AppendLine("<b>1- 🏅 اشتراک گُلد :</b>");
            str2.AppendLine("📊 حجم مشخص و پایدار");
            str2.AppendLine("🔒 اتصال پایدار در تمامی شرایط حتی اینترنت ملی");
            str2.AppendLine("✅ مناسب برای کاربرانی که به کیفیت بالا و ثبات اتصال اهمیت می‌دهند");
            str2.AppendLine("");
            str2.AppendLine("");
            str2.AppendLine("<b>2- 💎 اشتراک پرمیوم :</b>");
            str2.AppendLine("🔄 حجم نامحدود");
            str2.AppendLine("⚠️ ممکن است در برخی شرایط با نوسانات مواجه شود.");
            str2.AppendLine("");
            str2.AppendLine("");
            str2.AppendLine("انتخاب اشتراک مناسب با توجه به نیازهای شما، بهترین تجربه را فراهم می‌کند! 🌟");
            str2.AppendLine("");
            str2.AppendLine("〰️〰️〰️〰️〰️");
            str2.AppendLine("🚀 @" + BotSettings.Bot_ID);

            List<List<InlineKeyboardButton>> btns = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn = new InlineKeyboardButton("موارد بالا را خوانده ام  ✅");
            btn.CallbackData = "AccpetPolicy";
            row1.Add(btn);
            btns.Add(row1);
            var keyborad = new InlineKeyboardMarkup(btns);

            MessageModel message = new MessageModel();
            message.text = str2.ToString();
            message.keyboard = keyborad;    

            return message;
        }

        /// <summary>
        /// لیست تعداد کاربران ( زمانی که کاربر مدت زمان ماه را انتخاب کرده است )
        /// </summary>
        /// <param name="BotSettings"></param>
        /// <param name="callbackQuery"></param>
        /// <returns></returns>
        public static MessageModel SendSelectUser(tbBotSettings BotSettings,CallbackQuery callbackQuery)
        {
            var plans = BotSettings.tbUsers.tbLinkUserAndPlans.Where(s => s.tbPlans.IsRobotPlan == true && s.tbPlans.Plan_ID.ToString() == callbackQuery.Data).Select(s=> s.tbPlans).ToList();


            var keys = Keyboards.GetUserUnlimitedPlansKeyboard(plans);

            StringBuilder str = new StringBuilder();
            str.AppendLine("♨️ لطفا تعداد کاربر را انتخاب کنید");
            str.AppendLine("");
            str.AppendLine("🚀 @" + BotSettings.Bot_ID);


            MessageModel message = new MessageModel();
            message.text = str.ToString();
            message.keyboard = keys;
            return message;
        }
        /// <summary>
        /// لیست ماه ها ( زمانی که کاربر اشتراک پریمویم ( نامحدود ) است را انتخاب کرده است
        /// </summary>
        /// <param name="BotSettings"></param>
        /// <param name="plans"></param>
        /// <returns></returns>
        public static MessageModel SendSelectMonth(tbBotSettings BotSettings,List<tbPlans> plans)
        {
            
            plans = plans.GroupBy(s => s.PlanMonth).Select(g => g.FirstOrDefault()).ToList();
            var keys = Keyboards.GetMonthUnlimitedPlansKeyboard(plans);

            StringBuilder str = new StringBuilder();
            str.AppendLine("♨️ لطفا مدت زمان (ماه) مورد نظر خود را انتخاب کنید");
            str.AppendLine("");
            str.AppendLine("🚀 @" + BotSettings.Bot_ID);

            MessageModel message = new MessageModel();
            message.text = str.ToString();
            message.keyboard = keys;

            return message ;

        }

        public static MessageModel SendSelectSubType(tbBotSettings BotSettings)
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("♨️ لطفا نوع اشتراک مورد نظر خود را انتخاب کنید");
            str.AppendLine("");
            str.AppendLine("🚀 @" + BotSettings.Bot_ID);
            var keys = Keyboards.GetSubTypeKey();

            MessageModel message = new MessageModel();
            message.text = str.ToString();
            message.keyboard = keys;

            return message;
        }

        public static MessageModel SendSelectSubTypeTest(tbBotSettings BotSettings)
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("♨️ لطفا نوع اشتراک مورد نظر خود را انتخاب کنید");
            str.AppendLine("");
            str.AppendLine("🚀 @" + BotSettings.Bot_ID);
            var keys = Keyboards.GetSubTypeKeyTest();

            MessageModel message = new MessageModel();
            message.text = str.ToString();
            message.keyboard = keys;

            return message;
        }
    }

    public class MessageModel
    {
        public InlineKeyboardMarkup keyboard { get; set; }
        public string text { get; set; }
    }
}