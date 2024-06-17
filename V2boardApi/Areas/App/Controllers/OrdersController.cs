using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Models.OrdersModel;


namespace V2boardApi.Areas.App.Controllers
{
    [Authorize]
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
           
            var Orders = OrdersRepository.Where(p=> p.tbTelegramUsers.Tel_RobotID == Use.tbServers.Robot_ID).OrderByDescending(p=> p.OrderDate).Take(100).ToList();
            return View(Orders);
        }
    }
}