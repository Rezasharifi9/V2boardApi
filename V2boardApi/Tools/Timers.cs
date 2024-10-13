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

public class TimerService
{
    //ngrok http 4480 --host-header="localhost:4480"
    private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private System.Threading.Timer CheckLink;
    private System.Threading.Timer CheckRenewAccount;
    private System.Threading.Timer DeleteTestAccount;
    private System.Threading.Timer DeleteFactores;
    private tbServers Server;
    public TimerService()
    {
        // تنظیم تایمرها
        CheckLink = new System.Threading.Timer(async _ => await CheckSubTimerCallback(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(900000));
        DeleteFactores = new System.Threading.Timer(async _ => await CheckExpireFactores(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(3600000));
        CheckRenewAccount = new System.Threading.Timer(async _ => await CheckRenewAccountFun(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(300000));
        DeleteTestAccount = new System.Threading.Timer(async _ => await DeleteTestSub(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(86400000));
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
                                        if (BotSetting.Bot_Token != null && BotSetting.Active == true)
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
                                                                    var vol = reader.GetInt64("transfer_enable") - (reader.GetDouble("d") + reader.GetDouble("u"));

                                                                    var d = Utility.ConvertByteToGB(vol);

                                                                    if (d <= 1)
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
                                                                            await bot.Client.SendTextMessageAsync(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
                                                                        }
                                                                        catch
                                                                        {
                                                                            continue;
                                                                        }
                                                                        await bot.Client.SendTextMessageAsync(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
                                                                        link.tbL_Warning = true;
                                                                        await tbTelegramUserRepository.SaveChangesAsync();
                                                                    }

                                                                    var exp = reader.GetBodyDefinition("expired_at");
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
                                                                                await bot.Client.SendTextMessageAsync(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
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
                                                                        await bot.Client.SendTextMessageAsync(item.Tel_UniqUserID, st.ToString(), parseMode: ParseMode.Html);
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
            .WhereAsync(p => p.Active == true && p.Bot_Token != null)
            .ConfigureAwait(false);


            foreach (var item in botsetting)
            {
                using (MySqlEntities mySql = new MySqlEntities(Server.ConnectionString))
                {
                    await mySql.OpenAsync().ConfigureAwait(false);

                    string query = "DELETE FROM v2_user WHERE ((v2_user.d + v2_user.u) > v2_user.transfer_enable OR expired_at < UNIX_TIMESTAMP()) AND email LIKE @BotID";

                    using (var command = new MySqlCommand(query, mySql.MySqlConnection))
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
                    var tbTelegramUserRepository = new Repository<tbTelegramUsers>(db);
                    var tbLinksRepository = new Repository<tbLinks>(db);
                    var tbUsersRepository = new Repository<tbUsers>(db);
                    var tbPlanRepository = new Repository<tbPlans>(db);
                    MySqlEntities mySql = new MySqlEntities(Server.ConnectionString);

                    await mySql.OpenAsync();
                    foreach (var item in Order)
                    {
                        var bot = BotManager.GetBot(item.tbTelegramUsers.tbUsers.Username);
                        if (bot != null)
                        {
                            var Disc = new Dictionary<string, object>();
                            Disc.Add("@email", item.AccountName);
                            var Reader = await mySql.GetDataAsync("select * from v2_user where email like @email", Disc);
                            var Read = await Reader.ReadAsync();
                            if (Read)
                            {
                                var d = Reader.GetDouble("d");
                                var u = Reader.GetDouble("u");
                                var totalUsed = Reader.GetDouble("transfer_enable");

                                var total = Math.Round(Utility.ConvertByteToGB(totalUsed - (d + u)), 2);
                                var exp2 = Reader.GetBodyDefinition("expired_at");
                                var Ended = false;
                                if (!string.IsNullOrWhiteSpace(exp2))
                                {
                                    var expireTime = DateTime.Now;
                                    var ExpireDate = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp2)).AddHours(-3);
                                    if (ExpireDate <= expireTime)
                                    {
                                        Ended = true;
                                    }
                                }
                                if (total <= 0.2)
                                {
                                    Ended = true;
                                }
                                Reader.Close();
                                if (Ended)
                                {


                                    var Link = item.tbTelegramUsers.tbLinks.Where(p => p.tbL_Email == item.AccountName).FirstOrDefault();
                                    if (Link != null)
                                    {
                                        var t = Utility.ConvertGBToByte(Convert.ToInt64(item.Traffic));

                                        string exp = DateTime.Now.AddDays((int)item.Month * 30).ConvertDatetimeToSecond().ToString();
                                        Link.tbL_Warning = false;


                                        var Disc1 = new Dictionary<string, object>();
                                        Disc1.Add("@plan_id", item.V2_Plan_ID);
                                        Disc1.Add("@transfer_enable", t);
                                        Disc1.Add("@expired_at", exp);
                                        Disc1.Add("@email", item.AccountName);

                                        var reader2 = await mySql.GetDataAsync("select group_id from v2_plan where id ="+item.V2_Plan_ID);
                                        while (await reader2.ReadAsync())
                                        {
                                            var grid = reader2.GetInt32("group_id");

                                            Disc1.Add("@group_id", grid);
                                        }

                                        var DeviceLimit_Structur = "";
                                        var DeviceLimit_data = "";
                                        var planId = item.tbTelegramUsers.tbUsers.tbBotSettings.First().FK_Plan_ID;
                                        var Plan = tbPlanRepository.Where(s => s.Plan_ID == planId).FirstOrDefault();
                                        if (Plan.device_limit != null)
                                        {
                                            DeviceLimit_Structur = ",device_limit=" + Plan.device_limit+1;
                                            //Disc1.Add("@device_limit", Plan.device_limit);
                                        }
                                        

                                        var Query = "update v2_user set u=0,d=0,t=0,plan_id=@plan_id"+ DeviceLimit_Structur + ",group_id=@group_id,transfer_enable=@transfer_enable,expired_at=@expired_at where email=@email";

                                        var reader = await mySql.GetDataAsync(Query, Disc1);
                                        var result = reader.ReadAsync();
                                        reader.Close();



                                        await bot.Client.SendTextMessageAsync(Link.tbTelegramUsers.Tel_UniqUserID, "✅ اکانت شما با موفقیت تمدید شد از بخش سرویس ها جزئیات اکانت را می توانید مشاهده کنید");
                                        var InlineKeyboardMarkup = Keyboards.GetHomeButton();
                                        Link.tbL_Warning = false;
                                        Link.tb_AutoRenew = false;
                                        item.OrderStatus = "FINISH";
                                        item.Tel_RenewedDate = DateTime.Now;
                                        await tbLinksRepository.SaveChangesAsync();
                                        await tbUsersRepository.SaveChangesAsync();
                                        await tbOrdersRepository.SaveChangesAsync();
                                        await tbTelegramUserRepository.SaveChangesAsync();

                                    }
                                }
                            }

                            Reader.Close();
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

    #region پاک کردن فاکتور های منقضی شده
    public async Task CheckExpireFactores()
    {
        var DateNow = DateTime.Now.AddHours(-24);

        using (Entities db = new Entities())
        {
            var Factores = db.tbDepositWallet_Log.Where(s => s.dw_CreateDatetime <= DateNow && s.dw_Status == "FOR_PAY").ToList();
            foreach (var item in Factores)
            {
                try
                {

                    if (item.tbTelegramUsers.tbUsers.tbBotSettings.Where(s => s.Active == true && s.Enabled == true && s.IsActiveCardToCard == true).Count() != 0)
                    {
                        var BotSetting = item.tbTelegramUsers.tbUsers.tbBotSettings.ToList()[0];

                        var botClient = new TelegramBotClient(BotSetting.Bot_Token);

                        StringBuilder str = new StringBuilder();
                        str.Append("❌ فاکتور با مبلغ " + item.dw_Price.Value.ConvertToMony() + " ریال منقضی شد به هیچ عنوان این فاکتور را پرداخت نکنید");
                        str.AppendLine("");
                        str.AppendLine("");
                        str.AppendLine("🚀 @" + BotSetting.Bot_ID);

                        if (item.dw_message_id != null)
                        {
                            await botClient.SendTextMessageAsync(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyToMessageId: item.dw_message_id);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html);
                        }

                        db.tbDepositWallet_Log.Remove(item);

                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    db.SaveChanges();
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
                    db.tbDepositWallet_Log.Remove(item);
                }
            }
            db.SaveChanges();
        }


    }


    #endregion
}
