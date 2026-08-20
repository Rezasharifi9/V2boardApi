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
        /// متن شرایط سرویس نقره ای و طلایی رو می دهد
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
            str2.AppendLine("<b>1- 🥇 اشتراک طلایی :</b>");
            str2.AppendLine("📊 حجم مشخص و پایدار");
            str2.AppendLine("🔒 اتصال پایدار در تمامی شرایط حتی اینترنت ملی");
            str2.AppendLine("📱 مناسب برای تمامی دستگاه ها.");
            str2.AppendLine("✅ مناسب برای کاربرانی که به کیفیت بالا و ثبات اتصال اهمیت می‌دهند");
            str2.AppendLine("");
            str2.AppendLine("");
            str2.AppendLine("<b>2- 🥈 اشتراک نقره ای :</b>");
            str2.AppendLine("🔄 حجم نامحدود");
            str2.AppendLine("⚠️ ممکن است در برخی شرایط با نوسانات مواجه شود.");
            str2.AppendLine("🌐 سرعت متوسط");
            str2.AppendLine("📱 مناسب برای دستگاه‌های پیشرفته‌تر و اینترنت پر سرعت.");
            str2.AppendLine("📱 مناسب دستگاه های اندروید و آیفون");
            str2.AppendLine("");
            str2.AppendLine("❗️ نکته : حتما قبلا از خرید. اشتراک تست را فعال نموده و از عملکرد سرور ها با شرایط اینترنتان مطمئن شوید");
            str2.AppendLine("");
            str2.AppendLine("🌟 انتخاب اشتراک مناسب با توجه به نیازهای شما، بهترین تجربه را فراهم می‌کند");
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
            var Plan = BotSettings.tbUsers.tbLinkUserAndPlans.Where(s => s.tbPlans.IsRobotPlan == true && s.tbPlans.Plan_ID.ToString() == callbackQuery.Data).Select(s => s.tbPlans).FirstOrDefault();
            var Plans = BotSettings.tbUsers.tbLinkUserAndPlans.Where(s => s.tbPlans.IsRobotPlan == true && s.tbPlans.PlanMonth == Plan.PlanMonth).Select(s => s.tbPlans).ToList();

            var keys = Keyboards.GetUserUnlimitedPlansKeyboard(Plans);

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

        public static string FormatTariffLabel(tbPlans plan)
        {
            if (plan == null)
                return null;

            string spec;
            if (plan.IsRobotPlan)
            {
                spec = (plan.PlanMonth == 0 ? "نامحدود" : plan.PlanMonth + " ماهه") + " | نامحدود";
                if (plan.device_limit.HasValue && plan.device_limit.Value > 0)
                    spec += " | " + plan.device_limit.Value + " کاربر";
            }
            else
            {
                spec = (plan.PlanMonth == 0 ? "نامحدود" : plan.PlanMonth + " ماهه") + " | " + plan.PlanVolume + " گیگ";
            }

            if (!string.IsNullOrWhiteSpace(plan.Plan_Name))
                return plan.Plan_Name.Trim() + " — " + spec;

            return spec;
        }

        public static string BuildRenewedPackageConfirmMessage(
            double volumeGb,
            double? months,
            string subscriptionName = null,
            int? priceToman = null,
            string tariffLabel = null)
        {
            var str = new StringBuilder();
            str.AppendLine("✅ تراکنش شما با موفقیت تأیید شد.");
            var name = FormatSubscriptionDisplayName(subscriptionName);
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(tariffLabel))
                str.AppendLine("اشتراک «" + name + "» با تعرفه «" + tariffLabel + "» تمدید شد.");
            else
                str.AppendLine("✅ بسته تو تمدید کردم");
            str.AppendLine("");
            AppendPackageDetails(str, volumeGb, months, subscriptionName, priceToman, tariffLabel);
            str.AppendLine("");
            str.AppendLine("♨️ میتونی توی بخش مدیریت اشتراک ها ببینی که تمدید شده");
            return str.ToString();
        }

        public static string BuildReservedPackageConfirmMessage(
            double volumeGb,
            double? months,
            string subscriptionName = null,
            int? priceToman = null,
            string tariffLabel = null)
        {
            var str = new StringBuilder();
            str.AppendLine("✅ تراکنش شما با موفقیت تأیید شد.");
            var name = FormatSubscriptionDisplayName(subscriptionName);
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(tariffLabel))
                str.AppendLine("به دلیل داشتن بسته فعال، بسته تمدیدی اشتراک «" + name + "» با تعرفه «" + tariffLabel + "» به حالت رزرو رفته و پس از پایان بسته فعلی فعال خواهد شد.");
            else
                str.AppendLine("به دلیل داشتن بسته فعال، بسته تمدیدی به حالت رزرو رفته و پس از پایان بسته فعلی فعال خواهد شد.");
            str.AppendLine("");
            AppendPackageDetails(str, volumeGb, months, subscriptionName, priceToman, tariffLabel);
            return str.ToString();
        }

        private static string FormatSubscriptionDisplayName(string subscriptionName)
        {
            if (string.IsNullOrWhiteSpace(subscriptionName))
                return null;
            return subscriptionName.Contains("$")
                ? subscriptionName.Split('$')[0]
                : subscriptionName.Split('@')[0];
        }

        private static void AppendPackageDetails(
            StringBuilder str,
            double volumeGb,
            double? months,
            string subscriptionName,
            int? priceToman,
            string tariffLabel)
        {
            str.AppendLine("📋 مشخصات بسته:");
            var name = FormatSubscriptionDisplayName(subscriptionName);
            if (!string.IsNullOrWhiteSpace(name))
                str.AppendLine("نام اشتراک: " + name);
            if (!string.IsNullOrWhiteSpace(tariffLabel))
                str.AppendLine("تعرفه: " + tariffLabel);
            str.AppendLine("♾ حجم: " + volumeGb + " گیگ");
            str.AppendLine("⏳ مدت: " + (months == null || months == 0 ? "نامحدود" : months + " ماه"));
            if (priceToman.HasValue)
                str.AppendLine("💵 مبلغ: " + priceToman.Value.ConvertToMony() + " تومان");
        }
    }

    public class MessageModel
    {
        public InlineKeyboardMarkup keyboard { get; set; }
        public string text { get; set; }
    }
}