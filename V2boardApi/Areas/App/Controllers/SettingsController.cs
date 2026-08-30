using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data.SettingsViewModels;
using V2boardApi.Areas.api.Data.ViewModels;
using V2boardApi.Tools;
using NLogEntity = DataLayer.DomainModel.NLog;

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
            ViewBag.AdminUsername = user?.tbBotSettings?.FirstOrDefault()?.AdminUsername;

            if (user?.tbServers == null)
                return View(new tbServers());

            return View(user.tbServers);
        }

        [AuthorizeApp(Roles = "1")]
        public ActionResult SystemLogs()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1")]
        public ActionResult AlertLogs()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1")]
        public ActionResult AppRelease()
        {
            var row = db.tbAppRelease.AsNoTracking().FirstOrDefault();
            var items = AppReleaseViewModel.ParseItems(row != null ? row.tbAr_Changelog : null);
            if (items.Count == 0)
                items.Add("");

            AppReleaseEditViewModel model = new AppReleaseEditViewModel();
            model.DownloadUrl = row != null ? row.tbAr_DownloadUrl : null;
            model.Version = row != null ? row.tbAr_Version : null;
            model.VersionCode = row != null ? row.tbAr_VersionCode : null;
            model.ForceInstall = row != null && row.tbAr_ForceInstall;
            model.ChangelogItems = items;
            return View(model);
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
        public ActionResult SaveBotServerSettings(double? Discount_Percent, long? AdminTelegramUniqID, string Channel_ID, string BotbaseAddress, string Robot_Token, string Robot_ID, long? BotID, string AdminUsername = null)
        {
            try
            {
                var user = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
                var server = user?.tbServers;
                if (server == null)
                    return Json(new { status = "danger", message = "سرور یافت نشد" }, JsonRequestBehavior.AllowGet);

                server.Discount_Percent = Discount_Percent;
                server.AdminTelegramUniqID = AdminTelegramUniqID;
                server.Channel_ID = Channel_ID?.Trim();
                server.BotbaseAddress = BotbaseAddress?.Trim();
                server.Robot_Token = Robot_Token?.Trim();
                server.Robot_ID = Robot_ID?.Trim();
                server.BotID = BotID;

                // یوزرنیم تلگرام ادمین روی tbBotSettings کاربر Role=1 ذخیره می‌شود (بدون @)
                var botSetting = user.tbBotSettings.FirstOrDefault();
                if (botSetting != null)
                    botSetting.AdminUsername = AdminUsername?.Trim().TrimStart('@');

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

        #region نسخه اپلیکیشن

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeApp(Roles = "1")]
        public ActionResult SaveAppRelease(string DownloadUrl, string Version, int? VersionCode, string[] Changelog, bool ForceInstall = false)
        {
            try
            {
                DownloadUrl = NullIfEmpty(DownloadUrl, 500);
                Version = NullIfEmpty(Version, 30);
                var changelogJson = AppReleaseViewModel.SerializeItems(Changelog);

                if (ForceInstall && string.IsNullOrEmpty(DownloadUrl))
                    return Json(new { status = "warning", message = "برای نصب اجباری لینک دانلود لازم است" }, JsonRequestBehavior.AllowGet);

                if (ForceInstall && string.IsNullOrEmpty(Version) && !VersionCode.HasValue)
                    return Json(new { status = "warning", message = "برای نصب اجباری نسخه یا شماره بیلد لازم است" }, JsonRequestBehavior.AllowGet);

                if (!string.IsNullOrEmpty(DownloadUrl)
                    && !DownloadUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    && !DownloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return Json(new { status = "warning", message = "لینک دانلود باید با http یا https شروع شود" }, JsonRequestBehavior.AllowGet);

                var row = db.tbAppRelease.FirstOrDefault();
                if (row == null)
                {
                    row = new tbAppRelease();
                    db.tbAppRelease.Add(row);
                }

                row.tbAr_DownloadUrl = DownloadUrl;
                row.tbAr_Version = Version;
                row.tbAr_VersionCode = VersionCode;
                row.tbAr_Changelog = changelogJson;
                row.tbAr_ForceInstall = ForceInstall;
                row.tbAr_UpdatedAt = DateTime.Now;

                db.SaveChanges();
                logger.Info("تنظیمات نسخه اپلیکیشن ذخیره شد");
                return Json(new { status = "success", message = "تنظیمات نسخه اپلیکیشن ذخیره شد" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ذخیره تنظیمات نسخه اپلیکیشن با خطا مواجه شد");
                return Json(new { status = "danger", message = "ذخیره تنظیمات با خطا مواجه شد" }, JsonRequestBehavior.AllowGet);
            }
        }

        private static string NullIfEmpty(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        #endregion

        #region لاگ سیستم (NLog)

        [HttpPost]
        [AuthorizeApp(Roles = "1")]
        public ActionResult GetNLogs()
        {
            try
            {
                var dt = DataTablesRequest.Parse(Request);
                IQueryable<NLogEntity> query = db.NLog.AsNoTracking();

                query = ApplyNLogFilters(query, Request);

                if (!string.IsNullOrWhiteSpace(dt.SearchValue))
                {
                    var term = dt.SearchValue;
                    query = query.Where(p =>
                        (p.Message != null && p.Message.Contains(term)) ||
                        (p.Logger != null && p.Logger.Contains(term)) ||
                        (p.userName != null && p.userName.Contains(term)) ||
                        (p.ipAddress != null && p.ipAddress.Contains(term)) ||
                        (p.controllerName != null && p.controllerName.Contains(term)) ||
                        (p.actionName != null && p.actionName.Contains(term)) ||
                        (p.Exception != null && p.Exception.Contains(term)) ||
                        (p.customData != null && p.customData.Contains(term)));
                }

                var totalRecords = query.Count();

                switch (dt.SortColumnIndex)
                {
                    case 1:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.Level)
                            : query.OrderByDescending(p => p.Level);
                        break;
                    case 3:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.Logger)
                            : query.OrderByDescending(p => p.Logger);
                        break;
                    case 5:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.userName)
                            : query.OrderByDescending(p => p.userName);
                        break;
                    case 6:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.ipAddress)
                            : query.OrderByDescending(p => p.ipAddress);
                        break;
                    case 7:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.controllerName)
                            : query.OrderByDescending(p => p.controllerName);
                        break;
                    default:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.Logged)
                            : query.OrderByDescending(p => p.Logged);
                        break;
                }

                var pageSize = dt.Length > 0 ? dt.Length : 10;
                var rows = query.Skip(dt.Start).Take(pageSize)
                    .Select(p => new
                    {
                        p.ID,
                        p.Logged,
                        p.Level,
                        p.Message,
                        p.Logger,
                        p.userName,
                        p.ipAddress,
                        p.httpMethod,
                        p.controllerName,
                        p.actionName,
                        p.executionTime,
                        HasException = p.Exception != null && p.Exception != ""
                    })
                    .ToList();

                var data = rows.Select(p => new NLogListItemViewModel
                {
                    Id = p.ID,
                    Level = p.Level ?? "",
                    Logged = p.Logged.ConvertDateTimeToShamsi2(),
                    Logger = p.Logger ?? "",
                    Message = TruncateLogText(p.Message, 180),
                    UserName = p.userName ?? "",
                    IpAddress = p.ipAddress ?? "",
                    HttpMethod = p.httpMethod ?? "",
                    Controller = p.controllerName ?? "",
                    Action = p.actionName ?? "",
                    ExecutionTime = p.executionTime ?? "",
                    HasException = p.HasException
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
                logger.Error(ex, "نمایش لاگ‌های سیستم با خطا مواجه شد");
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<NLogListItemViewModel>() }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public ActionResult GetNLogDetail(int id)
        {
            var log = db.NLog.AsNoTracking().FirstOrDefault(p => p.ID == id);
            if (log == null)
                return Content("<p class='text-danger mb-0'>لاگ یافت نشد</p>");

            return PartialView("_NLogDetail", log);
        }

        private static IQueryable<NLogEntity> ApplyNLogFilters(IQueryable<NLogEntity> query, HttpRequestBase request)
        {
            var filterLevel = request.Form["filterLevel"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterLevel))
                query = query.Where(p => p.Level == filterLevel);

            var filterLogger = request.Form["filterLogger"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterLogger))
                query = query.Where(p => p.Logger != null && p.Logger.Contains(filterLogger));

            var filterUserName = request.Form["filterUserName"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterUserName))
                query = query.Where(p => p.userName != null && p.userName.Contains(filterUserName));

            var filterIp = request.Form["filterIp"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterIp))
                query = query.Where(p => p.ipAddress != null && p.ipAddress.Contains(filterIp));

            var filterController = request.Form["filterController"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterController))
                query = query.Where(p => p.controllerName != null && p.controllerName.Contains(filterController));

            var filterAction = request.Form["filterAction"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterAction))
                query = query.Where(p => p.actionName != null && p.actionName.Contains(filterAction));

            var filterHttpMethod = request.Form["filterHttpMethod"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterHttpMethod))
                query = query.Where(p => p.httpMethod == filterHttpMethod);

            var filterMessage = request.Form["filterMessage"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterMessage))
                query = query.Where(p => p.Message != null && p.Message.Contains(filterMessage));

            var filterHasException = request.Form["filterHasException"]?.Trim();
            if (filterHasException == "1")
                query = query.Where(p => p.Exception != null && p.Exception != "");
            else if (filterHasException == "0")
                query = query.Where(p => p.Exception == null || p.Exception == "");

            DateTime fromDate;
            if (Utility.TryParseShamsiDate(request.Form["filterFromDate"], out fromDate))
                query = query.Where(p => p.Logged >= fromDate);

            DateTime toDate;
            if (Utility.TryParseShamsiDate(request.Form["filterToDate"], out toDate))
            {
                var toEnd = toDate.Date.AddDays(1);
                query = query.Where(p => p.Logged < toEnd);
            }

            return query;
        }

        private static string TruncateLogText(string value, int max)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var compact = value.Replace("\r", " ").Replace("\n", " ");
            return compact.Length <= max ? compact : compact.Substring(0, max) + "...";
        }

        #endregion

        #region لاگ ارسال هشدارها

        [HttpPost]
        [AuthorizeApp(Roles = "1")]
        public ActionResult GetAlertSendLogs()
        {
            try
            {
                var dt = DataTablesRequest.Parse(Request);
                IQueryable<tbAlertSendLogs> query = db.tbAlertSendLogs.AsNoTracking();

                var filterRecipient = Request.Form["filterAlertRecipient"]?.Trim();
                if (!string.IsNullOrWhiteSpace(filterRecipient))
                    query = query.Where(p =>
                        (p.tbAsl_RecipientName != null && p.tbAsl_RecipientName.Contains(filterRecipient)) ||
                        (p.tbAsl_ChatId != null && p.tbAsl_ChatId.Contains(filterRecipient)));

                var filterAlertType = Request.Form["filterAlertType"]?.Trim();
                if (!string.IsNullOrWhiteSpace(filterAlertType))
                    query = query.Where(p => p.tbAsl_AlertType != null && p.tbAsl_AlertType.Contains(filterAlertType));

                var filterStatus = Request.Form["filterAlertStatus"]?.Trim();
                if (filterStatus == "1")
                    query = query.Where(p => p.tbAsl_IsSuccess);
                else if (filterStatus == "0")
                    query = query.Where(p => !p.tbAsl_IsSuccess);

                DateTime fromDate;
                if (Utility.TryParseShamsiDate(Request.Form["filterAlertFromDate"], out fromDate))
                    query = query.Where(p => p.tbAsl_SentAt >= fromDate);

                DateTime toDate;
                if (Utility.TryParseShamsiDate(Request.Form["filterAlertToDate"], out toDate))
                {
                    var toEnd = toDate.Date.AddDays(1);
                    query = query.Where(p => p.tbAsl_SentAt < toEnd);
                }

                if (!string.IsNullOrWhiteSpace(dt.SearchValue))
                {
                    var term = dt.SearchValue;
                    query = query.Where(p =>
                        (p.tbAsl_RecipientName != null && p.tbAsl_RecipientName.Contains(term)) ||
                        (p.tbAsl_AlertType != null && p.tbAsl_AlertType.Contains(term)) ||
                        (p.tbAsl_Message != null && p.tbAsl_Message.Contains(term)) ||
                        (p.tbAsl_ChatId != null && p.tbAsl_ChatId.Contains(term)) ||
                        (p.tbAsl_Error != null && p.tbAsl_Error.Contains(term)));
                }

                var totalRecords = query.Count();

                switch (dt.SortColumnIndex)
                {
                    case 1:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbAsl_RecipientName)
                            : query.OrderByDescending(p => p.tbAsl_RecipientName);
                        break;
                    case 3:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbAsl_AlertType)
                            : query.OrderByDescending(p => p.tbAsl_AlertType);
                        break;
                    case 5:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbAsl_IsSuccess)
                            : query.OrderByDescending(p => p.tbAsl_IsSuccess);
                        break;
                    default:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbAsl_SentAt)
                            : query.OrderByDescending(p => p.tbAsl_SentAt);
                        break;
                }

                var pageSize = dt.Length > 0 ? dt.Length : 10;
                var rows = query.Skip(dt.Start).Take(pageSize).ToList();

                var data = rows.Select(p => new AlertSendLogListItemViewModel
                {
                    Id = p.tbAsl_ID,
                    Recipient = p.tbAsl_RecipientName ?? "—",
                    ChatId = p.tbAsl_ChatId ?? "",
                    SentAt = p.tbAsl_SentAt.ConvertDateTimeToShamsi2(),
                    AlertType = p.tbAsl_AlertType ?? "",
                    Message = TruncateLogText(p.tbAsl_Message, 140),
                    MessageFull = p.tbAsl_Message ?? "",
                    IsSuccess = p.tbAsl_IsSuccess,
                    Error = p.tbAsl_Error ?? ""
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
                logger.Error(ex, "نمایش لاگ ارسال هشدارها با خطا مواجه شد");
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<AlertSendLogListItemViewModel>() }, JsonRequestBehavior.AllowGet);
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
