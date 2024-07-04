using DataLayer.DomainModel;
using DataLayer.Repository;
using DeviceDetectorNET.Class;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Web.Mvc;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using V2boardApi.Tools;
using V2boardBot.Models;

namespace V2boardApi.Areas.App.Controllers
{

    public class BotController : Controller
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Database Instance
        private static Entities db;
        //Home Keys
        private static ReplyKeyboardMarkup inlineKeyboardMarkup;

        //Timer Update info USD
        private static System.Timers.Timer timer;



        private System.Timers.Timer CheckLink;
        private System.Timers.Timer CheckRenewAccount;
        private System.Timers.Timer DeleteTestAccount;


        private static Repository<tbTelegramUsers> tbTelegramUserRepository;
        private static Repository<tbBotSettings> BotSettingRepository;
        private static Repository<tbServers> tbServerRepository;
        private static Repository<tbLinks> tbLinksRepository;
        private static Repository<tbPlans> tbPlansRepository;
        private static Repository<tbUsers> tbUsersRepository;
        private static Repository<tbOrders> tbOrdersRepository;
        private static Repository<tbLinkUserAndPlans> RepositoryLinkUserAndPlan;
        private static Repository<tbDepositWallet_Log> tbDepositLogRepo;
        private static tbServers Server;

        private static string StaticToken { get; set; }
        private static string RobotIDforTimer { get; set; }

        public BotController()
        {
            db = new Entities();

            #region ریپازیتوری های دیتابیس
            tbTelegramUserRepository = new Repository<tbTelegramUsers>(db);
            tbServerRepository = new Repository<tbServers>(db);
            tbLinksRepository = new Repository<tbLinks>(db);
            tbPlansRepository = new Repository<tbPlans>(db);
            tbUsersRepository = new Repository<tbUsers>(db);
            tbOrdersRepository = new Repository<tbOrders>(db);
            RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
            tbDepositLogRepo = new Repository<tbDepositWallet_Log>(db);
            BotSettingRepository = new Repository<tbBotSettings>(db);
            #endregion


            timer = new System.Timers.Timer();
            timer.Interval = 600000;
            timer.Start();
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;
            Timer_Elapsed(null, null);

            CheckLink = new System.Timers.Timer();
            CheckLink.Interval = 86400000;
            CheckLink.Elapsed += CheckDolarPrice;
            CheckLink.Start();
            CheckLink.Enabled = true;

            CheckRenewAccount = new System.Timers.Timer();
            CheckRenewAccount.Interval = 300000;
            CheckRenewAccount.Elapsed += CheckRenewAccountFun;
            CheckRenewAccount.Start();
            CheckRenewAccount.Enabled = true;

            DeleteTestAccount = new System.Timers.Timer();
            DeleteTestAccount.Interval = 86400000;
            DeleteTestAccount.Elapsed += DeleteTestSub;
            DeleteTestAccount.Start();
            DeleteTestAccount.Enabled = true;

        }

        #region تابع چک کردن قیمت دلار

        private static void CheckDolarPrice(object sender, ElapsedEventArgs e)
        {
            //var price = Utility.GetPriceUSDT();
            //DolarPrice = price;
        }

        #endregion

