using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data.MobileUsersViewModels;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    /// <summary>
    /// مدیریت دستگاه هایی که اپلیکیشن روی آن ها نصب شده است.
    /// فعلا فقط ادمین (Role 1) به این بخش دسترسی دارد.
    /// </summary>
    [AuthorizeApp(Roles = "1")]
    [LogActionFilter]
    public class MobileUsersController : Controller
    {
        private Entities db;
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Repository<tbMobileUsers> RepositoryMobileUsers { get; set; }

        public MobileUsersController()
        {
            db = new Entities();
            RepositoryMobileUsers = new Repository<tbMobileUsers>(db);
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

                var query = db.tbMobileUsers.Include(p => p.tbUsers).AsQueryable();

                if (!string.IsNullOrWhiteSpace(dt.SearchValue))
                {
                    var term = dt.SearchValue;
                    query = query.Where(p =>
                        p.tbMu_AndroidId.Contains(term) ||
                        (p.tbMu_Model != null && p.tbMu_Model.Contains(term)) ||
                        (p.tbMu_Manufacturer != null && p.tbMu_Manufacturer.Contains(term)) ||
                        (p.tbMu_AppVersion != null && p.tbMu_AppVersion.Contains(term)) ||
                        (p.tbUsers.Username != null && p.tbUsers.Username.Contains(term)));
                }

                query = ApplyCustomFilters(query, Request);

                var totalRecords = query.Count();

                switch (dt.SortColumnIndex)
                {
                    case 1:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbMu_Manufacturer).ThenBy(p => p.tbMu_Model)
                            : query.OrderByDescending(p => p.tbMu_Manufacturer).ThenByDescending(p => p.tbMu_Model);
                        break;
                    case 2:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbUsers.Username)
                            : query.OrderByDescending(p => p.tbUsers.Username);
                        break;
                    case 3:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbMu_AppVersion)
                            : query.OrderByDescending(p => p.tbMu_AppVersion);
                        break;
                    case 5:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbMu_RegisterDate)
                            : query.OrderByDescending(p => p.tbMu_RegisterDate);
                        break;
                    default:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbMu_LastSeenDate)
                            : query.OrderByDescending(p => p.tbMu_LastSeenDate);
                        break;
                }

                var pageSize = dt.Length > 0 ? dt.Length : 10;
                var page = query.Skip(dt.Start).Take(pageSize).ToList();

                var data = page.Select(item => new MobileUsersResponseModel
                {
                    Id = item.tbMu_ID,
                    Device = BuildDeviceName(item),
                    Agent = item.tbUsers == null ? "-" : item.tbUsers.Username,
                    AndroidId = item.tbMu_AndroidId,
                    AppVersion = item.tbMu_AppVersion ?? "-",
                    AndroidVersion = item.tbMu_AndroidVersion ?? "-",
                    Language = BuildLocale(item),
                    RegisterDate = FormatDate(item.tbMu_RegisterDate),
                    LastSeenDate = FormatDate(item.tbMu_LastSeenDate),
                    PushReady = item.tbMu_NotificationEnabled && !string.IsNullOrWhiteSpace(item.tbMu_FirebaseToken),
                    Rooted = item.tbMu_Rooted,
                    IsActive = item.tbMu_IsActive,
                    FactorCount = item.tbDepositWallet_Log.Count
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
                logger.Error(ex, "در نمایش کاربران موبایل با خطایی مواجه شدیم !");
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<MobileUsersResponseModel>() }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// فیلترهای اختصاصی صفحه : نماینده ، وضعیت آمادگی نوتیفیکیشن و فعال بودن دستگاه
        /// </summary>
        private static IQueryable<tbMobileUsers> ApplyCustomFilters(IQueryable<tbMobileUsers> query, System.Web.HttpRequestBase request)
        {
            var filterAgent = request.Form["filterAgent"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterAgent))
                query = query.Where(p => p.tbUsers.Username != null && p.tbUsers.Username.Contains(filterAgent));

            var filterDevice = request.Form["filterDevice"]?.Trim();
            if (!string.IsNullOrWhiteSpace(filterDevice))
            {
                query = query.Where(p =>
                    (p.tbMu_Model != null && p.tbMu_Model.Contains(filterDevice)) ||
                    (p.tbMu_Manufacturer != null && p.tbMu_Manufacturer.Contains(filterDevice)) ||
                    p.tbMu_AndroidId.Contains(filterDevice));
            }

            var filterPush = request.Form["filterPush"];
            if (filterPush == "1")
                query = query.Where(p => p.tbMu_NotificationEnabled && p.tbMu_FirebaseToken != null);
            else if (filterPush == "0")
                query = query.Where(p => !p.tbMu_NotificationEnabled || p.tbMu_FirebaseToken == null);

            var filterActive = request.Form["filterActive"];
            if (filterActive == "1")
                query = query.Where(p => p.tbMu_IsActive);
            else if (filterActive == "0")
                query = query.Where(p => !p.tbMu_IsActive);

            return query;
        }

        /// <summary>جزئیات کامل یک دستگاه برای نمایش در مودال</summary>
        [HttpGet]
        public ActionResult Details(int device_id)
        {
            try
            {
                var item = db.tbMobileUsers
                    .Include(p => p.tbUsers)
                    .FirstOrDefault(p => p.tbMu_ID == device_id);

                if (item == null)
                    return MessageBox.Warning("ناموفق", "دستگاهی با این شناسه یافت نشد");

                var paid = item.tbDepositWallet_Log
                    .Where(d => d.dw_Status == "FINISH" && d.dw_Price.HasValue)
                    .Sum(d => (double?)d.dw_Price.Value) ?? 0;

                var model = new MobileUserDetailsModel
                {
                    Id = item.tbMu_ID,
                    AndroidId = item.tbMu_AndroidId,
                    Agent = item.tbUsers == null ? "-" : item.tbUsers.Username,
                    BusinessName = item.tbUsers == null ? "-" : item.tbUsers.BussinesTitle,

                    Manufacturer = item.tbMu_Manufacturer ?? "-",
                    Model = item.tbMu_Model ?? "-",
                    Device = item.tbMu_Device ?? "-",
                    Product = item.tbMu_Product ?? "-",
                    Hardware = item.tbMu_Hardware ?? "-",

                    AndroidVersion = item.tbMu_AndroidVersion ?? "-",
                    Sdk = item.tbMu_Sdk,
                    AppVersion = item.tbMu_AppVersion ?? "-",
                    VersionCode = item.tbMu_VersionCode,
                    PackageName = item.tbMu_PackageName ?? "-",

                    Language = item.tbMu_Language ?? "-",
                    Country = item.tbMu_Country ?? "-",
                    Timezone = item.tbMu_Timezone ?? "-",

                    Screen = item.tbMu_ScreenWidth.HasValue && item.tbMu_ScreenHeight.HasValue
                        ? item.tbMu_ScreenWidth.Value + " × " + item.tbMu_ScreenHeight.Value
                        : "-",
                    Density = item.tbMu_Density,

                    NotificationEnabled = item.tbMu_NotificationEnabled,
                    HasFirebaseToken = !string.IsNullOrWhiteSpace(item.tbMu_FirebaseToken),
                    Rooted = item.tbMu_Rooted,
                    IsActive = item.tbMu_IsActive,

                    RegisterDate = FormatDate(item.tbMu_RegisterDate),
                    LastSeenDate = FormatDate(item.tbMu_LastSeenDate),
                    LastIp = item.tbMu_LastIp ?? "-",

                    FactorCount = item.tbDepositWallet_Log.Count,
                    OrderCount = item.tbOrders.Count,
                    TotalPaid = paid.ConvertToMony()
                };

                return PartialView("_DeviceDetails", model);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در نمایش جزئیات دستگاه " + device_id);
                return MessageBox.Error("ناموفق", "خطا در نمایش جزئیات دستگاه");
            }
        }

        /// <summary>
        /// فعال یا غیرفعال کردن دستگاه. دستگاه غیرفعال از لیست گیرندگان Push حذف می شود
        /// ولی رکورد و تاریخچه فاکتورهایش باقی می ماند.
        /// </summary>
        [HttpPost]
        public ActionResult ToggleActive(int device_id)
        {
            try
            {
                var item = RepositoryMobileUsers.Where(p => p.tbMu_ID == device_id).FirstOrDefault();
                if (item == null)
                    return MessageBox.Warning("ناموفق", "دستگاهی با این شناسه یافت نشد");

                item.tbMu_IsActive = !item.tbMu_IsActive;
                RepositoryMobileUsers.Save();

                return Toaster.Success("موفق", item.tbMu_IsActive ? "دستگاه فعال شد" : "دستگاه غیرفعال شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در تغییر وضعیت دستگاه " + device_id);
                return MessageBox.Error("ناموفق", "تغییر وضعیت دستگاه با خطا مواجه شد");
            }
        }

        private static string BuildDeviceName(tbMobileUsers item)
        {
            var name = ((item.tbMu_Manufacturer ?? "") + " " + (item.tbMu_Model ?? "")).Trim();
            return string.IsNullOrWhiteSpace(name) ? "نامشخص" : name;
        }

        private static string BuildLocale(tbMobileUsers item)
        {
            if (string.IsNullOrWhiteSpace(item.tbMu_Language))
                return "-";

            return string.IsNullOrWhiteSpace(item.tbMu_Country)
                ? item.tbMu_Language
                : item.tbMu_Language + "-" + item.tbMu_Country;
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ConvertDateTimeToShamsi2() : "-";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                RepositoryMobileUsers.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
