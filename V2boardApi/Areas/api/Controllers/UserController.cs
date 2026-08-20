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
        private Repository<tbPaymentLinks> RepositoryPayLinks { get; set; }
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
            RepositoryPayLinks = new Repository<tbPaymentLinks>();
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

                                    await botClient.SendMessage(parent.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html);
                                }
                            }

                            await RepositoryDepositWallet.SaveChangesAsync();

                            await botClient.SendMessage(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);

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

                                        await botClient.SendMessage(parent.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html);
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

                                        await botClient.SendMessage(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
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
            // فاکتورهای بدون کاربر تلگرام ( فاکتورهای ساخته شده از طریق API ) در این لیست جایی ندارند
            var Factors = await db.tbDepositWallet_Log.Where(p => p.dw_CreateDatetime >= Date && p.dw_Status == "FOR_PAY" && p.FK_TelegramUser_ID != null).OrderByDescending(p => p.dw_CreateDatetime).ToListAsync();
            var AgentFactors = await db.tbUserFactors.Where(p => p.tbUf_CreateTime >= Date && p.tbUf_Status == 1).OrderByDescending(p => p.tbUf_CreateTime).ToListAsync();
            List<GetFactorsViewModel> data = new List<GetFactorsViewModel>();
            foreach (var item in Factors)
            {
                GetFactorsViewModel factor = new GetFactorsViewModel();
                factor.FullName = item.tbTelegramUsers.Tel_Username + " " + "(" + item.tbTelegramUsers.Tel_FirstName + " " + item.tbTelegramUsers.Tel_LastName + ")";
                factor.Price = item.dw_Price.Value.ConvertToMony();
                data.Add(factor);
            }

            foreach (var item in AgentFactors)
            {
                GetFactorsViewModel factor = new GetFactorsViewModel();
                var agentName = string.IsNullOrWhiteSpace(item.tbUsers.FullName) ? item.tbUsers.Username : item.tbUsers.FullName;
                factor.FullName = item.tbUsers.Username + " (" + agentName + ")";
                factor.Price = item.tbUf_Value.Value.ConvertToMony();
                data.Add(factor);
            }

            return Ok(new { reuslt = data });
        }

        #endregion

        #region دریافت لیست تعرفه های نماینده که در ربات نمایش داده می شوند

        /// <summary>
        /// خواندن توکن نماینده از هدر Authorization (با یا بدون پیشوند Bearer)
        /// </summary>
        private string GetAgentTokenFromHeader()
        {
            IEnumerable<string> AuthValues;
            if (!Request.Headers.TryGetValues("Authorization", out AuthValues))
            {
                return null;
            }

            var token = AuthValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(token) && token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer ".Length);
            }

            token = token?.Trim();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> GetAgentPlans()
        {
            try
            {
                var token = GetAgentTokenFromHeader();
                if (token == null)
                {
                    return BadRequest("توکن در هدر Authorization ارسال نشده است");
                }

                var User = await db.tbUsers.FirstOrDefaultAsync(p => p.Token == token);
                if (User == null)
                {
                    return Content(HttpStatusCode.NotFound, "کاربری با این توکن یافت نشد");
                }

                var PlanLinks = await db.tbLinkUserAndPlans
                    .Where(p => p.L_FK_U_ID == User.User_ID && p.L_ShowInBot == true && p.L_Status == true && p.L_SellPrice != null)
                    .OrderBy(p => p.tbPlans.PlanMonth)
                    .ThenBy(p => p.tbPlans.PlanVolume)
                    .ToListAsync();

                List<AgentPlanViewModel> data = new List<AgentPlanViewModel>();
                foreach (var item in PlanLinks)
                {
                    AgentPlanViewModel plan = new AgentPlanViewModel();
                    plan.PlanId = item.Link_PU_ID;
                    plan.PlanVolume = item.tbPlans.PlanVolume;
                    plan.PlanMonth = item.tbPlans.PlanMonth;
                    plan.PlanPrice = item.L_SellPrice.Value;
                    plan.DeviceLimit = item.tbPlans.device_limit;
                    plan.IsUnlimited = item.tbPlans.IsRobotPlan;
                    data.Add(plan);
                }

                return Ok(new { result = data });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دریافت لیست تعرفه های نماینده");
                return Content(HttpStatusCode.InternalServerError, "خطا در دریافت لیست تعرفه ها");
            }
        }

        #endregion

        #region سوالات متداول ربات تلگرام

        /// <summary>
        /// همان پرسش و پاسخ دکمه «❓ سؤالات رایج» ربات را برمی گرداند
        /// تا اپلیکیشن همان متن را بدون وابستگی به تلگرام نشان دهد.
        /// توکن نماینده از هدر Authorization خوانده می شود تا آیدی پشتیبانی همان نماینده هم برگردد.
        /// </summary>
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> GetFaq()
        {
            try
            {
                var token = GetAgentTokenFromHeader();
                if (token == null)
                {
                    return BadRequest("توکن در هدر Authorization ارسال نشده است");
                }

                var User = await db.tbUsers.FirstOrDefaultAsync(p => p.Token == token);
                if (User == null)
                {
                    return Content(HttpStatusCode.NotFound, "کاربری با این توکن یافت نشد");
                }

                var BotSettings = User.tbBotSettings.FirstOrDefault();
                string supportUsername = null;
                if (BotSettings != null && !string.IsNullOrWhiteSpace(BotSettings.AdminUsername))
                {
                    supportUsername = BotSettings.AdminUsername.Trim().TrimStart('@');
                }

                return Ok(FaqViewModel.ForAgent(supportUsername));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دریافت سوالات متداول");
                return Content(HttpStatusCode.InternalServerError, "خطا در دریافت سوالات متداول");
            }
        }

        #endregion

        #region لینک های ارتباطی پشتیبانی نماینده

        /// <summary>
        /// لیست لینک های ارتباطی پشتیبانی همان نماینده را برمی گرداند
        /// تا اپلیکیشن صفحه ارتباط با پشتیبانی را بسازد.
        /// توکن نماینده از هدر Authorization خوانده می شود.
        /// </summary>
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> GetSupportLinks()
        {
            try
            {
                var token = GetAgentTokenFromHeader();
                if (token == null)
                {
                    return BadRequest("توکن در هدر Authorization ارسال نشده است");
                }

                var User = await db.tbUsers.FirstOrDefaultAsync(p => p.Token == token);
                if (User == null)
                {
                    return Content(HttpStatusCode.NotFound, "کاربری با این توکن یافت نشد");
                }

                var rows = await db.tbSupportLinks
                    .Where(s => s.FK_User_ID == User.User_ID)
                    .OrderBy(s => s.tbSl_ID)
                    .ToListAsync();

                var items = new List<SupportLinkItemViewModel>();
                foreach (var row in rows)
                {
                    SupportLinkItemViewModel item = new SupportLinkItemViewModel();
                    item.Id = row.tbSl_ID;
                    item.Title = row.tbSl_Title;
                    item.Link = row.tbSl_Link;
                    item.Phone = row.tbSl_Phone;
                    items.Add(item);
                }

                return Ok(new SupportLinkListViewModel { Items = items });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دریافت لینک های ارتباطی پشتیبانی");
                return Content(HttpStatusCode.InternalServerError, "خطا در دریافت لینک های ارتباطی");
            }
        }

        #endregion

        #region موجودی کیف پول ربات تلگرام مشتری

        /// <summary>
        /// موجودی کیف پول ربات تلگرام صاحب یک اشتراک را برمی گرداند
        /// تا اپلیکیشن بتواند دکمه پرداخت از کیف پول را نشان دهد.
        /// توکن نماینده از هدر Authorization و توکن ساب از بدنه خوانده می شود.
        /// </summary>
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> GetTelegramWallet(GetTelegramWalletModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.SubscriptionToken))
                {
                    return BadRequest("توکن اشتراک ارسال نشده است");
                }

                var token = GetAgentTokenFromHeader();
                if (token == null)
                {
                    return BadRequest("توکن در هدر Authorization ارسال نشده است");
                }

                var Agent = await db.tbUsers.FirstOrDefaultAsync(p => p.Token == token);
                if (Agent == null)
                {
                    return Content(HttpStatusCode.NotFound, "کاربری با این توکن یافت نشد");
                }

                var Link = await FindAgentSubscriptionAsync(model.SubscriptionToken.Trim(), Agent.Username);
                if (Link == null)
                {
                    return Content(HttpStatusCode.NotFound, "اشتراکی با این توکن برای این نماینده یافت نشد");
                }

                TelegramWalletViewModel data = new TelegramWalletViewModel();
                data.SubscriptionName = GetDisplayNameOfAccount(Link.tbL_Email);

                tbTelegramUsers TelUser = null;
                if (Link.FK_TelegramUserID != null)
                {
                    TelUser = await db.tbTelegramUsers.FirstOrDefaultAsync(p => p.Tel_UserID == Link.FK_TelegramUserID.Value && p.FK_User_ID == Agent.User_ID);
                }

                if (TelUser == null)
                {
                    data.HasWallet = false;
                    data.Balance = 0;
                    data.Message = "این اشتراک به حساب ربات تلگرام متصل نیست";
                    return Ok(data);
                }

                data.HasWallet = true;
                data.Balance = TelUser.Tel_Wallet == null ? 0 : Convert.ToInt64(TelUser.Tel_Wallet.Value);
                data.Message = "موجودی کیف پول ربات";
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دریافت موجودی کیف پول تلگرام");
                return Content(HttpStatusCode.InternalServerError, "خطا در دریافت موجودی کیف پول");
            }
        }

        #endregion

        #region ساخت فاکتور پرداخت مستقیم برای مشتری نماینده

        /// <summary>
        /// ساخت فاکتور پرداخت مستقیم (کارت به کارت) برای مشتری نماینده
        /// توکن نماینده از هدر Authorization و شناسه تعرفه از بدنه درخواست خوانده می شود.
        /// خالی بودن SubscriptionToken یعنی اشتراک جدید ساخته شود و پر بودن آن یعنی تمدید همان اشتراک.
        /// </summary>
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> CreateAgentInvoice(CreateAgentInvoiceModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("اطلاعات درخواست ارسال نشده است");
                }

                var token = GetAgentTokenFromHeader();
                if (token == null)
                {
                    return BadRequest("توکن در هدر Authorization ارسال نشده است");
                }

                var Agent = await db.tbUsers.FirstOrDefaultAsync(p => p.Token == token);
                if (Agent == null)
                {
                    return Content(HttpStatusCode.NotFound, "کاربری با این توکن یافت نشد");
                }

                var PlanLink = await db.tbLinkUserAndPlans
                    .FirstOrDefaultAsync(p => p.Link_PU_ID == model.PlanId
                                           && p.L_FK_U_ID == Agent.User_ID
                                           && p.L_Status == true
                                           && p.L_SellPrice != null);
                if (PlanLink == null)
                {
                    return Content(HttpStatusCode.NotFound, "تعرفه ای با این شناسه برای این نماینده یافت نشد");
                }

                var Card = Agent.tbBankCardNumbers.Where(p => p.Active).FirstOrDefault();
                if (Card == null)
                {
                    return Content(HttpStatusCode.NotFound, "برای این نماینده کارت بانکی فعالی ثبت نشده است");
                }

                // تعیین اشتراک : توکن خالی یعنی اشتراک جدید ، توکن پر یعنی تمدید اشتراک موجود همین نماینده
                string AccountName;
                string OrderType;
                var RequestedToken = model.SubscriptionToken == null ? "" : model.SubscriptionToken.Trim();
                if (RequestedToken.Length == 0)
                {
                    AccountName = await GenerateAccountNameAsync(Agent.Username);
                    if (AccountName == null)
                    {
                        return Content(HttpStatusCode.InternalServerError, "ساخت نام اشتراک جدید با خطا مواجه شد");
                    }
                    OrderType = "جدید";
                }
                else
                {
                    var Link = await FindAgentSubscriptionAsync(RequestedToken, Agent.Username);
                    if (Link == null)
                    {
                        return Content(HttpStatusCode.NotFound, "اشتراکی با این توکن برای این نماینده یافت نشد");
                    }
                    AccountName = Link.tbL_Email;
                    OrderType = "تمدید";
                }

                // قیمت به تومان ، مشابه گزینه پرداخت مستقیم ربات تخفیف فعال نماینده هم اعمال می شود
                var Price = PlanLink.L_SellPrice.Value;
                var BotSettings = Agent.tbBotSettings.FirstOrDefault();
                if (BotSettings != null && BotSettings.Present_Discount != null && BotSettings.Present_Discount != 0)
                {
                    Price -= (int)(Price * BotSettings.Present_Discount.Value);
                }

                var FullPrice = await BuildUniqueInvoicePriceAsync(Price);
                var TaxId = Guid.NewGuid().ToString().Split('-')[0] + "#" + Agent.User_ID;

                // دستگاهی که فاکتور را ساخته ، فقط اگر متعلق به همین نماینده باشد
                var MobileUserId = await FindMobileUserIdAsync(model.ResolveDeviceId(), Agent.User_ID);

                tbOrders Order = new tbOrders();
                Order.Order_Guid = Guid.NewGuid();
                Order.AccountName = AccountName;
                Order.OrderDate = DateTime.Now;
                Order.OrderStatus = "FOR_PAY";
                Order.OrderType = OrderType;
                Order.Order_Price = Price;
                Order.PriceWithOutDiscount = PlanLink.L_SellPrice.Value;
                Order.Traffic = PlanLink.tbPlans.PlanVolume;
                Order.Month = PlanLink.tbPlans.PlanMonth;
                Order.V2_Plan_ID = PlanLink.tbPlans.Plan_ID_V2;
                Order.FK_Link_Plan_ID = PlanLink.Link_PU_ID;
                Order.Tel_RenewedDate = DateTime.Now;
                Order.FK_MobileUser_ID = MobileUserId;

                tbDepositWallet_Log Deposit = new tbDepositWallet_Log();
                Deposit.dw_Price = FullPrice;
                Deposit.dw_CreateDatetime = DateTime.Now;
                Deposit.dw_Status = "FOR_PAY";
                Deposit.dw_PayMethod = "ApiCard";
                Deposit.dw_TaxId = TaxId;
                // روش پرداخت «اپلیکیشن» — این فاکتورها وارد مسیر تائید خودکار پیامک نمی شوند
                // چون CheckOrder فقط روی tbpm_Key == "CardToCard" فیلتر می کند
                Deposit.FK_PayMethod_ID = PaymentMethodIds.App;
                Deposit.FK_MobileUser_ID = MobileUserId;

                Order.tbDepositWallet_Log.Add(Deposit);
                db.tbOrders.Add(Order);
                await db.SaveChangesAsync();

                AgentInvoiceViewModel data = new AgentInvoiceViewModel();
                data.TrackingCode = TaxId;
                data.Amount = Convert.ToInt64(FullPrice);
                data.CardNumber = Card.CardNumber;
                data.CardHolderName = Card.InTheNameOf;
                data.SubscriptionName = GetDisplayNameOfAccount(AccountName);
                data.PlanPrice = PlanLink.L_SellPrice.Value;
                data.PlanVolume = PlanLink.tbPlans.PlanVolume;
                data.PlanMonth = PlanLink.tbPlans.PlanMonth;
                data.DeviceLimit = PlanLink.tbPlans.device_limit;

                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ساخت فاکتور پرداخت مستقیم نماینده");
                return Content(HttpStatusCode.InternalServerError, "خطا در ساخت فاکتور");
            }
        }

        /// <summary>
        /// جدا کردن بخش نمایشی نام اشتراک ( بخش قبل از $ و @ )
        /// </summary>
        private static string GetDisplayNameOfAccount(string AccountName)
        {
            if (string.IsNullOrEmpty(AccountName))
            {
                return AccountName;
            }

            var Cut = AccountName.IndexOfAny(new char[] { '$', '@' });
            return Cut >= 0 ? AccountName.Substring(0, Cut) : AccountName;
        }

        /// <summary>
        /// ساخت یک نام اشتراک جدید و یکتا با ساختار name$random@AgentUsername
        /// </summary>
        private async Task<string> GenerateAccountNameAsync(string AgentUsername)
        {
            for (int i = 0; i < 10; i++)
            {
                var Name = Guid.NewGuid().ToString().Split('-')[0] + "$" + Guid.NewGuid().ToString().Split('-')[0] + "@" + AgentUsername;
                var Exists = await db.tbLinks.AnyAsync(p => p.tbL_Email == Name);
                if (!Exists)
                {
                    return Name;
                }
            }

            return null;
        }

        /// <summary>
        /// پیدا کردن رکورد دستگاه بر اساس deviceId ، فقط اگر به همین نماینده تخصیص داده شده باشد.
        /// برنگرداندن رکورد خطا نیست ؛ فاکتور بدون اتصال به دستگاه هم ساخته می شود.
        /// </summary>
        private async Task<int?> FindMobileUserIdAsync(string DeviceId, int AgentUserId)
        {
            if (string.IsNullOrWhiteSpace(DeviceId))
            {
                return null;
            }

            var Id = DeviceId.Trim();
            var Device = await db.tbMobileUsers.FirstOrDefaultAsync(p => p.tbMu_AndroidId == Id && p.FK_User_ID == AgentUserId);
            if (Device == null)
            {
                logger.Warn("دستگاه " + Id + " برای نماینده " + AgentUserId + " یافت نشد ، فاکتور بدون اتصال به دستگاه ساخته می شود");
                return null;
            }

            Device.tbMu_LastSeenDate = DateTime.Now;
            return Device.tbMu_ID;
        }

        /// <summary>
        /// پیدا کردن اشتراک بر اساس توکن لینک ساب ، فقط در صورتی که متعلق به همین نماینده باشد
        /// </summary>
        private async Task<tbLinks> FindAgentSubscriptionAsync(string SubscriptionToken, string AgentUsername)
        {
            var Suffix = "@" + AgentUsername;

            return await db.tbLinks.FirstOrDefaultAsync(p => p.tbL_Token == SubscriptionToken && p.tbL_Email.EndsWith(Suffix));
        }

        /// <summary>
        /// تبدیل مبلغ تومان به ریال و افزودن سه رقم یکتا به انتهای آن
        /// یکتایی نسبت به فاکتورهای در انتظار پرداخت با همان مبلغ پایه بررسی می شود.
        /// </summary>
        private async Task<double> BuildUniqueInvoicePriceAsync(double PriceToman)
        {
            var BasePrice = Math.Round(PriceToman) * 10;

            var PendingPrices = await db.tbDepositWallet_Log
                .Where(p => p.dw_Status == "FOR_PAY"
                         && p.dw_Price != null
                         && p.dw_Price > BasePrice
                         && p.dw_Price <= BasePrice + 999)
                .Select(p => p.dw_Price.Value)
                .ToListAsync();

            var UsedSuffixes = new HashSet<int>(PendingPrices.Select(p => (int)(p - BasePrice)));

            Random ran = new Random();
            var Start = ran.Next(1, 1000);
            for (int i = 0; i < 999; i++)
            {
                var Suffix = ((Start - 1 + i) % 999) + 1;
                if (!UsedSuffixes.Contains(Suffix))
                {
                    return BasePrice + Suffix;
                }
            }

            return BasePrice + Start;
        }

        #endregion

        #region آپلود رسید فاکتور اپلیکیشن و ارسال به ادمین ربات

        /// <summary>
        /// آپلود عکس رسید برای فاکتور ساخته شده با CreateAgentInvoice.
        /// فایل روی سرور ذخیره و همان عکس برای ادمین ربات تلگرام نماینده ارسال می شود.
        /// ادمین مثل تائید دستی ربات ابتدا تائید می زند و بعد مبلغ فاکتور را انتخاب می کند.
        /// </summary>
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> UploadAgentInvoiceReceipt()
        {
            try
            {
                var token = GetAgentTokenFromHeader();
                if (token == null)
                {
                    return BadRequest("توکن در هدر Authorization ارسال نشده است");
                }

                var Agent = await db.tbUsers
                    .Include(p => p.tbBotSettings)
                    .FirstOrDefaultAsync(p => p.Token == token);
                if (Agent == null)
                {
                    return Content(HttpStatusCode.NotFound, "کاربری با این توکن یافت نشد");
                }

                string taxId;
                HttpPostedFile postedFile;
                if (!TryReadReceiptUpload(out taxId, out postedFile))
                {
                    if (string.IsNullOrWhiteSpace(taxId))
                    {
                        return BadRequest("کد پیگیری ارسال نشده است");
                    }

                    return BadRequest("فایل رسید ارسال نشده است");
                }

                if (!AppInvoiceReceiptService.IsAllowedImage(postedFile.FileName, postedFile.ContentLength))
                {
                    return BadRequest("فقط عکس jpg و png و webp تا سقف ۴ مگابایت پذیرفته می‌شود");
                }

                var Deposit = await db.tbDepositWallet_Log
                    .Include(p => p.tbOrders)
                    .Include(p => p.tbMobileUsers)
                    .FirstOrDefaultAsync(p => p.dw_TaxId == taxId);
                if (Deposit == null || Deposit.tbOrders == null)
                {
                    return Content(HttpStatusCode.NotFound, "فاکتوری با این کد پیگیری یافت نشد");
                }

                var Order = Deposit.tbOrders;
                var Suffix = "@" + Agent.Username;
                if (Order.AccountName == null || !Order.AccountName.EndsWith(Suffix))
                {
                    return Content(HttpStatusCode.NotFound, "فاکتوری با این کد پیگیری یافت نشد");
                }

                if (Deposit.FK_PayMethod_ID != PaymentMethodIds.App)
                {
                    return BadRequest("این فاکتور مربوط به اپلیکیشن نیست");
                }

                if (Deposit.dw_Status != "FOR_PAY")
                {
                    return BadRequest("این فاکتور قبلا تائید شده است");
                }

                var folder = HttpContext.Current.Server.MapPath(AppInvoiceReceiptService.FolderVirtualPath);
                var sendResult = await AppInvoiceReceiptService.SaveAndNotifyAdminAsync(
                    Deposit, Agent, postedFile.InputStream, postedFile.FileName, folder);

                Deposit.dw_payment_id = sendResult.FileName;
                await db.SaveChangesAsync();

                AgentInvoiceReceiptViewModel data = new AgentInvoiceReceiptViewModel();
                data.TrackingCode = taxId;
                data.ReceiptUploaded = true;
                data.SentToAdmin = sendResult.SentToAdmin;
                data.Message = sendResult.Message;
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در آپلود رسید فاکتور نماینده");
                return Content(HttpStatusCode.InternalServerError, "خطا در آپلود رسید فاکتور");
            }
        }

        /// <summary>
        /// خواندن TaxId و فایل از multipart فرم ASP.NET.
        /// نام فیلدها: TaxId / taxId و Receipt / receipt. اگر نام فایل مشخص نباشد اولین فایل گرفته می شود.
        /// </summary>
        private static bool TryReadReceiptUpload(out string taxId, out HttpPostedFile postedFile)
        {
            taxId = null;
            postedFile = null;
            if (HttpContext.Current == null || HttpContext.Current.Request == null)
            {
                return false;
            }

            var request = HttpContext.Current.Request;
            taxId = request.Form["TaxId"] ?? request.Form["taxId"] ?? request["TaxId"] ?? request["taxId"];
            if (!string.IsNullOrWhiteSpace(taxId))
            {
                taxId = taxId.Trim();
            }

            postedFile = request.Files["Receipt"] ?? request.Files["receipt"];
            if (postedFile == null && request.Files.Count > 0)
            {
                postedFile = request.Files[0];
            }

            return !string.IsNullOrWhiteSpace(taxId) && postedFile != null && postedFile.ContentLength > 0;
        }

        #endregion

        #region بررسی وضعیت فاکتور پرداخت مستقیم نماینده

        /// <summary>
        /// بررسی اینکه فاکتور ساخته شده با CreateAgentInvoice تائید شده است یا خیر
        /// کد پیگیری (TaxId) از بدنه درخواست و توکن نماینده از هدر Authorization خوانده می شود.
        /// اگر PayFromWallet برابر true باشد و فاکتور هنوز FOR_PAY باشد ،
        /// موجودی کیف پول ربات تلگرام صاحب اشتراک بررسی و در صورت کافی بودن کسر و اشتراک تمدید/ساخته می شود.
        /// بعد از تائید جزئیات اشتراک (حجم، روز باقی‌مانده، تاریخ انقضا) هم برگردانده می شود.
        /// در سفارش جدید لینک اشتراک هم برمی گردد.
        /// </summary>
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> CheckAgentInvoice(CheckAgentInvoiceModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.TaxId))
                {
                    return BadRequest("کد پیگیری ارسال نشده است");
                }

                var token = GetAgentTokenFromHeader();
                if (token == null)
                {
                    return BadRequest("توکن در هدر Authorization ارسال نشده است");
                }

                var Agent = await db.tbUsers
                    .Include(p => p.tbServers)
                    .FirstOrDefaultAsync(p => p.Token == token);
                if (Agent == null)
                {
                    return Content(HttpStatusCode.NotFound, "کاربری با این توکن یافت نشد");
                }

                var TaxId = model.TaxId.Trim();
                var Deposit = await db.tbDepositWallet_Log.FirstOrDefaultAsync(p => p.dw_TaxId == TaxId);
                if (Deposit == null || Deposit.tbOrders == null)
                {
                    return Content(HttpStatusCode.NotFound, "فاکتوری با این کد پیگیری یافت نشد");
                }

                // فاکتور باید متعلق به همین نماینده باشد
                var Order = Deposit.tbOrders;
                var Suffix = "@" + Agent.Username;
                if (Order.AccountName == null || !Order.AccountName.EndsWith(Suffix))
                {
                    return Content(HttpStatusCode.NotFound, "فاکتوری با این کد پیگیری یافت نشد");
                }

                if (model.PayFromWallet && Deposit.dw_Status == "FOR_PAY")
                {
                    var appService = new AppInvoiceService();
                    var walletResult = await appService.ConfirmFromWalletAsync(Deposit.dw_ID, Agent.User_ID);
                    if (!walletResult.Success)
                    {
                        AgentInvoiceStatusViewModel fail = new AgentInvoiceStatusViewModel();
                        fail.TrackingCode = TaxId;
                        fail.Status = Deposit.dw_Status;
                        fail.OrderType = Order.OrderType;
                        fail.SubscriptionName = GetDisplayNameOfAccount(Order.AccountName);
                        fail.Amount = Deposit.dw_Price == null ? 0 : Convert.ToInt64(Deposit.dw_Price.Value);
                        fail.IsConfirmed = false;
                        fail.HasReceipt = HasInvoiceReceipt(Deposit);
                        fail.Message = walletResult.Message;
                        return Ok(fail);
                    }

                    await db.Entry(Deposit).ReloadAsync();
                    await db.Entry(Order).ReloadAsync();
                }

                AgentInvoiceStatusViewModel data = new AgentInvoiceStatusViewModel();
                data.TrackingCode = TaxId;
                data.Status = Deposit.dw_Status;
                data.OrderType = Order.OrderType;
                data.SubscriptionName = GetDisplayNameOfAccount(Order.AccountName);
                data.Amount = Deposit.dw_Price == null ? 0 : Convert.ToInt64(Deposit.dw_Price.Value);
                data.IsConfirmed = Deposit.dw_Status == "FINISH";
                data.HasReceipt = HasInvoiceReceipt(Deposit);

                if (!data.IsConfirmed)
                {
                    data.Message = "هنوز فاکتور شما تمدید نشده است. این فرایند ممکن است ۵ تا ۱۵ دقیقه طول بکشد. در صورت عدم تائید، رسید خودتان را ارسال کنید";
                    return Ok(data);
                }

                await AttachSubscriptionDetailsAsync(data, Order, Agent);

                if (Order.OrderType == "تمدید")
                {
                    data.Message = Order.OrderStatus == "FOR_RESERVE"
                        ? "پرداخت تائید شد و بسته به صورت رزرو ثبت شد"
                        : "اشتراک شما با موفقیت تمدید شد";
                    return Ok(data);
                }

                // سفارش جدید : لینک اشتراک از روی توکن ثبت شده در tbLinks ساخته می شود
                var Link = await db.tbLinks.FirstOrDefaultAsync(p => p.tbL_Email == Order.AccountName);
                if (Link == null || string.IsNullOrWhiteSpace(Link.tbL_Token))
                {
                    data.Message = "پرداخت تائید شد ولی اشتراک هنوز ساخته نشده است";
                    return Ok(data);
                }

                var Server = Link.tbServers != null ? Link.tbServers : Agent.tbServers;
                if (Server == null || string.IsNullOrWhiteSpace(Server.SubAddress))
                {
                    data.Message = "پرداخت تائید شد ولی آدرس لینک اشتراک روی سرور تنظیم نشده است";
                    return Ok(data);
                }

                data.SubscriptionLink = "https://" + Server.SubAddress + "/api/v1/client/subscribe?token=" + Link.tbL_Token;
                if (!string.IsNullOrWhiteSpace(Server.BackupSubAddr))
                {
                    data.BackupSubscriptionLink = "https://" + Server.BackupSubAddr + "/api/v1/client/subscribe?token=" + Link.tbL_Token;
                }
                data.Message = "پرداخت تائید شد و اشتراک ساخته شده است";

                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در بررسی وضعیت فاکتور نماینده");
                return Content(HttpStatusCode.InternalServerError, "خطا در بررسی وضعیت فاکتور");
            }
        }

        private static bool HasInvoiceReceipt(tbDepositWallet_Log deposit)
        {
            if (deposit == null || string.IsNullOrWhiteSpace(deposit.dw_payment_id))
            {
                return false;
            }

            var ext = Path.GetExtension(deposit.dw_payment_id).ToLowerInvariant();
            return AppInvoiceReceiptService.AllowedExtensions.Contains(ext);
        }

        /// <summary>
        /// پر کردن جزئیات اشتراک از v2_user — همان فیلدهایی که Sub/Info و تائید دستی ربات نشان می‌دهند.
        /// خطا اینجا وضعیت فاکتور را خراب نمی‌کند.
        /// </summary>
        private async Task AttachSubscriptionDetailsAsync(AgentInvoiceStatusViewModel data, tbOrders order, tbUsers agent)
        {
            try
            {
                var server = agent.tbServers;
                if (server == null || string.IsNullOrWhiteSpace(server.ConnectionString) || string.IsNullOrWhiteSpace(order.AccountName))
                {
                    return;
                }

                using (MySqlEntities mySql = new MySqlEntities(server.ConnectionString))
                {
                    await mySql.OpenAsync();
                    var parameters = new Dictionary<string, object> { { "@email", order.AccountName } };
                    using (var reader = await mySql.GetDataAsync(
                        "SELECT u,d,transfer_enable,expired_at FROM v2_user WHERE email=@email LIMIT 1", parameters))
                    {
                        if (!await reader.ReadAsync())
                        {
                            return;
                        }

                        var usedBytes = Convert.ToDouble(reader.GetValue(0)) + Convert.ToDouble(reader.GetValue(1));
                        var totalBytes = Convert.ToDouble(reader.GetValue(2));
                        data.TotalVolumeGb = Math.Round(Utility.ConvertByteToGB(totalBytes), 2);
                        data.UsedVolumeGb = Math.Round(Utility.ConvertByteToGB(usedBytes), 2);
                        data.RemainingDays = -1;
                        data.ExpireDate = null;

                        if (!reader.IsDBNull(3))
                        {
                            var expireSeconds = Convert.ToInt64(reader.GetValue(3));
                            if (expireSeconds > 0)
                            {
                                var expire = Utility.ConvertSecondToDatetime(expireSeconds);
                                data.RemainingDays = Utility.CalculateLeftDayes(expire);
                                data.ExpireDate = Utility.ConvertDateTimeToShamsi5(expire);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "خواندن جزئیات اشتراک بعد از تائید فاکتور " + data.TrackingCode + " ناموفق بود");
            }
        }

        #endregion


        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> VerifyTetraPay(TetraRespModel model)
        {
            logger.Info("Verify Tetra Called", model);
            var facotr = RepositoryDepositWallet.Where(a => a.dw_Authority == model.authority && a.dw_Status == "FOR_PAY").FirstOrDefault();
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

        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> VerifyTetraPayLink(TetraRespModel model)
        {
            logger.Info("Verify Tetra Link Called", model);
            var PayStatus = RepositoryPayLinks.Where(a => a.py_authority == model.authority && !a.py_status).FirstOrDefault();
            if (PayStatus != null)
            {
                PayStatus.py_status = true;
                logger.Info("Verify Tetra Pay With Tracking ID : " + PayStatus.py_hash);
                await RepositoryPayLinks.SaveChangesAsync();
                return Ok();
            }
            else
            {
                logger.Warn("Not Found Tetra Pay With Authority : " + model.authority);
                return Content(HttpStatusCode.NotFound, "Not Found Tetra Pay With Authority : " + model.authority);
            }

        }

        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> VerifyPlisio(PlisioRespModel model)
        {
            logger.Info("Verify Plisio Called",model);
            if (model.status == "completed")
            {
                var facotr = RepositoryDepositWallet.Where(a => a.dw_TaxId == model.order_number && a.dw_Status == "FOR_PAY").FirstOrDefault();
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
            else
            {
                return Content(HttpStatusCode.Continue, model.status);
            }
        }
    }

}

