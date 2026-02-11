using DataLayer.DomainModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using V2boardBot.Functions;
using V2boardBot.Models;
using DataLayer.Repository;
using System.Timers;
using NLog;

namespace V2boardApi.Tools
{
    public class TransactionHanderService
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbPlans> RepositoryPlan { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbOrders> RepositoryOrder { get; set; }
        private Repository<tbLinkUserAndPlans> RepositoryLinkUserAndPlan { get; set; }
        private Repository<tbLinks> RepositoryLinks { get; set; }
        private Repository<tbDepositWallet_Log> RepositoryDepositWallet { get; set; }
        private Repository<tbTelegramUsers> RepositoryTelegramUser { get; set; }
        private Repository<tbUserFactors> RepositoryFactor { get; set; }
        private Repository<tbServerGroups> RepositoryServerGroups { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public TransactionHanderService()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryPlan = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
            RepositoryOrder = new Repository<tbOrders>();
            RepositoryLinks = new Repository<tbLinks>();
            RepositoryDepositWallet = new Repository<tbDepositWallet_Log>(db);
            RepositoryTelegramUser = new Repository<tbTelegramUsers>(db);
            RepositoryFactor = new Repository<tbUserFactors>(db);
            RepositoryServerGroups = new Repository<tbServerGroups>(db);
            Timer = new System.Timers.Timer();
            Timer.Elapsed += Timer_Elapsed;

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> CheckOrder(string SMSMessageText, string Mobile)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var Phone = JsonConvert.DeserializeObject(Mobile);
                    var User = await RepositoryUser.FirstOrDefaultAsync(p => p.PhoneNumber == Phone.ToString());
                    if (User != null)
                    {
                        int pr = int.Parse(SMSMessageText, NumberStyles.Currency);



                        var tbDepositLog = await RepositoryDepositWallet.WhereAsync(p => p.dw_Price == pr && p.dw_Status == "FOR_PAY" && p.dw_PayMethod == "Card");
                        var botSetting = User.tbBotSettings.FirstOrDefault();
                        TelegramBotClient botClient = new TelegramBotClient(botSetting.Bot_Token);

                        foreach (var item in tbDepositLog)
                        {
                            if (botSetting != null)
                            {

                                if (botSetting.InvitePercent != null)
                                {
                                    if (item.tbTelegramUsers.Tel_Parent_ID != null)
                                    {
                                        var parent = item.tbTelegramUsers.tbTelegramUsers2;
                                        parent.Tel_Wallet += Convert.ToInt32((item.dw_Price / 10) * botSetting.InvitePercent.Value);


                                        await RepositoryDepositWallet.SaveChangesAsync();

                                        StringBuilder str1 = new StringBuilder();
                                        str1.AppendLine("☺️ کاربر گرامی، به دلیل خرید دوستتان، ‌" + botSetting.InvitePercent * 100 + " درصد از مبلغ خرید ایشان به کیف پول شما اضافه شد. از حمایت شما سپاسگزاریم 🙏🏻");
                                        str1.AppendLine("");
                                        str1.AppendLine("💰 موجودی فعلی کیف پول شما: " + parent.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                        str1.AppendLine("");
                                        str1.AppendLine("🚀 @" + botSetting.Bot_ID);

                                        await botClient.SendTextMessageAsync(parent.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html);
                                    }
                                }




                            }

                            item.dw_Status = "FINISH";
                            if (item.FK_Order_ID != null)
                            {
                                var Link = await RepositoryLinks.FirstOrDefaultAsync(p => p.tbL_Email == item.tbOrders.AccountName);
                                if (Link != null)
                                {
                                    MySqlEntities mySql = new MySqlEntities(item.tbTelegramUsers.tbUsers.tbServers.ConnectionString);

                                    await mySql.OpenAsync();

                                    var Disc1 = new Dictionary<string, object>();
                                    Disc1.Add("@tbL_Email", Link.tbL_Email);


                                    var Reader = await mySql.GetDataAsync("select * from v2_user where email = @tbL_Email", Disc1);
                                    var Ended = false;
                                    while (await Reader.ReadAsync())
                                    {
                                        var d = Reader.GetDouble("d");
                                        var u = Reader.GetDouble("u");
                                        var totalUsed = Reader.GetDouble("transfer_enable");

                                        var total = Math.Round(Utility.ConvertByteToGB(totalUsed - (d + u)), 2);
                                        var exp2 = Reader.GetBodyDefinition("expired_at");

                                        if (!string.IsNullOrWhiteSpace(exp2))
                                        {
                                            var expireTime = DateTime.Now.AddHours(-2);
                                            var ExpireDate = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp2));
                                            if (expireTime > ExpireDate)
                                            {
                                                Ended = true;
                                            }
                                        }
                                        if (total <= 0.2)
                                        {
                                            Ended = true;
                                        }
                                    }
                                    Reader.Close();

                                    if (Ended)
                                    {
                                        tbOrders order = item.tbOrders;

                                        var t = Utility.ConvertGBToByte(Convert.ToInt64(order.Traffic));

                                        string exp = DateTime.Now.AddDays((int)(order.Month * 30)).ConvertDatetimeToSecond().ToString();

                                        Link.tbL_Warning = false;
                                        var Disc3 = new Dictionary<string, object>();
                                        Disc3.Add("@DefaultPlanIdInV2board", order.tbLinkUserAndPlans.tbPlans.Plan_ID_V2);
                                        Disc3.Add("@transfer_enable", t);
                                        Disc3.Add("@exp", exp);
                                        Disc3.Add("@email", Link.tbL_Email);
                                        var group = RepositoryServerGroups.Where(s => s.Group_Id == order.tbLinkUserAndPlans.tbPlans.Group_Id).First();
                                        Disc3.Add("@group_id", group.V2_Group_Id);
                                        var DeviceLimit_Structur = "";
                                        if (order.tbLinkUserAndPlans.tbPlans.device_limit != null)
                                        {
                                            Disc3.Add("@device_limit", order.tbLinkUserAndPlans.tbPlans.device_limit);

                                        }

                                        var Query = "update v2_user set u=0,d=0,t=0,plan_id=@DefaultPlanIdInV2board,transfer_enable=@transfer_enable,expired_at=@exp,device_limit=@device_limit,group_id=@group_id where email=@email";
                                        var reader = await mySql.GetDataAsync(Query, Disc3);
                                        var result = await reader.ReadAsync();
                                        reader.Close();



                                        var InlineKeyboardMarkup = Keyboards.GetHomeButton();

                                        Link.tbL_Warning = false;
                                        Link.tb_AutoRenew = false;

                                        order.OrderStatus = "FINISH";


                                        await mySql.CloseAsync();
                                        await RepositoryOrder.SaveChangesAsync();
                                        await RepositoryDepositWallet.SaveChangesAsync();
                                        transaction.Commit();

                                        StringBuilder str2 = new StringBuilder();
                                        str2.AppendLine("✅ بسته تو تمدید کردم");
                                        str2.AppendLine("");
                                        str2.AppendLine("♨️ میتونی توی بخش مدیریت اشتراک ها ببینی که تمدید شده");
                                        await RealUser.SetEmptyState(order.tbTelegramUsers.Tel_UniqUserID, db, order.tbTelegramUsers.tbUsers.Username);
                                        var kyes = Keyboards.GetHomeButton();
                                        await botClient.SendTextMessageAsync(order.tbTelegramUsers.Tel_UniqUserID, str2.ToString(), parseMode: ParseMode.Html, replyMarkup: kyes);

                                        return true;

                                    }
                                    else
                                    {
                                        tbOrders order = item.tbOrders;

                                        order.OrderStatus = "FOR_RESERVE";


                                        await mySql.CloseAsync();
                                        await RepositoryOrder.SaveChangesAsync();
                                        await RepositoryDepositWallet.SaveChangesAsync();
                                        transaction.Commit();


                                        StringBuilder str2 = new StringBuilder();
                                        str2.AppendLine("✅ چون هنوز حجم یا زمان داشتی بسته تو رزرو کردم");
                                        str2.AppendLine("");
                                        str2.AppendLine("♨️ بعد از اینکه بسته فعلیت تموم بشه خودم جایگزین میکنم تو نگران نباش و به کارت برس");
                                        str2.AppendLine("");
                                        await RealUser.SetEmptyState(order.tbTelegramUsers.Tel_UniqUserID, db, order.tbTelegramUsers.tbUsers.Username);
                                        var kyes = Keyboards.GetHomeButton();
                                        await botClient.SendTextMessageAsync(order.tbTelegramUsers.Tel_UniqUserID, str2.ToString(), parseMode: ParseMode.Html, replyMarkup: kyes);

                                        return true;
                                    }


                                }
                                else
                                {
                                    tbOrders Order = item.tbOrders;

                                    Order.OrderStatus = "FINISH";
                                    Order.Tel_RenewedDate = DateTime.Now;
                                    string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];

                                    var FullName = Order.AccountName;

                                    var t = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));

                                    string exp = DateTime.Now.AddDays((int)(Order.Month * 30)).ConvertDatetimeToSecond().ToString();

                                    MySqlEntities mySql = new MySqlEntities(Order.tbTelegramUsers.tbUsers.tbServers.ConnectionString);
                                    await mySql.OpenAsync();
                                    var Disc1 = new Dictionary<string, object>();
                                    Disc1.Add("@V2board", Order.tbLinkUserAndPlans.tbPlans.Plan_ID_V2);
                                    var reader = await mySql.GetDataAsync("select group_id,transfer_enable from v2_plan where id =@V2board", Disc1);
                                    long tran = 0;
                                    int grid = 0;
                                    while (await reader.ReadAsync())
                                    {
                                        tran = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));
                                        grid = reader.GetInt32("group_id");
                                    }
                                    string create = DateTime.Now.ConvertDatetimeToSecond().ToString();

