using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using V2boardApi.Areas.App.Data.UsersViewModels;

namespace V2boardApi.Tools
{
    public static class AgentDeleteService
    {
        public static AgentDeletePreviewViewModel BuildPreview(Entities db, tbUsers agent)
        {
            var agentId = agent.User_ID;
            var username = agent.Username ?? string.Empty;

            var telUserIds = db.tbTelegramUsers
                .Where(t => t.FK_User_ID == agentId)
                .Select(t => t.Tel_UserID)
                .ToList();

            var planLinkIds = db.tbLinkUserAndPlans
                .Where(p => p.L_FK_U_ID == agentId)
                .Select(p => p.Link_PU_ID)
                .ToList();

            var subNames = GetAgentSubscriptionNames(db, agentId, username);

            var logCount = db.tbLogs.Count(l =>
                (l.FK_Link_User_Plan_ID != null && planLinkIds.Contains(l.FK_Link_User_Plan_ID.Value))
                || (l.FK_NameUser_ID != null && subNames.Contains(l.FK_NameUser_ID)));

            var orderCount = db.tbOrders.Count(o =>
                (o.FK_Tel_UserID != null && telUserIds.Contains(o.FK_Tel_UserID.Value))
                || (o.FK_Link_Plan_ID != null && planLinkIds.Contains(o.FK_Link_Plan_ID.Value)));

            var depositLogCount = db.tbDepositWallet_Log.Count(d =>
                d.FK_TelegramUser_ID != null && telUserIds.Contains(d.FK_TelegramUser_ID.Value));

            var preview = new AgentDeletePreviewViewModel
            {
                Username = username,
                FactorCount = db.tbUserFactors.Count(f => f.FK_User_ID == agentId),
                LogCount = logCount,
                TelegramUserCount = telUserIds.Count,
                OrderCount = orderCount,
                DepositLogCount = depositLogCount,
                PlanCount = db.tbPlans.Count(p => p.FK_User_ID == agentId),
                ChildAgentCount = db.tbUsers.Count(u => u.Parent_ID == agentId && u.Role != 1),
                NotificationCount = db.tbNotifications.Count(n => n.tbNoti_FK_User_ID == agentId),
                LinkCount = db.tbLinks.Count(l => l.FK_User_ID == agentId || l.tbTelegramUsers.FK_User_ID == agentId)
            };

            preview.Message = BuildConfirmMessage(preview);
            return preview;
        }

        public static async Task DeleteAgentAsync(Entities db, tbUsers agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            if (agent.Role == 1)
                throw new InvalidOperationException("حذف حساب ادمین مجاز نیست.");

            var agentId = agent.User_ID;
            var username = agent.Username ?? string.Empty;

            BotManager.StopBot(username);
            HttpRuntime.Cache.Remove(username);

            var childAgents = await db.tbUsers
                .Where(u => u.Parent_ID == agentId && u.Role != 1)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var child in childAgents)
                await DeleteAgentAsync(db, child).ConfigureAwait(false);

            agent = await db.tbUsers.FirstOrDefaultAsync(u => u.User_ID == agentId).ConfigureAwait(false);
            if (agent == null)
                return;

            var telUserIds = await db.tbTelegramUsers
                .Where(t => t.FK_User_ID == agentId)
                .Select(t => t.Tel_UserID)
                .ToListAsync()
                .ConfigureAwait(false);

            var planLinkIds = await db.tbLinkUserAndPlans
                .Where(p => p.L_FK_U_ID == agentId)
                .Select(p => p.Link_PU_ID)
                .ToListAsync()
                .ConfigureAwait(false);

            var subNames = GetAgentSubscriptionNames(db, agentId, username);

            var logs = await db.tbLogs
                .Where(l =>
                    (l.FK_Link_User_Plan_ID != null && planLinkIds.Contains(l.FK_Link_User_Plan_ID.Value))
                    || (l.FK_NameUser_ID != null && subNames.Contains(l.FK_NameUser_ID)))
                .ToListAsync()
                .ConfigureAwait(false);
            db.tbLogs.RemoveRange(logs);

            var orderIdsFromTel = await db.tbOrders
                .Where(o => o.FK_Tel_UserID != null && telUserIds.Contains(o.FK_Tel_UserID.Value))
                .Select(o => o.Order_ID)
                .ToListAsync()
                .ConfigureAwait(false);

            if (orderIdsFromTel.Count > 0)
            {
                var depositsOnOrders = await db.tbDepositWallet_Log
                    .Where(d => d.FK_Order_ID != null && orderIdsFromTel.Contains(d.FK_Order_ID.Value))
                    .ToListAsync()
                    .ConfigureAwait(false);
                db.tbDepositWallet_Log.RemoveRange(depositsOnOrders);

                var orders = await db.tbOrders
                    .Where(o => orderIdsFromTel.Contains(o.Order_ID))
                    .ToListAsync()
                    .ConfigureAwait(false);
                db.tbOrders.RemoveRange(orders);
            }

            var factors = await db.tbUserFactors.Where(f => f.FK_User_ID == agentId).ToListAsync().ConfigureAwait(false);
            db.tbUserFactors.RemoveRange(factors);

