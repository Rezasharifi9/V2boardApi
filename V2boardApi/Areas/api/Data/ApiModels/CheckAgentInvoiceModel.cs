namespace V2boardApi.Areas.api.Data.ApiModels
{
    /// <summary>
    /// ورودی سرویس بررسی وضعیت فاکتور پرداخت مستقیم نماینده
    /// </summary>
    public class CheckAgentInvoiceModel
    {
        /// <summary>کد پیگیری فاکتور (همان dw_TaxId در جدول tbDepositWallet_Log)</summary>
        public string TaxId { get; set; }

        /// <summary>
        /// true یعنی مشتری پرداخت از کیف پول ربات تلگرام را انتخاب کرده است.
        /// اگر موجودی کافی باشد فاکتور همان لحظه تائید و مبلغ از Tel_Wallet کسر می شود.
        /// ارسال نشده یا false یعنی فقط وضعیت فاکتور خوانده شود (رفتار قبلی).
        /// </summary>
        public bool PayFromWallet { get; set; }
    }
}
