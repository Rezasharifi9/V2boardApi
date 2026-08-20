using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V2boardApi.Tools
{
    /// <summary>
    /// اطلاع‌رسانی به نماینده هنگام مصرف ۸۰٪ سقف اعتبار اشتراک (بدهی / Limit).
    /// </summary>
    public static class AgentLimitNotificationService
    {
        public const double ThresholdRatio = 0.8;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly ConcurrentDictionary<int, bool> NotifiedAtOrAboveThreshold = new ConcurrentDictionary<int, bool>();

        public static bool IsAgentEligible(tbUsers agent)
        {
            return agent != null
                && agent.Status == true
                && (agent.Role == 2 || agent.Role == 3 || agent.Role == 4);
        }

        public static bool IsAtOrAboveThreshold(double wallet, int? limit)
        {
            if (!limit.HasValue || limit.Value <= 0)
                return false;

            return wallet >= limit.Value * ThresholdRatio;
        }

        public static bool CrossedThreshold(double previousWallet, double currentWallet, int? limit)
        {
            return !IsAtOrAboveThreshold(previousWallet, limit)
                && IsAtOrAboveThreshold(currentWallet, limit);
        }

        public static void ScheduleCheckAfterWalletChange(int userId, double previousWallet)
        {
            Task.Run(async () =>
            {
                try
                {
                    await CheckAndNotifyAfterWalletChangeAsync(userId, previousWallet);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "خطا در بررسی هشدار سقف اعتبار نماینده " + userId);
                }
            });
        }

        public static async Task CheckAndNotifyAfterWalletChangeAsync(int userId, double previousWallet)
        {
            using (var db = new Entities())
            {
                var agent = await db.tbUsers
                    .Include(u => u.tbBotSettings)
                    .FirstOrDefaultAsync(u => u.User_ID == userId);

                if (!IsAgentEligible(agent))
                    return;

                var limit = agent.Limit;
                var currentWallet = agent.Wallet;

                if (!IsAtOrAboveThreshold(currentWallet, limit))
                {
                    bool ignored;
                    NotifiedAtOrAboveThreshold.TryRemove(userId, out ignored);
                    return;
                }

                if (CrossedThreshold(previousWallet, currentWallet, limit)
                    || !NotifiedAtOrAboveThreshold.ContainsKey(userId))
                {
                    if (await SendLimitWarningAsync(agent))
                        NotifiedAtOrAboveThreshold[userId] = true;
                }
            }
        }

        public static async Task ProcessAllAgentsAsync()
        {
            try
            {
                using (var db = new Entities())
                {
                    var agents = await db.tbUsers
                        .Include(u => u.tbBotSettings)
                        .Where(u => u.Status == true && (u.Role == 2 || u.Role == 3 || u.Role == 4))
                        .ToListAsync();

                    foreach (var agent in agents)
                    {
                        if (!IsAtOrAboveThreshold(agent.Wallet, agent.Limit))
                            continue;

                        if (NotifiedAtOrAboveThreshold.ContainsKey(agent.User_ID))
                            continue;

                        if (await SendLimitWarningAsync(agent))
                            NotifiedAtOrAboveThreshold[agent.User_ID] = true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در پردازش هشدار سقف اعتبار نمایندگان");
            }
        }

        private static async Task<bool> SendLimitWarningAsync(tbUsers agent)
        {
            var limit = agent.Limit ?? 0;
            if (limit <= 0)
                return false;

            var usedPercent = Math.Min(100, (int)Math.Round(agent.Wallet / limit * 100));
            var remaining = Math.Max(0, limit - (int)agent.Wallet);

            var msg = new StringBuilder();
            msg.AppendLine("⚠️ نماینده گرامی");
            msg.AppendLine("");
            msg.AppendLine("بیش از ۸۰٪ از سقف اعتبار اشتراک شما مصرف شده است.");
            msg.AppendLine("📊 بدهی فعلی: " + ((int)agent.Wallet).ConvertToMony() + " تومان");
            msg.AppendLine("📈 سقف اعتبار: " + limit.ConvertToMony() + " تومان");
            msg.AppendLine("💳 اعتبار باقی‌مانده: " + remaining.ConvertToMony() + " تومان");
            msg.AppendLine("(" + usedPercent + "% مصرف شده)");
            msg.AppendLine("");
            msg.AppendLine("برای جلوگیری از توقف امکان ایجاد اشتراک، لطفاً در اسرع وقت نسبت به پرداخت بدهی خود اقدام کنید.");

            // همین هشدار به‌صورت خودکار در بخش اعلانات پنل نماینده هم ثبت می‌شود.
            // dedup روی ۲۴ ساعت است تا پس از ری‌استارت برنامه (که کش NotifiedAtOrAboveThreshold
            // خالی می‌شود) اعلان تکراری برای نماینده ساخته نشود.
            return await SettlementService.SendAgentTelegramMessage(
                agent,
                msg.ToString(),
                panelTitle: PanelNotificationService.TitleLimitWarning);
        }
    }
}
