using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V2boardApi.Tools
{
    public static class BotInvoiceGuard
    {
        public const int PendingInvoiceDays = 2;

        public static async Task ExpireOldPendingInvoicesAsync(
            Repository<tbOrders> ordersRepository,
            Repository<tbDepositWallet_Log> depositRepository,
            int telUserId)
        {
            var expireBefore = DateTime.Now.AddDays(-PendingInvoiceDays);
            var oldOrders = ordersRepository
                .Where(o => o.FK_Tel_UserID == telUserId &&
                            o.OrderStatus == "FOR_PAY" &&
                            o.OrderDate < expireBefore)
                .ToList();

            foreach (var order in oldOrders)
            {
                order.OrderStatus = "EXPIRED";
                var deposits = depositRepository
                    .Where(d => d.FK_Order_ID == order.Order_ID && d.dw_Status == "FOR_PAY")
                    .ToList();
                foreach (var deposit in deposits)
                    deposit.dw_Status = "EXPIRED";
            }

            if (oldOrders.Count > 0)
            {
                await ordersRepository.SaveChangesAsync();
                await depositRepository.SaveChangesAsync();
            }
        }

        public static tbOrders GetBlockingPendingOrder(
            Repository<tbOrders> ordersRepository,
            int telUserId,
            int linkPlanId)
        {
            var cutoff = DateTime.Now.AddDays(-PendingInvoiceDays);
            return ordersRepository
                .Where(o => o.FK_Tel_UserID == telUserId &&
                            o.OrderStatus == "FOR_PAY" &&
                            o.FK_Link_Plan_ID == linkPlanId &&
                            o.OrderDate >= cutoff)
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefault();
        }

        public static tbDepositWallet_Log GetPendingDeposit(
            Repository<tbDepositWallet_Log> depositRepository,
            int orderId)
        {
            return depositRepository
                .Where(d => d.FK_Order_ID == orderId && d.dw_Status == "FOR_PAY")
                .OrderByDescending(d => d.dw_CreateDatetime)
                .FirstOrDefault();
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
