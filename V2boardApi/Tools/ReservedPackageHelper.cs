using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace V2boardApi.Tools
{
    public static class ReservedPackageHelper
    {
        public const string ReserveLogAction = "رزرو بسته";
        public const string ActivateReserveLogAction = "فعال‌سازی بسته رزرو";
        public const string CreatedLogAction = "کاربر اضافه کرد";
        public const string EditedLogAction = "کاربر تمدید شد";

        public static async Task<int?> ApplyDeletedSubscriptionWalletRefundAsync(
            tbUsers userAccount,
            tbLogs log,
            Repository<tbUsers> usersRepository,
            Repository<tbLinkServerGroupWithUsers> linkUserGroupRepository)
        {
            if (userAccount == null || log == null || !IsAgentRole(userAccount.Role.Value))
                return null;

            var refundAmount = 0;

            if (userAccount.Role == 2)
            {
                refundAmount = (int)log.SalePrice;

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

                userAccount.Wallet -= refundAmount;
            }
            else if (userAccount.Role == 3 && userAccount.tbUsers2 != null)
            {
                var groupId = log.tbLinkUserAndPlans.tbPlans.Group_Id;
                var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(
                    s => s.FK_Group_Id == groupId && s.FK_User_Id == userAccount.User_ID);

                if (log.PlanVolume != null && linkGroupUser != null)
                {
                    refundAmount = (int)((log.PlanVolume * linkGroupUser.PriceForGig)
                        + (log.PlanMonth * linkGroupUser.PriceForMonth)
                        + ((double)log.tbLinkUserAndPlans.tbPlans.device_limit * linkGroupUser.PriceForUser));
                    userAccount.Wallet -= refundAmount;
                }
                else if (linkGroupUser != null)
                {
                    refundAmount = (int)((log.tbLinkUserAndPlans.tbPlans.PlanVolume * linkGroupUser.PriceForGig)
                        + (log.tbLinkUserAndPlans.tbPlans.PlanMonth * linkGroupUser.PriceForMonth)
                        + ((int)log.tbLinkUserAndPlans.tbPlans.device_limit * linkGroupUser.PriceForUser));
                    userAccount.Wallet -= refundAmount;
                }
            }

            if (refundAmount <= 0)
                return null;

            usersRepository.Save();
            return refundAmount;
        }

        public static async Task<int?> ProcessSubscriptionDeleteLogsAsync(
            string name,
            string username,
            long? createdAtUnix,
            long download,
            long upload,
            Repository<tbLogs> logsRepository,
            Repository<tbUsers> usersRepository,
            Repository<tbLinkServerGroupWithUsers> linkUserGroupRepository)
        {
            var logs = (await logsRepository.WhereAsync(s =>
                    s.FK_NameUser_ID == name && s.tbLinkUserAndPlans.tbUsers.Username == username))
                .ToList();

            if (logs.Count == 0)
                return null;

            int? refundAmount = null;
            if (SubscriptionPackageHelper.IsEligibleForEarlyDeleteWithRefund(createdAtUnix, download, upload))
            {
                var creationLog = logs
                    .Where(l => l.Action == CreatedLogAction
                        && !SubscriptionLogHelper.IsDeletedSubscriptionLogName(l.FK_NameUser_ID))
                    .OrderByDescending(l => l.CreateDatetime)
                    .FirstOrDefault();

                if (creationLog != null)
                {
                    var userAccount = await usersRepository.FirstOrDefaultAsync(s => s.Username == username);
                    if (userAccount == null)
                        throw new InvalidOperationException("عدم صحت نام کاربری لطفا با مدیر تماس بگیرید !!");

                    if (userAccount.Role == 2 && userAccount.tbUsers2 == null)
                        throw new InvalidOperationException("عدم صحت نام کاربری لطفا با مدیر تماس بگیرید !!");

                    if (userAccount.Role == 3 && userAccount.tbUsers2 == null)
                        throw new InvalidOperationException("مدیر والدی برای شما تعریف نشده است لطفا با مدیر سامانه تماس بگیرید !!");

                    refundAmount = await ApplyDeletedSubscriptionWalletRefundAsync(
                        userAccount, creationLog, usersRepository, linkUserGroupRepository);
                }
            }

            SubscriptionLogHelper.MarkSubscriptionLogsDeleted(logs, name);
            return refundAmount;
        }

        public static bool IsAgentRole(int role)
        {
            return role == 2 || role == 3 || role == 4;
        }

        public static void RemoveReservePackageLogs(Repository<tbLogs> logsRepository, tbOrders order)
        {
            if (order == null || string.IsNullOrEmpty(order.AccountName) || !order.AccountName.Contains("@"))
                return;

            var subName = order.AccountName.Split('@')[0];
            var linkPlanId = order.FK_Link_Plan_ID ?? order.tbLinkUserAndPlans?.Link_PU_ID;
            if (!linkPlanId.HasValue)
                return;

            var logs = logsRepository
                .Where(l => l.FK_NameUser_ID == subName && l.FK_Link_User_Plan_ID == linkPlanId.Value)
                .ToList()
                .Where(l => ShouldRemoveLogForCancelledOrder(l, order))
                .ToList();

            foreach (var log in logs)
                logsRepository.Delete(log);

            if (logs.Count > 0)
                logsRepository.Save();
        }

        private static bool ShouldRemoveLogForCancelledOrder(tbLogs log, tbOrders order)
        {
            if (log == null || !LogMatchesOrderPackage(log, order))
                return false;

            if (log.Action == ReserveLogAction || log.Action == ActivateReserveLogAction)
                return LogCreatedNearOrder(log, order);

            if (log.Action == CreatedLogAction || log.Action == EditedLogAction)
                return LogCreatedNearOrder(log, order);

            return false;
        }

        private static bool LogMatchesOrderPackage(tbLogs log, tbOrders order)
        {
            var price = order.Order_Price ?? order.PriceWithOutDiscount;
            if (price.HasValue && log.SalePrice.HasValue && (int)log.SalePrice.Value != (int)price.Value)
                return false;

            if (Math.Abs(log.PlanVolume - order.Traffic) > 0.01)
                return false;

            if (Math.Abs(log.PlanMonth - order.Month) > 0.01)
                return false;

            return true;
        }

        private static bool LogCreatedNearOrder(tbLogs log, tbOrders order)
        {
            if (!order.OrderDate.HasValue || !log.CreateDatetime.HasValue)
                return true;

            var diffMinutes = (log.CreateDatetime.Value - order.OrderDate.Value).TotalMinutes;
            return diffMinutes >= -5 && diffMinutes <= 1440;
        }

        /// <summary>
        /// افزایش بدهی نماینده هنگام ساخت، تمدید یا رزرو اشتراک از ربات یا اپ.
        /// نقش ۳: مبلغ تخصیص گروه سرور (گیگ/ماه/کاربر). نقش ۲: قیمت تعرفه.
        /// شارژ کیف پول کاربر اینجا اعمال نمی‌شود؛ ذخیره با فراخواننده است.
        /// </summary>
        public static void ApplySubscriptionAgentDebt(tbUsers agent, tbLinkUserAndPlans planLink)
        {
            if (agent == null || planLink?.tbPlans == null)
                return;

            var plan = planLink.tbPlans;
            if (agent.Role == 3)
            {
                var prices = agent.tbLinkServerGroupWithUsers
                    .FirstOrDefault(s => s.FK_Group_Id == plan.Group_Id);
                if (prices == null)
                    return;

                var amount = (plan.PlanMonth * prices.PriceForMonth)
                           + (plan.PlanVolume * prices.PriceForGig)
                           + ((plan.device_limit ?? 0) * prices.PriceForUser);
                agent.Wallet += (int)amount;
            }
            else if (agent.Role == 2)
            {
                agent.Wallet += plan.Price;
            }
        }

        /// <summary>
        /// برگشت بدهی کیف‌پول نماینده برای سفارش رزرو.
        /// null = خطا / نقش ۲و۳ بدون امکان محاسبه؛ 0 = نیازی به برگشت نماینده نبود (مثلاً ادمین).
        /// </summary>
        public static async Task<int?> RefundReservedOrderWalletAsync(
            tbOrders order,
            Repository<tbUsers> usersRepository,
            Repository<tbPlans> plansRepository,
            Repository<tbLinkUserAndPlans> linkUserAndPlansRepository,
            Repository<tbLinkServerGroupWithUsers> linkUserGroupRepository)
        {
            if (order == null || string.IsNullOrEmpty(order.AccountName) || !order.AccountName.Contains("@"))
                return null;

            var username = order.AccountName.Split('@')[1];
            var userAccount = await usersRepository.FirstOrDefaultAsync(s => s.Username == username);
            if (userAccount == null || !userAccount.Role.HasValue)
                return null;

            // ادمین معمولاً بدهی کیف نماینده ندارد — لغو باید ادامه پیدا کند
            if (userAccount.Role == 1)
                return 0;

            if (!IsAgentRole(userAccount.Role.Value))
                return null;

            var plan = order.tbLinkUserAndPlans?.tbPlans;
            if (plan == null && order.V2_Plan_ID.HasValue)
                plan = await plansRepository.FirstOrDefaultAsync(p => p.Plan_ID_V2 == order.V2_Plan_ID);

            var linkPlan = order.tbLinkUserAndPlans;
            if (linkPlan == null && order.FK_Link_Plan_ID.HasValue)
                linkPlan = await linkUserAndPlansRepository.FirstOrDefaultAsync(p => p.Link_PU_ID == order.FK_Link_Plan_ID);
            if (plan == null && linkPlan?.tbPlans != null)
                plan = linkPlan.tbPlans;

            var orderPrice = order.Order_Price ?? order.PriceWithOutDiscount;
            var refundAmount = 0;

            if (userAccount.Role == 3)
            {
                if (plan != null)
                {
                    var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(
                        s => s.FK_Group_Id == plan.Group_Id && s.FK_User_Id == userAccount.User_ID);
                    if (linkGroupUser != null)
                    {
                        refundAmount = (int)((plan.PlanVolume * linkGroupUser.PriceForGig)
                            + (plan.PlanMonth * linkGroupUser.PriceForMonth)
                            + ((plan.device_limit ?? 0) * linkGroupUser.PriceForUser));
                    }
                }

                if (refundAmount <= 0 && orderPrice.HasValue && orderPrice.Value > 0)
                    refundAmount = (int)orderPrice.Value;

                if (refundAmount <= 0)
                    return null;

                userAccount.Wallet -= refundAmount;
            }
            else if (userAccount.Role == 2 || userAccount.Role == 4)
            {
                if (userAccount.Role == 2
                    && userAccount.tbUsers2 != null
                    && userAccount.tbUsers2.Role != 1
                    && userAccount.tbUsers2.Role == 3
                    && plan != null)
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

                if (linkPlan?.tbPlans != null && linkPlan.tbPlans.Price > 0)
                    refundAmount = (int)linkPlan.tbPlans.Price;
                else if (plan != null && plan.Price > 0)
                    refundAmount = (int)plan.Price;
                else if (orderPrice.HasValue && orderPrice.Value > 0)
                    refundAmount = (int)orderPrice.Value;

                if (refundAmount <= 0)
                    return null;

                userAccount.Wallet -= refundAmount;
            }

            if (refundAmount <= 0)
                return null;

            usersRepository.Save();
            return refundAmount;
        }

        /// <summary>
        /// اگر رزرو با کیف پول تلگرام پرداخت شده باشد (نه کارت)، مبلغ را به Tel_Wallet برمی‌گرداند.
        /// </summary>
        public static double? RefundTelegramWalletForReservedOrder(tbOrders order)
        {
            if (order?.tbTelegramUsers == null || !order.Order_Price.HasValue || order.Order_Price.Value <= 0)
                return null;

            if (order.tbDepositWallet_Log != null
                && order.tbDepositWallet_Log.Any(d => d.dw_Status == "FINISH"))
                return null;

            order.tbTelegramUsers.Tel_Wallet = (order.tbTelegramUsers.Tel_Wallet ?? 0) + order.Order_Price.Value;
            return order.Order_Price.Value;
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
            // برگشت کیف نماینده best-effort (نقش ۱ مقدار 0 برمی‌گرداند)
            await RefundReservedOrderWalletAsync(
                order, usersRepository, plansRepository, linkUserAndPlansRepository, linkUserGroupRepository);

            RefundTelegramWalletForReservedOrder(order);

            RemoveReservePackageLogs(logsRepository, order);
            ordersRepository.Delete(order);
            ordersRepository.Save();
        }
    }
}
