using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using V2boardApi.Areas.App.Data.BotFactoresViewModels;
using V2boardApi.Tools;
using V2boardBot.Functions;
using V2boardBot.Models;

namespace V2boardApi.Areas.App.Controllers
{
    [AuthorizeApp(Roles = "1,2,3,4")]
    [LogActionFilter]
    public class BotFactorsController : Controller
    {
        private Entities db;
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Repository<tbDepositWallet_Log> RepositoryDepositLog { get; set; }
        public BotFactorsController()
        {
            db = new Entities();
            RepositoryDepositLog = new Repository<tbDepositWallet_Log>(db);
        }


        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetFactores()
        {
            try
            {
                List<BotFactoresResponseModel> BotFactores = new List<BotFactoresResponseModel>();

                var Factors = RepositoryDepositLog.Where(p => p.tbTelegramUsers.tbUsers.Username == User.Identity.Name).ToList();

                foreach (var item in Factors)
                {
                    BotFactoresResponseModel factor = new BotFactoresResponseModel();
                    factor.Id = item.dw_ID;
                    if (item.dw_Status == "FOR_PAY")
                    {
                        factor.Status = 0;
                    }
                    else if (item.dw_Status == "FINISH")
                    {
                        factor.Status = 1;
                    }

                    factor.Date = item.dw_CreateDatetime.Value.ConvertDateTimeToShamsi2();
                    factor.User = item.tbTelegramUsers.Tel_Username + "(" + item.tbTelegramUsers.Tel_FirstName + " " + item.tbTelegramUsers.Tel_LastName + ")";
                    factor.Price = item.dw_Price.Value.ConvertToMony();
                    if (item.dw_PayMethod == null)
                    {
                        factor.PayMethod = 0;
                    }
                    else
                    {
                        if (item.dw_PayMethod == "Card")
                        {
                            factor.PayMethod = 0;
                        }
                        if (item.dw_PayMethod == "Gateway")
                        {
                            factor.PayMethod = 1;
                        }
                        if (item.dw_PayMethod == "HubSmart")
                        {
                            factor.PayMethod = 2;
                        }
                        if (item.dw_PayMethod == "Aranex")
                        {
                            factor.PayMethod = 3;
                        }


                    }

                    BotFactores.Add(factor);
                }


                return Json(new { data = BotFactores }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error("در نمایش فاکتور ها با خطایی مواجه شدیم !");
            }

            return View();
        }

        [HttpPost]
        public ActionResult Accept(int factor_id)
        {
            try
            {
                var User = JwtToken.GetUser_ID();
                var User_ID = Convert.ToInt32(User);
                var factor = RepositoryDepositLog.Where(s => s.dw_ID == factor_id && s.tbTelegramUsers.FK_User_ID == User_ID && s.dw_Status == "FOR_PAY").FirstOrDefault();
                if (factor != null)
                {
                    factor.dw_Status = "FINISH";
                    factor.tbTelegramUsers.Tel_Wallet += factor.dw_Price / 10;
                    StringBuilder str = new StringBuilder();
                    str.AppendLine("✅ کیف پول شما با موفقیت شارژ شد!");
                    str.AppendLine("");
                    str.AppendLine("💰 موجودی فعلی کیف پول شما: " + factor.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                    str.AppendLine("");
                    str.AppendLine("🔔 حالا می‌توانید برای خرید اشتراک جدید یا تمدید اشتراک اقدام کنید.");


                    var keyboard = Keyboards.GetHomeButton();

                    RealUser.SetUserStepWithoutAsync(factor.tbTelegramUsers.Tel_UniqUserID, "Start", db, factor.tbTelegramUsers.tbUsers.Username);


                    var botSetting = factor.tbTelegramUsers.tbUsers.tbBotSettings.FirstOrDefault();

                    TelegramBotClient botClient = new TelegramBotClient(botSetting.Bot_Token);

                    botClient.SendTextMessageAsync(factor.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);

                    if (botSetting.InvitePercent != null)
                    {
                        if (factor.tbTelegramUsers.Tel_Parent_ID != null)
                        {
                            var parent = factor.tbTelegramUsers.tbTelegramUsers2;
                            parent.Tel_Wallet += Convert.ToInt32((factor.dw_Price / 10) * botSetting.InvitePercent.Value);

                            StringBuilder str1 = new StringBuilder();
                            str1.AppendLine("☺️ کاربر گرامی، به دلیل خرید دوستتان، ‌" + botSetting.InvitePercent * 100 + " درصد از مبلغ خرید ایشان به کیف پول شما اضافه شد. از حمایت شما سپاسگزاریم 🙏🏻");
                            str1.AppendLine("");
                            str1.AppendLine("💰 موجودی فعلی کیف پول شما: " + parent.Tel_Wallet.Value.ConvertToMony() + " تومان");
                            str1.AppendLine("");
                            str1.AppendLine("🚀 @" + botSetting.Bot_ID);

                            botClient.SendTextMessageAsync(parent.Tel_UniqUserID, str1.ToString(), parseMode: ParseMode.Html);
                        }
                    }

                    RepositoryDepositLog.Save();
                    logger.Info("فاکتور به مبلغ " + factor.dw_Price.ToString().ConvertToMony() + " با موفقیت پرداخت شد");
                    return Toaster.Success("موفق", "تراکنش با موفقیت تائید شد");
                }
                else
                {
                    return MessageBox.Warning("ناموفق", "این تراکنش قبلا تائید شده");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "تائید فاکتور با خطا مواجه شد");
                return MessageBox.Error("ناموفق", "خطا در تائید فاکتور");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                RepositoryDepositLog.Dispose();

            }
            base.Dispose(disposing);
        }
    }
}