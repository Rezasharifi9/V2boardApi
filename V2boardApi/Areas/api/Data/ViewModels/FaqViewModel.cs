using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    /// <summary>
    /// یک پرسش و پاسخ از بخش «سؤالات رایج» ربات تلگرام
    /// </summary>
    public class FaqItemViewModel
    {
        [JsonProperty("question")]
        public string Question { get; set; }

        [JsonProperty("answer")]
        public string Answer { get; set; }
    }

    /// <summary>
    /// خروجی سرویس سوالات متداول — همان متنی که ربات با دکمه «❓ سؤالات رایج» می‌فرستد
    /// </summary>
    public class FaqViewModel
    {
        public const string DefaultTitle = "❓ سؤالات رایج در خصوص سرویس ها ❓";
        public const string DefaultFooter = "💬 اگر سوالی داشتید که پاسخ آن را نیافتید با پشتیبانی در ارتباط باشید.";

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("items")]
        public List<FaqItemViewModel> Items { get; set; }

        [JsonProperty("footer")]
        public string Footer { get; set; }

        /// <summary>آیدی پشتیبانی نماینده بدون @ — از tbBotSettings.AdminUsername</summary>
        [JsonProperty("supportUsername")]
        public string SupportUsername { get; set; }

        public static List<FaqItemViewModel> DefaultItems()
        {
            return new List<FaqItemViewModel>
            {
                new FaqItemViewModel
                {
                    Question = "🔹 آیا اشتراک من ثابت است و می‌توانم آی‌پی را تغییر دهم؟",
                    Answer = "بله، اشتراک ها به صورت ثابت (استاتیک) ارائه می‌شود."
                },
                new FaqItemViewModel
                {
                    Question = "🔹 آیا می‌توانم با چند دستگاه به یک اشتراک متصل شوم؟",
                    Answer = "بله، اشتراک ما به شما اجازه می‌دهد که بدون محدودیت کاربری، به چندین دستگاه به طور همزمان متصل شوید."
                },
                new FaqItemViewModel
                {
                    Question = "🔹 آیا می‌توانم موقعیت سرورم را تغییر دهم؟",
                    Answer = "بله، شما می‌توانید به راحتی از طریق لیست سرورهای موجود در اشتراک ، سرور مورد نظر خود را انتخاب کنید"
                },
                new FaqItemViewModel
                {
                    Question = "🔹 آیا حجم باقی مانده یا زمان باقی مانده به دوره بعد انتقال می یابد؟",
                    Answer = "خیر، حجم یا زمان باقی مانده شما به دوره بعد انتقال نمی یابد و باید در دوره خریداری شده مصرف شود !!"
                },
                new FaqItemViewModel
                {
                    Question = "🔹 آیا قبل از اتمام زمان یا حجم , بسته جدید تمدید کنم بسته قبلی از بین میرود ؟",
                    Answer = "خیر، اگر حجم یا زمان داشته باشید بسته جدید رزرو خواهد شد و بعد از پایان بسته فعلی جایگزین خواهد شد !!"
                }
            };
        }

        public static FaqViewModel ForAgent(string supportUsername)
        {
            return new FaqViewModel
            {
                Title = DefaultTitle,
                Items = DefaultItems(),
                Footer = DefaultFooter,
                SupportUsername = supportUsername
            };
        }

        /// <summary>
        /// همان پیام HTML که ربات تلگرام برای دکمه «❓ سؤالات رایج» می‌فرستد
        /// </summary>
        public static string BuildTelegramMessage(string adminUsername)
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("<b>" + DefaultTitle + "</b>");
            str.AppendLine("");
            str.AppendLine("");
            foreach (var item in DefaultItems())
            {
                str.AppendLine("<b>" + item.Question + "</b>");
                str.AppendLine(item.Answer);
                str.AppendLine("");
            }
            str.AppendLine(DefaultFooter);
            str.AppendLine("");
            str.AppendLine("🆔 @" + adminUsername);
            return str.ToString();
        }
    }
}
