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
                    "SELECT id, email, u, d, expired_at FROM v2_user WHERE email=@email", disc);

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
                var totalUse = Utility.ConvertByteToGB(reader.GetInt64("u") + reader.GetInt64("d"));

                var expireRaw = reader["expired_at"]?.ToString();
                var expireTime = default(DateTime);
                if (!string.IsNullOrEmpty(expireRaw))
                    expireTime = Utility.ConvertSecondToDatetime(Convert.ToDouble(expireRaw));

                reader.Close();

                var log = await logsRepository.FirstOrDefaultAsync(s =>
                    s.FK_NameUser_ID == name && s.tbLinkUserAndPlans.tbUsers.Username == username);

                if (log != null)
                {
                    if (totalUse <= 0.5 && (expireTime != default(DateTime) && expireTime >= DateTime.Now))
                    {
                        var userAccount = await usersRepository.FirstOrDefaultAsync(s => s.Username == username);
                        if (userAccount != null)
                            await ApplyWalletRefundForDeletedLogAsync(
                                userAccount, log, usersRepository, linkUserGroupRepository);
                    }

                    var logs = await logsRepository.WhereAsync(s =>
                        s.FK_NameUser_ID == name && s.tbLinkUserAndPlans.tbUsers.Username == username);

                    if (totalUse <= 0.5 && (expireTime != default(DateTime) && expireTime >= DateTime.Now))
                        await logsRepository.DeleteRangeAsync(logs);
                    else
                    {
                        foreach (var item in logs)
                            item.FK_NameUser_ID = "del_" + name;
                    }
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

        private static async Task ApplyWalletRefundForDeletedLogAsync(
            tbUsers userAccount,
            tbLogs log,
            Repository<tbUsers> usersRepository,
            Repository<tbLinkServerGroupWithUsers> linkUserGroupRepository)
        {
            if (userAccount.Role == 2)
            {
                userAccount.Wallet -= (int)log.SalePrice;

                if (userAccount.tbUsers2 != null && userAccount.tbUsers2.Role == 3)
                {
                    var groupId = log.tbLinkUserAndPlans.tbPlans.Group_Id;
                    var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(
                        s => s.FK_Group_Id == groupId && s.FK_User_Id == userAccount.tbUsers2.User_ID);
                    if (linkGroupUser != null)
                    {
                        userAccount.tbUsers2.Wallet -= (log.PlanVolume * linkGroupUser.PriceForGig)
                            + (log.PlanMonth * linkGroupUser.PriceForMonth);
                    }
                }
            }
            else if (userAccount.Role == 3 && userAccount.tbUsers2 != null)
            {
                var groupId = log.tbLinkUserAndPlans.tbPlans.Group_Id;
                var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(
                    s => s.FK_Group_Id == groupId && s.FK_User_Id == userAccount.User_ID);

                if (log.PlanVolume != null && linkGroupUser != null)
                {
                    var amount = (log.PlanVolume * linkGroupUser.PriceForGig)
                        + (log.PlanMonth * linkGroupUser.PriceForMonth)
                        + ((double)log.tbLinkUserAndPlans.tbPlans.device_limit * linkGroupUser.PriceForUser);
                    userAccount.Wallet -= amount;
                }
            }
        }
    }
}
