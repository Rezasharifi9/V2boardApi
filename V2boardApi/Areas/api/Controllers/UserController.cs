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


                        var tbUserFactor = await RepositoryFactor.FirstOrDefaultAsync(p => p.tbUf_Value == pr && p.IsPayed == false);
                        if (tbUserFactor != null)
                        {
                            tbUserFactor.IsPayed = true;
                            transaction.Commit();
                            RepositoryFactor.Save();
                            return Ok();
                        }

                        var date2 = DateTime.Now.AddDays(-2);
                        var tbDepositLog = await RepositoryDepositWallet.WhereAsync(p => p.dw_Price == pr && p.dw_Status == "FOR_PAY" && p.dw_CreateDatetime >= date2);

                        foreach (var item in tbDepositLog)
                        {
                            item.dw_Status = "FINISH";
                            item.tbTelegramUsers.Tel_Wallet += item.dw_Price / 10;
                            StringBuilder str = new StringBuilder();
                            str.AppendLine("✅ کیف پول شما با موفقیت شارژ شد!");
                            str.AppendLine("");
                            str.AppendLine("💰 موجودی فعلی کیف پول شما: " + item.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                            str.AppendLine("");
                            str.AppendLine("🔔 حالا می‌توانید برای خرید اشتراک جدید یا تمدید اشتراک اقدام کنید.");


                            var keyboard = Keyboards.GetHomeButton();

                            await RealUser.SetUserStep(item.tbTelegramUsers.Tel_UniqUserID, "Start", db, item.tbTelegramUsers.tbUsers.Username);


                            var botSetting = User.tbBotSettings.FirstOrDefault();
                            if (botSetting != null)
                            {
                                TelegramBotClient botClient = new TelegramBotClient(botSetting.Bot_Token);
                                await RepositoryDepositWallet.SaveChangesAsync();
                                await botClient.SendTextMessageAsync(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
                                transaction.Commit();

                                logger.Info("فاکتور به مبلغ " + pr.ConvertToMony() + " با موفقیت پرداخت شد");
                                return Ok();
                            }


                        }


                        return BadRequest("NOT FOUND ORDER");

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

        //#region تاریخچه مصرف کاربر

        //[Authorize]
        //public async Task<IHttpActionResult> GetTrafficUsage(int userId)
        //{
        //    var Token = Request.Headers.Authorization;
        //    var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true).First();

        //    try
        //    {

        //        MySqlEntities mysql = new MySqlEntities(User.tbServers.ConnectionString);
        //        await mysql.OpenAsync();

        //        var reader = await mysql.GetDataAsync("select * from v2_stat_user where user_id=" + userId);
        //        List<UsagesModel> Useages = new List<UsagesModel>();
        //        while (reader.Read())
        //        {
        //            UsagesModel model = new UsagesModel();
        //            var d = reader.GetInt64("d");
        //            var u = reader.GetInt64("u");

        //            var total = d + u;

        //            var UnixDate = reader.GetInt64("updated_at");

        //            var Date = Utility.ConvertSecondToDatetime(UnixDate);

        //            model.Date = Utility.ConvertDateTimeToShamsi(Date);
        //            model.Used = Utility.ConvertByteToMG(total);

        //            Useages.Add(model);
        //        }

        //        var Useage = Useages.GroupBy(p => p.Date).ToList();
        //        var use = Useage.Select(p => new { Date = p.Key, Used = p.Sum(s => Math.Round(s.Used, 2)) }).ToList();

        //        await mysql.CloseAsync();
        //        return Ok(use);

        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex, "دریافت تاریخچه مصرف اشتراک با خطا مواجه شد");
        //        return Content(System.Net.HttpStatusCode.InternalServerError, "دریافت اطلاعات با خطا مواجه شد");
        //    }

        //}

        //#endregion

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

    }

}

