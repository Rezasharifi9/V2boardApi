using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
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

                var query = db.tbDepositWallet_Log
                    .Include(p => p.tbTelegramUsers)
                    .Include(p => p.tbPaymentMethods)
                    .Where(p => p.tbTelegramUsers.tbUsers.Username == username);

                if (!string.IsNullOrWhiteSpace(dt.SearchValue))
                {
                    var term = dt.SearchValue;
                    query = query.Where(p =>
                        (p.dw_TaxId != null && p.dw_TaxId.Contains(term)) ||
                        (p.tbTelegramUsers.Tel_Username != null && p.tbTelegramUsers.Tel_Username.Contains(term)) ||
                        (p.tbTelegramUsers.Tel_FirstName != null && p.tbTelegramUsers.Tel_FirstName.Contains(term)) ||
                        (p.tbTelegramUsers.Tel_LastName != null && p.tbTelegramUsers.Tel_LastName.Contains(term)) ||
                        (p.tbPaymentMethods.tbpm_MethodName != null && p.tbPaymentMethods.tbpm_MethodName.Contains(term)));
                }

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

        private static BotFactoresResponseModel MapFactor(tbDepositWallet_Log item)
        {
            var factor = new BotFactoresResponseModel
            {
                UserId = item.FK_TelegramUser_ID ?? 0,
                Id = item.dw_ID,
                TaxId = item.dw_TaxId,
                Date = item.dw_CreateDatetime.HasValue ? item.dw_CreateDatetime.Value.ConvertDateTimeToShamsi2() : "-",
                User = item.tbTelegramUsers.Tel_Username + "(" + item.tbTelegramUsers.Tel_FirstName + " " + item.tbTelegramUsers.Tel_LastName + ")",
                Price = item.dw_Price.HasValue ? item.dw_Price.Value.ConvertToMony() : "0",
                PayMethod = item.tbPaymentMethods?.tbpm_MethodName ?? "-"
            };

            if (item.dw_Status == "FOR_PAY" || item.dw_Status == "EXPIRED")
                factor.Status = OrderStatusHelper.GetDepositDisplayStatus(item.dw_Status, item.dw_CreateDatetime);
            else if (item.dw_Status == "FINISH")
                factor.Status = OrderStatusHelper.DisplayFinished;

            return factor;
        }

        [HttpPost]
        public async Task<ActionResult> Accept(int factor_id)
        {
            try
            {
                var User = JwtToken.GetUser_ID();
                var User_ID = Convert.ToInt32(User);
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
