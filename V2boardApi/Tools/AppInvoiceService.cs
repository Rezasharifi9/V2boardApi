using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;

namespace V2boardApi.Tools
{
    /// <summary>
    /// تائید فاکتورهای ساخته شده از داخل اپلیکیشن موبایل
    /// (tbDepositWallet_Log.FK_PayMethod_ID = PaymentMethodIds.App).
    ///
    /// عمدا از TransactionHanderService.CheckOrder جداست : آن مسیر برای هر قدمش —
    /// از خواندن کانکشن استرینگ سرور تا ارسال پیام — به tbTelegramUsers نیاز دارد و
    /// فاکتور اپلیکیشن کاربر تلگرام ندارد. پیام تمدید برای مشتری از ربات نمی رود ؛
    /// مشتری نتیجه و جزئیات اشتراک را از خود برنامه با CheckAgentInvoice می گیرد.
    /// رسید عکس از UploadAgentInvoiceReceipt به ادمین ربات می رسد و تائید ادمین
    /// هم از همین سرویس (ConfirmAsync) انجام می شود.
    /// </summary>
    public class AppInvoiceService
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private Entities db;
        private Repository<tbLinks> RepositoryLinks { get; set; }
        private Repository<tbServerGroups> RepositoryServerGroups { get; set; }

        public AppInvoiceService()
        {
            db = new Entities();
            RepositoryLinks = new Repository<tbLinks>(db);
            RepositoryServerGroups = new Repository<tbServerGroups>(db);
        }

        public AppInvoiceService(Entities context)
        {
            db = context;
            RepositoryLinks = new Repository<tbLinks>(db);
            RepositoryServerGroups = new Repository<tbServerGroups>(db);
        }

        /// <summary>نتیجه تائید یک فاکتور اپلیکیشن</summary>
        public class ConfirmResult
        {
            public bool Success { get; set; }

            /// <summary>پیام فارسی قابل نمایش در پنل</summary>
            public string Message { get; set; }

            /// <summary>توکن اشتراک ساخته شده — فقط در سفارش جدید مقدار دارد</summary>
            public string SubscriptionToken { get; set; }

            public static ConfirmResult Fail(string message)
            {
                return new ConfirmResult { Success = false, Message = message };
            }
        }

        /// <summary>
        /// تائید فاکتور اپلیکیشن با کسر از کیف پول ربات تلگرام صاحب اشتراک.
        /// اگر موجودی کافی نباشد یا اشتراک به کاربر تلگرام وصل نباشد ، فاکتور دست نخورده می ماند.
        /// </summary>
        public Task<ConfirmResult> ConfirmFromWalletAsync(int depositId, int? agentUserId)
        {
            return ConfirmAsync(depositId, agentUserId, true);
        }

        /// <summary>
        /// تائید یک فاکتور اپلیکیشن و ساخت یا تمدید اشتراک روی v2board.
        /// </summary>
        /// <param name="depositId">شناسه فاکتور (tbDepositWallet_Log.dw_ID)</param>
        /// <param name="agentUserId">
        /// نماینده ای که تائید را انجام می دهد. null یعنی ادمین و بررسی مالکیت انجام نمی شود.
        /// </param>
        /// <param name="payFromWallet">
        /// true یعنی مبلغ از Tel_Wallet کاربر تلگرام کسر شود و بدهی نماینده هم مثل مسیر AccpetWallet ربات افزایش یابد.
        /// </param>
        public async Task<ConfirmResult> ConfirmAsync(int depositId, int? agentUserId, bool payFromWallet = false)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var Deposit = await db.tbDepositWallet_Log
                        .Include(p => p.tbOrders)
                        .FirstOrDefaultAsync(p => p.dw_ID == depositId
                                               && p.FK_PayMethod_ID == PaymentMethodIds.App);

                    if (Deposit == null)
                    {
                        return ConfirmResult.Fail("فاکتور اپلیکیشن با این شناسه یافت نشد");
                    }
                    if (Deposit.dw_Status != "FOR_PAY")
                    {
                        return ConfirmResult.Fail("این فاکتور قبلا رسیدگی شده است");
                    }

                    var Order = Deposit.tbOrders;
                    if (Order == null || string.IsNullOrWhiteSpace(Order.AccountName))
                    {
                        return ConfirmResult.Fail("این فاکتور سفارش متصل ندارد");
                    }

