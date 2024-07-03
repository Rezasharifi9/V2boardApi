using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [AuthorizeApp(Roles = "1,2")]
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
            try
            {
                var Us = db.tbUsers.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
                List<tbDepositWallet_Log> PayList = new List<tbDepositWallet_Log>();
                if (Us != null)
                {
                    var Logs = new List<tbDepositWallet_Log>();
                    foreach (var us in Us.tbTelegramUsers.ToList())
                    {
                        Logs.AddRange(us.tbDepositWallet_Log.ToList());
                    }


                    return View(Logs);
                }
            }
            catch (Exception ex)
            {
                logger.Error("در نمایش فاکتور ها با خطایی مواجه شدیم !");
            }

            return View();
        }
    }
}