            var paymentMethods = await db.tbPaymentMethodUser.Where(p => p.FK_User_ID == agentId).ToListAsync().ConfigureAwait(false);
            db.tbPaymentMethodUser.RemoveRange(paymentMethods);

            var cards = await db.tbBankCardNumbers.Where(c => c.FK_User_ID == agentId).ToListAsync().ConfigureAwait(false);
            db.tbBankCardNumbers.RemoveRange(cards);

            var connectionHelp = await db.tbConnectionHelp.Where(c => c.FK_User_ID == agentId).ToListAsync().ConfigureAwait(false);
            db.tbConnectionHelp.RemoveRange(connectionHelp);

            var supportLinks = await db.tbSupportLinks.Where(s => s.FK_User_ID == agentId).ToListAsync().ConfigureAwait(false);
            db.tbSupportLinks.RemoveRange(supportLinks);

            var notiUsers = await db.tbNotificationUser.Where(n => n.tbNotiUser_FK_User_ID == agentId).ToListAsync().ConfigureAwait(false);
            db.tbNotificationUser.RemoveRange(notiUsers);

            var notifications = await db.tbNotifications.Where(n => n.tbNoti_FK_User_ID == agentId).ToListAsync().ConfigureAwait(false);
            db.tbNotifications.RemoveRange(notifications);

            var changelogSeen = await db.tbPanelChangelogSeen.Where(s => s.FK_User_ID == agentId).ToListAsync().ConfigureAwait(false);
            db.tbPanelChangelogSeen.RemoveRange(changelogSeen);

            var agentLinks = await db.tbLinks
                .Where(l => l.FK_User_ID == agentId)
                .ToListAsync()
                .ConfigureAwait(false);
            db.tbLinks.RemoveRange(agentLinks);

            if (telUserIds.Count > 0)
            {
                var usersWithTel = await db.tbUsers
                    .Where(u => u.Admin_Telegram_ID != null && telUserIds.Contains(u.Admin_Telegram_ID.Value))
                    .ToListAsync()
                    .ConfigureAwait(false);
                foreach (var u in usersWithTel)
                    u.Admin_Telegram_ID = null;

                var telUsers = await db.tbTelegramUsers
                    .Where(t => t.FK_User_ID == agentId)
                    .ToListAsync()
                    .ConfigureAwait(false);

                var telIdSet = new HashSet<int>(telUserIds);
                foreach (var telUser in telUsers)
                {
                    if (telUser.Tel_Parent_ID != null && telIdSet.Contains(telUser.Tel_Parent_ID.Value))
                        telUser.Tel_Parent_ID = null;
                }

                var inviteChildren = await db.tbTelegramUsers
                    .Where(t => t.Tel_Parent_ID != null && telIdSet.Contains(t.Tel_Parent_ID.Value))
                    .ToListAsync()
                    .ConfigureAwait(false);
                foreach (var child in inviteChildren)
                    child.Tel_Parent_ID = null;

                db.tbTelegramUsers.RemoveRange(telUsers);
            }

            agent.Admin_Telegram_ID = null;
            db.tbUsers.Remove(agent);

            await db.SaveChangesAsync().ConfigureAwait(false);
        }

        private static List<string> GetAgentSubscriptionNames(Entities db, int agentId, string username)
        {
            var suffix = "@" + username;
            return db.tbLinks
                .Where(l => (l.FK_User_ID == agentId || l.tbTelegramUsers.FK_User_ID == agentId) && l.tbL_Email != null)
                .Select(l => l.tbL_Email)
                .ToList()
                .Where(e => e.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Split('@')[0])
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildConfirmMessage(AgentDeletePreviewViewModel preview)
        {
            var msg = new StringBuilder();
            msg.AppendLine("با حذف نماینده «" + preview.Username + "» موارد زیر از پنل پاک می‌شوند:");
            msg.AppendLine("");
            msg.AppendLine("• تمامی فاکتورها و تراکنش‌های مالی (" + preview.FactorCount + " فاکتور، " + preview.DepositLogCount + " تراکنش کیف پول)");
            msg.AppendLine("• تمامی لاگ‌های اشتراک و فروش (" + preview.LogCount + " مورد)");
            msg.AppendLine("• کاربران تلگرامی زیرمجموعه (" + preview.TelegramUserCount + " کاربر)");
            msg.AppendLine("• لینک‌های پنل به اشتراک (" + preview.LinkCount + " مورد)");
            msg.AppendLine("• سفارش‌ها و رزرو بسته‌ها (" + preview.OrderCount + " مورد)");
            msg.AppendLine("• تعرفه‌ها، تنظیمات ربات، اعلان‌ها و سایر داده‌های وابسته");
            if (preview.ChildAgentCount > 0)
                msg.AppendLine("• نمایندگان زیرمجموعه (" + preview.ChildAgentCount + " نماینده) به همراه وابستگی‌های آن‌ها");
            msg.AppendLine("");
            msg.AppendLine("⚠️ اشتراک‌ها و کانفیگ‌های روی سرور (Subscriptions) حذف نمی‌شوند و فقط ارتباط آن‌ها با پنل قطع می‌گردد.");
            msg.AppendLine("");
            msg.AppendLine("این عملیات غیرقابل بازگشت است.");
            return msg.ToString().TrimEnd();
        }
    }
}
