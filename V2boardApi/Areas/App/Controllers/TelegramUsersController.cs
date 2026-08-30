using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Telegram.Bot;
using V2boardApi.Areas.App.Data.OrdersViewModels;
using V2boardApi.Areas.App.Data.TelegramUsersViewModel;
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
        private Repository<tbOrders> RepositoryOrders { get; set; }
        Repository<tbLinks> RepositoryLinks { get; set; }
        private Repository<tbLinkUserAndPlans> RepositoryLinkUserAndPlans { get; set; }
        private Repository<tbLinkServerGroupWithUsers> RepositoryLinkUserGroup { get; set; }
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
            RepositoryOrders = new Repository<tbOrders>(db);
            RepositoryLinkUserAndPlans = new Repository<tbLinkUserAndPlans>(db);
            RepositoryLinkUserGroup = new Repository<tbLinkServerGroupWithUsers>(db);
        }


        #region لیست کاربران

        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        [HttpPost]
        public ActionResult GetAll()
        {
            try
            {
                var dt = DataTablesRequest.Parse(Request);
                var agent = db.tbUsers.First(p => p.Username == User.Identity.Name);

                var query = db.tbTelegramUsers
                    .Include(t => t.tbTelegramUsers2)
                    .Where(t => t.FK_User_ID == agent.User_ID);

                if (!string.IsNullOrWhiteSpace(dt.SearchValue))
                {
                    var term = dt.SearchValue;
                    query = query.Where(t =>
                        (t.Tel_Username != null && t.Tel_Username.Contains(term)) ||
                        (t.Tel_FirstName != null && t.Tel_FirstName.Contains(term)) ||
                        (t.Tel_LastName != null && t.Tel_LastName.Contains(term)));
                }

                var totalRecords = query.Count();

                switch (dt.SortColumnIndex)
                {
                    case 1:
                        query = dt.IsAscending
                            ? query.OrderBy(t => t.Tel_UserID)
                            : query.OrderByDescending(t => t.Tel_UserID);
                        break;
                    case 2:
                        query = dt.IsAscending
                            ? query.OrderBy(t => t.Tel_Username)
                            : query.OrderByDescending(t => t.Tel_Username);
                        break;
                    case 3:
                        query = dt.IsAscending
                            ? query.OrderBy(t => t.Tel_Wallet)
                            : query.OrderByDescending(t => t.Tel_Wallet);
                        break;
                    case 4:
                        query = dt.IsAscending
                            ? query.OrderBy(t => t.Tel_Status)
                            : query.OrderByDescending(t => t.Tel_Status);
                        break;
                    default:
                        query = dt.IsAscending
                            ? query.OrderBy(t => t.Tel_UserID)
                            : query.OrderByDescending(t => t.Tel_UserID);
                        break;
                }

                var pageSize = dt.Length > 0 ? dt.Length : 10;
                var page = query.Skip(dt.Start).Take(pageSize).ToList();

                var data = page.Select(item => new TelegramUsersResponseViewModel
                {
                    id = item.Tel_UserID,
                    Username = item.Tel_Username,
                    FullName = item.Tel_FirstName + " " + item.Tel_LastName,
                    InviteUser = item.Tel_Parent_ID != null ? item.tbTelegramUsers2?.Tel_Username : null,
                    Invited = item.Tel_Parent_ID != null ? 1 : 0,
                    SumBuy = item.tbOrders.Where(o => o.Order_Price.HasValue).Sum(o => o.Order_Price.Value).ConvertToMony(),
                    Wallet = item.Tel_Wallet.HasValue ? item.Tel_Wallet.Value.ConvertToMony() : "0",
                    Status = item.Tel_Status,
                    Profile = item.Tel_UniqUserID + ".jpg"
                }).ToList();

                return Json(new
                {
                    draw = dt.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دریافت لیست مشترکین");
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<TelegramUsersResponseViewModel>() }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        [HttpGet]
        public ActionResult BanUser(int id)
        {
            try
            {
                var agent = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
                if (agent == null)
                    return MessageBox.Warning("ناموفق", "کاربر یافت نشد");

                var telUser = RepositoryTelegramUsers
                    .Where(t => t.Tel_UserID == id && t.FK_User_ID == agent.User_ID).FirstOrDefault();

                if (telUser == null)
                    return MessageBox.Warning("ناموفق", "مشترک یافت نشد");

                telUser.Tel_Status = telUser.Tel_Status == 0 ? 1 : 0;
                RepositoryTelegramUsers.Save();

                var state = telUser.Tel_Status == 0 ? "مسدود" : "فعال";
                logger.Info("وضعیت مشترک تلگرام " + telUser.Tel_Username + " به " + state + " تغییر یافت");
                return Toaster.Success("موفق", "کاربر با موفقیت " + state + " شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در تغییر وضعیت مشترک تلگرام");
                return MessageBox.Error("ناموفق", "خطا در تغییر وضعیت کاربر");
            }
        }

        #endregion


        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult Details(int user_id, string type = "accounts")
        {
            ViewBag.Type = type;
            return View(user_id);
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult _UserCard(int user_id)
        {
            var user = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();
            var TelegramUser = RepositoryTelegramUsers.Where(p => p.Tel_UserID == user_id && p.FK_User_ID == user.User_ID).FirstOrDefault();

            return PartialView(TelegramUser);
        }
        [AuthorizeApp(Roles = "1,2,3,4")]
        [HttpPost]
        public ActionResult GetOrders()
        {
            try
            {
                var userIdStr = Request.Form.GetValues("user_id")?.FirstOrDefault();
                if (!int.TryParse(userIdStr, out var user_id))
                    return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<OrderResponseViewModel>() }, JsonRequestBehavior.AllowGet);

                var dt = DataTablesRequest.Parse(Request);
                var user = db.tbUsers.First(s => s.Username == User.Identity.Name);

                var query = db.tbOrders
                    .Include(o => o.tbTelegramUsers)
                    .Where(p => p.FK_Tel_UserID == user_id && p.tbTelegramUsers.FK_User_ID == user.User_ID);

                if (!string.IsNullOrWhiteSpace(dt.SearchValue))
                {
                    var term = dt.SearchValue;
                    query = query.Where(p => p.AccountName != null && p.AccountName.Contains(term));
                }

                var totalRecords = query.Count();

                query = dt.IsAscending
                    ? query.OrderBy(p => p.OrderDate)
                    : query.OrderByDescending(p => p.OrderDate);

                var pageSize = dt.Length > 0 ? dt.Length : 10;
                var page = query.Skip(dt.Start).Take(pageSize).ToList();

                var data = page.Select(item =>
                {
                    var model = new OrderResponseViewModel();
                    model.Status = OrderStatusHelper.GetOrderDisplayStatus(item.OrderStatus, item.OrderDate);
                    model.OrderStatus = item.OrderStatus;

                    model.CreateDate = item.OrderDate.HasValue ? item.OrderDate.Value.ConvertDateTimeToShamsi2() : "-";
                    model.Plan = item.Traffic + " گیگ " + item.Month + " ماهه";
                    model.SubName = item.AccountName?.Split('@')[0] ?? "-";
                    model.Price = item.Order_Price.HasValue ? item.Order_Price.Value.ConvertToMony() : "0";
                    model.OrderId = item.Order_ID;
                    if (model.Status == OrderStatusHelper.DisplayFinished && item.Tel_RenewedDate != null)
                        model.ActiveDate = item.Tel_RenewedDate.Value.ConvertDateTimeToShamsi2();
                    return model;
                }).ToList();

                return Json(new
                {
                    draw = dt.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دریافت سفارشات مشترک");
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<OrderResponseViewModel>() }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        [HttpPost]
        public async Task<ActionResult> GetAccounts()
        {
            try
            {
                var userIdStr = Request.Form.GetValues("user_id")?.FirstOrDefault();
                if (!int.TryParse(userIdStr, out var user_id))
                    return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<AccountsViewModel>() }, JsonRequestBehavior.AllowGet);

                var dt = DataTablesRequest.Parse(Request);
                var agent = db.tbUsers.First(p => p.Username == User.Identity.Name);
                var telUser = db.tbTelegramUsers.FirstOrDefault(t => t.Tel_UserID == user_id && t.FK_User_ID == agent.User_ID);
                if (telUser == null)
                    return Json(new { draw = dt.Draw, recordsTotal = 0, recordsFiltered = 0, data = new List<AccountsViewModel>() }, JsonRequestBehavior.AllowGet);

                var query = db.tbLinks.Where(p => p.FK_TelegramUserID == user_id);

                if (!string.IsNullOrWhiteSpace(dt.SearchValue))
                {
                    var term = dt.SearchValue;
                    query = query.Where(p => p.tbL_Email != null && p.tbL_Email.Contains(term));
                }

                var totalRecords = query.Count();

                query = dt.IsAscending
                    ? query.OrderBy(p => p.tbLink_ID)
                    : query.OrderByDescending(p => p.tbLink_ID);

                var pageSize = dt.Length > 0 ? dt.Length : 10;
                var links = query.Skip(dt.Start).Take(pageSize).ToList();

                var accounts = new List<AccountsViewModel>();
                using (var mysql = new MySqlEntities(agent.tbServers.ConnectionString))
                {
                    await mysql.OpenAsync();
                    foreach (var link in links)
                    {
                        var account = await BuildAccountFromLinkAsync(mysql, link);
                        if (account != null)
                            accounts.Add(account);
                    }
                    await mysql.CloseAsync();
                }

                return Json(new
                {
                    draw = dt.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = accounts
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دریافت اشتراک‌های مشترک");
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<AccountsViewModel>() }, JsonRequestBehavior.AllowGet);
            }
        }

        private static async Task<AccountsViewModel> BuildAccountFromLinkAsync(MySqlEntities mysql, tbLinks link)
        {
            var reader = await mysql.GetDataAsync("select * from v2_user where email='" + link.tbL_Email + "'");
            if (!await reader.ReadAsync())
            {
                reader.Close();
                return null;
            }

            var account = new AccountsViewModel
            {
                LinkID = link.tbLink_ID,
                V2UserId = reader.GetInt32("id"),
                Email = link.tbL_Email,
                V2boardUsername = link.tbL_Email.Split('@')[0],
                State = 1,
                TotalVolume = Utility.ConvertByteToGB(reader.GetInt64("transfer_enable")) + " GB"
            };

            var exp = reader["expired_at"]?.ToString();
            if (!string.IsNullOrEmpty(exp))
            {
                var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp));
                account.ExpireDate = Utility.ConvertDatetimeToShamsiDate(ex);
                if (ex <= DateTime.Now)
                    account.State = 2;
            }
            else
            {
                account.ExpireDate = "بدون تاریخ انقضا";
            }

            var u = reader.GetInt64("u");
            var d = reader.GetInt64("d");
            account.UsedVolume = Math.Round(Utility.ConvertByteToGB(u + d), 2) + " GB";

            var vol = reader.GetInt64("transfer_enable") - (u + d);
            if (vol <= 0)
                account.State = 3;
            else if (Convert.ToBoolean(reader.GetSByte("banned")))
                account.State = 4;

            account.RemainingVolume = Math.Round(Utility.ConvertByteToGB(vol), 2) + " GB";
            reader.Close();
            return account;
        }

        #region ارسال پیام همگانی

        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult _PartialPublicMessage()
        {
            return PartialView();
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
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
                            botClient.SendMessage(item.Tel_UniqUserID, message);
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
        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult _EditWallet(int user_id)
        {
            var us = RepositoryTelegramUsers.Where(p => p.Tel_UserID == user_id).FirstOrDefault();
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
        [ValidateAntiForgeryToken]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult EditWallet(int user_id, string userDeposit)
        {
            var us = RepositoryTelegramUsers.Where(p => p.Tel_UserID == user_id).FirstOrDefault();
            if (us != null)
            {
                try
                {
                    int num = int.Parse(userDeposit, System.Globalization.NumberStyles.Currency);
                    us.Tel_Wallet = num;
                }
                catch
                {
                    return MessageBox.Warning("هشدار", "لطفا مقدار را صحیح وارد کنید");
                }
                RepositoryUser.Save();
                logger.Info("شارژ کیف پول کاربر تلگرام تغییر کرد");
                return MessageBox.Success("موفق", "کیف پول کاربر با موفقیت شارژ شد");
            }
            else
            {
                return Content("2");
            }
        }

        #endregion

        #region پیام به کاربر

        [HttpGet]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult _SendMessage(int id)
        {
            var TelegramUser = RepositoryTelegramUsers.Where(p => p.Tel_UserID == id).FirstOrDefault();
            ViewBag.id = id;
            ViewBag.name = TelegramUser.Tel_FirstName + TelegramUser.Tel_LastName;
            return PartialView();
        }

        [HttpPost]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult SendMessage(string message, int id)
        {

            try
            {
                var TelegramUser = RepositoryTelegramUsers.Where(p => p.Tel_UserID == id).FirstOrDefault();
                if (TelegramUser != null)
                {

                    var server = RepositoryServers.Where(p => p.Robot_ID == TelegramUser.Tel_RobotID).First();

                    TelegramBotClient bot = new TelegramBotClient(server.Robot_Token);
                    bot.SendMessage(TelegramUser.Tel_UniqUserID, message);
                    bot.Close();
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

        #region لیست اشتراک های کاربر تلگرام

        #endregion

        #region انتقال اشتراک
        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult _MoveAccount(int LinkID)
        {
            var agent = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            var link = RepositoryLinks.Where(p => p.tbLink_ID == LinkID).FirstOrDefault();
            if (agent == null || link == null)
                return Content("");

            var telegramUsers = RepositoryTelegramUsers
                .Where(p => p.FK_User_ID == agent.User_ID && p.Tel_UserID != link.FK_TelegramUserID)
                .ToList();

            ViewBag.LinkID = LinkID;
            return PartialView(telegramUsers);
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult GetMoveAccountTargets(int linkId, int currentUserId)
        {
            var agent = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            var link = RepositoryLinks.Where(p => p.tbLink_ID == linkId).FirstOrDefault();
            if (agent == null || link == null)
                return Json(new { data = new object[0] }, JsonRequestBehavior.AllowGet);

            var source = RepositoryTelegramUsers.Where(t =>
                t.Tel_UserID == link.FK_TelegramUserID && t.FK_User_ID == agent.User_ID).FirstOrDefault();
            if (source == null)
                return Json(new { data = new object[0] }, JsonRequestBehavior.AllowGet);

            var targets = RepositoryTelegramUsers
                .Where(p => p.FK_User_ID == agent.User_ID && p.Tel_UserID != currentUserId)
                .OrderBy(p => p.Tel_FirstName)
                .ThenBy(p => p.Tel_LastName)
                .ToList()
                .Select(p =>
                {
                    var fullName = ((p.Tel_FirstName ?? "") + " " + (p.Tel_LastName ?? "")).Trim();
                    var label = string.IsNullOrEmpty(fullName)
                        ? p.Tel_Username
                        : fullName + " (" + p.Tel_Username + ")";
                    return new { id = p.Tel_UserID, label };
                })
                .ToList();

            return Json(new { data = targets }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult MoveAccount(int linkId, int targetTelUserId)
        {
            try
            {
                var agent = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
                var link = RepositoryLinks.Where(p => p.tbLink_ID == linkId).FirstOrDefault();
                if (agent == null || link == null)
                    return MessageBox.Warning("هشدار", "اشتراک یافت نشد");

                var sourceTel = RepositoryTelegramUsers.Where(t =>
                    t.Tel_UserID == link.FK_TelegramUserID && t.FK_User_ID == agent.User_ID).FirstOrDefault();
                if (sourceTel == null)
                    return MessageBox.Warning("هشدار", "دسترسی به این اشتراک مجاز نیست");

                var targetTel = RepositoryTelegramUsers.Where(t =>
                    t.Tel_UserID == targetTelUserId && t.FK_User_ID == agent.User_ID).FirstOrDefault();
                if (targetTel == null)
                    return MessageBox.Warning("هشدار", "مشترک مقصد یافت نشد");

                if (targetTelUserId == link.FK_TelegramUserID)
                    return MessageBox.Warning("هشدار", "اشتراک هم‌اکنون متعلق به این مشترک است");

                link.FK_TelegramUserID = targetTelUserId;

                var orders = RepositoryOrders.Where(o => o.AccountName == link.tbL_Email).ToList();
                foreach (var order in orders)
                    order.FK_Tel_UserID = targetTelUserId;

                RepositoryLinks.Save();
                logger.Info("اشتراک " + link.tbL_Email + " به مشترک " + targetTel.Tel_Username + " منتقل شد");
                return Toaster.Success("موفق", "اشتراک با موفقیت منتقل شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در انتقال اشتراک");
                return MessageBox.Error("ناموفق", "خطا در انتقال اشتراک");
            }
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> DeleteTelegramAccount(int linkId)
        {
            try
            {
                var agent = await RepositoryUser.FirstOrDefaultAsync(p => p.Username == User.Identity.Name);
                var link = await RepositoryLinks.FirstOrDefaultAsync(p => p.tbLink_ID == linkId);
                if (agent == null || link == null)
                    return MessageBox.Warning("هشدار", "اشتراک یافت نشد");

                var owner = await RepositoryTelegramUsers.FirstOrDefaultAsync(t =>
                    t.Tel_UserID == link.FK_TelegramUserID && t.FK_User_ID == agent.User_ID);
                if (owner == null)
                    return MessageBox.Warning("هشدار", "دسترسی به این اشتراک مجاز نیست");

                var deleted = await TelegramSubscriptionHelper.DeleteSubscriptionByLinkAsync(
                    link, agent, RepositoryLinks, RepositoryOrders, RepositoryLogs,
                    RepositoryUser, RepositoryPlans, RepositoryLinkUserAndPlans, RepositoryLinkUserGroup);

                if (!deleted)
                    return MessageBox.Warning("هشدار",
                        "تا پایان بسته فعال امکان حذف وجود ندارد؛ فقط اشتراک‌های تازه‌ساخته (کمتر از ۱ روز و کمتر از ۱ گیگ مصرف) قابل حذف با بازگشت هزینه هستند.");

                logger.Info("اشتراک تلگرام " + link.tbL_Email + " حذف شد");
                return Toaster.Success("موفق", "اشتراک با موفقیت حذف شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در حذف اشتراک تلگرام");
                return MessageBox.Error("ناموفق", "خطا در حذف اشتراک");
            }
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> CancelReservedOrder(int orderId)
        {
            try
            {
                var agent = await RepositoryUser.FirstOrDefaultAsync(p => p.Username == User.Identity.Name);
                if (agent == null)
                    return Toaster.Error("ناموفق", "کاربر یافت نشد");

                var order = await RepositoryOrders.table
                    .Include(o => o.tbLinkUserAndPlans)
                    .Include(o => o.tbLinkUserAndPlans.tbPlans)
                    .Include(o => o.tbTelegramUsers)
                    .Include(o => o.tbDepositWallet_Log)
                    .FirstOrDefaultAsync(o => o.Order_ID == orderId && o.OrderStatus == "FOR_RESERVE");

                if (order == null)
                    return MessageBox.Warning("هشدار", "سفارش رزرو یافت نشد");

                if (!order.FK_Tel_UserID.HasValue)
                    return MessageBox.Warning("هشدار", "این سفارش به مشترک تلگرام متصل نیست");

                var telOwner = await RepositoryTelegramUsers.FirstOrDefaultAsync(t =>
                    t.Tel_UserID == order.FK_Tel_UserID.Value && t.FK_User_ID == agent.User_ID);
                if (telOwner == null)
                    return MessageBox.Warning("هشدار", "دسترسی به این سفارش مجاز نیست");

                // Ensure navigation is set for Tel_Wallet refund
                if (order.tbTelegramUsers == null)
                    order.tbTelegramUsers = telOwner;

                // ادمین سیستم: فقط حذف رزرو، بدون برگشت کیف
                int? refundAmount = 0;
                double? telRefund = null;
                if ((agent.Role ?? 0) != 1)
                {
                    refundAmount = await ReservedPackageHelper.RefundReservedOrderWalletAsync(
                        order, RepositoryUser, RepositoryPlans, RepositoryLinkUserAndPlans, RepositoryLinkUserGroup);
                    if (!refundAmount.HasValue)
                        return Toaster.Error("ناموفق", "خطا در بازگشت مبلغ نماینده");

                    telRefund = ReservedPackageHelper.RefundTelegramWalletForReservedOrder(order);
                }

                ReservedPackageHelper.RemoveReservePackageLogs(RepositoryLogs, order);
                RepositoryOrders.Delete(order);
                RepositoryOrders.Save();

                logger.Info("رزرو سفارش " + orderId + " از پروفایل مشترک لغو شد");
                if ((refundAmount ?? 0) > 0 || telRefund.HasValue)
                    return Toaster.Success("موفق", "رزرو لغو شد و مبلغ بازگشت داده شد");
                return Toaster.Success("موفق", "رزرو با موفقیت لغو شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در لغو رزرو از پروفایل مشترک");
                return Toaster.Error("ناموفق", "خطا در لغو رزرو");
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                RepositoryUser.Dispose();
                RepositoryPlans.Dispose();
                RepositoryLogs.Dispose();
                RepositoryServers.Dispose();
                RepositoryLinks.Dispose();
                RepositoryOrders.Dispose();
                RepositoryLinkUserAndPlans.Dispose();
                RepositoryLinkUserGroup.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}