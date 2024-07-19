using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using V2boardApi.Models.AdminModel;
using V2boardApi.Tools;
using V2boardBot.Functions;

namespace V2boardApi.Areas.App.Controllers
{
    [LogActionFilter]
    public class TelegramUsersController : Controller
    {
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbTelegramUsers> RepositoryTelegramUsers { get; set; }
        private Repository<tbServers> RepositoryServers { get; set; }
        Repository<tbLinks> RepositoryLinks { get; set; }
        private System.Timers.Timer Timer { get; set; }
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public TelegramUsersController()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryPlans = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryTelegramUsers = new Repository<tbTelegramUsers>(db);
            RepositoryServers = new Repository<tbServers>(db);
            RepositoryLinks = new Repository<tbLinks>(db);
        }


        #region لیست کاربران

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult _PartialGetAllUsers(string username = null)
        {
            var Use = db.tbUsers.Where(p => p.Username == User.Identity.Name).First();
            return View(Use.tbTelegramUsers.ToList());
        }


        #endregion

        #region ارسال پیام همگانی

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult _PartialPublicMessage()
        {
            return PartialView();
        }

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult SendPublicMessage(string message)
        {
            try
            {
                var Use = db.tbUsers.Where(p => p.Username == User.Identity.Name).First();

                var BotSetting = Use.tbBotSettings.FirstOrDefault();
                if (BotSetting != null)
                {
                    if (BotSetting.Bot_Token != null)
                    {
                        TelegramBotClient botClient = new TelegramBotClient(BotSetting.Bot_Token);
                        foreach (var item in Use.tbTelegramUsers.ToList())
                        {
                            botClient.SendTextMessageAsync(item.Tel_UniqUserID, message);
                        }
                    }
                }

                logger.Info("پیام همگانی ارسال شد");

                return Content("1");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ارسال پیام همگانی با خطا مواجه شد");
                return Content("2");
            }
        }

        #endregion

        #region شارژ کیف پول کاربر
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult _EditWallet(int id)
        {
            var us = RepositoryTelegramUsers.Where(p => p.Tel_UserID == id).FirstOrDefault();
            if (us != null)
            {
                return PartialView(us);
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult EditWallet(int id, int wallet)
        {
            var us = RepositoryTelegramUsers.Where(p => p.Tel_UserID == id).FirstOrDefault();
            if (us != null)
            {
                us.Tel_Wallet = wallet;
                RepositoryUser.Save();
                logger.Info("شارژ کیف پول کاربر تلگرام تغییر کرد");
                return Content("1");
            }
            else
            {
                return Content("2");
            }
        }

        #endregion

        #region پیام به کاربر

        [HttpGet]
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult _SendMessage(int id)
        {
            var TelegramUser = RepositoryTelegramUsers.Where(p => p.Tel_UserID == id).FirstOrDefault();
            ViewBag.id = id;
            ViewBag.name = TelegramUser.Tel_FirstName + TelegramUser.Tel_LastName;
            return PartialView();
        }

        [HttpPost]
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult SendMessage(string message, int id)
        {

            try
            {
                var TelegramUser = RepositoryTelegramUsers.Where(p => p.Tel_UserID == id).FirstOrDefault();
                if (TelegramUser != null)
                {

                    var server = RepositoryServers.Where(p => p.Robot_ID == TelegramUser.Tel_RobotID).First();

                    TelegramBotClient bot = new TelegramBotClient(server.Robot_Token);
                    bot.SendTextMessageAsync(TelegramUser.Tel_UniqUserID, message);
                    bot.CloseAsync();
                    logger.Info("به کاربر تلرام " + TelegramUser.Tel_Username + " پیام ارسال شد");
                    return Content("1");

                }
                else
                {
                    return Content("2");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ارسال پیام با خطا مواجه شد");
                return Content("3");
            }

        }


        #endregion

        //[HttpGet]
        //[AuthorizeApp(Roles = "1,2")]
        //public ActionResult Orders(int TelegramUserId)
        //{
        //    return View();
        //}
        #region لیست اشتراک های کاربر تلگرام

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult Accounts(int id)
        {
            var Links = RepositoryLinks.Where(p => p.FK_TelegramUserID == id).ToList();
            var TelUser = RepositoryTelegramUsers.GetById(id);
            var Use = db.tbUsers.Where(p => p.Username == User.Identity.Name).First();

            MySqlEntities mysql = new MySqlEntities(Use.tbServers.ConnectionString);
            mysql.Open();

            List<AccountsViewModel> accounts = new List<AccountsViewModel>();
            foreach (var link in Links)
            {

                var Reader = mysql.GetData("select * from v2_user where email='" + link.tbL_Email + "'");
                if (Reader.Read())
                {
                    AccountsViewModel account = new AccountsViewModel();
                    account.LinkID = link.tbLink_ID;
                    account.V2boardUsername = link.tbL_Email;
                    account.State = "Active";
                    account.TotalVolume = Utility.ConvertByteToGB(Reader.GetInt64("transfer_enable")) + " GB";
                    var exp = Reader.GetBodyDefinition("expired_at");
                    var OnlineTime = Reader.GetBodyDefinition("t");
                    if (exp != "")
                    {
                        var e = Convert.ToInt64(exp);
                        var ex = Utility.ConvertSecondToDatetime(e);
                        account.ExpireDate = Utility.ConvertDatetimeToShamsiDate(ex);
                        if (ex <= DateTime.Now)
                        {
                            account.State = "ExpireTime";
                        }
                    }
                    var u = Reader.GetInt64("u");
                    var d = Reader.GetInt64("d");
                    var re = Utility.ConvertByteToGB(u + d);
                    account.UsedVolume = Math.Round(re, 2) + " GB";

                    var vol = Reader.GetInt64("transfer_enable") - (u + d);
                    var dd = Utility.ConvertByteToGB(vol);

                    if (vol <= 0)
                    {
                        account.State = "EndVolume";
                    }
                    else
                    if (Convert.ToBoolean(Reader.GetSByte("banned")))
                    {
                        account.State = "Ban";
                    }

                    account.RemainingVolume = Math.Round(dd, 2) + " GB";
                    accounts.Add(account);
                }
                Reader.Close();
            }
            mysql.Close();

            TempData["FullName"] = TelUser.Tel_Username + " (" + TelUser.Tel_FirstName + " " + TelUser.Tel_LastName + ")";
            return PartialView(accounts);
        }

        #endregion

        #region انتقال اشتراک
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult _MoveAccount(int LinkID)
        {
            var TelegramUsers = RepositoryTelegramUsers.Where(p => p.tbUsers.Username == User.Identity.Name).ToList();
            TempData["LinkID"] = LinkID;
            return PartialView(TelegramUsers);
        }

        [AuthorizeApp(Roles = "1,2")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult MoveAccount(int LinkID, int TelUser)
        {
            var Link = RepositoryLinks.Where(p => p.tbLink_ID == LinkID).FirstOrDefault();
            if (Link != null)
            {
                Link.FK_TelegramUserID = TelUser;
            }
            RepositoryLinks.Save();
            return RedirectToAction("Accounts", "TelegramUsers", new { id = TelUser });
        }

        #endregion
    }
}