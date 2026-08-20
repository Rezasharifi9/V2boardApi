using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DomainModel;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace V2boardApi.Tools
{
    /// <summary>
    /// آپلود رسید فاکتور اپلیکیشن و ارسال آن به ادمین ربات تلگرام نماینده.
    /// تائید ادمین همان دو مرحله‌ی ربات است: دکمه تائید ، بعد انتخاب مبلغ.
    /// </summary>
    public static class AppInvoiceReceiptService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>کال‌بک مرحله‌ی اول: نمایش لیست مبلغ فاکتورهای باز</summary>
        public const string AcceptPrefix = "Aaccept";

        /// <summary>کال‌بک مرحله‌ی دوم: تائید فاکتور انتخاب‌شده</summary>
        public const string ConfirmPrefix = "AFaccept";

        public const int MaxFileBytes = 4 * 1024 * 1024;

        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public const string FolderVirtualPath = "~/assets/img/AppInvoiceReceipts/";

        public class SendResult
        {
            public bool SentToAdmin { get; set; }
            public string FileName { get; set; }
            public string Message { get; set; }
        }

        public static bool IsAllowedImage(string fileName, long contentLength)
        {
            if (contentLength <= 0 || contentLength > MaxFileBytes)
            {
                return false;
            }

            var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
            return AllowedExtensions.Contains(ext);
        }

        public static string BuildFileName(string originalFileName)
        {
            var ext = Path.GetExtension(originalFileName ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                ext = ".jpg";
            }

            return Guid.NewGuid().ToString() + ext;
        }

        /// <summary>
        /// فایل را در دیسک ذخیره می‌کند و عکس را با دکمه‌ی تائید برای ادمین ربات می‌فرستد.
        /// معادل ارسال عکس رسید توسط کاربر در ربات.
        /// </summary>
        public static async Task<SendResult> SaveAndNotifyAdminAsync(
            tbDepositWallet_Log deposit,
            tbUsers agent,
            Stream fileStream,
            string originalFileName,
            string folderPhysicalPath)
        {
            var result = new SendResult();
            Directory.CreateDirectory(folderPhysicalPath);

            var fileName = BuildFileName(originalFileName);
            var fullPath = Path.Combine(folderPhysicalPath, fileName);

            using (var output = System.IO.File.Create(fullPath))
            {
                await fileStream.CopyToAsync(output);
            }

            DeletePreviousFile(deposit, folderPhysicalPath);
            result.FileName = fileName;

            var botSettings = agent.tbBotSettings.FirstOrDefault();
            if (botSettings == null || string.IsNullOrWhiteSpace(botSettings.Bot_Token) || botSettings.AdminBot_ID <= 0)
            {
                result.SentToAdmin = false;
                result.Message = "رسید ذخیره شد ولی ربات یا ادمین تلگرام نماینده تنظیم نشده است";
                logger.Warn("ارسال رسید فاکتور " + deposit.dw_TaxId + " ممکن نبود: ربات نماینده تنظیم نشده");
                return result;
            }

            try
            {
                var client = new TelegramBotClient(botSettings.Bot_Token);
                var caption = BuildCaption(deposit, agent);
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("✅ تائید", AcceptPrefix + "%" + deposit.dw_ID) }
                });

                using (var photoStream = System.IO.File.OpenRead(fullPath))
                {
                    var photo = InputFile.FromStream(photoStream, fileName);
                    await client.SendPhoto(botSettings.AdminBot_ID, photo, caption: caption, parseMode: ParseMode.Html, replyMarkup: keyboard);
                }

                result.SentToAdmin = true;
                result.Message = "رسید برای ادمین ارسال شد. پس از تائید، اشتراک تمدید می‌شود";
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ارسال رسید فاکتور اپلیکیشن " + deposit.dw_TaxId + " به تلگرام");
                result.SentToAdmin = false;
                result.Message = "رسید ذخیره شد ولی ارسال به تلگرام ادمین با خطا مواجه شد";
                return result;
            }
        }

        public static InlineKeyboardMarkup BuildAmountKeyboard(IList<tbDepositWallet_Log> invoices)
        {
            const int itemsPerRow = 2;
            var rows = new List<List<InlineKeyboardButton>>();
            for (int i = 0; i < invoices.Count; i += itemsPerRow)
            {
                var row = new List<InlineKeyboardButton>();
                for (int j = i; j < i + itemsPerRow && j < invoices.Count; j++)
                {
                    var price = invoices[j].dw_Price ?? 0;
                    row.Add(InlineKeyboardButton.WithCallbackData(price.ConvertToMony(), ConfirmPrefix + "%" + invoices[j].dw_ID));
                }
                rows.Add(row);
            }

            return new InlineKeyboardMarkup(rows);
        }

        private static string BuildCaption(tbDepositWallet_Log deposit, tbUsers agent)
        {
            var order = deposit.tbOrders;
            var displayName = GetDisplayName(order == null ? null : order.AccountName);
            var str = new StringBuilder();
            str.AppendLine("📱 رسید پرداخت اپلیکیشن");
            str.AppendLine("");
            str.AppendLine("👤 اشتراک : " + displayName);
            str.AppendLine("🔖 کد پیگیری : <code>" + deposit.dw_TaxId + "</code>");
            str.AppendLine("💰 مبلغ : <code>" + (deposit.dw_Price ?? 0).ConvertToMony() + "</code> ریال");
            if (order != null && !string.IsNullOrWhiteSpace(order.OrderType))
            {
                str.AppendLine("📦 نوع : " + order.OrderType);
            }

            var device = deposit.tbMobileUsers;
            if (device != null)
            {
                var deviceLabel = ((device.tbMu_Manufacturer ?? "") + " " + (device.tbMu_Model ?? "")).Trim();
                if (deviceLabel.Length == 0)
                {
                    deviceLabel = device.tbMu_Device;
                }
                if (!string.IsNullOrWhiteSpace(deviceLabel))
                {
                    str.AppendLine("📲 دستگاه : " + deviceLabel);
                }
            }

            if (agent != null && !string.IsNullOrWhiteSpace(agent.Username))
            {
                str.AppendLine("👔 نماینده : " + agent.Username);
            }

            str.AppendLine("");
            str.AppendLine("♨️ موارد فوق مورد تایید است ؟");
            return str.ToString();
        }

        private static void DeletePreviousFile(tbDepositWallet_Log deposit, string folderPhysicalPath)
        {
            if (deposit == null || string.IsNullOrWhiteSpace(deposit.dw_payment_id))
            {
                return;
            }

            var previous = Path.GetFileName(deposit.dw_payment_id);
            var ext = Path.GetExtension(previous).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return;
            }

            var previousPath = Path.Combine(folderPhysicalPath, previous);
            if (System.IO.File.Exists(previousPath))
            {
                try
                {
                    System.IO.File.Delete(previousPath);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "حذف رسید قبلی فاکتور " + deposit.dw_TaxId + " ناموفق بود");
                }
            }
        }

        private static string GetDisplayName(string accountName)
        {
            if (string.IsNullOrEmpty(accountName))
            {
                return accountName;
            }

            var cut = accountName.IndexOfAny(new[] { '$', '@' });
            return cut >= 0 ? accountName.Substring(0, cut) : accountName;
        }
    }
}
