using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using V2boardApi.Models.AdminModel;
using V2boardApi.Models.MysqlModel;
using WebGrease;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Web.Security;
using Microsoft.Ajax.Utilities;
using V2boardApi.Tools;
using System.Globalization;
using MySqlX.XDevAPI;
using Telegram.Bot;
using System.Text;
using DeviceDetectorNET;
using DeviceDetectorNET.Parser;
using Telegram.Bot.Types;
using System.Data.Entity.Validation;

namespace V2boardApi.Areas.App.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbUserFactors> RepositoryUserFactors { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public AdminController()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryPlans = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryUserFactors = new Repository<tbUserFactors>(db);
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
            var Users = new List<tbUsers>();
            if (username != null)
            {
                Users = RepositoryUser.table.Where(p => p.FK_Server_ID == Use.FK_Server_ID && p.Username.Contains(username)).OrderByDescending(p => p.User_ID).ToList();
            }
            else
            {
                Users = RepositoryUser.table.Where(p => p.FK_Server_ID == Use.FK_Server_ID).OrderByDescending(p => p.User_ID).ToList();
            }
            return View(Users);
        }


        #endregion

        #region افزودن کاربر

        [System.Web.Mvc.Authorize]
        public ActionResult _PartialCreate()
        {
            return View();
        }

        [System.Web.Mvc.Authorize]
        [System.Web.Mvc.HttpPost]
        public ActionResult Create(tbUsers tbUser, List<int> Plans)
        {

            try
            {
                var Us = db.tbUsers.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
                if (Us != null)
                {
                    var CheckExistsUser = RepositoryUser.Where(p => p.Username == tbUser.Username).Any();
                    if (CheckExistsUser)
                    {
                        return Content("2");
                    }
                    else
                    {
                        tbUser.Token = (tbUser.Username + tbUser.Password).ToSha256();
                        tbUser.Password = tbUser.Password.ToSha256();
                        tbUser.IsRenew = false;
                        tbUser.Status = true;
                        tbUser.Wallet = 0;
                        tbUser.Role = 2;
                        tbUser.FK_Server_ID = Us.FK_Server_ID;
                        tbUser.tbLinkUserAndPlans = new List<tbLinkUserAndPlans>();
                        foreach (var item in Plans)
                        {
                            tbLinkUserAndPlans plan = new tbLinkUserAndPlans();
                            plan.L_FK_P_ID = Convert.ToInt32(item);
                            plan.L_Status = true;
                            tbUser.tbLinkUserAndPlans.Add(plan);
                        }

                        RepositoryUser.Insert(tbUser);
                        RepositoryUser.Save();

                        return Content("1");
                    }

                }
                else
                {
                    return Content("3");
                }
            }
            catch (Exception ex)
            {
                return Content("3");
            }
        }

        #endregion

        #region ویرایش اطلاعات کاربر

        [System.Web.Mvc.Authorize]
        public ActionResult _PartialEdit(int id)
        {
            var us = db.tbUsers.Where(p => p.User_ID == id).FirstOrDefault();
            if (us != null)
            {
                return View(us);
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }

        }


        [System.Web.Mvc.Authorize]
        [System.Web.Mvc.HttpPost]
        public ActionResult Edit(tbUsers tbUser, List<int> Plans)
        {
            try
            {
                var Us = db.tbUsers.Where(p => p.User_ID == tbUser.User_ID).FirstOrDefault();
                if (Us != null)
                {
                    if (tbUser.Username != Us.Username)
                    {
                        var CheckExistsUser = RepositoryUser.Where(p => p.Username == tbUser.Username).Any();
                        if (CheckExistsUser)
                        {
                            return Content("2");
                        }
                    }

                    Us.FirstName = tbUser.FirstName;
                    Us.Username = tbUser.Username;
                    Us.LastName = tbUser.LastName;
                    Us.Password = tbUser.Password.ToSha256();
                    Us.Email = tbUser.Email;
                    Us.Limit = tbUser.Limit;
                    Us.TelegramID = tbUser.TelegramID;
                    foreach (var item in Us.tbLinkUserAndPlans.ToList())
                    {
                        item.L_Status = false;
                    }

                    foreach (var item in Plans)
                    {
                        var first = Us.tbLinkUserAndPlans.Where(p => p.L_FK_P_ID == Convert.ToInt32(item) && p.L_Status == false).FirstOrDefault();
                        if (first == null)
                        {
                            tbLinkUserAndPlans link = new tbLinkUserAndPlans();
                            link.L_FK_P_ID = Convert.ToInt32(item);
                            link.L_Status = true;
                            Us.tbLinkUserAndPlans.Add(link);
                        }
                        else
                        {
                            first.L_Status = true;
                        }
                    }
                    RepositoryUser.Save();
                    return Content("1");
                }
                else
                {
                    return Content("3");
                }
            }
            catch (Exception ex)
            {
                return Content("3");
            }


        }

        #endregion

        #region شارژ کیف پول کاربر

        public ActionResult _EditWallet(int id)
        {
            var us = db.tbUsers.Where(p => p.User_ID == id).FirstOrDefault();
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
        public ActionResult EditWallet(int id, string wallet)
        {
            var us = db.tbUsers.Where(p => p.User_ID == id).FirstOrDefault();
            if (us != null)
            {
                var intWallet = 0;
                try
                {
                    intWallet = int.Parse(wallet, NumberStyles.Currency);
                }
                catch(Exception ex)
                {
                    return Content("3");
                }

                tbUserFactors factor = new tbUserFactors();
                factor.tbUf_Value = us.Wallet - intWallet;
                factor.tbUf_CreateTime = DateTime.Now;
                factor.FK_User_ID = id;
                us.Wallet = intWallet;
                us.tbUserFactors.Add(factor);
                RepositoryUser.Save();
                return Content("1");
            }
            else
            {
                return Content("2");
            }
        }

        #endregion

        #region ورود

        [System.Web.Mvc.HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// تابع لاگین از سمت پنل ادمین
        /// </summary>
        /// <param name="loginModel"></param>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        public ActionResult Login(tbUsers user)
        {
            try
            {

                tbUsers User = RepositoryUser.table.Where(p => p.Username == user.Username && p.Role == 1).FirstOrDefault();
                if (User != null)
                {

                    var Sha = user.Password.ToSha256();

                    if (User.Password == Sha)
                    {
                        FormsAuthentication.SetAuthCookie(User.Username, false);
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        TempData["Error"] = "نام کاربری یا رمز عبور اشتباه است";
                        return RedirectToAction("Login", "Admin");
                    }

                }
                else
                {
                    TempData["Error"] = "نام کاربری یا رمز عبور اشتباه است";
                    return RedirectToAction("Login", "Admin");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطا در برقراری ارتباط با سرور";
                return RedirectToAction("Login", "Admin");
            }
        }

        #endregion

        #region خروج

        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Admin");
        }

        #endregion

        #region مسدود کردن کاربر

        public ActionResult BanUser(int id)
        {
            try
            {

                var User = RepositoryUser.Where(p => p.User_ID == id).FirstOrDefault();
                if (User != null)
                {
                    if (User.Status.Value)
                    {
                        User.Status = false;
                    }
                    else
                    {
                        User.Status = true;
                    }
                }

                RepositoryUser.Save();
                return Content("1");
            }
            catch (Exception ex)
            {
                return Content("2");
            }
        }

        #endregion

        #region لیست تعرفه ها در قالب Select2

        public ActionResult Select2Plans()
        {
            var Us = db.tbUsers.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

            if (Us != null)
            {
                var Plans = RepositoryPlans.Where(p => p.FK_Server_ID == Us.FK_Server_ID && p.Status == true).ToList();
                return PartialView(Plans);
            }
            else
            {
                return PartialView();
            }
        }

        #endregion

        #region نمایش لاگ ایجاد یا تمدید کاربر عمده 

        public ActionResult GetUserAccountLog(int id)
        {
            var User = RepositoryUser.Where(p => p.User_ID == id).FirstOrDefault();
            var Logs = RepositoryLogs.Where(p => p.tbLinkUserAndPlans.tbUsers.User_ID == id).OrderByDescending(p => p.CreateDatetime.Value).ToList();
            if (Logs != null)
            {

                UserLogViewModel userLogViewModel = new UserLogViewModel();
                userLogViewModel.Logs = Logs;
                var LastPay = User.tbUserFactors.OrderByDescending(p => p.tbUf_CreateTime).FirstOrDefault();
                if (LastPay != null)
                {
                    userLogViewModel.SumSaleFromLastPay = Logs.Where(p => p.CreateDatetime >= LastPay.tbUf_CreateTime).Sum(p => p.SalePrice.Value);
                    userLogViewModel.CountCreatedFormLastPay = Logs.Where(p => p.CreateDatetime >= LastPay.tbUf_CreateTime).Count();
                }
                else
                {
                    userLogViewModel.SumSaleFromLastPay = 0;
                    userLogViewModel.CountCreatedFormLastPay = 0;
                }
                userLogViewModel.SumSale = Logs.Sum(p => p.SalePrice.Value);
                userLogViewModel.CountCreated = Logs.Count();
                ViewBag.Name = User.Username;

                return View(userLogViewModel);
            }
            else
            {
                return View();
            }

        }



        #endregion

        #region فاکتور های پرداخت شده کاربر 

        public ActionResult Factors(int id)
        {

            var User = RepositoryUser.Where(p => p.User_ID == id).FirstOrDefault();
            if (User != null)
            {
                return PartialView(User.tbUserFactors.ToList());
            }
            return PartialView();
        }

        #endregion

        #region حذف اکانت عمده فروش

        public ActionResult DeleteAccountLog(int id)
        {
            try
            {
                var Log = RepositoryLogs.Where(p => p.log_ID == id).FirstOrDefault();
                if (Log != null)
                {
                    MySqlEntities mySqlEntities = new MySqlEntities(Log.tbLinkUserAndPlans.tbUsers.tbServers.ConnectionString);
                    mySqlEntities.Open();
                    var Ended = false;
                    var Reader2 = mySqlEntities.GetData("select * from v2_user where email='" + Log.FK_NameUser_ID + "@" + Log.tbLinkUserAndPlans.tbUsers.Username + "'");
                    if (Reader2.Read())
                    {
                        var d = Reader2.GetDouble("d");
                        var u = Reader2.GetDouble("u");
                        var totalUsed = Reader2.GetDouble("transfer_enable");

                        var total = Math.Round(Utility.ConvertByteToGB(totalUsed - (d + u)), 2);
                        var exp2 = Reader2.GetBodyDefinition("expired_at");
                        
                        if (!string.IsNullOrWhiteSpace(exp2))
                        {
                            var expireTime = DateTime.Now;
                            var ExpireDate = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp2));
                            if (ExpireDate <= expireTime)
                            {
                                Ended = true;
                            }
                        }
                        if (total <= 0)
                        {
                            Ended = true;
                        }
                    }
                    Reader2.Close();


                    var Reader = mySqlEntities.GetData("delete from v2_user where email='" + Log.FK_NameUser_ID + "@" + Log.tbLinkUserAndPlans.tbUsers.Username + "'");
                    if (!Ended)
                    {
                        Log.tbLinkUserAndPlans.tbUsers.Wallet -= Log.tbLinkUserAndPlans.tbPlans.Price;
                    }
                    
                    var Users = RepositoryUser.table.Where(p => p.FK_Server_ID == Log.tbLinkUserAndPlans.tbUsers.FK_Server_ID).OrderByDescending(p => p.User_ID).ToList();
                    RepositoryLogs.Delete(Log.log_ID);
                    RepositoryLogs.Save();
                    return PartialView("_PartialGetAllUsers", Users);
                }
                return Content("2");
            }
            catch (Exception ex)
            {
                return Content("2");
            }

        }
        #endregion


        public ActionResult Pay(string TOKEN)
        {
            if (!string.IsNullOrEmpty(TOKEN))
            {
                var User = RepositoryUser.Where(p => p.Token == TOKEN && p.Status == true).FirstOrDefault();
                if (User != null)
                {
                    var wallet = User.Wallet * 10;
                    if (!User.tbUserFactors.Where(p => p.tbUf_Value == wallet && p.IsPayed == false).Any() && User.Wallet != 0)
                    {
                        tbUserFactors userFactors = new tbUserFactors();
                        userFactors.tbUf_Value = wallet;
                        userFactors.tbUf_CreateTime = DateTime.Now;
                        userFactors.IsPayed = false;
                        User.tbUserFactors.Add(userFactors);
                        RepositoryUser.Save();

                        var Admin = RepositoryUser.Where(p => p.Role == 1 && p.FK_Server_ID == User.FK_Server_ID).FirstOrDefault();

                        if (Admin != null)
                        {
                            ViewBag.CardNumber = Admin.Card_Number;
                            ViewBag.FullName = Admin.FirstName + " " + Admin.LastName;
                        }
                        ViewBag.wallet = wallet;
                        return View(User);
                    }
                    else
                    {
                        var Admin = RepositoryUser.Where(p => p.Role == 1 && p.FK_Server_ID == User.FK_Server_ID).FirstOrDefault();

                        if (Admin != null)
                        {
                            ViewBag.CardNumber = Admin.Card_Number;
                            ViewBag.FullName = Admin.FirstName + " " + Admin.LastName;
                        }
                        ViewBag.wallet = wallet;
                        return View(User);
                    }
                }
                else
                {
                    return RedirectToAction("Error404", "Error");
                }
            }
            else
            {
                return RedirectToAction("Error404", "Error");
            }
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult Pay(tbUsers tbUser)
        {
            var User = RepositoryUser.Where(p => p.Token == tbUser.Token && p.Status == true).FirstOrDefault();
            if (User != null)
            {
                var wallet = (User.Wallet * 10);
                ViewBag.wallet = wallet;
                var Admin = RepositoryUser.Where(p => p.Role == 1 && p.FK_Server_ID == User.FK_Server_ID).FirstOrDefault();

                if (Admin != null)
                {
                    ViewBag.CardNumber = Admin.Card_Number;
                    ViewBag.FullName = Admin.FirstName + " " + Admin.LastName;
                }

                var Factor = User.tbUserFactors.Where(p => p.tbUf_Value == wallet && p.IsPayed == true).Any();
                if (Factor)
                {
                    if (wallet == tbUser.Wallet)
                    {
                        User.Wallet = 0;
                        RepositoryUser.Save();
                        TempData["MessageSuccess"] = "صورتحساب با موفقیت پرداخت شد";
                        return View(tbUser);
                    }
                }
                else
                {
                    TempData["MessageWarning"] = "شما هنوز واریزی انجام نداده اید";
                    return View(tbUser);
                }

                TempData["MessageError"] = "صورتحساب قبلا پرداخت شده";
                return View(tbUser);
            }
            return HttpNotFound();
        }
    }
}
