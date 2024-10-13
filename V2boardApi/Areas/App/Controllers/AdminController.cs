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
using Antlr.Runtime.Misc;
using System.Data.Entity;
using Mysqlx.Expr;
using System.Numerics;
using System.IO.Packaging;
using Stimulsoft.Data.Expressions.Antlr.Runtime.Misc;
using Org.BouncyCastle.Utilities;
using Newtonsoft.Json.Linq;

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
        private Repository<tbBotSettings> RepositoryBotSettings { get; set; }
        private Repository<tbServerGroups> serverGroup_Repo { get; set; }
        private Repository<tbBankCardNumbers> repositoryCard { get; set; }
        private Repository<tbOrders> repositoryOrders { get; set; }
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
            RepositoryBotSettings = new Repository<tbBotSettings>(db);
            serverGroup_Repo = new Repository<tbServerGroups>(db);
            repositoryCard = new Repository<tbBankCardNumbers>(db);
            repositoryOrders = new Repository<tbOrders>(db);
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

        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> _PartialGetAllUsers()

        {
            try
            {
                var Users = await RepositoryUser.WhereAsync(s => s.tbUsers2.Username == User.Identity.Name);
                Users.Add(await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name));
                List<UserViewModel> users = new List<UserViewModel>();
                foreach (var item in Users)
                {
                    UserViewModel user = new UserViewModel();
                    user.id = item.User_ID;
                    user.profile = item.Profile_Filename;
                    user.username = item.Username;
                    user.status = 1;
                    user.sumSellCount = RepositoryLogs.Where(p => p.tbLinkUserAndPlans.tbUsers.User_ID == item.User_ID).Select(s => (int)s.SalePrice).Sum().ConvertToMony() + " تومان";
                    user.sellCount = RepositoryLogs.Where(p => p.tbLinkUserAndPlans.tbUsers.User_ID == item.User_ID).Select(s => (int)s.SalePrice).Count();
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
                    user.used = item.Wallet.ConvertToMony() + " تومان";
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
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در لود لیست نمایندگان");
                return MessageBox.Error("خطا", "خطا در لود نمایندگان");
            }
        }

        #endregion

        #region افزودن و ویرایش کاربر

        #region ویرایش اطلاعات کاربر

        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult Edit(int id)
        {

            var us = new tbUsers();

            us = RepositoryUser.Where(s => s.User_ID == id && s.Username == User.Identity.Name).FirstOrDefault();

            if (us == null)
            {
                us = RepositoryUser.Where(s => s.User_ID == id && s.tbUsers2.Username == User.Identity.Name).FirstOrDefault();
            }
            if (us == null)
            {
                return RedirectToAction("Error404", "Error", new { area = "App" });
            }

            UserRequestModel user = new UserRequestModel();
            user.userId = us.User_ID;
            user.userLimit = us.Limit.Value.ConvertToMony();
            user.userContact = us.PhoneNumber;
            user.userEmail = us.Email;
            user.userFullname = us.FullName;
            user.userTelegramid = us.TelegramID;
            user.userUsername = us.Username;
            var data = user.ToDictionary();


            return Json(new { status = "success", data = data }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ثبت ویرایش و افزودن اطلاعات کاربر

        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateOrEdit(UserRequestModel user)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    var dbUser = new tbUsers();

                    if (user.userId != 0)
                    {
                        dbUser = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user.userId && s.Username == User.Identity.Name);

                        if (dbUser == null)
                        {
                            dbUser = await RepositoryUser.FirstOrDefaultAsync(s => s.tbUsers2.Username == User.Identity.Name);
                        }

                        if (dbUser == null)
                        {
                            return RedirectToAction("Error404", "Error", new { area = "App" });
                        }
                    }

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

                            tbUser.TelegramID = user.userTelegramid;
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
                            tbUser.Parent_ID = CurrentUser.User_ID;
                            tbUser.Register_Date = DateTime.Now;
                            RepositoryUser.Insert(tbUser);
                            await RepositoryUser.SaveChangesAsync();

                            logger.Info("نماینده افزوده شد");
                            return Toaster.Success("موفق", "نماینده با موفقیت افزوده شد");
                        }
                    }
                    else
                    {
                        tbUsers tbUser = await RepositoryUser.FirstOrDefaultAsync(p => p.User_ID == user.userId && p.tbUsers2.Username == User.Identity.Name);

                        if (tbUser == null)
                        {
                            tbUser = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                        }

                        if (tbUser == null)
                        {
                            return RedirectToAction("Error404", "Error", new { area = "App" });
                        }


                        tbUser.FullName = user.userFullname;
                        tbUser.Email = user.userEmail;
                        if (user.userPassword != null)
                        {
                            if (User.Identity.Name == tbUser.Username)
                            {
                                return MessageBox.Warning("هشدار", "شما مجوز تغییر رمز عبور خود را ندارید");
                            }
                            tbUser.Password = user.userPassword.ToSha256();
                            tbUser.Token = (user.userUsername + user.userPassword).ToSha256();

                        }
                        try
                        {
                            var Number = int.Parse(user.userLimit, NumberStyles.Currency);
                            if (tbUser.Limit != Number)
                            {
                                if (User.Identity.Name == tbUser.Username)
                                {
                                    return MessageBox.Warning("هشدار", "شما مجوز تغییر محدودیت خود را ندارید");
                                }
                            }
                            tbUser.Limit = Number;


                        }
                        catch
                        {
                            return MessageBox.Warning("هشدار", "لطفا مبلغ را صحیح وارد کنید", icon: icon.warning);
                        }
                        tbUser.PhoneNumber = user.userContact;



                        if (tbUser.Username != user.userUsername)
                        {
                            var CheckExistsUser = RepositoryUser.Where(p => p.Username == user.userUsername).Any();
                            if (CheckExistsUser)
                            {
                                return MessageBox.Warning("هشدار", "نماینده ای با این نام کاربری وجود دارد", icon: icon.warning);
                            }



                            using (MySqlEntities mysql = new MySqlEntities(tbUser.tbServers.ConnectionString))
                            {
                                await mysql.OpenAsync();

                                var Disc3 = new Dictionary<string, object>();
                                Disc3.Add("@old_email", "@" + tbUser.Username);
                                Disc3.Add("@new_email", "@" + user.userUsername);

                                var Reader = await mysql.GetDataAsync("update v2_user set email=REPLACE(email, @old_email, @new_email)", Disc3);
                                await Reader.ReadAsync();
                            }
                            tbUser.Username = user.userUsername;

                        }

                        await RepositoryUser.SaveChangesAsync();
                        logger.Info("نماینده ویرایش شد");
                        return Toaster.Success("موفق", "نماینده با موفقیت ویرایش شد");
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
                logger.Error(ex, "افزودن یا ویرایش نماینده با خطا مواجه شد");
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
        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult Details(int user_id, string type = "history")
        {
            ViewBag.Type = type;
            return View(user_id);
        }


        #endregion

        #region کارت جزئیات کاربر

        //نمایش پروفایل کاربر
        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult _UserCard(int userid)
        {
            var user = new tbUsers();

            user = RepositoryUser.Where(s => s.User_ID == userid && s.Username == User.Identity.Name).FirstOrDefault();

            if (user == null)
            {
                user = RepositoryUser.Where(s => s.User_ID == userid && s.tbUsers2.Username == User.Identity.Name).FirstOrDefault();
            }

            if (user == null)
            {
                return RedirectToAction("Error404", "Error", new { area = "App" });
            }

            return PartialView(user);
        }

        #endregion

        #endregion

        #region مسدود کردن کاربر
        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult BanUser(int id)
        {
            try
            {
                var user = new tbUsers();

                user = RepositoryUser.Where(s => s.User_ID == id && s.Username == User.Identity.Name).FirstOrDefault();

                if (user == null)
                {
                    user = RepositoryUser.Where(s => s.User_ID == id && s.tbUsers2.Username == User.Identity.Name).FirstOrDefault();
                }
                else
                {
                    return MessageBox.Warning("ناموفق", "شما امکان غیرفعال کردن حساب خود را ندارید !!");
                }
                if (user == null)
                {
                    return RedirectToAction("Error404", "Error", new { area = "App" });
                }
                if (user != null)
                {
                    if (user.Status.Value)
                    {
                        user.Status = false;
                    }
                    else
                    {
                        user.Status = true;
                    }
                }

                RepositoryUser.Save();
                logger.Info("وضعیت کاربر تغییر یافت");
                return Toaster.Success("موفق", "وضعیت نماینده با موفقیت تغییر کرد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "در تغییر وضعیت کاربر خطایی رخ داد");
                return MessageBox.Error("ناموفق", "در تغییر وضعیت نماینده خطایی رخ داد");
            }
        }

        #endregion

        #region انتخاب تعرفه برای نماینده

        #region نمایش
        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> SelectPlan(int user_id)
        {
            try
            {

                var user = new tbUsers();

                user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id && s.Username == User.Identity.Name);

                if (user == null)
                {
                    user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id && s.tbUsers2.Username == User.Identity.Name);
                }
                if (user == null)
                {
                    return RedirectToAction("Error404", "Error", new { area = "App" });
                }

                var plans = user.tbLinkUserAndPlans.Where(p => p.L_Status == true).ToList();

                return Json(new { status = "success", data = plans.Select(s => s.tbPlans.Plan_ID).ToList() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "در نمایش تعرفه ها با خطایی مواجه شدیم ");
                return MessageBox.Error("خطا", "نمایش تعرفه ها با خطا مواجه شد");
            }

        }
        #endregion

        #region ثبت

        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPlan(int user_id, List<int> userPlan)
        {
            try
            {
                var user = new tbUsers();

                user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id && s.Username == User.Identity.Name);

                if (user == null)
                {
                    user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id && s.tbUsers2.Username == User.Identity.Name);
                }
                if (user == null)
                {
                    return RedirectToAction("Error404", "Error", new { area = "App" });
                }
                foreach (var plan in user.tbLinkUserAndPlans)
                {
                    plan.L_Status = false;
                }

                if (userPlan != null)
                {
                    foreach (var plan in userPlan)
                    {
                        var planuser = user.tbLinkUserAndPlans.Where(p => p.L_FK_P_ID == plan && p.L_Status == false).FirstOrDefault();
                        if (planuser != null)
                        {
                            planuser.L_Status = true;
                        }
                        else
                        {
                            tbLinkUserAndPlans link = new tbLinkUserAndPlans();
                            link.L_Status = true;
                            link.L_FK_P_ID = plan;
                            user.tbLinkUserAndPlans.Add(link);
                        }
                    }
                }
                await RepositoryUser.SaveChangesAsync();
                return Toaster.Success("موفق", "تعرفه های نماینده با موفقیت تغییر کرد");

            }
            catch (Exception ex)
            {
                logger.Error(ex, "ثبت تعرفه ها با خطا مواجه شد");
                return MessageBox.Error("خطا", "ثبت تعرفه ها با خطا مواجه شد");
            }



        }

        #endregion

        #endregion

        #endregion

        #region ورود

        [System.Web.Mvc.HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        // کلاس کمکی برای ذخیره داده‌های کاربر
        public class UserData
        {
            public long UserId { get; set; }
            public string Email { get; set; }
            public DateTime ExpirationDate { get; set; }
        }
        /// <summary>
        /// تابع لاگین از سمت پنل ادمین
        /// </summary>
        /// <param name="loginModel"></param>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        public async Task<ActionResult> Login(string userUsername, string userPassword, bool userRemember)
        {
            try
            {
                //RedisConnector redis = new RedisConnector();
                //var key = redis.GetValue(100);
                //var s = new Serializer();
                //var d = (System.Collections.Hashtable)s.Deserialize(key); 
               
                

                var Sha = userPassword.ToSha256();
                tbUsers User = RepositoryUser.table.Where(p => p.Username == userUsername && p.Password == Sha).FirstOrDefault();

                //if (User.Username == "darkbaz")
                //{
                //    try
                //    {
                //        using (MySqlEntities mysql = new MySqlEntities(User.tbServers.ConnectionString))
                //        {
                //            await mysql.OpenAsync();

                //            // لیست برای ذخیره‌سازی داده‌های خوانده‌شده
                //            var userList = new List<UserData>();

                //            // دریافت اطلاعات از جدول v2_user
                //            var reader = await mysql.GetDataAsync("SELECT * FROM `v2_user` where v2_user.id = 1200");

                //            while (await reader.ReadAsync())
                //            {
                //                var exp = reader["expired_at"].ToString();
                //                if (!string.IsNullOrEmpty(exp))
                //                {
                //                    var user_id = reader.GetInt64("id");
                //                    var email = reader.GetString("email");
                //                    var e = Convert.ToInt64(exp);
                //                    var ex = Utility.ConvertSecondToDatetime(e);

                //                    userList.Add(new UserData
                //                    {
                //                        UserId = user_id,
                //                        Email = email,
                //                        ExpirationDate = ex
                //                    });
                //                }
                //            }

                //            reader.Close();

                //            foreach (var user in userList)
                //            {
                //                var DaysLeft = Utility.CalculateLeftDayes(user.ExpirationDate);
                //                var create_date = DateTime.Now.AddDays(-DaysLeft);
                //                var username = "";
                //                var name = "";
                //                try
                //                {
                //                    username = user.Email.Split('@')[1];
                //                    name = user.Email.Split('@')[0];
                //                }
                //                catch (Exception ex)
                //                {
                //                    logger.Error(ex, "121333");
                //                    continue;
                //                }

                //                var UserR = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == username);
                //                var start_date = default(DateTime);
                //                if (UserR != null)
                //                {
                //                    var logs = RepositoryLogs.Where(s => s.tbLinkUserAndPlans.L_FK_U_ID == UserR.User_ID && s.FK_NameUser_ID == name)
                //                                              .OrderByDescending(s => s.CreateDatetime)
                //                                              .FirstOrDefault();

                //                    if (logs != null)
                //                    {
                //                        start_date = logs.CreateDatetime.Value;
                //                    }
                //                }


                //                var Order = repositoryOrders.Where(s => s.AccountName == user.Email && s.OrderStatus == "FINISH")
                //                                            .OrderByDescending(s => s.OrderDate)
                //                                            .FirstOrDefault();
                //                if (Order != null)
                //                {

                //                    if (Order.OrderDate > start_date)
                //                    {
                //                        start_date = Order.OrderDate.Value;
                //                    }
                //                }


                //                if (start_date == default(DateTime))
                //                {
                //                    start_date = create_date;
                //                }

                //                // اجرای کوئری برای دریافت اطلاعات از v2_stat_user و v2_plan
                //                var query2 = $"select sum(d) as Download, sum(u) as Upload from v2_stat_user where user_id={user.UserId} and record_at >= {Utility.ConvertDatetimeToSecond(start_date)}";

                //                var reader2 = await mysql.GetDataAsync(query2);
                //                await reader2.ReadAsync();

                //                // پردازش اطلاعات v2_stat_user
                //                var u = reader2["upload"].ToString();
                //                var d = reader2["Download"].ToString();
                //                long download = string.IsNullOrEmpty(d) ? 0 : Convert.ToInt64(d);
                //                long upload = string.IsNullOrEmpty(u) ? 0 : Convert.ToInt64(u);

                //                reader2.Close();

                //                // به‌روزرسانی مقدار مصرف و موجودی کاربر
                //                var updateQuery = $"UPDATE v2_user SET d = {download}, u = {upload} WHERE id = {user.UserId}";
                //                var reader3 = await mysql.GetDataAsync(updateQuery);
                //                reader3.Close();
                //            }

                //            await mysql.CloseAsync();
                //        }
                //        return MessageBox.Warning("هشدار", "تمام");
                //    }
                //    catch (Exception ex)
                //    {
                //        logger.Error(ex, "خطا در درست کردن");
                //        return MessageBox.Warning("هشدار", "خطا");
                //    }
                //}



                if (User != null)
                {
                    if (!User.Status.Value)
                    {
                        return MessageBox.Warning("هشدار", "حساب کاربری شما غیرفعال شده است");
                    }
                    User.Token = (userUsername + userPassword).ToSha256();
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

                    if (Request.Cookies["Token"] != null)
                    {
                        Response.Cookies["Token"].Value = User.Token;
                    }
                    else
                    {
                        HttpCookie Token = new HttpCookie("Token");

                        Token.Value = User.Token;
                        if (userRemember)
                        {
                            Token.Expires = DateTime.Now.AddDays(7);
                        }

                        Response.Cookies.Add(Token);
                    }


                    FormsAuthentication.SetAuthCookie(User.Username, userRemember);

                    logger.Info("ورود موفق");
                    RepositoryUser.Save();
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
                    return MessageBox.Warning("اشتباه", "نام کاربری یا رمز عبور اشتباه است");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ورود کاربر");
                return MessageBox.Error("خطا", "خطا در برقراری ارتباط با سرور");
            }
        }

        #endregion

        #region خروج
        [System.Web.Mvc.Authorize]
        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            Response.Cookies.Clear();
            logger.Info("خروج موفق");
            return RedirectToAction("Login", "Admin");
        }

        #endregion

        #region نمایش لاگ ایجاد یا تمدید کاربر عمده 
        [AuthorizeApp(Roles = "1,3,4")]
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
                        model.Plan = item.PlanName;
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
        [AuthorizeApp(Roles = "1,3,4")]
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
        [AuthorizeApp(Roles = "1,3,4")]

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


        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditWallet(int user_id, string userDeposit)
        {
            try
            {
                var us = db.tbUsers.Where(p => p.User_ID == user_id).FirstOrDefault();
                var intWallet = 0;
                intWallet = int.Parse(userDeposit, NumberStyles.Currency);


                if (us.Role == 4)
                {
                    tbUserFactors factor = new tbUserFactors();
                    factor.tbUf_Value = us.Wallet - intWallet;
                    factor.tbUf_CreateTime = DateTime.Now;
                    factor.FK_User_ID = user_id;
                    factor.IsPayed = true;
                    us.Wallet = intWallet;
                    us.tbUserFactors.Add(factor);
                }
                else
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

                    var botSetting = await RepositoryBotSettings.FirstOrDefaultAsync(s => s.tbUsers.Username == User.Username);
                    if (botSetting != null)
                    {
                        if (botSetting.Enabled)
                        {
                            message = "ربات با موفقیت خاموش شد";
                            botSetting.Enabled = false;
                        }
                        else
                        {
                            message = "ربات با موفقیت روشن شد";
                            botSetting.Enabled = true;
                        }
                    }
                    await RepositoryBotSettings.SaveChangesAsync();


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

        #region دریافت کیف پول کاربر

        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> GetWallet()
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(p => p.Username == User.Identity.Name);

                if (user.Role == 4 || user.Role == 1)
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

                    var reader3 = await mySql.GetDataAsync("SELECT id FROM `v2_user` WHERE email like '%@" + user.Username + "'");
                    while (await reader3.ReadAsync().ConfigureAwait(false))
                    {
                        Users.Add(reader3.GetInt32("id"));
                    }
                    reader3.Close();

                    var Deb = user.tbUserFactors.Where(s => s.IsPayed == true).OrderByDescending(s => s.tbUf_CreateTime).FirstOrDefault();

                    if (Users.Count > 0)
                    {
                        double Unixtime = 0;
                        if (Deb != null)
                        {
                            Unixtime = Utility.ConvertDatetimeToSecond(Deb.tbUf_CreateTime.Value);
                        }
                        else
                        {
                            Unixtime = Utility.ConvertDatetimeToSecond(user.Register_Date.Value);
                        }
                        string userIdsJoined = string.Join(",", Users);
                        var Query = "SELECT SUM(v2_stat_user.u + v2_stat_user.d) as Used FROM `v2_stat_user` join v2_user on v2_user.id = v2_stat_user.user_id where v2_stat_user.created_at >=" + Unixtime + " and v2_stat_user.user_id IN (" + userIdsJoined + ")";
                        if (user.Role == 1)
                        {
                            Query = "SELECT SUM(v2_stat_user.u + v2_stat_user.d) as Used FROM `v2_stat_user` join v2_user on v2_user.id = v2_stat_user.user_id where v2_stat_user.user_id IN (" + userIdsJoined + ")";
                        }

                        var reader2 = await mySql.GetDataAsync(Query);
                        await reader2.ReadAsync();
                        var Data = reader2.GetBodyDefinition("Used");
                        if (Data != "")
                        {
                            used += Convert.ToInt64(Data);
                        }
                        reader2.Close();
                    }




                    await mySql.CloseAsync();

                    var Useage = Utility.ConvertByteToGB(used);

                    if (user.Role.Value == 1)
                    {

                        return Json(new { status = "success", data = new { Useage = Math.Round(Useage, 2).ConvertToMony() } }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        var userPerMonth = 0;
                        var userPerGig = 0;

                        foreach (var item in user.tbLinkServerGroupWithUsers)
                        {
                            userPerMonth += item.PriceForMonth;
                            userPerGig += item.PriceForGig;
                        }
                        return Json(new { status = "success", data = new { UserPerPrice = Utility.ConvertToMony(Math.Round((Useage * (userPerGig + userPerMonth)))), Useage = Math.Round(Useage, 2).ConvertToMony(), pricePerGig = userPerGig, pricePerMonth = userPerMonth } }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    if (user.Role == 3)
                    {
                        var userPerMonth = 0;
                        var userPerGig = 0;
                        var userPerUser = 0;

                        foreach (var item in user.tbLinkServerGroupWithUsers)
                        {
                            userPerMonth += item.PriceForMonth;
                            userPerGig += item.PriceForGig;
                            userPerUser += item.PriceForUser;
                        }

                        return Json(new { status = "success", data = new { debt = user.Wallet.ConvertToMony(), inventory = (user.Limit - user.Wallet).Value.ConvertToMony(), pricePerGig = userPerGig, pricePerMonth = userPerMonth, pricePerUser = userPerUser } }, JsonRequestBehavior.AllowGet);

                    }
                    else
                    {
                        return Json(new { status = "success", data = new { debt = user.Wallet.ConvertToMony(), inventory = (user.Limit - user.Wallet).Value.ConvertToMony() } }, JsonRequestBehavior.AllowGet);

                    }
                }



            }
            catch (Exception ex)
            {
                return Json(new { status = "error" }, JsonRequestBehavior.AllowGet);
            }



        }

        #endregion

        #region تابع مخرب کنترلر

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
        #endregion

        #region تابع تعیین کننده نماینده کل

        [AuthorizeApp(Roles = "1")]
        [System.Web.Http.HttpPost]
        public async Task<ActionResult> ChangeAgent(int user_id, bool status)
        {
            try
            {
                var User = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);
                if (User != null)
                {
                    if (User.tbLinkServerGroupWithUsers.Count() == 0)
                    {
                        return MessageBox.Warning("هشدار", "لطفا اول وضعیت دسته بندی های کاربر را تعیین کنید");
                    }
                    User.GeneralAgent = status;
                    if (status)
                    {
                        User.Role = 3;
                    }
                    else
                    {
                        User.Role = 2;
                    }
                    await RepositoryUser.SaveChangesAsync();
                    logger.Info("نماینده معمولی به درجه نماینده کل ارتقا یافت");
                    return MessageBox.Success("موفق", "وضعیت نماینده با موفقیت تغییر کرد");
                }
                else
                {
                    return MessageBox.Warning("خطا", "متاسفانه سیستم در پردازش با خطا مواجه شد");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "تغییر وضعیت نماینده کل با خطا مواجه شد");
                return MessageBox.Error("خطا", "متاسفانه درخواست شما با خطا مواجه شد لطفا مجدد تلاش کنید");
            }
        }

        #endregion

        #region تنظیمات نماینده
        [AuthorizeApp(Roles = "1")]
        public ActionResult Settings(int user_id)
        {
            return View();
        }
        #endregion

        #region تنظیمات کلی 
        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> SetGeneralSetting(int user_id, List<int> planGroup)
        {
            var user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);

            var groups = await serverGroup_Repo.GetAllAsync();

            foreach (var item in user.tbLinkServerGroupWithUsers)
            {
                user.tbLinkServerGroupWithUsers.Remove(item);
            }

            foreach (var group in planGroup)
            {

                tbLinkServerGroupWithUsers tbLinkServer = new tbLinkServerGroupWithUsers();
                tbLinkServer.FK_Group_Id = group;
                user.tbLinkServerGroupWithUsers.Add(tbLinkServer);
            }


            await RepositoryUser.SaveChangesAsync();

            logger.Info("ادمین گروه مجوز را تغییر داد");
            return Toaster.Success("موفق", "گروه مجوز با موفقیت ثبت شد");

        }

        #endregion

        #region تنظیمات ربات

        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpGet]
        public ActionResult _GetBotSetting(int user_id)
        {
            var user = RepositoryUser.Where(s => s.User_ID == user_id).FirstOrDefault();
            var botSetting = user.tbBotSettings.FirstOrDefault();
            if (botSetting != null)
            {
                return PartialView(botSetting);
            }
            else
            {
                return PartialView(new tbBotSettings());
            }
        }


        [System.Web.Mvc.HttpPost]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> SaveBotSetting(int id, int user_id, string BotId, string BotToken, long TelegramUserId, string ChannelId, int PricePerMonth_Major, int PricePerGig_Major, int PricePerMonth_Admin, int PricePerGig_Admin, bool? Active, bool? RequiredJoinChannel, bool? IsActiveCardToCard, bool? IsActiveSendReceipt, int userPlan, double? Present_Discount = null)
        {

            try
            {
                var Use = await RepositoryUser.FirstOrDefaultAsync(p => p.User_ID == user_id);

                if (RequiredJoinChannel == true)
                {
                    try
                    {

                        if (string.IsNullOrEmpty(ChannelId))
                        {
                            return MessageBox.Warning("ناموفق", "جهت فعال سازی عضویت اجباری آیدی کانال را وارد کنید");
                        }

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
                                    return MessageBox.Warning("ناموفق", "جهت فعال سازی عضویت اجباری آیدی کانال را درست وارد کنید");

                                    //return MessageBox.Warning("ناموفق","");
                                }
                                if (ex.InnerException.Message.Contains("inaccessible"))
                                {
                                    return MessageBox.Warning("ناموفق", "جهت فعال سازی عضویت اجباری باید ربات را داخل کانال خود ادمین کنید");
                                }
                            }
                            return MessageBox.Warning("ناموفق", "اعتبار سنجی توکن ناموفق لطفا توکن را چک کنید");
                        }
                    }
                    catch (Exception ex)
                    {
                        return MessageBox.Warning("ناموفق", "اعتبار سنجی توکن ناموفق لطفا توکن را چک کنید");
                    }

                }
                else
                {
                    TelegramBotClient bot = new TelegramBotClient(BotToken);
                    try
                    {
                        var res = bot.SendTextMessageAsync(TelegramUserId, "پیغام جهت صحت سنجی اطلاعات ثبت شده در تنظیمات می باشد");
                        var s = res.Result;
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                        {
                            if (ex.InnerException.Message.Contains("not found"))
                            {
                                return MessageBox.Warning("ناموفق", "شناسه تلگرام ادمین اشتباه است لطفا شناسه را از داخل getmyid_bot چک کنید");
                            }
                        }
                        return MessageBox.Warning("ناموفق", "اعتبار سنجی توکن ناموفق لطفا توکن را چک کنید");


                    }

                }

                if (id != 0)
                {
                    var botSettings = Use.tbBotSettings.FirstOrDefault(s => s.botSettingID == id);
                    if (!string.IsNullOrEmpty(ChannelId))
                    {
                        botSettings.ChannelID = ChannelId;
                    }
                    if (Present_Discount != null && Present_Discount != 0)
                    {
                        botSettings.Present_Discount = Present_Discount / 100;
                    }

                    var ress = BotManager.GetBot(Use.Username);
                    if (ress == null)
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

                    if (RequiredJoinChannel == null)
                    {
                        botSettings.RequiredJoinChannel = false;
                    }
                    else
                    {
                        botSettings.RequiredJoinChannel = true;
                    }


                    if (Active == null)
                    {
                        botSettings.Active = false;
                    }
                    else
                    {
                        botSettings.Active = true;
                    }


                    if (IsActiveCardToCard == null)
                    {
                        botSettings.IsActiveCardToCard = false;
                    }
                    else
                    {
                        botSettings.IsActiveCardToCard = true;
                    }

                    if (IsActiveSendReceipt == null)
                    {
                        botSettings.IsActiveSendReceipt = false;
                    }
                    else
                    {
                        botSettings.IsActiveSendReceipt = true;
                    }

                    botSettings.Bot_Token = BotToken;
                    botSettings.Bot_ID = BotId;
                    botSettings.AdminBot_ID = TelegramUserId;
                    botSettings.PricePerMonth_Major = PricePerMonth_Major;
                    botSettings.PricePerGig_Major = PricePerGig_Major;
                    botSettings.PricePerMonth_Admin = PricePerMonth_Admin;
                    botSettings.PricePerGig_Admin = PricePerGig_Admin;
                    botSettings.FK_Plan_ID = userPlan;

                    await RepositoryUser.SaveChangesAsync();

                    logger.Info("تنظیمات ربات ویرایش شد");
                }
                else
                {
                    var botSettings = new tbBotSettings();
                    if (!string.IsNullOrEmpty(ChannelId))
                    {
                        botSettings.ChannelID = ChannelId;
                    }
                    if (Present_Discount != null && Present_Discount != 0)
                    {
                        botSettings.Present_Discount = Present_Discount / 100;
                    }

                    var ress = BotManager.GetBot(Use.Username);
                    if (ress == null)
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

                    if (RequiredJoinChannel == null)
                    {
                        botSettings.RequiredJoinChannel = false;
                    }
                    else
                    {
                        botSettings.RequiredJoinChannel = true;
                    }


                    if (Active == null)
                    {
                        botSettings.Active = false;
                    }
                    else
                    {
                        botSettings.Active = true;
                    }


                    if (IsActiveCardToCard == null)
                    {
                        botSettings.IsActiveCardToCard = false;
                    }
                    else
                    {
                        botSettings.IsActiveCardToCard = true;
                    }

                    if (IsActiveSendReceipt == null)
                    {
                        botSettings.IsActiveSendReceipt = false;
                    }
                    else
                    {
                        botSettings.IsActiveSendReceipt = true;
                    }

                    botSettings.Bot_Token = BotToken;
                    botSettings.Bot_ID = BotId;
                    botSettings.AdminBot_ID = TelegramUserId;
                    botSettings.PricePerMonth_Major = PricePerMonth_Major;
                    botSettings.PricePerGig_Major = PricePerGig_Major;
                    botSettings.PricePerMonth_Admin = PricePerMonth_Admin;
                    botSettings.PricePerGig_Admin = PricePerGig_Admin;
                    botSettings.FK_Plan_ID = userPlan;
                    Use.tbBotSettings.Add(botSettings);

                    await RepositoryUser.SaveChangesAsync();

                    logger.Info("تنظیمات ربات افزوده شد");
                }


                return MessageBox.Success("موفق", "اطلاعات ربات با موفقیت ذخیره شد");

            }
            catch (Exception ex)
            {
                logger.Error(ex, "ذخیره سازی تنظیمات ربات با خطا مواجه شد");

                return MessageBox.Error("موفق", "ذخیره سازی اطلاعات با خطا مواجه شد");
            }


        }


        #endregion

        #region تنظیمات کارت ها


        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1")]
        public ActionResult _GetBankNumbers()
        {
            return PartialView();
        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> GetUserBankNumbers(int user_id)
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(p => p.User_ID == user_id);

                List<BankNumberViewModel> listBank = new List<BankNumberViewModel>();
                foreach (var item in user.tbBankCardNumbers)
                {
                    BankNumberViewModel bankNum = new BankNumberViewModel();
                    bankNum.Card_ID = item.CardNumber_ID;
                    bankNum.phoneNumber = item.phoneNumber;
                    bankNum.CardNumber = item.CardNumber;
                    bankNum.SmsNumberOfCard = item.BankSmsNumber;
                    bankNum.NameOfCard = item.InTheNameOf;
                    bankNum.Status = Convert.ToInt16(item.Active);
                    listBank.Add(bankNum);
                }


                return Json(new { status = "success", data = listBank }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return MessageBox.Error("ناموفق", "خطا در لود لیست کارت ها");
            }
        }


        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> SaveBankCard(int user_id, string CardNumber, string NameOfCard, string SmsNumberOfCard, string phoneNumber, int Card_ID = 0)
        {
            var user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);
            if (Card_ID != 0)
            {
                var Card = user.tbBankCardNumbers.Where(s => s.CardNumber_ID == Card_ID).FirstOrDefault();
                if (Card != null)
                {
                    Card.phoneNumber = phoneNumber;
                    Card.CardNumber = CardNumber;
                    Card.BankSmsNumber = SmsNumberOfCard;
                    Card.InTheNameOf = NameOfCard;

                }
                await RepositoryUser.SaveChangesAsync();
                return Toaster.Success("موفق", "اطلاعات کارت ویرایش شد");
            }
            else
            {
                tbBankCardNumbers card = new tbBankCardNumbers();
                card.phoneNumber = phoneNumber;
                card.CardNumber = CardNumber;
                card.BankSmsNumber = SmsNumberOfCard;
                card.InTheNameOf = NameOfCard;
                if (user.tbBankCardNumbers.Count == 0)
                {
                    card.Active = true;
                }
                user.tbBankCardNumbers.Add(card);
                await RepositoryUser.SaveChangesAsync();
                return Toaster.Success("موفق", "اطلاعات کارت اضافه شد");
            }
        }

        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpGet]
        public async Task<ActionResult> DeActiveCard(int Card_ID, int user_id)
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);

                var Card = user.tbBankCardNumbers.Where(s => s.CardNumber_ID == Card_ID).FirstOrDefault();
                if (Card != null)
                {
                    if (Card.Active)
                    {
                        return MessageBox.Warning("هشدار", "لطفا کارتی که غیرفعال است را برای فعال سازی انتخاب کنید");
                    }
                    else
                    {
                        foreach (var item in user.tbBankCardNumbers)
                        {
                            item.Active = false;
                        }
                        Card.Active = true;
                        await RepositoryUser.SaveChangesAsync();
                        logger.Info("وضعیت کارت با موفقیت تغییر کرد");
                        return Toaster.Success("موفق", "کارت مورد نظر فعال گردید !!");
                    }
                }
                else
                {
                    return MessageBox.Warning("هشدار", "کارت مورد نظر یافت نشد");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در تنظیمات کارت های نمایندگان");
                return MessageBox.Error("ناموفق", "خطا در تنظیمات کارت");
            }
        }


        [System.Web.Mvc.HttpGet]
        public async Task<ActionResult> DeleteCard(int Card_ID, int user_id)
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);


                var Card = user.tbBankCardNumbers.Where(s => s.CardNumber_ID == Card_ID).FirstOrDefault();
                if (Card != null)
                {
                    if (Card.Active == false)
                    {

                        repositoryCard.Delete(Card_ID);

                        await repositoryCard.SaveChangesAsync();
                        logger.Info("شماره کارت نماینده حذف شد");
                        return Toaster.Success("موفق", "کارت حذف گردید !!");

                    }
                    else
                    {
                        return MessageBox.Warning("هشدار", "برای حذف این کارت لطفا کارت دیگری را فعال کنید و سپس این کارت را حذف کنید");
                    }
                }
                else
                {
                    return MessageBox.Warning("هشدار", "کارت مورد نظر یافت نشد");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در حذف کارت");
                return MessageBox.Error("خطا", "خطا در حذف کارت");
            }

        }

        #endregion
    }
}
