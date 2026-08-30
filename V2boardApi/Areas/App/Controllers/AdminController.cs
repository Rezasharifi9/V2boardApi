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
using Org.BouncyCastle.Utilities;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Security.Claims;

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

        public ActionResult UserProfile()
        {
            var Us = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (Us != null)
            {
                return RedirectToAction("Details", new { type = "history", user_id = Us.User_ID });
            }
            return Content("NotFound");
        }

        #region تغییر پروفایل
        [System.Web.Mvc.Authorize]
        public ActionResult _Profile()
        {
            var Us = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
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
            var currentUser = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            ViewBag.UserRole = currentUser?.Role?.ToString() ?? string.Empty;
            if (currentUser != null && currentUser.Role == 1)
            {
                ViewBag.HeadAgents = RepositoryUser
                    .Where(s => s.Role == 3 && s.Parent_ID == currentUser.User_ID)
                    .OrderBy(s => s.Username)
                    .Select(s => new SelectUserViewModel { id = s.User_ID, username = s.Username })
                    .ToList();
            }
            return View();
        }

        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> _PartialGetAllUsers()
        {
            try
            {
                var adminUser = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                List<tbUsers> usersList;

                int filterParentId = 0;
                int.TryParse(Request["filterParentId"], out filterParentId);

                if (adminUser != null && adminUser.Role == 1 && filterParentId > 0)
                {
                    var headAgent = await RepositoryUser.FirstOrDefaultAsync(s =>
                        s.User_ID == filterParentId
                        && s.Role == 3
                        && s.Parent_ID == adminUser.User_ID);
                    usersList = headAgent != null
                        ? await RepositoryUser.WhereAsync(s => s.Parent_ID == headAgent.User_ID)
                        : new List<tbUsers>();
                }
                else
                {
                    usersList = await RepositoryUser.WhereAsync(s => s.tbUsers2.Username == User.Identity.Name);
                    if (adminUser != null && usersList.All(u => u == null || u.User_ID != adminUser.User_ID))
                        usersList.Add(adminUser);
                }

                usersList = usersList
                    .Where(u => u != null && !string.IsNullOrWhiteSpace(u.Username))
                    .OrderBy(GetAgentLimitSortPriority)
                    .ThenByDescending(u =>
                    {
                        var limit = u.Limit ?? 0;
                        return limit > 0 ? u.Wallet / limit : 0;
                    })
                    .ThenBy(u => u.Username)
                    .ToList();

                var agentIds = usersList.Select(u => u.User_ID).ToList();
                var allowedActions = new[]
                {
                    ReservedPackageHelper.CreatedLogAction,
                    ReservedPackageHelper.EditedLogAction,
                    ReservedPackageHelper.ReserveLogAction
                };
                var deletedPrefix = SubscriptionLogHelper.DeletedNamePrefix;

                var salesByAgent = db.tbLogs
                    .Where(l => l.tbLinkUserAndPlans != null
                        && l.tbLinkUserAndPlans.L_FK_U_ID != null
                        && agentIds.Contains(l.tbLinkUserAndPlans.L_FK_U_ID.Value)
                        && allowedActions.Contains(l.Action)
                        && (l.FK_NameUser_ID == null || !l.FK_NameUser_ID.StartsWith(deletedPrefix)))
                    .GroupBy(l => l.tbLinkUserAndPlans.L_FK_U_ID.Value)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        SellCount = g.Count(),
                        SumSell = g.Sum(x => x.SalePrice ?? 0)
                    })
                    .ToList()
                    .ToDictionary(x => x.UserId);

                var lastPayments = db.tbUserFactors
                    .Where(f => f.FK_User_ID != null
                        && agentIds.Contains(f.FK_User_ID.Value)
                        && f.tbUf_Status == 3
                        && f.tbUf_CreateTime.HasValue)
                    .GroupBy(f => f.FK_User_ID.Value)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        LastPaid = g.Max(x => x.tbUf_CreateTime)
                    })
                    .ToList()
                    .ToDictionary(x => x.UserId, x => x.LastPaid.Value);

                var telegramUserIds = new HashSet<int>();
                var telegramKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tel in db.tbTelegramUsers.Select(t => new { t.Tel_UserID, t.Tel_Username, t.Tel_UniqUserID }).ToList())
                {
                    telegramUserIds.Add(tel.Tel_UserID);

                    var usernameKey = TelegramNotifyHelper.NormalizeTelegramIdentity(tel.Tel_Username);
                    if (usernameKey != null)
                        telegramKeys.Add(usernameKey);

                    var uniqKey = TelegramNotifyHelper.NormalizeTelegramIdentity(tel.Tel_UniqUserID);
                    if (uniqKey != null)
                        telegramKeys.Add(uniqKey);
                }

                var today = DateTime.Now.Date;
                var result = new List<UserViewModel>();
                foreach (var item in usersList)
                {
                    var limit = item.Limit ?? 0;
                    salesByAgent.TryGetValue(item.User_ID, out var sales);
                    lastPayments.TryGetValue(item.User_ID, out var lastPaid);

                    DateTime? unpaidSince = lastPaid != default(DateTime)
                        ? lastPaid
                        : (item.Register_Date ?? item.Settlement_StartDate);

                    var user = new UserViewModel
                    {
                        id = item.User_ID,
                        profile = item.Profile_Filename,
                        username = item.Username,
                        fullName = item.FullName,
                        role = item.Role ?? 0,
                        parentId = item.Parent_ID ?? 0,
                        parentUsername = item.tbUsers2 != null ? item.tbUsers2.Username : "",
                        status = 1,
                        sortPriority = GetAgentLimitSortPriority(item),
                        sellCount = sales?.SellCount ?? 0,
                        sumSellCount = ((long)(sales?.SumSell ?? 0)).ConvertToMony() + " تومان",
                        walletValue = item.Wallet,
                        used = item.Wallet.ConvertToMony() + " تومان",
                        limit = limit.ConvertToMony() + " تومان",
                        RobotStatus = 0,
                        telegramActive = IsAgentTelegramActive(item, telegramUserIds, telegramKeys),
                        isBlocked = item.Settlement_IsBlocked,
                        lastPaymentDate = lastPaid != default(DateTime) ? lastPaid.ConvertDateTimeToShamsi5() : "",
                        lastPaymentSort = lastPaid != default(DateTime)
                            ? (long)(lastPaid.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds
                            : 0,
                        daysUnpaid = unpaidSince.HasValue ? Math.Max(0, (today - unpaidSince.Value.Date).Days) : 0
                    };

                    if (limit > 0)
                    {
                        if (item.Wallet >= limit)
                            user.status = 3;
                        else if (item.Wallet >= (limit - (limit * 0.2)))
                            user.status = 2;
                    }

                    if (item.Status == false)
                        user.status = 4;

                    var bot = BotManager.GetBot(user.username);
                    if (bot != null && bot.Started)
                        user.RobotStatus = 1;

                    result.Add(user);
                }

                return Json(new { data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در لود لیست نمایندگان");
                return Json(new { data = new List<UserViewModel>() }, JsonRequestBehavior.AllowGet);
            }
        }

        private static int GetAgentLimitSortPriority(tbUsers agent)
        {
            var limit = agent.Limit ?? 0;
            if (limit <= 0)
                return 2;

            if (agent.Wallet >= limit)
                return 0;

            if (agent.Wallet >= limit * 0.8)
                return 1;

            return 2;
        }

        private static bool IsAgentTelegramActive(tbUsers agent, HashSet<int> telegramUserIds, HashSet<string> telegramKeys)
        {
            if (agent == null)
                return false;

            if (agent.Admin_Telegram_ID.HasValue && telegramUserIds != null && telegramUserIds.Contains(agent.Admin_Telegram_ID.Value))
                return true;

            var identity = TelegramNotifyHelper.NormalizeTelegramIdentity(agent.TelegramID);
            return identity != null && telegramKeys != null && telegramKeys.Contains(identity);
        }

        private async Task<tbUsers> FindNetworkActionAgentAsync(int userId)
        {
            var current = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
            if (current == null || current.User_ID == userId)
                return null;

            var agent = await FindEditableUserAsync(userId);
            if (agent == null || agent.Role == 1)
                return null;

            if (agent.tbServers == null)
                await db.Entry(agent).Reference(u => u.tbServers).LoadAsync();

            return agent;
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
                var current = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
                if (current != null && current.Role == 1)
                    us = RepositoryUser.Where(s => s.User_ID == id).FirstOrDefault();
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

        private async Task<tbUsers> FindEditableUserAsync(int userId)
        {
            var tbUser = await RepositoryUser.FirstOrDefaultAsync(p => p.User_ID == userId && p.Username == User.Identity.Name);
            if (tbUser != null)
                return tbUser;

            tbUser = await RepositoryUser.FirstOrDefaultAsync(p => p.User_ID == userId && p.tbUsers2.Username == User.Identity.Name);
            if (tbUser != null)
                return tbUser;

            var current = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
            if (current != null && current.Role == 1)
                return await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == userId);

            return null;
        }

        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateOrEdit(UserRequestModel user)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    var dbUser = new tbUsers();

                    if (user.userId != 0)
                    {
                        dbUser = await FindEditableUserAsync(user.userId);

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

                            tbUser.TelegramID = TelegramNotifyHelper.NormalizeTelegramIdentity(user.userTelegramid);
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
                        tbUsers tbUser = await FindEditableUserAsync(user.userId);

                        if (tbUser == null)
                        {
                            return RedirectToAction("Error404", "Error", new { area = "App" });
                        }


                        tbUser.FullName = user.userFullname;
                        tbUser.Email = user.userEmail;
                        if (user.userPassword != null)
                        {
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
                        tbUser.TelegramID = TelegramNotifyHelper.NormalizeTelegramIdentity(user.userTelegramid);


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
        //[AuthorizeApp(Roles = "1,3,4")]
        public ActionResult Details(int? user_id, string type = "history")
        {

            var UserData = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            if (UserData != null)
            {
                if(UserData.Role == 2)
                {
                    ViewBag.Type = type;
                    return View(UserData.User_ID);
                }
                else if(UserData.Role == 3)
                {
                    if(UserData.User_ID == user_id)
                    {
                        ViewBag.Type = type;
                        return View(user_id);
                    }
                    if (user_id != null)
                    {
                        var Exists = UserData.tbUsers1.Where(a => a.User_ID == user_id).FirstOrDefault();
                        if (Exists != null)
                        {
                            ViewBag.Type = type;
                            return View(user_id);
                        }
                    }
                    else
                    {
                        ViewBag.Type = type;
                        return View(user_id);
                    }
                }
                else if (UserData.Role == 1)
                {
                    ViewBag.Type = type;
                    return View(user_id);
                }
            }

            return Content("NotFound");
        }


        #endregion

        #region کارت جزئیات کاربر

        //نمایش پروفایل کاربر
        //[AuthorizeApp(Roles = "1,3,4")]
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
                var current = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
                if (current != null && current.Role == 1)
                    user = RepositoryUser.Where(s => s.User_ID == userid).FirstOrDefault();
            }

            if (user == null)
            {
                return RedirectToAction("Error404", "Error", new { area = "App" });
            }

            // موعد تسویه بعدی فقط وقتی نمایش داده می‌شود که تسویه بدهی برای این نماینده تنظیم شده باشد
            ViewBag.SettlementDueDate = null;
            var settlementDue = SettlementService.GetNextDueDate(db, user);
            if (settlementDue.HasValue)
            {
                ViewBag.SettlementDueDate = settlementDue.Value.ConvertDateTimeToShamsi5();
                ViewBag.SettlementRemainingDays = (settlementDue.Value.Date - DateTime.Now.Date).Days;
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
                    var current = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
                    if (current != null && current.Role == 1)
                        user = RepositoryUser.Where(s => s.User_ID == id).FirstOrDefault();
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

        #region پیام و مسدودسازی اشتراک نماینده

        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> SendAgentMessage(int id, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                    return MessageBox.Warning("ناموفق", "متن پیام را وارد کنید");

                var current = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                var agent = await FindNetworkActionAgentAsync(id);
                if (current == null || agent == null)
                    return MessageBox.Warning("ناموفق", "امکان ارسال پیام برای این نماینده وجود ندارد");

                var text = message.Trim();
                PanelNotificationService.Create(
                    db,
                    agent.User_ID,
                    current.User_ID,
                    PanelNotificationService.TitleAdminMessage,
                    text,
                    14);
                await db.SaveChangesAsync();

                var telegramSent = await SettlementService.SendAgentTelegramMessage(
                    agent, text, mirrorToPanel: false, panelTitle: PanelNotificationService.TitleAdminMessage);

                logger.Info("پیام مدیریت برای نماینده " + agent.Username + " ارسال شد. تلگرام=" + telegramSent);
                if (telegramSent)
                    return Toaster.Success("موفق", "پیام در پنل و ربات ارسال شد");

                return Toaster.Warning("ارسال ناقص", "پیام در پنل ثبت شد اما ارسال به ربات انجام نشد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ارسال پیام به نماینده");
                return MessageBox.Error("ناموفق", "ارسال پیام با خطا مواجه شد");
            }
        }

        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> BlockAgentSubscriptions(int id)
        {
            try
            {
                var current = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                var agent = await FindNetworkActionAgentAsync(id);
                if (current == null || agent == null)
                    return MessageBox.Warning("ناموفق", "امکان مسدودسازی این نماینده وجود ندارد");

                if (agent.tbServers == null || string.IsNullOrEmpty(agent.tbServers.ConnectionString))
                    return MessageBox.Warning("ناموفق", "سرور این نماینده تنظیم نشده است");

                await SettlementService.BlockAgentSubscriptions(agent, db);
                agent.Settlement_IsBlocked = true;
                await db.SaveChangesAsync();

                var text = "تمامی اشتراک‌های زیرمجموعه شما توسط مدیریت مسدود گردید.";
                PanelNotificationService.Create(
                    db, agent.User_ID, current.User_ID, PanelNotificationService.TitleBlocked, text);
                await db.SaveChangesAsync();

                await SettlementService.SendAgentTelegramMessage(
                    agent,
                    "🚫 نماینده گرامی" + Environment.NewLine + Environment.NewLine + text,
                    mirrorToPanel: false,
                    panelTitle: PanelNotificationService.TitleBlocked);

                logger.Info("اشتراک‌های نماینده " + agent.Username + " توسط " + current.Username + " مسدود شد");
                return Toaster.Success("موفق", "اشتراک‌های زیرمجموعه این نماینده مسدود شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در مسدودسازی اشتراک‌های نماینده");
                return MessageBox.Error("ناموفق", "مسدودسازی اشتراک‌ها با خطا مواجه شد");
            }
        }

        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> UnblockAgentSubscriptions(int id)
        {
            try
            {
                var current = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                var agent = await FindNetworkActionAgentAsync(id);
                if (current == null || agent == null)
                    return MessageBox.Warning("ناموفق", "امکان رفع مسدودسازی این نماینده وجود ندارد");

                if (agent.tbServers == null || string.IsNullOrEmpty(agent.tbServers.ConnectionString))
                    return MessageBox.Warning("ناموفق", "سرور این نماینده تنظیم نشده است");

                await SettlementService.UnblockAgentSubscriptions(agent, db);
                agent.Settlement_IsBlocked = false;
                await db.SaveChangesAsync();

                var text = "مسدودسازی اشتراک‌های زیرمجموعه شما توسط مدیریت برداشته شد.";
                PanelNotificationService.Create(
                    db, agent.User_ID, current.User_ID, PanelNotificationService.TitleUnblocked, text);
                await db.SaveChangesAsync();

                await SettlementService.SendAgentTelegramMessage(
                    agent,
                    "✅ نماینده گرامی" + Environment.NewLine + Environment.NewLine + text,
                    mirrorToPanel: false,
                    panelTitle: PanelNotificationService.TitleUnblocked);

                logger.Info("مسدودسازی اشتراک‌های نماینده " + agent.Username + " توسط " + current.Username + " برداشته شد");
                return Toaster.Success("موفق", "مسدودسازی اشتراک‌های این نماینده برداشته شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در رفع مسدودسازی اشتراک‌های نماینده");
                return MessageBox.Error("ناموفق", "رفع مسدودسازی با خطا مواجه شد");
            }
        }

        #endregion

        #region حذف نماینده

        private tbUsers ResolveManagedAgent(int id)
        {
            var current = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            if (current == null || current.Role != 1 || current.User_ID == id)
                return null;

            var agent = RepositoryUser.Where(s => s.User_ID == id).FirstOrDefault();
            if (agent == null || agent.Role == 1)
                return null;

            if (agent.tbUsers2 != null && agent.tbUsers2.Username == User.Identity.Name)
                return agent;

            return null;
        }

        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpGet]
        public ActionResult GetDeleteAgentPreview(int id)
        {
            try
            {
                var agent = ResolveManagedAgent(id);
                if (agent == null)
                    return Json(new { status = "error", message = "نماینده یافت نشد یا مجوز حذف ندارید." }, JsonRequestBehavior.AllowGet);

                var preview = AgentDeleteService.BuildPreview(db, agent);
                return Json(new { status = "success", data = preview }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در آماده‌سازی حذف نماینده " + id);
                return Json(new { status = "error", message = "خطا در آماده‌سازی اطلاعات حذف" }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> DeleteAgent(int id)
        {
            try
            {
                var agent = ResolveManagedAgent(id);
                if (agent == null)
                    return MessageBox.Warning("ناموفق", "نماینده یافت نشد یا مجوز حذف ندارید.");

                var agentName = agent.Username;

                using (var transaction = db.Database.BeginTransaction())
                {
                    await AgentDeleteService.DeleteAgentAsync(db, agent);
                    transaction.Commit();
                }

                logger.Info("نماینده " + agentName + " به همراه وابستگی‌های پنل حذف شد");
                return Toaster.Success("موفق", "نماینده و داده‌های وابسته پنل با موفقیت حذف شدند");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در حذف نماینده " + id);
                return MessageBox.Error("ناموفق", "حذف نماینده با خطا مواجه شد");
            }
        }

        #endregion

        #region انتخاب تعرفه برای نماینده

        #region نمایش
        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> GetPlans(int user_id)
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
                    var current = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                    if (current != null && current.Role == 1)
                        user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);
                }
                if (user == null)
                {
                    return RedirectToAction("Error404", "Error", new { area = "App" });
                }

                var plans = user.tbLinkUserAndPlans.Where(p => p.L_Status == true).ToList();

                List<UserPlansViewModel> userPlansViews = new List<UserPlansViewModel>();
                foreach (var item in plans)
                {
                    UserPlansViewModel userPlans = new UserPlansViewModel();
                    userPlans.UserPlan_ID = item.Link_PU_ID;
                    userPlans.UserPlan_Name = item.tbPlans.Plan_Name;
                    userPlans.Plan_ID = item.L_FK_P_ID.Value;
                    userPlans.IsRobotPlan = item.L_ShowInBot;
                    if (item.L_SellPrice is null)
                    {
                        userPlans.UserPlan_Price = "ندارد";
                    }
                    else
                    {
                        userPlans.UserPlan_Price = item.L_SellPrice.Value.ConvertToMony() + " " + "ءتء";
                    }

                    userPlansViews.Add(userPlans);
                }


                return Json(new { status = "success", data = userPlansViews }, JsonRequestBehavior.AllowGet);
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
        public async Task<ActionResult> SetPlan(int user_id, int userPlan, string userPlanPrice)
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
                    var current = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                    if (current != null && current.Role == 1)
                        user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);
                }
                if (user == null)
                {
                    return RedirectToAction("Error404", "Error", new { area = "App" });
                }


                var planuser = user.tbLinkUserAndPlans.Where(p => p.L_FK_P_ID == userPlan).FirstOrDefault();
                if (planuser != null)
                {
                    planuser.L_Status = true;
                    if (userPlanPrice != "")
                    {
                        try
                        {
                            planuser.L_SellPrice = int.Parse(userPlanPrice, NumberStyles.Currency);
                        }
                        catch (Exception ex)
                        {
                            return MessageBox.Warning("هشدار", "لطفا قیمت فروش نماینده رو به صورت صحیح وارد کنید");
                        }
                    }
                }
                else
                {
                    tbLinkUserAndPlans link = new tbLinkUserAndPlans();
                    link.L_Status = true;
                    link.L_FK_P_ID = userPlan;
                    if (userPlanPrice != "")
                    {
                        try
                        {
                            link.L_SellPrice = int.Parse(userPlanPrice, NumberStyles.Currency);
                        }
                        catch (Exception ex)
                        {
                            return MessageBox.Warning("هشدار", "لطفا قیمت فروش نماینده رو به صورت صحیح وارد کنید");
                        }
                    }
                    user.tbLinkUserAndPlans.Add(link);
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

        #region حذف 

        [System.Web.Http.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> DeletePlan(int id)
        {
            var planLink = await RepositoryUserPlanLinks.FirstOrDefaultAsync(s => s.Link_PU_ID == id);

            planLink.L_Status = false;


            await RepositoryUserPlanLinks.SaveChangesAsync();

            return Toaster.Success("موفق", "تعرفه تخصیص داده شده حذف گردید");
        }


        #endregion

        #region ثبت کردن در ربات 

        [System.Web.Http.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> SetPlanInBot(int id)
        {
            var planLink = await RepositoryUserPlanLinks.FirstOrDefaultAsync(s => s.Link_PU_ID == id);

            if (planLink.L_ShowInBot == true)
            {
                planLink.L_ShowInBot = false;
            }
            else
            {
                planLink.L_ShowInBot = true;
            }


            await RepositoryUserPlanLinks.SaveChangesAsync();

            return Toaster.Success("موفق", "ثبت شد");
        }


        #endregion

        #endregion

        #region دریافت اطلاعیه های کاربر

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "3,2,4")]
        public ActionResult GetNotifications()
        {

            var user = RepositoryUser.table.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                var List = user.tbNotificationUser.Where(s => s.tbNotifications.tbNoti_EndDate >= DateTime.Now).OrderByDescending(s => s.tbNotifications.tbNoti_RegisterDate).ToList();
                return PartialView(List);
            }
            else
            {
                return PartialView(new List<tbNotificationUser>());
            }
        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "3,2,4")]
        public ActionResult GetCountNotification()
        {
            var user = RepositoryUser.table.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                var Count = user.tbNotificationUser.Where(s => s.tbNotifications.tbNoti_EndDate >= DateTime.Now).Count();
                return Content(Count.ToString());
            }
            else
            {
                return Content("0");
            }
        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "3,2,4")]
        public ActionResult GetCountNotSeenNotification()
        {
            var user = RepositoryUser.table.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                var Noti = user.tbNotificationUser.Where(s => s.tbNotifications.tbNoti_EndDate >= DateTime.Now && s.tbNotiUser_Seen == false).FirstOrDefault();


                if(Noti!= null)
                {
                    return Json(new { title = Noti.tbNotifications.tbNoti_Title, message = Noti.tbNotifications.tbNoti_Text, count = 1 }, JsonRequestBehavior.AllowGet) ;
                }
                else
                {
                    return Json(new { title = "", message = "", count = 0 },JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Content("0");
            }
        }


        /// <summary>
        /// تمام اعلانات خوانده‌نشده و منقضی‌نشده کاربر برای نمایش در مودال هنگام ورود.
        /// برخلاف GetCountNotSeenNotification که فقط یک اعلان برمی‌گرداند، اینجا کل لیست
        /// برگردانده می‌شود تا هیچ اعلانی بدون دیده‌شدن، seen نخورد.
        /// </summary>
        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "3,2,4")]
        public ActionResult GetUnseenNotifications()
        {
            var user = RepositoryUser.table.FirstOrDefault(s => s.Username == User.Identity.Name);
            if (user == null)
                return Json(new { show = false, items = new List<object>() }, JsonRequestBehavior.AllowGet);

            var items = user.tbNotificationUser
                .Where(s => s.tbNotifications.tbNoti_EndDate >= DateTime.Now && s.tbNotiUser_Seen == false)
                .OrderByDescending(s => s.tbNotifications.tbNoti_RegisterDate)
                .ToList()
                .Select(s => new
                {
                    id = s.tbNotiUser_ID,
                    title = s.tbNotifications.tbNoti_Title,
                    text = s.tbNotifications.tbNoti_Text,
                    date = Utility.GetTimeDifference(s.tbNotifications.tbNoti_RegisterDate, DateTime.Now),
                    icon = PanelNotificationService.GetIconClass(s.tbNotifications.tbNoti_Title),
                    color = PanelNotificationService.GetColorClass(s.tbNotifications.tbNoti_Title),
                    category = PanelNotificationService.GetCategoryLabel(s.tbNotifications.tbNoti_Title)
                })
                .ToList();

            return Json(new { show = items.Any(), items }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// فقط اعلاناتی که واقعاً به کاربر نمایش داده شده‌اند را seen می‌کند.
        /// </summary>
        [System.Web.Mvc.HttpPost]
        [AuthorizeApp(Roles = "3,2,4")]
        public ActionResult SeenNotifications(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return Content("Ok");

            var user = RepositoryUser.table.FirstOrDefault(s => s.Username == User.Identity.Name);
            if (user == null)
                return Content("Error");

            var targets = user.tbNotificationUser
                .Where(s => ids.Contains(s.tbNotiUser_ID) && s.tbNotiUser_Seen == false)
                .ToList();

            foreach (var item in targets)
            {
                item.tbNotiUser_Seen = true;
                item.tbNotiUser_DateSeen = DateTime.Now;
            }

            RepositoryUser.Save();
            return Content("Ok");
        }

        #endregion

        #region سین اطلاعیه کاربر
        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "3,2,4")]
        public ActionResult SeenUserNotif()
        {
            var user = RepositoryUser.table.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                var Noti = user.tbNotificationUser.Where(s => s.tbNotiUser_Seen == false).ToList();
                foreach (var item in Noti)
                {
                    item.tbNotiUser_Seen = true;
                    item.tbNotiUser_DateSeen = DateTime.Now;
                }

                RepositoryUser.Save();
                logger.Info("اطلاعیه سین شد");
                return Content("Ok");
            }
            else
            {
                return Content("Error");
            }
        }

        #endregion

        #region سین تمام اطلاعیه های کاربر
        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "3,2,4")]
        public ActionResult DeleteAllUserNotif()
        {
            var user = RepositoryUser.table.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                var Noties = user.tbNotificationUser.ToList();
                foreach (var Noti in Noties)
                {
                    Noti.tbNotiUser_Seen = true;
                    Noti.tbNotiUser_DateSeen = DateTime.Now;
                }

                RepositoryUser.Save();
                logger.Info("تمام اطلاعیه ها سین شد");
                return Content("Ok");
            }
            else
            {
                return Content("Error");
            }
        }

        #endregion

        #region ساخت فاکتور جهت پرداخت خودکار بدهی

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "3,2,4")]
        public ActionResult CreateFactoryForPay()
        {

            try
            {
                var user = RepositoryUser.table.Where(s => s.Username == User.Identity.Name).FirstOrDefault();

                if (user != null)
                {
                    if (user.Wallet > 0)
                    {
                        var factors = user.tbUserFactors.Where(a => a.tbUf_Status == 1).ToList();
                        foreach (var item in factors)
                        {
                            user.tbUserFactors.Remove(item);
                        }

                        Random ran = new Random();

                        var Dept = user.Wallet;

                        var PayAmount = (Dept * 10) + ran.Next(1, 999);

                        tbUserFactors userFactor = new tbUserFactors();
                        userFactor.tbUf_CreateTime = DateTime.Now;
                        userFactor.tbUf_Status = 1;
                        userFactor.tbUf_Value = PayAmount;
                        user.tbUserFactors.Add(userFactor);
                        RepositoryUser.Save();

                        logger.Info("کاربر فاکتور به مبلغ : " + PayAmount + " جهت پرداخت بدهی ساخت");

                        TempData["TimeForPay"] = true;
                        return RedirectToAction("Details", new { area = "App", type = "Factors", user_id = user.User_ID });
                    }
                    else
                    {
                        return RedirectToAction("Details", new { area = "App", type = "Factors", user_id = user.User_ID });
                    }
                }

                return MessageBox.Warning("خطا", "خطا در ساخت فاکتور");

            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ساخت فاکتور");
                return MessageBox.Warning("خطا", "خطا در ساخت فاکتور");

            }

        }

        #endregion

        #endregion

        #region ورود

        [System.Web.Mvc.HttpGet]
        public ActionResult Login()
        {
            var isTrue = RepositoryUser.table.Where(p => p.IsNotActiveSell == true && p.Role == 1 && p.Status == true).Any();
            if (isTrue)
            {
                ViewBag.IsNotActiveSell = true;
            }

            var principal = AuthSession.GetValidPrincipal(Request);
            if (principal != null)
            {
                int userId;
                if (int.TryParse(principal.FindFirst(ClaimTypes.Name)?.Value, out userId))
                {
                    var user = RepositoryUser.table.FirstOrDefault(p => p.User_ID == userId && p.Status == true);
                    if (user != null && (user.tbUsers2 == null || user.tbUsers2.Status == true))
                    {
                        ViewBag.CanContinue = true;
                        ViewBag.ContinueUserName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
                        ViewBag.ContinueRedirectUrl = user.Role == 1
                            ? Url.Action("Index", "ManagementDashboard")
                            : Url.Action("Index", "Subscriptions");
                    }
                }
            }
            else if (Request.Cookies["Token"] != null)
            {
                AuthSession.ClearLoginCookies(Response);
            }

            return View();
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult ContinueLogin()
        {
            try
            {
                var principal = AuthSession.GetValidPrincipal(Request);
                if (principal == null)
                {
                    AuthSession.ClearLoginCookies(Response);
                    return MessageBox.Warning("هشدار", "نشست شما منقضی شده است. لطفاً دوباره وارد شوید");
                }

                int userId;
                if (!int.TryParse(principal.FindFirst(ClaimTypes.Name)?.Value, out userId))
                {
                    AuthSession.ClearLoginCookies(Response);
                    return MessageBox.Warning("هشدار", "نشست نامعتبر است. لطفاً دوباره وارد شوید");
                }

                var user = RepositoryUser.table.FirstOrDefault(p => p.User_ID == userId);
                if (user == null || !user.Status.Value)
                {
                    AuthSession.ClearLoginCookies(Response);
                    return MessageBox.Warning("هشدار", "حساب کاربری شما غیرفعال شده است");
                }

                if (user.tbUsers2 != null && !user.tbUsers2.Status.Value)
                {
                    AuthSession.ClearLoginCookies(Response);
                    return MessageBox.Warning("هشدار", "حساب کاربری شما غیرفعال شده است");
                }

                FormsAuthentication.SetAuthCookie(user.Username, true);

                var redirectUrl = user.Role == 1
                    ? Url.Action("Index", "ManagementDashboard")
                    : Url.Action("Index", "Subscriptions");

                logger.Info("ورود با ادامه نشست ذخیره‌شده");
                return Json(new { status = "success", redirectURL = redirectUrl });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ادامه نشست ورود");
                return MessageBox.Error("خطا", "خطا در برقراری ارتباط با سرور");
            }
        }
        /// <summary>
        /// تابع لاگین از سمت پنل ادمین
        /// </summary>
        /// <param name="loginModel"></param>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        public async Task<ActionResult> Login(string userUsername, string userPassword)
        {
            try
            {
                //RedisConnector redis = new RedisConnector();
                //var key = redis.GetValue(100);
                //var s = new Serializer();
                //var d = (System.Collections.Hashtable)s.Deserialize(key); 



                if (string.IsNullOrEmpty(userUsername) || string.IsNullOrEmpty(userPassword))
                    return MessageBox.Warning("خطا", "نام کاربری و رمز عبور را وارد کنید");

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


                //if (User.Username == "darkbaz" || User.Username == "markazi")
                //{
                //    try
                //    {
                //        using (MySqlEntities mysql = new MySqlEntities(User.tbServers.ConnectionString))
                //        {
                //            await mysql.OpenAsync();

                //            // لیست برای ذخیره‌سازی داده‌های خوانده‌شده
                //            var userList = new List<UserData>();


                //            var logs = await RepositoryLogs.GetAllAsync();
                //            foreach (var item in logs)
                //            {
                //                if (item.tbLinkUserAndPlans.tbUsers != null)
                //                {
                //                    var email = item.FK_NameUser_ID + "@" + item.tbLinkUserAndPlans.tbUsers.Username;
                //                    var reader = await mysql.GetDataAsync("SELECT token FROM `v2_user` where v2_user.email = '" + email + "'");
                //                    while (await reader.ReadAsync())
                //                    {
                //                        item.SubToken = reader.GetBodyDefinition("token");
                //                    }
                //                    reader.Close();
                //                }



                //            }
                //            await RepositoryLogs.SaveChangesAsync();

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

                    if (User.tbUsers2 != null)
                    {
                        if(!User.tbUsers2.Status.Value)
                        {
                            return MessageBox.Warning("هشدار", "حساب کاربری شما غیرفعال شده است");
                        }
                    }

                    User.Token = (userUsername + userPassword).ToSha256();

                    var token = JwtToken.GenerateToken(User.User_ID.ToString(), User.Role.ToString(), JwtToken.GetSecretKey(), AuthSession.ExpireMinutes);
                    AuthSession.SetLoginCookies(Response, User, token);

                    logger.Info("ورود موفق");
                    RepositoryUser.Save();


                    if (User.Role == 1)
                    {
                        var URL = Url.Action("Index", "ManagementDashboard");
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
            AuthSession.ClearLoginCookies(Response);
            logger.Info("خروج موفق");
            return RedirectToAction("Login", "Admin");
        }

        #endregion

        #region نمایش لاگ ایجاد یا تمدید کاربر عمده 
        //[AuthorizeApp(Roles = "1,3,4")]
        public ActionResult GetUserAccountLog(int user_id, string fromDate = null, string toDate = null)
        {
            try
            {
                var result = BuildAgentHistory(user_id, fromDate, toDate);
                if (result == null)
                    return Json(new { data = new List<UserLogResponseModel>(), summary = (AgentHistorySummaryViewModel)null }, JsonRequestBehavior.AllowGet);

                return Json(new { data = result.Items, summary = result.Summary }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "در نمایش تاریخچه ساخت کاربر با خطایی مواجه شدیم !!");
                return Json(new { data = new List<UserLogResponseModel>(), summary = (AgentHistorySummaryViewModel)null }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeApp(Roles = "1,3,4,2")]
        public ActionResult ExportAgentHistoryPdf(int user_id, string fromDate = null, string toDate = null)
        {
            try
            {
                var result = BuildAgentHistory(user_id, fromDate, toDate);
                if (result == null)
                    return Content("نماینده یافت نشد");

                var pdfBytes = AgentHistoryPdfHelper.Export(result);
                var safeName = string.IsNullOrWhiteSpace(result.AgentUsername) ? "agent" : result.AgentUsername;
                var fileName = "agent-history-" + safeName + "-" + DateTime.Now.ToString("yyyyMMddHHmm") + ".pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خروجی PDF تاریخچه اشتراک نماینده با خطا مواجه شد");
                return Content("خطا در تهیه خروجی PDF");
            }
        }

        private AgentHistoryPdfResultViewModel BuildAgentHistory(int user_id, string fromDate, string toDate)
        {
            var user = RepositoryUser.Where(p => p.User_ID == user_id).FirstOrDefault();
            if (user == null)
                return null;

            DateTime? start = null;
            DateTime? end = null;
            if (!string.IsNullOrWhiteSpace(fromDate))
                start = ParsePersianDate(fromDate).Date;
            if (!string.IsNullOrWhiteSpace(toDate))
                end = ParsePersianDate(toDate).Date.AddDays(1).AddSeconds(-1);

            var logs = RepositoryLogs
                .Where(p => p.tbLinkUserAndPlans != null && p.tbLinkUserAndPlans.L_FK_U_ID == user_id && p.CreateDatetime.HasValue)
                .ToList()
                .Where(p =>
                {
                    var dt = p.CreateDatetime.Value;
                    if (start.HasValue && dt < start.Value) return false;
                    if (end.HasValue && dt > end.Value) return false;
                    return true;
                })
                .OrderByDescending(p => p.CreateDatetime)
                .ToList();

            var createdAction = Resource.LogActions.U_Created;
            var editedAction = Resource.LogActions.U_Edited;
            var reserveAction = ReservedPackageHelper.ReserveLogAction;

            var historyLogs = logs
                .Where(SubscriptionLogHelper.ShouldAppearInAgentHistory)
                .ToList();

            var summary = new AgentHistorySummaryViewModel
            {
                CreatedCount = historyLogs.Count(l =>
                    (l.Action == createdAction || l.Action == ReservedPackageHelper.CreatedLogAction)
                    && !SubscriptionLogHelper.IsDeletedSubscriptionLogName(l.FK_NameUser_ID)),
                RenewedCount = historyLogs.Count(l =>
                    (l.Action == editedAction || l.Action == reserveAction || l.Action == ReservedPackageHelper.EditedLogAction)
                    && !SubscriptionLogHelper.IsDeletedSubscriptionLogName(l.FK_NameUser_ID)),
                TotalSalesAmount = historyLogs.Where(SubscriptionLogHelper.CountsTowardSalesSummary).Sum(l => l.SalePrice ?? 0),
                PaidInvoicesAmount = user.tbUserFactors
                    .Where(f => f.tbUf_CreateTime.HasValue && f.tbUf_Value.HasValue && (f.tbUf_Status == 2 || f.tbUf_Status == 3))
                    .Where(f =>
                    {
                        var dt = f.tbUf_CreateTime.Value;
                        if (start.HasValue && dt < start.Value) return false;
                        if (end.HasValue && dt > end.Value) return false;
                        return true;
                    })
                    .Sum(f => f.tbUf_Value.Value)
                    .RialToToman()
            };
            summary.TotalSalesAmountFormatted = summary.TotalSalesAmount.ConvertToMony();
            summary.PaidInvoicesAmountFormatted = summary.PaidInvoicesAmount.ConvertToMony();

            var logModels = SubscriptionLogHelper.MapAgentHistoryItems(historyLogs);

            var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;

            return new AgentHistoryPdfResultViewModel
            {
                AgentName = displayName,
                AgentUsername = user.Username,
                FromDate = start.HasValue ? start.Value.ConvertDateTimeToShamsi2() : "ابتدا",
                ToDate = end.HasValue ? end.Value.ConvertDateTimeToShamsi2() : "اکنون",
                GeneratedAt = DateTime.Now.ConvertDateTimeToShamsi2(),
                Summary = summary,
                Items = logModels
            };
        }

        private static DateTime ParsePersianDate(string date)
        {
            try
            {
                return DateTime.Parse(date.Trim(), CultureInfo.GetCultureInfo("fa-IR"));
            }
            catch
            {
                var parts = date.Split('/');
                if (parts.Length == 3)
                {
                    var pc = new PersianCalendar();
                    return pc.ToDateTime(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), 0, 0, 0, 0);
                }
                throw;
            }
        }



        #endregion

        #region فاکتور های کاربر 
        [AuthorizeApp(Roles = "1,3,4,2")]
        public ActionResult Factors(int user_id)
        {

            var User = RepositoryUser.Where(p => p.User_ID == user_id).FirstOrDefault();
            if (User != null)
            {
                var Factors = User.tbUserFactors.OrderByDescending(a=> a.tbUf_CreateTime).ToList();
                List<UserFactorResponseModel> Factores = new List<UserFactorResponseModel>();
                foreach (var item in Factors)
                {
                    UserFactorResponseModel factor = new UserFactorResponseModel();
                    factor.PayDate = item.tbUf_CreateTime.Value.ConvertDateTimeToShamsi2();
                    factor.Price = item.tbUf_Value.Value.RialToToman().ConvertToMony();
                    factor.PayStatus = item.tbUf_Status.Value;
                    factor.factor_id = item.tbUf_ID;
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
            var current = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            if (current == null || current.Role != 1)
                return Content("");

            var us = db.tbUsers.Where(p => p.User_ID == user_id).FirstOrDefault();
            if (us != null && us.Role != 1)
                return PartialView(us);

            return Content("");
        }


        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditWallet(int user_id, string userDeposit)
        {
            try
            {
                var current = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
                if (current == null || current.Role != 1)
                    return MessageBox.Warning("ناموفق", "فقط ادمین سیستم مجاز به تغییر مبلغ کیف پول است");

                var us = db.tbUsers.Where(p => p.User_ID == user_id).FirstOrDefault();
                if (us == null || us.Role == 1)
                    return MessageBox.Warning("ناموفق", "امکان تغییر کیف پول این کاربر وجود ندارد");

                var intWallet = 0;
                intWallet = int.Parse(userDeposit, NumberStyles.Currency);


                if (us.Role == 4)
                {
                    tbUserFactors factor = new tbUserFactors();
                    factor.tbUf_Value = (us.Wallet - intWallet).TomanToRial();
                    factor.tbUf_CreateTime = DateTime.Now;
                    factor.FK_User_ID = user_id;
                    us.Wallet = intWallet;
                    us.tbUserFactors.Add(factor);
                }
                else
                if (us.Wallet != intWallet)
                {
                    tbUserFactors factor = new tbUserFactors();
                    factor.tbUf_Value = (us.Wallet - intWallet).TomanToRial();
                    factor.tbUf_CreateTime = DateTime.Now;
                    factor.FK_User_ID = user_id;
                    us.Wallet = intWallet;
                    us.tbUserFactors.Add(factor);
                }
                RepositoryUser.Save();
                logger.Info("کیف پول نماینده " + us.Username + " توسط ادمین به " + intWallet + " تغییر کرد");
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

        /// <summary>
        /// برچسب موعد تسویه بعدی برای دراپ‌داون پروفایل در هدر.
        /// اگر تسویه بدهی برای کاربر تنظیم نشده باشد null برمی‌گرداند تا در هدر چیزی نمایش داده نشود.
        /// </summary>
        private string BuildSettlementDueLabel(tbUsers user)
        {
            var dueDate = SettlementService.GetNextDueDate(db, user);
            if (!dueDate.HasValue)
                return null;

            var label = dueDate.Value.ConvertDateTimeToShamsi5();
            var remainingDays = (dueDate.Value.Date - DateTime.Now.Date).Days;

            if (remainingDays > 0)
                return label + " (" + remainingDays + " روز مانده)";

            if (remainingDays == 0)
                return label + " (امروز)";

            return label + " (" + (-remainingDays) + " روز گذشته)";
        }

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

                    var Deb = user.tbUserFactors.OrderByDescending(s => s.tbUf_CreateTime).FirstOrDefault();

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
                        var Data = reader2["Used"];
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
                        return Json(new { status = "success", data = new { UserPerPrice = Utility.ConvertToMony(Math.Round((Useage * (userPerGig + userPerMonth)))), Useage = Math.Round(Useage, 2).ConvertToMony(), pricePerGig = userPerGig, pricePerMonth = userPerMonth, settlementDue = BuildSettlementDueLabel(user) } }, JsonRequestBehavior.AllowGet);
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

                        return Json(new { status = "success", data = new { debt = user.Wallet.ConvertToMony(), inventory = (user.Limit - user.Wallet).Value.ConvertToMony(), pricePerGig = userPerGig, pricePerMonth = userPerMonth, pricePerUser = userPerUser, settlementDue = BuildSettlementDueLabel(user) } }, JsonRequestBehavior.AllowGet);

                    }
                    else
                    {
                        return Json(new { status = "success", data = new { debt = user.Wallet.ConvertToMony(), inventory = (user.Limit - user.Wallet).Value.ConvertToMony(), settlementDue = BuildSettlementDueLabel(user) } }, JsonRequestBehavior.AllowGet);

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
                    if (User.Role == 1)
                        return MessageBox.Warning("هشدار", "این گزینه برای پروفایل ادمین قابل تنظیم نیست");

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

        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpGet]
        public ActionResult _GetSettlementSetting(int user_id)
        {
            var user = RepositoryUser.Where(s => s.User_ID == user_id).FirstOrDefault();
            if (user == null)
                return Content("کاربر یافت نشد");
            return PartialView(user);
        }

        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveSettlementSetting(int user_id, bool settlementEnabled = false,
            int? settlementDaysAfterPayment = 15, int? settlementPreWarningDays = 2, int? settlementBlockGraceDays = 2)
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);
                if (user == null)
                    return MessageBox.Warning("ناموفق", "کاربر یافت نشد");

                user.Settlement_Enabled = settlementEnabled;
                user.Settlement_Type = "Rolling";
                user.Settlement_DayOfMonth = Math.Max(1, Math.Min(365, settlementDaysAfterPayment ?? SettlementService.DefaultDaysAfterLastPayment));
                user.Settlement_DayOfWeek = Math.Max(0, Math.Min(user.Settlement_DayOfMonth.Value, settlementPreWarningDays ?? SettlementService.DefaultPreWarningDays));
                user.Settlement_BlockGraceDays = Math.Max(0, Math.Min(30, settlementBlockGraceDays ?? SettlementService.DefaultBlockGraceDays));

                if (settlementEnabled && !user.Settlement_StartDate.HasValue)
                    user.Settlement_StartDate = DateTime.Now.Date;

                SettlementService.ResetSettlementWarnings(user);

                if (!settlementEnabled && user.Settlement_IsBlocked)
                {
                    await SettlementService.UnblockAgentSubscriptions(user, db);
                    user.Settlement_IsBlocked = false;
                }

                await RepositoryUser.SaveChangesAsync();
                logger.Info("تنظیمات تسویه نماینده " + user.Username + " ذخیره شد");
                return Toaster.Success("موفق", "تنظیمات تسویه با موفقیت ذخیره شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ذخیره تنظیمات تسویه");
                return MessageBox.Error("ناموفق", "خطا در ذخیره تنظیمات تسویه");
            }
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
        public ActionResult _GetAdminPanelSellSetting(int user_id)
        {
            var user = RepositoryUser.Where(s => s.User_ID == user_id).FirstOrDefault();
            if (user == null)
                return Content(string.Empty);
            if (user.Role != 1)
                return Content(string.Empty);

            return PartialView("_AdminPanelSellSetting", user);
        }

        [System.Web.Mvc.HttpPost]
        [AuthorizeApp(Roles = "1")]
        public ActionResult SaveAdminPanelSellSetting(int user_id, bool isNotActiveSell)
        {
            try
            {
                var user = RepositoryUser.Where(s => s.User_ID == user_id && s.Role == 1).FirstOrDefault();
                if (user == null)
                    return MessageBox.Warning("ناموفق", "فقط برای پروفایل ادمین قابل تنظیم است");

                user.IsNotActiveSell = isNotActiveSell;
                RepositoryUser.Save();
                logger.Info("وضعیت فروش نمایندگی پنل برای ادمین " + user.Username + " به " + isNotActiveSell + " تغییر یافت");
                return Toaster.Success("موفق", isNotActiveSell ? "فروش نمایندگی در پنل بسته شد" : "فروش نمایندگی در پنل فعال شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ذخیره وضعیت فروش نمایندگی پنل");
                return MessageBox.Error("ناموفق", "خطا در ذخیره تنظیمات");
            }
        }

        private static void ApplyBotSettingExtras(tbBotSettings botSettings, double? presentDiscount, double? invitePercent, bool? isNotActiveSell)
        {
            if (presentDiscount != null && presentDiscount != 0)
                botSettings.Present_Discount = presentDiscount / 100;
            else
                botSettings.Present_Discount = null;

            if (invitePercent != null && invitePercent != 0)
                botSettings.InvitePercent = invitePercent / 100;
            else
                botSettings.InvitePercent = null;

            botSettings.IsNotActiveSell = isNotActiveSell == true;
        }

        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpGet]
        public ActionResult _GetBotSetting(int user_id)
        {
            var user = RepositoryUser.Where(s => s.User_ID == user_id).FirstOrDefault();
            if (user == null)
                return Content("کاربر یافت نشد");

            var botSettings = user.tbBotSettings;
            var botSetting = botSettings != null ? botSettings.FirstOrDefault() : null;
            if (botSetting != null)
                return PartialView(botSetting);

            return PartialView(new tbBotSettings { FK_User_ID = user_id });
        }


        [System.Web.Mvc.HttpPost]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> SaveBotSetting(int id, int user_id, string BotId, string BotToken, long TelegramUserId, string ChannelId, bool? Enabled, bool? RequiredJoinChannel , bool? IsActiveSendReceipt, int userPlan, double? Present_Discount = null, double? InvitePercent = null, bool? IsNotActiveSell = null)
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
                            var joined = bot.GetChatMember("@" + ChannelId, TelegramUserId);
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
                        var res = bot.SendMessage(TelegramUserId, "پیغام جهت صحت سنجی اطلاعات ثبت شده در تنظیمات می باشد");
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
                    ApplyBotSettingExtras(botSettings, Present_Discount, InvitePercent, IsNotActiveSell);
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


                    if (Enabled == null)
                    {
                        botSettings.Enabled = false;
                    }
                    else
                    {
                        botSettings.Enabled = true;
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
                    ApplyBotSettingExtras(botSettings, Present_Discount, InvitePercent, IsNotActiveSell);

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


                    if (Enabled == null)
                    {
                        botSettings.Enabled = false;
                    }
                    else
                    {
                        botSettings.Enabled = true;
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

        #region تنظیمات ( آموزش ها )

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1")]
        public ActionResult _GetLearns()
        {
            return PartialView("_GetLearns");

        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> _GetLearnsAsync(int user_id)
        {
            var user = await RepositoryUser.FirstOrDefaultAsync(p => p.User_ID == user_id);

            List<LearnsViewModel> Learns = new List<LearnsViewModel>();

            foreach (var item in user.tbConnectionHelp)
            {
                LearnsViewModel learn = new LearnsViewModel();
                learn.Learn_Title = item.ch_Title;
                learn.Learn_Link = item.ch_Link;
                learn.Learn_ID = item.ch_ID;
                Learns.Add(learn);

            }


            return Json(new { status = "success", data = Learns }, JsonRequestBehavior.AllowGet);

        }

        [System.Web.Mvc.HttpGet]
        public async Task<ActionResult> DeleteLearn(int Learn_ID, int user_id)
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);


                var Learn = user.tbConnectionHelp.Where(s => s.ch_ID == Learn_ID).FirstOrDefault();
                if (Learn != null)
                {
                    user.tbConnectionHelp.Remove(Learn);

                    await repositoryCard.SaveChangesAsync();
                    logger.Info("آموزش با موفقیت حذف گردید");
                    return Toaster.Success("موفق", "آموزش مورد نظر حذف گردید");
                }
                else
                {
                    return MessageBox.Warning("هشدار", "آموزش مورد نظر یافت نشد");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در حذف آموزش");
                return MessageBox.Error("خطا", "خطا در حذف آموزش");
            }

        }


        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> SaveLearn(int user_id, string Learn_Title, string Learn_Link, int Learn_ID = 0)
        {
            var user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);
            if (Learn_ID != 0)
            {
                var Learn = user.tbConnectionHelp.Where(s => s.ch_ID == Learn_ID).FirstOrDefault();
                if (Learn != null)
                {
                    Learn.ch_Link = Learn_Link;
                    Learn.ch_Title = Learn_Title;
                    Learn.ch_Type = "vpn";

                }
                await RepositoryUser.SaveChangesAsync();
                return Toaster.Success("موفق", "اطلاعات آموزش ویرایش شد");
            }
            else
            {
                tbConnectionHelp learn = new tbConnectionHelp();
                learn.ch_Link = Learn_Link;
                learn.ch_Title = Learn_Title;
                learn.ch_Type = "vpn";


                user.tbConnectionHelp.Add(learn);
                await RepositoryUser.SaveChangesAsync();
                return Toaster.Success("موفق", "اطلاعات آموزش اضافه شد");
            }
        }


        #endregion

        #region تنظیمات پشتیبانی

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult _GetSupportLinks()
        {
            return PartialView("_GetSupportLinks");
        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> GetSupportLinks(int user_id)
        {
            var user = await RepositoryUser.FirstOrDefaultAsync(p => p.User_ID == user_id);
            if (user == null)
                return Json(new { status = "success", data = new List<SupportLinkViewModel>() }, JsonRequestBehavior.AllowGet);

            List<SupportLinkViewModel> links = new List<SupportLinkViewModel>();
            foreach (var item in user.tbSupportLinks.OrderBy(s => s.tbSl_ID))
            {
                SupportLinkViewModel link = new SupportLinkViewModel();
                link.Support_ID = item.tbSl_ID;
                link.Support_Title = item.tbSl_Title;
                link.Support_Link = item.tbSl_Link;
                link.Support_Phone = item.tbSl_Phone;
                links.Add(link);
            }

            return Json(new { status = "success", data = links }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> SaveSupportLink(int user_id, string Support_Title, string Support_Link, string Support_Phone, int Support_ID = 0)
        {
            try
            {
                Support_Title = (Support_Title ?? string.Empty).Trim();
                Support_Link = string.IsNullOrWhiteSpace(Support_Link) ? null : Support_Link.Trim();
                Support_Phone = string.IsNullOrWhiteSpace(Support_Phone) ? null : Support_Phone.Trim();

                if (string.IsNullOrEmpty(Support_Title))
                    return MessageBox.Warning("ناموفق", "عنوان ارتباط را وارد کنید");

                if (Support_Link == null && Support_Phone == null)
                    return MessageBox.Warning("ناموفق", "حداقل لینک ارتباط یا شماره تلفن را وارد کنید");

                var user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);
                if (user == null)
                    return MessageBox.Warning("ناموفق", "کاربر یافت نشد");

                if (Support_ID != 0)
                {
                    var Link = user.tbSupportLinks.Where(s => s.tbSl_ID == Support_ID).FirstOrDefault();
                    if (Link == null)
                        return MessageBox.Warning("هشدار", "لینک ارتباطی یافت نشد");

                    Link.tbSl_Title = Support_Title;
                    Link.tbSl_Link = Support_Link;
                    Link.tbSl_Phone = Support_Phone;
                    await RepositoryUser.SaveChangesAsync();
                    return Toaster.Success("موفق", "لینک ارتباطی ویرایش شد");
                }

                tbSupportLinks item = new tbSupportLinks();
                item.tbSl_Title = Support_Title;
                item.tbSl_Link = Support_Link;
                item.tbSl_Phone = Support_Phone;
                user.tbSupportLinks.Add(item);
                await RepositoryUser.SaveChangesAsync();
                return Toaster.Success("موفق", "لینک ارتباطی اضافه شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ذخیره لینک ارتباطی پشتیبانی");
                return MessageBox.Error("ناموفق", "خطا در ذخیره لینک ارتباطی");
            }
        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> DeleteSupportLink(int Support_ID, int user_id)
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(s => s.User_ID == user_id);
                if (user == null)
                    return MessageBox.Warning("هشدار", "کاربر یافت نشد");

                var Link = user.tbSupportLinks.Where(s => s.tbSl_ID == Support_ID).FirstOrDefault();
                if (Link == null)
                    return MessageBox.Warning("هشدار", "لینک ارتباطی یافت نشد");

                user.tbSupportLinks.Remove(Link);
                await RepositoryUser.SaveChangesAsync();
                logger.Info("لینک ارتباطی پشتیبانی حذف شد");
                return Toaster.Success("موفق", "لینک ارتباطی حذف گردید");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در حذف لینک ارتباطی پشتیبانی");
                return MessageBox.Error("خطا", "خطا در حذف لینک ارتباطی");
            }
        }

        #endregion

        #region دریافت لیست نمایندگان در قالب Select2

        [System.Web.Http.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> GetUsersSelect()
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                if (user != null)
                {
                    List<tbUsers> Users;
                    if (user.Role == 1)
                    {
                        Users = await RepositoryUser.table
                            .Where(s => s.FK_Server_ID == user.FK_Server_ID && s.Status == true && s.Role != 1)
                            .OrderBy(s => s.Username)
                            .ToListAsync();
                    }
                    else
                    {
                        Users = user.tbUsers1.Where(s => s.Status == true).OrderBy(s => s.Username).ToList();
                    }

                    List<SelectUserViewModel> SelectUsers = new List<SelectUserViewModel>();
                    foreach (var User in Users)
                    {
                        SelectUserViewModel userselect = new SelectUserViewModel();
                        userselect.id = User.User_ID;
                        userselect.username = User.Username;
                        userselect.debt = User.Wallet;
                        SelectUsers.Add(userselect);
                    }

                    return Json(new { data = SelectUsers }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دریافت لیست نمایندگان در قالب select");
                return null;
            }
        }

        #endregion
    }
}
