using DataLayer.DomainModel;
using DataLayer.Repository;
using Stimulsoft.Report.Mvc;
using Stimulsoft.Report;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data;
using V2boardApi.Tools;
using NLog;

namespace V2boardApi.Areas.App.Controllers
{
    [LogActionFilter]
    public class DashboardController : Controller
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbTelegramUsers> RepositoryTelegramUsers { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public DashboardController()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryPlans = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryTelegramUsers = new Repository<tbTelegramUsers>();


            var path = System.Web.HttpContext.Current.Server.MapPath("~/Key/license.key");

            Stimulsoft.Base.StiLicense.LoadFromFile(path);
        }

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult GetReport()
        {
            try
            {
                var report = StiReport.CreateNewDashboard();

                var user = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

                if (user.Role != 1)
                {
                    var path = Server.MapPath("~/Reports/Report.mrt");
                    report = StiTools.Load(path);

                    report.Dictionary.DataSources[0].Parameters["User_ID"].Value = user.User_ID.ToString();
                    report.Dictionary.DataSources[1].Parameters["tbUserID"].Value = user.User_ID.ToString();
                }
                else
                {

                    var path = Server.MapPath("~/Reports/ReportAdmin.mrt");
                    report = StiTools.Load(path);

                }

                return StiMvcViewer.GetReportResult(report);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "در نمایش داشبورد با خطایی مواجه شدیم");
                return View();
            }
        }

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult ViewerEvent()
        {
            return StiMvcViewer.ViewerEventResult();
        }

        public ActionResult _Dashboard()
        {
            return PartialView();
        }

    }
}