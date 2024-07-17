using DataLayer.DomainModel;
using DataLayer.Repository;
using DeviceDetectorNET.Class;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Transactions;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using V2boardApi.Tools;
using V2boardBot.Functions;
using V2boardBot.Models;
using V2boardBotApp.Models;
using V2boardBotApp.Models.ViewModels;

namespace V2boardApi.Areas.api.Controllers
{
    [System.Web.Http.AllowAnonymous]
    [LogActionFilter]
    public class BotController : ApiController
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
            Server = BotManager.Server;
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
                                if (bot != null)
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

            foreach (var item in Bots)
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

                    var Order = tbOrdersRepository.Where(p => p.OrderType == "تمدید" && p.OrderStatus == "FOR_RESERVE").ToList();

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


                                        var Link = item.tbTelegramUsers.tbLinks.Where(p => p.tbL_Email == item.AccountName).FirstOrDefault();
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

        [System.Web.Http.HttpPost]

        public async Task Update(string botName, Update update)
        {
            try
            {

                var bot = BotManager.GetBot(botName);
                if (bot == null)
                {
                    logger.Info("ربات پیدا نشد");
                    return;
                }

                if (bot.Started)
                {
                    if (update == null)
                    {
                        logger.Info("کلاس update پیدا نشد");
                        return;
                    }

                    db = new Entities();
                    BotSettingRepository = new Repository<tbBotSettings>(db);
                    var BotSettings = BotSettingRepository.Where(p => p.tbUsers.Username == botName).FirstOrDefault();

                    if (BotSettings.Active == true && BotSettings.tbUsers.Wallet <= BotSettings.tbUsers.Limit)
                    {
                        //nowPayment.UpdateNowPaymentModel(db);
                        #region ریپازیتوری های دیتابیس
                        tbTelegramUserRepository = new Repository<tbTelegramUsers>(db);
                        tbServerRepository = new Repository<tbServers>(db);
                        tbLinksRepository = new Repository<tbLinks>(db);
                        tbPlansRepository = new Repository<tbPlans>(db);
                        tbUsersRepository = new Repository<tbUsers>(db);
                        tbOrdersRepository = new Repository<tbOrders>(db);
                        RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
                        tbDepositLogRepo = new Repository<tbDepositWallet_Log>(db);

                        #endregion
                        long chatid = 0;
                        tbTelegramUsers UserAcc = new tbTelegramUsers();
                        if (update.Message is Telegram.Bot.Types.Message message)
                        {
                            chatid = update.Message.From.Id;
                            var mess = message.Text;

                            #region ارسال رسید تراکنش برای ادمین

                            if (update.Message.Type == MessageType.Photo)
                            {
                                var fileId = message.Photo[message.Photo.Length - 1].FileId; // Get the highest quality photo
                                var filed = await bot.Client.GetFileAsync(fileId);

                                var file = InputFile.FromFileId(fileId);
                                StringBuilder str = new StringBuilder();
                                str.AppendLine("ارسالی از");
                                str.Append(" 👤 ");
                                str.AppendLine(update.Message.From.Username + " (" + update.Message.From.FirstName + update.Message.From.LastName + ")");
                                str.AppendLine("");
                                str.Append("✅");
                                str.Append("برای تایید تراکنش مبلغ تراکنش را با ");
                                str.Append("/p-");
                                str.Append(" وارد کنید ");
                                str.AppendLine("");
                                str.AppendLine("");
                                str.Append("♨️");
                                str.AppendLine("مثال : ");
                                str.Append("/p-400485");
                                str.AppendLine("");
                                str.AppendLine("");
                                if (update.Message.Caption != null)
                                {
                                    if (update.Message.Caption.Length >= 1)
                                    {
                                        str.AppendLine("💬 متن کاربر : " + update.Message.Caption);
                                    }
                                }

                                await bot.Client.SendPhotoAsync(BotSettings.AdminBot_ID, file, parseMode: ParseMode.Html, caption: str.ToString());
                                return;
                            }

                            #endregion

                            if (chatid.ToString() == BotSettings.AdminBot_ID.ToString())
                            {
                                #region تایید تراکنش توسط ادمین

                                if ((bool)BotSettings.IsActiveSendReceipt)
                                {
                                    if (mess.Contains("/p-"))
                                    {
                                        var sp = mess.Split('-');
                                        if (sp.Length == 2)
                                        {
                                            var sp2 = sp[1];
                                            var num = int.TryParse(sp2, out int num2);
                                            if (num)
                                            {
                                                var date2 = DateTime.Now.AddDays(-2);
                                                var tbDepositLog = tbDepositLogRepo.Where(p => p.dw_Price == num2 && p.dw_Status == "FOR_PAY" && p.dw_CreateDatetime >= date2).ToList();

                                                foreach (var item in tbDepositLog)
                                                {
                                                    item.dw_Status = "FINISH";
                                                    item.tbTelegramUsers.Tel_Wallet += item.dw_Price / 10;
                                                    StringBuilder str = new StringBuilder();
                                                    str.AppendLine("✅ کیف پول شما با موفقیت شارژ شد");
                                                    str.AppendLine("");
                                                    str.AppendLine("💳 موجودی کیف پول شما : " + item.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                                    str.AppendLine("");
                                                    str.AppendLine("❗️ الان می تونید برای خرید یا تمدید اقدام کنید");

                                                    var keyboard = new ReplyKeyboardMarkup(new[]
                                                {
                            new[]
                            {

                                new KeyboardButton("💰 خرید سرویس"),
                                new KeyboardButton("💸 تمدید سرویس"),
                                new KeyboardButton("⚙️ سرویس ها")
                            },new[]
                            {
                                new KeyboardButton("👜 کیف پول"),
                                new KeyboardButton("📊 تعرفه ها"),
                                new KeyboardButton("♨️ اشتراک تست"),
                            },
                            new[]
                            {
                                new KeyboardButton("🔗 اضافه کردن لینک"),
                                new KeyboardButton("📚 راهنمای اتصال"),
                            },
                            new[]
                            {
                                new KeyboardButton("📞 ارتباط با پشتیبانی"),
                                new KeyboardButton("❔ سوالات متداول"),
                            }

                        });

                                                    keyboard.IsPersistent = true;
                                                    keyboard.ResizeKeyboard = true;
                                                    keyboard.OneTimeKeyboard = false;

                                                    RealUser.SetUserStep(item.tbTelegramUsers.Tel_UniqUserID, "Start", db, item.tbTelegramUsers.tbUsers.Username);
                                                    var botSetting = BotSettings;
                                                    if (botSetting != null)
                                                    {
                                                        TelegramBotClient botClient = new TelegramBotClient(botSetting.Bot_Token);
                                                        tbDepositLogRepo.Save();
                                                        await botClient.SendTextMessageAsync(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
                                                        

                                                        logger.Info("فاکتور به مبلغ " + num2.ConvertToMony() + " با موفقیت پرداخت شد");
                                                        return;
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }

                                #endregion

                                #region ارسال پیغام همگانی توسط ادمین
                                if (update.Message.Type == MessageType.Photo && message.Caption != null)
                                {
                                    if (message.Caption.StartsWith("/all"))
                                    {
                                        var fileId = message.Photo.Last().FileId;
                                        var file = InputFile.FromFileId(fileId);

                                        foreach (var item in BotSettings.tbUsers.tbTelegramUsers.ToList())
                                        {
                                            try
                                            {
                                                message.Caption = message.Caption.Replace("/all", "");
                                                await bot.Client.SendPhotoAsync(item.Tel_UniqUserID, file, caption: message.Caption, parseMode: ParseMode.Html);
                                            }
                                            catch (Exception ex)
                                            {

                                            }
                                        }
                                    }
                                }
                                else if (update.Message.Type == MessageType.Video && message.Caption != null)
                                {
                                    if (message.Caption.StartsWith("/all"))
                                    {
                                        var fileId = message.Video.FileId;
                                        var file = InputFile.FromFileId(fileId);

                                        foreach (var item in BotSettings.tbUsers.tbTelegramUsers.ToList())
                                        {
                                            try
                                            {
                                                message.Caption = message.Caption.Replace("/all", "");
                                                await bot.Client.SendVideoAsync(item.Tel_UniqUserID, file, caption: message.Caption, parseMode: ParseMode.Html);
                                            }
                                            catch (Exception ex)
                                            {

                                            }
                                        }
                                    }
                                }
                                else if (update.Message.Type == MessageType.Document && message.Caption != null)
                                {
                                    if (message.Caption.StartsWith("/all"))
                                    {
                                        var fileId = message.Document.FileId;
                                        var file = InputFile.FromFileId(fileId);

                                        foreach (var item in BotSettings.tbUsers.tbTelegramUsers.ToList())
                                        {
                                            try
                                            {
                                                message.Caption = message.Caption.Replace("/all", "");
                                                await bot.Client.SendDocumentAsync(item.Tel_UniqUserID, file, caption: message.Caption, parseMode: ParseMode.Html);
                                            }
                                            catch (Exception ex)
                                            {

                                            }
                                        }
                                    }
                                }
                                else if (update.Message.Type == MessageType.Text && message.Text != null)
                                {
                                    if (message.Text.StartsWith("/all"))
                                    {
                                        foreach (var item in BotSettings.tbUsers.tbTelegramUsers.ToList())
                                        {
                                            try
                                            {
                                                message.Text = message.Text.Replace("/all", "");
                                                await bot.Client.SendTextMessageAsync(item.Tel_UniqUserID, message.Text, parseMode: ParseMode.Html);
                                            }
                                            catch (Exception ex)
                                            {

                                            }
                                        }
                                    }
                                }

                                #endregion
                            }
                            if (update.Message.Type == MessageType.Text)
                            {

                                #region چک کردن اینکه آیا کاربر وجود دارد در دیتابیس یا نه
                                var User = tbTelegramUserRepository.Where(p => p.Tel_UniqUserID == chatid.ToString() && p.tbUsers.Username == botName).FirstOrDefault();
                                if (User == null)
                                {
                                    tbTelegramUsers Usr = new tbTelegramUsers();
                                    Usr.Tel_UniqUserID = chatid.ToString();
                                    Usr.Tel_Step = "Start";
                                    Usr.Tel_Wallet = 0;
                                    Usr.Tel_Monthes = 1;
                                    Usr.Tel_Traffic = 20;
                                    if (message.From.Username != null)
                                    {
                                        Usr.Tel_Username = message.From.Username;
                                    }

                                    if (message.From.LastName != null)
                                    {
                                        Usr.Tel_LastName = message.From.LastName;
                                    }
                                    if (message.From.FirstName != null)
                                    {
                                        Usr.Tel_FirstName = message.From.FirstName;
                                    }
                                    if (mess != "/start")
                                    {
                                        var inites = mess.Split(' ');
                                        if (inites.Count() == 2)
                                        {
                                            var id = inites[1];
                                            var Parent = tbTelegramUserRepository.Where(p => p.Tel_UniqUserID == id && p.tbUsers.Username == botName).FirstOrDefault();
                                            if (Parent != null)
                                            {
                                                Usr.Tel_Parent_ID = Parent.Tel_UserID;
                                            }
                                        }
                                    }


                                    Usr.Tel_RobotID = RobotIDforTimer;

                                    Usr.FK_User_ID = BotSettings.tbUsers.User_ID;
                                    tbTelegramUserRepository.Insert(Usr);
                                    tbTelegramUserRepository.Save();
                                    UserAcc = Usr;
                                }
                                else
                                {
                                    if (User.Tel_RobotID == null)
                                    {
                                        User.Tel_RobotID = RobotIDforTimer;
                                        tbTelegramUserRepository.Save();
                                    }
                                    var edited = false;
                                    if (message.From.Username != User.Tel_Username)
                                    {
                                        User.Tel_Username = message.From.Username;
                                        edited = true;
                                    }

                                    if (message.From.LastName != User.Tel_LastName)
                                    {
                                        User.Tel_LastName = message.From.LastName;
                                        edited = true;
                                    }
                                    if (message.From.FirstName != User.Tel_FirstName)
                                    {
                                        User.Tel_FirstName = message.From.FirstName;
                                        edited = true;
                                    }

                                    if (edited)
                                    {
                                        tbTelegramUserRepository.Save();
                                    }

                                    UserAcc = User;
                                    RealUser.SetUpdateMessageTime(User.Tel_UniqUserID, db, DateTime.UtcNow, botName);



                                }
                                #endregion

                                #region چک کردن آیا کاربر در کانال عضو آست یا خیر

                                if (BotSettings.ChannelID != null)
                                {
                                    if (BotSettings.RequiredJoinChannel == true)
                                    {

                                        var joined = await bot.Client.GetChatMemberAsync("@" + BotSettings.ChannelID, Convert.ToInt64(UserAcc.Tel_UniqUserID));

                                        if (joined != null && (joined.Status == ChatMemberStatus.Left || joined.Status == ChatMemberStatus.Kicked))
                                        {
                                            StringBuilder str = new StringBuilder();

                                            if (BotSettings.tbUsers.BussinesTitle != null)
                                            {
                                                str.AppendLine("کاربر عزیز به " + BotSettings.tbUsers.BussinesTitle + " " + "خوش آمدید");
                                            }
                                            else
                                            {
                                                str.AppendLine("کاربر عزیز به ربات ما خوش آمدید");
                                            }
                                            str.AppendLine("");
                                            str.AppendLine("لطفا برای استفاده از خدمات ما در کانال ما " + "@" + BotSettings.ChannelID + " " + "عضو شوید");
                                            str.AppendLine("");
                                            str.AppendLine("بعد از عضو شدن در کانال روی گزینه عضو شدم کلیک کنید");

                                            InlineKeyboardButton inlineKeyboardButton = new InlineKeyboardButton("عضو شدم");
                                            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
                                            inlineKeyboardButton.CallbackData = "Joined";
                                            buttons.Add(inlineKeyboardButton);

                                            var key = new InlineKeyboardMarkup(buttons);
                                            await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: key);
                                            return;
                                        }
                                    }
                                }

                                #endregion

                                #region بخش نمایش منو اصلی
                                if (mess == "/start" || mess.StartsWith("/start") || mess.StartsWith("⬅️ برگشت به صفحه اصلی"))
                                {

                                    inlineKeyboardMarkup = Keyboards.GetHomeButton();


                                    StringBuilder st = new StringBuilder();

                                    if (BotSettings.tbUsers.BussinesTitle != null)
                                    {
                                        st.AppendLine("سلام به ربات " + " <b>" + BotSettings.tbUsers.BussinesTitle + "</b> " + "خوش آمدید");
                                    }
                                    else
                                    {
                                        st.AppendLine("کاربر عزیز به ربات ما خوش آمدید");
                                    }

                                    st.AppendLine("");
                                    st.AppendLine("📌 جهت استفاده از ربات لطفا یکی از گزینه های زیر را انتخاب کنید:");
                                    st.AppendLine("");
                                    st.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                    RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                    var task = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, st.ToString(), replyMarkup: inlineKeyboardMarkup, replyToMessageId: message.MessageId, parseMode: ParseMode.Html);


                                    return;





                                }
                                #endregion

                                #region بخش چک کردن برای اینکه کاربر نام اکانت جدید خودش را وارد کند
                                else
                                if (UserAcc.Tel_Step == "WaitForEnterName")
                                {
                                    if (mess.Length < 15)
                                    {
                                        if (CheckExistsAccountInBot(mess, BotSettings))
                                        {
                                            SendTrafficCalculator(UserAcc, message.MessageId, BotSettings, bot.Client, botName, mess, Tel_Step: User.Tel_Step);
                                        }
                                        else
                                        {
                                            StringBuilder str = new StringBuilder();
                                            str.AppendLine("❌ این نام قبلا در سیستم وجود دارد لطفا نام دیگری وارد کنید");
                                            var backkey = Keyboards.GetBackButton();
                                            await bot.Client.SendTextMessageAsync(message.From.Id, str.ToString(), replyMarkup: backkey, replyToMessageId: message.MessageId);
                                        }

                                    }
                                    else
                                    {
                                        StringBuilder str1 = new StringBuilder();
                                        str1.AppendLine("تعداد کاراکتر های نام شما باید کمتر از 15 باشد");

                                        var backkey = Keyboards.GetBackButton();
                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForEnterName", db, botName);
                                        await bot.Client.SendTextMessageAsync(message.From.Id, str1.ToString(), replyMarkup: backkey, replyToMessageId: message.MessageId);
                                    }
                                }

                                #endregion

                                #region بخش خرید سرویس و تمدید

                                #region بخش فشردن گزینه خرید سرویس

                                else if (mess == "💰 خرید سرویس")
                                {
                                    RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                    if (UserAcc.tbLinks.Any())
                                    {
                                        StringBuilder str1 = new StringBuilder();
                                        str1.AppendLine("لطفا نامی برای اشتراک جدید خود وارد کنید");
                                        str1.AppendLine("");
                                        str1.AppendLine("❗️نام اشتراک شما باید انگلیسی باشد");
                                        str1.AppendLine("❗️حداکثر باید 15 کاراکتر وارد کنید");

                                        var backkey = Keyboards.GetBackButton();
                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForEnterName", db, botName);
                                        await bot.Client.SendTextMessageAsync(message.From.Id, str1.ToString(), replyMarkup: backkey, replyToMessageId: message.MessageId);
                                        return;
                                    }
                                    else
                                    {
                                        SendTrafficCalculator(UserAcc, message.MessageId, BotSettings, bot.Client, botName);
                                    }



                                    return;



                                }

                                #endregion

                                #region دکمه سرویس ها
                                if (mess == "⚙️ سرویس ها")
                                {
                                    var keyboard = Keyboards.GetServiceLinksKeyboard(UserAcc.Tel_UserID, tbLinksRepository);
                                    if (keyboard == null)
                                    {
                                        StringBuilder str2 = new StringBuilder();
                                        str2.AppendLine("❌ شما سرویسی ندارید");
                                        str2.AppendLine("");
                                        str2.AppendLine("🆔 @" + BotSettings.Bot_ID);

                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str2.ToString(), replyToMessageId: message.MessageId); return;
                                    }
                                    RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Select_AccountForShowInfo", db, botName);
                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("💢 لطفا اشتراک مورد نظر را انتخاب کنید 👇");
                                    str.AppendLine("");
                                    str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                    var editedMessage = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, text: str.ToString(), replyToMessageId: message.MessageId, replyMarkup: keyboard);
                                    return;
                                }
                                #endregion

                                #region بخش تمدید سرویس 

                                #region بخش فشار دادن دکمه تمدید
                                else if (mess == "💸 تمدید سرویس")
                                {
                                    #region بخش نمایش لینک های موجود کاربر
                                    RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                    var keyboard = Keyboards.GetServiceLinksKeyboard(UserAcc.Tel_UserID, tbLinksRepository);
                                    if (keyboard == null)
                                    {
                                        StringBuilder str2 = new StringBuilder();
                                        str2.AppendLine("❌ شما سرویسی ندارید");
                                        str2.AppendLine("");
                                        str2.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str2.ToString());
                                        return;
                                    }
                                    RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectAccount", db, botName);

                                    StringBuilder str3 = new StringBuilder();
                                    str3.AppendLine("♨️  لطفا اشتراک مورد نظر را انتخاب کنید");
                                    str3.AppendLine("");
                                    str3.AppendLine("🆔 @" + BotSettings.Bot_ID);

                                    var editedMessage = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str3.ToString(), replyMarkup: keyboard, replyToMessageId: message.MessageId);
                                    return;
                                    #endregion

                                }
                                #endregion

                                #endregion

                                #endregion

                                #region بخش منتظر بودن برای کاربر که مبلغ واریزی جهت افزایش کیف پول را وارد کند

                                #region شارژ کارت به کارت

                                if (UserAcc.Tel_Step == "Wait_For_Type_IncreasePrice")
                                {
                                    var price = 0;

                                    int.TryParse(mess, out price);
                                    if (price >= 10000 && price <= 5000000)
                                    {
                                        Random ran = new Random();
                                        var RanNumber = ran.Next(1, 999);

                                        var fullPrice = (price * 10) + RanNumber;
                                        StringBuilder str = new StringBuilder();

                                        var FirstCard = BotSettings.tbUsers.tbBankCardNumbers.Where(p => p.Active == true).FirstOrDefault();

                                        str.AppendLine("✅ فاکتور افزایش موجودی کیف پول شما  :");
                                        str.AppendLine("");
                                        str.AppendLine(" لطفا مبلغ " + "<code>" + fullPrice.ConvertToMony() + "</code>" + " ریال " + " را به شماره کارت " + FirstCard.CardNumber + " " + " به نام " + FirstCard.InTheNameOf + " واریز نمائید");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ روی مبلغ کلیک کنید مبلغ کپی می شود و نیازی به حفظ نیست");
                                        if((bool)BotSettings.IsActiveSendReceipt && (bool)BotSettings.IsActiveCardToCard)
                                        {
                                            str.AppendLine("❗️حتما حتما مبلغ را دقیق با سه رقم اخر واریز کنید در غیر اینصورت ربات واریزی شمارو تشخیص نمی دهد");
                                            str.AppendLine("");
                                            str.AppendLine("❗️ در صورت عدم تایید خودکار رسید واریزیتون رو برای ربات بفرستید");
                                        }
                                        else
                                        {
                                            if ((bool)BotSettings.IsActiveCardToCard)
                                            {
                                                str.AppendLine("❗️حتما حتما مبلغ را دقیق با سه رقم اخر واریز کنید در غیر اینصورت ربات واریزی شمارو تشخیص نمی دهد");
                                            }
                                            if ((bool)BotSettings.IsActiveSendReceipt)
                                            {
                                                str.AppendLine("");
                                                str.Append("✅");
                                                str.AppendLine("بعد واریزی حتما رسید را برای ربات بفرستید");
                                            }
                                        }
                                        tbDepositWallet_Log tbDeposit = new tbDepositWallet_Log();
                                        tbDeposit.dw_Price = fullPrice;
                                        tbDeposit.dw_CreateDatetime = DateTime.Now;
                                        tbDeposit.dw_Status = "FOR_PAY";
                                        tbDeposit.FK_TelegramUser_ID = UserAcc.Tel_UserID;
                                        tbDepositLogRepo.Insert(tbDeposit);
                                        tbDepositLogRepo.Save();
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Pay_IncreasePrice", db, botName);
                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                        return;
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("❌ فرمت مبلغ اشتباه");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ نکته : بازه مبلغ واریزی بین 10,000 تومان تا 5,000,000 تومان می باشد");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ مبلغ را بدون گذاشتن , وارد کنید");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ مبلغ را با اعداد انگلیسی وارد کنید");
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreasePrice", db, botName);

                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                        return;
                                    }
                                }

                                #endregion

                                #region شارژ ارز دیجیتال

                                if (UserAcc.Tel_Step == "Wait_For_Type_IncreasePriceRial")
                                {

                                    await bot.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "⚠️ این روش موقتا از دسترس خارج شده لطفا از روش کارت به کارت استفاده کنید", false);

                                    //var price = 0;

                                    //int.TryParse(mess, out price);
                                    //if (price >= 50000 && price <= 1000000)
                                    //{

                                    //    nowPayment.CreateIncreaseWalletPayment(bot, price, UserAcc.Tel_UserID, StaticToken, UserAcc.Tel_UniqUserID, DolarPrice);
                                    //    return;
                                    //}
                                    //else
                                    //{
                                    //    StringBuilder str = new StringBuilder();
                                    //    str.AppendLine("❌ فرمت مبلغ اشتباه");
                                    //    str.AppendLine("");
                                    //    str.AppendLine("❗️ نکته : بازه مبلغ واریزی بین 50,000 تومان تا 1,000,000 تومان می باشد");
                                    //    str.AppendLine("❗️ مبلغ را بدون گذاشتن , وارد کنید");

                                    //    RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreasePriceRial", db);

                                    //    await bot.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);
                                    //    return;
                                    //}
                                }

                                #endregion

                                #endregion

                                #region نمایش تعرفه ها

                                if (mess == "📊 تعرفه ها")
                                {

                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("");
                                    str.AppendLine("💸 هر گیگ : " + BotSettings.PricePerGig_Major.Value.ConvertToMony() + " تومان");
                                    str.AppendLine("⏳ هر ماه : " + BotSettings.PricePerMonth_Major.Value.ConvertToMony() + " تومان");
                                    str.AppendLine("");
                                    str.AppendLine("📱 اشتراک ها بدون محدودیت کاربر می باشد");
                                    str.AppendLine("");
                                    str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                    await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);

                                }

                                #endregion

                                #region راهنمای اتصال

                                if (mess == "📚 راهنمای اتصال")
                                {

                                    if (BotSettings.tbUsers.tbConnectionHelp.Count > 0)
                                    {
                                        var Keys = Keyboards.GetHelpKeyboard(BotSettings.tbUsers.tbConnectionHelp.Where(p => p.ch_Type == "vpn").ToList());


                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("📲 لطفا با توجه به نوع دستگاه خود یکی از گزینه های زیر را انتخاب کنید");
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(chatid, str.ToString(), parseMode: ParseMode.Html, replyMarkup: Keys, replyToMessageId: message.MessageId);

                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectPlatform", db, botName);
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("❌ ربات فاقد آموزش می باشد لطفا به پشتیبانی پیام دهید");
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(chatid, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);

                                    }


                                }


                                #endregion

                                #region کیف پول

                                if (mess == "👜 کیف پول")
                                {
                                    if (UserAcc != null)
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("📌 موجودی کیف پول شما : " + UserAcc.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                        str.AppendLine("");

                                        var learns = BotSettings.tbUsers.tbConnectionHelp.Where(p => p.ch_Type == "crypto").ToList();
                                        foreach (var item in learns)
                                        {
                                            str.AppendLine(" <a href='" + item.ch_Link + "'>" + item.ch_Title + "</a>");
                                        }
                                        str.AppendLine("");
                                        str.AppendLine("✅ جهت شارژ کیف پول، لطفا یکی از روش های زیر را انتخاب کنید");
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();


                                        List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();
                                        row2.Add(InlineKeyboardButton.WithCallbackData("💳 کارت به کارت", "InventoryIncreaseCard"));
                                        row2.Add(InlineKeyboardButton.WithCallbackData("💳 پرداخت ریالی", "InventoryIncreaseRial"));
                                        inlineKeyboards.Add(row2);

                                        List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                        row1.Add(InlineKeyboardButton.WithCallbackData("زیر مجموعه گیری 👬", "InventoryIncreaseSub"));
                                        inlineKeyboards.Add(row1);

                                        var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboards);

                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Select_Way_To_Increase", db, botName);

                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, disableWebPagePreview: true, replyToMessageId: message.MessageId);

                                    }
                                }

                                #endregion

                                #region ارتباط با پشتیبانی

                                if (mess == "📞 ارتباط با پشتیبانی")
                                {

                                    StringBuilder str = new StringBuilder();
                                    var fullName = "";
                                    if (!string.IsNullOrWhiteSpace(UserAcc.Tel_FirstName))
                                    {
                                        fullName += UserAcc.Tel_FirstName;
                                    }
                                    if (!string.IsNullOrWhiteSpace(UserAcc.Tel_LastName))
                                    {
                                        fullName += UserAcc.Tel_LastName;
                                    }

                                    str.AppendLine(fullName + " " + "عزیز");
                                    str.AppendLine("");
                                    str.AppendLine("👇 برای ارتباط با پشتیبانی می توانید به آیدی زیر پیام دهید");
                                    str.AppendLine("");
                                    //str.AppendLine("📱 @" + BotSetting.);

                                    await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString());



                                }

                                #endregion

                                #region اشتراک تست

                                if (mess == "♨️ اشتراک تست")
                                {
                                    if (UserAcc.Tel_GetedTestAccount == false || UserAcc.Tel_GetedTestAccount == null)
                                    {
                                        MySqlEntities mySql = new MySqlEntities(BotSettings.tbUsers.tbServers.ConnectionString);
                                        mySql.Open();
                                        var reader = mySql.GetData("select group_id,transfer_enable from v2_plan where id =" + Server.DefaultPlanIdInV2board);
                                        long tran = 0;
                                        int grid = 0;
                                        while (reader.Read())
                                        {
                                            tran = Utility.ConvertGBToByte(0.5);
                                            grid = reader.GetInt32("group_id");
                                        }
                                        string create = DateTime.Now.ConvertDatetimeToSecond().ToString();
                                        string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                                        string exp = DateTime.Now.AddDays(1).ConvertDatetimeToSecond().ToString();
                                        reader.Close();
                                        var isExists = true;
                                        var FullName = "";
                                        while (isExists)
                                        {
                                            Random ran = new Random();
                                            FullName = BotSettings.Bot_ID + "$" + ran.Next(999) + "@" + BotSettings.tbUsers.Username;
                                            var reader2 = mySql.GetData("select * from v2_user where email='" + FullName + "'");
                                            if (!reader2.Read())
                                            {
                                                isExists = false;
                                            }
                                            reader2.Close();
                                        }
                                        string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at) VALUES ('" + FullName + "'," + exp + "," + create + ",'" + Guid.NewGuid() + "',0,0,0," + tran + ",0," + grid + "," + Server.DefaultPlanIdInV2board + ",'" + token + "','" + Guid.NewGuid() + "'," + create + ")";


                                        reader = mySql.GetData(Query);
                                        reader.Close();

                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("💢 اشتراک تست : " + FullName.Split('@')[0]);
                                        str.AppendLine("");
                                        str.AppendLine("🚦 حجم : 500 مگ");
                                        str.AppendLine("⏳ مدت زمان : یک روز");
                                        str.AppendLine("");
                                        str.AppendLine("لینک اشتراک :");
                                        str.AppendLine("👇👇👇👇👇👇👇");
                                        str.AppendLine("");
                                        var SubLink = "https://" + Server.SubAddress + "/api/v1/client/subscribe?token=" + token;
                                        str.AppendLine("<code>" + SubLink + "</code>");
                                        str.AppendLine("");
                                        RealUser.SetGetedAccountTest(User.Tel_UniqUserID, db, botName);
                                        mySql.Close();

                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(chatid, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("❌ شما قبلا اشتراک تست دریافت کرده اید");
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(chatid, str.ToString(), replyToMessageId: message.MessageId);
                                    }
                                }

                                #endregion

                                #region سوالات متداول

                                if (mess == "❔ سوالات متداول")
                                {
                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("⁉️ <b>سوالات متداول مربوط به سرویس</b>");
                                    str.AppendLine("");
                                    str.AppendLine("🔸 سرویس شما آیپی ثابت هست؟ نمی‌خوام آیپیم تغییر کنه❗️\r\n🔹 بله تا زمانی که مشکلی برای آیپی یا سرور پیش نیاید آیپی ثابت می‌ماند");
                                    str.AppendLine("");
                                    str.AppendLine("🔸 با چند تا دستگاه میتونم از سرویسم استفاده کنم؟\r\n🔹 سرویس ما محدودیت کاربر ندارد شما با هر چند دستگاه که بخواهید می‌تونید متصل شوید.");
                                    str.AppendLine(""); ;
                                    str.AppendLine("🔸<b>نیم بها</b> به چه معناست ؟");
                                    str.AppendLine("🔹 به این معناست که آن کانفیگ برای شما با ضریب نصف ( نیم بها ) محاسبه خواهد شد یعنی با هر 1 گیگ استفاده 500 مگابایت از حجم سرویستون کسر میشه");
                                    str.AppendLine(""); ;
                                    str.AppendLine("🔸 با خرید یک سرویس به چه لوکیشن های میتونم وصل بشم؟\r\n🔹 به همه لوکیشن های موجود در لیست زیر میتوانید متصل شوید");
                                    str.AppendLine("");
                                    str.AppendLine("لیست کامل سرور ها :");
                                    MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);
                                    mySql.Open();
                                    var reader = mySql.GetData("SELECT * FROM `v2_server_vmess` where show=1 ORDER by sort");
                                    var Counter = 1;
                                    while (reader.Read())
                                    {
                                        str.AppendLine(Counter + "- " + reader.GetString("name"));
                                        Counter++;
                                    }
                                    reader.Close();
                                    mySql.Close();
                                    str.AppendLine("");
                                    str.AppendLine("💬 اگر سوالی داشتید که پاسخ آن را نیافتید با پشتیبانی در ارتباط باشید.");
                                    str.AppendLine("");
                                    str.AppendLine("〰️〰️〰️〰️〰️");
                                    str.AppendLine("🚀@" + BotSettings.Bot_ID);

                                    await bot.Client.SendTextMessageAsync(chatid, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                }

                                #endregion
                            }
                        }
                        else
                        if (update.CallbackQuery != null)
                        {
                            var callbackQuery = update.CallbackQuery;
                            var callback = update.CallbackQuery.Data.Split('%');

                            #region چک کردن اینکه آیا کاربر وجود دارد در دیتابیس یا نه
                            var User = tbTelegramUserRepository.Where(p => p.Tel_UniqUserID == update.CallbackQuery.From.Id.ToString() && p.tbUsers.Username == botName).FirstOrDefault();
                            if (User == null)
                            {
                                tbTelegramUsers Usr = new tbTelegramUsers();
                                Usr.Tel_UniqUserID = update.CallbackQuery.From.Id.ToString();
                                if (update.CallbackQuery.From.Username != null)
                                {
                                    Usr.Tel_Username = update.CallbackQuery.From.Username;

                                }
                                if (update.CallbackQuery.From.LastName != null)
                                {
                                    Usr.Tel_LastName = update.CallbackQuery.From.LastName;

                                }

                                if (update.CallbackQuery.From.FirstName != null)
                                {
                                    Usr.Tel_FirstName = update.CallbackQuery.From.FirstName;

                                }

                                Usr.Tel_RobotID = RobotIDforTimer;
                                Usr.Tel_Wallet = 0;
                                Usr.Tel_UpdateMessage = DateTime.UtcNow;
                                tbTelegramUserRepository.Insert(Usr);
                                tbTelegramUserRepository.Save();
                                UserAcc = Usr;
                            }
                            else
                            {
                                if (UserAcc.Tel_RobotID == null)
                                {
                                    User.Tel_RobotID = callbackQuery.Message.From.Username;
                                    tbTelegramUserRepository.Save();
                                }
                                if (User.Tel_UpdateMessage == null)
                                {
                                    User.Tel_UpdateMessage = DateTime.UtcNow;
                                    RealUser.SetUpdateMessageTime(User.Tel_UniqUserID, db, DateTime.UtcNow, botName);
                                }
                                UserAcc = User;
                            }

                            #endregion

                            #region چک کردن که کاربر عضو است در کانال یا خیر

                            if (BotSettings.RequiredJoinChannel == true)
                            {
                                if (callbackQuery.Data == "Joined")
                                {

                                    var joined = await bot.Client.GetChatMemberAsync("@" + BotSettings.ChannelID, Convert.ToInt64(UserAcc.Tel_UniqUserID));

                                    if (joined != null && (joined.Status == ChatMemberStatus.Member || joined.Status == ChatMemberStatus.Administrator || joined.Status == ChatMemberStatus.Creator))
                                    {

                                        inlineKeyboardMarkup = Keyboards.GetHomeButton();


                                        StringBuilder st = new StringBuilder();

                                        if (BotSettings.tbUsers.BussinesTitle != null)
                                        {
                                            st.AppendLine("سلام به " + " " + BotSettings.tbUsers.BussinesTitle + " " + "خوش آمدید");
                                        }
                                        else
                                        {
                                            st.AppendLine("کاربر عزیز به ربات ما خوش آمدید");
                                        }
                                        st.AppendLine("");
                                        st.AppendLine("📌 جهت استفاده از ربات لطفا یکی از گزینه های زیر را انتخاب کنید:");

                                        RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                        var task = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, st.ToString(), null, null, null, null, null, null, null, null, inlineKeyboardMarkup);

                                        return;


                                    }
                                    else
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ با عرض پوزش : کاربر گرامی هنوز در کانال عضو نشده اید", true);
                                        return;
                                    }

                                }
                                else
                                {
                                    var joined = await bot.Client.GetChatMemberAsync("@" + BotSettings.ChannelID, Convert.ToInt64(UserAcc.Tel_UniqUserID));
                                    if (joined != null && (joined.Status == ChatMemberStatus.Member || joined.Status == ChatMemberStatus.Administrator || joined.Status == ChatMemberStatus.Creator))
                                    {

                                    }
                                    else
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ با عرض پوزش : کاربر گرامی برای استفاده از خدمات ما باید در کانال ما عضو باشید", true);
                                        return;
                                    }
                                }
                            }


                            #endregion

                            var MessageTime = update.CallbackQuery.Message.Date.AddHours(-1).AddMinutes(-5);
                            var ThisTime = UserAcc.Tel_UpdateMessage.Value;

                            if (ThisTime > MessageTime)
                            {
                                RealUser.SetUpdateMessageTime(User.Tel_UniqUserID, db, DateTime.UtcNow, botName);

                                #region چک کردن وضعیت پرداخت ارز دیجیتال

                                //var btnpay = update.CallbackQuery.Data.Split('_');
                                //if (btnpay.Length >= 2)
                                //{
                                //    if (btnpay[0] == "paid")
                                //    {
                                //        var paymentId = btnpay[1];
                                //        nowPayment.CheckPaymentStatus(bot, paymentId, update, callbackQuery.From.Id.ToString());
                                //    }
                                //}

                                #endregion

                                #region دکمه برگشت

                                if (callbackQuery.Data == "back")
                                {
                                    if (callbackQuery.Data == "back" && UserAcc.Tel_Step == "Created_Factor")
                                    {
                                        inlineKeyboardMarkup = Keyboards.GetHomeButton();
                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Start", db, botName);
                                        var editedMessage = await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, "لطفا یکی از گزینه های زیر را انتخاب کنید", replyMarkup: inlineKeyboardMarkup);
                                        return;
                                    }
                                    if (!string.IsNullOrEmpty(callbackQuery.Message.Text))
                                    {
                                        inlineKeyboardMarkup = Keyboards.GetHomeButton();
                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Start", db, botName);
                                        var editedMessage = await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, "لطفا یکی از گزینه های زیر را انتخاب کنید");
                                        return;

                                    }
                                    else
                                    {
                                        inlineKeyboardMarkup = Keyboards.GetHomeButton();
                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Start", db, botName);
                                        await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                        await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, "لطفا یکی از گزینه های زیر را انتخاب کنید", replyMarkup: inlineKeyboardMarkup);
                                        return;
                                    }

                                }
                                if (callbackQuery.Data == "backToInfo")
                                {
                                    inlineKeyboardMarkup = Keyboards.GetHomeButton();
                                    RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Start", db, botName);
                                    await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                    return;
                                }

                                #endregion

                                #region نمایش اطلاعات اشتراک انتخاب شده

                                else if (UserAcc.Tel_Step == "Select_AccountForShowInfo")
                                {
                                    var Email = update.CallbackQuery.Data;

                                    var Link = tbLinksRepository.Where(p => p.tbL_Email == Email).FirstOrDefault();
                                    if (Link != null)
                                    {
                                        MySqlEntities mySql = new MySqlEntities(Link.tbServers.ConnectionString);
                                        mySql.Open();
                                        var reader = mySql.GetData("select * from v2_user where email='" + Email + "'");
                                        StringBuilder st = new StringBuilder();
                                        while (reader.Read())
                                        {


                                            st.AppendLine("📈 <strong>اطلاعات اشتراک شما</strong> : ");
                                            st.AppendLine("");
                                            st.AppendLine("📌 نام اشتراک : " + Link.tbL_Email.Split('@')[0].Split('$')[0]);
                                            st.AppendLine("");
                                            st.AppendLine("📉 <strong>اطلاعات مصرفی شما</strong> : ");
                                            var Status = "فعال";
                                            var re = Utility.ConvertByteToGB(reader.GetDouble("d") + reader.GetDouble("u"));
                                            var UsedVol = Math.Round(re, 2) + " گیگابایت";

                                            var vol = reader.GetInt64("transfer_enable") - (reader.GetDouble("d") + reader.GetDouble("u"));
                                            if (vol <= 0)
                                            {
                                                Status = "اتمام حجم";
                                            }


                                            var d = Utility.ConvertByteToGB(vol);

                                            var RemainingVolume = Math.Round(d, 2) + " گیگابایت";
                                            var volume = Utility.ConvertByteToGB(reader.GetInt64("transfer_enable")) + " گیگابایت";
                                            st.AppendLine("");
                                            st.AppendLine("🌐 حجم مصرفی : " + UsedVol);
                                            st.AppendLine("📶 حجم باقی مانده : " + RemainingVolume);
                                            st.AppendLine("📡 حجم کل : " + volume);
                                            st.AppendLine("");

                                            var ExpireTime = reader.GetBodyDefinition("expired_at");
                                            if (ExpireTime != "")
                                            {
                                                var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(ExpireTime));
                                                if (ex <= DateTime.Now)
                                                {
                                                    Status = "پایان تاریخ اشتراک";
                                                }
                                                st.AppendLine("📆 تاریخ انقضا :" + ex.ConvertDatetimeToShamsiDate() + " - " + "(" + Utility.CalculateLeftDayes(ex) + ")" + " روز دیگر تا انقضا");
                                            }
                                            else
                                            {
                                                st.AppendLine("📆 تاریخ انقضا : " + " بدون انقضا");
                                            }
                                            var IsBanned = reader.GetBoolean("banned");
                                            if (IsBanned)
                                            {
                                                Status = "مسدود";
                                            }
                                            st.AppendLine("");

                                            st.AppendLine("وضعیت : " + "<strong>" + Status + "</strong>");

                                            var SubLink = "https://" + Link.tbServers.SubAddress + "/api/v1/client/subscribe?token=" + Link.tbL_Token;
                                            st.AppendLine("");
                                            var Orders = tbOrdersRepository.Where(p => p.AccountName == Link.tbL_Email && p.OrderStatus == "FOR_RESERVE").ToList();
                                            if (Orders.Count >= 1)
                                            {
                                                st.AppendLine("<strong>" + "🌐 بسته های رزرو :" + "</strong>");
                                                st.AppendLine("");
                                                int Counter = 1;
                                                foreach (var item in Orders)
                                                {
                                                    st.AppendLine(Counter + "-" + " " + item.Traffic + " گیگ" + " " + item.Month + " ماهه");
                                                    Counter++;
                                                }
                                            }

                                            var image = InputFile.FromStream(new MemoryStream(Utility.GenerateQRCode(SubLink)));

                                            st.AppendLine("");
                                            st.AppendLine("🔗 Subscrition Link : " + "<code>" + SubLink + "</code>");
                                            st.AppendLine("");
                                            st.AppendLine("");
                                            st.AppendLine("💢 این نکته رو در نظر داشته باشید گزینه تغییر لینک باعث قطع اتصال لینک قبلی می شود و شما باید مجدد لینک جدید را به موبایل خود اضافه کنید");

                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();

                                            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                            row1.Add(InlineKeyboardButton.WithCallbackData("تغییر لینک 🔗", "Question%" + Link.tbL_Email));
                                            row1.Add(InlineKeyboardButton.WithCallbackData("📈 نمایش ریز مصرف", "Usage%" + Link.tbL_Email));
                                            inlineKeyboards.Add(row1);

                                            List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();
                                            row2.Add(InlineKeyboardButton.WithCallbackData("حذف اشتراک 🗑", "DeleteAcc%" + Link.tbL_Email));
                                            inlineKeyboards.Add(row2);

                                            //List<InlineKeyboardButton> row3 = new List<InlineKeyboardButton>();
                                            //row3.Add(InlineKeyboardButton.WithCallbackData("تمدید خودکار ⏳", "AutoRenew%" + Link.tb_RandomEmail));
                                            //inlineKeyboards.Add(row3);

                                            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);
                                            RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Back_ToUserLinks", db, botName);

                                            //await bot.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);

                                            // Send the QR code image as a message
                                            await bot.Client.SendPhotoAsync(
                                                chatId: callbackQuery.From.Id,
                                                photo: image,
                                                caption: st.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard
                                            );


                                            //var editedMessage = await bot.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, st.ToString(), ParseMode.Html, null, null, null, default);

                                        }
                                        reader.Close();
                                        mySql.Close();
                                        return;
                                    }



                                }

                                #endregion

                                #region نمایش اطلاعات لینک ریست شده

                                var clb = update.CallbackQuery.Data.Split('%');
                                if (clb.Length > 0)
                                {
                                    if (clb[0] == "ResetLink")
                                    {
                                        var email = clb[1];
                                        var Link = tbLinksRepository.Where(p => p.tbL_Email == email).FirstOrDefault();
                                        if (Link != null)
                                        {
                                            MySqlEntities mySql = new MySqlEntities(Link.tbServers.ConnectionString);
                                            mySql.Open();
                                            string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                                            var query = "update v2_user set token = '" + token + "',uuid='" + Guid.NewGuid() + "' where email='" + Link.tbL_Email + "'";
                                            var reader = mySql.GetData(query);

                                            StringBuilder st = new StringBuilder();
                                            st.AppendLine("📈 <strong>لینک جدید اشتراک شما : </strong>");
                                            st.AppendLine("👇👇👇👇👇👇👇");
                                            st.AppendLine("");
                                            var SubLink = "https://" + Link.tbServers.SubAddress + "/api/v1/client/subscribe?token=" + token;
                                            st.AppendLine("<code>" + SubLink + "</code>");
                                            st.AppendLine("");

                                            st.AppendLine("◀️ روی لینک کلیک کنید به صورت خودکار لینک کپی می شود");
                                            st.AppendLine("");
                                            st.AppendLine("◀️ همچنان می توانید از بخش سرویس ها انتخاب اشتراک مورد نظر لینک اشتراک خودتان را کپی کنید");
                                            st.AppendLine("");
                                            st.AppendLine("❌ هم اکنون لینک قبلی قطع اتصال شده است");

                                            var image = InputFile.FromStream(new MemoryStream(Utility.GenerateQRCode(SubLink)));

                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
                                            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                            row1.Add(InlineKeyboardButton.WithCallbackData("⬅️ برگشت به منو اصلی", "back"));
                                            inlineKeyboards.Add(row1);

                                            Link.tbL_Token = token;
                                            tbLinksRepository.Save();

                                            await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);
                                            reader.Close();
                                            mySql.Close();
                                            var res = await bot.Client.SendPhotoAsync(
                                                 chatId: callbackQuery.From.Id,
                                                 photo: image,
                                                 caption: st.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard
                                             );


                                        }

                                    }
                                }

                                #endregion

                                #region نمایش پیغام آیا مطمئن هستید برای ریست لینک
                                var cl = update.CallbackQuery.Data.Split('%');
                                if (cl.Length == 2)
                                {
                                    if (cl[0] == "Question")
                                    {
                                        List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();

                                        List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                        row1.Add(InlineKeyboardButton.WithCallbackData("بله", "ResetLink%" + cl[1]));
                                        row1.Add(InlineKeyboardButton.WithCallbackData("خیر", "backToInfo"));
                                        inlineKeyboards.Add(row1);

                                        var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboards);
                                        StringBuilder st = new StringBuilder();
                                        st.AppendLine("❓ آیا مطمئن هستید ؟");
                                        st.AppendLine("");
                                        st.AppendLine("❗️ بعد از تائید تمام افراد متصل قطع می شوند ");
                                        st.AppendLine("");
                                        st.AppendLine("❗️ نگران قطعی تلگرام خودتان نباشید بعد از تائید لینک جدید بلافاصله برای شما ارسال می شود");
                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, st.ToString(), replyMarkup: inlineKeyboard);
                                    }
                                }

                                #endregion

                                #region نمایش ریز مصرف کاربر


                                if (callback.Length == 2)
                                {
                                    if (callback[0] == "Usage")
                                    {
                                        var Email = callback[1];

                                        var Link = UserAcc.tbLinks.Where(p => p.tbL_Email == Email).FirstOrDefault();

                                        if (Link != null)
                                        {
                                            MySqlEntities mySqlEntities = new MySqlEntities(Link.tbServers.ConnectionString);
                                            mySqlEntities.Open();

                                            var unixTtime = Utility.ConvertDatetimeToSecond(DateTime.Now.AddDays(-30));
                                            var query = "SELECT v2_stat_user.*,v2_user.email FROM `v2_stat_user` join v2_user on v2_stat_user.user_id = v2_user.id where email=" + "'" + Email + "' and v2_stat_user.updated_at >=" + unixTtime;
                                            var reader = mySqlEntities.GetData(query);

                                            List<UseageViewModel> Useages = new List<UseageViewModel>();


                                            while (reader.Read())
                                            {
                                                UseageViewModel model = new UseageViewModel();
                                                var d = reader.GetInt64("d");
                                                var u = reader.GetInt64("u");

                                                var total = d + u;

                                                var UnixDate = reader.GetInt64("updated_at");

                                                var Date = Utility.ConvertSecondToDatetime(UnixDate);

                                                model.Date = Date;
                                                model.Used = Utility.ConvertByteToMG(total);

                                                Useages.Add(model);

                                            }


                                            var finalMdoel = Useages.GroupBy(p => p.Date.Date).Select(p => new { Date = p.Key, Used = p.Sum(s => s.Used) }).OrderByDescending(p => p.Date).ToList();

                                            StringBuilder st = new StringBuilder();
                                            st.AppendLine("📈 <b>تاریخچه مصرف 30 روز گذشته شما :</b>");
                                            st.AppendLine("");
                                            st.AppendLine("<b>تاریخ     حجم مصرفی</b>");
                                            foreach (var item in finalMdoel)
                                            {
                                                st.AppendLine(Utility.ConvertDateTimeToShamsi2(item.Date) + " " + " | " + " " + Math.Round(item.Used, 0) + " مگابایت");
                                            }


                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
                                            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                            row1.Add(InlineKeyboardButton.WithCallbackData("⬅️ برگشت ", "backToInfo"));
                                            inlineKeyboards.Add(row1);

                                            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

                                            await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, st.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
                                        }



                                    }
                                }

                                #endregion

                                #region نمایش آموزش ها

                                if (User.Tel_Step == "WaitForSelectPlatform")
                                {

                                    if (BotSettings.tbUsers.tbConnectionHelp.Count > 0)
                                    {
                                        var id = Convert.ToInt32(callbackQuery.Data);
                                        var Learn = BotSettings.tbUsers.tbConnectionHelp.Where(p => p.ch_ID == id).FirstOrDefault();
                                        if (Learn != null)
                                        {
                                            await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, Learn.ch_Link, disableWebPagePreview: false);
                                        }

                                    }


                                }


                                #endregion

                                #region  نمایش گزینه های دستگاه ها برای راهنما بعد از خرید

                                if (callbackQuery.Data == "ConnectionHelp")
                                {

                                    if (BotSettings.tbUsers.tbConnectionHelp.Count > 0)
                                    {
                                        var Keys = Keyboards.GetHelpKeyboard(BotSettings.tbUsers.tbConnectionHelp.Where(p => p.ch_Type == "vpn").ToList());


                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("📲 لطفا با توجه به نوع دستگاه خود یکی از گزینه های زیر را انتخاب کنید");
                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: Keys);

                                        RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectPlatform", db, botName);

                                    }


                                }


                                #endregion

                                #region کیف پول 

                                #region نمایش متن واریز مبلغ جهت افزایش شارژ

                                if (callbackQuery.Data == "InventoryIncreaseCard")
                                {
                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("◀️  لطفا مبلغ مورد نظر خود را جهت افزایش موجودی وارد کنید ");
                                    str.AppendLine("");
                                    str.AppendLine("❗️ نکته : بازه مبلغ واریزی بین 10,000 تومان تا 5,000,000 تومان می باشد");
                                    str.AppendLine("❗️ مبلغ را بدون گذاشتن , وارد کنید");

                                    RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreasePrice", db, botName);

                                    List<List<KeyboardButton>> inlineKeyboards = new List<List<KeyboardButton>>();
                                    List<KeyboardButton> row2 = new List<KeyboardButton>();
                                    row2.Add(new KeyboardButton("⬅️ برگشت به صفحه اصلی"));
                                    inlineKeyboards.Add(row2);

                                    inlineKeyboardMarkup = Keyboards.BasicKeyboard(inlineKeyboards);
                                    await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                    await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, str.ToString(), parseMode: ParseMode.Html, replyMarkup: inlineKeyboardMarkup);
                                }

                                if (callbackQuery.Data == "InventoryIncreaseRial")
                                {
                                    await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "این روش موقتا از دسترس خارج شده لطفا از روش کارت به کارت استفاده کنید ⚠️", true);
                                    //StringBuilder str = new StringBuilder();
                                    //str.AppendLine("◀️  لطفا مبلغ مورد نظر خود را جهت افزایش موجودی وارد کنید ");
                                    //str.AppendLine("");
                                    //str.AppendLine("❗️ نکته : بازه مبلغ واریزی بین 50,000 تومان تا 1,000,000 تومان می باشد");
                                    //str.AppendLine("❗️ مبلغ را بدون گذاشتن , وارد کنید");

                                    //RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreasePriceRial", db);

                                    //List<List<KeyboardButton>> inlineKeyboards = new List<List<KeyboardButton>>();
                                    //List<KeyboardButton> row2 = new List<KeyboardButton>();
                                    //row2.Add(new KeyboardButton("⬅️ برگشت به صفحه اصلی"));
                                    //inlineKeyboards.Add(row2);

                                    //inlineKeyboardMarkup = Keyboards.BasicKeyboard(inlineKeyboards);
                                    //await bot.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                    //await bot.SendTextMessageAsync(callbackQuery.From.Id, str.ToString(), parseMode: ParseMode.Html, replyMarkup: inlineKeyboardMarkup);
                                }

                                #endregion

                                #region گزینه زیر مجموعه گیری

                                if (callbackQuery.Data == "InventoryIncreaseSub")
                                {


                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("✅ شما میتوانید با اشتراک گذاری این لینک به ازای هر خرید یا تمدیدی که فرد دعوت شده توسط شما انجام میدهد " /*+ (BotSetting.FreeCredit.Value * 100) +*/ + " درصد مبلغ اون تعرفه را از ما اعتبار رایگان بگیرید 😄");
                                    str.AppendLine("");
                                    str.AppendLine("📌 شما میتوانید از طریق این اعتبار سرویس های مارا خریداری کنید یا سرویس های خریداری شده را تمدید کنید");
                                    str.AppendLine("");
                                    str.AppendLine("لینک دعوت شما 👇");
                                    str.AppendLine("🔗 https://t.me/" + callbackQuery.Message.From.Username + "?start=" + callbackQuery.From.Id);

                                    await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, str.ToString(), parseMode: ParseMode.Html);
                                }

                                #endregion

                                #region شارژ از طریق ارز دیجیتال

                                var btnpayWallet = update.CallbackQuery.Data.Split('_');
                                if (btnpayWallet.Length >= 2)
                                {
                                    if (btnpayWallet[0] == "paidwallet")
                                    {

                                        await bot.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "⚠️ این روش موقتا از دسترس خارج شده لطفا از روش کارت به کارت استفاده کنید");

                                        var paymentId = btnpayWallet[1];
                                        //nowPayment.CheckIncreaseWalletStatus(bot, paymentId, update, callbackQuery.From.Id.ToString());
                                    }
                                }

                                #endregion

                                #endregion

                                #region توابع مربوط به ماشین حساب تعرفه

                                #region افزودن حجم
                                if (callbackQuery.Data == "PlusTraffic")
                                {

                                    if (User.Tel_Traffic == 300)
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ متاسفانه امکان ارائه حجم بیشتر از 300 گیگ نمی باشد", true);
                                        return;
                                    }
                                    User.Tel_Traffic += 20;

                                    if (User.Tel_Traffic > 300)
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ متاسفانه امکان ارائه حجم بیشتر از 300 گیگ نمی باشد", true);
                                        User.Tel_Traffic = 300;

                                    }



                                    CustomTrafficKeyboard customTrafficKeyboard = new CustomTrafficKeyboard(BotSettings, User.Tel_Traffic, User.Tel_Monthes);
                                    var key = customTrafficKeyboard.GetKeyboard();

                                    RealUser.SetTraffic(User.Tel_UniqUserID, db, User.Tel_Traffic, botName);
                                    await bot.Client.EditMessageReplyMarkupAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, key);

                                    return;
                                }

                                #endregion

                                #region کم کردن حجم

                                if (callbackQuery.Data == "MinsTraffic")
                                {
                                    User.Tel_Traffic -= 5;
                                    if (User.Tel_Traffic < 10)
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ امکان ارائه حجم کمتر از 10 گیگ نیست", true);
                                        return;
                                    }


                                    CustomTrafficKeyboard customTrafficKeyboard = new CustomTrafficKeyboard(BotSettings, User.Tel_Traffic, User.Tel_Monthes);
                                    var key = customTrafficKeyboard.GetKeyboard();

                                    RealUser.SetTraffic(User.Tel_UniqUserID, db, User.Tel_Traffic, botName);
                                    await bot.Client.EditMessageReplyMarkupAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, key);

                                    return;
                                }
                                #endregion

                                #region افزودن ماه

                                if (callbackQuery.Data == "PlusMonth")
                                {

                                    if (User.Tel_Monthes == 3)
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ متاسفانه امکان ارائه مدت زمان بیشتر از 3 ماه نمی باشد", true);
                                        return;
                                    }
                                    User.Tel_Monthes++;

                                    if (User.Tel_Monthes > 3)
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ متاسفانه امکان ارائه مدت زمان بیشتر از 3 ماه نمی باشد", true);
                                        User.Tel_Monthes = 3;
                                    }


                                    CustomTrafficKeyboard customTrafficKeyboard = new CustomTrafficKeyboard(BotSettings, User.Tel_Traffic, User.Tel_Monthes);
                                    var key = customTrafficKeyboard.GetKeyboard();

                                    RealUser.SetMonth(User.Tel_UniqUserID, db, User.Tel_Monthes, botName);
                                    await bot.Client.EditMessageReplyMarkupAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, key);

                                    return;
                                }

                                #endregion

                                #region کم کردن ماه

                                if (callbackQuery.Data == "MinusMonth")
                                {

                                    User.Tel_Monthes--;
                                    if (User.Tel_Monthes < 1)
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ امکان ارائه مدت زمان کمتر از 1 ماه نیست", true);
                                        return;
                                    }




                                    CustomTrafficKeyboard customTrafficKeyboard = new CustomTrafficKeyboard(BotSettings, User.Tel_Traffic, User.Tel_Monthes);
                                    var key = customTrafficKeyboard.GetKeyboard();

                                    RealUser.SetMonth(User.Tel_UniqUserID, db, User.Tel_Monthes, botName);
                                    await bot.Client.EditMessageReplyMarkupAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, key);

                                    return;
                                }
                                #endregion

                                #endregion

                                #region ساخت فاکتور پرداخت برای کاربر

                                if (callbackQuery.Data == "NextLevel")
                                {
                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("📌 سرویس انتخابی شما 👇");
                                    str.AppendLine();
                                    if (User.Tel_Data != null)
                                    {
                                        str.AppendLine("نام اشتراک :" + User.Tel_Data.Split('%')[0].Split('@')[0].Split('$')[0]);
                                    }
                                    str.AppendLine();
                                    str.AppendLine("♾ ترافیک : " + User.Tel_Traffic + " گیگ");
                                    str.AppendLine("⏳ مدت زمان : " + User.Tel_Monthes + " ماه");
                                    str.AppendLine("");
                                    str.AppendLine("💵 اعتبار کیف پول شما :" + User.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                    if (BotSettings.Present_Discount != null)
                                    {
                                        str.AppendLine("💵 قیمت نهایی ( با تخفیف ) : " + Convert.ToInt32(((User.Tel_Traffic * BotSettings.PricePerGig_Major) + (User.Tel_Monthes * BotSettings.PricePerMonth_Major)).Value - (((User.Tel_Traffic * BotSettings.PricePerGig_Major) + (User.Tel_Monthes * BotSettings.PricePerMonth_Major)).Value * BotSettings.Present_Discount)).ConvertToMony() + " تومان");
                                    }
                                    else
                                    {
                                        str.AppendLine("💵 قیمت نهایی :" + ((User.Tel_Traffic * BotSettings.PricePerGig_Major) + (User.Tel_Monthes * BotSettings.PricePerMonth_Major)).Value.ConvertToMony() + " تومان");
                                    }
                                    str.AppendLine("");
                                    str.AppendLine("🔗 شما با خرید این سرویس میتوانید با تمامی اینترنت ها متصل شوید، همچنین محدودیت اتصال کاربر ندارد.");
                                    str.AppendLine("");
                                    str.AppendLine("⭐️ شما میتوانید به مراحل قبل برگردید و سرویس را تغییر دهید یا از همین مرحله خرید خود را تایید کنید.");
                                    str.AppendLine("");
                                    str.AppendLine("");

                                    var keys = Keyboards.GetAccpetBuyFromWallet();

                                    await bot.Client.EditMessageTextAsync(User.Tel_UniqUserID, update.CallbackQuery.Message.MessageId, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keys);
                                    return;
                                }


                                #endregion

                                #region برگشت به ماشین حساب تعرفه

                                if (callbackQuery.Data == "BackToCalc")
                                {
                                    CustomTrafficKeyboard keyboard = new CustomTrafficKeyboard(BotSettings, User.Tel_Traffic, User.Tel_Monthes);
                                    var key = keyboard.GetKeyboard();

                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("🔐 سرویست رو خودت بساز");
                                    str.AppendLine("");
                                    str.AppendLine("💸 هر گیگ : " + BotSettings.PricePerGig_Major.Value.ConvertToMony() + " تومان");
                                    str.AppendLine("");
                                    str.AppendLine("⏳ هر ماه : " + BotSettings.PricePerMonth_Major.Value.ConvertToMony() + " تومان");

                                    var editedMessage = await bot.Client.EditMessageTextAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId, str.ToString(), replyMarkup: key);


                                    //RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectPlan", db, mess + "%");

                                    return;

                                }

                                #endregion

                                #region پرداخت از کیف پول

                                if (callbackQuery.Data == "AccpetWallet")
                                {
                                    var AccountName = "";
                                    if (User.Tel_Data != null)
                                    {
                                        AccountName = User.Tel_Data.Split('%')[0];
                                    }
                                    var Wallet = UserAcc.Tel_Wallet;
                                    var Price = (User.Tel_Traffic * BotSettings.PricePerGig_Major) + (User.Tel_Monthes * BotSettings.PricePerMonth_Major);
                                    if (BotSettings.Present_Discount != null && BotSettings.Present_Discount != 0)
                                    {
                                        Price -= (int)(Price * BotSettings.Present_Discount);
                                    }
                                    var PirceWithoutDiscount = (User.Tel_Traffic * BotSettings.PricePerGig_Major) + (User.Tel_Monthes * BotSettings.PricePerMonth_Major);
                                    if (Wallet >= Price)
                                    {
                                        var Link = tbLinksRepository.Where(p => p.tbL_Email == AccountName).FirstOrDefault();
                                        if (Link != null)
                                        {
                                            MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);

                                            mySql.Open();

                                            var Reader = mySql.GetData("select * from v2_user where email like '" + Link.tbL_Email + "'");
                                            var Ended = false;
                                            while (Reader.Read())
                                            {
                                                var d = Reader.GetDouble("d");
                                                var u = Reader.GetDouble("u");
                                                var totalUsed = Reader.GetDouble("transfer_enable");

                                                var total = Math.Round(Utility.ConvertByteToGB(totalUsed - (d + u)), 2);
                                                var exp2 = Reader.GetBodyDefinition("expired_at");

                                                if (!string.IsNullOrWhiteSpace(exp2))
                                                {
                                                    var expireTime = DateTime.Now.AddHours(-2);
                                                    var ExpireDate = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp2));
                                                    if (expireTime > ExpireDate)
                                                    {
                                                        Ended = true;
                                                    }
                                                }
                                                if (total <= 0.2)
                                                {
                                                    Ended = true;
                                                }
                                            }
                                            Reader.Close();

                                            if (Ended)
                                            {
                                                tbOrders order = new tbOrders();
                                                order.Order_Guid = Guid.NewGuid();
                                                order.AccountName = Link.tbL_Email;
                                                order.OrderDate = DateTime.Now;
                                                order.OrderType = "تمدید";
                                                order.OrderStatus = "FINISH";
                                                order.Order_Price = Price;
                                                order.Traffic = User.Tel_Traffic;
                                                order.Month = User.Tel_Monthes;
                                                order.PriceWithOutDiscount = PirceWithoutDiscount;
                                                order.V2_Plan_ID = Server.DefaultPlanIdInV2board;
                                                order.FK_Tel_UserID = UserAcc.Tel_UserID;
                                                var UserAc = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName).FirstOrDefault();
                                                UserAc.Tel_Wallet -= Price;
                                                var t = Utility.ConvertGBToByte(Convert.ToInt64(order.Traffic));

                                                string exp = DateTime.Now.AddDays((int)(order.Month * 30)).ConvertDatetimeToSecond().ToString();

                                                Link.tbL_Warning = false;

                                                var Query = "update v2_user set u=0,d=0,t=0,plan_id=" + Server.DefaultPlanIdInV2board + ",transfer_enable=" + t + ",expired_at=" + exp + " where email='" + Link.tbL_Email + "'";
                                                var reader = mySql.GetData(Query);
                                                var result = reader.Read();
                                                reader.Close();



                                                var InlineKeyboardMarkup = Keyboards.GetHomeButton();
                                                if (Link.tbTelegramUsers.Tel_Parent_ID != null)
                                                {
                                                    var TelParentUser = tbTelegramUserRepository.Where(p => p.Tel_UserID == Link.tbTelegramUsers.Tel_Parent_ID && p.tbUsers.Username == botName).FirstOrDefault();
                                                    TelParentUser.Tel_Wallet += Convert.ToInt32((Price * Server.FreeCredit));
                                                    tbTelegramUserRepository.Save();


                                                    StringBuilder str = new StringBuilder();
                                                    str.AppendLine("✅ کاربر زیر مجموعه شما با موفقیت خرید انجام داد و کیف پول شما شارژ شد");
                                                    str.AppendLine("");
                                                    str.AppendLine("📌 موجودی کیف پول شما : " + TelParentUser.Tel_Wallet.Value.ConvertToMony() + " تومان");

                                                    var Keys = Keyboards.GetHomeButton();
                                                    await bot.Client.SendTextMessageAsync(TelParentUser.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: Keys);


                                                }
                                                Link.tbL_Warning = false;
                                                Link.tb_AutoRenew = false;
                                                tbOrdersRepository.Insert(order);
                                                tbLinksRepository.Save();
                                                tbUsersRepository.Save();
                                                tbOrdersRepository.Save();
                                                tbTelegramUserRepository.Save();

                                                StringBuilder str2 = new StringBuilder();
                                                str2.AppendLine("✅ بسته شما با موفقیت تمدید شد");
                                                str2.AppendLine("");
                                                str2.AppendLine("♨️ می توانید برای مشاهده اطلاعات اکانت و بسته به بخش سرویس ها مراجعه کنید");
                                                RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                                var kyes = Keyboards.GetHomeButton();
                                                await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, str2.ToString(), true);
                                                await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                                await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, "🏘 به منو اصلی بازگشتید", replyMarkup: kyes, parseMode: ParseMode.Html);

                                                BotSettings.tbUsers.Wallet += Price;
                                                BotSettingRepository.Save();
                                                return;
                                            }
                                            else
                                            {
                                                tbOrders order = new tbOrders();
                                                order.Order_Guid = Guid.NewGuid();
                                                order.AccountName = Link.tbL_Email;
                                                order.OrderDate = DateTime.Now;
                                                order.OrderType = "تمدید";
                                                order.OrderStatus = "FOR_RESERVE";
                                                order.Traffic = User.Tel_Traffic;
                                                order.Month = User.Tel_Monthes;
                                                order.PriceWithOutDiscount = PirceWithoutDiscount;
                                                order.V2_Plan_ID = Server.DefaultPlanIdInV2board;
                                                order.FK_Tel_UserID = UserAcc.Tel_UserID;

                                                var UserAc = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName).FirstOrDefault();
                                                UserAc.Tel_Wallet -= Price;
                                                order.Order_Price = Price;
                                                if (UserAc.Tel_Parent_ID != null)
                                                {
                                                    var TelParentUser = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAc.Tel_Parent_ID).FirstOrDefault();
                                                    TelParentUser.Tel_Wallet += Convert.ToInt32((Price * Server.FreeCredit));
                                                    tbTelegramUserRepository.Save();


                                                    StringBuilder str1 = new StringBuilder();
                                                    str1.AppendLine("✅ کاربر زیر مجموعه شما با موفقیت خرید انجام داد و کیف پول شما شارژ شد");
                                                    str1.AppendLine("");
                                                    str1.AppendLine("📌 موجودی کیف پول شما : " + TelParentUser.Tel_Wallet.Value.ConvertToMony() + " تومان");

                                                    RealUser.SetUserStep(TelParentUser.Tel_UniqUserID, "Start", db, botName);
                                                    var Keys = Keyboards.GetHomeButton();
                                                    await bot.Client.SendTextMessageAsync(TelParentUser.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html, replyMarkup: Keys);


                                                }


                                                tbOrdersRepository.Insert(order);
                                                tbOrdersRepository.Save();
                                                tbLinksRepository.Save();

                                                StringBuilder str = new StringBuilder();
                                                str.AppendLine("✅ بسته شما با موفقیت رزرو شد");
                                                str.AppendLine("");
                                                str.AppendLine("⚠️ بعد از اتمام حجم بسته یا زمان بسته فعلی اکانت شما به صورت خودکار تمدید می شود");
                                                str.AppendLine("");
                                                RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                                await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, str.ToString(), true);
                                                await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                                var kyes = Keyboards.GetHomeButton();
                                                await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, "🏘 به منو اصلی بازگشتید", replyMarkup: kyes, parseMode: ParseMode.Html);

                                                BotSettings.tbUsers.Wallet += PirceWithoutDiscount;
                                                BotSettingRepository.Save();


                                                return;
                                            }
                                        }
                                        else
                                        {
                                            Random ran = new Random();
                                            tbOrders Order = new tbOrders();
                                            Order.Order_Guid = Guid.NewGuid();


                                            bool IsExists = true;
                                            var s = Guid.NewGuid();
                                            if (string.IsNullOrEmpty(User.Tel_Username))
                                            {
                                                AccountName = s.ToString().Split('-')[ran.Next(0, 3)];
                                            }
                                            else
                                            {
                                                AccountName += User.Tel_Username;
                                            }

                                            if (Utility.IsPersian(AccountName))
                                            {
                                                AccountName = s.ToString().Split('-')[ran.Next(0, 3)];
                                            }


                                            Order.AccountName = AccountName + "$" + s.ToString().Split('-')[ran.Next(0, 3)] + "@" + BotSettings.tbUsers.Username;
                                            while (IsExists)
                                            {
                                                var Links = tbLinksRepository.Where(p => p.tbL_Email == Order.AccountName).Any();
                                                if (Links)
                                                {
                                                    Order.AccountName = AccountName + "$" + s.ToString().Split('-')[ran.Next(0, 3)] + "@" + BotSettings.tbUsers.Username;
                                                }
                                                else
                                                {
                                                    IsExists = false;
                                                }
                                            }


                                            Order.OrderDate = DateTime.Now;
                                            Order.OrderType = "خرید";
                                            Order.OrderStatus = "FINISH";
                                            Order.Traffic = User.Tel_Traffic;
                                            Order.Month = User.Tel_Monthes;
                                            Order.V2_Plan_ID = Server.DefaultPlanIdInV2board;
                                            Order.FK_Tel_UserID = UserAcc.Tel_UserID;
                                            Order.Order_Price = Price;
                                            Order.PriceWithOutDiscount = PirceWithoutDiscount;

                                            string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];

                                            var FullName = Order.AccountName;

                                            var t = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));

                                            string exp = DateTime.Now.AddDays((int)(Order.Month * 30)).ConvertDatetimeToSecond().ToString();

                                            MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);
                                            mySql.Open();

                                            var reader = mySql.GetData("select group_id,transfer_enable from v2_plan where id =" + Server.DefaultPlanIdInV2board);
                                            long tran = 0;
                                            int grid = 0;
                                            while (reader.Read())
                                            {
                                                tran = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));
                                                grid = reader.GetInt32("group_id");
                                            }
                                            string create = DateTime.Now.ConvertDatetimeToSecond().ToString();

                                            string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at) VALUES ('" + FullName + "'," + exp + "," + create + ",'" + Guid.NewGuid() + "',0,0,0," + tran + ",0," + grid + "," + Server.DefaultPlanIdInV2board + ",'" + token + "','" + Guid.NewGuid() + "'," + create + ")";
                                            reader.Close();

                                            reader = mySql.GetData(Query);
                                            reader.Close();

                                            StringBuilder st = new StringBuilder();
                                            st.AppendLine("📈 <strong>لینک اشتراک شما  : </strong>");
                                            st.AppendLine("👇👇👇👇👇👇👇");
                                            st.AppendLine("");
                                            var SubLink = "https://" + Server.SubAddress + "/api/v1/client/subscribe?token=" + token;
                                            st.AppendLine("<code>" + SubLink + "</code>");
                                            st.AppendLine("");

                                            st.AppendLine("◀️ روی لینک کلیک کنید به صورت خودکار لینک کپی می شود");
                                            st.AppendLine("");
                                            st.AppendLine("◀️ برای نمایش جزئیات اشتراک به بخش سرویس ها مراجعه کنید");
                                            st.AppendLine("");

                                            var image = InputFile.FromStream(new MemoryStream(Utility.GenerateQRCode(SubLink)));

                                            tbLinks tbLinks = new tbLinks();
                                            tbLinks.tbL_Email = Order.AccountName;
                                            tbLinks.tbL_Email = FullName;
                                            tbLinks.tbL_Token = token;
                                            tbLinks.FK_Server_ID = Server.ServerID;
                                            tbLinks.FK_TelegramUserID = UserAcc.Tel_UserID;
                                            tbLinks.tbL_Warning = false;
                                            tbLinks.tb_AutoRenew = false;
                                            mySql.Close();


                                            var UserAc = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName).FirstOrDefault();
                                            if (UserAc != null)
                                            {
                                                UserAc.Tel_Wallet -= Price;

                                            }

                                            if (UserAc.Tel_Parent_ID != null)
                                            {
                                                var TelParentUser = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAc.Tel_Parent_ID && p.tbUsers.Username == botName).FirstOrDefault();
                                                TelParentUser.Tel_Wallet += Convert.ToInt32((Price * Server.FreeCredit));



                                                StringBuilder str = new StringBuilder();
                                                str.AppendLine("✅ کاربر زیر مجموعه شما با موفقیت خرید انجام داد و کیف پول شما شارژ شد");
                                                str.AppendLine("");
                                                str.AppendLine("📌 موجودی کیف پول شما : " + TelParentUser.Tel_Wallet.Value.ConvertToMony() + " تومان");

                                                var Keys = Keyboards.GetHomeButton();
                                                await bot.Client.SendTextMessageAsync(TelParentUser.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: Keys);


                                            }
                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();

                                            List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();
                                            row2.Add(InlineKeyboardButton.WithCallbackData("📚 راهنمای اتصال", "ConnectionHelp"));
                                            inlineKeyboards.Add(row2);
                                            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

                                            tbOrdersRepository.Insert(Order);
                                            tbOrdersRepository.Save();
                                            tbTelegramUserRepository.Save();
                                            tbLinksRepository.Insert(tbLinks);
                                            tbLinksRepository.Save();
                                            RepositoryLinkUserAndPlan.Save();

                                            var keys = Keyboards.GetHomeButton();

                                            //await botClient.SendTextMessageAsync(UserAcc.Tel_UniqUserID, "✅ اکانت شما با موفقیت ایجاد شد", replyMarkup: keys);

                                            await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ اکانت شما با موفقیت ایجاد شد", true);


                                            await bot.Client.SendPhotoAsync(
                                              chatId: UserAcc.Tel_UniqUserID,
                                              photo: image,
                                              caption: st.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);

                                            await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, "به منو اصلی بازگشتید 🏘", parseMode: ParseMode.Html, replyMarkup: keys);
                                            await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                            RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                            BotSettings.tbUsers.Wallet += PirceWithoutDiscount;
                                            BotSettingRepository.Save();
                                            return;

                                        }
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("❌ موجودی کیف پول شما کافی نیست ");
                                        str.AppendLine("");
                                        str.AppendLine("⚠️ برای شارژ کیف پول به منو اصلی بازگردید و از بخش کیف پول اقدام به شارژ کیف پول خود کنید");

                                        await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, str.ToString(), true);
                                        return;
                                    }

                                }

                                #endregion

                                #region بخش انتخاب سرویس برای تمدید

                                if (User.Tel_Step == "WaitForSelectAccount")
                                {
                                    SendTrafficCalculator(UserAcc, callbackQuery.Message.MessageId, BotSettings, bot.Client, botName, callbackQuery.Data);

                                    return;
                                }

                                #endregion

                                #region نمایش متن مطمئن هستید برای حذف اشتراک

                                if (callback.Length == 2)
                                {
                                    if (callback[0] == "DeleteAcc")
                                    {
                                        if (EndedVolumeOrDate(callback[1]))
                                        {
                                            StringBuilder str = new StringBuilder();
                                            str.AppendLine("❓ آیا مطمئن هستید ؟");
                                            str.AppendLine("");
                                            str.AppendLine("❗️ بعد از حذف امکان تمدید این اشتراک نیست ");

                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();

                                            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                            row1.Add(InlineKeyboardButton.WithCallbackData("بله", "DeleteLink%" + callback[1]));
                                            row1.Add(InlineKeyboardButton.WithCallbackData("خیر", "backToInfo"));
                                            inlineKeyboards.Add(row1);

                                            var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboards);
                                            await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), replyMarkup: inlineKeyboard);
                                        }
                                        else
                                        {
                                            StringBuilder str2 = new StringBuilder();
                                            str2.AppendLine("⚠️ بعد از اتمام حجم یا زمان اشتراک امکان حذف اشتراک فعال می شود");

                                            await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, str2.ToString(), true);
                                        }
                                    }
                                }

                                #endregion

                                #region حذف اشتراک

                                if (callback.Length == 2)
                                {
                                    if (callback[0] == "DeleteLink")
                                    {
                                        var Link = User.tbLinks.Where(p => p.tbL_Email == callback[1]).FirstOrDefault();
                                        if (Link != null)
                                        {
                                            tbLinksRepository.Delete(Link);
                                            tbLinksRepository.Save();
                                            MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);
                                            mySql.Open();
                                            var Reader = mySql.GetData("delete from v2_user where email ='" + callback[1] + "'");
                                            Reader.Read();
                                            await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ اشتراک با موفقیت حذف شد", true);
                                            await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                            var kyes = Keyboards.GetHomeButton();
                                            await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, "🏘 به منو اصلی بازگشتید", replyMarkup: kyes, parseMode: ParseMode.Html);
                                            return;


                                        }


                                    }
                                }

                                #endregion

                            }
                            else
                            {
                                StringBuilder str2 = new StringBuilder();
                                str2.AppendLine("❌ این پیام منقضی شده");
                                str2.AppendLine("");
                                str2.AppendLine("♨️ لطفا مراحل را مجدد از اول طی کنید");

                                await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, str2.ToString(), true);
                                await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                            }

                        }
                    }
                    else
                    {
                        StringBuilder str2 = new StringBuilder();
                        str2.AppendLine("⚠️ با عرض پوزش ربات برای مدت کمی از دسترس خارج شده لطفا بعدا تلاش فرمائید");
                        str2.AppendLine("");
                        str2.AppendLine("🆔 @" + BotSettings.Bot_ID);
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
                logger.Error(ex, "خطا در برقراری ارتباط سرور تلگرام");
            }

            return;
        }

        public static bool CheckExistsAccountInBot(string username, tbBotSettings BotSetting)
        {


            username = username + "@" + BotSetting.tbUsers.Username;
            if (!db.tbLinks.Any(p => p.tbL_Email == username))
            {
                return true;
            }
            else
            {
                return false;
            }


        }

        public static bool EndedVolumeOrDate(string username)
        {
            MySqlEntities mysql = new MySqlEntities(Server.ConnectionString);
            mysql.Open();
            var reader = mysql.GetData("select * from v2_user where email = '" + username + "'");
            if (reader.Read())
            {
                var vol = reader.GetInt64("transfer_enable") - (reader.GetDouble("d") + reader.GetDouble("u"));
                if (vol <= 0)
                {
                    reader.Close();
                    mysql.Close();
                    return true;
                }
                var ExpireTime = reader.GetBodyDefinition("expired_at");
                if (ExpireTime != "")
                {
                    var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(ExpireTime));
                    if (ex <= DateTime.Now)
                    {
                        reader.Close();
                        mysql.Close();
                        return true;
                    }

                }
            }

            return false;
        }

        private static void SendTrafficCalculator(tbTelegramUsers User, int MessageId, tbBotSettings BotSetting, TelegramBotClient bot, string botName, string Data = null, string Tel_Step = null)
        {

            CustomTrafficKeyboard keyboard = new CustomTrafficKeyboard(BotSetting, User.Tel_Traffic, User.Tel_Monthes);
            var key = keyboard.GetKeyboard();

            StringBuilder str = new StringBuilder();
            str.AppendLine("🔐 سرویست رو خودت بساز");
            str.AppendLine("");
            str.AppendLine("💸 به ازای هر گیگ  : " + BotSetting.PricePerGig_Major.Value.ConvertToMony() + " تومان");
            str.AppendLine("");
            str.AppendLine("⏳ هر ماه : " + BotSetting.PricePerMonth_Major.Value.ConvertToMony() + " تومان");

            if (Data != null)
            {
                RealUser.SetUserStep(User.Tel_UniqUserID, "WaitForCalulate", db, botName, Data + "%");
            }
            else
            {
                RealUser.SetUserStep(User.Tel_UniqUserID, "WaitForCalulate", db, botName);
            }

            if (Tel_Step == "WaitForEnterName" || Tel_Step == null)
            {
                bot.SendTextMessageAsync(chatId: User.Tel_UniqUserID, str.ToString(), replyMarkup: key, replyToMessageId: MessageId);
            }
            else
            {
                bot.EditMessageTextAsync(chatId: User.Tel_UniqUserID, messageId: MessageId, str.ToString(), replyMarkup: key);
            }
        }



    }
}
