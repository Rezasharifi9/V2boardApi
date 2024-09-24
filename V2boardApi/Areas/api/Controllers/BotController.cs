using DataLayer;
using DataLayer.DomainModel;
using DataLayer.Repository;
using DeviceDetectorNET.Class;
using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
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
using V2boardApi.Models;
using V2boardApi.Tools;
using V2boardBot.Functions;
using V2boardBot.Models;
using V2boardBotApp.Models;
using V2boardBotApp.Models.ViewModels;
using static System.Windows.Forms.LinkLabel;
using MihaZupan;
using System.Windows.Forms;
using System.Windows.Input;
using V2boardApi.Tools;


namespace V2boardApi.Areas.api.Controllers
{
    [System.Web.Http.AllowAnonymous]
    [LogActionFilter]
    public class BotController : ApiController
    {
        //ngrok http 4480 --host-header="localhost:4480"
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Database Instance
        private Entities db;
        //Home Keys
        private ReplyKeyboardMarkup inlineKeyboardMarkup;

        //Timer
        private System.Threading.Timer CheckLink;
        private System.Threading.Timer CheckRenewAccount;

        private System.Threading.Timer DeleteTestAccount;
        private System.Threading.Timer DeleteFactores;


        private static tbServers Server;
        private static string RobotIDforTimer { get; set; }

        public BotController()
        {
            db = new Entities();

            //#region ریپازیتوری های دیتابیس
            //tbTelegramUserRepository = new Repository<tbTelegramUsers>(db);
            //tbServerRepository = new Repository<tbServers>(db);
            //tbLinksRepository = new Repository<tbLinks>(db);
            //tbPlansRepository = new Repository<tbPlans>(db);
            //tbUsersRepository = new Repository<tbUsers>(db);
            //tbOrdersRepository = new Repository<tbOrders>(db);
            //RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
            //tbDepositLogRepo = new Repository<tbDepositWallet_Log>(db);
            //BotSettingRepository = new Repository<tbBotSettings>(db);
            //#endregion



            Server = HttpRuntime.Cache["Server"] as tbServers;
        }



        [System.Web.Http.HttpPost]

