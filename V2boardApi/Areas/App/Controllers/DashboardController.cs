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
using Antlr.Runtime.Misc;
using System.Threading.Tasks;
using V2boardApi.Models.DashbordModel;
using DataLayer.Models.DashboardModel;

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
        private ViewRepository<vi_BotSales> BotSales_ViewRepository { get; set; }
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

        [AuthorizeApp(Roles = "1")]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> _WeeklyDataReport()
        {
            var user = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
            // ایجاد یک نمونه از PersianCalendar
            PersianCalendar persianCalendar = new PersianCalendar();

            // تاریخ امروز میلادی
            DateTime today = DateTime.Now.Date;

            // استخراج روز جاری هفته (0 برای شنبه)
            DayOfWeek dayOfWeek = persianCalendar.GetDayOfWeek(today);

            // محاسبه شروع هفته جاری (شنبه این هفته)
            int daysToSubtract = (int)dayOfWeek + 1; // تعداد روزهایی که باید از امروز کم کنیم
            DateTime startOfThisWeek = today.AddDays(-daysToSubtract);

            // محاسبه شروع هفته گذشته
            DateTime startOfLastWeek = startOfThisWeek.AddDays(-7); // 7 روز قبل از شروع این هفته

            // محاسبه پایان هفته گذشته (جمعه هفته گذشته)
            DateTime endOfLastWeek = startOfLastWeek.AddDays(6); // 6 روز بعد از شروع هفته گذشته

            // تبدیل تاریخ شروع هفته گذشته به شمسی
            int startOfLastWeekYear = persianCalendar.GetYear(startOfLastWeek);
            int startOfLastWeekMonth = persianCalendar.GetMonth(startOfLastWeek);
            int startOfLastWeekDay = persianCalendar.GetDayOfMonth(startOfLastWeek);

            // تبدیل تاریخ پایان هفته گذشته به شمسی
            int endOfLastWeekYear = persianCalendar.GetYear(endOfLastWeek);
            int endOfLastWeekMonth = persianCalendar.GetMonth(endOfLastWeek);
            int endOfLastWeekDay = persianCalendar.GetDayOfMonth(endOfLastWeek);

            WeeklyDataReportViewModel wkData = new WeeklyDataReportViewModel();


            

            var ThisWeekSale = db.GetBotSales().Where(s => Convert.ToDateTime(s.OrderDate) >= startOfThisWeek).Sum(s => s.SalePrice);
            ThisWeekSale+=db.GetUserSales().Where(s=> s.CreateDate >= startOfThisWeek).Sum(s => s.SalePrice);
            ThisWeekSale+=db.GetMasterUserSales().Where(s=> s.CreateDate >= startOfThisWeek).Sum(s => s.SalePrice);


            var OldWeekSale = db.GetBotSales().Where(s => Convert.ToDateTime(s.OrderDate) >= endOfLastWeek && Convert.ToDateTime(s.OrderDate) <= startOfThisWeek).Sum(s => s.SalePrice);
            OldWeekSale += db.GetUserSales().Where(s => s.CreateDate >= endOfLastWeek && s.CreateDate <= startOfThisWeek).Sum(s => s.SalePrice);
            OldWeekSale += db.GetMasterUserSales().Where(s => s.CreateDate >= endOfLastWeek && s.CreateDate <= startOfThisWeek).Sum(s => s.SalePrice);


            wkData.Sale = (long)ThisWeekSale;
            wkData.ProfitSale = Math.Round((double)(((double)(ThisWeekSale - OldWeekSale) / OldWeekSale) * 100), 2);

            DayOfWeek startOfWeek = persianCalendar.GetDayOfWeek(startOfThisWeek);
            int daysSinceStartOfWeek = (today.DayOfWeek - startOfWeek + 7) % 7;

            wkData.SellAvg = ThisWeekSale.Value / daysSinceStartOfWeek;



            using (MySqlEntities mySqlEntities = new MySqlEntities(user.tbServers.ConnectionString))
            {

                await mySqlEntities.OpenAsync();

                var ThisWeekUnix = Utility.ConvertDatetimeToSecond(startOfThisWeek);
                var OldWeekUnix = Utility.ConvertDatetimeToSecond(startOfLastWeek);
                var OldEndWeekUnix = Utility.ConvertDatetimeToSecond(endOfLastWeek);



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
                wkData.ProfitSub = Math.Round(((double)(ThisWeekCountSub - OldWeekCountSub) / OldWeekCountSub) * 100, 2);
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


                wkData.ProfitUseage = Math.Round(((double)(ThisUseageWeek - OldUseageWeek) / OldUseageWeek) * 100,2);
                wkData.SubscriptionUseage = Math.Round(Utility.ConvertByteToGB(ThisUseageWeek),2);

                await mySqlEntities.CloseAsync();
            }


            return PartialView(wkData);
        }
    }
}