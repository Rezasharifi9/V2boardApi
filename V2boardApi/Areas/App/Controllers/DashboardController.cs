using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data;
using V2boardApi.Tools;
using NLog;
using Antlr.Runtime.Misc;
using System.Threading.Tasks;
using V2boardApi.Models.DashbordModel;
using V2boardApi.Areas.App.Data.Dashboard;
using Telegram.Bot.Types;

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
        }

        [AuthorizeApp(Roles = "1")]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> _WeeklyDataReport()
        {
            var user = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
            var persianCalendar = new PersianCalendar();
            var today = DateTime.Now;
            var weekBounds = DashboardSalesHelper.GetPersianWeekBounds(today);
            var salesData = DashboardSalesHelper.LoadSalesData(db);

            var nowYear = persianCalendar.GetYear(today);
            var nowMonth = persianCalendar.GetMonth(today);

            WeeklyDataReportViewModel wkData = new WeeklyDataReportViewModel();

            var thisWeekSale = DashboardSalesHelper.SumSalesInRange(
                salesData, weekBounds.ThisWeekStart, weekBounds.ThisWeekEnd);
            var oldWeekSale = DashboardSalesHelper.SumSalesInRange(
                salesData, weekBounds.LastWeekStart, weekBounds.LastWeekEnd);
            var sellMonth = DashboardSalesHelper.SumSalesForPersianMonth(
                salesData, nowYear, nowMonth, DashboardSalesHelper.GetPassedDaysInPersianMonth(today));

            wkData.Sale = (long)Math.Round(thisWeekSale, 0);
            wkData.ProfitSale = DashboardSalesHelper.CalcChangePercent(thisWeekSale, oldWeekSale);

            var passedDays = DashboardSalesHelper.GetPassedDaysInPersianMonth(today);
            wkData.SellAvg = passedDays > 0 ? sellMonth / passedDays : 0;

            using (MySqlEntities mySqlEntities = new MySqlEntities(user.tbServers.ConnectionString))
            {

                await mySqlEntities.OpenAsync();

                var ThisWeekUnix = Utility.ConvertDatetimeToSecond(weekBounds.ThisWeekStart);
                var OldWeekUnix = Utility.ConvertDatetimeToSecond(weekBounds.LastWeekStart);
                var OldEndWeekUnix = Utility.ConvertDatetimeToSecond(weekBounds.LastWeekEnd);



                var QueryThisWeek = "SELECT COUNT(id) as CountUser from v2_user where v2_user.created_at >= " + ThisWeekUnix;

                var QueryOldWeek = "SELECT COUNT(id) as CountUser from v2_user where v2_user.created_at >=  " + OldWeekUnix + " and v2_user.created_at <=" + OldEndWeekUnix;


                var thisReader = await mySqlEntities.GetDataAsync(QueryThisWeek);
                await thisReader.ReadAsync();
                var ThisWeekCountSub = thisReader.GetInt32("CountUser");
                thisReader.Close();
                var OldReader = await mySqlEntities.GetDataAsync(QueryOldWeek);
                await OldReader.ReadAsync();
                var OldWeekCountSub = OldReader.GetInt32("CountUser");
                OldReader.Close();
                wkData.ProfitSub = DashboardSalesHelper.CalcChangePercent(ThisWeekCountSub, OldWeekCountSub);
                wkData.Subscriptions = ThisWeekCountSub;


                var QueryUseageSubThisWeek = "SELECT SUM(d+u) as total FROM `v2_stat_user` WHERE v2_stat_user.created_at >=" + ThisWeekUnix;

                var QueryUseageSubOldWeek = "SELECT SUM(d+u) as total FROM `v2_stat_user`WHERE v2_stat_user.created_at >=" + OldWeekUnix + " and v2_stat_user.created_at <=" + OldEndWeekUnix;



                thisReader = await mySqlEntities.GetDataAsync(QueryUseageSubThisWeek);
                await thisReader.ReadAsync();
                var ThisUseageWeek = thisReader.GetInt64("total");
                thisReader.Close();
                OldReader = await mySqlEntities.GetDataAsync(QueryUseageSubOldWeek);
                await OldReader.ReadAsync();
                var OldUseageWeek = OldReader.GetInt64("total");
                OldReader.Close();


                wkData.ProfitUseage = DashboardSalesHelper.CalcChangePercent(ThisUseageWeek, OldUseageWeek);
                wkData.SubscriptionUseage = Math.Round(Utility.ConvertByteToGB(ThisUseageWeek), 2);

                await mySqlEntities.CloseAsync();
            }


            return PartialView(wkData);
        }

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public ActionResult GetNowMonthSellReport()
        {

            var user = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();

            var Pc = new PersianCalendar();

            var data = new List<_PerformanceSysViewModel>();
            var salesData = DashboardSalesHelper.LoadSalesData(db);

            var NowYear = Pc.GetYear(DateTime.Now);
            var NowMonth = Pc.GetMonth(DateTime.Now);
            var NowDay = Pc.GetDayOfMonth(DateTime.Now);

            _PerformanceSysViewModel model = new _PerformanceSysViewModel();
            List<double> list_kol = DashboardSalesHelper.BuildPersianMonthChartData(salesData, NowYear, NowMonth, NowDay);
            List<string> list_date = new List<string>();

            for (int i = 1; i <= NowDay; i++)
                list_date.Add(i.ToString());

            model.id = 1;
            model.active_option = NowDay;
            model.chart_data = list_kol;
            model.chart_xData = list_date;

            data.Add(model);


            _PerformanceSysViewModel model_agent = new _PerformanceSysViewModel();
            List<Tuple<double, string>> agentSales = new List<Tuple<double, string>>();

            var agents = user.tbUsers1.Where(s => s.Status == true)
                                   .ToList();

            foreach (var agent in agents)
            {
                var BotSum = salesData.BotSales
                    .Where(s => string.Equals(s.Username, agent.Username, StringComparison.OrdinalIgnoreCase))
                    .Select(s => new { Item = s, Date = Utility.ParseBotSaleOrderDate(s.OrderDate) })
                    .Where(x => x.Date.HasValue
                        && Pc.GetYear(x.Date.Value) == NowYear
                        && Pc.GetMonth(x.Date.Value) == NowMonth)
                    .Sum(x => x.Item.SalePrice ?? 0);
                var UserSaleSum = salesData.UserSales
                    .Where(s => string.Equals(s.Username, agent.Username, StringComparison.OrdinalIgnoreCase)
                        && s.CreateDate.HasValue
                        && Pc.GetYear(s.CreateDate.Value) == NowYear
                        && Pc.GetMonth(s.CreateDate.Value) == NowMonth)
                    .Sum(s => s.SalePrice ?? 0);
                var MasterUserSUm = salesData.MasterSales
                    .Where(s => string.Equals(s.Username, agent.Username, StringComparison.OrdinalIgnoreCase)
                        && s.CreateDate.HasValue
                        && Pc.GetYear(s.CreateDate.Value) == NowYear
                        && Pc.GetMonth(s.CreateDate.Value) == NowMonth)
                    .Sum(s => s.SalePrice ?? 0);

                double sum = (BotSum + UserSaleSum + MasterUserSUm) / 1000d;

                agentSales.Add(new Tuple<double, string>(Math.Round(sum, 0), agent.Username));
            }

            var sortedSalesData = agentSales.OrderByDescending(s => s.Item1).Take(12).ToList();

            // Extract sorted values into list_sell and list_agent
            List<double> list_sell = sortedSalesData.Select(s => s.Item1).ToList();
            List<string> list_agent = sortedSalesData.Select(s => s.Item2).ToList();

            model_agent.id = 2;
            model_agent.active_option = 1;
            model_agent.chart_data = list_sell;
            model_agent.chart_xData = list_agent;

            data.Add(model_agent);



            _PerformanceSysViewModel model_bot = new _PerformanceSysViewModel();
            List<double> list_bot = new List<double>();
            List<string> list_date2 = new List<string>();

            for (int i = 1; i <= NowDay; i++)
            {
                var botDayAmount = salesData.BotSales
                    .Select(s => new { Item = s, Date = Utility.ParseBotSaleOrderDate(s.OrderDate) })
                    .Where(x => x.Date.HasValue
                        && Pc.GetYear(x.Date.Value) == NowYear
                        && Pc.GetMonth(x.Date.Value) == NowMonth
                        && Pc.GetDayOfMonth(x.Date.Value) == i)
                    .Sum(x => x.Item.SalePrice ?? 0);

                list_bot.Add(Math.Round(botDayAmount / 1000d, 0));
                list_date2.Add(i.ToString());
            }

            model_bot.id = 3;
            model_bot.active_option = 1;
            model_bot.chart_data = list_bot;
            model_bot.chart_xData = list_date2;

            data.Add(model_bot);

            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> _GetPerformanceSystemReport()
        {
            var user = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();

            var Pc = new PersianCalendar();

            _PerformanceSystemViewModel systemViewModel = new _PerformanceSystemViewModel();

            var today = DateTime.Now;
            var NowYear = Pc.GetYear(today);
            var NowMonth = Pc.GetMonth(today);

            var salesData = DashboardSalesHelper.LoadSalesData(db);

            var botSale = salesData.BotSales
                .Select(s => new { Item = s, Date = Utility.ParseBotSaleOrderDate(s.OrderDate) })
                .Where(x => x.Date.HasValue
                    && Pc.GetYear(x.Date.Value) == NowYear
                    && Pc.GetMonth(x.Date.Value) == NowMonth)
                .Sum(x => x.Item.SalePrice ?? 0);
            var userSale = salesData.UserSales
                .Where(s => s.CreateDate.HasValue
                    && Pc.GetYear(s.CreateDate.Value) == NowYear
                    && Pc.GetMonth(s.CreateDate.Value) == NowMonth)
                .Sum(s => s.SalePrice ?? 0);
            var masterUseSale = salesData.MasterSales
                .Where(s => s.CreateDate.HasValue
                    && Pc.GetYear(s.CreateDate.Value) == NowYear
                    && Pc.GetMonth(s.CreateDate.Value) == NowMonth)
                .Sum(s => s.SalePrice ?? 0);

            userSale += masterUseSale;
            var total = botSale + userSale;

            if (total > 0)
            {
                systemViewModel.Precent_Agent = Math.Round((userSale / total) * 100, 2);
                systemViewModel.Precent_bot = Math.Round((botSale / total) * 100, 2);
            }




            using (MySqlEntities mySqlEntities = new MySqlEntities(user.tbServers.ConnectionString))
            {

                await mySqlEntities.OpenAsync();

                var Datetime = Pc.ToDateTime(NowYear, NowMonth, 1, 0, 0, 0, 0);

                var NowMonthUnix = Utility.ConvertDatetimeToSecond(Datetime);


                var Query_ActiveSub = "SELECT COUNT(id) as CountUser FROM `v2_user` WHERE (u+d) < transfer_enable and expired_at >= UNIX_TIMESTAMP(NOW());";

                var Query_NewSub = "SELECT COUNT(id) as CountUser from v2_user where v2_user.created_at >=  " + NowMonthUnix;


                var thisReader = await mySqlEntities.GetDataAsync(Query_ActiveSub);
                await thisReader.ReadAsync();
                var ActiveSub = thisReader.GetInt32("CountUser");
                thisReader.Close();

                var OldReader = await mySqlEntities.GetDataAsync(Query_NewSub);
                await OldReader.ReadAsync();
                var NewSub = OldReader.GetInt32("CountUser");
                OldReader.Close();



                var QueryUseage_Download = "SELECT SUM(d) as download FROM `v2_stat_user` WHERE v2_stat_user.created_at >=" + NowMonthUnix;

                var QueryUseage_Upload = "SELECT SUM(u) as upload FROM `v2_stat_user` WHERE v2_stat_user.created_at >=" + NowMonthUnix + "";



                thisReader = await mySqlEntities.GetDataAsync(QueryUseage_Download);
                await thisReader.ReadAsync();
                var Useage_download = thisReader.GetInt64("download");
                thisReader.Close();

                OldReader = await mySqlEntities.GetDataAsync(QueryUseage_Upload);
                await OldReader.ReadAsync();
                var Usage_Upload = OldReader.GetInt64("upload");
                OldReader.Close();


                //wkData.ProfitUseage = Math.Round(((double)(ThisUseageWeek - OldUseageWeek) / OldUseageWeek) * 100, 2);
                //wkData.SubscriptionUseage = Math.Round(Utility.ConvertByteToGB(ThisUseageWeek), 2);

                await mySqlEntities.CloseAsync();

                systemViewModel.New_Sub = NewSub;
                systemViewModel.Active_Sub = ActiveSub;

                systemViewModel.Traffic_down = Math.Round(Utility.ConvertByteToGB(Useage_download), 2).ConvertToMony() + " گیگ";
                systemViewModel.Traffic_up = Math.Round(Utility.ConvertByteToGB(Usage_Upload), 2).ConvertToMony() + " گیگ";

            }


            return View(systemViewModel);

        }

        static List<DateTime> GetDatesBetween(DateTime start, DateTime end)
        {
            List<DateTime> dateList = new List<DateTime>();
            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                dateList.Add(date);
            }
            return dateList;
        }

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> GetTrafficReport()
        {

            var user = RepositoryUser.Where(s => s.Username == User.Identity.Name).FirstOrDefault();

            var persianCalendar = new PersianCalendar();
            var weekBounds = DashboardSalesHelper.GetPersianWeekBounds(DateTime.Now);

            List<DateTime> dates = GetDatesBetween(weekBounds.ThisWeekStart, DateTime.Now.Date);

            List<int> Dates = new List<int>();
            List<double> Traffic = new List<double>();

            using (MySqlEntities mySqlEntities = new MySqlEntities(user.tbServers.ConnectionString))
            {

                await mySqlEntities.OpenAsync();

                foreach (DateTime date in dates)
                {
                    var StartTime = Utility.ConvertDatetimeToSecond(date);
                    var dated = date.AddHours(24);
                    var EndTime = Utility.ConvertDatetimeToSecond(dated);

                    var QueryUseage_Download = "SELECT SUM(v2_stat_user.u + v2_stat_user.d) as traffic FROM `v2_stat_user` WHERE v2_stat_user.created_at >=" + StartTime + " and v2_stat_user.created_at <=" + EndTime;


                    var thisReader = await mySqlEntities.GetDataAsync(QueryUseage_Download);
                    await thisReader.ReadAsync();
                    var Use = thisReader.GetDouble("traffic");
                    thisReader.Close();

                    var Day = persianCalendar.GetDayOfMonth(date);

                    Dates.Add(Day);
                    Traffic.Add(Math.Round(Utility.ConvertByteToGB(Use),2));
                }

                await mySqlEntities.CloseAsync();
            }

            return Json(new { data = new { days = Dates, Traffics = Traffic } },JsonRequestBehavior.AllowGet);
        }
    }
}