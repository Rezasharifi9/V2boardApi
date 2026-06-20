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
    private tbServers Server;
    public TimerService()
    {
        // تنظیم تایمرها
        CheckLink = new System.Threading.Timer(async _ => await CheckSubTimerCallback(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(900000));
        AlertDeleteFactoresCard = new System.Threading.Timer(async _ => await CheckExpireFactores(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(3600000));
        CheckRenewAccount = new System.Threading.Timer(async _ => await CheckRenewAccountFun(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(60000));
        //CheckSubLimitedUser = new System.Threading.Timer(async _ => await CheckSubLimitedUsers(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(180000));
        DeleteTestAccount = new System.Threading.Timer(async _ => await DeleteTestSub(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(86400000));
        RemoveFactoresCard = new System.Threading.Timer(async _ => await RemoveExpireFactores(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(86400000));
        CheckSettlement = new System.Threading.Timer(async _ => await SettlementService.ProcessAllAgents(), null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
        Server = HttpRuntime.Cache["Server"] as tbServers;
    }


    #region چک کردن کاربری که حجمش نزدیک به اتمام برای اطلاع دادن 
    private async Task CheckSubTimerCallback()
    {
        try
        {

            if (Server != null)
            {
                using (Entities db = new Entities())
                {
                    var tbTelegramUserRepository = new Repository<tbTelegramUsers>(db);
                    var Users = await tbTelegramUserRepository.GetAllAsync();
                    var tbOrdersRepository = new Repository<tbOrders>(db);

                    MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);
                    await mySql.OpenAsync();
                    foreach (var item in Users.ToList())
                    {
                        try
                        {
                            if (item.tbUsers != null)
                            {
                                if (item.tbUsers.tbBotSettings != null)
                                {
                                    var BotSetting = item.tbUsers.tbBotSettings.FirstOrDefault();
                                    if (BotSetting != null)
                                    {
                                        if (BotSetting.Bot_Token != null && BotSetting.Enabled == true)
                                        {
                                            var bot = BotManager.GetBot(item.tbUsers.Username);
                                            if (bot != null)
                                            {
                                                foreach (var link in item.tbLinks.Where(p => p.tbL_Warning == false && p.tb_AutoRenew == false).ToList())
                                                {
                                                    var Order = await tbOrdersRepository.WhereAsync(p => p.AccountName == link.tbL_Email && p.OrderStatus == "FOR_RESERVE");
                                                    if (Order.Count == 0)
                                                    {
                                                        var GetDataQuery = "select u,d,transfer_enable,banned,expired_at from v2_user where email=@email";
                                                        Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
                                                        keyValuePairs.Add("@email", link.tbL_Email);
                                                        using (var reader = await mySql.GetDataAsync(GetDataQuery, keyValuePairs))
                                                        {
                                                            while (await reader.ReadAsync())
                                                            {
                                                                var bannd = reader.GetBoolean("banned");
                                                                if (!bannd)
                                                                {
                                                                    var vol = reader.GetInt64("transfer_enable") - (reader.GetInt64("d") + reader.GetInt64("u"));

                                                                    var d = Utility.ConvertByteToGB(vol);

                                                                    if (d <= 0.2)
                                                                    {
                                                                        StringBuilder st = new StringBuilder();
                                                                        if (link.tbL_Email.Split('@')[0].Contains('$'))
                                                                        {
                                                                            st.AppendLine("<b>" + "اشتراک : " + link.tbL_Email.Split('@')[0].Split('$')[0] + "</b>");
                                                                        }
                                                                        else
                                                                        {
                                                                            st.AppendLine("<b>" + "اشتراک : " + link.tbL_Email.Split('@')[0] + "</b>");
                                                                        }
                                                                        st.AppendLine("");
                                                                        st.Append("درحال اتمام حجم بسته می باشد لطفا هرچه سریعتر نسبت به تمدید اقدام کنید");
                                                                        st.AppendLine("");
                                                                        st.AppendLine("〰️〰️〰️〰️〰️");
                                                                        st.AppendLine("🚀@" + BotSetting.Bot_ID);

                                                                        try
                                                                        {
                                                                            await bot.Client.SendMessage(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
                                                                        }
                                                                        catch
                                                                        {
                                                                            continue;
                                                                        }
                                                                        await bot.Client.SendMessage(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
                                                                        link.tbL_Warning = true;
                                                                        await tbTelegramUserRepository.SaveChangesAsync();
                                                                    }

                                                                    var exp = reader["expired_at"]?.ToString();
                                                                    if (exp != "")
                                                                    {
                                                                        var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp));
                                                                        if (ex <= DateTime.Now.AddDays(-2) && ex >= DateTime.Now.AddDays(31))
                                                                        {
                                                                            StringBuilder st = new StringBuilder();
                                                                            if (link.tbL_Email.Split('@')[0].Contains('$'))
                                                                            {
                                                                                st.AppendLine("<b>" + "اشتراک : " + link.tbL_Email.Split('@')[0].Split('$')[0] + "</b>");
                                                                            }
                                                                            else
                                                                            {
                                                                                st.AppendLine("<b>" + "اشتراک : " + link.tbL_Email.Split('@')[0] + "</b>");
                                                                            }
                                                                            st.AppendLine("");
                                                                            st.AppendLine(" درحال اتمام زمان بسته می باشد لطفا هرچه سریعتر نسبت به تمدید اقدام کنید");
                                                                            st.AppendLine("");
                                                                            st.AppendLine("〰️〰️〰️〰️〰️");
                                                                            st.AppendLine("🚀@" + BotSetting.Bot_ID);
                                                                            try
                                                                            {
                                                                                await bot.Client.SendMessage(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
                                                                            }
                                                                            catch
                                                                            {
                                                                                continue;
                                                                            }
                                                                            link.tbL_Warning = true;
                                                                            await tbTelegramUserRepository.SaveChangesAsync();
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    StringBuilder st = new StringBuilder();
                                                                    if (link.tbL_Email.Split('@')[0].Contains('$'))
                                                                    {
                                                                        st.AppendLine("<b>" + "اشتراک : " + link.tbL_Email.Split('@')[0].Split('$')[0] + "</b>");
                                                                    }
                                                                    else
                                                                    {
                                                                        st.AppendLine("<b>" + "اشتراک : " + link.tbL_Email.Split('@')[0] + "</b>");
                                                                    }

                                                                    st.AppendLine("");
                                                                    st.AppendLine("توسط ادمین مسدود شد برای دانستن علت مسدودی به پشتیبانی پیام دهید");
                                                                    st.AppendLine("");
                                                                    st.AppendLine("〰️〰️〰️〰️〰️");
                                                                    st.AppendLine("🚀@" + BotSetting.Bot_ID);
                                                                    try
                                                                    {
                                                                        await bot.Client.SendMessage(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
                                                                    }
                                                                    catch
                                                                    {
                                                                        continue;
                                                                    }
                                                                    link.tbL_Warning = true;
                                                                    await tbTelegramUserRepository.SaveChangesAsync();
                                                                }

                                                            }
                                                        }

                                                    }



                                                }

                                            }
                                        }

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
        using (Entities db = new Entities())
        {
            var BotSettingRepository = new Repository<tbBotSettings>(db);
            var botsetting = await BotSettingRepository
            .WhereAsync(p => p.Enabled == true && p.Bot_Token != null)
            .ConfigureAwait(false);


            foreach (var item in botsetting)
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
        }

    }


    #endregion

    #region تایمر رزرو تمدید اکانت
    public async Task CheckRenewAccountFun()
    {
        try
        {
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
                                        await bot.Client.SendMessage(link.tbTelegramUsers.Tel_UniqUserID,
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
        var DateNow_CardToCard = DateTime.Now.AddHours(-24);
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

                        await botClient.SendMessage(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html,replyParameters: item.dw_message_id);

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

                        await botClient.SendMessage(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyParameters: item.dw_message_id);

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
        var DateNow = DateTime.Now.AddHours(-72);


        using (Entities db = new Entities())
        {
            var CardToCardFactores = db.tbDepositWallet_Log.Where(s => s.dw_CreateDatetime <= DateNow && s.dw_Status == "FOR_PAY" && s.tbPaymentMethods.tbpm_Key == "CardToCard" && s.dw_Alerted == false).ToList();
            foreach (var item in CardToCardFactores)
            {
                try
                {
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


            var TetraPayFactores = db.tbDepositWallet_Log.Where(s => s.dw_CreateDatetime <= DateNow && s.dw_Status == "FOR_PAY" && s.tbPaymentMethods.tbpm_Key == "TetraPay" && s.dw_Alerted == false).ToList();
            foreach (var item in TetraPayFactores)
            {
                try
                {

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
