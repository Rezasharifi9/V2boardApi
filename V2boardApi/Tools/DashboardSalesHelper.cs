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
