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
            if (order.tbLinkUserAndPlans?.tbPlans?.device_limit != null)
                deviceLimitPart = ",device_limit=" + (order.tbLinkUserAndPlans.tbPlans.device_limit + 1);

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
