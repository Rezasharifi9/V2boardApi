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
using V2boardApi.Tools;

namespace V2boardApi.Tools
{
    public static class SettlementService
    {
        public const string RemarksFlag = "[SETTLEMENT_BLOCK]";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static DateTime GetDueDateOnOrAfter(DateTime date, tbUsers user)
        {
            var start = user.Settlement_StartDate?.Date ?? date.Date;
            if (date.Date < start)
                date = start;

            if (string.Equals(user.Settlement_Type, "Monthly", StringComparison.OrdinalIgnoreCase))
            {
                int dom = user.Settlement_DayOfMonth ?? 1;
                dom = Math.Max(1, Math.Min(31, dom));
                var candidate = new DateTime(date.Year, date.Month, Math.Min(dom, DateTime.DaysInMonth(date.Year, date.Month)));
                if (candidate < date.Date)
                {
                    var next = date.AddMonths(1);
                    candidate = new DateTime(next.Year, next.Month, Math.Min(dom, DateTime.DaysInMonth(next.Year, next.Month)));
                }
                return candidate;
            }

            int targetDow = user.Settlement_DayOfWeek ?? (int)DayOfWeek.Saturday;
            int daysUntil = ((targetDow - (int)date.DayOfWeek) + 7) % 7;
            return date.Date.AddDays(daysUntil);
        }

        public static DateTime? GetLastDueDate(DateTime now, tbUsers user)
        {
            if (!user.Settlement_StartDate.HasValue)
                return null;

            var cursor = user.Settlement_StartDate.Value.Date;
            DateTime? last = null;
            var today = now.Date;

            while (true)
            {
                var due = GetDueDateOnOrAfter(cursor, user);
                if (due.Date > today)
                    break;
                last = due;
                cursor = due.AddDays(1);
            }

            return last;
        }

        public static DateTime? GetPreviousDueDate(DateTime dueDate, tbUsers user)
        {
            if (!user.Settlement_StartDate.HasValue)
                return null;

            var cursor = user.Settlement_StartDate.Value.Date;
            DateTime? prev = null;

            while (true)
            {
                var due = GetDueDateOnOrAfter(cursor, user);
                if (due.Date >= dueDate.Date)
                    break;
                prev = due;
                cursor = due.AddDays(1);
            }

            return prev;
        }

