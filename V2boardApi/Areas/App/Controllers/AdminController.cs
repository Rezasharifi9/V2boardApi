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
using System.Web;
using System.IO;
using NLog;
using Microsoft.Extensions.Logging;
using V2boardApi.Areas.App.Data.UsersViewModels;
using V2boardApi.Areas.App.Data.RequestModels;
using V2boardApi.Models;

namespace V2boardApi.Areas.App.Controllers
{
    [LogActionFilter]
    public class AdminController : Controller
    {

        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbTelegramUsers> RepositoryTelegramUser { get; set; }
        private Repository<tbDepositWallet_Log> RepositoryDepositLog { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbUserFactors> RepositoryUserFactors { get; set; }
        private Repository<tbLinkUserAndPlans> RepositoryUserPlanLinks { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public AdminController()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryPlans = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryTelegramUser = new Repository<tbTelegramUsers>(db);
            RepositoryDepositLog = new Repository<tbDepositWallet_Log>(db);
            RepositoryUserFactors = new Repository<tbUserFactors>(db);
            RepositoryUserPlanLinks = new Repository<tbLinkUserAndPlans>(db);
        }


        #region تغییر پروفایل
        [System.Web.Mvc.Authorize]
        public ActionResult _Profile()
        {
            var Us = db.tbUsers.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (Us != null)
            {
                return PartialView(Us);
            }
            else
            {
                return PartialView();
            }
        }

        [System.Web.Mvc.Authorize]
        [System.Web.Mvc.HttpPost]
        public ActionResult ChangeProfile(HttpPostedFileBase profile)
        {
            var Us = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (Us != null)
            {
                var ServerPath = Server.MapPath("~/Areas/App/assets/images/faces/");
                Us.Profile_Filename = Us.Username + Path.GetExtension(profile.FileName);
                profile.SaveAs(ServerPath + Us.Username + Path.GetExtension(profile.FileName));
                RepositoryUser.Save();
                logger.Info("پروفایل تغییر کرد");
                return Redirect("~/App/Dashboard");

            }
            else
            {
                return RedirectToAction("index", "dashboard");
            }

        }

        #endregion

        #region نمایندگان

        #region لیست کاربران

        [AuthorizeApp(Roles = "1")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1")]
        public ActionResult _PartialGetAllUsers()
        {
            var Users = RepositoryUser.GetAll().OrderByDescending(p => p.User_ID).ToList();
            List<UserViewModel> users = new List<UserViewModel>();
            foreach (var item in Users)
            {
                UserViewModel user = new UserViewModel();
                user.id = item.User_ID;
                user.profile = item.Profile_Filename;
                user.username = item.Username;
                user.status = 1;
                user.sumSellCount = RepositoryLogs.Where(p => p.tbLinkUserAndPlans.tbUsers.User_ID == item.User_ID).Select(s => s.SalePrice.Value).Sum().ConvertToMony() + " تومان";
                user.sellCount = RepositoryLogs.Where(p => p.tbLinkUserAndPlans.tbUsers.User_ID == item.User_ID).Select(s => s.SalePrice.Value).Count();
                if (item.Wallet >= item.Limit)
                {
                    user.status = 3;
                }
                else
                if (item.Wallet >= (item.Limit - (item.Limit * 0.2)))
                {
                    user.status = 2;
                }
                if (item.Status == false)
                {
                    user.status = 4;
                }
                user.used = item.Wallet.Value.ConvertToMony() + " تومان";
                user.limit = item.Limit.Value.ConvertToMony() + " تومان";
                user.RobotStatus = 0;

                var bot = BotManager.GetBot(user.username);
                if (bot != null)
                {
                    if (bot.Started)
                    {
                        user.RobotStatus = 1;
                    }
                }


                users.Add(user);
            }

            return Json(new { data = users }, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region افزودن و ویرایش کاربر

        #region ویرایش اطلاعات کاربر

        [AuthorizeApp(Roles = "1")]
        public ActionResult Edit(int id)
        {
            var us = db.tbUsers.Where(p => p.User_ID == id).FirstOrDefault();

            UserRequestModel user = new UserRequestModel();
            user.userId = us.User_ID;
            user.userLimit = us.Limit.Value.ConvertToMony();
            user.userContact = us.PhoneNumber;
            user.userEmail = us.Email;
            user.userFullname = us.FullName;
            user.userTelegramid = us.TelegramID;
            user.userUsername = us.Username;
            user.userPlan = us.tbLinkUserAndPlans.Select(p => p.L_FK_P_ID.Value).ToList();
            var data = user.ToDictionary();


            return Json(new { status = "success", data = data }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ثبت ویرایش و افزودن اطلاعات کاربر

        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOrEdit(UserRequestModel user)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    if (user.userId == 0)
                    {
                        if (user.userPassword == null)
                        {
                            return MessageBox.Warning("هشدار", "لطفا رمز عبور را وارد کنید", icon: icon.warning);
                        }
                        var CheckExistsUser = RepositoryUser.Where(p => p.Username == user.userUsername).Any();
                        if (CheckExistsUser)
                        {
                            return MessageBox.Warning("هشدار", "نماینده ای با این نام کاربری وجود دارد", icon: icon.warning);
                        }
                        else
                        {
                            tbUsers tbUser = new tbUsers();
                            tbUser.Username = user.userUsername;
                            tbUser.FullName = user.userFullname;
                            tbUser.Email = user.userEmail;
                            tbUser.Password = user.userPassword.ToSha256();

                            try
                            {
                                var Number = int.Parse(user.userLimit, NumberStyles.Currency);
                                tbUser.Limit = Number;
                            }
                            catch
                            {
                                return MessageBox.Warning("هشدار", "لطفا مبلغ را صحیح وارد کنید", icon: icon.warning);
                            }
                            var CurrentUser = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
                            tbUser.PhoneNumber = user.userContact;
                            tbUser.Token = (user.userUsername + user.userPassword).ToSha256();
                            tbUser.Password = user.userPassword.ToSha256();
                            tbUser.IsRenew = false;
                            tbUser.Status = true;
                            tbUser.Wallet = 0;
                            tbUser.Role = 2;
                            tbUser.FK_Server_ID = CurrentUser.FK_Server_ID;
                            tbUser.tbLinkUserAndPlans = new List<tbLinkUserAndPlans>();
                            foreach (var item in user.userPlan)
                            {
                                tbLinkUserAndPlans plan = new tbLinkUserAndPlans();
                                plan.L_FK_P_ID = Convert.ToInt32(item);
                                plan.L_Status = true;
                                tbUser.tbLinkUserAndPlans.Add(plan);
                            }

                            RepositoryUser.Insert(tbUser);
                            RepositoryUser.Save();

                            logger.Info("نماینده افزوده شد");
                            return MessageBox.Success("موفق", "نماینده با موفقیت افزوده شد");
                        }
                    }
                    else
                    {
                        tbUsers tbUser = RepositoryUser.Where(p => p.User_ID == user.userId).FirstOrDefault();
                        tbUser.Username = user.userUsername;
                        tbUser.FullName = user.userFullname;
                        tbUser.Email = user.userEmail;
                        if (user.userPassword != null)
                        {
                            tbUser.Password = user.userPassword.ToSha256();
                        }
                        try
                        {
                            var Number = int.Parse(user.userLimit, NumberStyles.Currency);
                            tbUser.Limit = Number;
                        }
                        catch
                        {
                            return MessageBox.Warning("هشدار", "لطفا مبلغ را صحیح وارد کنید", icon: icon.warning);
                        }
                        tbUser.PhoneNumber = user.userContact;
                        tbUser.Token = (user.userUsername + user.userPassword).ToSha256();

                        foreach (var item in tbUser.tbLinkUserAndPlans.ToList())
                        {
                            item.L_Status = false;
                        }

                        foreach (var item in user.userPlan)
                        {
                            var first = tbUser.tbLinkUserAndPlans.Where(p => p.L_FK_P_ID == Convert.ToInt32(item) && p.L_Status == false).FirstOrDefault();
                            if (first == null)
                            {
                                tbLinkUserAndPlans link = new tbLinkUserAndPlans();
                                link.L_FK_P_ID = Convert.ToInt32(item);
                                link.L_Status = true;
                                tbUser.tbLinkUserAndPlans.Add(link);
                            }
                            else
                            {
                                first.L_Status = true;
                            }
                        }


                        RepositoryUser.Save();
                        logger.Info("نماینده ویرایش شد");
                        return MessageBox.Success("موفق", "نماینده با موفقیت ویرایش شد");
                    }
                }
                else
                {
                    var errors = ModelState.GetError();
                    return MessageBox.Warning("هشدار", errors, icon: icon.warning);
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, "افزودن نماینده جدید با خطا مواجه شد");
                return MessageBox.Warning("ناموفق", "ثبت نماینده با خطا مواجه شد", icon: icon.error);
            }
        }

        #endregion

        #endregion

        #region نمایش جزئیات کاربر

        #region صفحه جزئیات کاربر

        /// <summary>
        /// جزئیات کاربر انتخاب شده
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AuthorizeApp(Roles = "1")]
        public ActionResult Details(int user_id, string type = "history")
        {
            ViewBag.Type = type;
            return View(user_id);
        }


        #endregion

        #region کارت جزئیات کاربر

        //نمایش پروفایل کاربر
        [AuthorizeApp(Roles = "1")]
        public ActionResult _UserCard(int userid)
        {
            var User = RepositoryUser.Where(p => p.User_ID == userid).FirstOrDefault();
            return PartialView(User);
        }

        #endregion

        #endregion

        #region مسدود کردن کاربر
        //[AuthorizeApp(Roles = "1")]
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
                logger.Info("وضعیت کاربر تغییر یافت");
                return Json(new { result = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "در تغییر وضعیت کاربر خطایی رخ داد");
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

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
        public ActionResult Login(string userUsername, string userPassword, bool userRemember)
        {
            try
            {
                var Sha = userPassword.ToSha256();
                tbUsers User = RepositoryUser.table.Where(p => p.Username == userUsername && p.Password == Sha).FirstOrDefault();
                if (User != null)
                {
                    if (Request.Cookies["Role"] != null)
                    {
                        Response.Cookies["Role"].Value = User.Role.Value.ToString();
                    }
                    else
                    {
                        HttpCookie cookie = new HttpCookie("Role");
                        cookie.Value = User.Role.Value.ToString();
                        if (userRemember)
                        {
                            cookie.Expires = DateTime.Now.AddDays(7);
                        }

                        Response.Cookies.Add(cookie);
                    }


                    if (userRemember)
                    {
                        FormsAuthentication.SetAuthCookie(User.Username, true);
                    }
                    else
                    {
                        FormsAuthentication.SetAuthCookie(User.Username, false);
                    }

                    logger.Info("ورود موفق");

                    if (User.Role == 1)
                    {
                        var URL = Url.Action("Index", "Admin");
                        return Json(new { status = "success", redirectURL = URL });

                    }
                    else
                    {
                        var URL = Url.Action("Index", "Subscriptions");
                        return Json(new { status = "success", redirectURL = URL });
                    }

                }
                else
                {
                    logger.Warn("ورود ناموفق");
                    TempData["Error"] = "نام کاربری یا رمز عبور اشتباه است";
                    return MessageBox.Warning("اشتباه", "نام کاربری یا رمز عبور اشتباه است");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ورود کاربر");
                TempData["Error"] = "خطا در برقراری ارتباط با سرور";
                return MessageBox.Error("خطا", "خطا در برقراری ارتباط با سرور");
            }
        }

        #endregion

        #region خروج
        [System.Web.Mvc.Authorize]
        public ActionResult LogOut()
        {
            Response.Cookies.Clear();
            FormsAuthentication.SignOut();
            logger.Info("خروج موفق");
            return RedirectToAction("Login", "Admin");
        }

        #endregion

        #region لیست تعرفه ها در قالب Select2
        //[AuthorizeApp(Roles = "1")]
        public ActionResult Select2Plans()
        {

            var Plans = RepositoryPlans.Where(s => s.Status == true).Select(p => new { id = p.Plan_ID, Name = p.Plan_Name }).ToList();
            return Json(new { result = Plans }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Select2UserPlans()
        {
            var UserLink = RepositoryUserPlanLinks.Where(p => p.tbUsers.Username == User.Identity.Name && p.tbPlans.Status == true).ToList();
            var Plans = UserLink.Select(p => new { id = p.tbPlans.Plan_ID, Name = p.tbPlans.Plan_Name }).ToList();
            return Json(new { result = Plans }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region نمایش لاگ ایجاد یا تمدید کاربر عمده 
        [AuthorizeApp(Roles = "1")]
        public ActionResult GetUserAccountLog(int user_id)
        {
            try
            {
                var user = RepositoryUser.Where(p => p.User_ID == user_id).FirstOrDefault();
                var Logs = RepositoryLogs.Where(p => p.tbLinkUserAndPlans.tbUsers.User_ID == user_id).OrderByDescending(p => p.CreateDatetime.Value).ToList();
                if (Logs != null)
                {
                    List<UserLogResponseModel> logs = new List<UserLogResponseModel>();
                    foreach (var item in Logs)
                    {
                        UserLogResponseModel model = new UserLogResponseModel();
                        model.id = item.log_ID;
                        model.SubName = item.FK_NameUser_ID.Split('@')[0];

                        if (model.SubName.Length > 20)
                        {
                            model.SubName = model.SubName.Substring(0, 10);
                        }

                        model.Event = item.Action;
                        model.CreateDate = item.CreateDatetime.Value.ConvertDateTimeToShamsi2();
                        model.SellPrice = item.SalePrice.Value.ConvertToMony();
                        model.Plan = item.tbLinkUserAndPlans.tbPlans.Plan_Name;
                        logs.Add(model);
                    }

                    return Json(new { data = logs }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "در نمایش تاریخچه ساخت کاربر با خطایی مواجه شدیم !!");
                return View();
            }
        }



        #endregion

        #region فاکتور های پرداخت شده کاربر 
        [AuthorizeApp(Roles = "1")]
        public ActionResult Factors(int user_id)
        {

            var User = RepositoryUser.Where(p => p.User_ID == user_id).FirstOrDefault();
            if (User != null)
            {
                var Factors = User.tbUserFactors.Where(p => p.IsPayed == true).ToList();
                List<UserFactorResponseModel> Factores = new List<UserFactorResponseModel>();
                foreach (var item in Factors)
                {
                    UserFactorResponseModel factor = new UserFactorResponseModel();
                    factor.PayDate = item.tbUf_CreateTime.Value.ConvertDateTimeToShamsi2();
                    factor.Price = item.tbUf_Value.Value.ConvertToMony();
                    Factores.Add(factor);
                }

                return Json(new { data = Factores }, JsonRequestBehavior.AllowGet);
            }
            return PartialView();
        }

        #endregion

        #region شارژ کیف پول کاربر
        [AuthorizeApp(Roles = "1")]

        public ActionResult _EditWallet(int user_id)
        {
            var us = db.tbUsers.Where(p => p.User_ID == user_id).FirstOrDefault();
            if (us != null)
            {
                return PartialView(us);
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }
        }


        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditWallet(int user_id, string userDeposit)
        {
            try
            {
                var us = db.tbUsers.Where(p => p.User_ID == user_id).FirstOrDefault();
                var intWallet = 0;
                intWallet = int.Parse(userDeposit, NumberStyles.Currency);

                if (us.Wallet != intWallet)
                {
                    tbUserFactors factor = new tbUserFactors();
                    factor.tbUf_Value = us.Wallet - intWallet;
                    factor.tbUf_CreateTime = DateTime.Now;
                    factor.FK_User_ID = user_id;
                    factor.IsPayed = true;
                    us.Wallet = intWallet;
                    us.tbUserFactors.Add(factor);
                }
                RepositoryUser.Save();
                return MessageBox.Success("موفق", "اطلاعات کیف پول با موفقیت تغییر کرد");
            }
            catch (Exception ex)
            {
                return MessageBox.Warning("هشدار", "لطفا مبلغ را صحیح وارد کنید", icon: icon.warning);
            }
        }
        #endregion

        #region روشن کردن ربات عمده فروش
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> StartBot(int user_id)
        {
            var User = await RepositoryUser.FirstOrDefaultAsync(p => p.User_ID == user_id);
            try
            {
                if (User != null)
                {
                    var Bot = BotManager.GetBot(User.Username);

                    if (Bot == null)
                    {
                        return MessageBox.Warning("هشدار", "تنظیمات ربات برای این کاربر انجام نشده است !!");
                    }

                    BotService service = new BotService();

                    try
                    {
                        await service.Register(User.Username);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "راه اندازی ربات با خطا مواجه شد");
                    }

                    var message = "";

                    if (Bot != null)
                    {
                        if (Bot.Started)
                        {
                            BotManager.StopBot(User.Username);
                            message = "ربات " + User.Username + " با موفقیت خاموش شد";
                        }
                        else
                        {
                            BotManager.StartBot(User.Username);
                            message = "ربات " + User.Username + " با موفقیت روشن شد";
                        }
                    }


                    logger.Info(message);
                    return MessageBox.Success("موفق", message);
                }
                return MessageBox.Error("هشدار", "ربات راه اندازی نشد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "راه اندازی ربات " + User.Username + " با خطا مواجه شد");
                return MessageBox.Error("هشدار", "راه اندازی ربات با خطا مواجه شد");
            }
        }

        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> StartBots()
        {
            var User = RepositoryUser.Where(p => p.tbBotSettings.Where(s => s.Bot_Token != null).Any()).ToList();

            try
            {
                foreach (var item in User)
                {
                    if (User != null)
                    {
                        var Bot = BotManager.GetBot(item.Username);

                        if (Bot == null)
                        {
                            return Content("warning-" + "تنظیمات ربات برای این کاربر انجام نشده است !!");
                        }

                        BotService service = new BotService();

                        try
                        {
                            try
                            {
                                await service.Register(item.Username);
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, "راه اندازی ربات با خطا مواجه شد");
                            }

                            if (Bot != null)
                            {
                                if (Bot.Started)
                                {
                                    BotManager.StopBot(item.Username);
                                }
                                else
                                {
                                    BotManager.StartBot(item.Username);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "ربات " + item.Username + " با خطا مواجه شد");
                        }



                    }
                }

                return Content("success-" + "ربات ها راه اندازی شدند");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "راه اندازی ربات ها با خطا مواجه شد");
                return Content("error-" + "راه اندازی ربات با خطا مواجه شد");

            }
        }


        #endregion

        [AuthorizeApp(Roles = "1,2,3")]
        public async Task<ActionResult> GetWallet()
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(p => p.Username == User.Identity.Name);

                if (user.Role == 3 || user.Role == 1)
                {
                    Int64 used = 0;

                    MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);

                    List<int> Users = new List<int>();
                    await mySql.OpenAsync();
                    foreach (var item in user.tbUsers1)
                    {
                        var reader = await mySql.GetDataAsync("SELECT id FROM `v2_user` WHERE email like '%@" + item.Username + "'");
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            Users.Add(reader.GetInt32("id"));
                        }
                        reader.Close();
                    }
                    string userIdsJoined = string.Join(",", Users);
                    var Query = "SELECT SUM(v2_stat_user.u + v2_stat_user.d) as Used FROM `v2_stat_user` join v2_user on v2_user.id = v2_stat_user.user_id where v2_stat_user.user_id IN (" + userIdsJoined + ")";
                    var reader2 = await mySql.GetDataAsync(Query);
                    await reader2.ReadAsync();
                    var Data = reader2.GetBodyDefinition("Used");
                    if (Data != "")
                    {
                        used += Convert.ToInt64(Data);
                    }

                    reader2.Close();


                    await mySql.CloseAsync();

                    var UserPerGig = Utility.ConvertByteToGB(used);
                    
                    if(user.Role.Value == 1)
                    {
                        return Json(new { status = "success", data = new { UserPerGig = Math.Round(UserPerGig, 2).ConvertToMony() } }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        var UserPerPrice = UserPerGig * user.PriceForGig;
                        return Json(new { status = "success", data = new { UserPerGig = Math.Round(UserPerGig, 2).ConvertToMony(), UserPerPrice = Math.Round(UserPerPrice.Value, 0).ConvertToMony() } }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    return Json(new { status = "success", data = new { debt = user.Wallet.Value.ConvertToMony(), inventory = (user.Limit - user.Wallet).Value.ConvertToMony() } }, JsonRequestBehavior.AllowGet);
                }



            }
            catch (Exception ex)
            {
                return Json(new { status = "error" }, JsonRequestBehavior.AllowGet);
            }



        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                RepositoryUser.Dispose();
                RepositoryPlans.Dispose();
                RepositoryLogs.Dispose();
                RepositoryServer.Dispose();
                RepositoryTelegramUser.Dispose();
                RepositoryDepositLog.Dispose();
                RepositoryUserFactors.Dispose();
                RepositoryUserPlanLinks.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
