using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using V2boardBotApp;
using V2boardBotApp.Models;

namespace V2boardBot.Models
{
    public static class Keyboards
    {

        public static ReplyKeyboardMarkup BasicKeyboard(List<List<KeyboardButton>> keys)
        {
            var keyboard = new ReplyKeyboardMarkup(keys);
            keyboard.IsPersistent = true;
            keyboard.ResizeKeyboard = true;
            keyboard.OneTimeKeyboard = true;
            return keyboard;
        }

        /// <summary>
        /// کیبورد اصلی صفحه
        /// </summary>
        /// <returns></returns>
        public static ReplyKeyboardMarkup GetHomeButton()
        {

            ReplyKeyboardMarkup keyboard;
            using (Entities db = new Entities())
            {
                
                var learn = db.tbConnectionHelp.Where(s => s.ch_Type.Contains("buy_sub")).FirstOrDefault();
                if (learn != null)
                {
                    keyboard = new ReplyKeyboardMarkup(new[]
                        {
                           new[]
                            {

                                new KeyboardButton("📲 آموزش خرید")
                            },
                            new[]
                            {

                                new KeyboardButton("🛒 خرید اشتراک"),
                                new KeyboardButton("🔄 تمدید اشتراک"),
                                new KeyboardButton("🌐 مدیریت اشتراک ‌ها")
                            },new[]
                            {
                                new KeyboardButton("👜 کیف پول من"),
                                new KeyboardButton("📊 تعرفه‌ها"),
                                new KeyboardButton("🎁 اشتراک تست"),
                            },
                            new[]
                            {
                                new KeyboardButton("❓ سؤالات رایج"),
                                new KeyboardButton("📘 آموزش اتصال")
                            },
                            new[]
                            {
                                new KeyboardButton("📞 ارتباط با پشتیبانی"),
                            }

                        });
                }
                else
                {
                    keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[]
                            {

                                new KeyboardButton("🛒 خرید اشتراک"),
                                new KeyboardButton("🔄 تمدید اشتراک"),
                                new KeyboardButton("🌐 مدیریت اشتراک ‌ها")
                            },new[]
                            {
                                new KeyboardButton("👜 کیف پول من"),
                                new KeyboardButton("📊 تعرفه‌ها"),
                                new KeyboardButton("🎁 اشتراک تست"),
                            },
                            new[]
                            {
                                new KeyboardButton("❓ سؤالات رایج"),
                                new KeyboardButton("📘 آموزش اتصال")
                            },
                            new[]
                            {
                                new KeyboardButton("📞 ارتباط با پشتیبانی"),
                            }

                        });
                }
            }

            


            keyboard.IsPersistent = false;
            keyboard.ResizeKeyboard = true;
            keyboard.OneTimeKeyboard = false;

