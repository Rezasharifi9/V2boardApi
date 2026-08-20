using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace V2boardApi.Tools
{
    /// <summary>
    /// دسته‌بندی اعلانات پنل. دسته در دیتابیس ذخیره نمی‌شود و هنگام نمایش
    /// از روی عنوان اعلان تشخیص داده می‌شود (عناوین از ثابت‌های همین کلاس می‌آیند).
    /// </summary>
    public enum PanelNotificationCategory
    {
        General = 0,
        Settlement = 1,
        Limit = 2,
        Block = 3,
        Payment = 4
    }

    /// <summary>
    /// سرویس مرکزی اعلانات پنل نماینده.
    ///
    /// هر پیامی که از طریق <see cref="SettlementService.SendAgentTelegramMessage"/> به نماینده
    /// ارسال می‌شود، به‌صورت خودکار در بخش اعلانات پنل هم ثبت می‌گردد؛ بنابراین برای اضافه‌شدن
    /// یک اعلان جدید به پنل کافی است پیام تلگرام از همان مسیر ارسال شود.
    ///
    /// نقاطی که خودشان اعلان پنل را با منطق dedup اختصاصی می‌سازند (مثل مراحل تسویه در
    /// SettlementService) با mirrorToPanel: false از میرور خودکار خارج می‌شوند.
    /// </summary>
    public static class PanelNotificationService
    {
        /// <summary>مدت نمایش اعلان در پنل (روز).</summary>
        public const int DefaultVisibleDays = 7;

        /// <summary>بازه جلوگیری از ثبت اعلان تکراری با عنوان و متن یکسان (ساعت).</summary>
        public const int DefaultDedupHours = 24;

        // ── عناوین ثابت اعلانات ──────────────────────────────────────────────
        public const string TitlePreWarning = "هشدار تسویه بدهی (قبل از موعد)";
        public const string TitleDueDay = "سررسید تسویه بدهی";
        public const string TitleOverdue = "پایان مهلت پرداخت بدهی";
        public const string TitlePreBlock = "هشدار مسدودسازی اشتراک‌ها";
        public const string TitleBlocked = "مسدودسازی اشتراک‌های زیرمجموعه";
        public const string TitleLimitWarning = "هشدار سقف اعتبار اشتراک";
        public const string TitleUnblocked = "فعال‌سازی مجدد اشتراک‌ها";
        public const string TitleFactorPaid = "ثبت فاکتور پرداخت";
        public const string TitleGeneral = "اطلاعیه سیستم";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly Regex HtmlTagRegex = new Regex("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex BlankLineRegex = new Regex(@"(\r?\n\s*){2,}", RegexOptions.Compiled);

        // خطوط خطاب که در پنل ارزشی ندارند و از ابتدای پیام تلگرام حذف می‌شوند
        private static readonly string[] GreetingLines =
        {
            "نماینده گرامی",
            "کاربر گرامی",
            "همکار گرامی"
        };

        #region ساخت اعلان

        /// <summary>
        /// ثبت اعلان پنل برای یک نماینده. سازنده اعلان، ادمین بالادست نماینده در نظر گرفته می‌شود.
        /// این متد فقط رکورد را به context اضافه می‌کند؛ ذخیره‌سازی بر عهده فراخوان است.
        /// </summary>
        public static void Create(
            Entities db,
            int agentUserId,
            int creatorUserId,
            string title,
            string text,
            int visibleDays = DefaultVisibleDays)
        {
            if (db == null || agentUserId <= 0)
                return;

            if (string.IsNullOrWhiteSpace(title))
                title = TitleGeneral;

            if (string.IsNullOrWhiteSpace(text))
                return;

            var noti = new tbNotifications
            {
                tbNoti_FK_User_ID = creatorUserId > 0 ? creatorUserId : agentUserId,
                tbNoti_Title = Truncate(title, 200),
                tbNoti_Text = text,
                tbNoti_RegisterDate = DateTime.Now,
                tbNoti_Status = 1,
                tbNoti_EndDate = DateTime.Now.AddDays(visibleDays)
            };

            noti.tbNotificationUser.Add(new tbNotificationUser
            {
                tbNotiUser_FK_User_ID = agentUserId,
                tbNotiUser_Seen = false
            });

            db.tbNotifications.Add(noti);
        }

        /// <summary>
        /// اعلان را فقط در صورتی می‌سازد که اعلانی با همان عنوان برای این نماینده
        /// بعد از <paramref name="since"/> ثبت نشده باشد.
        /// </summary>
        public static void EnsureByTitle(
            Entities db,
            int agentUserId,
            int creatorUserId,
            string title,
            string text,
            DateTime since,
            int visibleDays = DefaultVisibleDays)
        {
            if (ExistsByTitle(db, agentUserId, title, since))
                return;

            Create(db, agentUserId, creatorUserId, title, text, visibleDays);
        }

        public static bool ExistsByTitle(Entities db, int agentUserId, string title, DateTime since)
        {
            if (db == null || agentUserId <= 0 || string.IsNullOrWhiteSpace(title))
                return false;

            return db.tbNotificationUser.Any(nu =>
                nu.tbNotiUser_FK_User_ID == agentUserId
                && nu.tbNotifications.tbNoti_Title == title
                && nu.tbNotifications.tbNoti_RegisterDate >= since);
        }

        /// <summary>
        /// ثبت اعلان با جلوگیری از تکرار: اگر اعلانی با همین عنوان و همین متن در
        /// <paramref name="dedupHours"/> ساعت گذشته برای این نماینده ثبت شده باشد، چیزی ساخته نمی‌شود.
        /// این متد context خودش را باز می‌کند و تغییرات را ذخیره می‌کند.
        /// </summary>
        public static bool CreateDeduplicated(
            int agentUserId,
            int creatorUserId,
            string title,
            string text,
            int dedupHours = DefaultDedupHours,
            int visibleDays = DefaultVisibleDays)
        {
            if (agentUserId <= 0 || string.IsNullOrWhiteSpace(text))
                return false;

            try
            {
                using (var db = new Entities())
                {
                    var threshold = DateTime.Now.AddHours(-Math.Abs(dedupHours));

                    var duplicate = db.tbNotificationUser.Any(nu =>
                        nu.tbNotiUser_FK_User_ID == agentUserId
                        && nu.tbNotifications.tbNoti_Title == title
                        && nu.tbNotifications.tbNoti_Text == text
                        && nu.tbNotifications.tbNoti_RegisterDate >= threshold);

                    if (duplicate)
                        return false;

                    Create(db, agentUserId, creatorUserId, title, text, visibleDays);
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "خطا در ثبت اعلان پنل برای کاربر " + agentUserId);
                return false;
            }
        }

        #endregion

        #region تبدیل پیام تلگرام به اعلان پنل

        /// <summary>
        /// متن پیام تلگرام را به متن مناسب اعلان پنل تبدیل می‌کند:
        /// تگ‌های HTML، خطوط خطاب («نماینده گرامی») و خطوط خالی اضافه حذف می‌شوند.
        /// </summary>
        public static string BuildTextFromTelegramMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            var plain = HtmlTagRegex.Replace(message, string.Empty);

            var lines = plain
                .Replace("\r\n", "\n")
                .Split('\n')
                .Select(l => l.Trim())
                .ToList();

            // حذف خطوط خطاب و خطوط خالی ابتدای پیام
            while (lines.Count > 0 && (lines[0].Length == 0 || IsGreetingLine(lines[0])))
                lines.RemoveAt(0);

            var meaningful = lines.Where(l => l.Length > 0).ToList();
            if (meaningful.Count == 0)
                return null;

            var text = string.Join(Environment.NewLine, meaningful);
            return BlankLineRegex.Replace(text, Environment.NewLine).Trim();
        }

        /// <summary>
        /// اگر عنوانی برای اعلان مشخص نشده باشد، اولین جمله معنادار پیام به‌عنوان عنوان استفاده می‌شود.
        /// </summary>
        public static string BuildTitleFromText(string panelText)
        {
            if (string.IsNullOrWhiteSpace(panelText))
                return TitleGeneral;

            var firstLine = panelText
                .Replace("\r\n", "\n")
                .Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 0);

            if (string.IsNullOrWhiteSpace(firstLine))
                return TitleGeneral;

            firstLine = StripLeadingSymbols(firstLine);

            return firstLine.Length == 0 ? TitleGeneral : Truncate(firstLine, 80);
        }

        private static bool IsGreetingLine(string line)
        {
            var cleaned = StripLeadingSymbols(line);
            return GreetingLines.Any(g => cleaned.Equals(g, StringComparison.Ordinal));
        }

        /// <summary>حذف ایموجی و نشانه‌های ابتدای خط تا رسیدن به اولین حرف/رقم.</summary>
        private static string StripLeadingSymbols(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var index = 0;
            while (index < value.Length && !char.IsLetterOrDigit(value, index))
                index++;

            return index >= value.Length ? string.Empty : value.Substring(index).Trim();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength).TrimEnd() + "…";
        }

        #endregion

        #region دسته‌بندی و ظاهر

        public static PanelNotificationCategory GetCategory(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return PanelNotificationCategory.General;

            switch (title)
            {
                case TitleBlocked:
                case TitlePreBlock:
                    return PanelNotificationCategory.Block;

                case TitleLimitWarning:
                    return PanelNotificationCategory.Limit;

                case TitlePreWarning:
                case TitleDueDay:
                case TitleOverdue:
                    return PanelNotificationCategory.Settlement;

                case TitleFactorPaid:
                case TitleUnblocked:
                    return PanelNotificationCategory.Payment;
            }

            // اعلانات دستی ادمین یا عناوین مشتق‌شده از متن پیام
            if (title.Contains("مسدود"))
                return PanelNotificationCategory.Block;
            if (title.Contains("سقف اعتبار"))
                return PanelNotificationCategory.Limit;
            if (title.Contains("تسویه") || title.Contains("بدهی"))
                return PanelNotificationCategory.Settlement;
            if (title.Contains("فاکتور") || title.Contains("پرداخت"))
                return PanelNotificationCategory.Payment;

            return PanelNotificationCategory.General;
        }

        /// <summary>کلاس آیکون Tabler متناسب با دسته اعلان.</summary>
        public static string GetIconClass(PanelNotificationCategory category)
        {
            switch (category)
            {
                case PanelNotificationCategory.Block: return "ti ti-lock";
                case PanelNotificationCategory.Limit: return "ti ti-chart-pie";
                case PanelNotificationCategory.Settlement: return "ti ti-alert-triangle";
                case PanelNotificationCategory.Payment: return "ti ti-receipt";
                default: return "ti ti-message-2-exclamation";
            }
        }

        /// <summary>کلاس رنگ پس‌زمینه آواتار متناسب با شدت اعلان.</summary>
        public static string GetColorClass(PanelNotificationCategory category)
        {
            switch (category)
            {
                case PanelNotificationCategory.Block: return "bg-label-danger";
                case PanelNotificationCategory.Limit: return "bg-label-warning";
                case PanelNotificationCategory.Settlement: return "bg-label-warning";
                case PanelNotificationCategory.Payment: return "bg-label-success";
                default: return "bg-label-info";
            }
        }

        public static string GetCategoryLabel(PanelNotificationCategory category)
        {
            switch (category)
            {
                case PanelNotificationCategory.Block: return "مسدودسازی";
                case PanelNotificationCategory.Limit: return "سقف اعتبار";
                case PanelNotificationCategory.Settlement: return "تسویه بدهی";
                case PanelNotificationCategory.Payment: return "مالی";
                default: return "اطلاعیه";
            }
        }

        public static string GetIconClass(string title)
        {
            return GetIconClass(GetCategory(title));
        }

        public static string GetColorClass(string title)
        {
            return GetColorClass(GetCategory(title));
        }

        public static string GetCategoryLabel(string title)
        {
            return GetCategoryLabel(GetCategory(title));
        }

        #endregion
    }
}
