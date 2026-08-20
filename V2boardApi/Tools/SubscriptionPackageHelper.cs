using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace V2boardApi.Tools
{
    public static class SubscriptionPackageHelper
    {
        public const long MinRemainingBytes = 50L * 1024 * 1024;
        public const double MaxEarlyDeleteUsageGb = 1.0;
        public static readonly TimeSpan MaxEarlyDeleteAge = TimeSpan.FromDays(1);

        /// <summary>
        /// مقدار device_limit تعرفه را بدون تغییر برای ذخیره در v2_user برمی‌گرداند.
        /// </summary>
        public static int? ResolveDeviceLimitForV2(tbPlans plan)
        {
            if (plan?.device_limit == null || plan.device_limit.Value <= 0)
                return null;

            return (int)plan.device_limit.Value;
        }

        public static bool IsPackageEnded(long transferEnable, long download, long upload, object expiredAtUnix)
        {
            var remaining = transferEnable - (download + upload);
            if (remaining < MinRemainingBytes)
                return true;

            if (expiredAtUnix != null && !string.IsNullOrWhiteSpace(expiredAtUnix.ToString()))
            {
                var expireDate = Utility.ConvertSecondToDatetime(Convert.ToInt64(expiredAtUnix));
                if (expireDate <= DateTime.Now.AddHours(1))
                    return true;
            }

            return false;
        }

        public static bool IsEligibleForEarlyDeleteWithRefund(long? createdAtUnix, long download, long upload)
        {
            if (!createdAtUnix.HasValue || createdAtUnix.Value <= 0)
                return false;

            if (Utility.ConvertByteToGB(download + upload) >= MaxEarlyDeleteUsageGb)
                return false;

            var createdAt = Utility.ConvertSecondToDatetime(createdAtUnix.Value);
            return DateTime.Now - createdAt <= MaxEarlyDeleteAge;
        }

        public static bool CanAgentDeleteSubscription(
            int userRole,
            long transferEnable,
            long download,
            long upload,
            object expiredAtUnix,
            long? createdAtUnix = null)
        {
            if (userRole == 1 || userRole == 4)
                return true;

            if (IsEligibleForEarlyDeleteWithRefund(createdAtUnix, download, upload))
                return true;

            return IsPackageEnded(transferEnable, download, upload, expiredAtUnix);
        }

        public static bool CanAgentDeleteSubscription(int userRole, long transferEnable, long download, long upload, object expiredAtUnix)
        {
            return CanAgentDeleteSubscription(userRole, transferEnable, download, upload, expiredAtUnix, null);
        }

        public static async Task<bool> ApplyReservedOrderAsync(
            tbOrders order,
            MySqlEntities mySql,
            Repository<tbOrders> ordersRepository,
            Repository<tbLinks> linksRepository = null)
        {
            if (order == null || string.IsNullOrEmpty(order.AccountName) || !order.V2_Plan_ID.HasValue)
                return false;

            var t = Utility.ConvertGBToByte(Convert.ToInt64(order.Traffic));
            object expValue = null;
            if (order.Month != 0)
                expValue = DateTime.Now.AddDays((int)order.Month * 30).ConvertDatetimeToSecond().ToString();

            var disc = new Dictionary<string, object>
            {
                { "@plan_id", order.V2_Plan_ID.Value },
                { "@transfer_enable", t },
                { "@expired_at", expValue },
                { "@email", order.AccountName }
            };

            var reader2 = await mySql.GetDataAsync("select group_id from v2_plan where id = @plan_id", disc);
            if (!await reader2.ReadAsync())
            {
                reader2.Close();
                return false;
            }
            disc.Add("@group_id", reader2.GetInt32("group_id"));
            reader2.Close();

            var deviceLimitPart = ",device_limit=20";
            var resolvedDeviceLimit = ResolveDeviceLimitForV2(order.tbLinkUserAndPlans?.tbPlans);
            if (resolvedDeviceLimit.HasValue)
                deviceLimitPart = ",device_limit=" + resolvedDeviceLimit.Value;

            var query = "update v2_user set u=0,d=0,t=0,plan_id=@plan_id" + deviceLimitPart +
                        ",group_id=@group_id,transfer_enable=@transfer_enable,expired_at=@expired_at where email=@email";

            var reader = await mySql.GetDataAsync(query, disc);
            await reader.ReadAsync();
            reader.Close();

            if (linksRepository != null)
            {
                var link = linksRepository.Where(p => p.tbL_Email == order.AccountName).FirstOrDefault();
                if (link != null)
                {
                    SubscriptionReserveWarnHelper.ResetReserveWarnState(link);
                    link.tb_AutoRenew = false;
                    await linksRepository.SaveChangesAsync();
                }
            }

            order.OrderStatus = "FINISH";
            order.Tel_RenewedDate = DateTime.Now;
            await ordersRepository.SaveChangesAsync();
            return true;
        }
    }
}
