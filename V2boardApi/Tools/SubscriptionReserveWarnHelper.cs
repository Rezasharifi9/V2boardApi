using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace V2boardApi.Tools
{
    public static class SubscriptionReserveWarnHelper
    {
        public const int VolWarn2Gb = 1;
        public const int VolWarn1Gb = 2;
        public const int TimeWarn2Day = 4;
        public const int TimeWarn1Day = 8;

        public const double ThresholdVol2Gb = 2.0;
        public const double ThresholdVol1Gb = 1.0;

        public static void ResetReserveWarnState(tbLinks link)
        {
            if (link == null) return;
            link.tbL_ReserveWarnMask = 0;
            link.tbL_Warning = false;
        }

        public static int GetMask(tbLinks link)
        {
            return link?.tbL_ReserveWarnMask ?? 0;
        }

        public static List<ReserveWarnItem> GetPendingWarnings(
            tbLinks link,
            double remainingGb,
            DateTime? expireDate,
            bool isBanned,
            bool hasReservedPackage)
        {
            var pending = new List<ReserveWarnItem>();
            if (link == null || hasReservedPackage || link.tb_AutoRenew == true)
                return pending;

            var mask = GetMask(link);

            if (isBanned)
            {
                if (link.tbL_Warning != true)
                    pending.Add(new ReserveWarnItem(0, BuildBanMessage(link), setBanFlag: true));
                return pending;
            }

            if (remainingGb < ThresholdVol2Gb && (mask & VolWarn2Gb) == 0)
            {
                pending.Add(new ReserveWarnItem(VolWarn2Gb, BuildVolumeMessage(link, ThresholdVol2Gb)));
            }

            if (remainingGb < ThresholdVol1Gb && (mask & VolWarn1Gb) == 0)
            {
                pending.Add(new ReserveWarnItem(VolWarn1Gb, BuildVolumeMessage(link, ThresholdVol1Gb)));
            }

            if (expireDate.HasValue && expireDate.Value > DateTime.Now)
            {
                var remainingTime = expireDate.Value - DateTime.Now;
                if (remainingTime <= TimeSpan.FromDays(2) && (mask & TimeWarn2Day) == 0)
                    pending.Add(new ReserveWarnItem(TimeWarn2Day, BuildTimeMessage(link, 2)));

                if (remainingTime <= TimeSpan.FromDays(1) && (mask & TimeWarn1Day) == 0)
                    pending.Add(new ReserveWarnItem(TimeWarn1Day, BuildTimeMessage(link, 1)));
            }

            return pending;
        }

        public static void ApplySentFlags(tbLinks link, IEnumerable<ReserveWarnItem> sent)
        {
            if (link == null || sent == null) return;

            var mask = GetMask(link);
            foreach (var item in sent)
            {
                if (item.SetBanFlag)
                    link.tbL_Warning = true;
                else if (item.Flag > 0)
                    mask |= item.Flag;
            }
            link.tbL_ReserveWarnMask = mask;
        }

        public static string GetSubscriptionDisplayName(string email)
        {
            if (string.IsNullOrEmpty(email))
                return "-";

            var name = email.Split('@')[0];
            if (name.Contains("$"))
                name = name.Split('$')[0];
            return name;
        }

        private static string BuildVolumeMessage(tbLinks link, double thresholdGb)
        {
            var st = new StringBuilder();
            st.AppendLine("⚠️ <b>هشدار حجم</b>");
            st.AppendLine("");
            st.AppendLine("<b>اشتراک: " + GetSubscriptionDisplayName(link.tbL_Email) + "</b>");
            st.AppendLine("");
            if (thresholdGb <= ThresholdVol1Gb)
            {
                st.AppendLine("حجم باقی‌مانده اشتراکت کمتر از ۱ گیگابایت شده.");
                st.AppendLine("هرچه سریع‌تر از بخش تمدید، بسته رو رزرو کن تا بعد از اتمام بسته فعلی خودکار فعال بشه.");
            }
            else
            {
                st.AppendLine("حجم باقی‌مانده اشتراکت کمتر از ۲ گیگابایت شده.");
                st.AppendLine("اگر هنوز بسته رزرو نکردی، از بخش تمدید می‌تونی بسته رو رزرو کنی.");
            }
            return st.ToString();
        }

        private static string BuildTimeMessage(tbLinks link, int days)
        {
            var st = new StringBuilder();
            st.AppendLine("⚠️ <b>هشدار زمان</b>");
            st.AppendLine("");
            st.AppendLine("<b>اشتراک: " + GetSubscriptionDisplayName(link.tbL_Email) + "</b>");
            st.AppendLine("");
            st.AppendLine("کمتر از " + days + " روز از اشتراکت باقی مونده.");
            st.AppendLine("اگر بسته رزرو نداری، از بخش تمدید می‌تونی بسته رو رزرو کنی.");
            return st.ToString();
        }

        private static string BuildBanMessage(tbLinks link)
        {
            var st = new StringBuilder();
            st.AppendLine("<b>اشتراک: " + GetSubscriptionDisplayName(link.tbL_Email) + "</b>");
            st.AppendLine("");
            st.AppendLine("توسط ادمین مسدود شد؛ برای دانستن علت مسدودی به پشتیبانی پیام بده.");
            return st.ToString();
        }
    }

    public class ReserveWarnItem
    {
        public ReserveWarnItem(int flag, string message, bool setBanFlag = false)
        {
            Flag = flag;
            Message = message;
            SetBanFlag = setBanFlag;
        }

        public int Flag { get; }
        public string Message { get; }
        public bool SetBanFlag { get; }
    }
}
