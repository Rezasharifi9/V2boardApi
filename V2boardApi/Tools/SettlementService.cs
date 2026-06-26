using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace V2boardApi.Tools
{
    /// <summary>
    /// تسویه بدهی نماینده بر اساس فاصله از آخرین فاکتور پرداخت‌شده (نه تقویم هفتگی/ماهانه).
    /// </summary>
    public static class SettlementService
    {
        public const string RemarksFlag = "[SETTLEMENT_BLOCK]";
        public const int DefaultDaysAfterLastPayment = 15;
        public const int DefaultPreWarningDays = 2;
        public const int DefaultBlockGraceDays = 2;
        public const int OverdueReminderHours = 6;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static int GetDaysAfterLastPayment(tbUsers user)
        {
            return Math.Max(1, user.Settlement_DayOfMonth ?? DefaultDaysAfterLastPayment);
        }

        public static int GetPreWarningDays(tbUsers user)
        {
            var days = GetDaysAfterLastPayment(user);
            var pre = user.Settlement_DayOfWeek ?? DefaultPreWarningDays;
            return Math.Max(0, Math.Min(days, pre));
        }

        public static int GetBlockGraceDays(tbUsers user)
        {
            return Math.Max(0, user.Settlement_BlockGraceDays ?? DefaultBlockGraceDays);
        }

        public static DateTime? GetLastPaidFactorDate(Entities db, tbUsers agent)
        {
            return db.tbUserFactors
                .Where(f => f.FK_User_ID == agent.User_ID && f.tbUf_Status == 3 && f.tbUf_CreateTime.HasValue)
                .OrderByDescending(f => f.tbUf_CreateTime)
                .Select(f => f.tbUf_CreateTime)
                .FirstOrDefault();
        }

        public static DateTime GetSettlementAnchor(Entities db, tbUsers agent)
        {
            var lastPaid = GetLastPaidFactorDate(db, agent);
            if (lastPaid.HasValue)
                return lastPaid.Value.Date;

            if (agent.Settlement_StartDate.HasValue)
                return agent.Settlement_StartDate.Value.Date;

            return DateTime.Now.Date;
        }

        public static List<string> GetNetworkUsernames(tbUsers agent, Entities db)
        {
            var result = new List<string>();
            var queue = new Queue<int>();
            queue.Enqueue(agent.User_ID);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                var current = db.tbUsers.FirstOrDefault(u => u.User_ID == id);
                if (current == null || string.IsNullOrEmpty(current.Username))
                    continue;

                if (!result.Contains(current.Username))
                    result.Add(current.Username);

                var children = db.tbUsers
                    .Where(u => u.Parent_ID == id && u.Status == true)
                    .Select(u => u.User_ID)
                    .ToList();

                foreach (var childId in children)
                    queue.Enqueue(childId);
            }

            return result;
        }

        public static async Task SendAgentTelegramMessage(tbUsers agent, string message)
        {
            try
            {
                var botSetting = agent.tbBotSettings?.FirstOrDefault();
                if (botSetting == null || string.IsNullOrEmpty(botSetting.Bot_Token))
                    return;

                TelegramBotClient client;
                var cached = BotManager.GetBot(agent.Username);
                client = cached != null ? cached.Client : new TelegramBotClient(botSetting.Bot_Token);

                if (!string.IsNullOrEmpty(agent.TelegramID))
                {
                    using (var db = new Entities())
                    {
                        var telUser = db.tbTelegramUsers.FirstOrDefault(t => t.Tel_Username == agent.TelegramID);
                        if (telUser != null)
                            await client.SendMessage(telUser.Tel_UniqUserID, message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "خطا در ارسال پیام تسویه به نماینده " + agent.Username);
            }
        }

        public static async Task BlockAgentSubscriptions(tbUsers agent, Entities db)
        {
            if (agent.tbServers == null || string.IsNullOrEmpty(agent.tbServers.ConnectionString))
                return;

            var usernames = GetNetworkUsernames(agent, db);
            using (var mysql = new MySqlEntities(agent.tbServers.ConnectionString))
            {
                await mysql.OpenAsync();
                foreach (var username in usernames)
                {
                    var query = "UPDATE v2_user SET banned = 1, remarks = CONCAT(IFNULL(remarks,''), @flag) " +
                                "WHERE email LIKE @pattern AND (remarks IS NULL OR remarks NOT LIKE @flagLike)";
                    var parameters = new Dictionary<string, object>
                    {
                        { "@flag", RemarksFlag },
                        { "@flagLike", "%" + RemarksFlag + "%" },
                        { "@pattern", "%@" + username }
                    };
                    using (var reader = await mysql.GetDataAsync(query, parameters))
                    {
                        while (await reader.ReadAsync()) { }
                    }
                }
                await mysql.CloseAsync();
            }
        }

        public static async Task UnblockAgentSubscriptions(tbUsers agent, Entities db)
        {
            if (agent.tbServers == null || string.IsNullOrEmpty(agent.tbServers.ConnectionString))
                return;

            var usernames = GetNetworkUsernames(agent, db);
            using (var mysql = new MySqlEntities(agent.tbServers.ConnectionString))
            {
                await mysql.OpenAsync();
                foreach (var username in usernames)
                {
                    var query = "UPDATE v2_user SET banned = 0, remarks = REPLACE(IFNULL(remarks,''), @flag, '') " +
                                "WHERE email LIKE @pattern AND remarks LIKE @flagLike";
                    var parameters = new Dictionary<string, object>
                    {
                        { "@flag", RemarksFlag },
                        { "@flagLike", "%" + RemarksFlag + "%" },
                        { "@pattern", "%@" + username }
                    };
                    using (var reader = await mysql.GetDataAsync(query, parameters))
                    {
                        while (await reader.ReadAsync()) { }
                    }
                }
                await mysql.CloseAsync();
            }
        }

        public static void ResetSettlementWarnings(tbUsers agent)
        {
            agent.Settlement_LastPreWarning = null;
            agent.Settlement_LastOverdueWarning = null;
            agent.Settlement_LastDueDayWarning = null;
        }

        public static async Task OnAgentPaymentConfirmed(tbUsers agent, Entities db)
        {
            var wasBlocked = agent.Settlement_IsBlocked;

            if (wasBlocked)
                await UnblockAgentSubscriptions(agent, db);

            agent.Settlement_IsBlocked = false;
            ResetSettlementWarnings(agent);

            var lastPaid = GetLastPaidFactorDate(db, agent);
            if (lastPaid.HasValue)
                agent.Settlement_StartDate = lastPaid.Value.Date;

            await db.SaveChangesAsync();

            var msg = new StringBuilder();
            msg.AppendLine("نماینده گرامی");
            msg.AppendLine("");
            if (wasBlocked)
                msg.AppendLine("✅ پرداخت بدهی شما ثبت شد و تمامی اشتراک‌های زیرمجموعه مجدداً فعال گردید.");
            else
                msg.AppendLine("✅ پرداخت فاکتور شما ثبت شد. مهلت تسویه بعدی از امروز محاسبه می‌شود.");

            var dueDate = GetSettlementAnchor(db, agent).AddDays(GetDaysAfterLastPayment(agent));
            msg.AppendLine("📅 موعد تسویه بعدی: " + dueDate.ToString("yyyy/MM/dd", CultureInfo.GetCultureInfo("fa-IR")));
            await SendAgentTelegramMessage(agent, msg.ToString());
        }

        public static async Task ProcessAgent(tbUsers agent, Entities db, DateTime now)
        {
            if (!agent.Settlement_Enabled || agent.Status != true)
            {
                if (agent.Settlement_IsBlocked)
                {
                    await UnblockAgentSubscriptions(agent, db);
                    agent.Settlement_IsBlocked = false;
                    ResetSettlementWarnings(agent);
                    await db.SaveChangesAsync();
                }
                return;
            }

            var anchor = GetSettlementAnchor(db, agent);
            var daysUntilDue = GetDaysAfterLastPayment(agent);
            var preWarningDays = GetPreWarningDays(agent);
            var blockGraceDays = GetBlockGraceDays(agent);

            var dueDate = anchor.AddDays(daysUntilDue);
            var preWarningDate = dueDate.AddDays(-preWarningDays);
            var blockDate = dueDate.AddDays(blockGraceDays);
            var dueEnd = dueDate.Date.AddDays(1).AddSeconds(-1);

            var dueLabel = dueDate.ToString("yyyy/MM/dd", CultureInfo.GetCultureInfo("fa-IR"));

            if (now < preWarningDate)
                return;

            if (now.Date >= preWarningDate.Date && now.Date < dueDate.Date)
            {
                if (!agent.Settlement_LastPreWarning.HasValue ||
                    agent.Settlement_LastPreWarning.Value.Date < preWarningDate.Date)
                {
                    var remaining = (dueDate.Date - now.Date).Days;
                    var msg = new StringBuilder();
                    msg.AppendLine("⚠️ نماینده گرامی");
                    msg.AppendLine("");
                    msg.AppendLine(remaining + " روز تا موعد تسویه (" + dueLabel + ") باقی مانده است.");
                    msg.AppendLine("لطفاً در اسرع وقت نسبت به پرداخت بدهی اقدام کنید.");
                    await SendAgentTelegramMessage(agent, msg.ToString());
                    agent.Settlement_LastPreWarning = now;
                    await db.SaveChangesAsync();
                }
            }

            if (now.Date == dueDate.Date)
            {
                if (!agent.Settlement_LastDueDayWarning.HasValue ||
                    agent.Settlement_LastDueDayWarning.Value.Date < dueDate.Date)
                {
                    var msg = new StringBuilder();
                    msg.AppendLine("📅 نماینده گرامی");
                    msg.AppendLine("");
                    msg.AppendLine("سررسید تسویه شما (" + dueLabel + ") فرا رسیده است.");
                    msg.AppendLine("لطفاً در اسرع وقت نسبت به پرداخت بدهی اقدام کنید.");
                    await SendAgentTelegramMessage(agent, msg.ToString());
                    agent.Settlement_LastDueDayWarning = now;
                    await db.SaveChangesAsync();
                }
            }

            if (now > dueEnd && now < blockDate)
            {
                if (!agent.Settlement_LastOverdueWarning.HasValue ||
                    (now - agent.Settlement_LastOverdueWarning.Value).TotalHours >= OverdueReminderHours)
                {
                    var msg = new StringBuilder();
                    msg.AppendLine("❌ نماینده گرامی");
                    msg.AppendLine("");
                    msg.AppendLine("مهلت پرداخت شما به اتمام رسیده است.");
                    msg.AppendLine("لطفاً در اسرع وقت نسبت به پرداخت بدهی اقدام کنید.");
                    await SendAgentTelegramMessage(agent, msg.ToString());
                    agent.Settlement_LastOverdueWarning = now;
                    await db.SaveChangesAsync();
                }
            }

            if (now >= blockDate && !agent.Settlement_IsBlocked)
            {
                await BlockAgentSubscriptions(agent, db);
                agent.Settlement_IsBlocked = true;
                await db.SaveChangesAsync();

                var msg = new StringBuilder();
                msg.AppendLine("🚫 نماینده گرامی");
                msg.AppendLine("");
                msg.AppendLine("تمامی اشتراک‌های زیرمجموعه شما مسدود گردید.");
                msg.AppendLine("لطفاً نسبت به پرداخت بدهی اقدام کنید؛ پس از ثبت پرداخت، اشتراک‌ها خودکار فعال می‌شوند.");
                await SendAgentTelegramMessage(agent, msg.ToString());
            }
        }

        public static async Task ProcessAllAgents()
        {
            try
            {
                using (var db = new Entities())
                {
                    var agents = db.tbUsers
                        .Include(u => u.tbServers)
                        .Include(u => u.tbBotSettings)
                        .Where(u => u.Settlement_Enabled && u.Status == true && (u.Role == 2 || u.Role == 3 || u.Role == 4))
                        .ToList();

                    var now = DateTime.Now;
                    foreach (var agent in agents)
                    {
                        try
                        {
                            await ProcessAgent(agent, db, now);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "خطا در پردازش تسویه نماینده " + agent.Username);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در اجرای سرویس تسویه");
            }
        }
    }
}