                    var Agent = ResolveAgent(Order.AccountName);
                    if (Agent == null)
                    {
                        return ConfirmResult.Fail("نماینده این فاکتور یافت نشد");
                    }
                    if (agentUserId != null && Agent.User_ID != agentUserId.Value)
                    {
                        return ConfirmResult.Fail("این فاکتور متعلق به شما نیست");
                    }

                    var Server = Agent.tbServers;
                    if (Server == null || string.IsNullOrWhiteSpace(Server.ConnectionString))
                    {
                        return ConfirmResult.Fail("سرور این نماینده تنظیم نشده است");
                    }

                    var PlanLink = Order.tbLinkUserAndPlans;
                    if (PlanLink == null || PlanLink.tbPlans == null)
                    {
                        return ConfirmResult.Fail("تعرفه این سفارش یافت نشد");
                    }

                    var Link = await db.tbLinks
                        .Include(p => p.tbTelegramUsers)
                        .FirstOrDefaultAsync(p => p.tbL_Email == Order.AccountName);

                    var agentWalletBefore = Agent.Wallet;
                    if (payFromWallet)
                    {
                        var walletResult = ApplyWalletPayment(Deposit, Order, Agent, PlanLink, Link);
                        if (!walletResult.Success)
                        {
                            transaction.Rollback();
                            return walletResult;
                        }
                    }

                    ConfirmResult result;
                    using (MySqlEntities mySql = new MySqlEntities(Server.ConnectionString))
                    {
                        await mySql.OpenAsync();

                        result = Link == null
                            ? await CreateSubscriptionAsync(mySql, Order, PlanLink, Server)
                            : await RenewSubscriptionAsync(mySql, Order, PlanLink, Link);

                        await mySql.CloseAsync();
                    }

                    if (!result.Success)
                    {
                        transaction.Rollback();
                        return result;
                    }

                    Deposit.dw_Status = "FINISH";
                    await db.SaveChangesAsync();
                    transaction.Commit();

                    if (payFromWallet)
                    {
                        AgentLimitNotificationService.ScheduleCheckAfterWalletChange(Agent.User_ID, agentWalletBefore);
                    }

