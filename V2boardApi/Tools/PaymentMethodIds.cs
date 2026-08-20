namespace V2boardApi.Tools
{
    /// <summary>
    /// شناسه های ثابت جدول tbPaymentMethods.
    /// مقادیر با اسکریپت Database/AddMobileUsersTable.sql هماهنگ هستند.
    /// </summary>
    public static class PaymentMethodIds
    {
        /// <summary>
        /// پرداخت از داخل اپلیکیشن موبایل (tbpm_Key = "APP").
        /// فاکتورهایی که این روش پرداخت را دارند وارد مسیر تائید خودکار پیامک
        /// (TransactionHanderService.CheckOrder) نمی شوند و پیام ربات هم نمی گیرند.
        /// </summary>
        public const int App = 5;

        /// <summary>کلید همین روش پرداخت در ستون tbpm_Key</summary>
        public const string AppKey = "APP";
    }
}
