using DataLayer.DomainModel;
using System.Collections.Concurrent;
using System.Text;

namespace V2boardApi.Tools
{
    public static class BotInvoiceGuard
    {
        public const int InvoiceWarningHours = 24;
        public const int InvoiceDeleteHours = 72;

        /// <summary>برای نمایش «منقضی شده» در پنل — هم‌راستا با هشدار ۲۴ ساعته.</summary>
        public const int PendingInvoiceDays = 1;

        public const string BuyInvoiceStep = "WaitForBuyInvoicePay";
        public const string DuplicatePayClickMessage = "⚠️ فاکتور این تعرفه قبلا ثبت شده و هنوز اعتبار داره. همون مبلغ رو واریز کن یا تعرفه دیگری انتخاب کن";

        private static readonly ConcurrentDictionary<string, byte> PayClickLocks = new ConcurrentDictionary<string, byte>();

        public static bool TryEnterPayLock(string userUniqId)
        {
            return !string.IsNullOrEmpty(userUniqId) && PayClickLocks.TryAdd(userUniqId, 1);
        }

        public static void ExitPayLock(string userUniqId)
        {
            if (!string.IsNullOrEmpty(userUniqId))
                PayClickLocks.TryRemove(userUniqId, out _);
        }

        public static bool TryClaimPaymentClick(tbTelegramUsers user)
        {
            if (user == null)
                return false;
            if (user.Tel_Step != "WaitForPay" && user.Tel_Step != BuyInvoiceStep)
                return false;
            user.Tel_Step = BuyInvoiceStep;
            return true;
        }

        public static void ReleasePaymentClick(tbTelegramUsers user)
        {
            if (user != null && user.Tel_Step == BuyInvoiceStep)
                user.Tel_Step = "WaitForPay";
        }

        public static string BuildCardToCardPaymentMessage(
            string taxId,
            double fullPriceRial,
            string cardNumber,
            string cardHolderName,
            bool isActiveCardToCard,
            bool isActiveSendReceipt,
            string botId)
        {
            var str = new StringBuilder();
            str.AppendLine("✅  تراکنش شما باموفقیت ثبت شد ");
            str.AppendLine();
            str.AppendLine("کد پیگیری : " + "<code>" + taxId + "</code>");
            str.AppendLine();
            str.AppendLine("💳 لطفاً مبلغ " + "<code>" + fullPriceRial.ConvertToMony() + "</code>" + " ریال رو به شماره کارت زیر واریز کن :");
            str.AppendLine("");
            str.AppendLine(cardNumber);
            str.AppendLine("به نام : " + cardHolderName);
            str.AppendLine("");
            str.AppendLine("🔹 روی مبلغ کلیک کن تا خودش کپی بشه — لازم نیست حفظش کنی 😌");

            if (isActiveSendReceipt && isActiveCardToCard)
            {
                str.AppendLine("🔹 حتماً مبلغ رو دقیقاً با سه رقم آخر واریز کن. اگه مبلغ رو دقیق نزنی، ربات نمی‌تونه تراکنشت رو تشخیص بده ❗️");
                str.AppendLine("");
                str.AppendLine("📸 اگه به هر دلیلی پرداختت به‌صورت خودکار تأیید نشد، کافیه رسید واریزی رو به‌صورت عکس (نه فایل) برای ربات بفرستی.");
            }
            else
            {
                if (isActiveCardToCard)
                    str.AppendLine("❗️حتما حتما مبلغ را دقیق با سه رقم اخر واریز کنید در غیر اینصورت ربات واریزی شمارو تشخیص نمی دهد");
                if (isActiveSendReceipt)
                {
                    str.AppendLine("");
                    str.Append("✅");
                    str.AppendLine("بعد واریزی حتما رسید را برای ربات بفرستید");
                }
            }

            str.AppendLine("");
            if (isActiveCardToCard)
            {
                str.AppendLine("⚠️ نکته مهم:\r\n");
                str.AppendLine("<b>" + "هر فاکتور فقط ۲۴ ساعت اعتبار داره. اگه پیام \"منقضی شدن فاکتور\" برات اومد، دیگه هیچ مبلغی واریز نکن ❌ " + "</b>");
                str.AppendLine("");
                str.AppendLine("<b>" + "🔺 حواست باشه! اگه مبلغ اشتباه واریز بشه، امکان برگشت وجه وجود نداره 🙏" + "</b>");
                str.AppendLine("");
                str.AppendLine("<b> ⚠️ با اپلیکیشن های آپ و 780 واریز نکن این اپلیکیشن ها محدودیت واریزی دارند </b>");
            }

            str.AppendLine("");
            str.AppendLine("🆔 @" + botId);
            return str.ToString();
        }
    }
}
