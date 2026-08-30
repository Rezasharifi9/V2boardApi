using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace V2boardApi.Tools
{
    /// <summary>
    /// تسویه بدهی نماینده بر اساس فاصله از آخرین فاکتور پرداخت‌شده (نه تقویم هفتگی/ماهانه).
    /// </summary>
    public static class SettlementService
    {
        public const string RemarksFlag = "[SETTLEMENT_BLOCK]";
        public const int DefaultDaysAfterLastPayment = 15;
        public const int DefaultPreWarningDays = 2;
        public const int DefaultBlockGraceDays = 2;
        public const int OverdueReminderHours = 6;

        // یادآوری درخواست تایید مسدودسازی به ادمین هر ۲ روز تا زمان تصمیم‌گیری
        public const int BlockApprovalResendDays = 2;

        // پیشوند callback_data دکمه‌های تایید/رد مسدودسازی در ربات ادمین
        public const string ApproveBlockCallbackPrefix = "SettleBlockYes";
        public const string DeclineBlockCallbackPrefix = "SettleBlockNo";

        // مدت نمایش هشدارهای تسویه در بخش اعلانات پنل (روز)
        private const int PanelNotificationVisibleDays = PanelNotificationService.DefaultVisibleDays;

        // عناوین ثابت اعلانات پنل — برای جلوگیری از ساخت تکراری در هر چرخه تسویه.
        // منبع اصلی در PanelNotificationService است تا دسته‌بندی و آیکون اعلان هماهنگ بماند.
        private const string NotiTitlePreWarning = PanelNotificationService.TitlePreWarning;
        private const string NotiTitleDueDay = PanelNotificationService.TitleDueDay;
        private const string NotiTitleOverdue = PanelNotificationService.TitleOverdue;
        private const string NotiTitlePreBlock = PanelNotificationService.TitlePreBlock;
        private const string NotiTitleBlocked = PanelNotificationService.TitleBlocked;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static int GetDaysAfterLastPayment(tbUsers user)
        {
            return Math.Max(1, user.Settlement_DayOfMonth ?? DefaultDaysAfterLastPayment);
        }

        public static int GetPreWarningDays(tbUsers user)
        {
            var days = GetDaysAfterLastPayment(user);
            var pre = user.Settlement_DayOfWeek ?? DefaultPreWarningDays;
            return Math.Max(0, Math.Min(days, pre));
        }

        public static int GetBlockGraceDays(tbUsers user)
        {
            return Math.Max(0, user.Settlement_BlockGraceDays ?? DefaultBlockGraceDays);
        }

        public static DateTime? GetLastPaidFactorDate(Entities db, tbUsers agent)
        {
            return db.tbUserFactors
                .Where(f => f.FK_User_ID == agent.User_ID && f.tbUf_Status == 3 && f.tbUf_CreateTime.HasValue)
                .OrderByDescending(f => f.tbUf_CreateTime)
                .Select(f => f.tbUf_CreateTime)
                .FirstOrDefault();
        }

        public static DateTime GetSettlementAnchor(Entities db, tbUsers agent)
        {
            var lastPaid = GetLastPaidFactorDate(db, agent);
            if (lastPaid.HasValue)
                return lastPaid.Value.Date;

            if (agent.Settlement_StartDate.HasValue)
                return agent.Settlement_StartDate.Value.Date;

            return DateTime.Now.Date;
        }

        /// <summary>
        /// موعد تسویه بعدی نماینده. اگر تسویه بدهی برای این کاربر تنظیم نشده باشد (یا کاربر ادمین باشد)
        /// null برمی‌گرداند تا در پنل چیزی نمایش داده نشود.
        /// </summary>
        public static DateTime? GetNextDueDate(Entities db, tbUsers user)
        {
            if (user == null || !user.Settlement_Enabled || user.Role == 1)
                return null;

            return GetSettlementAnchor(db, user).AddDays(GetDaysAfterLastPayment(user));
        }

        public static List<string> GetNetworkUsernames(tbUsers agent, Entities db)
        {
            var result = new List<string>();
            var queue = new Queue<int>();
            queue.Enqueue(agent.User_ID);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                var current = db.tbUsers.FirstOrDefault(u => u.User_ID == id);
                if (current == null || string.IsNullOrEmpty(current.Username))
                    continue;

                if (!result.Contains(current.Username))
                    result.Add(current.Username);

                var children = db.tbUsers
                    .Where(u => u.Parent_ID == id && u.Status == true)
                    .Select(u => u.User_ID)
                    .ToList();

                foreach (var childId in children)
                    queue.Enqueue(childId);
            }

            return result;
        }

        /// <summary>
        /// آیا این نماینده یا یکی از بالادستی‌هایش توسط مدیریت (یا تسویه) مسدود شده است.
        /// </summary>
        public static bool IsAgentOrAncestorBlocked(tbUsers agent, Entities db)
        {
            if (agent == null || db == null)
                return false;

            var seen = new HashSet<int>();
            var current = agent;
            while (current != null && seen.Add(current.User_ID))
            {
                if (current.Settlement_IsBlocked)
                    return true;
                if (!current.Parent_ID.HasValue || current.Parent_ID.Value <= 0)
                    break;
                var parentId = current.Parent_ID.Value;
                current = db.tbUsers.FirstOrDefault(u => u.User_ID == parentId);
            }

            return false;
        }

        public static bool HasAdminBlockFlag(string remarks)
        {
            return !string.IsNullOrEmpty(remarks) &&
                   remarks.IndexOf(RemarksFlag, StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// ارسال پیام به نماینده. توکن ربات همیشه از کاربر ادمین (Role = 1) خوانده می‌شود،
        /// نه از ربات خود نماینده — تا نمایندگانی که ربات اختصاصی ندارند هم پیام دریافت کنند.
        ///
        /// همین پیام به‌صورت خودکار در بخش اعلانات پنل نماینده هم ثبت می‌شود؛ ثبت اعلان پنل
        /// مستقل از موفقیت ارسال تلگرام انجام می‌گیرد تا نماینده‌ای که ربات را بلاک کرده یا
        /// تلگرامش ثبت نشده هم اعلان را در پنل ببیند.
        /// </summary>
        /// <param name="mirrorToPanel">
        /// اگر false باشد اعلان پنل ثبت نمی‌شود — برای نقاطی که خودشان اعلان پنل را
        /// با منطق dedup اختصاصی می‌سازند (مراحل تسویه).
        /// </param>
        /// <param name="panelTitle">عنوان اعلان پنل؛ اگر خالی باشد از متن پیام ساخته می‌شود.</param>
        /// <param name="panelDedupHours">بازه جلوگیری از ثبت اعلان تکراری با همان عنوان و متن.</param>
        public static async Task<bool> SendAgentTelegramMessage(
            tbUsers agent,
            string message,
            bool mirrorToPanel = true,
            string panelTitle = null,
            int panelDedupHours = PanelNotificationService.DefaultDedupHours)
        {
            if (agent == null)
                return false;

            if (mirrorToPanel)
                MirrorMessageToPanel(agent, message, panelTitle, panelDedupHours);

            var alertType = !string.IsNullOrWhiteSpace(panelTitle)
                ? panelTitle
                : PanelNotificationService.TitleGeneral;
            var sent = false;
            string chatId = null;
            string error = null;

            try
            {
                using (var db = new Entities())
                {
                    var admin = GetOwnerAdmin(db, agent);
                    if (admin == null)
                    {
                        error = "ادمین سیستم یافت نشد";
                    }
                    else
                    {
                        var adminBotSetting = admin.tbBotSettings?.FirstOrDefault();
                        if (adminBotSetting == null || string.IsNullOrEmpty(adminBotSetting.Bot_Token))
                        {
                            error = "توکن ربات ادمین تنظیم نشده است";
                        }
                        else
                        {
                            var telegramId = db.tbUsers
                                .Where(u => u.User_ID == agent.User_ID)
                                .Select(u => u.TelegramID)
                                .FirstOrDefault() ?? agent.TelegramID;

                            chatId = TelegramNotifyHelper.ResolveChatIdFromProfile(
                                db, telegramId, adminBotSetting.Bot_ID);
                            if (string.IsNullOrEmpty(chatId))
                            {
                                error = "شناسه تلگرام نماینده ثبت نشده است";
                            }
                            else
                            {
                                var cached = BotManager.GetBot(admin.Username);
                                var client = cached != null && string.Equals(cached.Token, adminBotSetting.Bot_Token, StringComparison.Ordinal)
                                    ? cached.Client
                                    : new TelegramBotClient(adminBotSetting.Bot_Token);

                                string sendError = null;
                                sent = await TelegramNotifyHelper.TrySendMessageAsync(
                                    client, chatId, message, onFailure: e => sendError = e);
                                if (!sent)
                                    error = string.IsNullOrWhiteSpace(sendError) ? "ارسال پیام تلگرام ناموفق بود" : sendError;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sent = false;
                error = ex.Message;
            }

            AlertSendLogService.Write(agent, chatId, alertType, message, sent, error);
            return sent;
        }

        /// <summary>
        /// ثبت نسخه پنلی پیام تلگرام. خطای ثبت اعلان هرگز مانع ارسال پیام تلگرام نمی‌شود.
        /// </summary>
        private static void MirrorMessageToPanel(tbUsers agent, string message, string panelTitle, int dedupHours)
        {
            try
            {
                var panelText = PanelNotificationService.BuildTextFromTelegramMessage(message);
                if (string.IsNullOrWhiteSpace(panelText))
                    return;

                var title = string.IsNullOrWhiteSpace(panelTitle)
                    ? PanelNotificationService.BuildTitleFromText(panelText)
                    : panelTitle;

                int creatorId;
                using (var db = new Entities())
                {
                    creatorId = GetOwnerAdmin(db, agent)?.User_ID ?? agent.User_ID;
                }

                PanelNotificationService.CreateDeduplicated(
                    agent.User_ID, creatorId, title, panelText, dedupHours);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "خطا در میرور پیام تلگرام به اعلانات پنل نماینده " + agent.User_ID);
            }
        }

        public static async Task BlockAgentSubscriptions(tbUsers agent, Entities db)
        {
            if (agent.tbServers == null || string.IsNullOrEmpty(agent.tbServers.ConnectionString))
                return;

            var usernames = GetNetworkUsernames(agent, db);
            using (var mysql = new MySqlEntities(agent.tbServers.ConnectionString))
            {
                await mysql.OpenAsync();
                foreach (var username in usernames)
                {
                    var query = "UPDATE v2_user SET banned = 1, remarks = CONCAT(IFNULL(remarks,''), @flag) " +
                                "WHERE email LIKE @pattern AND (remarks IS NULL OR remarks NOT LIKE @flagLike)";
                    var parameters = new Dictionary<string, object>
                    {
                        { "@flag", RemarksFlag },
                        { "@flagLike", "%" + RemarksFlag + "%" },
                        { "@pattern", "%@" + username }
                    };
                    using (var reader = await mysql.GetDataAsync(query, parameters))
                    {
                        while (await reader.ReadAsync()) { }
                    }
                }
                await mysql.CloseAsync();
            }
        }

        public static async Task UnblockAgentSubscriptions(tbUsers agent, Entities db)
        {
            if (agent.tbServers == null || string.IsNullOrEmpty(agent.tbServers.ConnectionString))
                return;

            var usernames = GetNetworkUsernames(agent, db);
            using (var mysql = new MySqlEntities(agent.tbServers.ConnectionString))
            {
                await mysql.OpenAsync();
                foreach (var username in usernames)
                {
                    var query = "UPDATE v2_user SET banned = 0, remarks = REPLACE(IFNULL(remarks,''), @flag, '') " +
                                "WHERE email LIKE @pattern AND remarks LIKE @flagLike";
                    var parameters = new Dictionary<string, object>
                    {
                        { "@flag", RemarksFlag },
                        { "@flagLike", "%" + RemarksFlag + "%" },
                        { "@pattern", "%@" + username }
                    };
                    using (var reader = await mysql.GetDataAsync(query, parameters))
                    {
                        while (await reader.ReadAsync()) { }
                    }
                }
                await mysql.CloseAsync();
            }
        }

        public static void ResetSettlementWarnings(tbUsers agent)
        {
            agent.Settlement_LastPreWarning = null;
            agent.Settlement_LastOverdueWarning = null;
            agent.Settlement_LastDueDayWarning = null;
            agent.Settlement_BlockApprovalPending = false;
            agent.Settlement_LastBlockApprovalSent = null;
        }

        #region تاییدیه مسدودسازی توسط ادمین (Role = 1)

        private static tbUsers GetOwnerAdmin(Entities db, tbUsers agent)
        {
            tbUsers admin = null;
            if (agent.FK_Server_ID.HasValue)
            {
                admin = db.tbUsers
                    .Include(u => u.tbBotSettings)
                    .FirstOrDefault(u => u.Role == 1 && u.Status == true && u.FK_Server_ID == agent.FK_Server_ID);
            }

            if (admin == null)
            {
                admin = db.tbUsers
                    .Include(u => u.tbBotSettings)
                    .FirstOrDefault(u => u.Role == 1 && u.Status == true);
            }

            return admin;
        }

        /// <summary>
        /// ساخت اعلان در بخش Notification پنل برای نماینده (هشدار قبل از موعد / قبل از مسدودسازی).
        /// سازنده اعلان ادمین در نظر گرفته می‌شود و گیرنده خود نماینده است.
        /// </summary>
        private static void CreateAgentPanelNotification(Entities db, tbUsers agent, string title, string text)
        {
            var creatorId = GetOwnerAdmin(db, agent)?.User_ID ?? agent.User_ID;

            PanelNotificationService.Create(
                db, agent.User_ID, creatorId, title, text, PanelNotificationVisibleDays);
        }

        /// <summary>
        /// اعلان پنل را فقط در صورتی می‌سازد که در چرخه تسویه جاری اعلانی با همین عنوان
        /// برای این نماینده ثبت نشده باشد. عمداً به فلگ‌های ارسال تلگرام وابسته نیست تا اگر
        /// مرحله‌ای قبلاً بدون ثبت اعلان پردازش شده باشد، در اجرای بعدی جبران شود.
        /// </summary>
        private static void EnsureAgentPanelNotification(
            Entities db, tbUsers agent, string title, string text, DateTime cycleStart)
        {
            if (PanelNotificationService.ExistsByTitle(db, agent.User_ID, title, cycleStart))
                return;

            CreateAgentPanelNotification(db, agent, title, text);
        }

        /// <summary>
        /// فاکتور در انتظار پرداخت تسویه را برمی‌گرداند؛ اگر وجود نداشته باشد می‌سازد.
        /// مبلغ = بدهی (تومان) × ۱۰ + سه رقم رندوم، تا پرداخت کارت‌به‌کارت به‌صورت خودکار
        /// با همین فاکتور مچ شود (TransactionHanderService بر اساس مبلغ دقیق مچ می‌کند).
        /// اگر فاکتور در انتظار پرداختی وجود داشته باشد همان برگردانده می‌شود تا مبلغی که
        /// قبلاً به نماینده اعلام شده تغییر نکند.
        /// </summary>
        private static double? EnsureSettlementFactor(Entities db, tbUsers agent)
        {
            if (agent.Wallet <= 0)
                return null;

            var pending = db.tbUserFactors
                .Where(f => f.FK_User_ID == agent.User_ID && f.tbUf_Status == 1 && f.tbUf_Value.HasValue)
                .OrderByDescending(f => f.tbUf_CreateTime)
                .FirstOrDefault();

            if (pending != null)
                return pending.tbUf_Value.Value;

            // seed مستقل تا در یک اجرای تایمر برای چند نماینده مبلغ تکراری تولید نشود
            var random = new Random(Guid.NewGuid().GetHashCode());
            double payAmount = 0;

            for (var attempt = 0; attempt < 15; attempt++)
            {
                var candidate = (agent.Wallet * 10) + random.Next(1, 999);

                // مبلغ باید بین فاکتورهای در انتظار پرداخت یکتا باشد وگرنه مچ خودکار پرداخت اشتباه می‌شود
                var taken = db.tbUserFactors.Any(f => f.tbUf_Status == 1 && f.tbUf_Value == candidate);
                if (!taken)
                {
                    payAmount = candidate;
                    break;
                }
            }

            if (payAmount <= 0)
            {
                logger.Warn("تولید مبلغ یکتا برای فاکتور تسویه نماینده " + agent.Username + " ناموفق بود");
                return null;
            }

            db.tbUserFactors.Add(new tbUserFactors
            {
                FK_User_ID = agent.User_ID,
                tbUf_CreateTime = DateTime.Now,
                tbUf_Status = 1,
                tbUf_Value = payAmount,
                tbUf_Description = "فاکتور خودکار تسویه بدهی"
            });

            logger.Info("فاکتور خودکار تسویه به مبلغ " + payAmount + " برای نماینده " + agent.Username + " ساخته شد");
            return payAmount;
        }

        private static string BuildFactorPaymentText(double? factorValue)
        {
            if (!factorValue.HasValue)
                return null;

            return "🧾 مبلغ قابل پرداخت: " + factorValue.Value.ConvertToMony() + " ریال"
                 + Environment.NewLine
                 + "⚠️ لطفاً دقیقاً همین مبلغ را واریز کنید؛ سه رقم آخر برای شناسایی خودکار پرداخت شما است.";
        }

        private static string BuildBlockApprovalMessage(tbUsers agent, Entities db, DateTime now)
        {
            var displayName = !string.IsNullOrWhiteSpace(agent.FullName) ? agent.FullName
                : !string.IsNullOrWhiteSpace(agent.BussinesTitle) ? agent.BussinesTitle
                : agent.Username;

            var lastPaid = GetLastPaidFactorDate(db, agent);
            var lastPaidLabel = lastPaid.HasValue ? lastPaid.Value.ConvertDateTimeToShamsi2() : "—";
            var daysSince = Math.Max(0, (now.Date - GetSettlementAnchor(db, agent)).Days);

            var sb = new StringBuilder();
            sb.AppendLine("🚨 <b>درخواست تایید مسدودسازی نماینده</b>");
            sb.AppendLine("");
            sb.AppendLine("👤 <b>نام نماینده:</b> " + displayName);
            sb.AppendLine("🆔 <b>نام کاربری نماینده:</b> " + agent.Username);
            sb.AppendLine("💰 <b>مقدار بدهی:</b> " + agent.Wallet.ConvertToMony() + " تومان");

            var pendingFactor = db.tbUserFactors
                .Where(f => f.FK_User_ID == agent.User_ID && f.tbUf_Status == 1 && f.tbUf_Value.HasValue)
                .OrderByDescending(f => f.tbUf_CreateTime)
                .FirstOrDefault();
            if (pendingFactor != null)
                sb.AppendLine("🧾 <b>فاکتور در انتظار پرداخت:</b> " + pendingFactor.tbUf_Value.Value.ConvertToMony() + " ریال");

            sb.AppendLine("📅 <b>تاریخ آخرین فاکتور پرداخت‌شده:</b> " + lastPaidLabel);
            sb.AppendLine("⏳ <b>مدت بدون پرداخت:</b> " + daysSince + " روز");
            sb.AppendLine("");
            sb.AppendLine("در صورت تایید، تمامی اشتراک‌های زیرمجموعه این نماینده مسدود می‌شوند.");
            sb.AppendLine("اگر تصمیمی نگیرید یا رد کنید، این پیام هر " + BlockApprovalResendDays + " روز یک‌بار تکرار می‌شود.");
            return sb.ToString();
        }

        /// <summary>
        /// ارسال درخواست تایید مسدودسازی به ربات ادمین (Role = 1) همراه با دکمه‌های تایید/رد.
        /// </summary>
        private static async Task<bool> SendBlockApprovalRequestToAdmin(tbUsers agent, Entities db, DateTime now)
        {
            try
            {
                var admin = GetOwnerAdmin(db, agent);
                if (admin == null)
                {
                    logger.Warn("درخواست تایید مسدودسازی ارسال نشد: ادمین (Role=1) یافت نشد — نماینده " + agent.Username);
                    AlertSendLogService.Write(
                        null, "ادمین", null, "درخواست تایید مسدودسازی",
                        BuildBlockApprovalMessage(agent, db, now), false, "ادمین سیستم یافت نشد");
                    return false;
                }

                var botSetting = admin.tbBotSettings?.FirstOrDefault();
                if (botSetting == null || string.IsNullOrEmpty(botSetting.Bot_Token) || botSetting.AdminBot_ID <= 0)
                {
                    logger.Warn("درخواست تایید مسدودسازی ارسال نشد: توکن ربات یا شناسه تلگرام ادمین تنظیم نشده");
                    AlertSendLogService.Write(
                        admin, null, "درخواست تایید مسدودسازی",
                        BuildBlockApprovalMessage(agent, db, now), false, "توکن ربات یا شناسه تلگرام ادمین تنظیم نشده است");
                    return false;
                }

                var cached = BotManager.GetBot(admin.Username);
                var client = cached != null ? cached.Client : new TelegramBotClient(botSetting.Bot_Token);

                var keyboard = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                {
                    new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("✅ تایید و مسدودسازی", ApproveBlockCallbackPrefix + "%" + agent.User_ID),
                        InlineKeyboardButton.WithCallbackData("❌ انصراف", DeclineBlockCallbackPrefix + "%" + agent.User_ID)
                    }
                });

                var message = BuildBlockApprovalMessage(agent, db, now);
                var chatId = botSetting.AdminBot_ID.ToString();
                string sendError = null;
                var sent = await TelegramNotifyHelper.TrySendMessageAsync(
                    client, chatId, message, ParseMode.Html, keyboard, onFailure: e => sendError = e);

                AlertSendLogService.Write(
                    admin,
                    chatId,
                    "درخواست تایید مسدودسازی",
                    message,
                    sent,
                    sent ? null : (string.IsNullOrWhiteSpace(sendError) ? "ارسال پیام تلگرام ناموفق بود" : sendError));

                return sent;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ارسال درخواست تایید مسدودسازی نماینده " + agent.Username);
                AlertSendLogService.Write(
                    null,
                    agent != null ? AlertSendLogService.FormatRecipientName(agent) : "—",
                    null,
                    "درخواست تایید مسدودسازی",
                    "خطا در ارسال درخواست تایید مسدودسازی نماینده " + (agent?.Username ?? "—"),
                    false,
                    ex.Message);
                return false;
            }
        }

        /// <summary>
        /// اجرای مسدودسازی پس از تایید ادمین از داخل ربات. اشتراک‌ها را مسدود و نماینده را مطلع می‌کند.
        /// </summary>
        public static async Task ApproveAndBlockAsync(tbUsers agent, Entities db)
        {
            await BlockAgentSubscriptions(agent, db);

            agent.Settlement_IsBlocked = true;
            agent.Settlement_BlockApprovalPending = false;
            await db.SaveChangesAsync();

            var text = "بدهی شما تسویه نشده و تمامی اشتراک‌های زیرمجموعه شما مسدود گردید. " +
                       "لطفاً نسبت به پرداخت بدهی اقدام کنید؛ پس از ثبت پرداخت، اشتراک‌ها خودکار فعال می‌شوند.";
            CreateAgentPanelNotification(db, agent, NotiTitleBlocked, text);
            await db.SaveChangesAsync();

            var msg = new StringBuilder();
            msg.AppendLine("🚫 نماینده گرامی");
            msg.AppendLine("");
            msg.AppendLine("تمامی اشتراک‌های زیرمجموعه شما مسدود گردید.");
            msg.AppendLine("لطفاً نسبت به پرداخت بدهی اقدام کنید؛ پس از ثبت پرداخت، اشتراک‌ها خودکار فعال می‌شوند.");
            // اعلان پنل بالاتر با CreateAgentPanelNotification ثبت شد
            await SendAgentTelegramMessage(agent, msg.ToString(), mirrorToPanel: false, panelTitle: NotiTitleBlocked);
        }

        /// <summary>
        /// رد درخواست مسدودسازی توسط ادمین. اشتراک‌ها مسدود نمی‌شوند؛
        /// تایمر یادآوری ری‌ست می‌شود تا پس از N روز دوباره درخواست ارسال گردد.
        /// </summary>
        public static async Task DeclineBlockAsync(tbUsers agent, Entities db, DateTime now)
        {
            agent.Settlement_BlockApprovalPending = true;
            agent.Settlement_LastBlockApprovalSent = now;
            await db.SaveChangesAsync();
        }

        #endregion

        public static async Task OnAgentPaymentConfirmed(tbUsers agent, Entities db)
        {
            var wasBlocked = agent.Settlement_IsBlocked;

            if (wasBlocked)
                await UnblockAgentSubscriptions(agent, db);

            agent.Settlement_IsBlocked = false;
            ResetSettlementWarnings(agent);

            var lastPaid = GetLastPaidFactorDate(db, agent);
            if (lastPaid.HasValue)
                agent.Settlement_StartDate = lastPaid.Value.Date;

            await db.SaveChangesAsync();

            var msg = new StringBuilder();
            msg.AppendLine("نماینده گرامی");
            msg.AppendLine("");
            if (wasBlocked)
                msg.AppendLine("✅ پرداخت بدهی شما ثبت شد و تمامی اشتراک‌های زیرمجموعه مجدداً فعال گردید.");
            else
                msg.AppendLine("✅ پرداخت فاکتور شما ثبت شد. مهلت تسویه بعدی از امروز محاسبه می‌شود.");

            var dueDate = GetSettlementAnchor(db, agent).AddDays(GetDaysAfterLastPayment(agent));
            msg.AppendLine("📅 موعد تسویه بعدی: " + dueDate.ToString("yyyy/MM/dd", CultureInfo.GetCultureInfo("fa-IR")));

            var paymentTitle = wasBlocked
                ? PanelNotificationService.TitleUnblocked
                : PanelNotificationService.TitleFactorPaid;
            await SendAgentTelegramMessage(agent, msg.ToString(), panelTitle: paymentTitle);
        }

        public static async Task ProcessAgent(tbUsers agent, Entities db, DateTime now)
        {
            if (!agent.Settlement_Enabled || agent.Status != true)
            {
                if (agent.Settlement_IsBlocked)
                {
                    await UnblockAgentSubscriptions(agent, db);
                    agent.Settlement_IsBlocked = false;
                    ResetSettlementWarnings(agent);
                    await db.SaveChangesAsync();
                }
                return;
            }

            var anchor = GetSettlementAnchor(db, agent);
            var daysUntilDue = GetDaysAfterLastPayment(agent);
            var preWarningDays = GetPreWarningDays(agent);
            var blockGraceDays = GetBlockGraceDays(agent);

            var dueDate = anchor.AddDays(daysUntilDue);
            var preWarningDate = dueDate.AddDays(-preWarningDays);
            var blockDate = dueDate.AddDays(blockGraceDays);
            var dueEnd = dueDate.Date.AddDays(1).AddSeconds(-1);

            var dueLabel = dueDate.ToString("yyyy/MM/dd", CultureInfo.GetCultureInfo("fa-IR"));

            if (now < preWarningDate)
                return;

            // در هر مرحله ابتدا فاکتور و اعلان پنل تضمین می‌شوند (idempotent و مستقل از فلگ‌های تلگرام)،
            // سپس پیام تلگرام با گیت مخصوص خودش ارسال می‌شود.

            // ── هشدار قبل از موعد ──
            if (now.Date >= preWarningDate.Date && now.Date < dueDate.Date)
            {
                var remaining = (dueDate.Date - now.Date).Days;
                var payText = BuildFactorPaymentText(EnsureSettlementFactor(db, agent));

                var notiText = remaining + " روز تا موعد تسویه (" + dueLabel + ") باقی مانده است. لطفاً نسبت به پرداخت بدهی اقدام کنید.";
                if (payText != null)
                    notiText += " " + payText.Replace(Environment.NewLine, " ");

                EnsureAgentPanelNotification(db, agent, NotiTitlePreWarning, notiText, anchor);
                await db.SaveChangesAsync();

                if (!agent.Settlement_LastPreWarning.HasValue ||
                    agent.Settlement_LastPreWarning.Value.Date < preWarningDate.Date)
                {
                    var msg = new StringBuilder();
                    msg.AppendLine("⚠️ نماینده گرامی");
                    msg.AppendLine("");
                    msg.AppendLine(remaining + " روز تا موعد تسویه (" + dueLabel + ") باقی مانده است.");
                    msg.AppendLine("لطفاً در اسرع وقت نسبت به پرداخت بدهی اقدام کنید.");
                    if (payText != null)
                    {
                        msg.AppendLine("");
                        msg.AppendLine(payText);
                    }

                    await SendAgentTelegramMessage(agent, msg.ToString(), mirrorToPanel: false, panelTitle: NotiTitlePreWarning);
                    agent.Settlement_LastPreWarning = now;
                    await db.SaveChangesAsync();
                }
            }

            // ── روز سررسید ──
            if (now.Date == dueDate.Date)
            {
                var payText = BuildFactorPaymentText(EnsureSettlementFactor(db, agent));

                var notiText = "سررسید تسویه شما (" + dueLabel + ") فرا رسیده است. لطفاً نسبت به پرداخت بدهی اقدام کنید.";
                if (payText != null)
                    notiText += " " + payText.Replace(Environment.NewLine, " ");

                EnsureAgentPanelNotification(db, agent, NotiTitleDueDay, notiText, anchor);
                await db.SaveChangesAsync();

                if (!agent.Settlement_LastDueDayWarning.HasValue ||
                    agent.Settlement_LastDueDayWarning.Value.Date < dueDate.Date)
                {
                    var msg = new StringBuilder();
                    msg.AppendLine("📅 نماینده گرامی");
                    msg.AppendLine("");
                    msg.AppendLine("سررسید تسویه شما (" + dueLabel + ") فرا رسیده است.");
                    msg.AppendLine("لطفاً در اسرع وقت نسبت به پرداخت بدهی اقدام کنید.");
                    if (payText != null)
                    {
                        msg.AppendLine("");
                        msg.AppendLine(payText);
                    }

                    await SendAgentTelegramMessage(agent, msg.ToString(), mirrorToPanel: false, panelTitle: NotiTitleDueDay);
                    agent.Settlement_LastDueDayWarning = now;
                    await db.SaveChangesAsync();
                }
            }

            // ── پس از سررسید (یادآوری هر ۶ ساعت، اعلان پنل یک‌بار در هر چرخه) ──
            if (now > dueEnd && now < blockDate)
            {
                var payText = BuildFactorPaymentText(EnsureSettlementFactor(db, agent));

                var notiText = "مهلت پرداخت بدهی شما به اتمام رسیده است. لطفاً در اسرع وقت نسبت به پرداخت اقدام کنید.";
                if (payText != null)
                    notiText += " " + payText.Replace(Environment.NewLine, " ");

                EnsureAgentPanelNotification(db, agent, NotiTitleOverdue, notiText, anchor);
                await db.SaveChangesAsync();

                if (!agent.Settlement_LastOverdueWarning.HasValue ||
                    (now - agent.Settlement_LastOverdueWarning.Value).TotalHours >= OverdueReminderHours)
                {
                    var msg = new StringBuilder();
                    msg.AppendLine("❌ نماینده گرامی");
                    msg.AppendLine("");
                    msg.AppendLine("مهلت پرداخت شما به اتمام رسیده است.");
                    msg.AppendLine("لطفاً در اسرع وقت نسبت به پرداخت بدهی اقدام کنید.");
                    if (payText != null)
                    {
                        msg.AppendLine("");
                        msg.AppendLine(payText);
                    }

                    await SendAgentTelegramMessage(agent, msg.ToString(), mirrorToPanel: false, panelTitle: NotiTitleOverdue);
                    agent.Settlement_LastOverdueWarning = now;
                    await db.SaveChangesAsync();
                }
            }

            // ── مسدودسازی: فقط پس از تایید ادمین (Role=1) در ربات ──
            // در صورت عدم تصمیم یا رد، هر BlockApprovalResendDays روز درخواست دوباره ارسال می‌شود.
            if (now >= blockDate && !agent.Settlement_IsBlocked)
            {
                var payText = BuildFactorPaymentText(EnsureSettlementFactor(db, agent));

                var notiText = "بدهی شما تسویه نشده است و اشتراک‌های زیرمجموعه شما در آستانه مسدودسازی قرار دارند. لطفاً فوراً نسبت به پرداخت بدهی اقدام کنید.";
                if (payText != null)
                    notiText += " " + payText.Replace(Environment.NewLine, " ");

                EnsureAgentPanelNotification(db, agent, NotiTitlePreBlock, notiText, anchor);
                await db.SaveChangesAsync();

                var firstRequest = !agent.Settlement_BlockApprovalPending;
                var dueForRequest = firstRequest
                    || !agent.Settlement_LastBlockApprovalSent.HasValue
                    || (now - agent.Settlement_LastBlockApprovalSent.Value).TotalDays >= BlockApprovalResendDays;

                if (dueForRequest)
                {
                    if (firstRequest)
                    {
                        var agentMsg = new StringBuilder();
                        agentMsg.AppendLine("🚨 نماینده گرامی");
                        agentMsg.AppendLine("");
                        agentMsg.AppendLine("بدهی شما تسویه نشده و اشتراک‌های زیرمجموعه شما در آستانه مسدودسازی قرار دارند.");
                        agentMsg.AppendLine("لطفاً فوراً نسبت به پرداخت بدهی اقدام کنید.");
                        if (payText != null)
                        {
                            agentMsg.AppendLine("");
                            agentMsg.AppendLine(payText);
                        }

                        await SendAgentTelegramMessage(agent, agentMsg.ToString(), mirrorToPanel: false, panelTitle: NotiTitlePreBlock);
                    }

                    await SendBlockApprovalRequestToAdmin(agent, db, now);

                    agent.Settlement_BlockApprovalPending = true;
                    agent.Settlement_LastBlockApprovalSent = now;
                    await db.SaveChangesAsync();
                }
            }
        }

        public static async Task ProcessAllAgents()
        {
            try
            {
                using (var db = new Entities())
                {
                    var agents = db.tbUsers
                        .Include(u => u.tbServers)
                        .Include(u => u.tbBotSettings)
                        .Where(u => u.Settlement_Enabled && u.Status == true && (u.Role == 2 || u.Role == 3 || u.Role == 4))
                        .ToList();

                    var now = DateTime.Now;
                    foreach (var agent in agents)
                    {
                        try
                        {
                            await ProcessAgent(agent, db, now);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "خطا در پردازش تسویه نماینده " + agent.Username);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در اجرای سرویس تسویه");
            }
        }
    }
}