                                    var Disc3 = new Dictionary<string, object>();
                                    Disc3.Add("@FullName", FullName);
                                    Disc3.Add("@expired", exp);
                                    Disc3.Add("@create", create);
                                    Disc3.Add("@guid", Guid.NewGuid());
                                    Disc3.Add("@tran", tran);
                                    Disc3.Add("@grid", grid);
                                    Disc3.Add("@V2boardId", Order.tbLinkUserAndPlans.tbPlans.Plan_ID_V2);
                                    Disc3.Add("@token", token);
                                    Disc3.Add("@passwrd", Guid.NewGuid());

                                    var DeviceLimit_Structur = "";
                                    var DeviceLimit_data = "";

                                    if (Order.tbLinkUserAndPlans.tbPlans.device_limit != null)
                                    {
                                        DeviceLimit_Structur = ",device_limit";
                                        Disc3.Add("@device_limit", Order.tbLinkUserAndPlans.tbPlans.device_limit);
                                        DeviceLimit_data = ",@device_limit";
                                    }



                                    string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at" + DeviceLimit_Structur + ") VALUES (@FullName,@expired,@create,@guid,0,0,0,@tran,0,@grid,@V2boardId,@token,@passwrd,@create" + DeviceLimit_data + ")";
                                    reader.Close();

                                    reader = await mySql.GetDataAsync(Query, Disc3);
                                    reader.Close();

                                    StringBuilder st = new StringBuilder();
                                    st.AppendLine("📈 <strong>لینک اشتراکت  : </strong>");
                                    st.AppendLine("👇👇👇👇👇👇👇");
                                    st.AppendLine("");
                                    var SubLink = "https://" + Order.tbTelegramUsers.tbUsers.tbServers.SubAddress + "/api/v1/client/subscribe?token=" + token;
                                    st.AppendLine("<code>" + SubLink + "</code>");
                                    st.AppendLine("");

                                    st.AppendLine("◀️ روی لینک کلیک کنی خودش کپی میشه و میتونی توی نرم افزار اضافه اش کنی");
                                    st.AppendLine("");
                                    st.AppendLine("◀️ جزئیات اشتراک رو میتونی داخل بخش مدیریت اشتراک ها ببینی");
                                    st.AppendLine("");
                                    st.AppendLine("◀️ در ضمن روی گزینه زیر کلیک کنی میتونی طبق راهنما به اشتراکت وصل بشی و لذتشو ببری 👇");
                                    st.AppendLine("");

                                    var image = InputFile.FromStream(new MemoryStream(Utility.GenerateQRCode(SubLink)));

                                    tbLinks tbLinks = new tbLinks();
                                    tbLinks.tbL_Email = Order.AccountName;
                                    tbLinks.tbL_Email = FullName;
                                    tbLinks.tbL_Token = token;
                                    tbLinks.FK_Server_ID = Order.tbTelegramUsers.tbUsers.tbServers.ServerID;
                                    tbLinks.FK_TelegramUserID = Order.tbTelegramUsers.Tel_UserID;
                                    tbLinks.tbL_Warning = false;
                                    tbLinks.tb_AutoRenew = false;
                                    await mySql.CloseAsync();



                                    List<List<InlineKeyboardButton>> inlineKeyboards = new List<List<InlineKeyboardButton>>();

                                    List<InlineKeyboardButton> row2 = new List<InlineKeyboardButton>();
                                    InlineKeyboardButton inlineKeyboard = new InlineKeyboardButton("🔗 اتصال به اشتراک");
                                    WebAppInfo appInfo = new WebAppInfo();
                                    appInfo.Url = SubLink;
                                    inlineKeyboard.WebApp = appInfo;
                                    row2.Add(inlineKeyboard);
                                    inlineKeyboards.Add(row2);
                                    var keyboard = new InlineKeyboardMarkup(inlineKeyboards);



                                    RepositoryLinks.Insert(tbLinks);


                                    await RepositoryOrder.SaveChangesAsync();

                                    await RepositoryTelegramUser.SaveChangesAsync();
                                    await RepositoryLinks.SaveChangesAsync();
                                    await RepositoryLinkUserAndPlan.SaveChangesAsync();
                                    transaction.Commit();

                                    var keys = Keyboards.GetHomeButton();

                                    //await botClient.SendTextMessageAsync(UserAcc.Tel_UniqUserID, "✅ اکانت شما با موفقیت ایجاد شد", replyMarkup: keys);

                                    await botClient.SendTextMessageAsync(Order.tbTelegramUsers.Tel_UniqUserID, "✅ اکانت شما با موفقیت ایجاد شد");


                                    await botClient.SendPhotoAsync(
                                      chatId: Order.tbTelegramUsers.Tel_UniqUserID,
                                      photo: image,
                                      caption: st.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);

                                    await botClient.SendTextMessageAsync(Order.tbTelegramUsers.Tel_UniqUserID, "به منو اصلی بازگشتید 🏘", parseMode: ParseMode.Html, replyMarkup: keys);
                                    await RealUser.SetEmptyState(Order.tbTelegramUsers.Tel_UniqUserID, db, Order.tbTelegramUsers.tbUsers.Username);

                                    return true;
                                }
                            }
                            else
                            {


                                StringBuilder str = new StringBuilder();
                                str.AppendLine("✅ کیف پولتو شارژ کردم");
                                str.AppendLine("");
                                str.AppendLine("💰 موجودی الانت : " + item.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                str.AppendLine("");
                                str.AppendLine("🔔 خب حالا برو اشتراکتو تمدید کن یا اشتراک جدید بخر و حالشو ببر.");
                                str.AppendLine("");
                                str.AppendLine("توجه کن اگر اشتراک داری برو تو بخش تمدید و تمدید کن وگرنه اشتراکت تموم میشه و قطع میشی");

                                item.tbTelegramUsers.Tel_Wallet += item.dw_Price / 10;
                                await RepositoryDepositWallet.SaveChangesAsync();
                                transaction.Commit();
                                var keyboard = Keyboards.GetHomeButton();

                                await RealUser.SetUserStep(item.tbTelegramUsers.Tel_UniqUserID, "Start", db, item.tbTelegramUsers.tbUsers.Username);


                                await botClient.SendTextMessageAsync(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);


                                return true;
                            }







                        }
                        return false;
                    }
                    else
                    {

                        logger.Warn("چنین شماره ای " + Mobile + " در سیستم یافت نشد");
                        return false;
                    }


                }
                catch (Exception ex)
                {
                    int pr = int.Parse(SMSMessageText, NumberStyles.Currency);
                    transaction.Rollback();
                    logger.Error(ex, "خطا در پرداخت فاکتور به مبلغ " + pr.ConvertToMony() + " رخ داد");
                    return false;
                }
            }
        }
    }
}