                    logger.Info("فاکتور اپلیکیشن " + Deposit.dw_TaxId + (payFromWallet ? " از کیف پول تائید شد" : " تائید شد"));
                    return result;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "خطا در تائید فاکتور اپلیکیشن " + depositId);
                    return ConfirmResult.Fail("تائید فاکتور با خطا مواجه شد");
                }
            }
        }

        /// <summary>
        /// کسر از کیف پول ربات تلگرام صاحب اشتراک و افزایش بدهی نماینده ،
        /// همان منطق callback AccpetWallet در BotController.
        /// موجودیت ها فقط در حافظه عوض می شوند ؛ ذخیره با تراکنش بیرونی است.
        /// </summary>
        private ConfirmResult ApplyWalletPayment(tbDepositWallet_Log Deposit, tbOrders Order, tbUsers Agent, tbLinkUserAndPlans PlanLink, tbLinks Link)
        {
            if (Link == null || Link.FK_TelegramUserID == null)
            {
                return ConfirmResult.Fail("این اشتراک به حساب ربات تلگرام متصل نیست");
            }

            var TelUser = Link.tbTelegramUsers;
            if (TelUser == null || TelUser.FK_User_ID != Agent.User_ID)
            {
                return ConfirmResult.Fail("این اشتراک به حساب ربات تلگرام متصل نیست");
            }

            var Price = Order.Order_Price ?? 0;
            var Wallet = TelUser.Tel_Wallet ?? 0;
            if (Wallet < Price)
            {
                return ConfirmResult.Fail("موجودی کیف پول کافی نیست");
            }

            if (Agent.Role == 3)
            {
                var Prices = Agent.tbLinkServerGroupWithUsers
                    .FirstOrDefault(s => s.FK_Group_Id == PlanLink.tbPlans.Group_Id);
                if (Prices == null)
                {
                    return ConfirmResult.Fail("امکان پرداخت از کیف پول در حال حاضر وجود ندارد لطفا با پشتیبانی ارتباط بگیرید");
                }

                var FinalPrice = (PlanLink.tbPlans.PlanMonth * Prices.PriceForMonth)
                               + (PlanLink.tbPlans.PlanVolume * Prices.PriceForGig)
                               + ((PlanLink.tbPlans.device_limit ?? 0) * Prices.PriceForUser);

                if (Agent.Wallet + FinalPrice > Agent.Limit)
                {
                    return ConfirmResult.Fail("امکان پرداخت از کیف پول در حال حاضر وجود ندارد لطفا با پشتیبانی ارتباط بگیرید");
                }

                Agent.Wallet += (int)FinalPrice;
            }
            else if (Agent.Role == 2)
            {
                if (Agent.Wallet + PlanLink.L_SellPrice > Agent.Limit)
                {
                    return ConfirmResult.Fail("امکان پرداخت از کیف پول در حال حاضر وجود ندارد لطفا با پشتیبانی ارتباط بگیرید");
                }

                Agent.Wallet += PlanLink.tbPlans.Price;
            }

            TelUser.Tel_Wallet = Wallet - Price;
            Order.FK_Tel_UserID = TelUser.Tel_UserID;
            Deposit.FK_TelegramUser_ID = TelUser.Tel_UserID;
            Deposit.dw_PayMethod = "ApiWallet";

            return new ConfirmResult { Success = true };
        }

        /// <summary>
        /// نماینده از روی پسوند نام اشتراک (name$random@AgentUsername) پیدا می شود ،
        /// چون فاکتور اپلیکیشن کاربر تلگرام ندارد که از آن به نماینده برسیم.
        /// </summary>
        private tbUsers ResolveAgent(string AccountName)
        {
            var Cut = AccountName.LastIndexOf('@');
            if (Cut < 0 || Cut == AccountName.Length - 1)
            {
                return null;
            }

            var Username = AccountName.Substring(Cut + 1);
            return db.tbUsers.FirstOrDefault(p => p.Username == Username);
        }

        /// <summary>ساخت اشتراک جدید روی v2board و ثبت آن در tbLinks</summary>
        private async Task<ConfirmResult> CreateSubscriptionAsync(MySqlEntities mySql, tbOrders Order, tbLinkUserAndPlans PlanLink, tbServers Server)
        {
            var Token = Guid.NewGuid().ToString().Split('-')[0]
                      + Guid.NewGuid().ToString().Split('-')[1]
                      + Guid.NewGuid().ToString().Split('-')[2];

            var Traffic = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));
            var Expire = DateTime.Now.AddDays((int)(Order.Month * 30)).ConvertDatetimeToSecond().ToString();
            var Created = DateTime.Now.ConvertDatetimeToSecond().ToString();

            var GroupId = 0;
            var Disc1 = new Dictionary<string, object>();
            Disc1.Add("@V2board", PlanLink.tbPlans.Plan_ID_V2);
            var reader = await mySql.GetDataAsync("select group_id from v2_plan where id = @V2board", Disc1);
            while (await reader.ReadAsync())
            {
                GroupId = reader.GetInt32("group_id");
            }
            reader.Close();

            var Disc2 = new Dictionary<string, object>();
            Disc2.Add("@FullName", Order.AccountName);
            Disc2.Add("@expired", Expire);
            Disc2.Add("@create", Created);
            Disc2.Add("@guid", Guid.NewGuid());
            Disc2.Add("@tran", Traffic);
            Disc2.Add("@grid", GroupId);
            Disc2.Add("@V2boardId", PlanLink.tbPlans.Plan_ID_V2);
            Disc2.Add("@token", Token);
            Disc2.Add("@passwrd", Guid.NewGuid());

            var DeviceLimitColumn = "";
            var DeviceLimitValue = "";
            var DeviceLimit = SubscriptionPackageHelper.ResolveDeviceLimitForV2(PlanLink.tbPlans);
            if (DeviceLimit.HasValue)
            {
                DeviceLimitColumn = ",device_limit";
                DeviceLimitValue = ",@device_limit";
                Disc2.Add("@device_limit", DeviceLimit.Value);
            }

            var Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at"
                      + DeviceLimitColumn
                      + ") VALUES (@FullName,@expired,@create,@guid,0,0,0,@tran,0,@grid,@V2boardId,@token,@passwrd,@create"
                      + DeviceLimitValue + ")";

            reader = await mySql.GetDataAsync(Query, Disc2);
            reader.Close();

            tbLinks NewLink = new tbLinks();
            NewLink.tbL_Email = Order.AccountName;
            NewLink.tbL_Token = Token;
            NewLink.FK_Server_ID = Server.ServerID;
            NewLink.tb_AutoRenew = false;
            SubscriptionReserveWarnHelper.ResetReserveWarnState(NewLink);
            RepositoryLinks.Insert(NewLink);

            Order.OrderStatus = "FINISH";
            Order.Tel_RenewedDate = DateTime.Now;

            return new ConfirmResult
            {
                Success = true,
                SubscriptionToken = Token,
                Message = "پرداخت تائید شد و اشتراک ساخته شد"
            };
        }

        /// <summary>
        /// تمدید اشتراک موجود. اگر بسته فعلی هنوز تمام نشده باشد سفارش رزرو می شود
        /// (OrderStatus = FOR_RESERVE) — همان رفتاری که مسیر ربات دارد.
        /// </summary>
        private async Task<ConfirmResult> RenewSubscriptionAsync(MySqlEntities mySql, tbOrders Order, tbLinkUserAndPlans PlanLink, tbLinks Link)
        {
            var Disc1 = new Dictionary<string, object>();
            Disc1.Add("@tbL_Email", Link.tbL_Email);

            var Found = false;
            var Ended = false;
            var reader = await mySql.GetDataAsync("select d,u,transfer_enable,expired_at from v2_user where email = @tbL_Email", Disc1);
            while (await reader.ReadAsync())
            {
                Found = true;
                Ended = SubscriptionPackageHelper.IsPackageEnded(
                    reader.GetInt64("transfer_enable"),
                    reader.GetInt64("d"),
                    reader.GetInt64("u"),
                    reader["expired_at"]);
            }
            reader.Close();

            if (!Found)
            {
                return ConfirmResult.Fail("اشتراک این سفارش روی سرور یافت نشد");
            }

            if (!Ended)
            {
                // بسته فعلی هنوز اعتبار دارد — بسته خریداری شده رزرو می ماند
                Order.OrderStatus = "FOR_RESERVE";
                return new ConfirmResult
                {
                    Success = true,
                    Message = "پرداخت تائید شد و بسته به صورت رزرو ثبت شد"
                };
            }

            var Traffic = Utility.ConvertGBToByte(Convert.ToInt64(Order.Traffic));
            var Expire = DateTime.Now.AddDays((int)(Order.Month * 30)).ConvertDatetimeToSecond().ToString();

            var Group = RepositoryServerGroups.Where(s => s.Group_Id == PlanLink.tbPlans.Group_Id).FirstOrDefault();
            if (Group == null)
            {
                return ConfirmResult.Fail("دسته بندی سرور این تعرفه یافت نشد");
            }

            var Disc2 = new Dictionary<string, object>();
            Disc2.Add("@DefaultPlanIdInV2board", PlanLink.tbPlans.Plan_ID_V2);
            Disc2.Add("@transfer_enable", Traffic);
            Disc2.Add("@exp", Expire);
            Disc2.Add("@email", Link.tbL_Email);
            Disc2.Add("@group_id", Group.V2_Group_Id);

            var DeviceLimitSet = "";
            var DeviceLimit = SubscriptionPackageHelper.ResolveDeviceLimitForV2(PlanLink.tbPlans);
            if (DeviceLimit.HasValue)
            {
                DeviceLimitSet = ",device_limit=@device_limit";
                Disc2.Add("@device_limit", DeviceLimit.Value);
            }

            var Query = "update v2_user set u=0,d=0,t=0,plan_id=@DefaultPlanIdInV2board,transfer_enable=@transfer_enable,expired_at=@exp,group_id=@group_id"
                      + DeviceLimitSet + " where email=@email";

            reader = await mySql.GetDataAsync(Query, Disc2);
            reader.Close();

            SubscriptionReserveWarnHelper.ResetReserveWarnState(Link);
            Link.tb_AutoRenew = false;

            Order.OrderStatus = "FINISH";
            Order.Tel_RenewedDate = DateTime.Now;

            return new ConfirmResult
            {
                Success = true,
                Message = "پرداخت تائید شد و اشتراک تمدید شد"
            };
        }
    }
}
