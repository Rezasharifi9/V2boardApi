using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.EnterpriseServices;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Timers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;
using System.Web.Management;
using System.Web.Mvc;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;
using Antlr.Runtime;
using DataLayer.DomainModel;
using DataLayer.Repository;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.X509;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using V2boardApi.Models;
using V2boardApi.Models.V2boardModel;
using V2boardApi.Tools;
using V2boardBot.Models;
using System.Threading.Tasks;
using V2boardApi.Areas.api.Data.ViewModels;
using V2boardBot.Functions;
using System.Windows;
using System.Web.WebSockets;
using Org.BouncyCastle.Crypto.Generators;
using System.Web.Security;
using YamlDotNet.Core.Tokens;
using System.Windows.Controls;
using LiteDB;
using DeviceDetectorNET.Class;
using V2boardBotApp.Models;
using NLog;
using System.Data.Entity;
using V2boardApi.Areas.api.Data.ApiModels;
using MySqlX.XDevAPI.Common;
using System.Net;

namespace V2boardApi.Areas.api.Controllers
{
    [EnableCors(origins: "*", "*", "*")]
    [LogActionFilter]
    public class UserController : ApiController
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
        private Repository<tbFirebaseMobileTokens> tbFirebaseMobileTokens { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public UserController()
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
            tbFirebaseMobileTokens = new Repository<tbFirebaseMobileTokens>();
            Timer = new System.Timers.Timer();
            Timer.Elapsed += Timer_Elapsed;

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #region لاگین مربوط به اپلیکیشن
        [System.Web.Http.HttpPost]
        public IHttpActionResult LoginAdmin(ReqLoginModel req)
        {
            try
            {
                var pass = req.password.ToSha256();
                var User = RepositoryUser.Where(p => p.Username == req.username && p.Password == pass && p.Status == true).FirstOrDefault();
                if (User != null)
                {
                    var Server = User.tbServers;

                    var ActiveBank = User.tbBankCardNumbers.Where(p => p.Active == true).FirstOrDefault();
                    var Token = (req.username + req.password).ToSha256();
                    logger.Info("ورود موفق با اپلیکیشن");
                    return Ok(new { Token = Token, phoneNumber = User.PhoneNumber, BankSmsNumbers = ActiveBank?.BankSmsNumber?.Split(',').ToList() });

                }
                else
                {
                    logger.Info("ورود ناموفق در اپلیکیشن");
                    return Content(System.Net.HttpStatusCode.NotFound, "نام کاربری یا رمز عبور اشتباه است");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ورود ناموفق در اپلیکیشن");
                return BadRequest("خطا در ارتباط با سرور");
            }
        }

        #endregion

        #region تابع برای ربات که تراکنش هارو چک میکنه
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> CheckOrder(string SMSMessageText, string Mobile)
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



                        var date2 = DateTime.Now.AddHours(-24);
                        var tbDepositLog = await RepositoryDepositWallet.WhereAsync(p => p.dw_Price == pr && p.dw_Status == "FOR_PAY" && p.dw_CreateDatetime >= date2);
                        var botSetting = User.tbBotSettings.FirstOrDefault();
                        foreach (var item in tbDepositLog)
                        {
                            item.dw_Status = "FINISH";
                            item.tbTelegramUsers.Tel_Wallet += item.dw_Price / 10;
                            StringBuilder str = new StringBuilder();
                            str.AppendLine("✅ کیف پولتو شارژ کردم");
                            str.AppendLine("");
                            str.AppendLine("💰 موجودی الانت : " + item.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                            str.AppendLine("");
                            str.AppendLine("🔔 خب حالا برو اشتراکتو تمدید کن یا اشتراک جدید بخر و حالشو ببر.");
                            str.AppendLine("");
                            str.AppendLine("توجه کن اگر اشتراک داری برو تو بخش تمدید و تمدید کن وگرنه اشتراکت تموم میشه و قطع میشی");


                            var keyboard = Keyboards.GetHomeButton();

                            await RealUser.SetUserStep(item.tbTelegramUsers.Tel_UniqUserID, "Start", db, item.tbTelegramUsers.tbUsers.Username);



                            if (botSetting != null)
                            {
                                TelegramBotClient botClient = new TelegramBotClient(botSetting.Bot_Token);
                                await RepositoryDepositWallet.SaveChangesAsync();
                                await botClient.SendTextMessageAsync(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);

                                if (botSetting.InvitePercent != null)
                                {
                                    if (item.tbTelegramUsers.Tel_Parent_ID != null)
                                    {
                                        var parent = item.tbTelegramUsers.tbTelegramUsers2;
                                        parent.Tel_Wallet += Convert.ToInt32((item.dw_Price / 10) * botSetting.InvitePercent.Value);

                                        StringBuilder str1 = new StringBuilder();
                                        str1.AppendLine("☺️ کاربر گرامی، به دلیل خرید دوستتان، ‌" + botSetting.InvitePercent * 100 + " درصد از مبلغ خرید ایشان به کیف پول شما اضافه شد. از حمایت شما سپاسگزاریم 🙏🏻");
                                        str1.AppendLine("");
                                        str1.AppendLine("💰 موجودی فعلی کیف پول شما: " + parent.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                        str1.AppendLine("");
                                        str1.AppendLine("🚀 @" + botSetting.Bot_ID);

                                        await botClient.SendTextMessageAsync(parent.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html);
                                    }
                                }

                                await RepositoryDepositWallet.SaveChangesAsync();
                                transaction.Commit();

                                logger.Info("فاکتور به مبلغ " + pr.ConvertToMony() + " با موفقیت پرداخت شد");
                                return Ok();
                            }


                        }
                        var NewPrice = pr / 10;
                        var DayAgo = DateTime.Now.AddHours(-6);
                        var tbUserFactor = await RepositoryFactor.FirstOrDefaultAsync(p => p.tbUf_Value == NewPrice && p.tbUf_CreateTime.Value >= DayAgo && p.tbUf_Status == 1);
                        if (tbUserFactor != null)
                        {
                            tbUserFactor.tbUf_Status = 2;
                            RepositoryFactor.Save();

                            var UserAgent = tbUserFactor.tbUsers;
                            var PayedFactores = await RepositoryFactor.WhereAsync(s => s.tbUf_Status == 2 && s.FK_User_ID == UserAgent.User_ID);

                            var SumPayFactores = PayedFactores.Sum(s => s.tbUf_Value);
                            var SumPay2Factor = SumPayFactores;
                            var res = SumPayFactores * 0.02;
                            SumPay2Factor += (int)res;
                            StringBuilder str = new StringBuilder();
                            str.AppendLine("✅ نماینده گرامی رسید شما با موفقیت از سمت بانک تائید شد");
                            str.AppendLine("");
                            StringBuilder str2 = new StringBuilder();
                            str2.AppendLine("🧑‍💻 مدیر عزیز");
                            str2.AppendLine("");
                            str2.AppendLine("🤵 نماینده با نام کاربری : " + UserAgent.Username);
                            str2.AppendLine("");
                            if (SumPay2Factor >= UserAgent.Wallet)
                            {
                                var Remainder = SumPayFactores - UserAgent.Wallet;


                                var PayedFactroress = PayedFactores.OrderByDescending(s => s.tbUf_CreateTime).ToList();

                                if (SumPayFactores >= UserAgent.Wallet)
                                {
                                    UserAgent.Wallet = 0;
                                    if (Remainder > 0)
                                    {
                                        UserAgent.Wallet -= (int)Remainder;
                                        str.AppendLine("♨️ هزینه مازاد پرداختی به حالت بستانکار در کیف پول شما لحاظ شد");

                                        str2.AppendLine("کیف پول اش صفرشد و به حالت بستنکار در آمد");
                                    }
                                    else
                                    {
                                        str.AppendLine("♨️ بدهی شما صفر شد");

                                        str2.AppendLine("بدهی اش صفرشد");
                                    }
                                    tbUserFactor.tbUf_Description = "آخرین فاکتور ثبت شده";
                                }
                                else
                                {
                                    str.AppendLine("♨️ رسید های شما از بدهی شما کسر و 2 درصد بدهی در کیف پول شما درج گردید");
                                    str2.AppendLine("کیف پول اش به مقدار 2 درصد بدهی درج گردید و مابقی کسر گردید");
                                    UserAgent.Wallet = Math.Abs((int)Remainder);
                                }


                                foreach (var item in PayedFactores)
                                {
                                    item.tbUf_Status = 3;
                                }


                            }
                            else
                            {
                                str.AppendLine("♨️ رسید شما در سیستم ذخیره می شود بعد پرداخت کامل از بدهی شما کسر خواهد شد");

                                str2.AppendLine("واریزی اش در سیستم ثبت گردید");
                            }

                            var TelegramUser = await RepositoryTelegramUser.FirstOrDefaultAsync(s => s.Tel_Username == UserAgent.TelegramID);
                            if (TelegramUser != null)
                            {
                                str.AppendLine("");
                                str.AppendLine("<b>" + "⚠️ نکته : حتما رسید را نهایتا تا 3 ساعت بعد از واریز برای ربات ارسال کنید" + "</b>");
                                str.AppendLine("");
                                str.AppendLine("🆔 @" + botSetting.Bot_ID);
                                TelegramBotClient botClient = new TelegramBotClient(botSetting.Bot_Token);
                                await botClient.SendTextMessageAsync(TelegramUser.Tel_UniqUserID, str.ToString());
                            }
                            RepositoryFactor.Save();
                            transaction.Commit();

                            var admin = RepositoryTelegramUser.Where(s => s.Tel_UniqUserID == botSetting.AdminBot_ID.ToString()).FirstOrDefault();
                            if (admin != null)
                            {
                                TelegramBotClient botClient = new TelegramBotClient(botSetting.Bot_Token);
                                await botClient.SendTextMessageAsync(admin.Tel_UniqUserID, str2.ToString());
                            }

                            return BadRequest("Add");
                        }
                        else
                        {
                            tbUserFactors factor = new tbUserFactors();
                            factor.tbUf_Value = NewPrice;
                            factor.tbUf_CreateTime = DateTime.Now;
                            RepositoryFactor.Insert(factor);
                            await RepositoryFactor.SaveChangesAsync();
                            transaction.Commit();
                            return BadRequest("Add");

                        }
                    }
                    else
                    {

                        logger.Warn("چنین شماره ای " + Mobile + " در سیستم یافت نشد");
                        return BadRequest("FINISHED");
                    }


                }
                catch (Exception ex)
                {
                    int pr = int.Parse(SMSMessageText, NumberStyles.Currency);
                    transaction.Rollback();
                    logger.Error(ex, "خطا در پرداخت فاکتور به مبلغ " + pr.ConvertToMony() + " رخ داد");
                    return BadRequest();
                }
            }
        }

