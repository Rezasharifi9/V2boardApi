using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public ActionResult GetFactores()
        {
            try
            {
                List<BotFactoresResponseModel> BotFactores = new List<BotFactoresResponseModel>();

                var Factors = RepositoryDepositLog.Where(p => p.tbTelegramUsers.tbUsers.Username == User.Identity.Name).ToList();

                foreach (var item in Factors)
                {
                    BotFactoresResponseModel factor = new BotFactoresResponseModel();
                    if (item.dw_Status == "FOR_PAY")
                    {
                        factor.Status = 0;
                    }
                    else if (item.dw_Status == "FINISH")
                    {
                        factor.Status = 1;
                    }

                    factor.Date = item.dw_CreateDatetime.Value.ConvertDateTimeToShamsi2();
                    factor.User = item.tbTelegramUsers.Tel_Username + "(" + item.tbTelegramUsers.Tel_FirstName + " " + item.tbTelegramUsers.Tel_LastName + ")";
                    factor.Price = item.dw_Price.Value.ConvertToMony();
                    BotFactores.Add(factor);
                }


                return Json(new { data = BotFactores }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error("در نمایش فاکتور ها با خطایی مواجه شدیم !");
            }

            return View();
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