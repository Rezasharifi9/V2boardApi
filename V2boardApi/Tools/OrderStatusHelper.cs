using System;

namespace V2boardApi.Tools
{
    /// <summary>
    /// نمایش وضعیت سفارش/فاکتور در پنل — بدون وضعیت جدید در دیتابیس.
    /// وضعیت‌های ذخیره‌شده: FINISH، FOR_PAY، FOR_RESERVE، CANCELED
    /// </summary>
    public static class OrderStatusHelper
    {
        public const int DisplayPendingActivation = 0;
        public const int DisplayFinished = 1;
        public const int DisplayPendingPayment = 3;
        public const int DisplayExpired = 4;
        public const int DisplayCanceled = 5;

        public static int GetOrderDisplayStatus(string orderStatus, DateTime? orderDate)
        {
            if (string.Equals(orderStatus, "FOR_RESERVE", StringComparison.OrdinalIgnoreCase))
                return DisplayPendingActivation;

            if (string.Equals(orderStatus, "FINISH", StringComparison.OrdinalIgnoreCase))
                return DisplayFinished;

            if (string.Equals(orderStatus, "CANCELED", StringComparison.OrdinalIgnoreCase))
                return DisplayCanceled;

            if (string.Equals(orderStatus, "FOR_PAY", StringComparison.OrdinalIgnoreCase)
                || string.Equals(orderStatus, "EXPIRED", StringComparison.OrdinalIgnoreCase))
            {
                if (IsExpiredPendingOrder(orderDate))
                    return DisplayExpired;

                if (string.Equals(orderStatus, "FOR_PAY", StringComparison.OrdinalIgnoreCase))
                    return DisplayPendingPayment;
            }

            return -1;
        }

        public static int GetDepositDisplayStatus(string dwStatus, DateTime? createDate)
        {
            if (string.Equals(dwStatus, "FINISH", StringComparison.OrdinalIgnoreCase))
                return DisplayFinished;

            if (string.Equals(dwStatus, "CANCELED", StringComparison.OrdinalIgnoreCase))
                return DisplayCanceled;

            if (string.Equals(dwStatus, "FOR_PAY", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dwStatus, "EXPIRED", StringComparison.OrdinalIgnoreCase))
            {
                if (IsExpiredPendingDeposit(createDate))
                    return DisplayExpired;

                if (string.Equals(dwStatus, "FOR_PAY", StringComparison.OrdinalIgnoreCase))
                    return 0;
            }

            return -1;
        }

        public static bool IsExpiredPendingOrder(DateTime? orderDate)
        {
            if (!orderDate.HasValue)
                return false;

            return orderDate.Value <= DateTime.Now.AddHours(-BotInvoiceGuard.InvoiceWarningHours);
        }

        public static bool IsExpiredPendingDeposit(DateTime? createDate)
        {
            if (!createDate.HasValue)
                return false;

            // هم‌راستا با Timers.CheckExpireFactores برای کارت‌به‌کارت
            return createDate.Value <= DateTime.Now.AddHours(-BotInvoiceGuard.InvoiceWarningHours);
        }
    }
}
