using System.Threading.Tasks;
using System;
using DataLayer.DomainModel;
using DataLayer.Repository;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using System.Text;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using V2boardApi.Tools;
using V2boardBot.Models;
using System.Linq;
using System.Web;
using NLog;
using MySqlConnector;

public class TimerService
{
    //ngrok http 4480 --host-header="localhost:4480"
    private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private System.Threading.Timer CheckLink;
    private System.Threading.Timer CheckRenewAccount;
    //private System.Threading.Timer CheckSubLimitedUser;
    private System.Threading.Timer DeleteTestAccount;
    private System.Threading.Timer DeleteFactores;
    private System.Threading.Timer AlertDeleteFactoresCard;
    private System.Threading.Timer RemoveFactoresCard;
    private System.Threading.Timer CheckSettlement;
    private System.Threading.Timer CheckAgentLimitWarning;
    private System.Threading.Timer DailySalesReportTimer;
    private static readonly object DailyReportLock = new object();
    private static DateTime? _lastDailyReportDate;
    private tbServers Server;
    public TimerService()
    {
        // تنظیم تایمرها — همه از SafeRun رد می شوند تا هیچ استثنایی به تِرد پس زمینه نشت نکند
        CheckLink = new System.Threading.Timer(_ => SafeRun(CheckSubTimerCallback, "CheckSubTimerCallback"), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(900000));
        AlertDeleteFactoresCard = new System.Threading.Timer(_ => SafeRun(CheckExpireFactores, "CheckExpireFactores"), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(3600000));
        CheckRenewAccount = new System.Threading.Timer(_ => SafeRun(CheckRenewAccountFun, "CheckRenewAccountFun"), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(60000));
        //CheckSubLimitedUser = new System.Threading.Timer(_ => SafeRun(CheckSubLimitedUsers, "CheckSubLimitedUsers"), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(180000));
        DeleteTestAccount = new System.Threading.Timer(_ => SafeRun(DeleteTestSub, "DeleteTestSub"), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(86400000));
        RemoveFactoresCard = new System.Threading.Timer(_ => SafeRun(RemoveExpireFactores, "RemoveExpireFactores"), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(86400000));
        CheckSettlement = new System.Threading.Timer(_ => SafeRun(SettlementService.ProcessAllAgents, "ProcessAllAgents"), null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
        CheckAgentLimitWarning = new System.Threading.Timer(_ => SafeRun(AgentLimitNotificationService.ProcessAllAgentsAsync, "ProcessAllAgentsAsync"), null, TimeSpan.FromMinutes(10), TimeSpan.FromHours(1));
        DailySalesReportTimer = new System.Threading.Timer(_ => SafeRun(CheckDailySalesReport, "CheckDailySalesReport"), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        Server = ServerCacheHelper.Get();
    }

    /// <summary>
    /// اجرای کال بک تایمر با گارد کامل؛ بدون این گارد یک استثنای مدیریت نشده
    /// (مثل قطع بودن MySQL) کل پروسه IIS را می بندد.
    /// </summary>
    private static async void SafeRun(Func<Task> callback, string name)
    {
        try
        {
            await callback().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try { logger.Error(ex, "خطای مدیریت نشده در تایمر " + name); }
            catch { }
        }
    }

    private tbServers EnsureServer()
    {
        if (Server == null)
            Server = ServerCacheHelper.Get();
        return Server;
    }

    #region گزارش فروش روزانه — هر شب ساعت 23:59

    private async Task CheckDailySalesReport()
    {
        try
        {
            var now = DateTime.Now;
            if (now.Hour != 23 || now.Minute != 59)
                return;

            lock (DailyReportLock)
            {
                if (_lastDailyReportDate == now.Date)
                    return;
                _lastDailyReportDate = now.Date;
            }

            if (EnsureServer() == null)
                return;

            await DailySalesReportService.SendDailyReportsAsync(Server);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "خطا در زمان‌بندی گزارش فروش روزانه");
        }
    }

    #endregion


    #region چک کردن کاربری که حجمش نزدیک به اتمام برای اطلاع دادن 
    private async Task CheckSubTimerCallback()
    {
        try
        {
            if (EnsureServer() == null)
                return;

            using (Entities db = new Entities())
            {
                var tbTelegramUserRepository = new Repository<tbTelegramUsers>(db);
                var tbOrdersRepository = new Repository<tbOrders>(db);
                var users = await tbTelegramUserRepository.GetAllAsync();

                using (var mySql = new MySqlEntities(Server.ConnectionString))
                {
                    await mySql.OpenAsync();
                    foreach (var item in users.ToList())
                    {
                        try
                        {
                            if (item.tbUsers?.tbBotSettings == null)
                                continue;

                            var botSetting = item.tbUsers.tbBotSettings.FirstOrDefault();
                            if (botSetting?.Bot_Token == null || botSetting.Enabled != true)
                                continue;

                            var bot = BotManager.GetBot(item.tbUsers.Username);
                            if (bot == null)
                                continue;

                            foreach (var link in item.tbLinks.Where(p => p.tb_AutoRenew != true).ToList())
                            {
                                var reservedOrders = await tbOrdersRepository.WhereAsync(
                                    p => p.AccountName == link.tbL_Email && p.OrderStatus == "FOR_RESERVE");
                                var hasReservedPackage = reservedOrders.Count > 0;
                                if (hasReservedPackage)
                                {
                                    if (SubscriptionReserveWarnHelper.GetMask(link) != 0)
                                    {
                                        link.tbL_ReserveWarnMask = 0;
                                        await tbTelegramUserRepository.SaveChangesAsync();
                                    }
                                    continue;
                                }

                                var queryParams = new Dictionary<string, object> { { "@email", link.tbL_Email } };
                                using (var reader = await mySql.GetDataAsync(
                                    "select u,d,transfer_enable,banned,expired_at from v2_user where email=@email",
                                    queryParams))
                                {
                                    if (!await reader.ReadAsync())
                                        continue;

                                    var isBanned = reader.GetBoolean("banned");
                                    var remainingBytes = reader.GetInt64("transfer_enable")
                                        - (reader.GetInt64("d") + reader.GetInt64("u"));
                                    var remainingGb = Utility.ConvertByteToGB(remainingBytes);

                                    DateTime? expireDate = null;
                                    var exp = reader["expired_at"]?.ToString();
                                    if (!string.IsNullOrEmpty(exp))
                                        expireDate = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp));

                                    reader.Close();

                                    var warnings = SubscriptionReserveWarnHelper.GetPendingWarnings(
                                        link, remainingGb, expireDate, isBanned, false);

                                    if (warnings.Count == 0)
                                        continue;

                                    var sent = new List<ReserveWarnItem>();
                                    foreach (var warning in warnings)
                                    {
                                        var message = warning.Message;
                                        message += Environment.NewLine;
                                        message += Environment.NewLine;
                                        message += "〰️〰️〰️〰️〰️";
                                        message += Environment.NewLine;
                                        message += "🚀@" + botSetting.Bot_ID;

                                        var delivered = await TelegramNotifyHelper.TrySendMessageAsync(
                                            bot.Client, item.Tel_UniqUserID, message, ParseMode.Html);
                                        if (delivered)
                                            sent.Add(warning);
                                    }

                                    if (sent.Count > 0)
                                    {
                                        SubscriptionReserveWarnHelper.ApplySentFlags(link, sent);
                                        await tbTelegramUserRepository.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                        }
                    }
                    await mySql.CloseAsync().ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }

    #endregion


    #region تابع حذف کردن اشتراکات تست

    private async Task DeleteTestSub()
    {
        try
        {
            if (EnsureServer() == null)
                return;

            using (Entities db = new Entities())
            {
                var BotSettingRepository = new Repository<tbBotSettings>(db);
                var botsetting = await BotSettingRepository
                .WhereAsync(p => p.Enabled == true && p.Bot_Token != null)
                .ConfigureAwait(false);


                foreach (var item in botsetting)
                {
                    try
                    {
                        using (MySqlEntities mySql = new MySqlEntities(Server.ConnectionString))
                        {
                            await mySql.OpenAsync().ConfigureAwait(false);

                            string query = "DELETE FROM v2_user WHERE ((v2_user.d + v2_user.u) > v2_user.transfer_enable OR expired_at < UNIX_TIMESTAMP()) AND email LIKE @BotID";

                            using (var command = new MySqlConnector.MySqlCommand(query, mySql.MySqlConnection))
                            {
                                command.Parameters.AddWithValue("@BotID", item.Bot_ID + "%");

                                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                                {
                                    while (await reader.ReadAsync().ConfigureAwait(false))
                                    {
                                        // پردازش در صورت نیاز
                                    }
                                    reader.Close();
                                }
                            }
                            await mySql.CloseAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        // قطع بودن MySQL نباید بقیه ربات ها را متوقف کند
                        logger.Error(ex, "خطا در حذف اشتراک های تست ربات " + item.Bot_ID);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "خطا در تایمر حذف اشتراک های تست");
        }
    }


    #endregion

    #region تایمر رزرو تمدید اکانت
    public async Task CheckRenewAccountFun()
    {
        try
        {
            if (EnsureServer() == null)
                return;

            using (Entities db = new Entities())
            {
                var tbOrdersRepository = new Repository<tbOrders>(db);
                var Order = await tbOrdersRepository.WhereAsync(p => p.OrderType == "تمدید" && p.OrderStatus == "FOR_RESERVE");

                if (Order.Count >= 1)
                {
                    var tbLinksRepository = new Repository<tbLinks>(db);
                    MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);

                    await mySql.OpenAsync();
                    foreach (var item in Order)
                    {
                        var disc = new Dictionary<string, object> { { "@email", item.AccountName } };
                        using (var reader = await mySql.GetDataAsync("select u,d,transfer_enable,expired_at from v2_user where email = @email", disc))
                        {
                            if (!await reader.ReadAsync())
                                continue;

                            var d = reader.GetInt64("d");
                            var u = reader.GetInt64("u");
                            var transferEnable = reader.GetInt64("transfer_enable");
                            var expiredAt = reader["expired_at"];
                            reader.Close();

                            if (!SubscriptionPackageHelper.IsPackageEnded(transferEnable, d, u, expiredAt))
                                continue;

                            var applied = await SubscriptionPackageHelper.ApplyReservedOrderAsync(
                                item, mySql, tbOrdersRepository, tbLinksRepository);

                            if (!applied)
                                continue;

                            try
                            {
                                var agentUsername = item.AccountName.Contains("@")
                                    ? item.AccountName.Split('@')[1]
                                    : item.tbTelegramUsers?.tbUsers?.Username;

                                if (!string.IsNullOrEmpty(agentUsername))
                                {
                                    var bot = BotManager.GetBot(agentUsername);
                                    var link = tbLinksRepository.Where(p => p.tbL_Email == item.AccountName).FirstOrDefault();
                                    if (bot != null && link?.tbTelegramUsers != null)
                                    {
                                        await TelegramNotifyHelper.TrySendMessageAsync(
                                            bot.Client,
                                            link.tbTelegramUsers.Tel_UniqUserID,
                                            "✅ بسته رزرو شده شما فعال شد. از بخش مدیریت اشتراک‌ها جزئیات را مشاهده کنید.");
                                    }
                                }
                            }
                            catch { }
                        }
                    }

                    await mySql.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }

    }

    #endregion

    #region پاک کردن فاکتورها

    #region پیغام پاک شدن فاکتور های کارت به کارت
    public async Task CheckExpireFactores()
    {
        try
        {
            await CheckExpireFactoresCore();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "خطا در تایمر هشدار انقضای فاکتورها");
        }
    }

    private async Task CheckExpireFactoresCore()
    {
        var DateNow_CardToCard = DateTime.Now.AddHours(-BotInvoiceGuard.InvoiceWarningHours);
        var DateNow_TetraPay = DateTime.Now.AddHours(-1);

        using (Entities db = new Entities())
        {
            var CardToCardFactores = db.tbDepositWallet_Log.Where(s => s.dw_CreateDatetime <= DateNow_CardToCard && s.dw_Status == "FOR_PAY" && s.tbPaymentMethods.tbpm_Key == "CardToCard" && s.dw_Alerted == false).ToList();
            foreach (var item in CardToCardFactores)
            {
                try
                {

                    if (item.tbTelegramUsers.tbUsers.tbBotSettings.Count() != 0)
                    {
                        var BotSetting = item.tbTelegramUsers.tbUsers.tbBotSettings.ToList()[0];

                        var botClient = new TelegramBotClient(BotSetting.Bot_Token);

                        StringBuilder str = new StringBuilder();
                        str.Append("❌ تراکنش با مبلغ " + item.dw_Price.Value.ConvertToMony() + " ریال منقضی شد لطفاً تحت هیچ شرایطی اقدام به پرداخت آن نکنید. مسئولیت هرگونه پرداخت بر عهده کاربر خواهد بود.");
                        str.AppendLine("");
                        str.AppendLine("");
                        str.AppendLine("🚀 @" + BotSetting.Bot_ID);

                        var delivered = await TelegramNotifyHelper.TrySendMessageAsync(
                            botClient,
                            item.tbTelegramUsers.Tel_UniqUserID,
                            str.ToString(),
                            ParseMode.Html,
                            replyParameters: item.dw_message_id);

                        if (delivered)
                            item.dw_Alerted = true;

                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // مدیریت خطا، مانند لاگ کردن و اطلاع‌رسانی به کاربر
                    // مثلا:
                    foreach (var entry in ex.Entries)
                    {
                        if (entry.State == EntityState.Deleted)
                        {
                            // داده ممکن است توسط فرآیند دیگری حذف شده باشد
                            // اقدامات مناسب مانند نمایش پیام خطا
                        }
                        else if (entry.State == EntityState.Modified)
                        {
                            // داده ممکن است توسط فرآیند دیگری تغییر کرده باشد
                            // بازخوانی داده‌ها و تصمیم‌گیری برای ادامه کار
                            entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                        }
                    }

                }
                catch (Exception ex)
                {
                }

            }


            var TetraPayFactores = db.tbDepositWallet_Log.Where(s => s.dw_CreateDatetime <= DateNow_TetraPay && s.dw_Status == "FOR_PAY" && s.tbPaymentMethods.tbpm_Key == "TetraPay" && s.dw_Alerted == false).ToList();
            foreach (var item in TetraPayFactores)
            {
                try
                {

                    if (item.tbTelegramUsers.tbUsers.tbBotSettings.Count() != 0)
                    {
                        var BotSetting = item.tbTelegramUsers.tbUsers.tbBotSettings.ToList()[0];

                        var botClient = new TelegramBotClient(BotSetting.Bot_Token);

                        StringBuilder str = new StringBuilder();
                        str.Append("❌ تراکنش با مبلغ " + item.dw_Price.Value.ConvertToMony() + " ریال منقضی شد لطفاً تحت هیچ شرایطی اقدام به پرداخت آن نکنید. مسئولیت هرگونه پرداخت بر عهده کاربر خواهد بود.");
                        str.AppendLine("");
                        str.AppendLine("");
                        str.AppendLine("🚀 @" + BotSetting.Bot_ID);

                        var delivered = await TelegramNotifyHelper.TrySendMessageAsync(
                            botClient,
                            item.tbTelegramUsers.Tel_UniqUserID,
                            str.ToString(),
                            ParseMode.Html,
                            replyParameters: item.dw_message_id);

                        if (delivered)
                            item.dw_Alerted = true;

                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // مدیریت خطا، مانند لاگ کردن و اطلاع‌رسانی به کاربر
                    // مثلا:
                    foreach (var entry in ex.Entries)
                    {
                        if (entry.State == EntityState.Deleted)
                        {
                            // داده ممکن است توسط فرآیند دیگری حذف شده باشد
                            // اقدامات مناسب مانند نمایش پیام خطا
                        }
                        else if (entry.State == EntityState.Modified)
                        {
                            // داده ممکن است توسط فرآیند دیگری تغییر کرده باشد
                            // بازخوانی داده‌ها و تصمیم‌گیری برای ادامه کار
                            entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                        }
                    }

                }
                catch (Exception ex)
                {
                }

                
            }

            db.SaveChanges();

        }


    }


    #endregion

    #region  پاک کردن فاکتور های کارت به کارت
    public async Task RemoveExpireFactores()
    {
        try
        {
            await RemoveExpireFactoresCore();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "خطا در تایمر حذف فاکتورهای منقضی");
        }
    }

    private async Task RemoveExpireFactoresCore()
    {
        var deleteBefore = DateTime.Now.AddHours(-BotInvoiceGuard.InvoiceDeleteHours);

        using (Entities db = new Entities())
        {
            // فاکتورهای اپلیکیشن هم اینجا پاک می شوند ؛ مسیر هشدار تلگرامی بالاتر
            // فقط CardToCard را می بیند ، پس برای این فاکتورها پیامی فرستاده نمی شود.
            var CardToCardFactores = db.tbDepositWallet_Log
                .Where(s => s.dw_CreateDatetime <= deleteBefore
                    && s.dw_Status == "FOR_PAY"
                    && (s.tbPaymentMethods.tbpm_Key == "CardToCard"
                        || s.tbPaymentMethods.tbpm_Key == PaymentMethodIds.AppKey))
                .ToList();
            foreach (var item in CardToCardFactores)
            {
                try
                {
                    if (item.FK_Order_ID.HasValue)
                    {
                        var order = db.tbOrders.FirstOrDefault(o =>
                            o.Order_ID == item.FK_Order_ID.Value && o.OrderStatus == "FOR_PAY");
                        if (order != null)
                            db.tbOrders.Remove(order);
                    }
                    db.tbDepositWallet_Log.Remove(item);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // مدیریت خطا، مانند لاگ کردن و اطلاع‌رسانی به کاربر
                    // مثلا:
                    foreach (var entry in ex.Entries)
                    {
                        if (entry.State == EntityState.Deleted)
                        {
                            // داده ممکن است توسط فرآیند دیگری حذف شده باشد
                            // اقدامات مناسب مانند نمایش پیام خطا
                        }
                        else if (entry.State == EntityState.Modified)
                        {
                            // داده ممکن است توسط فرآیند دیگری تغییر کرده باشد
                            // بازخوانی داده‌ها و تصمیم‌گیری برای ادامه کار
                            entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                        }
                    }

                }
                catch (Exception ex)
                {
                }

            }


            var TetraPayFactores = db.tbDepositWallet_Log
                .Where(s => s.dw_CreateDatetime <= deleteBefore
                    && s.dw_Status == "FOR_PAY"
                    && s.tbPaymentMethods.tbpm_Key == "TetraPay")
                .ToList();
            foreach (var item in TetraPayFactores)
            {
                try
                {
                    if (item.FK_Order_ID.HasValue)
                    {
                        var order = db.tbOrders.FirstOrDefault(o =>
                            o.Order_ID == item.FK_Order_ID.Value && o.OrderStatus == "FOR_PAY");
                        if (order != null)
                            db.tbOrders.Remove(order);
                    }
                    db.tbDepositWallet_Log.Remove(item);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // مدیریت خطا، مانند لاگ کردن و اطلاع‌رسانی به کاربر
                    // مثلا:
                    foreach (var entry in ex.Entries)
                    {
                        if (entry.State == EntityState.Deleted)
                        {
                            // داده ممکن است توسط فرآیند دیگری حذف شده باشد
                            // اقدامات مناسب مانند نمایش پیام خطا
                        }
                        else if (entry.State == EntityState.Modified)
                        {
                            // داده ممکن است توسط فرآیند دیگری تغییر کرده باشد
                            // بازخوانی داده‌ها و تصمیم‌گیری برای ادامه کار
                            entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                        }
                    }

                }
                catch (Exception ex)
                {
                }


            }

            db.SaveChanges();

        }


    }


    #endregion

    #endregion
}