        #endregion

        #region تابع تائید کردن پرداختی زرین پال

        [System.Web.Http.HttpGet]
        public async Task<HttpResponseMessage> VerifyPayZarinPal(string BotName, string Authority)
        {



            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var User = await RepositoryUser.FirstOrDefaultAsync(p => p.Username == BotName);
                    if (User != null)
                    {
                        var date2 = DateTime.Now.AddHours(-24);
                        var item = await RepositoryDepositWallet.FirstOrDefaultAsync(p => p.dw_Status == "FOR_PAY" && p.dw_TaxId == Authority);
                        var botSetting = User.tbBotSettings.FirstOrDefault();
                        if (item != null)
                        {
                            item.dw_Status = "FINISH";
                            item.tbTelegramUsers.Tel_Wallet += item.dw_Price / 10;
                            StringBuilder str = new StringBuilder();
                            str.AppendLine("✅ کیف پولتو شارژ کردم");
                            str.AppendLine("");
                            str.AppendLine("💰 موجودی الانت : " + item.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                            str.AppendLine("");
                            str.AppendLine("🔔 خب حالا برو اشتراکتو تمدید کن یا اشتراک جدید بخر و حالشو ببر.");
                            str.AppendLine("");
                            str.AppendLine("توجه کن اگر اشتراک داری برو تو بخش تمدید و تمدید کن وگرنه اشتراکت تموم میشه و قطع میشی");

                            var keyboard = Keyboards.GetHomeButton();


                            var htmlBuilder = new StringBuilder();

                            htmlBuilder.Append("<html><head><meta charset='UTF-8'><title>پرداخت موفق</title><style>");
                            htmlBuilder.Append("body { font-family: 'Vazir', sans-serif; background-color: #f0f8ff; text-align: center; padding-top: 100px; direction: rtl; }");
                            htmlBuilder.Append(".message-box { background-color: #e0ffe0; border: 2px solid #4CAF50; display: inline-block; padding: 30px 50px; border-radius: 20px; box-shadow: 0 4px 10px rgba(0,0,0,0.1); }");
                            htmlBuilder.Append("h1 { color: #2e7d32; margin-bottom: 20px; }");
                            htmlBuilder.Append("p { font-size: 18px; color: #333; }");
                            htmlBuilder.Append(".back-btn { margin-top: 30px; display: inline-block; padding: 10px 25px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 10px; font-size: 16px; }");
                            htmlBuilder.Append(".back-btn:hover { background-color: #45a049; }");
                            htmlBuilder.Append("</style></head><body>");

                            htmlBuilder.Append("<div class='message-box'>");
                            htmlBuilder.Append("<h1>✅ کیف پول شما با موفقیت شارژ شد!</h1>");
                            htmlBuilder.Append("<p>برای ادامه، لطفاً به ربات بازگردید 🤖</p>");
                            htmlBuilder.AppendFormat("<a class='back-btn' href='https://t.me/{0}'>بازگشت به ربات</a>", botSetting.Bot_ID);
                            htmlBuilder.Append("</div></body></html>");


                            TelegramBotClient botClient = new TelegramBotClient(botSetting.Bot_Token);


                            if (botSetting.InvitePercent != null)
                            {
                                if (item.tbTelegramUsers.Tel_Parent_ID != null)
                                {
                                    var parent = item.tbTelegramUsers.tbTelegramUsers2;
                                    parent.Tel_Wallet += Convert.ToInt32((item.dw_Price / 10) * botSetting.InvitePercent.Value);

                                    StringBuilder str1 = new StringBuilder();
                                    str1.AppendLine("☺️ کاربر گرامی، به دلیل خرید دوستتان، ‌" + botSetting.InvitePercent * 100 + " درصد از مبلغ خرید ایشان به کیف پول شما اضافه شد. از حمایت شما سپاسگزاریم 🙏🏻");
                                    str1.AppendLine("");
                                    str1.AppendLine("💰 موجودی فعلی کیف پول شما: " + parent.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                    str1.AppendLine("");
                                    str1.AppendLine("🚀 @" + botSetting.Bot_ID);

                                    await botClient.SendTextMessageAsync(parent.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html);
                                }
                            }

                            await RepositoryDepositWallet.SaveChangesAsync();

                            await botClient.SendTextMessageAsync(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);

                            logger.Info("فاکتور با آیدی " + item.dw_TaxId + " با موفقیت پرداخت شد");

                            var response = new HttpResponseMessage(HttpStatusCode.OK);
                            response.Content = new StringContent(htmlBuilder.ToString(), Encoding.UTF8, "text/html");
                            transaction.Commit();
                            return response;


                        }
                        else
                        {
                            return new HttpResponseMessage(HttpStatusCode.BadRequest);
                        }
                    }
                    else
                    {

                        return new HttpResponseMessage(HttpStatusCode.BadRequest);
                    }


                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "خطا در تائید تراکنش آیدی " + Authority + " رخ داد");
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }
            }
        }

        #endregion

        #region تائید پرداختی هاب اسمارت

        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> VerifyPay(string BotName, string PayMethod, string TaxId)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var User = await RepositoryUser.FirstOrDefaultAsync(p => p.Username == BotName);
                    if (User != null)
                    {



                        var date2 = DateTime.Now.AddHours(-24);
                        var tbDepositLog = await RepositoryDepositWallet.WhereAsync(p => p.dw_Status == "FOR_PAY" && p.dw_TaxId == TaxId && p.dw_PayMethod == PayMethod);
                        var botSetting = User.tbBotSettings.FirstOrDefault();
                        foreach (var item in tbDepositLog)
                        {
                            item.dw_Status = "FINISH";
                            item.tbTelegramUsers.Tel_Wallet += item.dw_Price / 10;
                            StringBuilder str = new StringBuilder();
                            str.AppendLine("✅ کیف پولتو شارژ کردم");
                            str.AppendLine("");
                            str.AppendLine("💰 موجودی الانت : " + item.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                            str.AppendLine("");
                            str.AppendLine("🔔 خب حالا برو اشتراکتو تمدید کن یا اشتراک جدید بخر و حالشو ببر.");
                            str.AppendLine("");
                            str.AppendLine("توجه کن اگر اشتراک داری برو تو بخش تمدید و تمدید کن وگرنه اشتراکت تموم میشه و قطع میشی");

                            var keyboard = Keyboards.GetHomeButton();




                            if (botSetting != null)
                            {
                                TelegramBotClient botClient = new TelegramBotClient(botSetting.Bot_Token);


                                if (botSetting.InvitePercent != null)
                                {
                                    if (item.tbTelegramUsers.Tel_Parent_ID != null)
                                    {
                                        var parent = item.tbTelegramUsers.tbTelegramUsers2;
                                        parent.Tel_Wallet += Convert.ToInt32((item.dw_Price / 10) * botSetting.InvitePercent.Value);

                                        StringBuilder str1 = new StringBuilder();
                                        str1.AppendLine("☺️ کاربر گرامی، به دلیل خرید دوستتان، ‌" + botSetting.InvitePercent * 100 + " درصد از مبلغ خرید ایشان به کیف پول شما اضافه شد. از حمایت شما سپاسگزاریم 🙏🏻");
                                        str1.AppendLine("");
                                        str1.AppendLine("💰 موجودی فعلی کیف پول شما: " + parent.Tel_Wallet.Value.ConvertToMony() + " تومان");
                                        str1.AppendLine("");
                                        str1.AppendLine("🚀 @" + botSetting.Bot_ID);

                                        await botClient.SendTextMessageAsync(parent.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html);
                                    }
                                }




                                if (botSetting.HubSmartPay_Status && PayMethod == "HubSmart")
                                {
                                    HubSmartAPI hubSmartAPI = new HubSmartAPI(botSetting.HubSmart_API_KEY);
                                    RequestVerifyTransaction verifyTransaction = new RequestVerifyTransaction();
                                    verifyTransaction.token = item.dw_hubsmart_token;

                                    var response = await hubSmartAPI.Verify(verifyTransaction);
                                    if (response.status)
                                    {
                                        await RealUser.SetUserStep(item.tbTelegramUsers.Tel_UniqUserID, "Start", db, item.tbTelegramUsers.tbUsers.Username);

                                        await botClient.SendTextMessageAsync(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
                                        await RepositoryDepositWallet.SaveChangesAsync();
                                        transaction.Commit();
                                    }
                                    else
                                    {
                                        logger.Warn("خطا در تائید تراکنش آیدی " + TaxId + " رخ داد");
                                    }
                                }

                                logger.Info("فاکتور با آیدی " + item.dw_TaxId + " با موفقیت پرداخت شد");
                                return Ok();
                            }


                        }
                        return BadRequest("FINISHED");
                    }
                    else
                    {

                        return BadRequest("FINISHED");
                    }


                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "خطا در تائید تراکنش آیدی " + TaxId + " رخ داد");
                    return BadRequest();
                }
            }
        }

        #endregion

        #region دریافت فاکتور ها برای اپلیکیشن

        [System.Web.Http.HttpGet]
        [Authorize]
        public async Task<IHttpActionResult> GetFactors()
        {
            var Date = DateTime.Now.AddDays(-1);
            var Factors = await db.tbDepositWallet_Log.Where(p => p.dw_CreateDatetime >= Date && p.dw_Status == "FOR_PAY").OrderByDescending(p => p.dw_CreateDatetime).ToListAsync();
            List<GetFactorsViewModel> data = new List<GetFactorsViewModel>();
            foreach (var item in Factors)
            {
                GetFactorsViewModel factor = new GetFactorsViewModel();
                factor.FullName = item.tbTelegramUsers.Tel_Username + " " + "(" + item.tbTelegramUsers.Tel_FirstName + " " + item.tbTelegramUsers.Tel_LastName + ")";
                factor.Price = item.dw_Price.Value.ConvertToMony();
                data.Add(factor);
            }

            return Ok(new { reuslt = data });
        }

        #endregion

        #region اضافه کردن توکن فایر بیس کاربر

        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> addFirebaseToken(AddFirebaseTokenModel tokenModel)
        {
            try
            {
                tbUsers User = null;
                var Log = await RepositoryLogs.FirstOrDefaultAsync(s => s.SubToken == tokenModel.sub_Token);
                if (Log != null)
                {
                    User = Log.tbLinkUserAndPlans.tbUsers;
                }
                else
                {
                    var link = await RepositoryLinks.FirstOrDefaultAsync(s => s.tbL_Token == tokenModel.sub_Token);
                    if (link != null)
                    {
                        User = link.tbTelegramUsers.tbUsers;
                    }
                }

                if (User != null)
                {
                    var firbase = await tbFirebaseMobileTokens.FirstOrDefaultAsync(s => s.tbFireBase_SubToken == tokenModel.sub_Token && s.tbFirebase_Token == tokenModel.firebase_token);
                    if (firbase != null)
                    {
                        return Content(System.Net.HttpStatusCode.Conflict, "این توکن از قبل ثبت شده است");
                    }
                    tbFirebaseMobileTokens tbFirebaseMobile = new tbFirebaseMobileTokens();
                    tbFirebaseMobile.tbFirebase_Token = tokenModel.firebase_token;
                    tbFirebaseMobile.tbFireBase_FK_User_ID = User.User_ID;
                    tbFirebaseMobile.tbFireBase_SubToken = tokenModel.sub_Token;
                    tbFirebaseMobileTokens.Insert(tbFirebaseMobile);
                    await tbFirebaseMobileTokens.SaveChangesAsync();
                    logger.Info("توکن فایربیس نماینده " + User.Username + " برای توکن " + tokenModel.sub_Token + " اضافه گردید");
                    return Ok();
                }
                else
                {
                    return Content(System.Net.HttpStatusCode.NotFound, "توکن مورد نظر یافت نشد");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ثبت توکن فایبر بیس با خطا مواجه شد");
                return Content(System.Net.HttpStatusCode.InternalServerError, "خطا در پردازش اطلاعات");
            }
        }

        #endregion

    }

}

