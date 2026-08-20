using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using V2boardApi.Areas.App.Data.RequestModels;
using V2boardApi.Areas.App.Data.UsersViewModels;
using V2boardApi.Resource;

namespace V2boardApi.Tools
{
    public static class SubscriptionLogHelper
    {
        public const string DeletedNamePrefix = "del_";
        public const string DeletedEventLabel = "اشتراک حذف شد";

        public static bool IsAllowedSubscriptionLogAction(string action)
        {
            return action == ReservedPackageHelper.CreatedLogAction
                || action == ReservedPackageHelper.EditedLogAction
                || action == ReservedPackageHelper.ReserveLogAction
                || action == LogActions.U_Created
                || action == LogActions.U_Edited;
        }

        public static bool IsDeletedSubscriptionLogName(string fkNameUserId)
        {
            return !string.IsNullOrEmpty(fkNameUserId)
                && fkNameUserId.StartsWith(DeletedNamePrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetSubscriptionAccountName(string fkNameUserId)
        {
            if (string.IsNullOrWhiteSpace(fkNameUserId))
                return "-";

            if (IsDeletedSubscriptionLogName(fkNameUserId))
                return fkNameUserId.Substring(DeletedNamePrefix.Length);

            return fkNameUserId.Split('@')[0];
        }

        public static string MarkDeletedLogName(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName))
                return accountName;

            if (IsDeletedSubscriptionLogName(accountName))
                return accountName;

            return DeletedNamePrefix + accountName;
        }

        public static void MarkSubscriptionLogsDeleted(IEnumerable<tbLogs> logs, string accountName)
        {
            if (logs == null || string.IsNullOrWhiteSpace(accountName))
                return;

            foreach (var log in logs)
                log.FK_NameUser_ID = MarkDeletedLogName(accountName);
        }

        public static bool CountsTowardSalesSummary(tbLogs log)
        {
            if (log == null || IsDeletedSubscriptionLogName(log.FK_NameUser_ID))
                return false;

            return IsAllowedSubscriptionLogAction(log.Action);
        }

        public static bool ShouldAppearInAgentHistory(tbLogs log)
        {
            if (log == null)
                return false;

            return IsAllowedSubscriptionLogAction(log.Action)
                || IsDeletedSubscriptionLogName(log.FK_NameUser_ID);
        }

        public static List<UserLogResponseModel> MapAgentHistoryItems(IEnumerable<tbLogs> logs)
        {
            var items = new List<UserLogResponseModel>();
            var seenDeleted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in logs.OrderByDescending(l => l.CreateDatetime))
            {
                if (!ShouldAppearInAgentHistory(item) || !item.CreateDatetime.HasValue)
                    continue;

                if (IsDeletedSubscriptionLogName(item.FK_NameUser_ID))
                {
                    var accountName = GetSubscriptionAccountName(item.FK_NameUser_ID);
                    if (!seenDeleted.Add(accountName))
                        continue;

                    var displayName = accountName;
                    if (displayName.Length > 20)
                        displayName = displayName.Substring(0, 10);

                    items.Add(new UserLogResponseModel
                    {
                        id = item.log_ID,
                        SubName = displayName,
                        Event = DeletedEventLabel,
                        CreateDate = item.CreateDatetime.Value.ConvertDateTimeToShamsi2(),
                        SellPrice = "-",
                        Plan = item.PlanName
                    });
                    continue;
                }

                var subName = GetSubscriptionAccountName(item.FK_NameUser_ID);
                if (subName.Length > 20)
                    subName = subName.Substring(0, 10);

                items.Add(new UserLogResponseModel
                {
                    id = item.log_ID,
                    SubName = subName,
                    Event = item.Action,
                    CreateDate = item.CreateDatetime.Value.ConvertDateTimeToShamsi2(),
                    SellPrice = item.SalePrice.HasValue ? item.SalePrice.Value.ConvertToMony() : "0",
                    Plan = item.PlanName
                });
            }

            return items;
        }
    }
}
