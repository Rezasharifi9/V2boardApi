using DataLayer.DomainModel;
using DataLayer.Repository;
using System.Linq;
using System.Threading.Tasks;

namespace V2boardApi.Tools
{
    public static class ReservedPackageHelper
    {
        public const string ReserveLogAction = "رزرو بسته";

        public static void RemoveReservePackageLogs(Repository<tbLogs> logsRepository, tbOrders order)
        {
            if (order == null || string.IsNullOrEmpty(order.AccountName) || !order.AccountName.Contains("@"))
                return;

            var subName = order.AccountName.Split('@')[0];
            var linkPlanId = order.FK_Link_Plan_ID ?? order.tbLinkUserAndPlans?.Link_PU_ID;
            if (!linkPlanId.HasValue)
                return;

            var logs = logsRepository
                .Where(l => l.FK_NameUser_ID == subName
                    && l.FK_Link_User_Plan_ID == linkPlanId.Value
                    && l.Action == ReserveLogAction)
                .ToList();

            foreach (var log in logs)
                logsRepository.Delete(log);

            if (logs.Count > 0)
                logsRepository.Save();
        }

        public static async Task<bool> RefundReservedOrderWalletAsync(
            tbOrders order,
            Repository<tbUsers> usersRepository,
            Repository<tbPlans> plansRepository,
            Repository<tbLinkUserAndPlans> linkUserAndPlansRepository,
            Repository<tbLinkServerGroupWithUsers> linkUserGroupRepository)
        {
            var username = order.AccountName.Split('@')[1];
            var userAccount = await usersRepository.FirstOrDefaultAsync(s => s.Username == username);
            if (userAccount == null)
                return false;

            var plan = order.tbLinkUserAndPlans?.tbPlans;
            if (plan == null && order.V2_Plan_ID.HasValue)
                plan = await plansRepository.FirstOrDefaultAsync(p => p.Plan_ID_V2 == order.V2_Plan_ID);
            if (plan == null)
                return false;

            var linkPlan = order.tbLinkUserAndPlans;
            if (linkPlan == null && order.FK_Link_Plan_ID.HasValue)
                linkPlan = await linkUserAndPlansRepository.FirstOrDefaultAsync(p => p.Link_PU_ID == order.FK_Link_Plan_ID);

            if (userAccount.Role == 3)
            {
                var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(
                    s => s.FK_Group_Id == plan.Group_Id && s.FK_User_Id == userAccount.User_ID);
                if (linkGroupUser == null)
                    return false;

                userAccount.Wallet -= (int)((plan.PlanVolume * linkGroupUser.PriceForGig)
                    + (plan.PlanMonth * linkGroupUser.PriceForMonth)
                    + ((plan.device_limit ?? 0) * linkGroupUser.PriceForUser));
            }

            if (userAccount.Role == 2)
            {
                if (userAccount.tbUsers2 != null && userAccount.tbUsers2.Role != 1 && userAccount.tbUsers2.Role == 3)
                {
                    var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(
                        s => s.FK_Group_Id == plan.Group_Id && s.FK_User_Id == userAccount.tbUsers2.User_ID);
                    if (linkGroupUser != null)
                    {
                        userAccount.tbUsers2.Wallet -= (plan.PlanVolume * linkGroupUser.PriceForGig)
                            + (plan.PlanMonth * linkGroupUser.PriceForMonth)
                            + ((double)(plan.device_limit ?? 0) * linkGroupUser.PriceForUser);
                    }
                }

                if (linkPlan?.tbPlans != null)
                    userAccount.Wallet -= (int)linkPlan.tbPlans.Price;
            }

            usersRepository.Save();
            return true;
        }

        public static async Task CancelReservedOrderAsync(
            tbOrders order,
            Repository<tbOrders> ordersRepository,
            Repository<tbLogs> logsRepository,
            Repository<tbUsers> usersRepository,
            Repository<tbPlans> plansRepository,
            Repository<tbLinkUserAndPlans> linkUserAndPlansRepository,
            Repository<tbLinkServerGroupWithUsers> linkUserGroupRepository)
        {
            if (!await RefundReservedOrderWalletAsync(order, usersRepository, plansRepository, linkUserAndPlansRepository, linkUserGroupRepository))
                return;

            RemoveReservePackageLogs(logsRepository, order);
            ordersRepository.Delete(order);
            ordersRepository.Save();
        }
    }
}
