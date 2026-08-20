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
using V2boardApi.Areas.App.Data.BotFactoresViewModels;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [AuthorizeApp(Roles = "1,2,3,4")]
    [LogActionFilter]
    public class BotFactorsController : Controller
    {
        private Entities db;
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Repository<tbDepositWallet_Log> RepositoryDepositLog { get; set; }

        public BotFactorsController()
        {
            db = new Entities();
            RepositoryDepositLog = new Repository<tbDepositWallet_Log>(db);
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetAll()
        {
            try
            {
                var dt = DataTablesRequest.Parse(Request);
                var username = User.Identity.Name;

                // پسوند نام اشتراک های همین کاربر — فاکتورهای اپلیکیشن کاربر تلگرام ندارند
                // و اگر دستگاهشان هم ثبت نشده باشد فقط از همین راه به مالکشان می رسیم.
                var accountSuffix = "@" + username;

                var query = db.tbDepositWallet_Log
                    .Include(p => p.tbTelegramUsers)
                    .Include(p => p.tbPaymentMethods)
                    .Include(p => p.tbMobileUsers)
                    .Include(p => p.tbOrders)
                    .Where(p => p.tbTelegramUsers.tbUsers.Username == username
                             || (p.FK_PayMethod_ID == PaymentMethodIds.App
                                 && (p.tbMobileUsers.tbUsers.Username == username
                                     || (p.tbOrders.AccountName != null && p.tbOrders.AccountName.EndsWith(accountSuffix)))));

                if (!string.IsNullOrWhiteSpace(dt.SearchValue))
                {
                    var term = dt.SearchValue;
                    query = query.Where(p =>
                        (p.dw_TaxId != null && p.dw_TaxId.Contains(term)) ||
                        (p.tbTelegramUsers.Tel_Username != null && p.tbTelegramUsers.Tel_Username.Contains(term)) ||
                        (p.tbTelegramUsers.Tel_FirstName != null && p.tbTelegramUsers.Tel_FirstName.Contains(term)) ||
                        (p.tbTelegramUsers.Tel_LastName != null && p.tbTelegramUsers.Tel_LastName.Contains(term)) ||
                        (p.tbMobileUsers.tbMu_Model != null && p.tbMobileUsers.tbMu_Model.Contains(term)) ||
                        (p.tbMobileUsers.tbMu_Manufacturer != null && p.tbMobileUsers.tbMu_Manufacturer.Contains(term)) ||
                        (p.tbPaymentMethods.tbpm_MethodName != null && p.tbPaymentMethods.tbpm_MethodName.Contains(term)));
                }

                query = ApplyCustomFilters(query, Request);

                var totalRecords = query.Count();

                switch (dt.SortColumnIndex)
                {
                    case 1:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbTelegramUsers.Tel_Username)
                            : query.OrderByDescending(p => p.tbTelegramUsers.Tel_Username);
                        break;
                    case 3:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbPaymentMethods.tbpm_MethodName)
                            : query.OrderByDescending(p => p.tbPaymentMethods.tbpm_MethodName);
                        break;
                    case 4:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.dw_Status)
                            : query.OrderByDescending(p => p.dw_Status);
                        break;
                    case 5:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.dw_TaxId)
                            : query.OrderByDescending(p => p.dw_TaxId);
                        break;
                    case 6:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.dw_Price)
                            : query.OrderByDescending(p => p.dw_Price);
                        break;
                    default:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.dw_CreateDatetime)
                            : query.OrderByDescending(p => p.dw_CreateDatetime);
                        break;
                }

                var pageSize = dt.Length > 0 ? dt.Length : 10;
                var factors = query.Skip(dt.Start).Take(pageSize).ToList();

                var data = factors.Select(MapFactor).ToList();

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
                logger.Error(ex, "در نمایش فاکتور ها با خطایی مواجه شدیم !");
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<BotFactoresResponseModel>() }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// اعمال فیلترهای اختصاصی صفحه فاکتورها: کاربر، شماره پیگیری، بازه مبلغ و بازه تاریخ (شمسی).
        /// </summary>
        private static IQueryable<tbDepositWallet_Log> ApplyCustomFilters(IQueryable<tbDepositWallet_Log> query, HttpRequestBase request)
        {
            var filterUser = request.Form["filterUser"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterUser))
            {
                query = query.Where(p =>
                    (p.tbTelegramUsers.Tel_Username != null && p.tbTelegramUsers.Tel_Username.Contains(filterUser)) ||
                    (p.tbTelegramUsers.Tel_FirstName != null && p.tbTelegramUsers.Tel_FirstName.Contains(filterUser)) ||
                    (p.tbTelegramUsers.Tel_LastName != null && p.tbTelegramUsers.Tel_LastName.Contains(filterUser)) ||
                    (p.tbMobileUsers.tbMu_Model != null && p.tbMobileUsers.tbMu_Model.Contains(filterUser)) ||
                    (p.tbMobileUsers.tbMu_Manufacturer != null && p.tbMobileUsers.tbMu_Manufacturer.Contains(filterUser)));
            }

            var filterTaxId = request.Form["filterTaxId"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterTaxId))
                query = query.Where(p => p.dw_TaxId != null && p.dw_TaxId.Contains(filterTaxId));

            var amountMin = ParseFilterAmount(request.Form["filterAmountMin"]);
            if (amountMin.HasValue)
                query = query.Where(p => p.dw_Price.HasValue && p.dw_Price.Value >= amountMin.Value);

            var amountMax = ParseFilterAmount(request.Form["filterAmountMax"]);
            if (amountMax.HasValue)
                query = query.Where(p => p.dw_Price.HasValue && p.dw_Price.Value <= amountMax.Value);

            DateTime fromDate;
            if (Utility.TryParseShamsiDate(request.Form["filterFromDate"], out fromDate))
                query = query.Where(p => p.dw_CreateDatetime.HasValue && p.dw_CreateDatetime.Value >= fromDate);

            DateTime toDate;
            if (Utility.TryParseShamsiDate(request.Form["filterToDate"], out toDate))
            {
                var toEnd = toDate.Date.AddDays(1);
                query = query.Where(p => p.dw_CreateDatetime.HasValue && p.dw_CreateDatetime.Value < toEnd);
            }

            return query;
        }

        /// <summary>
        /// پارس مبلغ فیلتر با نرمال‌سازی ارقام فارسی/عربی و حذف جداکننده‌ها (مستقل از culture).
        /// </summary>
        private static double? ParseFilterAmount(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (var ch in raw.Trim())
            {
                if (ch >= '۰' && ch <= '۹')            // ارقام فارسی
                    sb.Append((char)('0' + (ch - '۰')));
                else if (ch >= '٠' && ch <= '٩')       // ارقام عربی
                    sb.Append((char)('0' + (ch - '٠')));
                else if (char.IsDigit(ch) || ch == '.')
                    sb.Append(ch);
                // بقیه (کاما، فاصله، ریال و ...) نادیده گرفته می‌شوند
            }

            double value;
            if (double.TryParse(sb.ToString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out value))
                return value;

            return null;
        }

        private static BotFactoresResponseModel MapFactor(tbDepositWallet_Log item)
        {
            var factor = new BotFactoresResponseModel
            {
                UserId = item.FK_TelegramUser_ID ?? 0,
                Id = item.dw_ID,
                TaxId = item.dw_TaxId,
                Date = item.dw_CreateDatetime.HasValue ? item.dw_CreateDatetime.Value.ConvertDateTimeToShamsi2() : "-",
                User = BuildUserTitle(item),
                Price = item.dw_Price.HasValue ? item.dw_Price.Value.ConvertToMony() : "0",
                PayMethod = item.tbPaymentMethods?.tbpm_MethodName ?? "-",
                IsMobile = item.FK_PayMethod_ID == PaymentMethodIds.App,
                DeviceId = item.FK_MobileUser_ID ?? 0
            };

            if (item.dw_Status == "FOR_PAY" || item.dw_Status == "EXPIRED")
                factor.Status = OrderStatusHelper.GetDepositDisplayStatus(item.dw_Status, item.dw_CreateDatetime);
            else if (item.dw_Status == "FINISH")
                factor.Status = OrderStatusHelper.DisplayFinished;

            return factor;
        }

        /// <summary>
        /// عنوان ستون کاربر. فاکتور اپلیکیشن کاربر تلگرام ندارد ، پس نام دستگاه
        /// (و در نبود دستگاه ثبت شده ، نام اشتراک) نمایش داده می شود.
        /// </summary>
        private static string BuildUserTitle(tbDepositWallet_Log item)
        {
            if (item.tbTelegramUsers != null)
            {
                return item.tbTelegramUsers.Tel_Username
                     + "(" + item.tbTelegramUsers.Tel_FirstName + " " + item.tbTelegramUsers.Tel_LastName + ")";
            }

            if (item.tbMobileUsers != null)
            {
                var device = (item.tbMobileUsers.tbMu_Manufacturer + " " + item.tbMobileUsers.tbMu_Model).Trim();
                return string.IsNullOrWhiteSpace(device) ? "دستگاه " + item.tbMobileUsers.tbMu_ID : device;
            }

            var account = item.tbOrders?.AccountName;
            if (string.IsNullOrWhiteSpace(account))
                return "-";

            var cut = account.IndexOfAny(new[] { '$', '@' });
            return cut >= 0 ? account.Substring(0, cut) : account;
        }

        [HttpPost]
        public async Task<ActionResult> Accept(int factor_id)
        {
            try
            {
                var User = JwtToken.GetUser_ID();
                var User_ID = Convert.ToInt32(User);

                // فاکتور اپلیکیشن مسیر تائید جداگانه دارد : کاربر تلگرام ندارد و
                // نباید هیچ پیامی به ربات تاییدیه ها فرستاده شود.
                var appFactor = RepositoryDepositLog
                    .Where(s => s.dw_ID == factor_id && s.FK_PayMethod_ID == PaymentMethodIds.App && s.dw_Status == "FOR_PAY")
                    .FirstOrDefault();

                if (appFactor != null)
                {
                    var appService = new AppInvoiceService();
                    var appResult = await appService.ConfirmAsync(factor_id, User_ID);
                    if (appResult.Success)
                        return Toaster.Success("موفق", appResult.Message);

                    return MessageBox.Warning("ناموفق", appResult.Message);
                }

                var factor = RepositoryDepositLog.Where(s => s.dw_ID == factor_id && s.tbTelegramUsers.FK_User_ID == User_ID && s.dw_Status == "FOR_PAY").FirstOrDefault();
                if (factor != null)
                {
                    TransactionHanderService service = new TransactionHanderService();
                    var res = await service.CheckOrder(factor.dw_Price.ToString(), factor.tbTelegramUsers.tbUsers.PhoneNumber);
                    if (res)
                        return Toaster.Success("موفق", "تراکنش با موفقیت تائید شد");

                    return MessageBox.Warning("ناموفق", "تائید تراکنش با خطا مواجه شد");
                }

                return MessageBox.Warning("ناموفق", "این تراکنش قبلا تائید شده");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "تائید فاکتور با خطا مواجه شد");
                return MessageBox.Error("ناموفق", "خطا در تائید فاکتور");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                RepositoryDepositLog.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
