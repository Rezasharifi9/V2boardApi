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
using ExcelLibrary.BinaryFileFormat;

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
        private Repository<tbServerGroups> RepositoryServerGroups { get; set; }
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
            RepositoryServerGroups = new Repository<tbServerGroups>(db);
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
            TransactionHanderService service = new TransactionHanderService();
            var res = await service.CheckOrder(SMSMessageText, Mobile);
            if (res)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        #endregion

        #region تابع تائید کردن پرداختی زرین پال

        [System.Web.Http.HttpGet]
        public async Task<HttpResponseMessage> VerifyPayZarinPal(string BotName, string TaxId)
        {



            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var User = await RepositoryUser.FirstOrDefaultAsync(p => p.Username == BotName);
                    if (User != null)
                    {
                        var date2 = DateTime.Now.AddHours(-24);
                        var item = await RepositoryDepositWallet.FirstOrDefaultAsync(p => p.dw_Status == "FOR_PAY" && p.dw_TaxId == TaxId && p.dw_PayMethod == "Gateway");
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
                    logger.Error(ex, "خطا در تائید تراکنش آیدی " + TaxId + " رخ داد");
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


        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> VerifyTetraPay(TetraRespModel model)
        {
            var facotr = RepositoryDepositWallet.Where(a=> a.dw_Authority == model.authority && a.dw_Status == "FOR_PAY").FirstOrDefault();
            if (facotr != null)
            {
                TransactionHanderService transactionHanderService = new TransactionHanderService();
                await transactionHanderService.CheckOrderTetraPay(facotr.dw_ID, facotr.tbTelegramUsers.tbUsers.PhoneNumber);
                return Ok();
            }
            else
            {
                return Content(HttpStatusCode.NotFound, "تراکنشی با این مشخصات یافت نشد");
            }
            
        }
    }

}

