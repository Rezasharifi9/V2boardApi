using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace V2boardApi.Tools
{
    public class SalesDataSnapshot
    {
        public List<GetBotSales_Result> BotSales { get; set; }
        public List<GetUserSales_Result> UserSales { get; set; }
        public List<GetMasterUserSales_Result> MasterSales { get; set; }
    }

    public class PersianWeekBounds
    {
        public DateTime ThisWeekStart { get; set; }
        public DateTime ThisWeekEnd { get; set; }
        public DateTime LastWeekStart { get; set; }
        public DateTime LastWeekEnd { get; set; }
    }

    public class PersianMonthRange
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int PassedDays { get; set; }
        public int LastYear { get; set; }
        public int LastMonth { get; set; }
        public DateTime ThisMonthStart { get; set; }
        public DateTime ThisMonthEnd { get; set; }
        public DateTime LastMonthStart { get; set; }
        public DateTime LastMonthEnd { get; set; }
        public DateTime LastMonthComparableEnd { get; set; }
        public DateTime TodayStart { get; set; }
        public DateTime TodayEnd { get; set; }
        public DateTime YesterdayStart { get; set; }
        public DateTime YesterdayEnd { get; set; }
    }

    public class ChannelSalesBreakdown
    {
        public double Bot { get; set; }
        public double Agent { get; set; }
        public double Master { get; set; }
        public double Total { get { return Bot + Agent + Master; } }
    }

    public static class DashboardSalesHelper
    {
        public static SalesDataSnapshot LoadSalesData(Entities db)
        {
            return new SalesDataSnapshot
            {
                BotSales = db.GetBotSales().ToList(),
                UserSales = db.GetUserSales().ToList(),
                MasterSales = db.GetMasterUserSales().ToList()
            };
        }

        public static PersianWeekBounds GetPersianWeekBounds(DateTime today)
        {
            today = today.Date;
            var daysFromSaturday = ((int)today.DayOfWeek + 1) % 7;
            var thisWeekStart = today.AddDays(-daysFromSaturday);
            var thisWeekEnd = EndOfDay(today);
            var lastWeekStart = thisWeekStart.AddDays(-7);
            var lastWeekEnd = EndOfDay(thisWeekStart.AddDays(-1));

            return new PersianWeekBounds
            {
                ThisWeekStart = thisWeekStart,
                ThisWeekEnd = thisWeekEnd,
                LastWeekStart = lastWeekStart,
                LastWeekEnd = lastWeekEnd
            };
        }

        public static double SumSalesInRange(SalesDataSnapshot data, DateTime start, DateTime end)
        {
            if (data == null)
                return 0;

            var botAmount = FilterBotSales(data.BotSales, start, end).Sum(s => s.SalePrice ?? 0);
            var userAmount = FilterUserSales(data.UserSales, start, end).Sum(s => s.SalePrice ?? 0);
            var masterAmount = FilterMasterSales(data.MasterSales, start, end).Sum(s => s.SalePrice ?? 0);
            return botAmount + userAmount + masterAmount;
        }

        public static double SumSalesForPersianMonth(SalesDataSnapshot data, int year, int month, int? upToDay = null)
        {
            if (data == null)
                return 0;

            var pc = new PersianCalendar();
            var lastDay = upToDay ?? pc.GetDaysInMonth(year, month);
            double total = 0;

            for (var day = 1; day <= lastDay; day++)
                total += SumSalesForPersianDay(data, year, month, day);

            return total;
        }

        public static double SumSalesForPersianDay(SalesDataSnapshot data, int year, int month, int day)
        {
            if (data == null)
                return 0;

            var pc = new PersianCalendar();

            var botAmount = data.BotSales
                .Select(s => new { Item = s, Date = Utility.ParseBotSaleOrderDate(s.OrderDate) })
                .Where(x => x.Date.HasValue
                    && pc.GetYear(x.Date.Value) == year
                    && pc.GetMonth(x.Date.Value) == month
                    && pc.GetDayOfMonth(x.Date.Value) == day)
                .Sum(x => x.Item.SalePrice ?? 0);

            var userAmount = data.UserSales
                .Where(s => s.CreateDate.HasValue
                    && pc.GetYear(s.CreateDate.Value) == year
                    && pc.GetMonth(s.CreateDate.Value) == month
                    && pc.GetDayOfMonth(s.CreateDate.Value) == day)
                .Sum(s => s.SalePrice ?? 0);

            var masterAmount = data.MasterSales
                .Where(s => s.CreateDate.HasValue
                    && pc.GetYear(s.CreateDate.Value) == year
                    && pc.GetMonth(s.CreateDate.Value) == month
                    && pc.GetDayOfMonth(s.CreateDate.Value) == day)
                .Sum(s => s.SalePrice ?? 0);

            return botAmount + userAmount + masterAmount;
        }

        public static List<double> BuildPersianMonthChartData(SalesDataSnapshot data, int year, int month, int upToDay)
        {
            var chartData = new List<double>();
            for (var day = 1; day <= upToDay; day++)
            {
                var amount = SumSalesForPersianDay(data, year, month, day) / 1000d;
                chartData.Add(Math.Round(amount, 0));
            }

            return chartData;
        }

        public static double CalcChangePercent(double current, double previous)
        {
            if (previous <= 0)
                return current > 0 ? 100 : 0;

            return Math.Round(((current - previous) / previous) * 100, 2);
        }

        public static int GetPassedDaysInPersianMonth(DateTime today)
        {
            var pc = new PersianCalendar();
            return pc.GetDayOfMonth(today);
        }

        public static PersianMonthRange GetPersianMonthRange(DateTime today)
        {
            today = today.Date;
            var pc = new PersianCalendar();
            var year = pc.GetYear(today);
            var month = pc.GetMonth(today);
            var day = pc.GetDayOfMonth(today);
            var thisMonthStart = pc.ToDateTime(year, month, 1, 0, 0, 0, 0);

            int lastYear;
            int lastMonth;
            if (month == 1)
            {
                lastYear = year - 1;
                lastMonth = 12;
            }
            else
            {
                lastYear = year;
                lastMonth = month - 1;
            }

            var lastMonthStart = pc.ToDateTime(lastYear, lastMonth, 1, 0, 0, 0, 0);
            var lastMonthDays = pc.GetDaysInMonth(lastYear, lastMonth);
            var lastMonthEnd = EndOfDay(pc.ToDateTime(lastYear, lastMonth, lastMonthDays, 0, 0, 0, 0));
            var comparableDay = Math.Min(day, lastMonthDays);
            var lastMonthComparableEnd = EndOfDay(pc.ToDateTime(lastYear, lastMonth, comparableDay, 0, 0, 0, 0));

            return new PersianMonthRange
            {
                Year = year,
                Month = month,
                PassedDays = day,
                LastYear = lastYear,
                LastMonth = lastMonth,
                ThisMonthStart = thisMonthStart,
                ThisMonthEnd = EndOfDay(today),
                LastMonthStart = lastMonthStart,
                LastMonthEnd = lastMonthEnd,
                LastMonthComparableEnd = lastMonthComparableEnd,
                TodayStart = today,
                TodayEnd = EndOfDay(today),
                YesterdayStart = today.AddDays(-1),
                YesterdayEnd = EndOfDay(today.AddDays(-1))
            };
        }

        public static int CountSalesInRange(SalesDataSnapshot data, DateTime start, DateTime end)
        {
            if (data == null)
                return 0;

            return FilterBotSales(data.BotSales, start, end).Count
                + FilterUserSales(data.UserSales, start, end).Count
                + FilterMasterSales(data.MasterSales, start, end).Count;
        }

        public static ChannelSalesBreakdown SumChannelSales(SalesDataSnapshot data, DateTime start, DateTime end)
        {
            if (data == null)
                return new ChannelSalesBreakdown();

            return new ChannelSalesBreakdown
            {
                Bot = FilterBotSales(data.BotSales, start, end).Sum(s => s.SalePrice ?? 0),
                Agent = FilterUserSales(data.UserSales, start, end).Sum(s => s.SalePrice ?? 0),
                Master = FilterMasterSales(data.MasterSales, start, end).Sum(s => s.SalePrice ?? 0)
            };
        }

        private static DateTime EndOfDay(DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        private static List<GetBotSales_Result> FilterBotSales(List<GetBotSales_Result> source, DateTime start, DateTime end)
        {
            return source.Where(s =>
            {
                if (string.IsNullOrWhiteSpace(s.OrderDate))
                    return false;

                DateTime dt;
                if (!Utility.TryParseBotSaleOrderDate(s.OrderDate, out dt))
                    return false;

                return dt >= start && dt <= end;
            }).ToList();
        }

        private static List<GetUserSales_Result> FilterUserSales(List<GetUserSales_Result> source, DateTime start, DateTime end)
        {
            return source.Where(s =>
            {
                if (!s.CreateDate.HasValue)
                    return false;

                return s.CreateDate.Value >= start && s.CreateDate.Value <= end;
            }).ToList();
        }

        private static List<GetMasterUserSales_Result> FilterMasterSales(List<GetMasterUserSales_Result> source, DateTime start, DateTime end)
        {
            return source.Where(s =>
            {
                if (!s.CreateDate.HasValue)
                    return false;

                return s.CreateDate.Value >= start && s.CreateDate.Value <= end;
            }).ToList();
        }
    }
}
