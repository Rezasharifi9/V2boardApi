using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data.AdminDashboard;
using V2boardApi.Models.DashbordModel;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    public class AdminDashboardController : Controller
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbTelegramUsers> RepositoryTelegramUsers { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public AdminDashboardController()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryPlans = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryTelegramUsers = new Repository<tbTelegramUsers>();
        }

        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> GetSalesSummary()
        {
            SalesSummaryViewModel salesSummary = new SalesSummaryViewModel();

            var user = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
            // ایجاد یک نمونه از PersianCalendar
            PersianCalendar persianCalendar = new PersianCalendar();


            var Year = persianCalendar.GetYear(DateTime.Now);
            var FirstYear = persianCalendar.ToDateTime(Year, 1, 1, 0, 0, 0, 0);


            var BotSells = db.GetBotSales().Where(s => Convert.ToDateTime(s.OrderDate) >= FirstYear).ToList();
            var UserSales = db.GetUserSales().Where(s => s.CreateDate >= FirstYear).ToList();
            var MasterSales = db.GetMasterUserSales().Where(s => s.CreateDate >= FirstYear).ToList();


            var SellAgent = UserSales.Where(s => s.CreateDate >= FirstYear).Sum(s => s.SalePrice);
            SellAgent += MasterSales.Where(s => s.CreateDate >= FirstYear).Sum(s => s.SalePrice);


            using (MySqlEntities mySqlEntities = new MySqlEntities(user.tbServers.ConnectionString))
            {

                await mySqlEntities.OpenAsync();

                var ThisYearUnixData = Utility.ConvertDatetimeToSecond(FirstYear);

                var ThisYearSub = "SELECT COUNT(id) as CountUser from v2_user where v2_user.created_at >= " + ThisYearUnixData;

                var thisReader = await mySqlEntities.GetDataAsync(ThisYearSub);
                await thisReader.ReadAsync();
                var CountSub = thisReader.GetInt32("CountUser");
                thisReader.Close();

                salesSummary.TotalCustomers = CountSub;

                await mySqlEntities.CloseAsync();
            }

            salesSummary.TotalSales = BotSells.Sum(a=> a.SalePrice.Value) + UserSales.Sum(a=> a.SalePrice.Value) + MasterSales.Sum(a=> a.SalePrice.Value);
            salesSummary.BotSales = BotSells.Sum(a => a.SalePrice.Value);
            salesSummary.AgentSales = MasterSales.Sum(a=> a.SalePrice.Value) + UserSales.Sum (a=> a.SalePrice.Value);
            

            return PartialView("_SalesSummaryReport", salesSummary);
        }

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> GetTopAgents()
        {
            PersianCalendar persianCalendar = new PersianCalendar();

            TopAgentsViewModel topAgentsView = new TopAgentsViewModel();

            var Year = persianCalendar.GetYear(DateTime.Now);
            var FirstYear = persianCalendar.ToDateTime(Year, 1, 1, 0, 0, 0, 0);


            var UserSales = db.GetUserSales().Where(s => s.CreateDate >= FirstYear).GroupBy(a=> a.Username).Select(a=> new { Username = a.Key, Sale = a.Sum(q=> q.SalePrice) }).OrderByDescending(e=> e.Sale).Take(10).ToList();
            var MasterSales = db.GetMasterUserSales().Where(s => s.CreateDate >= FirstYear).GroupBy(a => a.Username).Select(a => new { Username = a.Key, Sale = a.Sum(q => q.SalePrice) }).OrderByDescending(e => e.Sale).Take(10).ToList();


            topAgentsView.Agents = UserSales.Select(a=> a.Username).ToList();
            topAgentsView.Agents.AddRange(MasterSales.Select(a => a.Username).ToList());


            topAgentsView.Sells = UserSales.Select(a=> a.Sale.Value).ToList();
            topAgentsView.Sells.AddRange(MasterSales.Select(a => a.Sale.Value).ToList());


            return Json(new { data = topAgentsView }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> GetBusinessPerformance()
        {
            // ایجاد یک نمونه از PersianCalendar
            PersianCalendar persianCalendar = new PersianCalendar();

            OverallSalesPerformanceViewModel viewModel = new OverallSalesPerformanceViewModel();

            var Year = persianCalendar.GetYear(DateTime.Now);
            var FirstYear = persianCalendar.ToDateTime(Year, 1, 1, 0, 0, 0, 0);


            var BotSells = db.GetBotSales().Where(s => Convert.ToDateTime(s.OrderDate) >= FirstYear).ToList();
            var UserSales = db.GetUserSales().Where(s => s.CreateDate >= FirstYear).ToList();
            var MasterSales = db.GetMasterUserSales().Where(s => s.CreateDate >= FirstYear).ToList();

            viewModel.chart_xData = new List<string>();
            viewModel.chart_data = new List<double>();

            for (var i = 1; i <= 12; i++)
            {
                
                var FirstMonth = persianCalendar.ToDateTime(Year, i, 1, 0, 0, 0, 0);
                var LastMonth = new DateTime();
                if (i == 12)
                {
                     LastMonth = persianCalendar.ToDateTime(Year, i, 1, 0, 0, 0, 0);

                }
                else
                {
                    LastMonth = persianCalendar.ToDateTime(Year, i + 1, 1, 0, 0, 0, 0);
                }
                var PersianMonth = Utility.GetMonthName(FirstMonth);

                var Sales = BotSells.Where(a => Convert.ToDateTime(a.OrderDate) >= FirstMonth && Convert.ToDateTime(a.OrderDate) <= LastMonth).Sum(a => a.SalePrice.Value);
                Sales+= UserSales.Where(s => s.CreateDate >= FirstMonth && s.CreateDate<=LastMonth).Sum(a => a.SalePrice.Value);
                Sales+= MasterSales.Where(s => s.CreateDate >= FirstMonth && s.CreateDate <= LastMonth).Sum(a => a.SalePrice.Value);

                viewModel.chart_xData.Add(PersianMonth);
                viewModel.chart_data.Add(Sales);
            }

            var list = new List<OverallSalesPerformanceViewModel>();
            list.Add(viewModel);

            return Json(new { data = list }, JsonRequestBehavior.AllowGet);

        }
    }
}