            return keyboard;
        }


        /// <summary>
        /// کیبورد صفحه اصلی ادمین
        /// </summary>
        /// <returns></returns>
        public static ReplyKeyboardMarkup GetAdminHomeButton()
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                new KeyboardButton("✅ تمدید اشتراک"),
                                new KeyboardButton("➕ ایجاد اشتراک"),
                            },
                            new[]
                            {
                                new KeyboardButton("📲 آموزش ها"),
                                new KeyboardButton("👬 مدیریت اشتراک ها"),
                            },
                            new[]
                            {
                                new KeyboardButton("📊 آمار"),
                                new KeyboardButton("💳 کیف پول"),
                            }

                        }) ;


            keyboard.IsPersistent = true;
            keyboard.ResizeKeyboard = true;
            keyboard.OneTimeKeyboard = false;

            return keyboard;
        }


        public static InlineKeyboardMarkup PayDebitMajor()
        {


            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            var key = new InlineKeyboardButton("💳 پرداخت");
            key.CallbackData = "pay";

            buttons.Add(key);

            var replyMurkup = new InlineKeyboardMarkup(buttons);
            return replyMurkup;
        }

        public static ReplyKeyboardMarkup GetLinksKeyboard(int TelegramUserID, Repository<tbLinks> Rep)
        {
            var Links = Rep.table.Where(p => p.FK_TelegramUserID == TelegramUserID).ToList();

            List<List<KeyboardButton>> inlineKeyboards = new List<List<KeyboardButton>>();
            if (Links.Count != 0)
            {
                int itemsPerRow = 2; // تعداد دکمه‌ها در هر سطر

                for (int i = 0; i < Links.Count; i += itemsPerRow)
                {
                    List<KeyboardButton> row = new List<KeyboardButton>();

                    for (int j = i; j < i + itemsPerRow && j < Links.Count; j++)
                    {
                        row.Add(new KeyboardButton(Links[j].tbL_Email.Split('@')[0]));
                    }

                    inlineKeyboards.Add(row);
                }
            }
            else
            {
                return null;
            }
            List<KeyboardButton> row1 = new List<KeyboardButton>();
            row1.Add(new KeyboardButton("⬅️ برگشت به صفحه اصلی"));
            inlineKeyboards.Add(row1);
            var keyboard = new ReplyKeyboardMarkup(inlineKeyboards);
            keyboard.IsPersistent = true;
            keyboard.ResizeKeyboard = true;
            keyboard.OneTimeKeyboard = false;
            return keyboard;
        }

        public static InlineKeyboardMarkup GetServiceLinksKeyboard(int TelegramUserID, Repository<tbLinks> Rep)
        {
            var Links = Rep.Where(p => p.FK_TelegramUserID == TelegramUserID).ToList();

            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
            if (Links.Count != 0)
            {
                int itemsPerRow = 2; // تعداد دکمه‌ها در هر سطر

                for (int i = 0; i < Links.Count; i += itemsPerRow)
                {
                    List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();

                    for (int j = i; j < i + itemsPerRow && j < Links.Count; j++)
                    {
                        var Name = Links[j].tbL_Email.Split('@')[0];
                        if (Name.Contains('$'))
                        {
                            Name = Name.Split('$')[0];
                        }
                        row.Add(InlineKeyboardButton.WithCallbackData(Name, Links[j].tbL_Email));
                    }

                    inlineKeyboards.Add(row);
                }
            }
            else
            {
                return null;
            }
            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

            return keyboard;
        }


        public static InlineKeyboardMarkup GetPlansKeyboard(string Email, Repository<tbLinkUserAndPlans> Rep)
        {
            var username = Email.Split('@')[1];
            var Plans = Rep.Where(p => p.tbUsers.Username == username && p.tbPlans.Plan_Des != null).ToList();

            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
            int itemsPerRow = 2; // تعداد دکمه‌ها در هر سطر

            for (int i = 0; i < Plans.Count; i += itemsPerRow)
            {
                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();

                for (int j = i; j < i + itemsPerRow && j < Plans.Count; j++)
                {
                    row.Add(InlineKeyboardButton.WithCallbackData(Plans[j].tbPlans.Plan_Name, Plans[j].tbPlans.Plan_ID + "%" + Email));
                }

                inlineKeyboards.Add(row);
            }
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            row1.Add(InlineKeyboardButton.WithCallbackData("⬅️ برگشت", "backToInfo"));
            inlineKeyboards.Add(row1);


            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

            return keyboard;
        }

        public static InlineKeyboardMarkup GetMonthUnlimitedPlansKeyboard(List<tbPlans> plans)
        {

            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
            int itemsPerRow = 2; // تعداد دکمه‌ها در هر سطر
            for (int i = 0; i < plans.Count; i += itemsPerRow)
            {
                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();

                for (int j = i; j < i + itemsPerRow && j < plans.Count; j++)
                {
                    row.Add(InlineKeyboardButton.WithCallbackData(plans[j].PlanMonth.ToString() + " ماهه", plans[j].Plan_ID.ToString()));
                }

                inlineKeyboards.Add(row);
            }
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            row1.Add(InlineKeyboardButton.WithCallbackData("⬅️ برگشت", "backToInfo"));
            inlineKeyboards.Add(row1);


            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

            return keyboard;
        }

        public static InlineKeyboardMarkup GetUserUnlimitedPlansKeyboard(List<tbPlans> plans)
        {

            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
            int itemsPerRow = 2; // تعداد دکمه‌ها در هر سطر

            for (int i = 0; i < plans.Count; i += itemsPerRow)
            {
                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();

                for (int j = i; j < i + itemsPerRow && j < plans.Count; j++)
                {
                    row.Add(InlineKeyboardButton.WithCallbackData((plans[j].device_limit).ToString() + " کاربر", plans[j].Plan_ID.ToString()));
                }

                inlineKeyboards.Add(row);
            }
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            row1.Add(InlineKeyboardButton.WithCallbackData("⬅️ برگشت", "backToInfo"));
            inlineKeyboards.Add(row1);


            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

            return keyboard;
        }

        //public static ReplyKeyboardMarkup GetPlansKeyboardForAdmin(string username, Repository<tbLinkUserAndPlans> Rep)
        //{
        //    var Plans = Rep.Where(p => p.tbUsers.Username == username && p.tbPlans.Plan_Des != null).OrderBy(p => p.tbPlans.Price2).ToList();

        //    List<List<KeyboardButton>> inlineKeyboards = new List<List<KeyboardButton>>();
        //    int itemsPerRow = 2; // تعداد دکمه‌ها در هر سطر

        //    for (int i = 0; i < Plans.Count; i += itemsPerRow)
        //    {
        //        List<KeyboardButton> row = new List<KeyboardButton>();

        //        for (int j = i; j < i + itemsPerRow && j < Plans.Count; j++)
        //        {
        //            row.Add(new KeyboardButton(Plans[j].tbPlans.Plan_Des));
        //        }

        //        inlineKeyboards.Add(row);
        //    }
        //    List<KeyboardButton> row1 = new List<KeyboardButton>();
        //    row1.Add(new KeyboardButton("⬅️ برگشت به صفحه اصلی"));
        //    inlineKeyboards.Add(row1);
        //    var keyboard = new ReplyKeyboardMarkup(inlineKeyboards);
        //    keyboard.IsPersistent = true;
        //    keyboard.ResizeKeyboard = true;
        //    keyboard.OneTimeKeyboard = false;
        //    return keyboard;
        //}


        public static InlineKeyboardMarkup GetPlansKeyboard(string linkId, List<tbPlans> plans)
        {
            var Plans = plans.Where(p => p.Status == true && p.Plan_Des != null).ToList();

            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
            int itemsPerRow = 2; // تعداد دکمه‌ها در هر سطر

            for (int i = 0; i < Plans.Count; i += itemsPerRow)
            {
                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();

                for (int j = i; j < i + itemsPerRow && j < Plans.Count; j++)
                {
                    row.Add(InlineKeyboardButton.WithCallbackData(Plans[j].Plan_Des, "Select_Plan" + "%" + linkId + "_" + Plans[j].Plan_ID_V2));
                }

                inlineKeyboards.Add(row);
            }
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            row1.Add(InlineKeyboardButton.WithCallbackData("⬅️ برگشت", "backToInfo"));
            inlineKeyboards.Add(row1);
            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

            return keyboard;
        }


        public static ReplyKeyboardMarkup GetBackButton()
        {
            List<List<KeyboardButton>> inlineKeyboards = new List<List<KeyboardButton>>();
            List<KeyboardButton> row1 = new List<KeyboardButton>();
            row1.Add(new KeyboardButton("⬅️ برگشت به صفحه اصلی"));
            inlineKeyboards.Add(row1);
            var key = new ReplyKeyboardMarkup(inlineKeyboards);
            key.IsPersistent = true;
            key.ResizeKeyboard = true;
            key.OneTimeKeyboard = false;
            return key;
        }

        public static ReplyKeyboardMarkup GetPaymentMethods()
        {
            List<List<KeyboardButton>> inlineKeyboards = new List<List<KeyboardButton>>();

            List<KeyboardButton> row2 = new List<KeyboardButton>();
            //row2.Add(new KeyboardButton("💳 پرداخت ریالی"));
            inlineKeyboards.Add(row2);

            List<KeyboardButton> row1 = new List<KeyboardButton>();
            //row1.Add(new KeyboardButton("💳 کارت به کارت"));
            row1.Add(new KeyboardButton("👜 پرداخت از کیف پول"));
            inlineKeyboards.Add(row1);

            List<KeyboardButton> row3 = new List<KeyboardButton>();
            row3.Add(new KeyboardButton("⬅️ برگشت به صفحه اصلی"));
            inlineKeyboards.Add(row3);

            var keyboard = new ReplyKeyboardMarkup(inlineKeyboards);
            keyboard.IsPersistent = true;
            keyboard.ResizeKeyboard = true;
            keyboard.OneTimeKeyboard = false;
            return keyboard;
        }

        /// <summary>
        /// تابع آوردن دکمه تائید پرداخت
        /// </summary>
        /// <returns></returns>
        public static InlineKeyboardMarkup GetAcceptPaymentLink(string paymentId, string Wallet, string price)
        {
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn = new InlineKeyboardButton("✅ واریز کردم");

            btn.CallbackData = "paid_" + paymentId;
            var url = "https://t.me/SwapinoBot?start=BuyTron-" + Wallet + "-" + price + "-Tron";
            
            row1.Add(InlineKeyboardButton.WithUrl("🏧 پرداخت", url));
            row1.Add(btn);
            var keyborad = new InlineKeyboardMarkup(row1);

            return keyborad;
        }

        /// <summary>
        /// تابع آوردن دکمه تائید پرداخت
        /// </summary>
        /// <returns></returns>
        public static InlineKeyboardMarkup GetPaymentButtonForIncreaseWallet(string paymentId, string Wallet, string price)
        {
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn1 = new InlineKeyboardButton("🏧 پرداخت");
            InlineKeyboardButton btn = new InlineKeyboardButton("✅ واریز کردم");

            btn.CallbackData = "paidwallet_" + paymentId;
            btn1.Pay = true;
            btn1.Url = "https://t.me/SwapinoBot?start=BuyTron-" + Wallet + "-" + price + "-Tron";

            row1.Add(btn1);
            row1.Add(btn);
            var keyborad = new InlineKeyboardMarkup(row1);

            return keyborad;
        }

        public static InlineKeyboardMarkup GetHelpKeyboard(List<tbConnectionHelp> tbConnectionHelps)
        {
            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
            if (tbConnectionHelps.Count != 0)
            {
                int itemsPerRow = 2; // تعداد دکمه‌ها در هر سطر

                for (int i = 0; i < tbConnectionHelps.Count; i += itemsPerRow)
                {
                    List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();

                    for (int j = i; j < i + itemsPerRow && j < tbConnectionHelps.Count; j++)
                    {
                        row.Add(InlineKeyboardButton.WithCallbackData(tbConnectionHelps[j].ch_Title, tbConnectionHelps[j].ch_ID.ToString()));
                    }

                    inlineKeyboards.Add(row);
                }
            }
            else
            {
                return null;
            }
            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);
            return keyboard;
        }


        /// <summary>
        /// تابع آوردن دکمه تائید پرداخت
        /// </summary>
        /// <returns></returns>
        public static InlineKeyboardMarkup GetAccpetBuyFromWallet()
        {
            List<List<InlineKeyboardButton>> btns = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn = new InlineKeyboardButton("💰 پرداخت از کیف پول");
            btn.CallbackData = "AccpetWallet";
            row1.Add(btn);
            btns.Add(row1);

            List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn2 = new InlineKeyboardButton("🔙 برگشت");
            btn2.CallbackData = "BackToCalc";
            row2.Add(btn2);
            btns.Add(row2);
            var keyborad = new InlineKeyboardMarkup(btns);

            return keyborad;
        }

        /// <summary>
        /// تابع آوردن دکمه تائید پرداخت
        /// </summary>
        /// <returns></returns>
        public static InlineKeyboardMarkup GetAccpetBuyUnlimtedFromWallet(int PlanId)
        {
            List<List<InlineKeyboardButton>> btns = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn = new InlineKeyboardButton("💰 پرداخت از کیف پول");
            btn.CallbackData = "AccpetWalletUnlimited%"+ PlanId;
            row1.Add(btn);
            btns.Add(row1);

            List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn2 = new InlineKeyboardButton("🔙 برگشت");
            btn2.CallbackData = "backToInfo%" + PlanId;
            row2.Add(btn2);
            btns.Add(row2);
            var keyborad = new InlineKeyboardMarkup(btns);

            return keyborad;
        }


        /// <summary>
        /// تابع آوردن دکمه انتخاب نوع اشتراک
        /// </summary>
        /// <returns></returns>
        public static InlineKeyboardMarkup GetSubTypeKey()
        {
            List<List<InlineKeyboardButton>> btns = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn = new InlineKeyboardButton("🥇 طلایی");
            btn.CallbackData = "gold";
            row1.Add(btn);
            

            InlineKeyboardButton btn2 = new InlineKeyboardButton("🥈 نقره ای");
            btn2.CallbackData = "premium";
            row1.Add(btn2);

            btns.Add(row1);

            List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();

            row2.Add(InlineKeyboardButton.WithCallbackData("⬅️ برگشت", "backToInfo"));
            btns.Add(row2);

            var keyborad = new InlineKeyboardMarkup(btns);

            return keyborad;
        }

        /// <summary>
        /// تابع آوردن دکمه انتخاب نوع اشتراک
        /// </summary>
        /// <returns></returns>
        public static InlineKeyboardMarkup GetSubTypeKeyTest()
        {
            List<List<InlineKeyboardButton>> btns = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn = new InlineKeyboardButton("🥇 طلایی");
            btn.CallbackData = "gold_test";
            row1.Add(btn);


            InlineKeyboardButton btn2 = new InlineKeyboardButton("🥈 نقره ای");
            btn2.CallbackData = "premium_test";
            row1.Add(btn2);

            btns.Add(row1);

            var keyborad = new InlineKeyboardMarkup(btns);

            return keyborad;
        }

        /// <summary>
        ///  تابع آوردن دکمه انتخاب نوع اشتراک برای آموزش خرید
        /// </summary>
        /// <returns></returns>
        public static InlineKeyboardMarkup GetSubTypeKeyForLearn()
        {
            List<List<InlineKeyboardButton>> btns = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
            InlineKeyboardButton btn = new InlineKeyboardButton("🥇 طلایی");
            btn.CallbackData = "goldLearn";
            row1.Add(btn);


            InlineKeyboardButton btn2 = new InlineKeyboardButton("🥈 نقره ای");
            btn2.CallbackData = "silverLearn";
            row1.Add(btn2);

            btns.Add(row1);

            var keyborad = new InlineKeyboardMarkup(btns);

            return keyborad;
        }
    }
}