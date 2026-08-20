using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data.ManagementDashboard;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [LogActionFilter]
    [AuthorizeApp(Roles = "1")]
    public class ManagementDashboardController : Controller
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly Entities db;
        private readonly Repository<tbUsers> repositoryUser;

        public ManagementDashboardController()
        {
            db = new Entities();
            repositoryUser = new Repository<tbUsers>(db);
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> GetOverview()
        {
            try
            {
                var model = await BuildOverviewAsync();
                return PartialView("_Overview", model);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در بارگذاری داشبورد مدیریت");
                return PartialView("_Overview", new ManagementDashboardViewModel
                {
                    PeriodTitle = "ماه جاری",
                    TodayLabel = DateTime.Now.ConvertDateTimeToShamsi5(),
                    TopAgents = new List<NamedAmountViewModel>(),
                    TopPlans = new List<NamedAmountViewModel>(),
                    TopDebtors = new List<NamedAmountViewModel>()
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetCharts()
        {
            try
            {
                var charts = await BuildChartsAsync();
                return Json(new { status = "success", data = charts }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در بارگذاری نمودارهای داشبورد مدیریت");
                return Json(new { status = "error", data = new ManagementDashboardChartsViewModel() }, JsonRequestBehavior.AllowGet);
            }
        }

        private async Task<ManagementDashboardViewModel> BuildOverviewAsync()
        {
            var today = DateTime.Now;
            var range = DashboardSalesHelper.GetPersianMonthRange(today);
            var salesData = DashboardSalesHelper.LoadSalesData(db);
            var monthChannel = DashboardSalesHelper.SumChannelSales(salesData, range.ThisMonthStart, range.ThisMonthEnd);
            var lastMonthComparable = DashboardSalesHelper.SumSalesInRange(salesData, range.LastMonthStart, range.LastMonthComparableEnd);
            var todaySales = DashboardSalesHelper.SumSalesInRange(salesData, range.TodayStart, range.TodayEnd);
            var yesterdaySales = DashboardSalesHelper.SumSalesInRange(salesData, range.YesterdayStart, range.YesterdayEnd);
            var monthOrderCount = DashboardSalesHelper.CountSalesInRange(salesData, range.ThisMonthStart, range.ThisMonthEnd);

            var agents = repositoryUser.table
                .Where(u => u.Role != 1)
                .Select(u => new
                {
                    u.User_ID,
                    u.Username,
                    u.FullName,
                    u.Status,
                    u.Wallet,
                    u.Settlement_IsBlocked
                })
                .ToList();

            var pendingInvoices = db.tbUserFactors
                .Where(f => f.tbUf_Status == 1 && f.tbUf_Value.HasValue)
                .Select(f => f.tbUf_Value.Value)
                .ToList();

            var createdActions = new[] { Resource.LogActions.U_Created, ReservedPackageHelper.CreatedLogAction };
            var editedActions = new[] { Resource.LogActions.U_Edited, ReservedPackageHelper.EditedLogAction };

            var monthLogs = db.tbLogs
                .Where(l => l.CreateDatetime.HasValue
                    && l.CreateDatetime >= range.ThisMonthStart
                    && l.CreateDatetime <= range.ThisMonthEnd)
                .Select(l => new { l.Action, l.SalePrice, l.PlanName })
                .ToList();

            var panelNew = monthLogs.Where(l => createdActions.Contains(l.Action)).ToList();
            var panelRenew = monthLogs.Where(l => editedActions.Contains(l.Action)).ToList();

            var botOrders = db.tbOrders
                .Where(o => o.OrderStatus == "FINISH"
                    && o.OrderDate.HasValue
                    && o.OrderDate >= range.ThisMonthStart
                    && o.OrderDate <= range.ThisMonthEnd)
                .Select(o => new { o.OrderType, o.Order_Price })
                .ToList();

            var botNew = botOrders.Where(o => o.OrderType != "تمدید").ToList();
            var botRenew = botOrders.Where(o => o.OrderType == "تمدید").ToList();

            var newAmount = panelNew.Sum(l => l.SalePrice ?? 0) + botNew.Sum(o => o.Order_Price ?? 0);
            var renewAmount = panelRenew.Sum(l => l.SalePrice ?? 0) + botRenew.Sum(o => o.Order_Price ?? 0);
            var newCount = panelNew.Count + botNew.Count;
            var renewCount = panelRenew.Count + botRenew.Count;
            var mixTotal = newCount + renewCount;

            var customers = await LoadCustomerKpisAsync(range);

            var monthSales = monthChannel.Total;
            var totalChannel = monthSales > 0 ? monthSales : 0;

            var topAgents = BuildTopAgents(salesData, agents.Select(a => a.Username).ToList(), range);
            var topPlans = monthLogs
                .Where(l => l.SalePrice.HasValue && l.SalePrice.Value > 0)
                .GroupBy(l => string.IsNullOrWhiteSpace(l.PlanName) ? "بدون نام" : l.PlanName)
                .Select(g => new NamedAmountViewModel
                {
                    Name = g.Key,
                    Amount = g.Sum(x => x.SalePrice ?? 0),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Amount)
                .Take(8)
                .ToList();

            var topDebtors = agents
                .Where(a => a.Wallet > 0)
                .OrderByDescending(a => a.Wallet)
                .Take(8)
                .Select(a => new NamedAmountViewModel
                {
                    Name = string.IsNullOrWhiteSpace(a.FullName) ? a.Username : a.FullName,
                    Amount = a.Wallet,
                    Count = 0
                })
                .ToList();

            return new ManagementDashboardViewModel
            {
                PeriodTitle = Utility.GetMonthName(today) + " " + range.Year.ToString(CultureInfo.InvariantCulture),
                TodayLabel = today.ConvertDateTimeToShamsi5(),
                MonthSales = monthSales,
                MonthSalesChangePercent = DashboardSalesHelper.CalcChangePercent(monthSales, lastMonthComparable),
                TodaySales = todaySales,
                TodaySalesChangePercent = DashboardSalesHelper.CalcChangePercent(todaySales, yesterdaySales),
                DailyAverage = range.PassedDays > 0 ? monthSales / range.PassedDays : 0,
                AverageOrderValue = monthOrderCount > 0 ? monthSales / monthOrderCount : 0,
                Arpu = customers.ActiveCustomers > 0 ? monthSales / customers.ActiveCustomers : 0,
                MonthOrderCount = monthOrderCount,
                BotSales = monthChannel.Bot,
                AgentSales = monthChannel.Agent + monthChannel.Master,
                MasterSales = monthChannel.Master,
                BotSalesPercent = Percent(monthChannel.Bot, totalChannel),
                AgentSalesPercent = Percent(monthChannel.Agent + monthChannel.Master, totalChannel),
                MasterSalesPercent = Percent(monthChannel.Master, totalChannel),
                NewSalesAmount = newAmount,
                RenewSalesAmount = renewAmount,
                NewSalesCount = newCount,
                RenewSalesCount = renewCount,
                RenewRatePercent = Percent(renewCount, mixTotal),
                AgentReceivables = agents.Where(a => a.Wallet > 0).Sum(a => a.Wallet),
                PendingInvoiceCount = pendingInvoices.Count,
                PendingInvoiceAmount = pendingInvoices.Sum(),
                ActiveAgentCount = agents.Count(a => a.Status == true && !a.Settlement_IsBlocked),
                BlockedAgentCount = agents.Count(a => a.Settlement_IsBlocked || a.Status != true),
                TelegramActiveCount = db.tbTelegramUsers.Count(t => t.Tel_Status == 1 && !t.Tel_IsBlocked),
                TotalCustomers = customers.TotalCustomers,
                ActiveCustomers = customers.ActiveCustomers,
                ExpiredCustomers = customers.ExpiredCustomers,
                ExpiringSoonCount = customers.ExpiringSoonCount,
                TrafficExhaustedCount = customers.TrafficExhaustedCount,
                NewCustomersThisMonth = customers.NewCustomersThisMonth,
                NewCustomersChangePercent = DashboardSalesHelper.CalcChangePercent(customers.NewCustomersThisMonth, customers.NewCustomersLastMonth),
                OnlineNow = customers.OnlineNow,
                BannedCustomers = customers.BannedCustomers,
                ChurnRatePercent = Percent(customers.ExpiredThisMonth, customers.ActiveCustomers + customers.ExpiredThisMonth),
                TopAgents = topAgents,
                TopPlans = topPlans,
                TopDebtors = topDebtors
            };
        }

        private async Task<ManagementDashboardChartsViewModel> BuildChartsAsync()
        {
            var today = DateTime.Now.Date;
            var range = DashboardSalesHelper.GetPersianMonthRange(today);
            var salesData = DashboardSalesHelper.LoadSalesData(db);
            var monthChannel = DashboardSalesHelper.SumChannelSales(salesData, range.ThisMonthStart, range.ThisMonthEnd);

            var trendLabels = new List<string>();
            var trendSales = new List<double>();
            for (var i = 13; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var start = day;
                var end = day.Date.AddDays(1).AddTicks(-1);
                trendLabels.Add(day.ConvertDateTimeToMonthAndDay());
                trendSales.Add(Math.Round(DashboardSalesHelper.SumSalesInRange(salesData, start, end), 0));
            }

            var createdActions = new[] { Resource.LogActions.U_Created, ReservedPackageHelper.CreatedLogAction };
            var editedActions = new[] { Resource.LogActions.U_Edited, ReservedPackageHelper.EditedLogAction };

            var monthLogs = db.tbLogs
                .Where(l => l.CreateDatetime.HasValue
                    && l.CreateDatetime >= range.ThisMonthStart
                    && l.CreateDatetime <= range.ThisMonthEnd)
                .Select(l => new { l.Action, l.SalePrice })
                .ToList();

            var botOrders = db.tbOrders
                .Where(o => o.OrderStatus == "FINISH"
                    && o.OrderDate.HasValue
                    && o.OrderDate >= range.ThisMonthStart
                    && o.OrderDate <= range.ThisMonthEnd)
                .Select(o => new { o.OrderType, o.Order_Price })
                .ToList();

            var newAmount = monthLogs.Where(l => createdActions.Contains(l.Action)).Sum(l => l.SalePrice ?? 0)
                + botOrders.Where(o => o.OrderType != "تمدید").Sum(o => o.Order_Price ?? 0);
            var renewAmount = monthLogs.Where(l => editedActions.Contains(l.Action)).Sum(l => l.SalePrice ?? 0)
                + botOrders.Where(o => o.OrderType == "تمدید").Sum(o => o.Order_Price ?? 0);

            var agents = repositoryUser.table
                .Where(u => u.Role != 1 && u.Status == true)
                .Select(u => u.Username)
                .ToList();
            var topAgents = BuildTopAgents(salesData, agents, range);

            var customers = await LoadCustomerKpisAsync(range);

            return new ManagementDashboardChartsViewModel
            {
                TrendLabels = trendLabels,
                TrendSales = trendSales,
                ChannelLabels = new List<string> { "ربات", "نمایندگان" },
                ChannelValues = new List<double>
                {
                    Math.Round(monthChannel.Bot, 0),
                    Math.Round(monthChannel.Agent + monthChannel.Master, 0)
                },
                MixLabels = new List<string> { "خرید جدید", "تمدید" },
                MixValues = new List<double>
                {
                    Math.Round(newAmount, 0),
                    Math.Round(renewAmount, 0)
                },
                CustomerLabels = new List<string> { "فعال", "در آستانه انقضا", "ترافیک تمام", "منقضی" },
                CustomerValues = new List<double>
                {
                    customers.ActiveCustomers,
                    customers.ExpiringSoonCount,
                    customers.TrafficExhaustedCount,
                    customers.ExpiredCustomers
                },
                AgentLabels = topAgents.Select(a => a.Name).ToList(),
                AgentValues = topAgents.Select(a => Math.Round(a.Amount, 0)).ToList()
            };
        }

        private List<NamedAmountViewModel> BuildTopAgents(SalesDataSnapshot salesData, List<string> usernames, PersianMonthRange range)
        {
            var result = new List<NamedAmountViewModel>();
            if (salesData == null || usernames == null)
                return result;

            foreach (var username in usernames)
            {
                if (string.IsNullOrWhiteSpace(username))
                    continue;

                var bot = salesData.BotSales
                    .Select(s => new { Item = s, Date = Utility.ParseBotSaleOrderDate(s.OrderDate) })
                    .Where(x => x.Date.HasValue
                        && string.Equals(x.Item.Username, username, StringComparison.OrdinalIgnoreCase)
                        && x.Date.Value >= range.ThisMonthStart
                        && x.Date.Value <= range.ThisMonthEnd)
                    .Sum(x => x.Item.SalePrice ?? 0);

                var user = salesData.UserSales
                    .Where(s => string.Equals(s.Username, username, StringComparison.OrdinalIgnoreCase)
                        && s.CreateDate.HasValue
                        && s.CreateDate.Value >= range.ThisMonthStart
                        && s.CreateDate.Value <= range.ThisMonthEnd)
                    .Sum(s => s.SalePrice ?? 0);

                var master = salesData.MasterSales
                    .Where(s => string.Equals(s.Username, username, StringComparison.OrdinalIgnoreCase)
                        && s.CreateDate.HasValue
                        && s.CreateDate.Value >= range.ThisMonthStart
                        && s.CreateDate.Value <= range.ThisMonthEnd)
                    .Sum(s => s.SalePrice ?? 0);

                var total = bot + user + master;
                if (total <= 0)
                    continue;

                result.Add(new NamedAmountViewModel
                {
                    Name = username,
                    Amount = total,
                    Count = 0
                });
            }

            return result.OrderByDescending(a => a.Amount).Take(8).ToList();
        }

        private async Task<CustomerKpiSnapshot> LoadCustomerKpisAsync(PersianMonthRange range)
        {
            var snapshot = new CustomerKpiSnapshot();
            var user = await repositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
            if (user == null || user.tbServers == null || string.IsNullOrWhiteSpace(user.tbServers.ConnectionString))
                return snapshot;

            var monthStartUnix = (long)range.ThisMonthStart.ConvertDatetimeToSecond();
            var lastMonthStartUnix = (long)range.LastMonthStart.ConvertDatetimeToSecond();
            const int sevenDays = 7 * 86400;

            var query = @"SELECT
                COUNT(*) AS total_users,
                COALESCE(SUM(CASE WHEN expired_at >= UNIX_TIMESTAMP() AND (u + d) < transfer_enable THEN 1 ELSE 0 END), 0) AS active_users,
                COALESCE(SUM(CASE WHEN expired_at < UNIX_TIMESTAMP() THEN 1 ELSE 0 END), 0) AS expired_users,
                COALESCE(SUM(CASE WHEN expired_at >= UNIX_TIMESTAMP() AND expired_at < UNIX_TIMESTAMP() + @sevenDays AND (u + d) < transfer_enable THEN 1 ELSE 0 END), 0) AS expiring_7d,
                COALESCE(SUM(CASE WHEN expired_at >= UNIX_TIMESTAMP() AND (u + d) >= transfer_enable THEN 1 ELSE 0 END), 0) AS traffic_exhausted,
                COALESCE(SUM(CASE WHEN created_at >= @monthStart THEN 1 ELSE 0 END), 0) AS new_this_month,
                COALESCE(SUM(CASE WHEN created_at >= @lastMonthStart AND created_at < @monthStart THEN 1 ELSE 0 END), 0) AS new_last_month,
                COALESCE(SUM(CASE WHEN expired_at >= @monthStart AND expired_at < UNIX_TIMESTAMP() THEN 1 ELSE 0 END), 0) AS expired_this_month,
                COALESCE(SUM(CASE WHEN (UNIX_TIMESTAMP() - t) < 60 AND expired_at >= UNIX_TIMESTAMP() AND (u + d) < transfer_enable THEN 1 ELSE 0 END), 0) AS online_now,
                COALESCE(SUM(CASE WHEN banned = 1 THEN 1 ELSE 0 END), 0) AS banned_users
                FROM v2_user";

            var parameters = new Dictionary<string, object>
            {
                { "@monthStart", monthStartUnix },
                { "@lastMonthStart", lastMonthStartUnix },
                { "@sevenDays", sevenDays }
            };

            try
            {
                using (var mysql = new MySqlEntities(user.tbServers.ConnectionString))
                {
                    await mysql.OpenAsync();
                    using (var reader = await mysql.GetDataAsync(query, parameters))
                    {
                        if (await reader.ReadAsync())
                        {
                            snapshot.TotalCustomers = ReadInt(reader, "total_users");
                            snapshot.ActiveCustomers = ReadInt(reader, "active_users");
                            snapshot.ExpiredCustomers = ReadInt(reader, "expired_users");
                            snapshot.ExpiringSoonCount = ReadInt(reader, "expiring_7d");
                            snapshot.TrafficExhaustedCount = ReadInt(reader, "traffic_exhausted");
                            snapshot.NewCustomersThisMonth = ReadInt(reader, "new_this_month");
                            snapshot.NewCustomersLastMonth = ReadInt(reader, "new_last_month");
                            snapshot.ExpiredThisMonth = ReadInt(reader, "expired_this_month");
                            snapshot.OnlineNow = ReadInt(reader, "online_now");
                            snapshot.BannedCustomers = ReadInt(reader, "banned_users");
                        }
                    }
                    await mysql.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "خطا در خواندن شاخص‌های مشتریان از MySQL");
            }

            return snapshot;
        }

        private static int ReadInt(System.Data.Common.DbDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
                return 0;

            return Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static double Percent(double part, double total)
        {
            if (total <= 0)
                return 0;

            return Math.Round((part / total) * 100, 1);
        }

        private sealed class CustomerKpiSnapshot
        {
            public int TotalCustomers { get; set; }
            public int ActiveCustomers { get; set; }
            public int ExpiredCustomers { get; set; }
            public int ExpiringSoonCount { get; set; }
            public int TrafficExhaustedCount { get; set; }
            public int NewCustomersThisMonth { get; set; }
            public int NewCustomersLastMonth { get; set; }
            public int ExpiredThisMonth { get; set; }
            public int OnlineNow { get; set; }
            public int BannedCustomers { get; set; }
        }
    }
}
