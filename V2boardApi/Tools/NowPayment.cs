using DataLayer.DomainModel;
using DataLayer.Repository;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Windows;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using V2boardApi.Tools;
using V2boardBot.Models;
using V2boardBotApp;
using V2boardBotApp.Models;

namespace V2boardBot.Tools
{

    public class NowPayment
    {
        private HttpClient PaymentClient { get; set; }

        #region ریپازیتوری ها
        private Repository<tbPlans> repositoryPlans;
        private Repository<tbUsers> RepositoryUsers;
        private Repository<tbLinks> RepositoryLinks;
        private Repository<tbLinkUserAndPlans> RepositoryLinkPlan;
        private Repository<tbTelegramUsers> RepositoryTelegramUsers;
        private Repository<tbServers> RepositoryServers;
        private Repository<tbOrders> RepositoryOrders;
        private static Repository<tbDepositWallet_Log> tbDepositLogRepo;
        private Entities db;

        #endregion

        #region سازنده تابع

        public NowPayment(Entities _db,string NowPaymentURL, string ApiKey)
        {
            PaymentClient = new HttpClient();
            PaymentClient.BaseAddress = new Uri(NowPaymentURL + "/v1/");
            PaymentClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            db = _db;
            repositoryPlans = new Repository<tbPlans>(db);
            RepositoryUsers = new Repository<tbUsers>(db);
            RepositoryLinks = new Repository<tbLinks>(db);
            RepositoryLinkPlan = new Repository<tbLinkUserAndPlans>(db);
            RepositoryTelegramUsers = new Repository<tbTelegramUsers>(db);
            RepositoryServers = new Repository<tbServers>(db);
            RepositoryOrders = new Repository<tbOrders>(db);
            tbDepositLogRepo = new Repository<tbDepositWallet_Log>(db);
        }

        #endregion

        #region بروزرسانی Entity های صفحه

        public void UpdateNowPaymentModel(Entities _db)
        {

        }


        #endregion


        //#region ایجاد یک تراکنش جدید ( از شارژ کیف پول )

        //public void CreateIncreaseWalletPayment(TelegramBotClient bot, int price, int tel_user_id, long tel_uniq_id, Telegram.Bot.Types.Message message)
        //{

        //    var USDTPrice = Utility.GetPriceUSDT();

        //    PaymentReqModel paymentReq = new PaymentReqModel();
        //    paymentReq.price_amount = (price + 10000) / (Convert.ToDouble(USDTPrice));
        //    paymentReq.price_currency = "usd";
        //    paymentReq.pay_currency = "trx";
        //    paymentReq.order_id = Guid.NewGuid().ToString();
        //    var serial = JsonConvert.SerializeObject(paymentReq);

        //    StringContent stringContent = new StringContent(serial, Encoding.UTF8, "application/json");

        //    var request = PaymentClient.PostAsync("payment", stringContent);

        //    if (request.Result.StatusCode == System.Net.HttpStatusCode.Created)
        //    {
        //        var result = request.Result.Content.ReadAsStringAsync();

        //        var payment = JsonConvert.DeserializeObject<PaymentRespModel>(result.Result.ToString());
        //        StringBuilder str = new StringBuilder();
        //        str.AppendLine("✅ تراکنش شما با مبلغ " + price.ConvertToMony() + " تومان ایجاد شد.");
        //        str.AppendLine("");
        //        str.AppendLine("⚠️ مهم: پس از پرداخت حتما روی دکمه \"✅ واریز کردم\" بزنید تا تراکنش تایید و موجودی به کیف پول شما اضافه شود");
        //        str.AppendLine("");
        //        str.AppendLine("❌ این تراکنش تنها 20 دقیقه معتبر است بعد از آن پرداخت نکنید.");
        //        str.AppendLine("");
        //        tbDepositWallet_Log tbDeposit = new tbDepositWallet_Log();
        //        tbDeposit.dw_Price = price * 10;
        //        tbDeposit.dw_CreateDatetime = DateTime.Now;
        //        tbDeposit.dw_Status = "FOR_PAY";
        //        tbDeposit.FK_TelegramUser_ID = tel_user_id;
        //        tbDeposit.dw_payment_id = payment.payment_id;
        //        tbDepositLogRepo.Insert(tbDeposit);
        //        tbDepositLogRepo.Save();
        //        var ConvertPrice = payment.pay_amount.ToString().Replace(".", "_");

        //        var key = Keyboards.GetPaymentButtonForIncreaseWallet(payment.payment_id, payment.pay_address, ConvertPrice);
        //        bot.EditMessageTextAsync(tel_uniq_id, message.MessageId, str.ToString(), parseMode: ParseMode.Html, replyMarkup: key);
        //    }
        //    else
        //    {
        //        var s=  request.Result.Content.ReadAsStringAsync();
        //    }
        //}

        //#endregion

        //#region چک کردن وضعیت تراکنش شارژ کیف پول 

        //public void CheckIncreaseWalletStatus(Telegram.Bot.TelegramBotClient bot, tbBotSettings botSetting, string paymentId, Update update, string Tel_uniq_userid)
        //{
        //    var tbDepositLog = tbDepositLogRepo.Where(p => p.dw_payment_id == paymentId && p.dw_Status == "FOR_PAY" && p.tbTelegramUsers.Tel_UniqUserID == Tel_uniq_userid).FirstOrDefault();
        //    if (tbDepositLog != null)
        //    {
        //        var request = PaymentClient.GetAsync("payment/" + paymentId);

        //        if (request.Result.StatusCode == System.Net.HttpStatusCode.OK)
        //        {
        //            var resultReq = request.Result.Content.ReadAsStringAsync();
        //            var payment = JsonConvert.DeserializeObject<PaymentRespModel>(resultReq.Result.ToString());
        //            if (payment.payment_status == "finished")
        //            {
        //                tbDepositLog.dw_Status = "FINISH";
        //                tbDepositLog.tbTelegramUsers.Tel_Wallet += (tbDepositLog.dw_Price / 10) + 10000;
        //                StringBuilder str = new StringBuilder();
        //                str.AppendLine("✅ کیف پول شما با موفقیت شارژ شد");
        //                str.AppendLine("");
        //                str.AppendLine("📌 موجودی کیف پول شما : " + tbDepositLog.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");

                       
        //                tbDepositLogRepo.Save();
        //                bot.SendTextMessageAsync(Tel_uniq_userid, str.ToString(), parseMode: ParseMode.Html);
        //                return;
        //            }
        //            else
        //            {
        //                StringBuilder str = new StringBuilder();
        //                str.AppendLine("❌ تراکنش شما هنوز در سیستم ثبت نشده اگر پرداخت کرده اید 2 دقیقه دیگر مجدد تلاش کنید");
        //                bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, str.ToString(), showAlert: true);
        //                return;
        //            }

        //        }
        //    }

        //}


        //#endregion

        #region مدل ایجاد تراکنش

        public class PaymentReqModel
        {
            public double price_amount { get; set; }
            public string price_currency { get; set; }
            public string pay_currency { get; set; }
            public string order_id { get; set; }

        }

        #endregion

        #region مدل وضعیت تراکنش

        public class PaymentRespModel
        {
            public string payment_id { get; set; }
            public string payment_status { get; set; }
            public string pay_address { get; set; }
            public double price_amount { get; set; }
            public string price_currency { get; set; }
            public double pay_amount { get; set; }
            public string pay_currency { get; set; }
            public string order_id { get; set; }
            public DateTime valid_until { get; set; }

        }

        #endregion
    }
}