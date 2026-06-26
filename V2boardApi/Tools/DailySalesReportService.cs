using DataLayer.DomainModel;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace V2boardApi.Tools
{
    public class DailySalesReportData
    {
        public string Title { get; set; }
        public string DateLabel { get; set; }
        public double TotalSalesAmount { get; set; }
        public int TotalSubscriptionCount { get; set; }
        public double AgentSalesAmount { get; set; }
        public double BotSalesAmount { get; set; }
        public double TotalUsageGb { get; set; }
    }

    public static class DailySalesReportService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task SendDailyReportsAsync(tbServers server)
        {
            if (server == null)
                return;

            var dayStart = DateTime.Today;
            var dayEnd = DateTime.Today.AddDays(1).AddSeconds(-1);

            try
            {
                using (var db = new Entities())
                {
                    var botSales = db.GetBotSales().ToList();
                    var userSales = db.GetUserSales().ToList();
                    var masterSales = db.GetMasterUserSales().ToList();

                    var adminReport = await BuildAdminReportAsync(dayStart, dayEnd, botSales, userSales, masterSales, server);
                    await SendAdminReportAsync(server, db, adminReport);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ارسال گزارش فروش روزانه");
            }
        }

        private static async Task<DailySalesReportData> BuildAdminReportAsync(
            DateTime dayStart,
            DateTime dayEnd,
            List<GetBotSales_Result> botSales,
            List<GetUserSales_Result> userSales,
            List<GetMasterUserSales_Result> masterSales,
            tbServers server)
        {
            var botToday = FilterBotSales(botSales, dayStart, dayEnd);
            var userToday = FilterUserSales(userSales, dayStart, dayEnd);
            var masterToday = FilterMasterSales(masterSales, dayStart, dayEnd);

            var agentAmount = userToday.Sum(s => s.SalePrice ?? 0) + masterToday.Sum(s => s.SalePrice ?? 0);
            var botAmount = botToday.Sum(s => s.SalePrice ?? 0);

            return new DailySalesReportData
            {
                Title = "گزارش عملکرد فروش روزانه — کل سیستم",
                DateLabel = dayStart.ConvertDateTimeToShamsi2(),
                TotalSalesAmount = agentAmount + botAmount,
                TotalSubscriptionCount = userToday.Count + masterToday.Count + botToday.Count,
                AgentSalesAmount = agentAmount,
                BotSalesAmount = botAmount,
                TotalUsageGb = await GetTotalUsageGbAsync(server, dayStart, dayEnd, null)
            };
        }

        private static async Task<DailySalesReportData> BuildAgentReportAsync(
            tbUsers agent,
            DateTime dayStart,
            DateTime dayEnd,
            List<GetBotSales_Result> botSales,
            List<GetUserSales_Result> userSales,
            List<GetMasterUserSales_Result> masterSales,
            tbServers server)
        {
            var username = agent.Username;
            var botToday = FilterBotSales(botSales, dayStart, dayEnd, username);
            var userToday = FilterUserSales(userSales, dayStart, dayEnd, username);
            var masterToday = FilterMasterSales(masterSales, dayStart, dayEnd, username);

            var agentAmount = userToday.Sum(s => s.SalePrice ?? 0) + masterToday.Sum(s => s.SalePrice ?? 0);
            var botAmount = botToday.Sum(s => s.SalePrice ?? 0);

            var displayName = string.IsNullOrWhiteSpace(agent.FullName) ? username : agent.FullName;

            return new DailySalesReportData
            {
                Title = "گزارش عملکرد فروش روزانه — " + displayName,
                DateLabel = dayStart.ConvertDateTimeToShamsi2(),
                TotalSalesAmount = agentAmount + botAmount,
                TotalSubscriptionCount = userToday.Count + masterToday.Count + botToday.Count,
                AgentSalesAmount = agentAmount,
                BotSalesAmount = botAmount,
                TotalUsageGb = await GetTotalUsageGbAsync(server, dayStart, dayEnd, username)
            };
        }

        private static List<GetBotSales_Result> FilterBotSales(List<GetBotSales_Result> source, DateTime start, DateTime end, string username = null)
        {
            return source.Where(s =>
            {
                if (username != null && !string.Equals(s.Username, username, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (string.IsNullOrWhiteSpace(s.OrderDate))
                    return false;
                DateTime dt;
                if (!Utility.TryParseBotSaleOrderDate(s.OrderDate, out dt))
                    return false;
                return dt >= start && dt <= end;
            }).ToList();
        }

        private static List<GetUserSales_Result> FilterUserSales(List<GetUserSales_Result> source, DateTime start, DateTime end, string username = null)
        {
            return source.Where(s =>
            {
                if (username != null && !string.Equals(s.Username, username, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (!s.CreateDate.HasValue)
                    return false;
                return s.CreateDate.Value >= start && s.CreateDate.Value <= end;
            }).ToList();
        }

        private static List<GetMasterUserSales_Result> FilterMasterSales(List<GetMasterUserSales_Result> source, DateTime start, DateTime end, string username = null)
        {
            return source.Where(s =>
            {
                if (username != null && !string.Equals(s.Username, username, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (!s.CreateDate.HasValue)
                    return false;
                return s.CreateDate.Value >= start && s.CreateDate.Value <= end;
            }).ToList();
        }

        private static async Task<double> GetTotalUsageGbAsync(tbServers server, DateTime dayStart, DateTime dayEnd, string agentUsername)
        {
            try
            {
                var unixStart = (long)dayStart.ConvertDatetimeToSecond();
                var unixEnd = (long)dayEnd.ConvertDatetimeToSecond();

                using (var mysql = new MySqlEntities(server.ConnectionString))
                {
                    await mysql.OpenAsync();

                    string query;
                    Dictionary<string, object> parameters;

                    if (string.IsNullOrEmpty(agentUsername))
                    {
                        query = "SELECT SUM(u + d) AS total FROM v2_stat_user WHERE created_at >= @start AND created_at <= @end";
                        parameters = new Dictionary<string, object>
                        {
                            { "@start", unixStart },
                            { "@end", unixEnd }
                        };
                    }
                    else
                    {
                        query = "SELECT SUM(s.u + s.d) AS total FROM v2_stat_user s " +
                                  "INNER JOIN v2_user v ON v.id = s.user_id " +
                                  "WHERE s.created_at >= @start AND s.created_at <= @end AND v.email LIKE @pattern";
                        parameters = new Dictionary<string, object>
                        {
                            { "@start", unixStart },
                            { "@end", unixEnd },
                            { "@pattern", "%@" + agentUsername }
                        };
                    }

                    long total = 0;
                    using (var reader = await mysql.GetDataAsync(query, parameters))
                    {
                        if (await reader.ReadAsync())
                        {
                            if (!reader.IsDBNull(reader.GetOrdinal("total")))
                                total = reader.GetInt64("total");
                        }
                    }

                    await mysql.CloseAsync();
                    return Math.Round(Utility.ConvertByteToGB(total), 2);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "خطا در محاسبه حجم مصرفی گزارش روزانه");
                return 0;
            }
        }

        public static string FormatReportMessage(DailySalesReportData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>📊 " + data.Title + "</b>");
            sb.AppendLine("");
            sb.AppendLine("📅 <b>تاریخ:</b> " + data.DateLabel);
            sb.AppendLine("");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━");
            sb.AppendLine("💰 <b>فروش کل:</b> " + data.TotalSalesAmount.ConvertToMony() + " تومان");
            sb.AppendLine("📦 <b>تعداد اشتراک:</b> " + data.TotalSubscriptionCount.ToString("N0", CultureInfo.InvariantCulture));
            sb.AppendLine("");
            sb.AppendLine("👥 <b>فروش نمایندگان:</b> " + data.AgentSalesAmount.ConvertToMony() + " تومان");
            sb.AppendLine("🤖 <b>فروش ربات:</b> " + data.BotSalesAmount.ConvertToMony() + " تومان");
            sb.AppendLine("");
            sb.AppendLine("📶 <b>حجم مصرفی کل:</b> " + data.TotalUsageGb.ToString("N2", CultureInfo.InvariantCulture) + " GB");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━");
            sb.AppendLine("");
            sb.AppendLine("<i>🕚 گزارش خودکار — ساعت 23:59</i>");
            return sb.ToString();
        }

        private static tbUsers GetAdminUser(Entities db, tbServers server)
        {
            return db.tbUsers
                .Include(u => u.tbBotSettings)
                .FirstOrDefault(u => u.Role == 1 && u.Status == true && u.FK_Server_ID == server.ServerID);
        }

        private static TelegramBotClient ResolveAdminBotClient(tbServers server, Entities db)
        {
            var botToken = server.Robot_Token;
            tbUsers admin = null;

            if (string.IsNullOrEmpty(botToken))
            {
                admin = GetAdminUser(db, server);
                botToken = admin?.tbBotSettings?.FirstOrDefault()?.Bot_Token;
            }

            if (string.IsNullOrEmpty(botToken))
                return null;

            if (admin == null)
                admin = GetAdminUser(db, server);

            if (admin != null)
            {
                var cached = BotManager.GetBot(admin.Username);
                if (cached != null && string.Equals(cached.Token, botToken, StringComparison.Ordinal))
                    return cached.Client;
            }

            return new TelegramBotClient(botToken);
        }

        private static string ResolveAgentChatId(tbUsers agent, tbServers server, Entities db)
        {
            if (string.IsNullOrEmpty(agent.TelegramID))
                return null;

            tbTelegramUsers telUser = null;
            if (!string.IsNullOrEmpty(server.Robot_ID))
            {
                telUser = db.tbTelegramUsers.FirstOrDefault(t =>
                    t.Tel_Username == agent.TelegramID && t.Tel_RobotID == server.Robot_ID);
            }

            if (telUser == null)
                telUser = db.tbTelegramUsers.FirstOrDefault(t => t.Tel_Username == agent.TelegramID);

            return telUser?.Tel_UniqUserID;
        }

        private static async Task SendAdminReportAsync(tbServers server, Entities db, DailySalesReportData report)
        {
            var message = FormatReportMessage(report);
            var bot = ResolveAdminBotClient(server, db);
            long? chatId = server.AdminTelegramUniqID;

            if (!chatId.HasValue)
            {
                var admin = GetAdminUser(db, server);
                var botSetting = admin?.tbBotSettings?.FirstOrDefault();
                if (botSetting?.AdminBot_ID > 0)
                    chatId = botSetting.AdminBot_ID;
            }

            if (bot == null || !chatId.HasValue)
            {
                logger.Warn("گزارش روزانه ادمین ارسال نشد: توکن ربات یا شناسه تلگرام ادمین تنظیم نشده");
                return;
            }

            try
            {
                await bot.SendMessage(chatId.Value, message, parseMode: ParseMode.Html);
                logger.Info("گزارش فروش روزانه به ادمین ارسال شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ارسال گزارش روزانه به ادمین");
            }
        }

        private static async Task SendAgentReportAsync(tbServers server, Entities db, tbUsers agent, DailySalesReportData report)
        {
            var bot = ResolveAdminBotClient(server, db);
            if (bot == null)
            {
                logger.Warn("گزارش روزانه به نماینده " + agent.Username + " ارسال نشد: توکن ربات اصلی سیستم تنظیم نشده");
                return;
            }

            var chatId = ResolveAgentChatId(agent, server, db);
            if (string.IsNullOrEmpty(chatId))
                return;

            try
            {
                var message = FormatReportMessage(report);
                await bot.SendMessage(chatId, message, parseMode: ParseMode.Html);
                logger.Info("گزارش فروش روزانه به نماینده " + agent.Username + " از ربات اصلی ارسال شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ارسال گزارش روزانه به نماینده " + agent.Username);
            }
        }
    }
}
