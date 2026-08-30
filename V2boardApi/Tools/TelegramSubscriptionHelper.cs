using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V2boardApi.Tools
{
    public static class TelegramSubscriptionHelper
    {
        public static async Task CancelReservedOrdersForEmailAsync(
            string email,
            Repository<tbOrders> ordersRepository,
            Repository<tbLogs> logsRepository,
            Repository<tbUsers> usersRepository,
            Repository<tbPlans> plansRepository,
            Repository<tbLinkUserAndPlans> linkUserAndPlansRepository,
            Repository<tbLinkServerGroupWithUsers> linkUserGroupRepository)
        {
            var orders = ordersRepository.table
                .Include(o => o.tbLinkUserAndPlans)
                .Include(o => o.tbLinkUserAndPlans.tbPlans)
                .Include(o => o.tbTelegramUsers)
                .Include(o => o.tbDepositWallet_Log)
                .Where(o => o.AccountName == email && o.OrderStatus == "FOR_RESERVE")
                .ToList();

            foreach (var order in orders)
            {
                await ReservedPackageHelper.CancelReservedOrderAsync(
                    order, ordersRepository, logsRepository, usersRepository,
                    plansRepository, linkUserAndPlansRepository, linkUserGroupRepository);
            }
        }

        public static async Task<bool> DeleteSubscriptionByLinkAsync(
            tbLinks link,
            tbUsers agent,
            Repository<tbLinks> linksRepository,
            Repository<tbOrders> ordersRepository,
            Repository<tbLogs> logsRepository,
            Repository<tbUsers> usersRepository,
            Repository<tbPlans> plansRepository,
            Repository<tbLinkUserAndPlans> linkUserAndPlansRepository,
            Repository<tbLinkServerGroupWithUsers> linkUserGroupRepository)
        {
            if (link == null || agent?.tbServers == null || string.IsNullOrEmpty(link.tbL_Email))
                return false;

            await CancelReservedOrdersForEmailAsync(
                link.tbL_Email, ordersRepository, logsRepository, usersRepository,
                plansRepository, linkUserAndPlansRepository, linkUserGroupRepository);

            using (var mySql = new MySqlEntities(agent.tbServers.ConnectionString))
            {
                await mySql.OpenAsync();

                var disc = new Dictionary<string, object> { { "@email", link.tbL_Email } };
                var reader = await mySql.GetDataAsync(
                    "SELECT id, email, u, d, transfer_enable, expired_at, created_at FROM v2_user WHERE email=@email", disc);

                if (!await reader.ReadAsync())
                {
                    reader.Close();
                    linksRepository.Delete(link);
                    linksRepository.Save();
                    await mySql.CloseAsync();
                    return true;
                }

                var v2UserId = reader.GetInt32("id");
                var name = reader.GetString("email").Split('@')[0];
                var username = reader.GetString("email").Split('@')[1];
                var download = reader.GetInt64("d");
                var upload = reader.GetInt64("u");
                var transferEnable = reader.GetInt64("transfer_enable");
                var expiredAtRaw = reader["expired_at"];
                long? createdAtUnix = null;
                var createdAtRaw = reader["created_at"]?.ToString();
                if (!string.IsNullOrWhiteSpace(createdAtRaw))
                    createdAtUnix = Convert.ToInt64(createdAtRaw);

                if (!SubscriptionPackageHelper.CanAgentDeleteSubscription(
                    agent.Role ?? 0, transferEnable, download, upload, expiredAtRaw, createdAtUnix))
                {
                    reader.Close();
                    await mySql.CloseAsync();
                    return false;
                }

                reader.Close();

                try
                {
                    await ReservedPackageHelper.ProcessSubscriptionDeleteLogsAsync(
                        name, username, createdAtUnix, download, upload,
                        logsRepository, usersRepository, linkUserGroupRepository);
                }
                catch (InvalidOperationException)
                {
                    await mySql.CloseAsync();
                    return false;
                }

                using (var deleteReader = await mySql.GetDataAsync(
                    "DELETE FROM v2_user WHERE id=@id",
                    new Dictionary<string, object> { { "@id", v2UserId } }))
                {
                    while (await deleteReader.ReadAsync()) { }
                }

                await mySql.CloseAsync();
            }

            linksRepository.Delete(link);
            linksRepository.Save();
            await logsRepository.SaveChangesAsync();
            await usersRepository.SaveChangesAsync();
            return true;
        }

        public static string GetSubscriptionDisplayName(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "بدون نام";

            var name = email.Contains("@") ? email.Split('@')[0] : email;
            if (name.Contains("$"))
                name = name.Split('$')[0];

            return string.IsNullOrWhiteSpace(name) ? "بدون نام" : name;
        }

        public static async Task<List<TelegramSubscriptionSummary>> LoadUserSubscriptionSummariesAsync(
            IList<tbLinks> links,
            string fallbackConnectionString)
        {
            var summaries = new List<TelegramSubscriptionSummary>();
            if (links == null || links.Count == 0)
                return summaries;

            foreach (var group in links.GroupBy(l => l.FK_Server_ID))
            {
                var groupLinks = group.Where(l => !string.IsNullOrWhiteSpace(l.tbL_Email)).ToList();
                if (groupLinks.Count == 0)
                    continue;

                var connectionString = groupLinks
                    .Select(l => l.tbServers != null ? l.tbServers.ConnectionString : null)
                    .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                if (string.IsNullOrWhiteSpace(connectionString))
                    connectionString = fallbackConnectionString;

                var byEmail = new Dictionary<string, TelegramSubscriptionSummary>(StringComparer.OrdinalIgnoreCase);
                foreach (var link in groupLinks)
                {
                    var summary = new TelegramSubscriptionSummary
                    {
                        Email = link.tbL_Email,
                        DisplayName = GetSubscriptionDisplayName(link.tbL_Email),
                        Found = false
                    };
                    ApplyStatus(summary);
                    summaries.Add(summary);
                    byEmail[link.tbL_Email] = summary;
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                    continue;

                var parameters = new Dictionary<string, object>();
                var paramNames = new List<string>();
                for (var i = 0; i < groupLinks.Count; i++)
                {
                    var key = "@e" + i;
                    paramNames.Add(key);
                    parameters[key] = groupLinks[i].tbL_Email;
                }

                var mySql = new MySqlEntities(connectionString);
                await mySql.OpenAsync();
                var query = "SELECT email,u,d,transfer_enable,expired_at,banned FROM v2_user WHERE email IN (" + string.Join(",", paramNames) + ")";
                var reader = await mySql.GetDataAsync(query, parameters);
                while (await reader.ReadAsync())
                {
                    var email = reader["email"] != null ? reader["email"].ToString() : null;
                    TelegramSubscriptionSummary summary;
                    if (string.IsNullOrWhiteSpace(email) || !byEmail.TryGetValue(email, out summary))
                        continue;

                    var upload = reader.GetInt64("u");
                    var download = reader.GetInt64("d");
                    var transferEnable = reader.GetInt64("transfer_enable");
                    var remainingBytes = transferEnable - (download + upload);
                    var banned = reader.GetBoolean("banned");

                    DateTime? expireAt = null;
                    var expireRaw = reader["expired_at"];
                    if (expireRaw != null && expireRaw != DBNull.Value && !string.IsNullOrWhiteSpace(expireRaw.ToString()))
                        expireAt = Utility.ConvertSecondToDatetime(Convert.ToInt64(expireRaw));

                    summary.Found = true;
                    summary.IsBanned = banned;
                    summary.TotalGb = Math.Round(Utility.ConvertByteToGB(transferEnable), 2);
                    summary.UsedGb = Math.Round(Utility.ConvertByteToGB(download + upload), 2);
                    summary.RemainingGb = Math.Round(Utility.ConvertByteToGB(remainingBytes), 2);
                    summary.VolumeEnded = remainingBytes <= 0;
                    summary.ExpireAt = expireAt;
                    summary.DateEnded = expireAt.HasValue && expireAt.Value <= DateTime.Now;
                    summary.DaysLeft = expireAt.HasValue ? Utility.CalculateLeftDayes(expireAt.Value) : 0;
                    summary.HasUnlimitedTime = !expireAt.HasValue;
                    summary.WouldReserveOnRenew = !SubscriptionPackageHelper.IsPackageEnded(
                        transferEnable, download, upload, expireRaw);
                    ApplyStatus(summary);
                }
                reader.Close();
                await mySql.CloseAsync();
            }

            return summaries;
        }

        public static string BuildSubscriptionListMessage(
            IList<TelegramSubscriptionSummary> summaries,
            string botId,
            TelegramSubscriptionListMode mode)
        {
            var str = new StringBuilder();
            str.AppendLine("");
            if (mode == TelegramSubscriptionListMode.Renew)
            {
                str.AppendLine("♨️ عزیزم اشتراکتو انتخاب کن تا بریم برای تمدیدش");
                str.AppendLine("");
                str.AppendLine("قبلش یه نگاه به وضعیت هرکدوم بنداز تا بدونی حجم کل، مصرف و زمانش چقدره 👇");
            }
            else
            {
                str.AppendLine("♻️ باشه، از همون اشتراک‌هایی که از قبل داری استفاده می‌کنیم.");
                str.AppendLine("");
                str.AppendLine("یکی رو انتخاب کن تا بسته‌ی جدید روی همون اعمال بشه. وضعیت حجم و زمان هر کدوم اینه 👇");
            }
            str.AppendLine("");

            var index = 1;
            foreach (var item in summaries ?? Enumerable.Empty<TelegramSubscriptionSummary>())
            {
                str.AppendLine(index + " - <b>" + item.DisplayName + "</b>");
                if (!item.Found)
                {
                    str.AppendLine("⚠️ اطلاعات این اشتراک پیدا نشد");
                }
                else
                {
                    str.AppendLine("📡 حجم کل : " + FormatGb(item.TotalGb));
                    str.AppendLine("📉 حجم مصرف‌شده : " + FormatGb(item.UsedGb));
                    str.AppendLine("📶 حجم باقی‌مانده : " + FormatGb(item.RemainingGb < 0 ? 0 : item.RemainingGb));
                    str.AppendLine("⏳ زمان باقی‌مانده : " + FormatRemainingTime(item));
                    str.AppendLine("وضعیت : <b>" + item.StatusText + "</b>");
                }
                str.AppendLine("");
                index++;
            }

            str.AppendLine("〰️〰️〰️〰️〰️");
            str.AppendLine("🆔 @" + botId);
            return str.ToString();
        }

        public static string BuildExistingSubscriptionWarning(string botId)
        {
            var str = new StringBuilder();
            str.AppendLine("");
            str.AppendLine("<b>⚠️ عزیزم یه لحظه وایسا!</b>");
            str.AppendLine("");
            str.AppendLine("تو از قبل اشتراک داری. اگه الان بری سراغ خرید، یه اشتراک کاملاً جدید با لینک جدا برات ساخته می‌شه.");
            str.AppendLine("");
            str.AppendLine("مطمئنی اشتراک جدید می‌خوای، یا می‌خوای همون قبلی‌هاتو شارژ کنی؟");
            str.AppendLine("");
            str.AppendLine("〰️〰️〰️〰️〰️");
            str.AppendLine("🆔 @" + botId);
            return str.ToString();
        }

        public static string BuildRenewReserveWarning(TelegramSubscriptionSummary summary, string botId)
        {
            var str = new StringBuilder();
            str.AppendLine("");
            str.AppendLine("<b>⚠️ عزیزم این اشتراک هنوز تموم نشده!</b>");
            str.AppendLine("");
            str.AppendLine("📶 حجم باقی‌مانده : " + FormatGb(summary.RemainingGb < 0 ? 0 : summary.RemainingGb));
            str.AppendLine("⏳ زمان باقی‌مانده : " + FormatRemainingTime(summary));
            str.AppendLine("");
            str.AppendLine("اگه الان تمدیدش کنی، بسته جدید رزرو می‌شه و بعد از تموم شدن همین بسته خودش فعال می‌شه.");
            str.AppendLine("");
            str.AppendLine("مطمئنی می‌خوای ادامه بدی؟");
            str.AppendLine("");
            str.AppendLine("〰️〰️〰️〰️〰️");
            str.AppendLine("🆔 @" + botId);
            return str.ToString();
        }

        private static void ApplyStatus(TelegramSubscriptionSummary summary)
        {
            if (!summary.Found)
            {
                summary.IsActive = false;
                summary.StatusText = "⚠️ اطلاعات پیدا نشد";
                return;
            }

            if (summary.IsBanned)
            {
                summary.IsActive = false;
                summary.StatusText = "🚫 مسدود";
                return;
            }

            if (summary.VolumeEnded && summary.DateEnded)
            {
                summary.IsActive = false;
                summary.StatusText = "❌ تموم شده (حجم و زمان)";
                return;
            }

            if (summary.VolumeEnded)
            {
                summary.IsActive = false;
                summary.StatusText = "❌ تموم شده (حجم)";
                return;
            }

            if (summary.DateEnded)
            {
                summary.IsActive = false;
                summary.StatusText = "❌ تموم شده (زمان)";
                return;
            }

            summary.IsActive = true;
            summary.StatusText = "✅ فعال (حجم و زمان داره)";
        }

        private static string FormatGb(double gb)
        {
            return gb.ToString("0.##") + " گیگ";
        }

        private static string FormatRemainingTime(TelegramSubscriptionSummary summary)
        {
            if (summary.HasUnlimitedTime)
                return "نامحدود";
            if (summary.DateEnded || summary.DaysLeft <= 0)
                return "تمام شده";
            return summary.DaysLeft + " روز";
        }
    }

    public enum TelegramSubscriptionListMode
    {
        Renew,
        ExistingBuy
    }

    public class TelegramSubscriptionSummary
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public bool Found { get; set; }
        public bool IsBanned { get; set; }
        public bool VolumeEnded { get; set; }
        public bool DateEnded { get; set; }
        public bool HasUnlimitedTime { get; set; }
        public bool WouldReserveOnRenew { get; set; }
        public bool IsActive { get; set; }
        public double TotalGb { get; set; }
        public double UsedGb { get; set; }
        public double RemainingGb { get; set; }
        public int DaysLeft { get; set; }
        public DateTime? ExpireAt { get; set; }
        public string StatusText { get; set; }
    }
}
