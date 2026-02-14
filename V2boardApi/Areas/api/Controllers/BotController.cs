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
using StackExchange.Redis;
using V2boardBot.Tools;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Net;
using V2boardApi.PaymentMethods;
using static V2boardApi.PaymentMethods.TetraPay;


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

        private NowPayment nowPayment;
        private HubSmartAPI HubSmartAPI;
        private ZarinPalPayment ZarinPal;
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
                    if (BotSettings.Enabled == false)
                    {
                        StringBuilder str2 = new StringBuilder();
                        str2.AppendLine("😔❤️ اوه عزیزم خیلی ببخشید ربات غیرفعال شده یکم صبر کنی زودی برمیگردیم ");
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
                        return;
                    }
                    if (BotSettings.Enabled && BotSettings.tbUsers.Wallet <= BotSettings.tbUsers.Limit)
                    {
                        nowPayment = new NowPayment(db, "https://api.nowpayments.io", BotSettings.NowPayment_API_KEY);
                        ZarinPal = new ZarinPalPayment(BotSettings.PaymentGateWay_Merchant_ID, BotSettings.PaymentGateWay_Key);
                        HubSmartAPI = new HubSmartAPI(BotSettings.HubSmart_API_KEY);
                        var tbTelegramUserRepository = new Repository<tbTelegramUsers>(db);
                        var tbServerRepository = new Repository<tbServers>(db);
                        var tbLinksRepository = new Repository<tbLinks>(db);
                        var tbPlansRepository = new Repository<tbPlans>(db);
                        var tbUsersRepository = new Repository<tbUsers>(db);
                        var tbOrdersRepository = new Repository<tbOrders>(db);
                        var RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
                        var tbDepositLogRepo = new Repository<tbDepositWallet_Log>(db);
                        var tbServerGroupsRepo = new Repository<tbServerGroups>(db);
                        var PaymentMethodRepo = new Repository<tbPaymentMethods>(db);
                        var PaymentMethodUserRepo = new Repository<tbPaymentMethodUser>(db);
                        var SettingRepo = new Repository<tbSettings>(db);
                        var V2boardPlanId = BotSettings.tbPlans.Plan_ID_V2;
                        long chatid = 0;
                        tbTelegramUsers UserAcc = new tbTelegramUsers();
                        if (update.Message is Telegram.Bot.Types.Message message)
                        {

                            chatid = update.Message.From.Id;
                            var mess = message.Text;

                            if (!string.IsNullOrEmpty(mess) && mess.StartsWith("@") && BotSettings.AdminBot_ID == chatid)
                            {
                                if (mess.Contains("="))
                                {
                                    var Username = mess.Split('=')[0];
                                    Username = Username.Remove(0, 1);
                                    var TelegramUser = tbTelegramUserRepository.Where(a => a.Tel_Username == Username).FirstOrDefault();
                                    if (TelegramUser != null)
                                    {
                                        var OrgMessage = mess.Split('=')[1];

                                        await bot.Client.SendTextMessageAsync(TelegramUser.Tel_UniqUserID, OrgMessage, parseMode: ParseMode.Html);
                                        await bot.Client.SendTextMessageAsync(chatid, "✅ پیام با موفقیت ارسال شد", parseMode: ParseMode.Html);
                                        return;
                                    }
                                    else
                                    {
                                        await bot.Client.SendTextMessageAsync(chatid, "❌ کاربری با این آیدی یافت نشد", parseMode: ParseMode.Html);
                                        return;
                                    }
                                }
                                else
                                {
                                    await bot.Client.SendTextMessageAsync(chatid, "ادمین عزیز بعد از درج نام کاربری = گذاشته و سپس پیام خود را بنویسید", parseMode: ParseMode.Html);
                                    return;
                                }
                            }

                            var CardToCardStatus = BotSettings.tbUsers.tbPaymentMethodUser.Where(a => a.tbPaymentMethods.tbpm_Key == "CardToCard" && a.tbpu_Status).Any();
                            if (CardToCardStatus)
                            {
                                if (BotSettings.AdminBot_ID != chatid)
                                {
                                    #region ارسال رسید تراکنش برای ادمین

                                    if (update.Message.Type == MessageType.Photo)
                                    {
                                        var Deposit = await tbDepositLogRepo.WhereAsync(p => p.dw_Status == "FOR_PAY" && p.tbTelegramUsers.Tel_UniqUserID == chatid.ToString());

                                        if (Deposit.Count == 0)
                                        {
                                            StringBuilder st = new StringBuilder();
                                            st.Append("❗️فاکتوری برای این رسید یافت نشد یا قبلا تائید شده است");
                                            await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, st.ToString(), replyMarkup: inlineKeyboardMarkup, replyToMessageId: message.MessageId, parseMode: ParseMode.Html);
                                            return;
                                        }

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
                                        var users = BotSettings.tbUsers.tbTelegramUsers.ToList();

                                        // پیدا کردن طول دستور (مثلاً "/all " یا فقط "/all")
                                        var m = Regex.Match(message.Text, @"^/all(\s+)?");
                                        int removeLen = m.Success ? m.Length : 4; // محافظت در برابر حالت‌های عجیب

                                        // متن جدید (بدون /all)
                                        var newText = message.Text.Substring(removeLen);

                                        // اصلاح/کپی کردن entityها (اگر وجود داشته باشه)
                                        MessageEntity[] adjustedEntities = null;
                                        if (message.Entities != null && message.Entities.Length > 0)
                                        {
                                            var list = new List<MessageEntity>();
                                            foreach (var e in message.Entities)
                                            {
                                                int entStart = e.Offset;
                                                int entEnd = e.Offset + e.Length; // exclusive end

                                                // اگر entity کاملاً قبل از بخش حذف‌شده بود -> حذفش کن (معمولاً برای دستور /all پیش میاد)
                                                if (entEnd <= removeLen)
                                                {
                                                    continue;
                                                }

                                                // اگر entity کاملاً بعد از بخش حذف‌شده -> offset رو کم کن
                                                if (entStart >= removeLen)
                                                {
                                                    var ne = new MessageEntity
                                                    {
                                                        Type = e.Type,
                                                        Offset = e.Offset - removeLen,
                                                        Length = e.Length,
                                                        Url = e.Url,
                                                        User = e.User,
                                                        Language = e.Language,
                                                        CustomEmojiId = e.CustomEmojiId
                                                    };
                                                    list.Add(ne);
                                                    continue;
                                                }

                                                // اگر entity قسمتی در بخش حذف‌شده و قسمتی در متن جدید داره -> کوتاهش کن و قرارش بده در ابتدای متن جدید
                                                if (entStart < removeLen && entEnd > removeLen)
                                                {
                                                    int newLen = entEnd - removeLen; // بخشی که بعد از حذف مانده
                                                    var ne = new MessageEntity
                                                    {
                                                        Type = e.Type,
                                                        Offset = 0,
                                                        Length = newLen,
                                                        Url = e.Url,
                                                        User = e.User,
                                                        Language = e.Language,
                                                        CustomEmojiId = e.CustomEmojiId
                                                    };
                                                    list.Add(ne);
                                                    continue;
                                                }
                                            }

                                            adjustedEntities = list.ToArray();
                                        }

                                        // ارسال به همه کاربران (هر کاربر را جداگانه با entities ارسال کن)
                                        Task.Run(async () =>
                                        {
                                            foreach (var item in users)
                                            {
                                                try
                                                {
                                                    // استفاده از entities بجای parseMode
                                                    await bot.Client.SendTextMessageAsync(
                                                        chatId: item.Tel_UniqUserID,
                                                        text: newText,
                                                        entities: adjustedEntities
                                                    );
                                                }
                                                catch (Exception ex)
                                                {
                                                    // لاگ کن یا هندل کن
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

                                    if (mess == "/start" || mess.StartsWith("/start"))
                                    {


                                        inlineKeyboardMarkup = Keyboards.GetHomeButton();


                                        StringBuilder st = new StringBuilder();

                                        st.AppendLine("<b>" + " 🌍 خوش اومدی به دنیای اینترنت بدون محدودیت! " + "</b>");
                                        st.AppendLine("");
                                        st.AppendLine("اگه این اولین بارته، باید بدونی یه تجربه‌ی متفاوت در انتظارت ـه! 🔥");
                                        st.AppendLine("");
                                        st.AppendLine("✅ سرورهای ما توی چندین لوکیشن پرسرعت و باکیفیت فعالن:\r");
                                        st.AppendLine("🇩🇪 آلمان | 🇺🇸 آمریکا | 🇫🇮 فنلاند | 🇹🇷 ترکیه\r\n🇳🇱 هلند | 🇫🇷 فرانسه | 🇬🇧 انگلیس");
                                        st.AppendLine("");
                                        st.AppendLine("✨ با هر اشتراک، می‌تونی بین این لوکیشن‌ها جابجا شی و همیشه بهترین پینگ و سرعت رو داشته باشی — مخصوصاً برای گیم، استریم و کارهای حرفه‌ای 🎮🎬💼");
                                        st.AppendLine("");
                                        st.AppendLine("🔒 بدون محدودیت اتصال\r\n🚀 سرعت بالا روی همه اینترنت‌ها\r\n\U0001f9e1 پشتیبانی همیشه در دسترس\r\n\r\n📢 تازه اگه هنوز مطمئن نیستی، می‌تونی یه اشتراک تست رایگان هم بگیری و امتحانش کنی!\r\nفقط چند ثانیه زمان می‌بره، ولی تجربه‌ش برات فرق می‌کنه 😉");
                                        st.AppendLine("🆔 @" + BotSettings.Bot_ID);

                                        Usr.Tel_RobotID = RobotIDforTimer;

                                        Usr.FK_User_ID = BotSettings.tbUsers.User_ID;
                                        tbTelegramUserRepository.Insert(Usr);
                                        await tbTelegramUserRepository.SaveChangesAsync();
                                        UserAcc = Usr;
                                        var Path = HttpContext.Current.Server.MapPath("~/assets/img/TelegramUserProfiles/" + chatid + ".jpg");
                                        await SaveUserProfilePicture(chatid, bot.Client, bot.Token, Path);
                                        await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);

                                        var task = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, st.ToString(), replyMarkup: inlineKeyboardMarkup, replyToMessageId: message.MessageId, parseMode: ParseMode.Html);
                                        return;

                                    }



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

                                    if (BotSettings.IsNotActiveSell)
                                    {
                                        StringBuilder st2 = new StringBuilder();

                                        st2.AppendLine("<b>" + "❌ به اطلاع می‌رسانیم فروش در حال حاضر به‌صورت موقت متوقف شده است. ❌" + "</b>");
                                        st2.AppendLine("");

                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, st2.ToString(), parseMode: ParseMode.Html);

                                    }
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

                                #region  گزینه مدیریت اشتراک ها و خرید و تمدید

                                #region بخش فشردن گزینه خرید سرویس



                                else if (mess == "🛒 خرید اشتراک")
                                {

                                    if (!BotSettings.IsNotActiveSell)
                                    {
                                        await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);


                                        //await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "SelectSubType", db, botName);

                                        var AccName = "";
                                        if (User.Tel_Username != null)
                                        {
                                            var Link = await tbLinksRepository.FirstOrDefaultAsync(a => a.tbL_Email.Contains(User.Tel_Username) && a.tbTelegramUsers.Tel_UniqUserID == UserAcc.Tel_UniqUserID);
                                            if (Link != null)
                                            {
                                                AccName = User.Tel_Username + Guid.NewGuid().ToString().Split('-')[0] + "$" + Guid.NewGuid().ToString().Split('-')[0] + "@" + botName;
                                            }
                                            else
                                            {
                                                AccName = User.Tel_Username + "$" + Guid.NewGuid().ToString().Split('-')[0] + "@" + botName;
                                            }

                                        }
                                        else
                                        {

                                            AccName = Guid.NewGuid().ToString().Split('-')[0] + "$" + Guid.NewGuid().ToString().Split('-')[0] + "@" + botName;
                                        }


                                        var keyboard = Keyboards.GetPlansKeyboard(AccName, RepositoryLinkUserAndPlan);

                                        StringBuilder str = new StringBuilder();

                                        var ordered = RepositoryLinkUserAndPlan.Where(s => s.L_SellPrice != null && s.L_ShowInBot == true && s.L_FK_U_ID == BotSettings.FK_User_ID && s.L_Status == true).OrderBy(s => s.tbPlans.PlanMonth).ThenBy(s => s.tbPlans.PlanVolume);


                                        if (BotSettings.Present_Discount != null)
                                        {

                                            str.Append("<b>" + "🚦 بسته مناسب خودتو انتخاب کن!\r\n " + "</b>");
                                            str.AppendLine("");
                                            str.AppendLine("");
                                            str.AppendLine("با توجه به مصرف اینترنتت، ما تعرفه ‌هایی با حجم و زمان ‌های مختلف آماده کردیم. کافیه ببینی چقدر مصرف داری و همون تعرفه رو فعال کنی 💥\r\n\r\n"); str.AppendLine("");
                                            str.AppendLine("💥 با " + "%" + BotSettings.Present_Discount * 100 + " تخفیف ویژه 💥");
                                            str.AppendLine("");
                                            str.AppendLine("<b>" + "اشتراک های حجمی :" + "</b>");
                                            var Counter = 1;
                                            foreach (var item in ordered)
                                            {
                                                str.AppendLine(Counter + " - " + item.tbPlans.PlanMonth + " ماهه " + item.tbPlans.PlanVolume + " گیگ" + " | " + "<s>" + item.L_SellPrice.Value.ConvertToMony() + "</s>" + " 👈 " + (item.L_SellPrice.Value - (item.L_SellPrice.Value * BotSettings.Present_Discount)).Value.ConvertToMony() + " تومان");

                                                Counter++;
                                            }

                                            str.AppendLine("");
                                            str.AppendLine("💢 اشتراک های حجمی فاقد محدودیت کاربر هستند");
                                        }
                                        else
                                        {
                                            str.Append("<b>" + "🚦 بسته مناسب خودتو انتخاب کن!\r\n " + "</b>");
                                            str.AppendLine("");
                                            str.AppendLine("");
                                            str.AppendLine("با توجه به مصرف اینترنتت، ما تعرفه ‌هایی با حجم و زمان ‌های مختلف آماده کردیم. کافیه ببینی چقدر مصرف داری و همون تعرفه رو فعال کنی 💥\r\n\r\n");
                                            var Counter = 1;
                                            str.AppendLine("<b>" + "اشتراک های حجمی :" + "</b>");
                                            foreach (var item in ordered)
                                            {

                                                str.AppendLine(Counter + " - " + item.tbPlans.PlanMonth + " ماهه " + item.tbPlans.PlanVolume + " گیگ" + " 👈 " + item.L_SellPrice.Value.ConvertToMony() + " تومان");

                                                Counter++;
                                            }

                                            str.AppendLine("");
                                            str.AppendLine("💢 اشتراک های حجمی فاقد محدودیت کاربر هستند");

                                        }


                                        str.AppendLine("");

                                        str.AppendLine("");
                                        str.AppendLine("〰️〰️〰️〰️〰️");
                                        str.AppendLine("🚀@" + BotSettings.Bot_ID);
                                        //await SendTrafficCalculator(UserAcc, BotSettings, bot.Client, botName, messageId: callbackQuery.Message.MessageId);


                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), replyMarkup: keyboard, parseMode: ParseMode.Html);

                                        //await SendTrafficCalculator(UserAcc, callbackQuery.Message.MessageId, BotSettings, bot.Client, botName, callbackQuery.Data);

                                        return;
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("💢 با عرض پوزش فروش ربات موقتا غیرفعال شده است .");
                                        str.AppendLine("");
                                        str.AppendLine("〰️〰️〰️〰️〰️");
                                        str.AppendLine("🚀@" + BotSettings.Bot_ID);
                                        //await SendTrafficCalculator(UserAcc, BotSettings, bot.Client, botName, messageId: callbackQuery.Message.MessageId);


                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);
                                    }
                                }

                                #endregion

                                #region دکمه سرویس ها
                                if (mess == "🌐 مدیریت اشتراک ‌ها")
                                {
                                    var keyboard = Keyboards.GetServiceLinksKeyboard(UserAcc.Tel_UserID, tbLinksRepository);
                                    if (keyboard == null)
                                    {
                                        StringBuilder str2 = new StringBuilder();
                                        str2.AppendLine("");
                                        str2.AppendLine("<b>" + "❌ عزیزم شما اشتراکی نداری" + "</b>");
                                        str2.AppendLine("");
                                        str2.AppendLine("♨️ برو تو بخش تعرفه ها یه نگاه بنداز ببین کدوم بسته به دردت می‌خوره، بعد کیف پولتو شارژ کن و برو برا خرید!\n");
                                        str2.AppendLine("");
                                        str2.AppendLine("🆔 @" + BotSettings.Bot_ID);

                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str2.ToString(), replyToMessageId: message.MessageId, parseMode: ParseMode.Html); return;
                                    }
                                    await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Select_AccountForShowInfo", db, botName);
                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("");
                                    str.AppendLine("<b>" + "💢 اشتراکتو انتخاب کن ببینیم مصرفت تو چه مایه‌س!\n" + "</b>");
                                    str.AppendLine("");
                                    str.AppendLine("اگه خواستی لینکت رو تغییر بدی، یا اسم اشتراکت رو عوض کنی، همه‌ش همین‌جاست – از همین بخش می‌تونی مدیریت کنی.\n");
                                    str.AppendLine("");
                                    str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                    var editedMessage = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, text: str.ToString(), replyToMessageId: message.MessageId, replyMarkup: keyboard, parseMode: ParseMode.Html);
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
                                        StringBuilder str3 = new StringBuilder();
                                        str3.AppendLine("<b>" + "❌ عزیزم شما اشتراکی نداری" + "</b>");
                                        str3.AppendLine("");
                                        str3.AppendLine("♨️ برو تو بخش تعرفه ها یه نگاه بنداز ببین کدوم بسته به دردت می‌خوره، بعد کیف پولتو شارژ کن و برو برا خرید!\r\n\r\n");
                                        str3.AppendLine("");
                                        str3.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str3.ToString(), parseMode: ParseMode.Html);
                                        return;
                                    }
                                    await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForSelectAccount", db, botName);

                                    StringBuilder str2 = new StringBuilder();
                                    str2.AppendLine("");
                                    str2.AppendLine("♨️  عزیزم اشتراکتو انتخاب کن تا بریم برای تمدیدش");
                                    str2.AppendLine("〰️〰️〰️〰️〰️");
                                    str2.AppendLine("🆔 @" + BotSettings.Bot_ID);

                                    var editedMessage = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str2.ToString(), replyMarkup: keyboard, replyToMessageId: message.MessageId, parseMode: ParseMode.Html);
                                    return;
                                    #endregion

                                }
                                #endregion

                                #endregion

                                #endregion

                                #region نمایش تعرفه ها

                                if (mess == "📊 تعرفه‌ها")
                                {

                                    var Plans = RepositoryLinkUserAndPlan.GetAll().Where(s => s.L_FK_U_ID == BotSettings.FK_User_ID && s.L_SellPrice != null && s.L_ShowInBot == true && s.L_Status == true).ToList();

                                    StringBuilder str = new StringBuilder();

                                    if (BotSettings.Present_Discount != null)
                                    {

                                        str.Append("<b>" + "🚦 بسته مناسب خودتو انتخاب کن!\r\n " + "</b>");
                                        str.AppendLine("");
                                        str.AppendLine("");
                                        str.AppendLine("با توجه به مصرف اینترنتت، ما تعرفه ‌هایی با حجم و زمان ‌های مختلف آماده کردیم. کافیه ببینی چقدر مصرف داری و همون تعرفه رو فعال کنی 💥\r\n\r\n"); str.AppendLine("");
                                        str.AppendLine("💥 با " + "%" + BotSettings.Present_Discount * 100 + " تخفیف ویژه 💥");
                                        str.AppendLine("");
                                        str.AppendLine("<b>" + "اشتراک های حجمی :" + "</b>");
                                        var Counter = 1;
                                        var ordered = Plans.OrderBy(s => s.tbPlans.PlanMonth).ThenBy(s => s.tbPlans.PlanVolume);
                                        foreach (var item in ordered)
                                        {
                                            str.AppendLine(Counter + " - " + item.tbPlans.PlanMonth + " ماهه " + item.tbPlans.PlanVolume + " گیگ" + " | " + "<s>" + item.L_SellPrice.Value.ConvertToMony() + "</s>" + " 👈 " + (item.L_SellPrice.Value - (item.L_SellPrice.Value * BotSettings.Present_Discount)).Value.ConvertToMony() + " تومان");

                                            Counter++;
                                        }

                                        str.AppendLine("");
                                        str.AppendLine("💢 اشتراک های حجمی فاقد محدودیت کاربر هستند");


                                    }
                                    else
                                    {
                                        str.Append("<b>" + "🚦 بسته مناسب خودتو انتخاب کن!\r\n " + "</b>");
                                        str.AppendLine("");
                                        str.AppendLine("");
                                        str.AppendLine("با توجه به مصرف اینترنتت، ما تعرفه ‌هایی با حجم و زمان ‌های مختلف آماده کردیم. کافیه ببینی چقدر مصرف داری و همون تعرفه رو فعال کنی 💥\r\n\r\n");
                                        var Counter = 1;
                                        str.AppendLine("<b>" + "اشتراک های حجمی :" + "</b>");
                                        var ordered = Plans.OrderBy(s => s.tbPlans.PlanMonth).ThenBy(s => s.tbPlans.PlanVolume);
                                        foreach (var item in ordered)
                                        {

                                            str.AppendLine(Counter + " - " + item.tbPlans.PlanMonth + " ماهه " + item.tbPlans.PlanVolume + " گیگ" + " 👈 " + item.L_SellPrice.Value.ConvertToMony() + " تومان");

                                            Counter++;
                                        }

                                        str.AppendLine("");
                                        str.AppendLine("💢 اشتراک های حجمی فاقد محدودیت کاربر هستند");

                                    }


                                    str.AppendLine("");

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
                                        str.AppendLine("📲 یکی از گزینه های زیر رو با توجه به گوشی یا دستگاهت انتخاب کن");
                                        str.AppendLine("");
                                        str.AppendLine("اگر لینک اشتراکتو داری میتونی توی مرورگر باز کنی اونجا هم آموزش دادیم");
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
                                    if (!BotSettings.IsNotActiveSell)
                                    {
                                        if (UserAcc != null)
                                        {
                                            StringBuilder str = new StringBuilder();
                                            str.AppendLine("");
                                            str.AppendLine("<b>" + "📌 موجودی کیف پولت : " + UserAcc.Tel_Wallet.Value.ConvertToMony() + " تومان" + "</b>");
                                            str.AppendLine("");

                                            var learns = BotSettings.tbUsers.tbConnectionHelp.Where(p => p.ch_Type == "crypto").ToList();
                                            foreach (var item in learns)
                                            {
                                                str.AppendLine(" <a href='" + item.ch_Link + "'>" + item.ch_Title + "</a>");
                                            }
                                            str.AppendLine("");
                                            str.AppendLine("✅ واسه شارژ کیف پول، یکی از روش‌های زیر رو انتخاب کن 😊");
                                            str.AppendLine("");
                                            str.AppendLine("👥 اگه دوست داری اعتبار رایگان بگیری، کافیه چندتا از رفقاتو از بخش " + "<b>زیرمجموعه‌گیری</b>" + " دعوت کنی. هم خودت می‌بری، هم اونا! 🎁");
                                            str.AppendLine("");
                                            str.AppendLine("➖➖➖➖➖➖➖➖➖");
                                            str.AppendLine("");
                                            str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                            List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();


                                            var ActivePayes = PaymentMethodUserRepo.Where(a => a.tbpu_Status && a.FK_User_ID == User.FK_User_ID).ToList();
                                            foreach (var payment in ActivePayes)
                                            {
                                                switch (payment.tbPaymentMethods.tbpm_Key)
                                                {
                                                    case "CardToCard":
                                                        List<InlineKeyboardButton> row1 = new List<InlineKeyboardButton>();
                                                        row1.Add(InlineKeyboardButton.WithCallbackData("💳 کارت به کارت", "InventoryIncreaseCard"));

                                                        inlineKeyboards.Add(row1);
                                                        break;
                                                    case "TetraPay":
                                                        List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();
                                                        row2.Add(InlineKeyboardButton.WithCallbackData("🏧 کارت به کارت ( واسط )", "InventoryIncreaseTetraPay"));
                                                        inlineKeyboards.Add(row2);
                                                        break;

                                                }
                                            }



                                            List<InlineKeyboardButton> row5 = new List<InlineKeyboardButton>();
                                            row5.Add(InlineKeyboardButton.WithCallbackData("زیر مجموعه گیری 👬", "InventoryIncreaseSub"));
                                            inlineKeyboards.Add(row5);

                                            var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboards);

                                            await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Select_Way_To_Increase", db, botName);

                                            await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, disableWebPagePreview: true, replyToMessageId: message.MessageId);

                                        }
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("💢 با عرض پوزش فروش ربات موقتا غیرفعال شده است .");
                                        str.AppendLine("");
                                        str.AppendLine("〰️〰️〰️〰️〰️");
                                        str.AppendLine("🚀@" + BotSettings.Bot_ID);
                                        //await SendTrafficCalculator(UserAcc, BotSettings, bot.Client, botName, messageId: callbackQuery.Message.MessageId);


                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);
                                        return;
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

                                    str.AppendLine("✅ عزیزم مشکلی داری میتونی به من بگی برای ارتباط با من به آیدی زیر پیام بده  ");
                                    str.AppendLine("");
                                    str.AppendLine("📱 @" + BotSettings.AdminUsername);
                                    str.AppendLine("");
                                    str.AppendLine("میتونی قبلی اینکه پیام بدی آموزش اتصال داخل ربات رو چک کنی اگر مشکلت رفع نشد خودم کنارتم بهم پیام بده ☺️😘");
                                    str.AppendLine("");
                                    str.AppendLine("🆔 @" + BotSettings.Bot_ID);

                                    await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);



                                }

                                #endregion

                                #region اشتراک تست

                                if (mess == "🎁 اشتراک تست")
                                {
                                    if (UserAcc.Tel_GetedTestAccount == false || UserAcc.Tel_GetedTestAccount == null)
                                    {

                                        MySqlEntities mySql = new MySqlEntities(BotSettings.tbUsers.tbServers.ConnectionString);
                                        await mySql.OpenAsync();
                                        var Disc1 = new Dictionary<string, object>();
                                        Disc1.Add("@v2board_id", V2boardPlanId);
                                        long tran = Utility.ConvertGBToByte(1);
                                        int grid = BotSettings.GroupId_test.Value;
                                        string create = DateTime.Now.ConvertDatetimeToSecond().ToString();
                                        string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                                        string exp = DateTime.Now.AddDays(1).ConvertDatetimeToSecond().ToString();
                                        var isExists = true;
                                        var FullName = "";
                                        while (isExists)
                                        {
                                            Random ran = new Random();
                                            FullName = BotSettings.Bot_ID + "$" + ran.Next(999) + "@" + BotSettings.tbUsers.Username;
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
                                        str.AppendLine("🌿 رفیق! اشتراک تستت با موفقیت ساخته شد! 🎉");
                                        str.AppendLine("");
                                        str.AppendLine("💢 شناسه اشتراک : " + FullName.Split('@')[0]);
                                        str.AppendLine("");
                                        str.AppendLine("🚦 حجم کل : 1 گیگابایت");
                                        str.AppendLine("⏳ مدت زمان : یک روز");
                                        str.AppendLine("");
                                        str.AppendLine("🔗 لینک اتصال: ");
                                        str.AppendLine("👇👇👇👇👇👇👇");
                                        str.AppendLine("");
                                        var SubLink = "https://" + Server.SubAddress + "/api/v1/client/subscribe?token=" + token;
                                        str.AppendLine("<code>" + SubLink + "</code>");
                                        str.AppendLine("");
                                        str.AppendLine("📘 اگه اولین باره وصل می‌شی یا مشکلی داشتی، حتما یه سر به بخش \"آموزش اتصال\" بزن، همه چی رو کامل برات توضیح دادیم 😉");
                                        str.AppendLine("");
                                        str.AppendLine("البته اگر لینکت رو هم باز کنی اونجا هم توضیح دادیم 😁");
                                        await mySql.CloseAsync();

                                        str.AppendLine("");
                                        str.AppendLine("🚀 @" + BotSettings.Bot_ID);

                                        var tbTelegram = tbTelegramUserRepository.Where(s => s.Tel_UniqUserID == User.Tel_UniqUserID && s.tbUsers.Username == botName).FirstOrDefault();
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
                                        str.AppendLine("❌ عزیزم شما یکبار فقط می توانید اشتراک تست دریافت کنید");
                                        str.AppendLine("");
                                        str.AppendLine("🚀 @" + BotSettings.Bot_ID);
                                        await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, str.ToString());
                                    }
                                }
                                #endregion

                                #region آموزش خرید

                                if (mess == "📲 آموزش خرید")
                                {
                                    var learn = BotSettings.tbUsers.tbConnectionHelp.Where(s => s.ch_Type == "buy_sub").FirstOrDefault();
                                    if (learn != null)
                                    {
                                        await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, learn.ch_Link, disableWebPagePreview: false);
                                    }
                                    else
                                    {
                                        var keys = Keyboards.GetHomeButton();
                                        await bot.Client.SendTextMessageAsync(chatid, "بازگشت به منو اصلی", parseMode: ParseMode.Html, replyMarkup: keys);
                                    }
                                }

                                #endregion

                                #region بخش منتظر بودن برای کاربر که مبلغ واریزی جهت افزایش کیف پول را وارد کند

                                #region شارژ کارت به کارت

                                if (UserAcc.Tel_Step == "Wait_For_Type_IncreasePrice")
                                {
                                    var price = 0;

                                    int.TryParse(mess, out price);
                                    if (price >= 50000 && price <= 5000000)
                                    {
                                        Random ran = new Random();
                                        var RanNumber = ran.Next(1, 999);

                                        var fullPrice = (price * 10);
                                        if (BotSettings.IsActiveCardToCard == true)
                                        {
                                            fullPrice += RanNumber;
                                        }
                                        StringBuilder str = new StringBuilder();

                                        var TaxId = Guid.NewGuid().ToString().Split('-')[0] + "#" + User.Tel_UserID;

                                        var FirstCard = BotSettings.tbUsers.tbBankCardNumbers.Where(p => p.Active == true).FirstOrDefault();
                                        var PaymentCard = BotSettings.tbUsers.tbPaymentMethodUser.Where(a => a.tbPaymentMethods.tbpm_Key == "CardToCard" && a.tbpu_Status).FirstOrDefault();
                                        str.AppendLine("✅  تراکنش شما باموفقیت ثبت شد ");
                                        str.AppendLine();
                                        str.AppendLine("کد پیگیری : " + "<code>" + TaxId + "</code>");
                                        str.AppendLine();
                                        str.AppendLine("💳 لطفاً مبلغ " + "<code>" + fullPrice.ConvertToMony() + "</code>" + " ریال رو به شماره کارت زیر واریز کن :");
                                        str.AppendLine("");
                                        str.AppendLine(FirstCard.CardNumber);
                                        str.AppendLine("به نام : " + FirstCard.InTheNameOf);
                                        str.AppendLine("");
                                        str.AppendLine("🔹 روی مبلغ کلیک کن تا خودش کپی بشه — لازم نیست حفظش کنی 😌");
                                        if ((bool)BotSettings.IsActiveSendReceipt && (bool)BotSettings.IsActiveCardToCard)
                                        {
                                            str.AppendLine("🔹 حتماً مبلغ رو دقیقاً با سه رقم آخر واریز کن. اگه مبلغ رو دقیق نزنی، ربات نمی‌تونه تراکنشت رو تشخیص بده ❗️");
                                            str.AppendLine("");
                                            str.AppendLine("📸 اگه به هر دلیلی پرداختت به‌صورت خودکار تأیید نشد، کافیه رسید واریزی رو به‌صورت عکس (نه فایل) برای ربات بفرستی.");
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
                                        if (BotSettings.IsActiveCardToCard == true)
                                        {
                                            str.AppendLine("⚠️ نکته مهم:\r\n");
                                            str.AppendLine("<b>" + "هر تراکنش فقط ۲۴ ساعت اعتبار داره. اگه پیام \"منقضی شدن فاکتور\" برات اومد، دیگه هیچ مبلغی واریز نکن ❌ " + "</b>");
                                            str.AppendLine("");
                                            str.AppendLine("<b>" + "🔺 حواست باشه! اگه مبلغ اشتباه واریز بشه، امکان برگشت وجه وجود نداره 🙏" + "</b>");
                                        }

                                        tbDepositWallet_Log tbDeposit = new tbDepositWallet_Log();
                                        tbDeposit.dw_Price = fullPrice;
                                        tbDeposit.dw_CreateDatetime = DateTime.Now;
                                        tbDeposit.dw_Status = "FOR_PAY";
                                        tbDeposit.FK_TelegramUser_ID = UserAcc.Tel_UserID;
                                        tbDeposit.dw_message_id = update.Message.MessageId;
                                        tbDeposit.dw_PayMethod = "Card";
                                        tbDeposit.FK_PayMethod_ID = PaymentCard.tbpu_ID;
                                        tbDepositLogRepo.Insert(tbDeposit);
                                        await tbDepositLogRepo.SaveChangesAsync();
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Pay_IncreasePrice", db, botName);
                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);



                                        return;
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("❌ فرمت مبلغ اشتباه");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ نکته : بازه مبلغ واریزی بین 50,000 تومان تا 5,000,000 تومان می باشد");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ مبلغ را بدون گذاشتن , وارد کنید");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ مبلغ را با اعداد انگلیسی وارد کنید");
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreasePrice", db, botName);

                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                        return;
                                    }
                                }

                                #endregion

                                #region شارژ تتراپی


                                if (UserAcc.Tel_Step == "Wait_For_Type_IncreaseTetraPay")
                                {

                                    var message1 = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, "در حال ساخت تراکنش ...", parseMode: ParseMode.Html, replyToMessageId: message.MessageId);

                                    var price = 0;

                                    int.TryParse(mess, out price);
                                    if (price >= 50000 && price <= 5000000)
                                    {
                                        var TetraApi = User.tbUsers.tbPaymentMethodUser.Where(a => a.tbPaymentMethods.tbpm_Key == "TetraPay").FirstOrDefault();

                                        var setting = SettingRepo.Where(a => a.tbKey == "TetraPay_Addr").FirstOrDefault();
                                        if (setting == null)
                                        {
                                            logger.Error("عدم ثبت آدرس درگاه پرداخت تتراپی");
                                            throw new Exception();
                                        }

                                        var TaxId = Guid.NewGuid().ToString().Split('-')[0]+"#"+User.Tel_UserID;

                                        TetraPay tetraPay = new TetraPay(setting.tbValue);

                                        RequestCreateOrderModel model = new RequestCreateOrderModel();
                                        model.ApiKey = TetraApi.tbpu_ApiKey;
                                        model.Amount = price * 10;
                                        model.Hash_id = TaxId;
                                        model.CallbackURL = "https://" + Server.SubAddress + "/User/VerifyTetraPay";

                                        var TetraRes = await tetraPay.CreateOrder(model);
                                        if (TetraRes.Status == "100")
                                        {
                                            StringBuilder str = new StringBuilder();


                                            str.AppendLine("✅  تراکنش شما باموفقیت ثبت شد ");
                                            str.AppendLine();
                                            str.AppendLine("کد پیگیری : "+"<code>"+ TaxId + "</code>");
                                            str.AppendLine();
                                            str.AppendLine("👇 قبل از اینکه پول رو واریز کنی، اینا رو حتماً حواست باشه :");
                                            str.AppendLine();
                                            str.AppendLine("💰 مبلغ رو دقیق و بدون تغییر واریز کن؛ حتی اختلاف کم هم مشکل ایجاد می‌کنه.");
                                            str.AppendLine("🚫 لطفاً مبلغ رو خرد خرد نفرست؛ یک‌جا کامل واریز کن.");
                                            str.AppendLine("🔄 فقط کارت‌به‌کارت انجام بده؛ پل یا واریز شِبا تأیید نمی‌شه.");
                                            str.AppendLine("⏳ هر تراکنش فقط 1 ساعت مهلت پرداخت داره");
                                            str.AppendLine();
                                            str.AppendLine("❗ یه نکته که خیلیا اشتباه می‌کنن :");
                                            str.AppendLine("تو مبلغ رو به تومان وارد می‌کنی، ولی عددی که اعلام میشه ریاله. پس دقیقاً همون عدد ریالی رو واریز کن.");
                                            str.AppendLine();
                                            str.AppendLine("⚠️ حواست باشه موارد که گفتم رو رعایت کنی در غیراینصورت عواقب به عهده کاربر خواهد بود");
                                            str.AppendLine();
                                            str.AppendLine("");
                                            str.AppendLine("🆔 @" + BotSettings.Bot_ID);

                                            tbDepositWallet_Log tbDeposit = new tbDepositWallet_Log();
                                            tbDeposit.dw_Price = price * 10;
                                            tbDeposit.dw_CreateDatetime = DateTime.Now;
                                            tbDeposit.dw_Status = "FOR_PAY";
                                            tbDeposit.dw_TaxId = TaxId;
                                            tbDeposit.FK_TelegramUser_ID = UserAcc.Tel_UserID;
                                            tbDeposit.dw_message_id = update.Message.MessageId;
                                            tbDeposit.dw_PayMethod = "Card";
                                            tbDeposit.dw_Authority = TetraRes.Authority;
                                            tbDeposit.FK_PayMethod_ID = TetraApi.tbPaymentMethods.tbpm_ID;

                                            tbDepositLogRepo.Insert(tbDeposit);
                                            await tbDepositLogRepo.SaveChangesAsync();

                                            await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreaseTetraPay", db, botName);

                                            var Keyboard = Keyboards.GetPaymentButtonForIncreaseWalletTetraPay(TetraRes.payment_url_bot);
                                            var reply_message = await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId,replyMarkup: Keyboard);

                                            await bot.Client.DeleteMessageAsync(UserAcc.Tel_UniqUserID, message1.MessageId);

                                            StringBuilder stringBuilder = new StringBuilder();
                                            stringBuilder.AppendLine();
                                            stringBuilder.AppendLine("🏧 تائید تراکنشت بین 15 تا 30 دقیقه طول میکشه بعد از پرداخت مبلغ به صورت خودکار به کیف پولت واریز میشه ");
                                            stringBuilder.AppendLine();
                                            stringBuilder.AppendLine("📞 در صورت هرگونه مشکل میتوانید به پشتیبانی پیام بدید");
                                            stringBuilder.AppendLine();
                                            stringBuilder.AppendLine("🆔 @" + BotSettings.AdminUsername);
                                            var keys = Keyboards.GetHomeButton();
                                            await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, stringBuilder.ToString(), parseMode: ParseMode.Html, replyToMessageId: reply_message.MessageId, replyMarkup: keys);
                                            return;
                                        }
                                        else
                                        {

                                            logger.Warn("ساخت تراکنش با خطا مواجه شد", model);

                                            StringBuilder str = new StringBuilder();
                                            str.Append("❌ ساخت تراکنش با خطا مواجه شد . با پشتیبانی ارتباط بگیرید");
                                            await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                            return;
                                        }





                                        return;
                                    }
                                    else
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("❌ فرمت مبلغ اشتباه");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ نکته : بازه مبلغ واریزی بین 50,000 تومان تا 5,000,000 تومان می باشد");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ مبلغ را بدون گذاشتن , وارد کنید");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ مبلغ را با اعداد انگلیسی وارد کنید");
                                        str.AppendLine("");
                                        str.AppendLine("🆔 @" + BotSettings.Bot_ID);
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreaseTetraPay", db, botName);

                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                        return;
                                    }
                                }


                                #endregion

                                #endregion

                                #region بخش چک کردن برای اینکه کاربر نام اکانت جدید خودش را وارد کند

                                if (UserAcc.Tel_Step == "WaitForName")
                                {
                                    if (mess.Length <= 20 && mess.Length >= 4)
                                    {
                                        if (Utility.IsEnglishText(mess))
                                        {
                                            if (mess.Contains('@') || mess.Contains('$'))
                                            {

                                                StringBuilder str1 = new StringBuilder();
                                                str1.AppendLine("❌ نام اشتراک نمی تواند حاوی کاراکتر @ یا $ باشد");
                                                await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html);
                                                return;
                                            }

                                            var OldName = UserAcc.Tel_Data;
                                            var LastName = UserAcc.Tel_Data.Split('$')[1];
                                            var NewName = mess;


                                            var Link = tbLinksRepository.Where(s => s.tbL_Email == OldName).First();

                                            Link.tbL_Email = NewName + "$" + LastName;

                                            using (MySqlEntities mySql = new MySqlEntities(UserAcc.tbUsers.tbServers.ConnectionString))
                                            {
                                                await mySql.OpenAsync();
                                                var reader = await mySql.GetDataAsync("update v2_user set email = '" + Link.tbL_Email + "'" + " where email ='" + OldName + "'");
                                                await reader.ReadAsync();
                                                reader.Close();
                                                await mySql.CloseAsync();
                                            }


                                            foreach (var item in User.tbOrders)
                                            {
                                                item.AccountName = Link.tbL_Email;
                                            }

                                            await tbLinksRepository.SaveChangesAsync();

                                            StringBuilder str = new StringBuilder();
                                            str.AppendLine("✅ نام اشتراک شما با موفقیت تغییر یافت");
                                            str.AppendLine("");
                                            str.AppendLine("❗️ برای نمایش جزئیات مجدد وارد بخش مدیریت اشتراک ها بشید");
                                            await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);
                                            await RealUser.SetEmptyState(UserAcc.Tel_UniqUserID, db, botName);
                                            return;
                                        }
                                        else
                                        {
                                            StringBuilder str1 = new StringBuilder();
                                            str1.AppendLine("❗️نام اشتراک شما باید انگلیسی باشد");
                                            str1.AppendLine("");
                                            str1.AppendLine("📝 لطفا نام جدید مورد نظر خود مجدد را وارد نمائید");

                                            await bot.Client.SendTextMessageAsync(message.From.Id, str1.ToString(), replyToMessageId: message.MessageId);
                                        }
                                    }
                                    else
                                    {
                                        StringBuilder str1 = new StringBuilder();
                                        str1.AppendLine("⚠️ نام اشتراک باید حداقل 4 حرف و حداکثر 20 حرف باشد");
                                        str1.AppendLine("");
                                        str1.AppendLine("📝 لطفا نام جدید مورد نظر خود مجدد را وارد نمائید");

                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForName", db, botName);
                                        await bot.Client.SendTextMessageAsync(message.From.Id, str1.ToString(), replyToMessageId: message.MessageId);
                                    }
                                }

                                #endregion
                            }

                            #region سوالات متداول

                            if (mess == "❓ سؤالات رایج")
                            {
                                StringBuilder str = new StringBuilder();
                                str.AppendLine("<b>" + "❓ سؤالات رایج در خصوص سرویس ها ❓" + "</b>");
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

                                            var Deposit = await tbDepositLogRepo.WhereAsync(p => p.tbTelegramUsers.Tel_UniqUserID == id && p.dw_Status == "FOR_PAY");

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
                                                TransactionHanderService service = new TransactionHanderService();
                                                var res = await service.CheckOrder(Deposit.dw_Price.ToString(), Deposit.tbTelegramUsers.tbUsers.PhoneNumber);
                                                if (res)
                                                {
                                                    await bot.Client.SendTextMessageAsync(BotSettings.AdminBot_ID, "✅ تراکنش با موفقیت تایید شد");
                                                }
                                                else
                                                {
                                                    await bot.Client.SendTextMessageAsync(BotSettings.AdminBot_ID, "❌ تائید تراکنش با خطا مواجه شد");
                                                }
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
                                    if (BotSettings.ChannelID != null)
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
                            }


                            #endregion

                            var MessageTime = update.CallbackQuery.Message.Date;
                            var ThisTime = DateTime.UtcNow.AddMinutes(-30);

                            var isAdmin = false;
                            if (chatid == BotSettings.AdminBot_ID)
                            {
                                isAdmin = true;
                            }

                            if (ThisTime < MessageTime || isAdmin)
                            {
                                await RealUser.SetUpdateMessageTime(User.Tel_UniqUserID, db, DateTime.UtcNow, botName);

                                #region چک کردن وضعیت پرداخت آرانکس

                                var btnpay = update.CallbackQuery.Data.Split('%');
                                if (btnpay.Length == 2)
                                {
                                    if (btnpay[0] == "Payed")
                                    {
                                        var paymentId = btnpay[1];

                                        HttpClient httpClient = new HttpClient();
                                        httpClient.BaseAddress = new Uri("https://api.aranex.net");
                                        var result = await httpClient.GetAsync("/payment?id=" + paymentId);
                                        if (result.IsSuccessStatusCode)
                                        {
                                            var model = JObject.Parse(result.Content.ReadAsStringAsync().Result);
                                            var state = model["result"].ToString();
                                            if (state == "True")
                                            {
                                                var date2 = DateTime.Now.AddHours(-24);
                                                var tbDepositLog = await tbDepositLogRepo.FirstOrDefaultAsync(p => p.dw_TaxId == paymentId && p.dw_Status == "FOR_PAY");
                                                if (tbDepositLog != null)
                                                {
                                                    tbDepositLog.dw_Status = "FINISH";
                                                    tbDepositLog.tbTelegramUsers.Tel_Wallet += tbDepositLog.dw_Price / 10;
                                                    StringBuilder str = new StringBuilder();
                                                    str.AppendLine("✅ کیف پولتو شارژ کردم");
                                                    str.AppendLine("");
                                                    str.AppendLine("💰 موجودی الانت : " + tbDepositLog.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                                    str.AppendLine("");
                                                    str.AppendLine("🔔 خب حالا برو اشتراکتو تمدید کن یا اشتراک جدید بخر و حالشو ببر.");
                                                    str.AppendLine("");
                                                    str.AppendLine("توجه کن اگر اشتراک داری برو تو بخش تمدید و تمدید کن وگرنه اشتراکت تموم میشه و قطع میشی");


                                                    var keyboard = Keyboards.GetHomeButton();

                                                    await RealUser.SetUserStep(tbDepositLog.tbTelegramUsers.Tel_UniqUserID, "Start", db, tbDepositLog.tbTelegramUsers.tbUsers.Username);




                                                    TelegramBotClient botClient = new TelegramBotClient(BotSettings.Bot_Token);
                                                    await tbDepositLogRepo.SaveChangesAsync();
                                                    await botClient.SendTextMessageAsync(tbDepositLog.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);

                                                    if (BotSettings.InvitePercent != null)
                                                    {
                                                        if (tbDepositLog.tbTelegramUsers.Tel_Parent_ID != null)
                                                        {
                                                            var parent = tbDepositLog.tbTelegramUsers.tbTelegramUsers2;
                                                            parent.Tel_Wallet += Convert.ToInt32((tbDepositLog.dw_Price / 10) * BotSettings.InvitePercent.Value);

                                                            StringBuilder str1 = new StringBuilder();
                                                            str1.AppendLine("☺️ کاربر گرامی، به دلیل خرید دوستتان، ‌" + BotSettings.InvitePercent * 100 + " درصد از مبلغ خرید ایشان به کیف پول شما اضافه شد. از حمایت شما سپاسگزاریم 🙏🏻");
                                                            str1.AppendLine("");
                                                            str1.AppendLine("💰 موجودی فعلی کیف پول شما: " + parent.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                                            str1.AppendLine("");
                                                            str1.AppendLine("🚀 @" + BotSettings.Bot_ID);

                                                            await botClient.SendTextMessageAsync(parent.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html);
                                                        }
                                                    }

                                                    await tbDepositLogRepo.SaveChangesAsync();


                                                    logger.Info("فاکتور به مبلغ " + tbDepositLog.dw_Price + " با موفقیت پرداخت شد");
                                                    return;
                                                }
                                                else
                                                {
                                                    StringBuilder str = new StringBuilder();
                                                    str.AppendLine("❌ تراکنشت تو سیستم ثبت نشده یا قبلا تائید شده");

                                                    await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, str.ToString(), true);
                                                    return;
                                                }

                                            }
                                            else
                                            {
                                                StringBuilder str = new StringBuilder();
                                                str.AppendLine("❌ تراکنشت هنوز تو سیستم ثبت نشده چند لحظه بعد مجدد تلاش کن");

                                                await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, str.ToString(), true);
                                                return;
                                            }
                                        }

                                    }
                                }

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
                                    if (User.Tel_Step == "WaitForSelectPlan")
                                    {
                                        var type = BotMessages.SendSelectSubType(BotSettings);
                                        await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                        await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "WaitForSelectAccount", db, botName);
                                        return;
                                    }
                                    if (User.Tel_Step == "WaitForPay")
                                    {
                                        var type = BotMessages.SendSelectSubType(BotSettings);
                                        await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                        await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "WaitForSelectPlan", db, botName);
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
                                            row2.Add(InlineKeyboardButton.WithCallbackData("تغییرنام اشتراک ✏️", "Rename%" + Link.tbL_Email));
                                            inlineKeyboards.Add(row2);


                                            InlineKeyboardButton inlineKeyboard = new InlineKeyboardButton("🔗 اتصال به اشتراک");
                                            WebAppInfo appInfo = new WebAppInfo();
                                            appInfo.Url = SubLink;
                                            inlineKeyboard.WebApp = appInfo;
                                            row2.Add(inlineKeyboard);


                                            List<InlineKeyboardButton> row3 = new List<InlineKeyboardButton>();
                                            row3.Add(InlineKeyboardButton.WithCallbackData("حذف اشتراک 🗑", "DeleteAcc%" + Link.tbL_Email));
                                            inlineKeyboards.Add(row3);

                                            List<InlineKeyboardButton> row4 = new List<InlineKeyboardButton>();
                                            row4.Add(InlineKeyboardButton.WithCallbackData("دریافت کانفیگ ها 🔗", "GetConfig%" + Link.tbL_Token));
                                            inlineKeyboards.Add(row4);

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

                                #region نمایش کانفیگ
                                if (callback.Length == 2)
                                {
                                    if (callback[0] == "GetConfig")
                                    {
                                        HttpClient client = new HttpClient();
                                        client.BaseAddress = new Uri(Server.ServerAddress + "/api/v1/");
                                        client.DefaultRequestHeaders.UserAgent.TryParseAdd("Happ/3.8.1");
                                        var res = client.GetAsync(client.BaseAddress + "client/subscribe?token=" + callback[1]);
                                        if (res.Result.StatusCode == System.Net.HttpStatusCode.OK)
                                        {
                                            var result = await res.Result.Content.ReadAsStringAsync();
                                            var Base64 = Utility.Base64Decode(result);
                                            var lines = Base64
    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
    .Select(x => x.Trim())
    .Where(x => x.Length > 0)
    .ToArray();

                                            await bot.Client.SendTextMessageAsync(
                                                callbackQuery.From.Id,
                                                "🔷 ————- لیست سرور ها ————- 🔷",
                                                parseMode: ParseMode.Html
                                            );

                                            var normal = lines.Where(x => !x.Contains("EM")).ToList(); // بهتره شرط دقیق‌تر باشه
                                            const int chunkSize = 5;

                                            for (int offset = 0; offset < normal.Count; offset += chunkSize)
                                            {
                                                var chunk = normal.Skip(offset).Take(chunkSize);
                                                var finalText = string.Join("\n", chunk);

                                                var html = $"<pre><code>{WebUtility.HtmlEncode(finalText)}</code></pre>";

                                                await bot.Client.SendTextMessageAsync(
                                                    callbackQuery.From.Id,
                                                    html,
                                                    parseMode: ParseMode.Html
                                                );
                                            }

                                            var emList = lines.Where(x => x.Contains("EM")).ToList(); // بهتره شرط دقیق‌تر باشه
                                            if (emList.Count > 0)
                                            {
                                                await bot.Client.SendTextMessageAsync(
                                                    callbackQuery.From.Id,
                                                    "🔻 ————- این کانفیگ ها در شرایط اضطراری و درصورت برقراری نبودن سایر سرور ها استفاده شود ————- 🔻",
                                                    parseMode: ParseMode.Html
                                                );

                                                foreach (var item in emList)
                                                {
                                                    await bot.Client.SendTextMessageAsync(
                                                        callbackQuery.From.Id,
                                                        $"<pre><code>{WebUtility.HtmlEncode(item)}</code></pre>",
                                                        parseMode: ParseMode.Html
                                                    );
                                                }
                                            }

                                            await bot.Client.SendTextMessageAsync(
                                                callbackQuery.From.Id,
                                                "👆👆 لطفا تمامی سرور ها رو تک به تک به گوشیتون اضافه کنید 💢",
                                                parseMode: ParseMode.Html
                                            );
                                        }
                                    }
                                }

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

                                #region تغییر نام اشتراک

                                if (callback.Length == 2)
                                {
                                    if (callback[0] == "Rename")
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("♨️ تغییر نام اشتراک : " + callback[1].Split('@')[0].Split('$')[0]);
                                        str.AppendLine("");
                                        str.AppendLine("📝 لطفا نام جدید مورد نظر خود را وارد نمائید");
                                        str.AppendLine("");
                                        str.AppendLine("⚠️ نکته : نام اشتراک باید انگلیسی باشد");
                                        str.AppendLine("⚠️ نام اشتراک باید حداقل 4 حرف و حداکثر 20 حرف باشد");

                                        await bot.Client.DeleteMessageAsync(UserAcc.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                        await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, text: str.ToString(), parseMode: ParseMode.Html);
                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "WaitForName", db, bot.Name, callback[1]);
                                        return;
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

                                #region کارت به کارت

                                if (callbackQuery.Data == "InventoryIncreaseCard")
                                {
                                    if (BotSettings.IsActiveCardToCard == true || BotSettings.IsActiveSendReceipt == true)
                                    {
                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("◀️  لطفا مبلغ تعرفه یا هر مبلغی رو که میخای وارد کن ");
                                        str.AppendLine("");
                                        str.AppendLine("❗️ توجه کن : بازه مبلغ واریزیت باید بین 50,000 تومان تا 1,000,000 تومان باشه");
                                        str.AppendLine("❗️ مبلغو بدون گذاشتن , وارد کن");

                                        await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreasePrice", db, botName);

                                        List<List<KeyboardButton>> inlineKeyboards = new List<List<KeyboardButton>>();
                                        List<KeyboardButton> row2 = new List<KeyboardButton>();
                                        row2.Add(new KeyboardButton("⬅️ برگشت به صفحه اصلی"));
                                        inlineKeyboards.Add(row2);

                                        inlineKeyboardMarkup = Keyboards.BasicKeyboard(inlineKeyboards);
                                        await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                        await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, str.ToString(), parseMode: ParseMode.Html, replyMarkup: inlineKeyboardMarkup);
                                    }
                                    else
                                    {
                                        await bot.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "😔❤️ اوه عزیزم خیلی ببخشید این روش فعلا در دسترس نیست لطفا از روش دیگه استفاده کن ", showAlert: true);
                                    }

                                }

                                #endregion

                                #region تتراپی

                                if (callbackQuery.Data == "InventoryIncreaseTetraPay")
                                {

                                    StringBuilder str = new StringBuilder();
                                    str.AppendLine("◀️  لطفا مبلغ تعرفه یا هر مبلغی رو که میخای وارد کن ");
                                    str.AppendLine("");
                                    str.AppendLine("❗️ توجه کن : بازه مبلغ واریزیت باید بین 50,000 تومان تا 1,000,000 تومان باشه");
                                    str.AppendLine("❗️ مبلغو بدون گذاشتن , وارد کن");

                                    await RealUser.SetUserStep(UserAcc.Tel_UniqUserID, "Wait_For_Type_IncreaseTetraPay", db, botName);

                                    List<List<KeyboardButton>> inlineKeyboards = new List<List<KeyboardButton>>();
                                    List<KeyboardButton> row2 = new List<KeyboardButton>();
                                    row2.Add(new KeyboardButton("⬅️ برگشت به صفحه اصلی"));
                                    inlineKeyboards.Add(row2);

                                    inlineKeyboardMarkup = Keyboards.BasicKeyboard(inlineKeyboards);
                                    await bot.Client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);
                                    await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, str.ToString(), parseMode: ParseMode.Html, replyMarkup: inlineKeyboardMarkup);
                                }

                                #endregion

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


                                #endregion

                                #region ساخت فاکتور پرداخت برای کاربر


                                if (callback.Length == 3)
                                {
                                    if (callback[0] == "NextLevel")
                                    {
                                        await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "WaitForPay", db, botName);

                                        var LinkPlanId = Convert.ToInt32(callback[1]);
                                        var AccName = callback[2].Split('@')[0].Split('$')[0];
                                        var Plan = await RepositoryLinkUserAndPlan.FirstOrDefaultAsync(s => s.Link_PU_ID == LinkPlanId);

                                        StringBuilder str = new StringBuilder();
                                        str.AppendLine("📌 اشتراکی که انتخاب کردی اینه 👇");
                                        str.AppendLine();
                                        str.AppendLine("نام اشتراک : " + AccName);
                                        str.AppendLine();
                                        str.AppendLine("♾ ترافیک : " + Plan.tbPlans.PlanVolume + " گیگ");
                                        str.AppendLine("⏳ مدت زمان : " + Plan.tbPlans.PlanMonth + " ماه");
                                        str.AppendLine("");
                                        str.AppendLine("💵 موجودی کیف پولت : " + User.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                        if (BotSettings.Present_Discount != null)
                                        {
                                            str.AppendLine("💵 قیمت نهایی ( با تخفیف ) : " + (Plan.L_SellPrice.Value - (Plan.L_SellPrice.Value * BotSettings.Present_Discount.Value)).ConvertToMony() + " تومان");
                                        }
                                        else
                                        {
                                            str.AppendLine("💵 قیمت نهایی : " + (Plan.L_SellPrice.Value).ConvertToMony() + " تومان");
                                        }
                                        str.AppendLine("");
                                        str.AppendLine("🔗 با خرید این اشتراک، راحت می‌تونی با همه نوع اینترنت وصل شی و خیالت راحت باشه چون هیچ محدودیتی برای تعداد اتصال کاربر هم نداری 😉");
                                        str.AppendLine("");
                                        str.AppendLine("⭐️ اگه خواستی اشتراکت رو عوض کنی، می‌تونی برگردی به مراحل قبل.\r\nیا اگه همه چی اوکیه، همین‌جا خریدتو تایید کن و بزن بریم! 🚀");
                                        str.AppendLine("");
                                        str.AppendLine("");

                                        var keys = Keyboards.GetAccpetBuyFromWallet(LinkPlanId, callback[2]);

                                        await bot.Client.EditMessageTextAsync(User.Tel_UniqUserID, update.CallbackQuery.Message.MessageId, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keys);
                                        return;
                                    }
                                }




                                #endregion

                                #region پرداخت از کیف پول


                                if (callback.Length == 3)
                                {
                                    if (callback[0] == "AccpetWallet")
                                    {

                                        var LinkPlanId = System.Convert.ToInt32(callback[1]);
                                        var AccName = callback[2];
                                        var Plan = await RepositoryLinkUserAndPlan.FirstOrDefaultAsync(s => s.Link_PU_ID == LinkPlanId);


                                        var UserAgent = UserAcc.tbUsers;

                                        if (UserAgent.Role == 3)
                                        {
                                            var Prices = UserAgent.tbLinkServerGroupWithUsers.Where(s => s.FK_Group_Id == Plan.tbPlans.Group_Id).FirstOrDefault();

                                            var FinalPrice = (Plan.tbPlans.PlanMonth * Prices.PriceForMonth) + (Plan.tbPlans.PlanVolume * Prices.PriceForGig) + (Plan.tbPlans.device_limit * Prices.PriceForUser);

                                            var AgentWallet = UserAgent.Wallet + FinalPrice;

                                            if (AgentWallet > UserAgent.Limit)
                                            {
                                                await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ متاسفانه فعلا امکان ایجاد یا تمدید اشتراک نمی باشد لطفا با پشتیبانی ارتباط بگیرید", true);
                                                return;
                                            }
                                            else
                                            {
                                                UserAgent.Wallet += FinalPrice.Value;
                                            }
                                        }
                                        else if (UserAgent.Role == 2)
                                        {
                                            var AgentWallet = UserAgent.Wallet + Plan.L_SellPrice;
                                            if (AgentWallet > UserAgent.Limit)
                                            {
                                                await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, "⚠️ متاسفانه فعلا امکان ایجاد یا تمدید اشتراک نمی باشد لطفا با پشتیبانی ارتباط بگیرید", true);
                                                return;
                                            }
                                            else
                                            {
                                                UserAgent.Wallet += Plan.tbPlans.Price;
                                            }
                                        }

                                        var AccountName = "";
                                        if (User.Tel_Data != null)
                                        {
                                            AccountName = AccName;
                                        }
                                        var Wallet = UserAcc.Tel_Wallet;
                                        var Price = Plan.L_SellPrice.Value;
                                        if (BotSettings.Present_Discount != null && BotSettings.Present_Discount != 0)
                                        {
                                            Price -= (int)(Price * BotSettings.Present_Discount);
                                        }
                                        int PirceWithoutDiscount = Plan.L_SellPrice.Value;
                                        if (Wallet >= Price)
                                        {
                                            var Link = await tbLinksRepository.FirstOrDefaultAsync(p => p.tbL_Email == AccountName);
                                            if (Link != null)
                                            {
                                                MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);

                                                await mySql.OpenAsync();

                                                var Disc1 = new Dictionary<string, object>();
                                                Disc1.Add("@tbL_Email", Link.tbL_Email);


                                                var Reader = await mySql.GetDataAsync("select * from v2_user where email = @tbL_Email", Disc1);
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
                                                    order.Traffic = Plan.tbPlans.PlanVolume;
                                                    order.Month = Plan.tbPlans.PlanMonth;
                                                    order.PriceWithOutDiscount = PirceWithoutDiscount;
                                                    order.V2_Plan_ID = Plan.tbPlans.Plan_ID_V2;
                                                    order.FK_Tel_UserID = UserAcc.Tel_UserID;
                                                    order.FK_Link_Plan_ID = Plan.Link_PU_ID;
                                                    order.Tel_RenewedDate = DateTime.Now;
                                                    var UserAc = tbTelegramUserRepository.Where(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName).FirstOrDefault();
                                                    UserAc.Tel_Wallet -= Price;
                                                    var t = Utility.ConvertGBToByte(Convert.ToInt64(order.Traffic));

                                                    string exp = DateTime.Now.AddDays((int)(order.Month * 30)).ConvertDatetimeToSecond().ToString();

                                                    Link.tbL_Warning = false;
                                                    var Disc3 = new Dictionary<string, object>();
                                                    Disc3.Add("@DefaultPlanIdInV2board", Plan.tbPlans.Plan_ID_V2);
                                                    Disc3.Add("@transfer_enable", t);
                                                    Disc3.Add("@exp", exp);
                                                    Disc3.Add("@email", Link.tbL_Email);
                                                    var group = tbServerGroupsRepo.Where(s => s.Group_Id == Plan.tbPlans.Group_Id).First();
                                                    Disc3.Add("@group_id", group.V2_Group_Id);
                                                    var DeviceLimit_Structur = "";
                                                    if (Plan.tbPlans.device_limit != null)
                                                    {
                                                        Disc3.Add("@device_limit", Plan.tbPlans.device_limit);

                                                    }

                                                    var Query = "update v2_user set u=0,d=0,t=0,plan_id=@DefaultPlanIdInV2board,transfer_enable=@transfer_enable,expired_at=@exp,device_limit=@device_limit,group_id=@group_id where email=@email";
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
                                                    str2.AppendLine("✅ بسته تو تمدید کردم");
                                                    str2.AppendLine("");
                                                    str2.AppendLine("♨️ میتونی توی بخش مدیریت اشتراک ها ببینی که تمدید شده");
                                                    await RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                                    var kyes = Keyboards.GetHomeButton();
                                                    await bot.Client.SendTextMessageAsync(callbackQuery.From.Id, str2.ToString(), parseMode: ParseMode.Html, replyMarkup: kyes);
                                                    await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);


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
                                                    order.Traffic = Plan.tbPlans.PlanVolume;
                                                    order.Month = Plan.tbPlans.PlanMonth;
                                                    order.PriceWithOutDiscount = PirceWithoutDiscount;
                                                    order.V2_Plan_ID = Plan.tbPlans.Plan_ID_V2;
                                                    order.FK_Tel_UserID = UserAcc.Tel_UserID;
                                                    order.FK_Link_Plan_ID = Plan.Link_PU_ID;
                                                    var UserAc = await tbTelegramUserRepository.FirstOrDefaultAsync(p => p.Tel_UserID == UserAcc.Tel_UserID && p.tbUsers.Username == botName);
                                                    UserAc.Tel_Wallet -= Price;
                                                    order.Order_Price = Price;


                                                    tbOrdersRepository.Insert(order);
                                                    await tbOrdersRepository.SaveChangesAsync();
                                                    await tbLinksRepository.SaveChangesAsync();

                                                    StringBuilder str = new StringBuilder();
                                                    str.AppendLine("✅ چون هنوز حجم یا زمان داشتی بسته تو رزرو کردم");
                                                    str.AppendLine("");
                                                    str.AppendLine("♨️ بعد از اینکه بسته فعلیت تموم بشه خودم جایگزین میکنم تو نگران نباش و به کارت برس");
                                                    str.AppendLine("");
                                                    await RealUser.SetEmptyState(User.Tel_UniqUserID, db, botName);
                                                    await bot.Client.DeleteMessageAsync(User.Tel_UniqUserID, callbackQuery.Message.MessageId);
                                                    var kyes = Keyboards.GetHomeButton();
                                                    await bot.Client.SendTextMessageAsync(User.Tel_UniqUserID, str.ToString(), replyMarkup: kyes, parseMode: ParseMode.Html);

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



                                                var s = Guid.NewGuid();
                                                if (string.IsNullOrEmpty(User.Tel_Username))
                                                {
                                                    AccountName = s.ToString().Split('-')[ran.Next(0, 3)];
                                                }
                                                else
                                                {
                                                    var acc = tbLinksRepository.Where(p => p.FK_TelegramUserID == UserAcc.Tel_UserID).Count();
                                                    acc++;

                                                    AccountName += User.Tel_Username + acc;
                                                }



                                                Order.AccountName = AccountName + "$" + s.ToString().Split('-')[ran.Next(0, 3)] + "@" + BotSettings.tbUsers.Username;
                                                bool IsExists = true;
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
                                                Order.Traffic = Plan.tbPlans.PlanVolume;
                                                Order.Month = Plan.tbPlans.PlanMonth;
                                                Order.V2_Plan_ID = Plan.tbPlans.Plan_ID_V2;
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
                                                Disc1.Add("@V2board", Plan.tbPlans.Plan_ID_V2);
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
                                                Disc3.Add("@V2boardId", Plan.tbPlans.Plan_ID_V2);
                                                Disc3.Add("@token", token);
                                                Disc3.Add("@passwrd", Guid.NewGuid());

                                                var DeviceLimit_Structur = "";
                                                var DeviceLimit_data = "";

                                                if (Plan.tbPlans.device_limit != null)
                                                {
                                                    DeviceLimit_Structur = ",device_limit";
                                                    Disc3.Add("@device_limit", Plan.tbPlans.device_limit);
                                                    DeviceLimit_data = ",@device_limit";
                                                }



                                                string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at" + DeviceLimit_Structur + ") VALUES (@FullName,@expired,@create,@guid,0,0,0,@tran,0,@grid,@V2boardId,@token,@passwrd,@create" + DeviceLimit_data + ")";
                                                reader.Close();

                                                reader = await mySql.GetDataAsync(Query, Disc3);
                                                reader.Close();

                                                StringBuilder st = new StringBuilder();
                                                st.AppendLine("📈 <strong>لینک اشتراکت  : </strong>");
                                                st.AppendLine("👇👇👇👇👇👇👇");
                                                st.AppendLine("");
                                                var SubLink = "https://" + Server.SubAddress + "/api/v1/client/subscribe?token=" + token;
                                                st.AppendLine("<code>" + SubLink + "</code>");
                                                st.AppendLine("");

                                                st.AppendLine("◀️ روی لینک کلیک کنی خودش کپی میشه و میتونی توی نرم افزار اضافه اش کنی");
                                                st.AppendLine("");
                                                st.AppendLine("◀️ جزئیات اشتراک رو میتونی داخل بخش مدیریت اشتراک ها ببینی");
                                                st.AppendLine("");
                                                st.AppendLine("◀️ در ضمن روی گزینه زیر کلیک کنی میتونی طبق راهنما به اشتراکت وصل بشی و لذتشو ببری 👇");
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
                                                InlineKeyboardButton inlineKeyboard = new InlineKeyboardButton("🔗 اتصال به اشتراک");
                                                WebAppInfo appInfo = new WebAppInfo();
                                                appInfo.Url = SubLink;
                                                inlineKeyboard.WebApp = appInfo;
                                                row2.Add(inlineKeyboard);
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

                                                await BotSettingRepository.SaveChangesAsync();
                                                return;

                                            }
                                        }
                                        else
                                        {
                                            StringBuilder str = new StringBuilder();
                                            str.AppendLine("❌ اوه عزیزم کیف پولت شارژ نداره ");
                                            str.AppendLine("");
                                            str.AppendLine("⚠️ برو تو بخش کیف پول من و از اونجا کیف پولتو شارژ کن بعد برگرد . من منتظرتم 😉");

                                            await bot.Client.AnswerCallbackQueryAsync(callbackQuery.Id, str.ToString(), true);
                                            return;
                                        }
                                    }
                                }



                                #endregion

                                #region بخش انتخاب اشتراک برای تمدید

                                if (User.Tel_Step == "WaitForSelectAccount")
                                {
                                    await RealUser.SetUserStep(User.Tel_UniqUserID, "WaitForSelectPlan", db, botName, callbackQuery.Data);
                                    //var type = BotMessages.SendSelectSubType(BotSettings);

                                    //await RealUser.SetUserStep(User.Tel_UniqUserID.ToString(), "SelectSubType", db, botName);


                                    var keyboard = Keyboards.GetPlansKeyboard(callbackQuery.Data, RepositoryLinkUserAndPlan);

                                    StringBuilder str = new StringBuilder();

                                    var ordered = RepositoryLinkUserAndPlan.Where(s => s.L_SellPrice != null && s.L_ShowInBot == true && s.L_FK_U_ID == BotSettings.FK_User_ID && s.L_Status == true).OrderBy(s => s.tbPlans.PlanMonth).ThenBy(s => s.tbPlans.PlanVolume);

                                    if (BotSettings.Present_Discount != null)
                                    {

                                        str.Append("<b>" + "🚦 بسته مناسب خودتو انتخاب کن!\r\n " + "</b>");
                                        str.AppendLine("");
                                        str.AppendLine("");
                                        str.AppendLine("با توجه به مصرف اینترنتت، ما تعرفه ‌هایی با حجم و زمان ‌های مختلف آماده کردیم. کافیه ببینی چقدر مصرف داری و همون تعرفه رو فعال کنی 💥\r\n\r\n"); str.AppendLine("");
                                        str.AppendLine("💥 با " + "%" + BotSettings.Present_Discount * 100 + " تخفیف ویژه 💥");
                                        str.AppendLine("");
                                        str.AppendLine("<b>" + "اشتراک های حجمی :" + "</b>");
                                        var Counter = 1;
                                        foreach (var item in ordered)
                                        {
                                            str.AppendLine(Counter + " - " + item.tbPlans.PlanMonth + " ماهه " + item.tbPlans.PlanVolume + " گیگ" + " | " + "<s>" + item.L_SellPrice.Value.ConvertToMony() + "</s>" + " 👈 " + (item.L_SellPrice.Value - (item.L_SellPrice.Value * BotSettings.Present_Discount)).Value.ConvertToMony() + " تومان");

                                            Counter++;
                                        }

                                        str.AppendLine("");
                                        str.AppendLine("💢 اشتراک های حجمی فاقد محدودیت کاربر هستند");

                                    }
                                    else
                                    {
                                        str.Append("<b>" + "🚦 بسته مناسب خودتو انتخاب کن!\r\n " + "</b>");
                                        str.AppendLine("");
                                        str.AppendLine("");
                                        str.AppendLine("با توجه به مصرف اینترنتت، ما تعرفه ‌هایی با حجم و زمان ‌های مختلف آماده کردیم. کافیه ببینی چقدر مصرف داری و همون تعرفه رو فعال کنی 💥\r\n\r\n");
                                        var Counter = 1;
                                        str.AppendLine("<b>" + "اشتراک های حجمی :" + "</b>");

                                        foreach (var item in ordered)
                                        {

                                            str.AppendLine(Counter + " - " + item.tbPlans.PlanMonth + " ماهه " + item.tbPlans.PlanVolume + " گیگ" + " 👈 " + item.L_SellPrice.Value.ConvertToMony() + " تومان");

                                            Counter++;
                                        }

                                        str.AppendLine("");
                                        str.AppendLine("💢 اشتراک های حجمی فاقد محدودیت کاربر هستند");
                                    }


                                    str.AppendLine("");

                                    str.AppendLine("");
                                    str.AppendLine("〰️〰️〰️〰️〰️");
                                    str.AppendLine("🚀@" + BotSettings.Bot_ID);

                                    //await SendTrafficCalculator(UserAcc, BotSettings, bot.Client, botName, messageId: callbackQuery.Message.MessageId);


                                    await bot.Client.SendTextMessageAsync(UserAcc.Tel_UniqUserID, str.ToString(), replyMarkup: keyboard, parseMode: ParseMode.Html);

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
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در برقراری ارتباط سرور تلگرام");
            }

            return;
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
                    var httpClient = new HttpClient();



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
