using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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
    }
}
