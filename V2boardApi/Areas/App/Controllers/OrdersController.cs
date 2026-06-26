using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data.OrdersViewModels;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [AuthorizeApp(Roles = "1,2,3,4")]
    [LogActionFilter]
    public class OrdersController : Controller
    {
        private Entities _db;
        private Repository<tbOrders> OrdersRepository;
        private Repository<tbUsers> UsersRepository;

        public OrdersController()
        {
            _db = new Entities();
            OrdersRepository = new Repository<tbOrders>(_db);
            UsersRepository = new Repository<tbUsers>(_db);
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

                var query = _db.tbOrders
                    .Include(o => o.tbTelegramUsers)
                    .Where(p => p.tbTelegramUsers.tbUsers.Username == username);

                if (!string.IsNullOrWhiteSpace(dt.SearchValue))
                {
                    var term = dt.SearchValue;
                    query = query.Where(p =>
                        (p.AccountName != null && p.AccountName.Contains(term)) ||
                        (p.tbTelegramUsers.Tel_Username != null && p.tbTelegramUsers.Tel_Username.Contains(term)) ||
                        (p.tbTelegramUsers.Tel_FirstName != null && p.tbTelegramUsers.Tel_FirstName.Contains(term)) ||
                        (p.tbTelegramUsers.Tel_LastName != null && p.tbTelegramUsers.Tel_LastName.Contains(term)));
                }

                var totalRecords = query.Count();

                switch (dt.SortColumnIndex)
                {
                    case 1:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.AccountName)
                            : query.OrderByDescending(p => p.AccountName);
                        break;
                    case 2:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.tbTelegramUsers.Tel_Username)
                            : query.OrderByDescending(p => p.tbTelegramUsers.Tel_Username);
                        break;
                    case 4:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.OrderDate)
                            : query.OrderByDescending(p => p.OrderDate);
                        break;
                    case 5:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.Tel_RenewedDate)
                            : query.OrderByDescending(p => p.Tel_RenewedDate);
                        break;
                    case 6:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.Order_Price)
                            : query.OrderByDescending(p => p.Order_Price);
                        break;
                    case 7:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.OrderStatus)
                            : query.OrderByDescending(p => p.OrderStatus);
                        break;
                    default:
                        query = dt.IsAscending
                            ? query.OrderBy(p => p.OrderDate)
                            : query.OrderByDescending(p => p.OrderDate);
                        break;
                }

                var pageSize = dt.Length > 0 ? dt.Length : 10;
                var ordersPage = query.Skip(dt.Start).Take(pageSize).ToList();
                var data = ordersPage.Select(MapOrder).ToList();

                return Json(new
                {
                    draw = dt.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data
                }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<OrderResponseViewModel>() }, JsonRequestBehavior.AllowGet);
            }
        }

        private static OrderResponseViewModel MapOrder(tbOrders item)
        {
            var model = new OrderResponseViewModel();

            model.Status = OrderStatusHelper.GetOrderDisplayStatus(item.OrderStatus, item.OrderDate);

            model.CreateDate = item.OrderDate.HasValue ? item.OrderDate.Value.ConvertDateTimeToShamsi2() : "-";
            model.Plan = item.Traffic + " گیگ " + item.Month + " ماهه";
            model.SubName = item.AccountName?.Split('@')[0] ?? "-";
            model.Price = item.Order_Price.HasValue ? item.Order_Price.Value.ConvertToMony() : "0";
            model.UserId = item.FK_Tel_UserID ?? 0;
            model.UserCreator = item.tbTelegramUsers.Tel_Username + "(" + item.tbTelegramUsers.Tel_FirstName + " " + item.tbTelegramUsers.Tel_LastName + ")";

            if (model.Status == 1)
            {
                model.ActiveDate = item.Tel_RenewedDate.HasValue
                    ? item.Tel_RenewedDate.Value.ConvertDateTimeToShamsi2()
                    : "انجام شده";
            }
            else
            {
                model.ActiveDate = "-";
            }

            return model;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
                UsersRepository.Dispose();
                OrdersRepository.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
