using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
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

        [AuthorizeApp(Roles = "1")]
        public ActionResult Index()
        {
            var user = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (user?.tbServers == null)
                return View(new tbServers());

            return View(user.tbServers);
        }

        #region تست و ذخیره MySQL

        [HttpPost]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> TestMysqlConnection(string ServerIP, string DataBaseName, string Username, string Password)
        {
            try
            {
                var connection = BuildMysqlConnectionString(ServerIP, DataBaseName, Username, Password);
                using (var mySqlEntities = new MySqlEntities(connection))
                {
                    await mySqlEntities.OpenAsync();
                    await mySqlEntities.CloseAsync();
                }
                return Json(new { status = "success", message = "ارتباط با سرویس MySQL برقرار شد" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "عدم برقراری ارتباط با MySQL");
                return Json(new { status = "warning", message = "عدم برقراری ارتباط با سرویس MySQL" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeApp(Roles = "1")]
        public ActionResult SaveMysqlSettings(string ServerAddress, string ServerIP, string Username, string Password, string DataBaseName, string ApiToken_V2board)
        {
            try
            {
                var server = GetCurrentServer();
                if (server == null)
                    return Json(new { status = "danger", message = "سرور یافت نشد" }, JsonRequestBehavior.AllowGet);

                server.ServerAddress = ServerAddress?.Trim();
                server.ServerIP = ServerIP?.Trim();
                server.Username = Username?.Trim();
                server.Password = Password;
                server.DataBaseName = DataBaseName?.Trim();
                server.ApiToken_V2board = ApiToken_V2board?.Trim();

                RepositoryServer.Save();
                logger.Info("تنظیمات MySQL پنل ذخیره شد");
                return Json(new { status = "success", message = "تنظیمات ارتباط با MySQL ذخیره شد" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ذخیره تنظیمات MySQL با خطا مواجه شد");
                return Json(new { status = "danger", message = "ذخیره تنظیمات با خطا مواجه شد" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region تنظیمات عمومی

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeApp(Roles = "1")]
        public ActionResult SaveGeneralSettings(string SubAddress, string BackupSubAddr)
        {
            try
            {
                var server = GetCurrentServer();
                if (server == null)
                    return Json(new { status = "danger", message = "سرور یافت نشد" }, JsonRequestBehavior.AllowGet);

                server.SubAddress = SubAddress?.Trim();
                server.BackupSubAddr = BackupSubAddr?.Trim();

                RepositoryServer.Save();
                logger.Info("تنظیمات عمومی پنل ذخیره شد");
                return Json(new { status = "success", message = "تنظیمات عمومی ذخیره شد" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ذخیره تنظیمات عمومی با خطا مواجه شد");
                return Json(new { status = "danger", message = "ذخیره تنظیمات با خطا مواجه شد" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region تنظیمات ربات (سطح سرور)

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeApp(Roles = "1")]
        public ActionResult SaveBotServerSettings(double? Discount_Percent, long? AdminTelegramUniqID, string Channel_ID, string BotbaseAddress, string Robot_Token, string Robot_ID, long? BotID)
        {
            try
            {
                var server = GetCurrentServer();
                if (server == null)
                    return Json(new { status = "danger", message = "سرور یافت نشد" }, JsonRequestBehavior.AllowGet);

                server.Discount_Percent = Discount_Percent;
                server.AdminTelegramUniqID = AdminTelegramUniqID;
                server.Channel_ID = Channel_ID?.Trim();
                server.BotbaseAddress = BotbaseAddress?.Trim();
                server.Robot_Token = Robot_Token?.Trim();
                server.Robot_ID = Robot_ID?.Trim();
                server.BotID = BotID;

                RepositoryServer.Save();
                logger.Info("تنظیمات ربات سرور ذخیره شد");
                return Json(new { status = "success", message = "تنظیمات ربات ذخیره شد" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ذخیره تنظیمات ربات با خطا مواجه شد");
                return Json(new { status = "danger", message = "ذخیره تنظیمات با خطا مواجه شد" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region کارت بانکی (سایر بخش‌ها)

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
                        Use.tbBankCardNumbers.Remove(Card);
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
                            item.Active = false;
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

        private tbServers GetCurrentServer()
        {
            var user = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            return user?.tbServers;
        }

        private static string BuildMysqlConnectionString(string serverIp, string databaseName, string username, string password)
        {
            return $"Server={serverIp};Port=3306;Database={databaseName};Uid={username};Pwd={password};SslMode=None;AllowPublicKeyRetrieval=True;";
        }

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