        public static bool HasPaidInSettlementPeriod(Entities db, tbUsers agent, DateTime periodStart, DateTime dueEnd)
        {
            return db.tbUserFactors.Any(f =>
                f.FK_User_ID == agent.User_ID &&
                f.tbUf_Status == 3 &&
                f.tbUf_CreateTime >= periodStart &&
                f.tbUf_CreateTime <= DateTime.Now);
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

                var children = db.tbUsers.Where(u => u.Parent_ID == id && u.Status == true).Select(u => u.User_ID).ToList();
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

                TelegramBotClient client = null;
                var cached = BotManager.GetBot(agent.Username);
                if (cached != null)
                    client = cached.Client;
                else
                    client = new TelegramBotClient(botSetting.Bot_Token);

                //if (botSetting.AdminBot_ID > 0)
                //{
                //    await client.SendMessage(botSetting.AdminBot_ID, message);
                //    return;
                //}

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
                    var reader = await mysql.GetDataAsync(query, parameters);
                    while (await reader.ReadAsync()) { }
                    reader.Close();
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
                    var reader = await mysql.GetDataAsync(query, parameters);
                    while (await reader.ReadAsync()) { }
                    reader.Close();
                }
                await mysql.CloseAsync();
            }
        }

        public static async Task OnAgentPaymentConfirmed(tbUsers agent, Entities db)
        {
            if (!agent.Settlement_IsBlocked)
                return;

            await UnblockAgentSubscriptions(agent, db);
            agent.Settlement_IsBlocked = false;
            agent.Settlement_LastPreWarning = null;
            agent.Settlement_LastOverdueWarning = null;
            await db.SaveChangesAsync();

            var msg = new StringBuilder();
            msg.AppendLine("نماینده گرامی");
            msg.AppendLine("");
            msg.AppendLine("✅ پرداخت تسویه شما ثبت شد و اشتراک‌های زیرمجموعه مجدداً فعال گردید.");
            await SendAgentTelegramMessage(agent, msg.ToString());
        }

        public static async Task ProcessAgent(tbUsers agent, Entities db, DateTime now)
        {
            if (!agent.Settlement_Enabled || !agent.Settlement_StartDate.HasValue || agent.Status != true)
            {
                if (agent.Settlement_IsBlocked)
                {
                    await UnblockAgentSubscriptions(agent, db);
                    agent.Settlement_IsBlocked = false;
                    await db.SaveChangesAsync();
                }
                return;
            }

            var lastDue = GetLastDueDate(now, agent);
            DateTime dueDate;
            if (lastDue.HasValue)
                dueDate = lastDue.Value;
            else
            {
                dueDate = GetDueDateOnOrAfter(agent.Settlement_StartDate.Value.Date, agent);
                if (now.Date < dueDate.Date.AddDays(-2))
                    return;
            }

            var prevDue = GetPreviousDueDate(dueDate, agent);
            var periodStart = (prevDue?.Date.AddDays(1) ?? agent.Settlement_StartDate.Value.Date);
            var dueEnd = dueDate.Date.AddDays(1).AddSeconds(-1);
            var preWarningDate = dueDate.Date.AddDays(-2);
            var blockDate = dueDate.Date.AddDays(2);

            bool paid = HasPaidInSettlementPeriod(db, agent, periodStart, dueEnd);

            if (paid)
            {
                if (agent.Settlement_IsBlocked)
                    await OnAgentPaymentConfirmed(agent, db);
                else
                {
                    agent.Settlement_LastPreWarning = null;
                    agent.Settlement_LastOverdueWarning = null;
                    await db.SaveChangesAsync();
                }
                return;
            }

            var dueLabel = dueDate.ToString("yyyy/MM/dd", CultureInfo.GetCultureInfo("fa-IR"));

            if (now.Date >= preWarningDate && now.Date < dueDate.Date)
            {
                if (!agent.Settlement_LastPreWarning.HasValue || agent.Settlement_LastPreWarning.Value.Date < preWarningDate)
                {
                    var msg = new StringBuilder();
                    msg.AppendLine("⚠️ نماینده گرامی");
                    msg.AppendLine("");
                    msg.AppendLine("۲ روز تا موعد تسویه (" + dueLabel + ") باقی مانده است.");
                    msg.AppendLine("لطفاً نسبت به پرداخت بدهی خود از بخش پروفایل من اقدام کنید.");
                    await SendAgentTelegramMessage(agent, msg.ToString());
                    agent.Settlement_LastPreWarning = now;
                    await db.SaveChangesAsync();
                }
            }

            if (now > dueEnd && now.Date < blockDate)
            {
                if (!agent.Settlement_LastOverdueWarning.HasValue ||
                    (now - agent.Settlement_LastOverdueWarning.Value).TotalHours >= 6)
                {
                    var msg = new StringBuilder();
                    msg.AppendLine("❌ نماینده گرامی");
                    msg.AppendLine("");
                    msg.AppendLine("موعد تسویه (" + dueLabel + ") گذشته و هنوز پرداختی ثبت نشده است.");
                    msg.AppendLine("لطفاً هرچه سریع‌تر نسبت به پرداخت بدهی اقدام کنید.");
                    await SendAgentTelegramMessage(agent, msg.ToString());
                    agent.Settlement_LastOverdueWarning = now;
                    await db.SaveChangesAsync();
                }
            }

            if (now.Date >= blockDate && !agent.Settlement_IsBlocked)
            {
                await BlockAgentSubscriptions(agent, db);
                agent.Settlement_IsBlocked = true;
                await db.SaveChangesAsync();

                var msg = new StringBuilder();
                msg.AppendLine("🚫 نماینده گرامی");
                msg.AppendLine("");
                msg.AppendLine("به دلیل عدم پرداخت بدهی پس از موعد تسویه، تمامی اشتراک‌های زیرمجموعه شما مسدود گردید.");
                msg.AppendLine("پس از پرداخت و ثبت فاکتور، اشتراک‌ها به‌صورت خودکار فعال می‌شوند.");
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
