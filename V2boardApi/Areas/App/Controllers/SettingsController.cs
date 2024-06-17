using DataLayer.DomainModel;
using DataLayer.Repository;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Windows;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [V2boardApi.Tools.Authorize]
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
        public ActionResult Index()
        {
            return View();
        }

        #region چک کردن ارتباط با mySql 

        [HttpPost]
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

        public ActionResult _BaseSetting()
        {
            var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
            return PartialView(Use.tbServers);
        }

        [HttpPost]
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

        public ActionResult _BotSetting()
        {
            var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
            return PartialView(Use.tbServers);
        }

        [HttpPost]
        public ActionResult SaveBotSetting(string BotId, string BotToken, long TelegramUserId, string ChannelId)
        {

            try
            {
                var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
                if (Use.tbServers == null)
                {
                    tbServers server = new tbServers();
                    server.Robot_ID = BotId;
                    server.Robot_Token = BotToken;
                    server.AdminTelegramUniqID = TelegramUserId;
                    server.Channel_ID = ChannelId;
                    RepositoryServer.Insert(server);
                    RepositoryServer.Save();

                    Use.FK_Server_ID = server.ServerID;

                    RepositoryUser.Save();
                }
                else
                {
                    Use.tbServers.Robot_ID = BotId;
                    Use.tbServers.Robot_Token = BotToken;
                    Use.tbServers.AdminTelegramUniqID = TelegramUserId;
                    Use.tbServers.Channel_ID = ChannelId;
                    RepositoryUser.Save();
                }

                return Content("success-" + "اطلاعات ربات با موفقیت ذخیره شد");
            }
            catch (Exception ex)
            {
                return Content("danger-", "ذخیره سازی اطلاعات با خطا مواجه شد");
            }


        }

        #endregion

        #region کارت بانکی ها

        public ActionResult _BankNumbers()
        {
            var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
            if (Use.tbServers.tbBankCardNumbers != null)
            {
                return PartialView(Use.tbServers.tbBankCardNumbers.FirstOrDefault());
            }
            return PartialView();
        }

        [HttpPost]
        public ActionResult SaveBankNumbers(string CardNumber, string NameOfCard, string SmsNumberOfCard,string phoneNumber)
        {

            try
            {
                var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
                if (Use.tbServers.tbBankCardNumbers != null)
                {
                    var Card = Use.tbServers.tbBankCardNumbers.Where(p => p.CardNumber == CardNumber).FirstOrDefault();
                    if (Card != null)
                    {
                        Card.CardNumber = CardNumber;
                        Card.InTheNameOf = NameOfCard;
                        Card.BankSmsNumber = SmsNumberOfCard;
                        Card.phoneNumber = phoneNumber;
                        Card.Active = true;
                        RepositoryUser.Save();
                    }
                }
                return Content("success-" + "اطلاعات بانکی با موفقیت ذخیره شد");
            }
            catch(Exception ex)
            {
                return Content("danger-", "ذخیره سازی اطلاعات با خطا مواجه شد");
            }

            return PartialView();
        }


        public ActionResult DeleteCard(string CardNumber)
        {
            try
            {
                var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
                if (Use.tbServers.tbBankCardNumbers != null)
                {
                    var Card = Use.tbServers.tbBankCardNumbers.Where(p => p.CardNumber == CardNumber).FirstOrDefault();
                    if (Card != null)
                    {
                        Use.tbServers.tbBankCardNumbers.Remove(Card);
                    }
                    RepositoryUser.Save();
                }
                return Content("success-" + "کارت با موفقیت حذف شد");
            }
            catch ( Exception ex)
            {
                return Content("danger-", "ذخیره سازی اطلاعات با خطا مواجه شد");
            }
        }


        public ActionResult DeactiveCard(int id)
        {
            try
            {
                var Use = RepositoryUser.Where(p => p.Username == User.Identity.Name).First();
                if (Use.tbServers.tbBankCardNumbers != null)
                {
                    var Card = Use.tbServers.tbBankCardNumbers.Where(p => p.CardNumber_ID == id).FirstOrDefault();
                    if (Card != null)
                    {
                        if (Card.Active == true)
                        {
                            Card.Active = false;
                        }
                        else
                        {
                            Card.Active = true;
                        }
                    }
                    RepositoryUser.Save();
                }
                return RedirectToAction("Index", "Settings");
            }
            catch(Exception ex)
            {
                return RedirectToAction("Index", "Settings");
            }
        }

        #endregion

    }
}