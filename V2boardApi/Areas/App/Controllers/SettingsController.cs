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
        [AuthorizeApp(Roles = "1,2,3,4")]
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
        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult _BotSetting()
        {
            var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();

            if (Use.tbBotSettings.FirstOrDefault() == null)
            {
                return PartialView(new tbBotSettings());
            }

            return PartialView(Use.tbBotSettings.FirstOrDefault());
        }

        #endregion

        #region کارت بانکی ها
        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult _BankNumbers()
        {
            var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
            return PartialView(Use.tbBankCardNumbers.ToList());
        }

        [HttpPost]
        [AuthorizeApp(Roles = "1,2,3,4")]
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

        [AuthorizeApp(Roles = "1,2,3,4")]
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

        [AuthorizeApp(Roles = "1,2,3,4")]
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
        [AuthorizeApp(Roles = "1,2,3,4")]
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