        public async Task Update(string botName, Update update)
        {
            try
            {

                using (Entities db = new Entities())
                {
                    #region ریپازیتوری های دیتابیس
                    var BotSettingRepository = new Repository<tbBotSettings>(db);
                    #endregion

                    var bot = BotManager.GetBot(botName);
                    if (bot == null)
                    {
                        logger.Warn("ربات پیدا نشد");
                        return;
                    }

                    //if (bot.Started)
                    //{
                    if (update == null)
                    {
                        logger.Warn("کلاس update پیدا نشد");
                        return;
                    }
                    var BotSettings = await BotSettingRepository.FirstOrDefaultAsync(p => p.tbUsers.Username == botName);

                    if (BotSettings.Active == true && BotSettings.Enabled && BotSettings.tbUsers.Wallet <= BotSettings.tbUsers.Limit)
                    {
                        var tbTelegramUserRepository = new Repository<tbTelegramUsers>(db);
                        var tbServerRepository = new Repository<tbServers>(db);
                        var tbLinksRepository = new Repository<tbLinks>(db);
                        var tbPlansRepository = new Repository<tbPlans>(db);
                        var tbUsersRepository = new Repository<tbUsers>(db);
                        var tbOrdersRepository = new Repository<tbOrders>(db);
                        var RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
                        var tbDepositLogRepo = new Repository<tbDepositWallet_Log>(db);
                        var V2boardPlanId = BotSettings.tbPlans.Plan_ID_V2;
                        long chatid = 0;
                        tbTelegramUsers UserAcc = new tbTelegramUsers();
                        if (update.Message is Telegram.Bot.Types.Message message)
                        {

                            chatid = update.Message.From.Id;
                            var mess = message.Text;

                            if ((bool)BotSettings.IsActiveSendReceipt)
                            {
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

                                    if (update.Message.Caption != null)
                                    {
                                        if (update.Message.Caption.Length >= 1)
                                        {
                                            str.AppendLine("💬 متن کاربر : " + update.Message.Caption);
                                        }
                                    }
                                    str.AppendLine("");
                                    str.AppendLine("");
                                    str.AppendLine("♨️ موارد فوق مورد تایید است ؟");
                                    str.Append("");

                                    List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
                                    List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                    row1.Add(InlineKeyboardButton.WithCallbackData("✅ تائید", "accept%" + update.Message.From.Id));
                                    inlineKeyboards.Add(row1);
                                    var key = new InlineKeyboardMarkup(inlineKeyboards);



                                    await bot.Client.SendPhotoAsync(BotSettings.AdminBot_ID, file, replyMarkup: key, parseMode: ParseMode.Html, caption: str.ToString());
                                    return;
                                }

                                #endregion
                            }

                            if (chatid.ToString() == BotSettings.AdminBot_ID.ToString())
                            {

                                #region ارسال پیغام همگانی توسط ادمین
                                if (update.Message.Type == MessageType.Photo && message.Caption != null)
                                {
                                    if (message.Caption.StartsWith("/all"))
                                    {
                                        var fileId = message.Photo.Last().FileId;
                                        var file = InputFile.FromFileId(fileId);
                                        var Users = BotSettings.tbUsers.tbTelegramUsers.ToList();
                                        Task.Run(() =>
                                        {
                                            foreach (var item in Users)
                                            {
                                                try
                                                {
                                                    message.Caption = message.Caption.Replace("/all", "");
                                                    bot.Client.SendPhotoAsync(item.Tel_UniqUserID, file, caption: message.Caption, parseMode: ParseMode.Html);
                                                }
                                                catch (Exception ex)
                                                {

                                                }
                                            }
                                        });
                                    }
                                }
                                else if (update.Message.Type == MessageType.Video && message.Caption != null)
                                {
                                    if (message.Caption.StartsWith("/all"))
                                    {
                                        var fileId = message.Video.FileId;
                                        var file = InputFile.FromFileId(fileId);
                                        var Users = BotSettings.tbUsers.tbTelegramUsers.ToList();
                                        Task.Run(() =>
                                        {
                                            foreach (var item in Users)
                                            {
                                                try
                                                {
                                                    message.Caption = message.Caption.Replace("/all", "");
                                                    bot.Client.SendVideoAsync(item.Tel_UniqUserID, file, caption: message.Caption, parseMode: ParseMode.Html);
                                                }
                                                catch (Exception ex)
                                                {

                                                }
                                            }
                                        });
                                    }
                                }
                                else if (update.Message.Type == MessageType.Document && message.Caption != null)
                                {
                                    if (message.Caption.StartsWith("/all"))
                                    {
                                        var Users = BotSettings.tbUsers.tbTelegramUsers.ToList();
                                        Task.Run(() =>
                                        {
                                            var fileId = message.Document.FileId;
                                            var file = InputFile.FromFileId(fileId);

                                            foreach (var item in Users)
                                            {
                                                try
                                                {
                                                    message.Caption = message.Caption.Replace("/all", "");
                                                    bot.Client.SendDocumentAsync(item.Tel_UniqUserID, file, caption: message.Caption, parseMode: ParseMode.Html);
                                                }
                                                catch (Exception ex)
                                                {

                                                }
                                            }

                                        });
                                    }
                                }
                                else if (update.Message.Type == MessageType.Text && message.Text != null)
                                {
                                    if (message.Text.StartsWith("/all"))
                                    {
                                        var Users = BotSettings.tbUsers.tbTelegramUsers.ToList();
                                        Task.Run(() =>
                                        {
                                            foreach (var item in Users)
                                            {
                                                try
                                                {
                                                    message.Text = message.Text.Replace("/all", "");
                                                    bot.Client.SendTextMessageAsync(item.Tel_UniqUserID, message.Text, parseMode: ParseMode.Html);
                                                }
                                                catch (Exception ex)
                                                {

                                                }
                                            }
                                        });
                                    }
                                }

                                #endregion
                            }
                            if (update.Message.Type == MessageType.Text)
                            {

                                #region چک کردن اینکه آیا کاربر وجود دارد در دیتابیس یا نه
                                var User = await tbTelegramUserRepository.FirstOrDefaultAsync(p => p.Tel_UniqUserID == chatid.ToString() && p.tbUsers.Username == botName);
                                if (User == null)
                                {
                                    tbTelegramUsers Usr = new tbTelegramUsers();
                                    Usr.Tel_UniqUserID = chatid.ToString();
                                    Usr.Tel_Step = "Start";
                                    Usr.Tel_Wallet = 0;
                                    Usr.Tel_Monthes = 1;
                                    Usr.Tel_Traffic = 20;
                                    Usr.Tel_Status = 1;
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
                                            var Parent = await tbTelegramUserRepository.FirstOrDefaultAsync(p => p.Tel_UniqUserID == id && p.tbUsers.Username == botName);
                                            if (Parent != null)
                                            {
                                                Usr.Tel_Parent_ID = Parent.Tel_UserID;
                                            }
                                        }
                                    }


                                    Usr.Tel_RobotID = RobotIDforTimer;
                                    Usr.Tel_RegisterDate = DateTime.Now;
                                    Usr.FK_User_ID = BotSettings.tbUsers.User_ID;
                                    tbTelegramUserRepository.Insert(Usr);
                                    await tbTelegramUserRepository.SaveChangesAsync();
                                    UserAcc = Usr;
                                    var Path = HttpContext.Current.Server.MapPath("~/assets/img/TelegramUserProfiles/" + chatid + ".jpg");
                                    await SaveUserProfilePicture(chatid, bot.Client, bot.Token, Path);
                                }
                                else
                                {
                                    if (User.Tel_RobotID == null)
                                    {
                                        User.Tel_RobotID = RobotIDforTimer;
                                        await tbTelegramUserRepository.SaveChangesAsync();
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
                                        await tbTelegramUserRepository.SaveChangesAsync();
                                    }

                                    UserAcc = User;
                                    await RealUser.SetUpdateMessageTime(User.Tel_UniqUserID, db, DateTime.UtcNow, botName);
                                    var Path = HttpContext.Current.Server.MapPath("~/assets/img/TelegramUserProfiles/" + chatid + ".jpg");
                                    await SaveUserProfilePicture(chatid, bot.Client, bot.Token, Path);
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
                                if (mess == "/start" || mess.StartsWith("/start"))
                                {

                                    inlineKeyboardMarkup = Keyboards.GetHomeButton();


                                    StringBuilder st = new StringBuilder();

                                    st.AppendLine("<b>" + " به جمع ما خوش آمدید! 👋" + "</b>");
                                    st.AppendLine("");
                                    st.AppendLine("با سرویس‌های ویژه ما، VPN سریع‌تر و تجربه‌ای بهتر در انتظار شماست.");
                                    st.AppendLine("");
                                    st.AppendLine("💼 هر لحظه و هر جا که بخواهید، به ما اعتماد کنید!");
                                    st.AppendLine("");
                                    st.AppendLine("برای ادامه یکی از گزینه های زیر را انتخاب کنید 👇");
                                    st.AppendLine("");
                                    st.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                    await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                    var task = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, st.ToString(), replyMarkup: inlineKeyboardMarkup, replyToMessageId: message.MessageId, parseMode: ParseMode.Html);
                                    return;

                                }

                                if (mess.StartsWith("⬅️ برگشت به صفحه اصلی"))
                                {

                                    inlineKeyboardMarkup = Keyboards.GetHomeButton();
                                    StringBuilder st = new StringBuilder();

                                    st.AppendLine("<b>" + " 🌺 سلام به ربات " + BotSettings.tbUsers.BussinesTitle + " خوش آمدید 👋 " + "</b>");
                                    st.AppendLine("");
                                    st.AppendLine("");
                                    st.AppendLine("برای ادامه یکی از گزینه های زیر را انتخاب کنید 👇");
                                    st.AppendLine("");
                                    st.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                    await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

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
                                            await SendTrafficCalculator(UserAcc, message.MessageId, BotSettings, bot.Client, botName, mess, Tel_Step: User.Tel_Step);
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
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForEnterName", db, botName);
                                        await bot.Client.SendTextMessageAsync(message.From.Id, str1.ToString(), replyMarkup: backkey, replyToMessageId: message.MessageId);
                                    }
                                }

                                #endregion

                                #region بخش خرید سرویس و تمدید

                                #region بخش فشردن گزینه خرید سرویس

                                else if (mess == "🛒 خرید اشتراک")
                                {
                                    await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                    var me = BotMessages.SendAccpetPolicySub(BotSettings);

                                    await bot.Client.SendTextMessageAsync(message.From.Id, me.text, replyMarkup: me.keyboard, replyToMessageId: message.MessageId, parseMode: ParseMode.Html);

                                    //await SendTrafficCalculator(UserAcc, message.MessageId, BotSettings, bot.Client, botName);
                                    return;
                                }

                                #endregion

                                #region دکمه سرویس ها
                                if (mess == "🌐 مدیریت اشتراک ‌ها")
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
                                    await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Select_AccountForShowInfo", db, botName);
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
                                else if (mess == "🔄 تمدید اشتراک")
                                {
                                    #region بخش نمایش لینک های موجود کاربر
                                    await RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
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
                                    await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectAccount", db, botName);

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
                                        if ((bool)BotSettings.IsActiveSendReceipt && (bool)BotSettings.IsActiveCardToCard)
                                        {
                                            str.AppendLine("❗️حتما حتما مبلغ را دقیق با سه رقم اخر واریز کنید در غیر اینصورت ربات واریزی شمارو تشخیص نمی دهد");
                                            str.AppendLine("");
                                            str.AppendLine("❗️ در صورت عدم تایید خودکار رسید واریزیتون رو به صورت تصویر ( نه فایل ) برای ربات بفرستید");
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
                                        str.AppendLine("");
                                        str.AppendLine("<b>" + "⚠️ نکته : این فاکتور تا 24 ساعت معتبر است و بعد از دریافت پیام منقضی شدن فاکتور به هیچ عنوان مبلغی واریز نکنید " + "</b>");
                                        tbDepositWallet_Log tbDeposit = new tbDepositWallet_Log();
                                        tbDeposit.dw_Price = fullPrice;
                                        tbDeposit.dw_CreateDatetime = DateTime.Now;
                                        tbDeposit.dw_Status = "FOR_PAY";
                                        tbDeposit.FK_TelegramUser_ID = UserAcc.Tel_UserID;
                                        tbDeposit.dw_message_id = update.Message.MessageId;
                                        tbDepositLogRepo.Insert(tbDeposit);
                                        await tbDepositLogRepo.SaveChangesAsync();
                                        str.AppendLine("");
                                        str.AppendLine("〰️〰️〰️〰️〰️");
                                        str.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Pay_IncreasePrice", db, botName);
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
                                        str.AppendLine("〰️〰️〰️〰️〰️");
                                        str.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreasePrice", db, botName);

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

                                if (mess == "📊 تعرفه‌ها")
                                {

                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("📊 تعرفه های اشتراک به شرح زیر است :");
                                    str.AppendLine("");


                                    if (BotSettings.Present_Discount != null)
                                    {
                                        str.AppendLine("<b>1- 🏅 اشتراک گُلد ( با تخفیف )</b>");
                                        str.AppendLine("");
                                        str.AppendLine("💸 قیمت هر گیگ : " + "<s> " + BotSettings.PricePerGig_Major.ConvertToMony() + " تومان" + " </s>" + " 👈 " + (BotSettings.PricePerGig_Major - (BotSettings.PricePerGig_Major * BotSettings.Present_Discount)).Value.ConvertToMony() + " تومان");
                                        str.AppendLine("⏳ قیمت هر ماه : " + "<s>" + BotSettings.PricePerMonth_Major.ConvertToMony() + " تومان" + "</s>" + " 👈 " + (BotSettings.PricePerMonth_Major - (BotSettings.PricePerMonth_Major * BotSettings.Present_Discount)).Value.ConvertToMony() + " تومان");
                                        str.AppendLine("");
                                    }
                                    else
                                    {
                                        str.AppendLine("<b>1- 🏅 اشتراک گُلد</b>");
                                        str.AppendLine("");
                                        str.AppendLine("💸 قیمت هر گیگ : " + BotSettings.PricePerGig_Major.ConvertToMony() + " تومان");
                                        str.AppendLine("⏳ قیمت هر ماه : " + BotSettings.PricePerMonth_Major.ConvertToMony() + " تومان");
                                        str.AppendLine("");
                                    }


                                    var Plans = BotSettings.tbUsers.tbPlans.Where(s => s.IsRobotPlan).ToList();
                                    if (Plans.Count() >= 1)
                                    {
                                        if (BotSettings.Present_Discount != null)
                                        {
                                            str.AppendLine("<b>2-  💎 اشتراک پرمیوم ( باتخفیف )</b>");
                                            str.AppendLine("");
                                            var counter = 1;
                                            foreach (var item in Plans)
                                            {
                                                str.AppendLine(counter + " - " + item.PlanMonth + " ماهه" + " | " + (item.device_limit - 1) + " کاربر" + " | " + "<s>" + item.Price.Value.ConvertToMony() + " تومان" + "</s>" + " 👈 " + (item.Price.Value - (item.Price.Value * BotSettings.Present_Discount)).Value.ConvertToMony() + " تومان");
                                                counter++;
                                            }
                                        }
                                        else
                                        {
                                            str.AppendLine("<b>2- 💎 اشتراک پرمیوم</b>");
                                            str.AppendLine("");
                                            var counter = 1;
                                            foreach (var item in Plans)
                                            {
                                                str.AppendLine(counter + " - " + item.PlanMonth + " ماهه" + " | " + (item.device_limit - 1) + " کاربر" + " | " + item.Price.Value.ConvertToMony() + " تومان");
                                                counter++;
                                            }
                                        }
                                    }

                                    str.AppendLine("");
                                    str.AppendLine("");
                                    str.AppendLine("🔗 شما با خرید این سرویس میتوانید با تمامی اینترنت ها متصل شوید.");
                                    str.AppendLine("");
                                    str.AppendLine("〰️〰️〰️〰️〰️");
                                    str.AppendLine("🚀@" + BotSettings.Bot_ID);
                                    await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);

                                }

                                #endregion

                                #region راهنمای اتصال

                                if (mess == "📘 آموزش اتصال")
                                {

                                    if (BotSettings.tbUsers.tbConnectionHelp.Count > 0)
                                    {
                                        var Keys = Keyboards.GetHelpKeyboard(BotSettings.tbUsers.tbConnectionHelp.Where(p => p.ch_Type == "vpn").ToList());


                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("📲 لطفا با توجه به نوع دستگاه خود یکی از گزینه های زیر را انتخاب کنید");
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(chatid, str.ToString(), parseMode: ParseMode.Html, replyMarkup: Keys, replyToMessageId: message.MessageId);

                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectPlatform", db, botName);
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

                                if (mess == "👜 کیف پول من")
                                {
                                    if (UserAcc != null)
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("");
                                        str.AppendLine("<b>" + "📌 موجودی کیف پول شما : " + UserAcc.Tel_Wallet.Value.ConvertToMony() + " تومان" + "</b>");
                                        str.AppendLine("");

                                        var learns = BotSettings.tbUsers.tbConnectionHelp.Where(p => p.ch_Type == "crypto").ToList();
                                        foreach (var item in learns)
                                        {
                                            str.AppendLine(" <a href='" + item.ch_Link + "'>" + item.ch_Title + "</a>");
                                        }
                                        str.AppendLine("");
                                        str.AppendLine("✅ جهت شارژ کیف پول، لطفا یکی از روش های زیر را انتخاب کنید");
                                        str.AppendLine("");
                                        str.AppendLine("👥 با دعوت دوستان خود از بخش " + "<b>زیر مجموعه گیری</b>" + "، اعتبار رایگان دریافت کنید!");
                                        str.AppendLine("");
                                        str.AppendLine("➖➖➖➖➖➖➖➖➖");
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

                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Select_Way_To_Increase", db, botName);

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

                                    str.AppendLine("✅ جهت ارتباط با پشتیبانی با آیدی زیر در ارتباط باشید 👇  ");
                                    str.AppendLine("");
                                    str.AppendLine("📱 @" + BotSettings.AdminUsername);
                                    str.AppendLine("");
                                    str.AppendLine("⚠️ لطفا قبل از ارسال پیام اگر مشکلی در اتصال دارید ابتدا بخش <b>📘 آموزش اتصال</b> را مطالعه کنید.");
                                    str.AppendLine("");
                                    str.AppendLine("🆔 @" + BotSettings.Bot_ID);

                                    await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);



                                }

                                #endregion

                                #region اشتراک تست

                                if (mess == "🎁 اشتراک تست")
                                {
                                    await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                    var me = BotMessages.SendSelectSubTypeTest(BotSettings);

                                    await bot.Client.SendTextMessageAsync(message.From.Id, me.text, replyMarkup: me.keyboard, replyToMessageId: message.MessageId, parseMode: ParseMode.Html);

                                    //await SendTrafficCalculator(UserAcc, message.MessageId, BotSettings, bot.Client, botName);
                                    return;
                                }
                                #endregion
                            }



                            #region سوالات متداول

                            if (mess == "❓ سؤالات رایج")
                            {
                                StringBuilder str = new StringBuilder();
                                str.AppendLine("<b>" + "❓ سؤالات متداول درباره اشتراک‌ها ❓" + "</b>");
                                str.AppendLine("");
                                str.AppendLine("");
                                str.AppendLine("<b>" + "🔹 آیا اشتراک من ثابت است و می‌توانم آی‌پی را تغییر دهم؟" + "</b>");
                                str.AppendLine("بله، اشتراک ها به صورت ثابت (استاتیک) ارائه می‌شود.");
                                str.AppendLine("");
                                str.AppendLine("<b>" + "🔹 آیا می‌توانم با چند دستگاه به یک اشتراک متصل شوم؟" + "</b>");
                                str.AppendLine("بله، اشتراک ما به شما اجازه می‌دهد که بدون محدودیت کاربری، به چندین دستگاه به طور همزمان متصل شوید.");
                                str.AppendLine("");
                                str.AppendLine("<b>" + "🔹 آیا می‌توانم موقعیت سرورم را تغییر دهم؟" + "</b>");
                                str.AppendLine("بله، شما می‌توانید به راحتی از طریق لیست سرورهای موجود در اشتراک ، سرور مورد نظر خود را انتخاب کنید");
                                str.AppendLine("");
                                str.AppendLine("<b>" + "🔹 آیا حجم باقی مانده یا زمان باقی مانده به دوره بعد انتقال می یابد؟" + "</b>");
                                str.AppendLine("خیر، حجم یا زمان باقی مانده شما به دوره بعد انتقال نمی یابد و باید در دوره خریداری شده مصرف شود !!");
                                str.AppendLine("");
                                str.AppendLine("<b>" + "🔹 آیا قبل از اتمام زمان یا حجم , بسته جدید تمدید کنم بسته قبلی از بین میرود ؟" + "</b>");
                                str.AppendLine("خیر، اگر حجم یا زمان داشته باشید بسته جدید رزرو خواهد شد و بعد از پایان بسته فعلی جایگزین خواهد شد !!");
                                str.AppendLine("");
                                str.AppendLine("💬 اگر سوالی داشتید که پاسخ آن را نیافتید با پشتیبانی در ارتباط باشید.");
                                str.AppendLine("");
                                str.AppendLine("🆔 @" + BotSettings.AdminUsername);

                                await bot.Client.SendTextMessageAsync(chatid, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                            }

                            #endregion
                        }

                        else
                    if (update.CallbackQuery != null)
                        {
                            var callbackQuery = update.CallbackQuery;
                            var callback = update.CallbackQuery.Data.Split('%');


                            #region تایید تراکنش توسط ادمین

                            if ((bool)BotSettings.IsActiveSendReceipt)
                            {
                                if (update.CallbackQuery.From.Id == BotSettings.AdminBot_ID)
                                {
                                    if (callback.Length == 2)
                                    {
                                        var btn = callback[0];
                                        var id = callback[1];

                                        if (btn == "accept")
                                        {
                                            var NowDate = DateTime.Now.AddHours(-1);
                                            var Deposit = await tbDepositLogRepo.WhereAsync(p => p.dw_CreateDatetime >= NowDate && p.dw_Status == "FOR_PAY");

                                            int itemsPerRow = 2; // تعداد دکمه‌ها در هر سطر
                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
                                            for (int i = 0; i < Deposit.Count; i += itemsPerRow)
                                            {
                                                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();

                                                for (int j = i; j < i + itemsPerRow && j < Deposit.Count; j++)
                                                {
                                                    row.Add(InlineKeyboardButton.WithCallbackData(Deposit[j].dw_Price.Value.ConvertToMony(), "Faccept%" + Deposit[j].dw_ID));
                                                }

                                                inlineKeyboards.Add(row);
                                            }

                                            if (Deposit.Count == 0)
                                            {
                                                StringBuilder str = new StringBuilder();
                                                str.AppendLine("❌  فاکتوری برای این کاربر یافت نشد");
                                                await bot.Client.SendTextMessageAsync(update.CallbackQuery.From.Id, str.ToString(), parseMode: ParseMode.Html);
                                            }
                                            else
                                            {
                                                var key = new InlineKeyboardMarkup(inlineKeyboards);
                                                StringBuilder str = new StringBuilder();
                                                str.AppendLine(" لیست فاکتور های ساخته شده :");
                                                str.AppendLine("");
                                                str.AppendLine("❗️ روی فاکتور پرداخت شده کلیک کنید");
                                                await bot.Client.SendTextMessageAsync(update.CallbackQuery.From.Id, str.ToString(), replyMarkup: key, parseMode: ParseMode.Html);
                                            }
                                            return;
                                        }
                                        else if (btn == "Faccept")
                                        {
                                            var Deposit = await tbDepositLogRepo.FirstOrDefaultAsync(p => p.dw_ID.ToString() == id && p.dw_Status == "FOR_PAY");
                                            if (Deposit != null)
                                            {
                                                Deposit.dw_Status = "FINISH";
                                                Deposit.tbTelegramUsers.Tel_Wallet += Deposit.dw_Price / 10;
                                                StringBuilder str = new StringBuilder();
                                                str.AppendLine("✅ کیف پول شما با موفقیت شارژ شد!");
                                                str.AppendLine("");
                                                str.AppendLine("💰 موجودی فعلی کیف پول شما: " + Deposit.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                                str.AppendLine("");
                                                str.AppendLine("🔔 حالا می‌توانید برای خرید اشتراک جدید یا تمدید اشتراک اقدام کنید.");
                                                str.AppendLine("");
                                                str.AppendLine("〰️〰️〰️〰️〰️");
                                                str.AppendLine("🚀 @" + Deposit.tbTelegramUsers.tbUsers.tbBotSettings.ToList()[0].Bot_ID);
                                                var keyboard = Keyboards.GetHomeButton();

                                                await RealUser.SetUserStep(Deposit.tbTelegramUsers.Tel_UniqUserID, "Start", db, Deposit.tbTelegramUsers.tbUsers.Username);
                                                await tbDepositLogRepo.SaveChangesAsync();
                                                await bot.Client.SendTextMessageAsync(Deposit.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
                                                await bot.Client.SendTextMessageAsync(BotSettings.AdminBot_ID, "✅ تراکنش با موفقیت تایید شد");
                                                return;
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region چک کردن اینکه آیا کاربر وجود دارد در دیتابیس یا نه
                            var User = await tbTelegramUserRepository.FirstOrDefaultAsync(p => p.Tel_UniqUserID == update.CallbackQuery.From.Id.ToString() && p.tbUsers.Username == botName);
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
                                await tbTelegramUserRepository.SaveChangesAsync();
                                UserAcc = Usr;
                            }
                            else
                            {
                                if (UserAcc.Tel_RobotID == null)
                                {
                                    User.Tel_RobotID = callbackQuery.Message.From.Username;
                                    await tbTelegramUserRepository.SaveChangesAsync();
                                }
                                if (User.Tel_UpdateMessage == null)
                                {
                                    User.Tel_UpdateMessage = DateTime.UtcNow;
                                    await RealUser.SetUpdateMessageTime(User.Tel_UniqUserID, db, DateTime.UtcNow, botName);
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
                                        st.AppendLine("");
                                        st.AppendLine("〰️〰️〰️〰️〰️");
                                        st.AppendLine("🚀@" + BotSettings.Bot_ID);
                                        await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

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

                            var MessageTime = update.CallbackQuery.Message.Date;
                            var ThisTime = DateTime.UtcNow.AddMinutes(-3);

                            var isAdmin = false;
                            if (chatid == BotSettings.AdminBot_ID)
                            {
                                isAdmin = true;
                            }

                            if (ThisTime < MessageTime || isAdmin)
                            {
                                await RealUser.SetUpdateMessageTime(User.Tel_UniqUserID, db, DateTime.UtcNow, botName);

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
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Start", db, botName);
                                        var editedMessage = await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, "لطفا یکی از گزینه های زیر را انتخاب کنید", replyMarkup: inlineKeyboardMarkup);
                                        return;
                                    }
                                    if (!string.IsNullOrEmpty(callbackQuery.Message.Text))
                                    {
                                        inlineKeyboardMarkup = Keyboards.GetHomeButton();
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Start", db, botName);
                                        var editedMessage = await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, "لطفا یکی از گزینه های زیر را انتخاب کنید");
                                        return;

                                    }
                                    else
                                    {
                                        inlineKeyboardMarkup = Keyboards.GetHomeButton();
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Start", db, botName);
                                        await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                        await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, "لطفا یکی از گزینه های زیر را انتخاب کنید", replyMarkup: inlineKeyboardMarkup);
                                        return;
                                    }

                                }
                                if (callback.Length == 2)
                                {
                                    if (callback[0] == "backToInfo")
                                    {
                                        callbackQuery.Data = callback[1];
                                        var mess = BotMessages.SendSelectUser(BotSettings, callbackQuery);

                                        await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, mess.text, parseMode: ParseMode.Html, replyMarkup: mess.keyboard);

                                        await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "SelectDevice", db, botName);


                                        return;
                                    }
                                }

                                if (callbackQuery.Data == "backToInfo")
                                {


                                    if (User.Tel_Step == "SelectSubType")
                                    {
                                        if (User.Tel_Data != null)
                                        {
                                            #region بخش نمایش لینک های موجود کاربر
                                            await RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                            var keyboard = Keyboards.GetServiceLinksKeyboard(UserAcc.Tel_UserID, tbLinksRepository);
                                            if (keyboard == null)
                                            {
                                                StringBuilder str2 = new StringBuilder();
                                                str2.AppendLine("❌ شما سرویسی ندارید");
                                                str2.AppendLine("");
                                                str2.AppendLine("〰️〰️〰️〰️〰️");
                                                str2.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                                await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str2.ToString());
                                                return;
                                            }
                                            await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectAccount", db, botName);

                                            StringBuilder str3 = new StringBuilder();
                                            str3.AppendLine("♨️  لطفا اشتراک مورد نظر را انتخاب کنید");
                                            str3.AppendLine("");
                                            str3.AppendLine("〰️〰️〰️〰️〰️");
                                            str3.AppendLine("🚀 @" + BotSettings.Bot_ID);

                                            await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, str3.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
                                            return;
                                            #endregion
                                        }
                                        var model = BotMessages.SendAccpetPolicySub(BotSettings);

                                        await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, model.text, parseMode: ParseMode.Html, replyMarkup: model.keyboard);
                                        return;
                                    }
                                    if (User.Tel_Step == "SelectDevice")
                                    {
                                        var plans = BotSettings.tbUsers.tbLinkUserAndPlans.Where(s => s.tbPlans.IsRobotPlan == true).Select(s => s.tbPlans).ToList();
                                        var model = BotMessages.SendSelectMonth(BotSettings, plans);
                                        await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "SelectMonth", db, botName);
                                        await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, model.text, parseMode: ParseMode.Html, replyMarkup: model.keyboard);
                                        return;
                                    }
                                    if (User.Tel_Step == "SelectMonth")
                                    {
                                        var type = BotMessages.SendSelectSubType(BotSettings);
                                        await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "SelectSubType", db, botName);
                                        await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, type.text, parseMode: ParseMode.Html, replyMarkup: type.keyboard);
                                        return;
                                    }


                                    inlineKeyboardMarkup = Keyboards.GetHomeButton();
                                    await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Start", db, botName);
                                    await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                    return;
                                }

