using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Models.OrdersModel;
using V2boardApi.Tools;


namespace V2boardApi.Areas.App.Controllers
{
    [AuthorizeApp(Roles = "1,2")]
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

            var Use = UsersRepository.Where(p => p.Username == User.Identity.Name).First();

            var Orders = new List<tbOrders>();
            foreach (var item in Use.tbTelegramUsers.ToList())
            {
                Orders.AddRange(item.tbOrders.ToList());
            }
            return View(Orders);
        }
    }
}