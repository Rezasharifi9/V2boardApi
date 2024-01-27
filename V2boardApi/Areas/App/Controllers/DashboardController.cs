using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data;

namespace V2boardApi.Areas.App.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private V2boardSiteEntities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbTelegramUsers> RepositoryTelegramUsers { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public DashboardController()
        {
            db = new V2boardSiteEntities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryPlans = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryTelegramUsers = new Repository<tbTelegramUsers>();
        }
        public ActionResult Index()
        {
            var user = RepositoryUser.GetAll(p => p.Username == User.Identity.Name && p.Role == 1).FirstOrDefault();
            if (user != null)
            {
                DashboardViewModel model = new DashboardViewModel();
                var Users = user.tbServers.tbUsers.ToList();
                // تاریخ هفته قبل
                DateTime lastDayStartDate = DateTime.Now.AddDays(-((int)DateTime.Now.DayOfWeek) - 6);
                // تاریخ هفته جاری
                DateTime lastDayEndDate = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);

                // تاریخ هفته جاری
                DateTime thisDayStartDate = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);

                DateTime EndOfWeek = thisDayStartDate.AddDays(6);

                // فروش هفته قبل 
                double OldWeekSaleFromMajor = 0;
                //فروش هفته قبل از ربات
                double OldWeekSaleFromBot = 0;
                //کاربرای هفته قبل
                double OldWeekCountUser = 0;
                // تعداد فروش هفته قبل در ربات
                double OldWeekCountBot = 0;
                foreach (var item in Users)
                {
                    foreach (var link in item.tbLinkUserAndPlans.ToList())
                    {
                        //فروش هفته جاری
                        model.SaleFromMajor += link.tbLogs.Where(p => p.CreateDatetime >= thisDayStartDate && p.CreateDatetime <= EndOfWeek).Sum(p => p.tbLinkUserAndPlans.tbPlans.Price.Value) * 10;
                        // فروش هفته قبل 
                        OldWeekSaleFromMajor += link.tbLogs.Where(p => p.CreateDatetime >= lastDayStartDate && p.CreateDatetime <= lastDayEndDate).Sum(p => p.tbLinkUserAndPlans.tbPlans.Price.Value) * 10;

                        //فروش امروز از پنل عمده فروش
                        model.SaleFromMajorToday += link.tbLogs.Where(p => p.CreateDatetime >= DateTime.Now.Date).Sum(p => p.tbLinkUserAndPlans.tbPlans.Price.Value) * 10;

                        //کاربرای هفته جاری
                        model.CountUserFromMajor += link.tbLogs.Where(p => p.CreateDatetime >= thisDayStartDate).Count();
                        //کاربرای هفته قبل
                        OldWeekCountUser += link.tbLogs.Where(p => p.CreateDatetime >= lastDayStartDate && p.CreateDatetime <= lastDayEndDate).Count();
                    }
                }

                foreach (var item in RepositoryTelegramUsers.GetAll(p => p.Tel_RobotID == user.tbServers.Robot_ID).ToList())
                {
                    //فروش هفته جاری از ربات
                    model.SaleFromBot += item.tbOrders.Where(p => p.OrderDate >= thisDayStartDate && p.OrderDate <= EndOfWeek && p.OrderStatus == "FINISH").Sum(p => p.Order_Price.Value);
                    //فروش هفته قبل از ربات
                    OldWeekSaleFromBot += item.tbOrders.Where(p => p.OrderDate >= lastDayStartDate && p.OrderDate <= lastDayEndDate && p.OrderStatus == "FINISH").Sum(p => p.Order_Price.Value);
                    //تعداد فروش هفته جاری از ربات
                    model.CountUserFromBot += item.tbOrders.Where(p => p.OrderDate >= thisDayStartDate && p.OrderStatus == "FINISH").Count();
                    // تعداد فروش هفته قبل در ربات
                    OldWeekCountBot += item.tbOrders.Where(p => p.OrderDate >= lastDayStartDate && p.OrderDate <= lastDayEndDate && p.OrderStatus == "FINISH").Count();
                    //فروش امروز از ربات
                    model.SaleFromBotToday += item.tbOrders.Where(p => p.OrderDate >= DateTime.Now.Date && p.OrderStatus == "FINISH").Sum(p => p.Order_Price.Value);

                }


                model.InterestRatesFromBot = (model.SaleFromBot - OldWeekSaleFromBot) / OldWeekSaleFromBot * 100;
                model.InterestRatesFromMajor = (model.SaleFromMajor - OldWeekSaleFromMajor) / OldWeekSaleFromMajor * 100;

                model.InterestUserRatesBot = (model.CountUserFromBot - OldWeekCountBot) / OldWeekCountBot * 100;
                model.InterestUserRatesFromMajor = (model.CountUserFromMajor - OldWeekCountUser) / OldWeekCountUser * 100;

                return View(model);
            }

            return View();


        }

        [HttpGet]
        public ActionResult CountAccountCreatedFromMajor()
        {
            var user = RepositoryUser.GetAll(p => p.Username == User.Identity.Name && p.Role == 1).FirstOrDefault();
            if (user != null)
            {
                DateTime lastDayStartDate = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);
                DateTime endOfWeek = lastDayStartDate.AddDays(6);

                CountAccountCreatedFromMajorModel majorModel = new CountAccountCreatedFromMajorModel();
                majorModel.Users = new List<string>();
                majorModel.Prices = new List<int>();
                foreach (var item in user.tbServers.tbUsers.Where(p => p.Role == 2).ToList())
                {
                    var SumPrice = 0;
                    foreach (var item2 in item.tbLinkUserAndPlans.ToList())
                    {
                        SumPrice += item2.tbLogs.Where(p => p.CreateDatetime >= lastDayStartDate && p.CreateDatetime <= endOfWeek).Sum(p => p.tbLinkUserAndPlans.tbPlans.Price.Value);
                    }
                    if (SumPrice > 0)
                    {
                        majorModel.Prices.Add(SumPrice);
                        majorModel.Users.Add(item.Username);
                    }
                }

                return Json(majorModel, JsonRequestBehavior.AllowGet);
            }
            return Json(null);
        }
    }
}