                                #endregion

                                #region نمایش اطلاعات اشتراک انتخاب شده

                                else if (UserAcc.Tel_Step == "Select_AccountForShowInfo")
                                {
                                    var Email = update.CallbackQuery.Data;

                                    var Link = await tbLinksRepository.FirstOrDefaultAsync(p => p.tbL_Email == Email);
                                    if (Link != null)
                                    {

                                        MySqlEntities mySql = new MySqlEntities(Link.tbServers.ConnectionString);
                                        await mySql.OpenAsync();
                                        var Disc1 = new Dictionary<string, object>();
                                        Disc1.Add("@Email", Email);
                                        var reader = await mySql.GetDataAsync("select * from v2_user where email=@Email", Disc1);
                                        StringBuilder st = new StringBuilder();
                                        while (await reader.ReadAsync())
                                        {
                                            var ExpireTime = reader.GetBodyDefinition("expired_at");
                                            var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(ExpireTime));


                                            var Status = "فعال";
                                            var re = Utility.ConvertByteToGB(reader.GetDouble("d") + reader.GetDouble("u"));
                                            var UsedVol = Math.Round(re, 2) + " گیگابایت";

                                            var vol = reader.GetInt64("transfer_enable") - (reader.GetDouble("d") + reader.GetDouble("u"));
                                            if (vol <= 0)
                                            {
                                                Status = "اتمام حجم";
                                            }

                                            if (ExpireTime != "")
                                            {

                                                if (ex <= DateTime.Now)
                                                {
                                                    Status = "پایان تاریخ اشتراک";
                                                }

                                            }

                                            var IsBanned = reader.GetBoolean("banned");
                                            if (IsBanned)
                                            {
                                                Status = "مسدود";
                                            }


                                            var d = Utility.ConvertByteToGB(vol);

                                            var RemainingVolume = Math.Round(d, 2) + " گیگابایت";
                                            var volume = Utility.ConvertByteToGB(reader.GetInt64("transfer_enable")) + " گیگابایت";

                                            st.AppendLine("📋 <strong> اطلاعات اشتراک شما</strong> : ");
                                            st.AppendLine("🔖 نام اشتراک : " + Link.tbL_Email.Split('@')[0].Split('$')[0]);
                                            st.AppendLine("📆 تاریخ انقضا :" + ex.ConvertDatetimeToShamsiDate());
                                            st.AppendLine("⏳ روزهای باقی‌مانده: " + Utility.CalculateLeftDayes(ex));
                                            st.AppendLine("");
                                            st.AppendLine("");
                                            st.AppendLine("📉 <strong>اطلاعات مصرف  شما</strong> : ");
                                            st.AppendLine("🌐 حجم مصرفی : " + UsedVol);
                                            st.AppendLine("📶 حجم باقی مانده : " + RemainingVolume);
                                            st.AppendLine("📡 حجم کل : " + volume);
                                            st.AppendLine("");
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
                                            st.AppendLine("🔗 لینک اشتراک : " + "<code>" + SubLink + "</code>");
                                            st.AppendLine("");
                                            st.AppendLine("");
                                            st.AppendLine("❗ توجه: در صورت تغییر لینک اتصال، لینک قبلی به طور خودکار قطع می‌شود. برای اتصال مجدد، از لینک جدید استفاده کنید");
                                            st.AppendLine("");
                                            st.AppendLine("〰️〰️〰️〰️〰️");
                                            st.AppendLine("🚀@" + BotSettings.Bot_ID);
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
                                            await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Back_ToUserLinks", db, botName);

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
                                        await mySql.CloseAsync();
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
                                        var Link = await tbLinksRepository.FirstOrDefaultAsync(p => p.tbL_Email == email);
                                        if (Link != null)
                                        {
                                            MySqlEntities mySql = new MySqlEntities(Link.tbServers.ConnectionString);
                                            await mySql.OpenAsync();
                                            string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                                            var Disc1 = new Dictionary<string, object>();
                                            Disc1.Add("@token", token);
                                            Disc1.Add("@Guid", Guid.NewGuid());
                                            Disc1.Add("@email", Link.tbL_Email);
                                            var query = "update v2_user set token=@token,uuid=@Guid where email=@email";
                                            var reader = await mySql.GetDataAsync(query, Disc1);

                                            StringBuilder st = new StringBuilder();
                                            st.AppendLine("📈 <strong>لینک جدید اشتراک شما : </strong>");
                                            st.AppendLine("👇👇👇👇👇👇👇");
                                            st.AppendLine("");
                                            var SubLink = "https://" + Link.tbServers.SubAddress + "/api/v1/client/subscribe?token=" + token;
                                            st.AppendLine("<code>" + SubLink + "</code>");
                                            st.AppendLine("");

                                            st.AppendLine("◀️ روی لینک کلیک کنید به صورت خودکار لینک کپی می شود");
                                            st.AppendLine("");
                                            st.AppendLine("◀️ همچنان می توانید از بخش اشتراک ها انتخاب اشتراک مورد نظر لینک اشتراک خودتان را کپی کنید");
                                            st.AppendLine("");
                                            st.AppendLine("❌ هم اکنون لینک قبلی قطع اتصال شده است");
                                            st.AppendLine("〰️〰️〰️〰️〰️");
                                            st.AppendLine("🚀@" + BotSettings.Bot_ID);
                                            var image = InputFile.FromStream(new MemoryStream(Utility.GenerateQRCode(SubLink)));

                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
                                            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                            row1.Add(InlineKeyboardButton.WithCallbackData("⬅️ برگشت به منو اصلی", "back"));
                                            inlineKeyboards.Add(row1);

                                            Link.tbL_Token = token;
                                            await tbLinksRepository.SaveChangesAsync();

                                            await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);
                                            reader.Close();
                                            await mySql.CloseAsync();
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
                                        st.AppendLine("");
                                        st.AppendLine("〰️〰️〰️〰️〰️");
                                        st.AppendLine("🚀@" + BotSettings.Bot_ID);
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
                                            await mySqlEntities.OpenAsync();

                                            var unixTtime = Utility.ConvertDatetimeToSecond(DateTime.Now.AddDays(-30));
                                            var query = "SELECT v2_stat_user.*,v2_user.email FROM `v2_stat_user` join v2_user on v2_stat_user.user_id = v2_user.id where email=@email and v2_stat_user.updated_at >=@unixTtime";
                                            var Disc1 = new Dictionary<string, object>();
                                            Disc1.Add("@email", Email);
                                            Disc1.Add("@unixTtime", unixTtime);
                                            var reader = await mySqlEntities.GetDataAsync(query, Disc1);

                                            List<UseageViewModel> Useages = new List<UseageViewModel>();


                                            while (await reader.ReadAsync())
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
                                            st.AppendLine("");
                                            st.AppendLine("〰️〰️〰️〰️〰️");
                                            st.AppendLine("🚀@" + BotSettings.Bot_ID);

                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();
                                            List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                            row1.Add(InlineKeyboardButton.WithCallbackData("⬅️ برگشت ", "backToInfo"));
                                            inlineKeyboards.Add(row1);

                                            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

                                            await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, st.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
                                            await mySqlEntities.CloseAsync();
                                        }



                                    }
                                }

