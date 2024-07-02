using DataLayer.DomainModel;
using DataLayer.Repository;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Windows;
using Telegram.Bot;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{

    public class SettingsController : Controller
    {
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbBankCardNumbers> RepositoryCards { get; set; }

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
        public ActionResult ScanPort(string IPAddress, string DatabaseName, string Username, string Password)
        {
            try
            {
                var Connection = "Server=" + IPAddress + ";User ID=" + Username + ";Password=" + Password + ";Database=" + DatabaseName + "";
                MySqlEntities mySqlEntities = new MySqlEntities(Connection);
                mySqlEntities.Open();
                return Content("success-" + "ارتباط با سرویس MYSQL برقرار شد");
            }
            catch (Exception ex)
            {
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
                    return Content("success-" + "اطلاعات سرور با موفقیت ذخیره شد");
                }
            }
            catch (Exception ex)
            {
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
        public ActionResult SaveBotSetting(string BotId, string BotToken, long TelegramUserId, string ChannelId, int PricePerMonth_Major, int PricePerGig_Major, bool Active, bool RequiredJoinChannel, double? Present_Discount = null)
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
                        var joined = bot.GetChatMemberAsync(900535071, TelegramUserId);
                        var s = joined.Result.Status;

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
                    tbBotSettings botSettings = new tbBotSettings();

                    botSettings.RequiredJoinChannel = RequiredJoinChannel;
                    botSettings.Bot_Token = BotToken;
                    botSettings.Bot_ID = BotId;
                    botSettings.Active = Active;
                    botSettings.AdminBot_ID = TelegramUserId;
                    if (ChannelId != null)
                    {
                        botSettings.ChannelID = ChannelId;
                    }
                    if (Present_Discount != null && Present_Discount != 0)
                    {
                        botSettings.Present_Discount = Present_Discount / 100;
                    }
                    botSettings.PricePerMonth_Major = PricePerMonth_Major;
                    botSettings.PricePerGig_Major = PricePerGig_Major;
                    Use.tbBotSettings.Add(botSettings);
                    RepositoryUser.Save();
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
                    botSettings.RequiredJoinChannel = RequiredJoinChannel;
                    botSettings.Bot_Token = BotToken;
                    botSettings.Bot_ID = BotId;
                    botSettings.Active = Active;
                    botSettings.AdminBot_ID = TelegramUserId;
                    botSettings.PricePerMonth_Major = PricePerMonth_Major;
                    botSettings.PricePerGig_Major = PricePerGig_Major;
                    RepositoryUser.Save();
                }

                return Content("success-" + "اطلاعات ربات با موفقیت ذخیره شد");
            }
            catch (Exception ex)
            {
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

                }
                return Content("success-" + "اطلاعات بانکی با موفقیت ذخیره شد");
            }
            catch (Exception ex)
            {
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
                return Content("success-" + "کارت با موفقیت حذف شد");
            }
            catch (Exception ex)
            {
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
                }
                return RedirectToAction("Index", "Settings");
            }
            catch (Exception ex)
            {
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

    }
}