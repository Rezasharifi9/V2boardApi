using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using Org.BouncyCastle.Asn1.X509;
using Stimulsoft.Base.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Windows;
using Telegram.Bot;
using Telegram.Bot.Types;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [LogActionFilter]
    public class SettingsController : Controller
    {
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbBankCardNumbers> RepositoryCards { get; set; }
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public SettingsController()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryCards = new Repository<tbBankCardNumbers>(db);
        }

        // GET: App/Settings
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult Index()
        {
            return View();
        }

        #region چک کردن ارتباط با mySql 

        [HttpPost]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> ScanPort(string IPAddress, string DatabaseName, string Username, string Password)
        {
            try
            {
                var Connection = "Server=" + IPAddress + ";User ID=" + Username + ";Password=" + Password + ";Database=" + DatabaseName + "";
                MySqlEntities mySqlEntities = new MySqlEntities(Connection);
                await mySqlEntities.OpenAsync();
                await mySqlEntities.CloseAsync();
                return Content("success-" + "ارتباط با سرویس MYSQL برقرار شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "عدم برقراری ارتباط با MYSQL ");
                return Content("warning-" + "عدم برقرار ارتباط با سرویس MYSQL");
            }



        }

        #endregion

        #region تنظیمات سرور
        [AuthorizeApp(Roles = "1")]
        public ActionResult _BaseSetting()
        {
            var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
            return PartialView(Use.tbServers);
        }

        [HttpPost]
        [AuthorizeApp(Roles = "1")]
        public ActionResult SaveServerSetting(string IPAddress, string ServerAddress, string DatabaseName, string Username, string Password, string SubAddr)
        {
            try
            {



                var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
                if (Use.tbServers != null)
                {
                    Use.tbServers.ServerIP = IPAddress;
                    Use.tbServers.ServerAddress = ServerAddress;
                    Use.tbServers.DataBaseName = DatabaseName;
                    Use.tbServers.Username = Username;
                    Use.tbServers.Password = Password;
                    Use.tbServers.SubAddress = SubAddr;
                    RepositoryUser.Save();
                    logger.Info("تنظیمات سرور ویرایش شد");
                    return Content("success-" + "اطلاعات سرور با موفقیت ذخیره شد");
                }
                else
                {
                    tbServers server = new tbServers();
                    server.ServerIP = IPAddress;
                    server.DataBaseName = DatabaseName;
                    server.Username = Username;
                    server.Password = Password;
                    server.SubAddress = SubAddr;
                    server.ServerAddress = ServerAddress;
                    RepositoryServer.Insert(server);
                    RepositoryServer.Save();

                    Use.FK_Server_ID = server.ServerID;

                    RepositoryUser.Save();
                    logger.Info("تنظیمات سرور ویرایش شد");
                    return Content("success-" + "اطلاعات سرور با موفقیت ذخیره شد");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ذخیره سازی تنظیمات سرور با خطا مواجه شد");
                return Content("danger-", "ذخیره سازی اطلاعات با خطا مواجه شد");
            }
        }

        #endregion

        #region تنظیمات ربات
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult _BotSetting()
        {
            var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();

            if (Use.tbBotSettings.FirstOrDefault() == null)
            {
                return PartialView(new tbBotSettings());
            }

            return PartialView(Use.tbBotSettings.FirstOrDefault());
        }

        [HttpPost]
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult SaveBotSetting(string BotId, string BotToken, long TelegramUserId, string ChannelId, int PricePerMonth_Major, int PricePerGig_Major, bool Active, bool RequiredJoinChannel, bool IsActiveCardToCard, bool IsActiveSendReceipt, double? Present_Discount = null)
        {

            try
            {
                var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();

                if (ChannelId != null && RequiredJoinChannel)
                {
                    try
                    {
                        TelegramBotClient bot = new TelegramBotClient(BotToken);
                        try
                        {
                            var joined = bot.GetChatMemberAsync("@" + ChannelId, TelegramUserId);
                            var s = joined.Result.Status;

                        }
                        catch (Exception ex)
                        {
                            if (ex.InnerException != null)
                            {
                                if (ex.InnerException.Message.Contains("not found"))
                                {
                                    return Content("error-" + "جهت فعال سازی عضویت اجباری آیدی کانال را درست وارد کنید");
                                }
                                if (ex.InnerException.Message.Contains("inaccessible"))
                                {
                                    return Content("error-" + "جهت فعال سازی عضویت اجباری باید ربات را داخل کانال خود ادمین کنید");
                                }
                            }
                            return Content("error-" + "اعتبار سنجی توکن ناموفق لطفا توکن را چک کنید");
                        }
                    }
                    catch (Exception ex)
                    {
                        return Content("error-" + "اعتبار سنجی توکن ناموفق لطفا توکن را چک کنید");
                    }

                }
                else
                {
                    TelegramBotClient bot = new TelegramBotClient(BotToken);
                    try
                    {
                        var res =  bot.SendTextMessageAsync(TelegramUserId, "پیغام جهت صحت سنجی اطلاعات ثبت شده در تنظیمات می باشد");
                        var s = res.Result;
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                        {
                            if (ex.InnerException.Message.Contains("not found"))
                            {
                                return Content("error-" + "شناسه تلگرام ادمین اشتباه است لطفا شناسه را از داخل getmyid_bot چک کنید");
                            }
                        }
                        return Content("error-" + "اعتبار سنجی توکن ناموفق لطفا توکن را چک کنید");
                    }

                }

                

                if (Use.tbBotSettings.Count == 0)
                {
                    //tbBotSettings botSettings = new tbBotSettings();

                    //botSettings.RequiredJoinChannel = RequiredJoinChannel;
                    //botSettings.Bot_Token = BotToken;
                    //botSettings.Bot_ID = BotId;
                    //botSettings.Active = Active;
                    //botSettings.AdminBot_ID = TelegramUserId;
                    //botSettings.IsActiveCardToCard = IsActiveCardToCard;
                    //botSettings.IsActiveSendReceipt = IsActiveSendReceipt;
                    //var res = BotManager.GetBot(Use.Username);
                    //if (res == null)
                    //{
                    //    BotManager.AddBot(Use.Username, BotToken);
                    //}
                    //else
                    //{
                    //    BotManager.Bots[Use.Username].Token = BotToken;
                    //}
                    //if (ChannelId != null)
                    //{
                    //    botSettings.ChannelID = ChannelId;
                    //}
                    //if (Present_Discount != null && Present_Discount != 0)
                    //{
                    //    botSettings.Present_Discount = Present_Discount / 100;
                    //}
                    //botSettings.PricePerMonth_Major = PricePerMonth_Major;
                    //botSettings.PricePerGig_Major = PricePerGig_Major;





                    //Use.tbBotSettings.Add(botSettings);
                    //RepositoryUser.Save();
                    return Content("warning-" + "ادمین قبل از تنظیم باید برای شما قیمت پایه را تعریف کند");
                }
                else
                {
                    var botSettings = Use.tbBotSettings.FirstOrDefault();
                    if (ChannelId != null)
                    {
                        botSettings.ChannelID = ChannelId;
                    }
                    if (Present_Discount != null && Present_Discount != 0)
                    {
                        botSettings.Present_Discount = Present_Discount / 100;
                    }

                    var res = BotManager.GetBot(Use.Username);
                    if (res == null)
                    {
                        BotManager.AddBot(Use.Username, BotToken);
                    }
                    else
                    {

                        if (BotManager.GetBot(Use.Username).Token != BotToken)
                        {
                            BotManager.StopBot(Use.Username);
                        }
                    }

                    if (PricePerMonth_Major < botSettings.PricePerMonth_Admin)
                    {
                        return Content("warning-" + "مبلغ به ازای هر ماه شما نمی تواند کمتر از قیمت تنظیم شده ادمین باشد");
                    }
                    if (PricePerGig_Major < botSettings.PricePerGig_Admin)
                    {
                        return Content("warning-" + "مبلغ به ازای هر گیگ شما نمی تواند کمتر از قیمت تنظیم شده ادمین باشد");
                    }

                    botSettings.RequiredJoinChannel = RequiredJoinChannel;
                    botSettings.Bot_Token = BotToken;
                    botSettings.Bot_ID = BotId;
                    botSettings.Active = Active;
                    botSettings.AdminBot_ID = TelegramUserId;
                    botSettings.PricePerMonth_Major = PricePerMonth_Major;
                    botSettings.PricePerGig_Major = PricePerGig_Major;
                    botSettings.IsActiveCardToCard = IsActiveCardToCard;
                    botSettings.IsActiveSendReceipt = IsActiveSendReceipt;
                    RepositoryUser.Save();

                    logger.Info("تنظیمات ربات ویرایش شد");
                }

                return Content("success-" + "اطلاعات ربات با موفقیت ذخیره شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ذخیره سازی تنظیمات ربات با خطا مواجه شد");
                return Content("error-" + "ذخیره سازی اطلاعات با خطا مواجه شد");
            }


        }

        #endregion

        #region کارت بانکی ها
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult _BankNumbers()
        {
            var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
            return PartialView(Use.tbBankCardNumbers.ToList());
        }

        [HttpPost]
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult SaveBankNumbers(string CardNumber, string NameOfCard, string SmsNumberOfCard, string phoneNumber, int Card_ID)
        {

            try
            {
                var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
                if (Use.tbBankCardNumbers.Count() > 0)
                {
                    if (Card_ID != 0)
                    {
                        var Card = Use.tbBankCardNumbers.Where(p => p.CardNumber_ID == Card_ID).FirstOrDefault();
                        if (Card != null)
                        {
                            Card.CardNumber = CardNumber;
                            Card.InTheNameOf = NameOfCard;
                            Card.BankSmsNumber = SmsNumberOfCard;
                            Card.phoneNumber = phoneNumber;
                            RepositoryUser.Save();
                            logger.Info("تنظیمات کارت بانکی ویرایش شد");
                        }
                    }
                    else
                    {
                        tbBankCardNumbers Card1 = new tbBankCardNumbers();
                        Card1.CardNumber = CardNumber;
                        Card1.InTheNameOf = NameOfCard;
                        Card1.BankSmsNumber = SmsNumberOfCard;
                        Card1.phoneNumber = phoneNumber;
                        Card1.Active = false;
                        Use.tbBankCardNumbers.Add(Card1);
                        RepositoryUser.Save();
                        logger.Info("کارت بانکی جدید اضافه شد");
                    }
                }
                else
                {
                    tbBankCardNumbers Card = new tbBankCardNumbers();
                    Card.CardNumber = CardNumber;
                    Card.InTheNameOf = NameOfCard;
                    Card.BankSmsNumber = SmsNumberOfCard;
                    Card.phoneNumber = phoneNumber;
                    Card.Active = true;
                    Use.tbBankCardNumbers.Add(Card);
                    RepositoryUser.Save();
                    logger.Info("کارت بانکی جدید اضافه شد");

                }
                return Content("success-" + "اطلاعات بانکی با موفقیت ذخیره شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ذخیره سازی تنظیمات کارت با خطا مواجه شد");
                return Content("danger-", "ذخیره سازی اطلاعات با خطا مواجه شد");
            }
        }

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult DeleteCard(string CardNumber)
        {
            try
            {
                var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
                if (Use.tbBankCardNumbers != null)
                {
                    var Card = Use.tbBankCardNumbers.Where(p => p.CardNumber == CardNumber).FirstOrDefault();
                    if (Card != null)
                    {
                        Use.tbBankCardNumbers.Remove(Card);
                    }
                    RepositoryUser.Save();
                }
                logger.Info("کارت با موفقیت حذف شد");
                return Content("success-" + "کارت با موفقیت حذف شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "حذف کارت با خطا مواجه شد");
                return Content("danger-", "ذخیره سازی اطلاعات با خطا مواجه شد");
            }
        }

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult DeactiveCard(int id)
        {
            try
            {
                var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
                if (Use.tbBankCardNumbers != null)
                {
                    var Card = Use.tbBankCardNumbers.Where(p => p.CardNumber_ID == id).FirstOrDefault();
                    if (Card != null)
                    {
                        foreach (var item in Use.tbBankCardNumbers)
                        {
                            item.Active = false;
                        }
                        Card.Active = true;
                    }
                    RepositoryUser.Save();
                    logger.Info("کارت با موفقیت غیرفعال شد");
                }
                return RedirectToAction("Index", "Settings");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "غیرفعال سازی کارت با خطا مواجه شد");
                return RedirectToAction("Index", "Settings");
            }
        }

        [HttpGet]
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult _EditBankNumber(int CardID)
        {
            var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
            var Card = Use.tbBankCardNumbers.Where(p => p.CardNumber_ID == CardID).FirstOrDefault();

            return PartialView(Card);
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                RepositoryUser.Dispose();
                RepositoryServer.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}