                                #endregion

                                #region راهنمای اتصال برای بعد از خرید لینک


                                if (callback.Length == 1)
                                {
                                    if (callback[0] == "ConnectionHelp")
                                    {
                                        if (BotSettings.tbUsers.tbConnectionHelp.Count > 0)
                                        {
                                            var Keys = Keyboards.GetHelpKeyboard(BotSettings.tbUsers.tbConnectionHelp.Where(p => p.ch_Type == "vpn").ToList());


                                            StringBuilder str = new StringBuilder();
                                            str.AppendLine("📲 لطفا با توجه به نوع دستگاه خود یکی از گزینه های زیر را انتخاب کنید");
                                            str.AppendLine("");
                                            str.AppendLine("〰️〰️〰️〰️〰️");
                                            str.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                            await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, str.ToString(), parseMode: ParseMode.Html, replyMarkup: Keys);

                                            await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectPlatform", db, botName);
                                        }
                                        else
                                        {
                                            StringBuilder str = new StringBuilder();
                                            str.AppendLine("❌ ربات فاقد آموزش می باشد لطفا به پشتیبانی پیام دهید");
                                            str.AppendLine("");
                                            str.AppendLine("〰️〰️〰️〰️〰️");
                                            str.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                            await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, str.ToString(), parseMode: ParseMode.Html);

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

                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectPlatform", db, botName);

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

                                    await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreasePrice", db, botName);

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
                                    if (BotSettings.InvitePercent != null)
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("✅ شما میتوانید با اشتراک گذاری این لینک به ازای هر خرید یا تمدیدی که فرد دعوت شده توسط شما انجام میدهد " + (BotSettings.InvitePercent.Value * 100) + " درصد مبلغ اون تعرفه را از ما اعتبار رایگان بگیرید 😄");
                                        str.AppendLine("");
                                        str.AppendLine("📌 شما میتوانید از طریق این اعتبار اشتراک های مارا خریداری کنید یا اشتراک های خریداری شده را تمدید کنید");
                                        str.AppendLine("");
                                        str.AppendLine("لینک دعوت شما 👇");
                                        str.AppendLine("🔗 https://t.me/" + callbackQuery.Message.From.Username + "?start=" + callbackQuery.From.Id);

                                        await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, str.ToString(), parseMode: ParseMode.Html);
                                    }
                                    else
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "⚠️ این روش موقتا از دسترس خارج شده لطفا از روش کارت به کارت استفاده کنید");
                                    }

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

                                    await RealUser.SetTraffic(User.Tel_UniqUserID, db, User.Tel_Traffic, botName);
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

                                    await RealUser.SetTraffic(User.Tel_UniqUserID, db, User.Tel_Traffic, botName);
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

                                    await RealUser.SetMonth(User.Tel_UniqUserID, db, User.Tel_Monthes, botName);
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

                                    await RealUser.SetMonth(User.Tel_UniqUserID, db, User.Tel_Monthes, botName);
                                    await bot.Client.EditMessageReplyMarkupAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, key);

                                    return;
                                }
                                #endregion

                                #endregion

                                #region ساخت فاکتور پرداخت برای کاربر

                                if (callbackQuery.Data == "NextLevel")
                                {
                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("📌 اشتراک انتخابی شما 👇");
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
                                    str.AppendLine("🔗 شما با خرید این اشتراک میتوانید با تمامی اینترنت ها متصل شوید، همچنین محدودیت اتصال کاربر ندارد.");
                                    str.AppendLine("");
                                    str.AppendLine("⭐️ شما میتوانید به مراحل قبل برگردید و اشتراک را تغییر دهید یا از همین مرحله خرید خود را تایید کنید.");
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
                                    str.AppendLine("🔐 اشتراکت رو خودت بساز");
                                    str.AppendLine("");
                                    str.AppendLine("💸 هر گیگ : " + BotSettings.PricePerGig_Major.ConvertToMony() + " تومان");
                                    str.AppendLine("");
                                    str.AppendLine("⏳ هر ماه : " + BotSettings.PricePerMonth_Major.ConvertToMony() + " تومان");

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
                                        var Link = await tbLinksRepository.FirstOrDefaultAsync(p => p.tbL_Email == AccountName);
                                        if (Link != null)
                                        {
                                            MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);

                                            await mySql.OpenAsync();

                                            var Disc1 = new Dictionary<string, object>();
                                            Disc1.Add("@tbL_Email", Link.tbL_Email);


                                            var Reader = await mySql.GetDataAsync("select * from v2_user where email like @tbL_Email", Disc1);
                                            var Ended = false;
                                            while (await Reader.ReadAsync())
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
                                                order.V2_Plan_ID = V2boardPlanId;
                                                order.FK_Tel_UserID = UserAcc.Tel_UserID;
                                                order.Tel_RenewedDate = DateTime.Now;
                                                var UserAc = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName).FirstOrDefault();
                                                UserAc.Tel_Wallet -= Price;
                                                var t = Utility.ConvertGBToByte(Convert.ToInt64(order.Traffic));

                                                string exp = DateTime.Now.AddDays((int)(order.Month * 30)).ConvertDatetimeToSecond().ToString();

                                                Link.tbL_Warning = false;
                                                var Disc3 = new Dictionary<string, object>();
                                                Disc3.Add("@DefaultPlanIdInV2board", V2boardPlanId);
                                                Disc3.Add("@transfer_enable", t);
                                                Disc3.Add("@exp", exp);
                                                Disc3.Add("@email", Link.tbL_Email);
                                                var DeviceLimit_Structur = "";
                                                if (BotSettings.tbPlans.device_limit != null)
                                                {
                                                    DeviceLimit_Structur = ",@device_limit=" + BotSettings.tbPlans.device_limit;
                                                    Disc1.Add("@device_limit", BotSettings.tbPlans.device_limit);
                                                }

                                                var Query = "update v2_user set u=0,d=0,t=0,plan_id=@DefaultPlanIdInV2board,transfer_enable=@transfer_enable,expired_at=@exp where email=@email" + DeviceLimit_Structur;
                                                var reader = await mySql.GetDataAsync(Query, Disc3);
                                                var result = await reader.ReadAsync();
                                                reader.Close();



                                                var InlineKeyboardMarkup = Keyboards.GetHomeButton();

                                                Link.tbL_Warning = false;
                                                Link.tb_AutoRenew = false;
                                                tbOrdersRepository.Insert(order);
                                                await tbLinksRepository.SaveChangesAsync();
                                                await tbUsersRepository.SaveChangesAsync();
                                                await tbOrdersRepository.SaveChangesAsync();
                                                await tbTelegramUserRepository.SaveChangesAsync();

                                                StringBuilder str2 = new StringBuilder();
                                                str2.AppendLine("✅ بسته شما با موفقیت تمدید شد");
                                                str2.AppendLine("");
                                                str2.AppendLine("♨️ می توانید برای مشاهده اطلاعات اکانت و بسته به بخش مدیریت اشتراک ها مراجعه کنید");
                                                await RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                                var kyes = Keyboards.GetHomeButton();
                                                await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, str2.ToString(), parseMode: ParseMode.Html, replyMarkup: kyes);
                                                await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);


                                                BotSettings.tbUsers.Wallet += PirceWithoutDiscount;
                                                await BotSettingRepository.SaveChangesAsync();
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
                                                order.V2_Plan_ID = V2boardPlanId;
                                                order.FK_Tel_UserID = UserAcc.Tel_UserID;

                                                var UserAc = await tbTelegramUserRepository.FirstOrDefaultAsync(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName);
                                                UserAc.Tel_Wallet -= Price;
                                                order.Order_Price = Price;


                                                tbOrdersRepository.Insert(order);
                                                await tbOrdersRepository.SaveChangesAsync();
                                                await tbLinksRepository.SaveChangesAsync();

                                                StringBuilder str = new StringBuilder();
                                                str.AppendLine("✅ بسته شما با موفقیت رزرو شد");
                                                str.AppendLine("");
                                                str.AppendLine("⚠️ بعد از اتمام حجم بسته یا زمان بسته فعلی اکانت شما به صورت خودکار تمدید می شود");
                                                str.AppendLine("");
                                                await RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                                await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                                var kyes = Keyboards.GetHomeButton();
                                                await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, str.ToString(), replyMarkup: kyes, parseMode: ParseMode.Html);

                                                BotSettings.tbUsers.Wallet += PirceWithoutDiscount;
                                                await BotSettingRepository.SaveChangesAsync();
                                            }
                                            await mySql.CloseAsync();
                                            return;
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
                                                if (string.IsNullOrEmpty(AccountName))
                                                {
                                                    AccountName += User.Tel_Username;
                                                }
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
                                            Order.V2_Plan_ID = V2boardPlanId;
                                            Order.FK_Tel_UserID = UserAcc.Tel_UserID;
                                            Order.Order_Price = Price;
                                            Order.PriceWithOutDiscount = PirceWithoutDiscount;

                                            string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];

                                            var FullName = Order.AccountName;

                                            var t = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));

                                            string exp = DateTime.Now.AddDays((int)(Order.Month * 30)).ConvertDatetimeToSecond().ToString();

                                            MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);
                                            await mySql.OpenAsync();
                                            var Disc1 = new Dictionary<string, object>();
                                            Disc1.Add("@V2board", V2boardPlanId);
                                            var reader = await mySql.GetDataAsync("select group_id,transfer_enable from v2_plan where id =@V2board", Disc1);
                                            long tran = 0;
                                            int grid = 0;
                                            while (await reader.ReadAsync())
                                            {
                                                tran = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));
                                                grid = reader.GetInt32("group_id");
                                            }
                                            string create = DateTime.Now.ConvertDatetimeToSecond().ToString();

                                            var Disc3 = new Dictionary<string, object>();
                                            Disc3.Add("@FullName", FullName);
                                            Disc3.Add("@expired", exp);
                                            Disc3.Add("@create", create);
                                            Disc3.Add("@guid", Guid.NewGuid());
                                            Disc3.Add("@tran", tran);
                                            Disc3.Add("@grid", grid);
                                            Disc3.Add("@V2boardId", V2boardPlanId);
                                            Disc3.Add("@token", token);
                                            Disc3.Add("@passwrd", Guid.NewGuid());

                                            var DeviceLimit_Structur = "";
                                            var DeviceLimit_data = "";

                                            if (BotSettings.tbPlans.device_limit != null)
                                            {
                                                DeviceLimit_Structur = ",device_limit";
                                                Disc3.Add("@device_limit", BotSettings.tbPlans.device_limit);
                                                DeviceLimit_data = ",@device_limit";
                                            }



                                            string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at" + DeviceLimit_Structur + ") VALUES (@FullName,@expired,@create,@guid,0,0,0,@tran,0,@grid,@V2boardId,@token,@passwrd,@create" + DeviceLimit_data + ")";
                                            reader.Close();

                                            reader = await mySql.GetDataAsync(Query, Disc3);
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
                                            st.AppendLine("◀️ برای نمایش جزئیات اشتراک به بخش مدیریت اشتراک ها مراجعه کنید");
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
                                            await mySql.CloseAsync();


                                            var UserAc = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName).FirstOrDefault();
                                            if (UserAc != null)
                                            {
                                                UserAc.Tel_Wallet -= Price;

                                            }

                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();

                                            List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();
                                            row2.Add(InlineKeyboardButton.WithCallbackData("📚 راهنمای اتصال", "ConnectionHelp"));
                                            inlineKeyboards.Add(row2);
                                            var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

                                            tbOrdersRepository.Insert(Order);
                                            await tbOrdersRepository.SaveChangesAsync();
                                            await tbTelegramUserRepository.SaveChangesAsync();
                                            tbLinksRepository.Insert(tbLinks);
                                            await tbLinksRepository.SaveChangesAsync();
                                            await RepositoryLinkUserAndPlan.SaveChangesAsync();

                                            var keys = Keyboards.GetHomeButton();

                                            //await botClient.SendTextMessageAsync(UserAcc.Tel_UniqUserID, "✅ اکانت شما با موفقیت ایجاد شد", replyMarkup: keys);

                                            await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ اکانت شما با موفقیت ایجاد شد", true);


                                            await bot.Client.SendPhotoAsync(
                                              chatId: UserAcc.Tel_UniqUserID,
                                              photo: image,
                                              caption: st.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);

                                            await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, "به منو اصلی بازگشتید 🏘", parseMode: ParseMode.Html, replyMarkup: keys);
                                            await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                            await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                            BotSettings.tbUsers.Wallet += PirceWithoutDiscount;
                                            await BotSettingRepository.SaveChangesAsync();
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

                                #region بخش انتخاب اشتراک برای تمدید

                                if (User.Tel_Step == "WaitForSelectAccount")
                                {
                                    await RealUser.SetUserStep(User.Tel_UniqUserID, "WaitForCalulate", db, botName, callbackQuery.Data);
                                    var type = BotMessages.SendSelectSubType(BotSettings);

                                    await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "SelectSubType", db, botName);

                                    await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, type.text, parseMode: ParseMode.Html, replyMarkup: type.keyboard);

                                    //await SendTrafficCalculator(UserAcc, callbackQuery.Message.MessageId, BotSettings, bot.Client, botName, callbackQuery.Data);

                                    return;
                                }

                                #endregion

                                #region نمایش متن مطمئن هستید برای حذف اشتراک

                                if (callback.Length == 2)
                                {
                                    if (callback[0] == "DeleteAcc")
                                    {
                                        if (await EndedVolumeOrDate(callback[1]))
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
                                            await tbLinksRepository.SaveChangesAsync();
                                            MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);
                                            await mySql.OpenAsync();
                                            await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ اشتراک با موفقیت حذف شد", true);
                                            await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                            var kyes = Keyboards.GetHomeButton();
                                            await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, "🏘 به منو اصلی بازگشتید", replyMarkup: kyes, parseMode: ParseMode.Html);

                                            var Disc1 = new Dictionary<string, object>();
                                            Disc1.Add("@email", callback[1]);

                                            var Reader = await mySql.GetDataAsync("delete from v2_user where email =@email", Disc1);
                                            await Reader.ReadAsync();
                                            await mySql.CloseAsync();
                                            return;


                                        }


                                    }
                                }

                                #endregion

                                #region ایجاد فاکتور

                                if (callback.Length == 1)
                                {
                                    if (callback[0] == "CreateFactor")
                                    {
                                        var PriceForGig = BotSettings.PricePerGig_Major;
                                        var PriceForMonth = BotSettings.PricePerMonth_Major;
                                        int price = Convert.ToInt32((User.Tel_Traffic * PriceForGig) + (User.Tel_Monthes * PriceForMonth));

                                        if (BotSettings.Present_Discount != null)
                                        {
                                            price = (int)(price - (price * BotSettings.Present_Discount));
                                        }


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
                                        if ((bool)BotSettings.IsActiveSendReceipt && (bool)BotSettings.IsActiveCardToCard)
                                        {
                                            str.AppendLine("❗️حتما حتما مبلغ را دقیق با سه رقم اخر واریز کنید در غیر اینصورت ربات واریزی شمارو تشخیص نمی دهد");
                                            str.AppendLine("");
                                            str.AppendLine("❗️ در صورت عدم تایید خودکار رسید واریزیتون رو به صورت تصویر ( نه فایل ) برای ربات بفرستید");
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
                                        str.AppendLine("");
                                        str.AppendLine("<b>" + "⚠️ نکته : این فاکتور تا 24 ساعت معتبر است و بعد از دریافت پیام منقضی شدن فاکتور به هیچ عنوان مبلغی واریز نکنید " + "</b>");
                                        tbDepositWallet_Log tbDeposit = new tbDepositWallet_Log();
                                        tbDeposit.dw_Price = fullPrice;
                                        tbDeposit.dw_CreateDatetime = DateTime.Now;
                                        tbDeposit.dw_Status = "FOR_PAY";
                                        tbDeposit.FK_TelegramUser_ID = UserAcc.Tel_UserID;
                                        //tbDeposit.dw_message_id = update.Message.MessageId;
                                        tbDepositLogRepo.Insert(tbDeposit);
                                        await tbDepositLogRepo.SaveChangesAsync();
                                        str.AppendLine("");
                                        str.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Pay_IncreasePrice", db, botName);
                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);



                                        return;
                                    }
                                }

                                #endregion

                                #region مربوط ساختار اشتراک نامحدود

                                #region تائید شرایط خرید اشتراک


                                if (callbackQuery.Data == "AccpetPolicy")
                                {
                                    var type = BotMessages.SendSelectSubType(BotSettings);

                                    await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "SelectSubType", db, botName);

                                    await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, type.text, parseMode: ParseMode.Html, replyMarkup: type.keyboard);

                                }

                                #endregion

                                #region اشتراک گُلد

                                if (callbackQuery.Data == "gold")
                                {
                                    await SendTrafficCalculator(UserAcc, callbackQuery.Message.MessageId, BotSettings, bot.Client, botName);
                                }

                                #endregion

                                #region اشتراک پریمیوم

                                if (callbackQuery.Data == "premium")
                                {
                                    var plans = BotSettings.tbUsers.tbLinkUserAndPlans.Where(s => s.tbPlans.IsRobotPlan == true).Select(s => s.tbPlans).ToList();
                                    if (plans.Count > 0)
                                    {
                                        var mess = BotMessages.SendSelectMonth(BotSettings, plans);
                                        await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "SelectMonth", db, botName);
                                        await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, mess.text, parseMode: ParseMode.Html, replyMarkup: mess.keyboard);
                                        return;
                                    }
                                    else
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "⚠️ فروش اشتراک پریمیوم موقتا متوقف شده است");
                                    }
                                }

                                #endregion

                                #region انتخاب تعداد کاربر اشتراک نامحدود

                                if (User.Tel_Step == "SelectMonth")
                                {
                                    var mess = BotMessages.SendSelectUser(BotSettings, callbackQuery);

                                    await bot.Client.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, mess.text, parseMode: ParseMode.Html, replyMarkup: mess.keyboard);

                                    await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "SelectDevice", db, botName);
                                    return;
                                }



                                #endregion

                                #region ایجاد فاکتور برای اشتراک نامحدود

                                if (User.Tel_Step == "SelectDevice")
                                {
                                    var Plan = BotSettings.tbUsers.tbPlans.Where(s => s.Plan_ID.ToString() == callbackQuery.Data).FirstOrDefault();
                                    if (Plan != null)
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("📌 اشتراک انتخابی شما 👇");
                                        str.AppendLine();
                                        str.AppendLine("♾ ترافیک : " + Plan.PlanVolume + " گیگ (مصرف منصفانه )");
                                        str.AppendLine("⏳ مدت زمان : " + Plan.PlanMonth + " ماه");
                                        str.AppendLine("📲 تعداد کاربر : " + (Plan.device_limit - 1).ToString());
                                        str.AppendLine("");
                                        str.AppendLine("💵 اعتبار کیف پول شما :" + User.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                        if (BotSettings.Present_Discount != null)
                                        {
                                            str.AppendLine("💵 قیمت نهایی ( با تخفیف ) : " + Convert.ToInt32(Plan.Price - (Plan.Price * BotSettings.Present_Discount)).ConvertToMony() + " تومان");
                                        }
                                        else
                                        {
                                            str.AppendLine("💵 قیمت نهایی :" + Plan.Price.Value.ConvertToMony() + " تومان");
                                        }
                                        str.AppendLine("");
                                        str.AppendLine("🔗 شما با خرید این اشتراک میتوانید با تمامی اینترنت ها متصل شوید.");
                                        str.AppendLine("");
                                        str.AppendLine("⭐️ شما میتوانید به مراحل قبل برگردید و اشتراک را تغییر دهید یا از همین مرحله خرید خود را تایید کنید.");
                                        str.AppendLine("");
                                        str.AppendLine("");

                                        var keys = Keyboards.GetAccpetBuyUnlimtedFromWallet(Plan.Plan_ID);

                                        await bot.Client.EditMessageTextAsync(User.Tel_UniqUserID, update.CallbackQuery.Message.MessageId, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keys);

                                        await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "AccpetPayUnlimited", db, botName);
                                        return;
                                    }

                                }

                                #endregion

                                #region پرداخت از کیف پول ( اشتراک نامحدود )

                                if (callback.Length == 2)
                                {
                                    if (callback[0] == "AccpetWalletUnlimited")
                                    {
                                        var PlanID = callback[1];
                                        var Plan = await tbPlansRepository.FirstOrDefaultAsync(s => s.Plan_ID.ToString() == PlanID);
                                        var AccountName = "";
                                        if (User.Tel_Data != null)
                                        {
                                            AccountName = User.Tel_Data.Split('%')[0];
                                        }
                                        var Wallet = UserAcc.Tel_Wallet;
                                        var Price = Plan.Price;
                                        if (BotSettings.Present_Discount != null && BotSettings.Present_Discount != 0)
                                        {
                                            Price -= (int)(Price * BotSettings.Present_Discount);
                                        }
                                        var PirceWithoutDiscount = Plan.Price;
                                        if (Wallet >= Price)
                                        {
                                            var Link = await tbLinksRepository.FirstOrDefaultAsync(p => p.tbL_Email == AccountName);
                                            if (Link != null)
                                            {
                                                MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);

                                                await mySql.OpenAsync();

                                                var Disc1 = new Dictionary<string, object>();
                                                Disc1.Add("@tbL_Email", Link.tbL_Email);


                                                var Reader = await mySql.GetDataAsync("select * from v2_user where email like @tbL_Email", Disc1);
                                                var Ended = false;
                                                while (await Reader.ReadAsync())
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
                                                    order.Traffic = Plan.PlanVolume;
                                                    order.Month = Plan.PlanMonth;
                                                    order.PriceWithOutDiscount = PirceWithoutDiscount;
                                                    order.V2_Plan_ID = Plan.Plan_ID_V2;
                                                    order.FK_Tel_UserID = UserAcc.Tel_UserID;
                                                    order.Tel_RenewedDate = DateTime.Now;
                                                    var UserAc = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName).FirstOrDefault();
                                                    UserAc.Tel_Wallet -= Price;
                                                    var t = Utility.ConvertGBToByte(Convert.ToInt64(order.Traffic));

                                                    string exp = DateTime.Now.AddDays((int)(order.Month * 30)).ConvertDatetimeToSecond().ToString();

                                                    Link.tbL_Warning = false;
                                                    var Disc3 = new Dictionary<string, object>();
                                                    Disc3.Add("@DefaultPlanIdInV2board", Plan.Plan_ID_V2);
                                                    Disc3.Add("@transfer_enable", t);
                                                    Disc3.Add("@exp", exp);
                                                    Disc3.Add("@email", Link.tbL_Email);
                                                    Disc3.Add("@group", Plan.Group_Id);
                                                    var DeviceLimit_Structur = "";
                                                    if (Plan.device_limit != null)
                                                    {
                                                        DeviceLimit_Structur = ",device_limit=" + Plan.device_limit;
                                                        //Disc3.Add("@device_limit", Plan.device_limit);
                                                    }

                                                    var Query = "update v2_user set u=0,d=0,t=0,plan_id=@DefaultPlanIdInV2board,group_id=@group" + DeviceLimit_Structur + ", transfer_enable=@transfer_enable,expired_at=@exp where email=@email";
                                                    var reader = await mySql.GetDataAsync(Query, Disc3);
                                                    var result = await reader.ReadAsync();
                                                    reader.Close();



                                                    var InlineKeyboardMarkup = Keyboards.GetHomeButton();

                                                    Link.tbL_Warning = false;
                                                    Link.tb_AutoRenew = false;
                                                    tbOrdersRepository.Insert(order);
                                                    await tbLinksRepository.SaveChangesAsync();
                                                    await tbUsersRepository.SaveChangesAsync();
                                                    await tbOrdersRepository.SaveChangesAsync();
                                                    await tbTelegramUserRepository.SaveChangesAsync();

                                                    StringBuilder str2 = new StringBuilder();
                                                    str2.AppendLine("✅ بسته شما با موفقیت تمدید شد");
                                                    str2.AppendLine("");
                                                    str2.AppendLine("♨️ می توانید برای مشاهده اطلاعات اکانت و بسته به بخش مدیریت اشتراک ها مراجعه کنید");
                                                    await RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                                    var kyes = Keyboards.GetHomeButton();
                                                    await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, str2.ToString(), parseMode: ParseMode.Html, replyMarkup: kyes);
                                                    await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);


                                                    BotSettings.tbUsers.Wallet += Price;
                                                    await BotSettingRepository.SaveChangesAsync();
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
                                                    order.Traffic = Plan.PlanVolume;
                                                    order.Month = Plan.PlanMonth;
                                                    order.PriceWithOutDiscount = PirceWithoutDiscount;
                                                    order.V2_Plan_ID = Plan.Plan_ID_V2;
                                                    order.FK_Tel_UserID = UserAcc.Tel_UserID;

                                                    var UserAc = await tbTelegramUserRepository.FirstOrDefaultAsync(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName);
                                                    UserAc.Tel_Wallet -= Price;
                                                    order.Order_Price = Price;


                                                    tbOrdersRepository.Insert(order);
                                                    await tbOrdersRepository.SaveChangesAsync();
                                                    await tbLinksRepository.SaveChangesAsync();

                                                    StringBuilder str = new StringBuilder();
                                                    str.AppendLine("✅ بسته شما با موفقیت رزرو شد");
                                                    str.AppendLine("");
                                                    str.AppendLine("⚠️ بعد از اتمام حجم بسته یا زمان بسته فعلی اکانت شما به صورت خودکار تمدید می شود");
                                                    str.AppendLine("");
                                                    await RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                                    await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                                    var kyes = Keyboards.GetHomeButton();
                                                    await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, str.ToString(), replyMarkup: kyes, parseMode: ParseMode.Html);

                                                    BotSettings.tbUsers.Wallet += PirceWithoutDiscount;
                                                    await BotSettingRepository.SaveChangesAsync();
                                                }
                                                await mySql.CloseAsync();
                                                return;
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
                                                    var acc = tbLinksRepository.Where(p => p.tbL_Email == Order.AccountName).Count();
                                                    acc += 1;

                                                    AccountName += User.Tel_Username + acc;
                                                }
                                                Order.AccountName = AccountName;
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
                                                Order.Traffic = Plan.PlanVolume;
                                                Order.Month = Plan.PlanMonth;
                                                Order.V2_Plan_ID = Plan.Plan_ID_V2;
                                                Order.FK_Tel_UserID = UserAcc.Tel_UserID;
                                                Order.Order_Price = Price;
                                                Order.PriceWithOutDiscount = PirceWithoutDiscount;

                                                string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];

                                                var FullName = Order.AccountName;

                                                var t = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));

                                                string exp = DateTime.Now.AddDays((int)(Order.Month * 30)).ConvertDatetimeToSecond().ToString();

                                                MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);
                                                await mySql.OpenAsync();
                                                var Disc1 = new Dictionary<string, object>();
                                                Disc1.Add("@V2board", Plan.Plan_ID_V2);
                                                var reader = await mySql.GetDataAsync("select group_id,transfer_enable from v2_plan where id =@V2board", Disc1);
                                                long tran = 0;
                                                int grid = 0;
                                                while (await reader.ReadAsync())
                                                {
                                                    tran = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));
                                                    grid = reader.GetInt32("group_id");
                                                }
                                                string create = DateTime.Now.ConvertDatetimeToSecond().ToString();

                                                var Disc3 = new Dictionary<string, object>();
                                                Disc3.Add("@FullName", FullName);
                                                Disc3.Add("@expired", exp);
                                                Disc3.Add("@create", create);
                                                Disc3.Add("@guid", Guid.NewGuid());
                                                Disc3.Add("@tran", tran);
                                                Disc3.Add("@grid", grid);
                                                Disc3.Add("@V2boardId", Plan.Plan_ID_V2);
                                                Disc3.Add("@token", token);
                                                Disc3.Add("@passwrd", Guid.NewGuid());

                                                var DeviceLimit_Structur = "";
                                                var DeviceLimit_data = "";

                                                if (Plan.device_limit != null)
                                                {
                                                    DeviceLimit_Structur = ",device_limit";
                                                    Disc3.Add("@device_limit", Plan.device_limit);
                                                    DeviceLimit_data = ",@device_limit";
                                                }



                                                string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at" + DeviceLimit_Structur + ") VALUES (@FullName,@expired,@create,@guid,0,0,0,@tran,0,@grid,@V2boardId,@token,@passwrd,@create" + DeviceLimit_data + ")";
                                                reader.Close();

                                                reader = await mySql.GetDataAsync(Query, Disc3);
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
                                                st.AppendLine("◀️ برای نمایش جزئیات اشتراک به بخش مدیریت اشتراک ها مراجعه کنید");
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
                                                await mySql.CloseAsync();


                                                var UserAc = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName).FirstOrDefault();
                                                if (UserAc != null)
                                                {
                                                    UserAc.Tel_Wallet -= Price;

                                                }

                                                List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();

                                                List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();
                                                row2.Add(InlineKeyboardButton.WithCallbackData("📚 راهنمای اتصال", "ConnectionHelp"));
                                                inlineKeyboards.Add(row2);
                                                var keyboard = new InlineKeyboardMarkup(inlineKeyboards);

                                                tbOrdersRepository.Insert(Order);
                                                await tbOrdersRepository.SaveChangesAsync();
                                                await tbTelegramUserRepository.SaveChangesAsync();
                                                tbLinksRepository.Insert(tbLinks);
                                                await tbLinksRepository.SaveChangesAsync();
                                                await RepositoryLinkUserAndPlan.SaveChangesAsync();

                                                var keys = Keyboards.GetHomeButton();

                                                //await botClient.SendTextMessageAsync(UserAcc.Tel_UniqUserID, "✅ اکانت شما با موفقیت ایجاد شد", replyMarkup: keys);

                                                await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ اکانت شما با موفقیت ایجاد شد", true);


                                                await bot.Client.SendPhotoAsync(
                                                  chatId: UserAcc.Tel_UniqUserID,
                                                  photo: image,
                                                  caption: st.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);

                                                await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, "به منو اصلی بازگشتید 🏘", parseMode: ParseMode.Html, replyMarkup: keys);
                                                await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                                await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                                BotSettings.tbUsers.Wallet += PirceWithoutDiscount;
                                                await BotSettingRepository.SaveChangesAsync();
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
                                }


                                #endregion

                                #endregion

                                #region ایجاد اشتراک تست

                                #region پریمیوم

                                if (callbackQuery.Data == "premium_test")
                                {
                                    if (UserAcc.Tel_GetedTestAccountUnlimited == false || UserAcc.Tel_GetedTestAccountUnlimited == null)
                                    {

                                        MySqlEntities mySql = new MySqlEntities(BotSettings.tbUsers.tbServers.ConnectionString);
                                        await mySql.OpenAsync();
                                        var Disc1 = new Dictionary<string, object>();
                                        Disc1.Add("@v2board_id", V2boardPlanId);
                                        long tran = Utility.ConvertGBToByte(500);
                                        int grid = BotSettings.GroupId_testUnlimited.Value;

                                        string create = DateTime.Now.ConvertDatetimeToSecond().ToString();
                                        string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                                        string exp = DateTime.Now.AddHours(2).ConvertDatetimeToSecond().ToString();

                                        var isExists = true;
                                        var FullName = "";
                                        while (isExists)
                                        {
                                            Random ran = new Random();
                                            FullName = Guid.NewGuid().ToString().Split('-')[0] + "$" + ran.Next(999) + "@" + BotSettings.tbUsers.Username;
                                            var Disc2 = new Dictionary<string, object>();
                                            Disc2.Add("@FullName", FullName);
                                            var reader2 = await mySql.GetDataAsync("select * from v2_user where email=@FullName", Disc2);
                                            if (!reader2.Read())
                                            {
                                                isExists = false;
                                            }
                                            reader2.Close();
                                        }

                                        var Disc3 = new Dictionary<string, object>();
                                        Disc3.Add("@FullName", FullName);
                                        Disc3.Add("@expired", exp);
                                        Disc3.Add("@create", create);
                                        Disc3.Add("@guid", Guid.NewGuid());
                                        Disc3.Add("@tran", tran);
                                        Disc3.Add("@grid", grid);
                                        Disc3.Add("@V2boardId", V2boardPlanId);
                                        Disc3.Add("@token", token);
                                        Disc3.Add("@passwrd", Guid.NewGuid());


                                        var DeviceLimit_Structur = "";
                                        var DeviceLimit_data = "";

                                        if (BotSettings.tbPlans.device_limit != null)
                                        {
                                            DeviceLimit_Structur = ",device_limit";
                                            Disc3.Add("@device_limit", BotSettings.tbPlans.device_limit);
                                            DeviceLimit_data = ",@device_limit";
                                        }

                                        string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at" + DeviceLimit_Structur + ") VALUES (@FullName, @expired, @create, @guid, 0, 0, 0, @tran, 0, @grid, @V2boardId, @token,@passwrd,@create" + DeviceLimit_data + ")";


                                        var reader = await mySql.GetDataAsync(Query, Disc3);
                                        reader.Close();

                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("🌿 کاربر عزیز اشتراک تست شما با موفقیت ساخته شد❕");
                                        str.AppendLine("");
                                        str.AppendLine("💢 شناسه اشتراک : " + FullName.Split('@')[0]);
                                        str.AppendLine("");
                                        str.AppendLine("🚦 حجم کل : 500 گیگ (مصرف منصفانه )");
                                        str.AppendLine("⏳ مدت زمان : 2 ساعت");
                                        str.AppendLine("");
                                        str.AppendLine("🔗 لینک اتصال: ");
                                        str.AppendLine("👇👇👇👇👇👇👇");
                                        str.AppendLine("");
                                        var SubLink = "https://" + Server.SubAddress + "/api/v1/client/subscribe?token=" + token;
                                        str.AppendLine("<code>" + SubLink + "</code>");
                                        str.AppendLine("");
                                        str.AppendLine("⁉️ برای دریافت راهنما به بخش \"📘 آموزش اتصال\" بروید.");
                                        await mySql.CloseAsync();

                                        str.AppendLine("");
                                        str.AppendLine("🚀 @" + BotSettings.Bot_ID);

                                        var tbTelegram = tbTelegramUserRepository.Where(s => s.Tel_UniqUserID == User.Tel_UniqUserID).FirstOrDefault();
                                        if (tbTelegram != null)
                                        {
                                            tbTelegram.Tel_GetedTestAccountUnlimited = true;
                                        }
                                        tbTelegramUserRepository.Save();

                                        await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("❌ شما قبلا اشتراک تست پرمیوم دریافت کرده اید");
                                        str.AppendLine("");
                                        str.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, str.ToString());
                                    }
                                }

                                #endregion

                                #region گُلد

                                if (callbackQuery.Data == "gold_test")
                                {
                                    if (UserAcc.Tel_GetedTestAccount == false || UserAcc.Tel_GetedTestAccount == null)
                                    {

                                        MySqlEntities mySql = new MySqlEntities(BotSettings.tbUsers.tbServers.ConnectionString);
                                        await mySql.OpenAsync();
                                        var Disc1 = new Dictionary<string, object>();
                                        Disc1.Add("@v2board_id", V2boardPlanId);
                                        long tran = Utility.ConvertGBToByte(0.5);
                                        int grid = BotSettings.GroupId_test.Value;
                                        string create = DateTime.Now.ConvertDatetimeToSecond().ToString();
                                        string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                                        string exp = DateTime.Now.AddDays(1).ConvertDatetimeToSecond().ToString();
                                        var isExists = true;
                                        var FullName = "";
                                        while (isExists)
                                        {
                                            Random ran = new Random();
                                            FullName = Guid.NewGuid().ToString().Split('-')[0] + "$" + ran.Next(999) + "@" + BotSettings.tbUsers.Username;
                                            var Disc2 = new Dictionary<string, object>();
                                            Disc2.Add("@FullName", FullName);
                                            var reader2 = await mySql.GetDataAsync("select * from v2_user where email=@FullName", Disc2);
                                            if (!reader2.Read())
                                            {
                                                isExists = false;
                                            }
                                            reader2.Close();
                                        }

                                        var Disc3 = new Dictionary<string, object>();
                                        Disc3.Add("@FullName", FullName);
                                        Disc3.Add("@expired", exp);
                                        Disc3.Add("@create", create);
                                        Disc3.Add("@guid", Guid.NewGuid());
                                        Disc3.Add("@tran", tran);
                                        Disc3.Add("@grid", grid);
                                        Disc3.Add("@V2boardId", V2boardPlanId);
                                        Disc3.Add("@token", token);
                                        Disc3.Add("@passwrd", Guid.NewGuid());


                                        var DeviceLimit_Structur = "";
                                        var DeviceLimit_data = "";

                                        if (BotSettings.tbPlans.device_limit != null)
                                        {
                                            DeviceLimit_Structur = ",device_limit";
                                            Disc3.Add("@device_limit", BotSettings.tbPlans.device_limit);
                                            DeviceLimit_data = ",@device_limit";
                                        }

                                        string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at" + DeviceLimit_Structur + ") VALUES (@FullName, @expired, @create, @guid, 0, 0, 0, @tran, 0, @grid, @V2boardId, @token,@passwrd,@create" + DeviceLimit_data + ")";


                                        var reader = await mySql.GetDataAsync(Query, Disc3);
                                        reader.Close();

                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("🌿 کاربر عزیز اشتراک تست شما با موفقیت ساخته شد❕");
                                        str.AppendLine("");
                                        str.AppendLine("💢 شناسه اشتراک : " + FullName.Split('@')[0]);
                                        str.AppendLine("");
                                        str.AppendLine("🚦 حجم کل : 500 مگ");
                                        str.AppendLine("⏳ مدت زمان : یک روز");
                                        str.AppendLine("");
                                        str.AppendLine("🔗 لینک اتصال: ");
                                        str.AppendLine("👇👇👇👇👇👇👇");
                                        str.AppendLine("");
                                        var SubLink = "https://" + Server.SubAddress + "/api/v1/client/subscribe?token=" + token;
                                        str.AppendLine("<code>" + SubLink + "</code>");
                                        str.AppendLine("");
                                        str.AppendLine("⁉️ برای دریافت راهنما به بخش \"📘 آموزش اتصال\" بروید.");
                                        await mySql.CloseAsync();

                                        str.AppendLine("");
                                        str.AppendLine("🚀 @" + BotSettings.Bot_ID);

                                        var tbTelegram = tbTelegramUserRepository.Where(s => s.Tel_UniqUserID == User.Tel_UniqUserID).FirstOrDefault();
                                        if (tbTelegram != null)
                                        {
                                            tbTelegram.Tel_GetedTestAccount = true;
                                        }
                                        tbTelegramUserRepository.Save();
                                        await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("❌ شما قبلا اشتراک تست گُلد دریافت کرده اید");
                                        str.AppendLine("");
                                        str.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, str.ToString());
                                    }
                                }

                                #endregion

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

                        else
                        {
                            StringBuilder str2 = new StringBuilder();
                            str2.AppendLine("⚠️ با عرض پوزش ربات برای مدت کمی از دسترس خارج شده لطفا بعدا تلاش کنید");
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
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در برقراری ارتباط سرور تلگرام");
            }

            return;
        }



        private bool CheckExistsAccountInBot(string username, tbBotSettings BotSetting)
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

        private async Task<bool> EndedVolumeOrDate(string username)
        {
            MySqlEntities mysql = new MySqlEntities(Server.ConnectionString);
            await mysql.OpenAsync();
            var Disc1 = new Dictionary<string, object>();
            Disc1.Add("@email", username);
            var reader = await mysql.GetDataAsync("select * from v2_user where email = @email", Disc1);
            if (await reader.ReadAsync())
            {
                var vol = reader.GetInt64("transfer_enable") - (reader.GetDouble("d") + reader.GetDouble("u"));
                if (vol <= 0)
                {
                    reader.Close();
                    await mysql.CloseAsync();
                    return true;
                }
                var ExpireTime = reader.GetBodyDefinition("expired_at");
                if (ExpireTime != "")
                {
                    var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(ExpireTime));
                    if (ex <= DateTime.Now)
                    {
                        reader.Close();
                        await mysql.CloseAsync();
                        return true;
                    }

                }
            }

            return false;
        }

        private async Task SendTrafficCalculator(tbTelegramUsers User, int MessageId, tbBotSettings BotSetting, TelegramBotClient bot, string botName, string Data = null, string Tel_Step = null)
        {

            CustomTrafficKeyboard keyboard = new CustomTrafficKeyboard(BotSetting, User.Tel_Traffic, User.Tel_Monthes);
            var key = keyboard.GetKeyboard();

            StringBuilder str = new StringBuilder();
            str.AppendLine("🔐 اشتراکت رو خودت بساز");
            str.AppendLine("");
            str.AppendLine("💸 به ازای هر گیگ  : " + BotSetting.PricePerGig_Major.ConvertToMony() + " تومان");
            str.AppendLine("");
            str.AppendLine("⏳ هر ماه : " + BotSetting.PricePerMonth_Major.ConvertToMony() + " تومان");

            await bot.EditMessageTextAsync(chatId: User.Tel_UniqUserID, messageId: MessageId, str.ToString(), replyMarkup: key);
        }

        public async Task<bool> SaveUserProfilePicture(long userId, TelegramBotClient bot, string token, string path)
        {

            if (!System.IO.File.Exists(path))
            {
                var userProfilePhotos = await bot.GetUserProfilePhotosAsync(userId);

                if (userProfilePhotos.TotalCount > 0)
                {
                    var photo = userProfilePhotos.Photos.First()[0];
                    var file = await bot.GetFileAsync(photo.FileId);

                    var fileUrl = $"https://api.telegram.org/file/bot{token}/{file.FilePath}";
                    HttpClient httpClient;
                    var Sock = new tbSocks5();
                    using (Entities db = new Entities())
                    {
                        var Sok = db.tbSocks5.Where(s => s.Active == true).FirstOrDefault();
                        if (Sok != null)
                        {
                            Sock = Sok;
                        }
                        else
                        {
                            Sock = null;
                        }
                    }
                    if (Sock != null)
                    {
                        // آدرس پروکسی و پورت
                        var proxy = new HttpToSocks5Proxy(Sock.HostName, Sock.Port, username: Sock.Username, password: Sock.Password);

                        // تنظیمات TelegramBotClient با پروکسی

                        httpClient = new HttpClient(new HttpClientHandler
                        {
                            Proxy = proxy,
                            UseProxy = true
                        });
                    }
                    else
                    {
                        httpClient = new HttpClient();
                    }



                    var response = await httpClient.GetAsync(fileUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var imageData = await response.Content.ReadAsByteArrayAsync();

                        // تعیین مسیر ذخیره‌سازی فایل
                        var fileName = $"{userId}.jpg";
                        var savePath = Path.Combine(HttpContext.Current.Server.MapPath("~/assets/img/TelegramUserProfiles"), fileName);

                        // ذخیره فایل در سرور
                        System.IO.File.WriteAllBytes(savePath, imageData);

                        // اگر از پایگاه داده استفاده می‌کنید، مسیر فایل را ذخیره کنید
                        // برای مثال:
                        // user.ProfileImagePath = "/UserProfiles/" + fileName;
                        // db.SaveChanges();

                        return true;
                    }

                }
            }



            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


    }
}
