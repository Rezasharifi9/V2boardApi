using DataLayer.DomainModel;
using DataLayer.Repository;
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

namespace V2boardApi.Areas.App.Controllers
{
    [System.Web.Mvc.Authorize]
    public class TelegramUsersController : Controller
    {
        private V2boardSiteEntities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbTelegramUsers> RepositoryTelegramUsers { get; set; }
        private Repository<tbServers> RepositoryServers { get; set; }
        Repository<tbLinks> RepositoryLinks { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public TelegramUsersController()
        {
            db = new V2boardSiteEntities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryPlans = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryTelegramUsers = new Repository<tbTelegramUsers>(db);
            RepositoryServers = new Repository<tbServers>(db);
            RepositoryLinks = new Repository<tbLinks>(db);
        }


        #region لیست کاربران

        [System.Web.Mvc.Authorize]
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult _PartialGetAllUsers(string username = null)
        {
            var Use = db.tbUsers.Where(p => p.Username == User.Identity.Name).First();
            var Users = new List<tbTelegramUsers>();
            if (username != null)
            {
                Users = RepositoryTelegramUsers.Where(p => p.Tel_RobotID == Use.tbServers.Robot_ID && p.Tel_Username == username).ToList();
            }
            else
            {
                Users = RepositoryTelegramUsers.Where(p => p.Tel_RobotID == Use.tbServers.Robot_ID).ToList();
            }
            return View(Users);
        }


        #endregion


        public ActionResult _PartialPublicMessage()
        {
            return PartialView();
        }


        public ActionResult SendPublicMessage(string message)
        {
            try
            {
                var Use = db.tbUsers.Where(p => p.Username == User.Identity.Name).First();

                var Users = RepositoryTelegramUsers.Where(p => p.Tel_RobotID == Use.tbServers.Robot_ID).ToList();
                TelegramBotClient botClient = new TelegramBotClient(Use.tbServers.Robot_Token);
                foreach (var item in Users)
                {
                    botClient.SendTextMessageAsync(item.Tel_UniqUserID, message);
                }

                return Content("1");
            }
            catch (Exception ex)
            {
                return Content("2");
            }
        }


        #region شارژ کیف پول کاربر

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
        public ActionResult _SendMessage(int id)
        {
            var TelegramUser = RepositoryTelegramUsers.Where(p => p.Tel_UserID == id).FirstOrDefault();
            ViewBag.id = id;
            ViewBag.name = TelegramUser.Tel_FirstName + TelegramUser.Tel_LastName;
            return PartialView();
        }

        [HttpPost]
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
                    return Content("1");

                }
                else
                {
                    return Content("2");
                }
            }
            catch(Exception ex)
            {
                return Content("3");
            }
            
        }


        #endregion

        [HttpGet]
        public ActionResult Orders(int TelegramUserId)
        {
            return View();
        }

        public ActionResult Accounts(int id)
        {
            var Links = RepositoryLinks.Where(p => p.FK_TelegramUserID == id).ToList();

            var Use = db.tbUsers.Where(p => p.Username == User.Identity.Name).First();

            MySqlEntities mysql = new MySqlEntities(Use.tbServers.ConnectionString);
            mysql.Open();

            List<AccountsViewModel> accounts = new List<AccountsViewModel>();
            foreach (var link in Links)
            {
                
                var Reader = mysql.GetData("select * from v2_user where email='" + link.tb_RandomEmail + "'");
                if (Reader.Read())
                {
                    AccountsViewModel account = new AccountsViewModel();
                    account.V2boardUsername = link.tb_RandomEmail;
                    account.State = "فعال";
                    account.TotalVolume = Utility.ConvertByteToGB(Reader.GetInt64("transfer_enable"));
                    var exp = Reader.GetBodyDefinition("expired_at");
                    var OnlineTime = Reader.GetBodyDefinition("t");
                    if (exp != "")
                    {
                        var e = Convert.ToInt64(exp);
                        var ex = Utility.ConvertSecondToDatetime(e);
                        account.ExpireDate = Utility.ConvertDateTimeToShamsi(ex);
                        if (ex <= DateTime.Now)
                        {
                            account.State = "پایان تاریخ اشتراک";
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
                        account.State = "اتمام حجم";
                    }
                    else
                    if (Convert.ToBoolean(Reader.GetSByte("banned")))
                    {
                        account.State = "مسدود";
                    }

                    account.RemainingVolume = Math.Round(dd, 2) + " GB";
                    accounts.Add(account);
                }
                Reader.Close();
            }
            mysql.Close();

            return PartialView(accounts);
        }
    }
}