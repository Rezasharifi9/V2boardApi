using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data.OrdersViewModels;
using V2boardApi.Models.OrdersModel;
using V2boardApi.Tools;


namespace V2boardApi.Areas.App.Controllers
{
    [AuthorizeApp(Roles = "1,2")]
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
        public ActionResult GetOrders()
        {
            var Orders = OrdersRepository.Where(p => p.tbTelegramUsers.tbUsers.Username == User.Identity.Name).ToList();
            List<OrderResponseViewModel> orders = new List<OrderResponseViewModel>();
            foreach (var item in Orders)
            {
                OrderResponseViewModel model = new OrderResponseViewModel();
                if (item.OrderStatus == "FOR_RESERVE")
                {
                    model.Status = 0;
                }
                else if (item.OrderStatus == "FINISH")
                {
                    model.Status = 1;
                }
                
                model.CreateDate = item.OrderDate.Value.ConvertDateTimeToShamsi2();
                model.Plan = item.Traffic + " گیگ " + item.Month + " ماهه";
                model.SubName = item.AccountName.Split('@')[0];
                model.Price = item.Order_Price.Value.ConvertToMony();
                model.UserCreator = item.tbTelegramUsers.Tel_Username + "(" + item.tbTelegramUsers.Tel_FirstName + " " + item.tbTelegramUsers.Tel_LastName + ")";
                orders.Add(model);
            }

            return Json(new { data = orders }, JsonRequestBehavior.AllowGet);
        }
    }
}