        #region چک کردن کاربری که حجمش نزدیک به اتمام برای اطلاع دادن 
        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {

                if (BotManager.Server != null)
                {
                    using (var _db = new Entities())
                    {
                        var tbTelegramUserRepository3 = new Repository<tbTelegramUsers>(_db);
                        var Users = tbTelegramUserRepository3.Where(p => p.Tel_RobotID == RobotIDforTimer);
                        tbOrdersRepository = new Repository<tbOrders>(_db);

                        MySqlEntities mySql = new MySqlEntities(BotManager.Server.ConnectionString);
                        mySql.Open();
                        foreach (var item in Users.ToList())
                        {
                            try
                            {
                                var BotSetting = item.tbUsers.tbBotSettings.FirstOrDefault();
                                var bot = BotManager.GetBot(item.tbUsers.Username);
                                if(bot!= null)
                                {

                                    var joined = bot.Client.GetChatMemberAsync("@" + BotSetting.ChannelID, Convert.ToInt64(item.Tel_UniqUserID));

                                    if (joined.Result != null && (joined.Result.Status == ChatMemberStatus.Left || joined.Result.Status == ChatMemberStatus.Administrator || joined.Result.Status == ChatMemberStatus.Kicked))
                                    {
                                        foreach (var link in item.tbLinks.Where(p => p.tbL_Warning == false && p.tb_AutoRenew == false).ToList())
                                        {

                                            var Order = tbOrdersRepository.Where(p => p.AccountName == link.tbL_Email && p.OrderStatus == "FOR_RESERVE").ToList();
                                            if (Order.Count == 0)
                                            {
                                                var reader = mySql.GetData("select u,d,transfer_enable,banned,expired_at from v2_user where email='" + link.tbL_Email + "'");
                                                while (reader.Read())
                                                {


                                                    var bannd = reader.GetBoolean("banned");
                                                    if (!bannd)
                                                    {
                                                        var vol = reader.GetInt64("transfer_enable") - (reader.GetDouble("d") + reader.GetDouble("u"));

                                                        var d = Utility.ConvertByteToGB(vol);

                                                        if (d <= 1)
                                                        {
                                                            StringBuilder st = new StringBuilder();
                                                            st.AppendLine("<b>" + "اشتراک : " + link.tbL_Email.Split('@')[0] + "</b>");
                                                            st.AppendLine("");
                                                            st.Append("درحال اتمام حجم بسته می باشد لطفا هرچه سریعتر نسبت به تمدید اقدام کنید");
                                                            bot.Client.SendTextMessageAsync(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
                                                            link.tbL_Warning = true;
                                                            tbTelegramUserRepository3.Save();
                                                        }

                                                        var exp = reader.GetBodyDefinition("expired_at");
                                                        if (exp != "")
                                                        {
                                                            var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp));
                                                            if (ex <= DateTime.Now.AddDays(-2))
                                                            {
                                                                StringBuilder st = new StringBuilder();
                                                                st.AppendLine("<b>" + "اشتراک : " + link.tbL_Email.Split('@')[0] + "</b>");
                                                                st.AppendLine("");
                                                                st.AppendLine(" درحال اتمام زمان بسته می باشد لطفا هرچه سریعتر نسبت به تمدید اقدام کنید");
                                                                bot.Client.SendTextMessageAsync(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
                                                                link.tbL_Warning = true;
                                                                tbTelegramUserRepository3.Save();
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        StringBuilder st = new StringBuilder();
                                                        st.AppendLine("<b>" + "اشتراک : " + link.tbL_Email.Split('@')[0] + "</b>");
                                                        st.AppendLine("");
                                                        st.AppendLine("توسط ادمین مسدود شد برای دانستن علت مسدودی به پشتیبانی پیام دهید");
                                                        bot.Client.SendTextMessageAsync(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
                                                        link.tbL_Warning = true;
                                                        tbTelegramUserRepository3.Save();
                                                    }

                                                }
                                                reader.Close();
                                            }



                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex);
                            }




                        }
                        mySql.Close();


                    }
                }


            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        #endregion

        #region تابع حذف کردن اشتراکات تست

        private static void DeleteTestSub(object sender, ElapsedEventArgs e)
        {
            var Bots = BotManager.GetAllBots();
            
            foreach(var item in Bots)
            {
                var botsetting = BotSettingRepository.Where(p => p.Bot_Token == item.Token && p.Active == true && p.ChannelID != null).FirstOrDefault();
                if (botsetting != null)
                {
                    MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);
                    mySql.Open();
                    var reader = mySql.GetData("DELETE from v2_user where ((v2_user.d + v2_user.u) > v2_user.transfer_enable or expired_at < UNIX_TIMESTAMP()) and email like '" + botsetting.Bot_ID + "%'");
                    reader.Read();
                    reader.Close();
                    mySql.Close();
                }
            }

        }

        #endregion

        #region تایمر رزرو تمدید اکانت
        public static void CheckRenewAccountFun(object sender, ElapsedEventArgs e)
        {
            try
            {

                using (var db = new Entities())
                {
                    var tbOrdersRepository = new Repository<tbOrders>(db);
                    var tbLinksRepository = new Repository<tbLinks>(db);
                    var tbServerRepository = new Repository<tbServers>(db);

                    var Order = tbOrdersRepository.Where(p => p.OrderType == "تمدید" && p.OrderStatus == "FOR_RESERVE" && p.tbTelegramUsers.Tel_RobotID == RobotIDforTimer).ToList();

                    if (Order.Count >= 1)
                    {

                        MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);

                        mySql.Open();
                        foreach (var item in Order)
                        {
                            var bot = BotManager.GetBot(item.tbTelegramUsers.tbUsers.Username);
                            if (bot != null)
                            {
                                var Reader = mySql.GetData("select * from v2_user where email like '" + item.AccountName + "'");
                                var Read = Reader.Read();
                                if (Read)
                                {
                                    var d = Reader.GetDouble("d");
                                    var u = Reader.GetDouble("u");
                                    var totalUsed = Reader.GetDouble("transfer_enable");

                                    var total = Math.Round(Utility.ConvertByteToGB(totalUsed - (d + u)), 2);
                                    var exp2 = Reader.GetBodyDefinition("expired_at");
                                    var Ended = false;
                                    if (!string.IsNullOrWhiteSpace(exp2))
                                    {
                                        var expireTime = DateTime.Now;
                                        var ExpireDate = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp2)).AddHours(-3);
                                        if (ExpireDate <= expireTime)
                                        {
                                            Ended = true;
                                        }
                                    }
                                    if (total <= 0.2)
                                    {
                                        Ended = true;
                                    }
                                    Reader.Close();
                                    if (Ended)
                                    {
                                        var Link = tbLinksRepository.Where(p => p.tbL_Email == item.AccountName).FirstOrDefault();
                                        if (Link != null)
                                        {
                                            var t = Utility.ConvertGBToByte(Convert.ToInt64(item.Traffic));

                                            string exp = DateTime.Now.AddDays((int)item.Month * 30).ConvertDatetimeToSecond().ToString();
                                            Link.tbL_Warning = false;

                                            var Query = "update v2_user set u=0,d=0,t=0,plan_id=" + item.V2_Plan_ID + ",transfer_enable=" + t + ",expired_at=" + exp + " where email='" + item.AccountName + "'";
                                            var reader = mySql.GetData(Query);
                                            var result = reader.Read();
                                            reader.Close();



                                            bot.Client.SendTextMessageAsync(Link.tbTelegramUsers.Tel_UniqUserID, "✅ اکانت شما با موفقیت تمدید شد از بخش سرویس ها جزئیات اکانت را می توانید مشاهده کنید");
                                            var InlineKeyboardMarkup = Keyboards.GetHomeButton();
                                            Link.tbL_Warning = false;
                                            Link.tb_AutoRenew = false;
                                            item.OrderStatus = "FINISH";
                                            tbLinksRepository.Save();
                                            tbUsersRepository.Save();
                                            tbOrdersRepository.Save();
                                            tbTelegramUserRepository.Save();

                                        }
                                    }
                                }

                                Reader.Close();
                            }

                        }

                        mySql.Close();
                    }
                }



            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

        }

        #endregion

        [HttpPost]
        public async Task<ActionResult> Update(string botName, Update update)
        {
            try
            {
                var bot = BotManager.GetBot(botName);
                if (bot == null)
                {
                    logger.Info("ربات پیدا نشد");
                    return new HttpStatusCodeResult(404, "Bot not found");
                }

                if (bot.Started)
                {
                    if (update == null || update.Type != Telegram.Bot.Types.Enums.UpdateType.Message)
                    {
                        logger.Info("کلاس update پیدا نشد");
                        return new HttpStatusCodeResult(400);
                    }

                    var message = update.Message;
                    if (message?.Text != null)
                    {
                        await bot.Client.SendTextMessageAsync(message.Chat.Id, "You said:\n" + message.Text + " " + botName);
                    }
                }
                else
                {
                    StringBuilder str2 = new StringBuilder();
                    str2.AppendLine("⚠️ با عرض پوزش ربات برای مدت کمی از دسترس خارج شده لطفا بعدا تلاش فرمائید");
                    if (update.CallbackQuery != null)
                    {
                        await bot.Client.SendTextMessageAsync(update.CallbackQuery.From.Id, str2.ToString(), parseMode: ParseMode.Html);
                    }
                    else
                    {
                        await bot.Client.SendTextMessageAsync(update.Message.From.Id, str2.ToString(), parseMode: ParseMode.Html);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("خطا در برقراری ارتباط سرور تلگرام", ex);
            }

            return new HttpStatusCodeResult(200);
        }
    }
}