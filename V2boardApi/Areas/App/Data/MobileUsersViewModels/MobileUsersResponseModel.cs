namespace V2boardApi.Areas.App.Data.MobileUsersViewModels
{
    /// <summary>یک سطر جدول کاربران موبایل در پنل</summary>
    public class MobileUsersResponseModel
    {
        public int Id { get; set; }

        /// <summary>سازنده و مدل گوشی ، مثل Samsung SM-A546E</summary>
        public string Device { get; set; }

        /// <summary>نماینده ای که این دستگاه زیرمجموعه اوست</summary>
        public string Agent { get; set; }

        public string AndroidId { get; set; }
        public string AppVersion { get; set; }
        public string AndroidVersion { get; set; }
        public string Language { get; set; }
        public string RegisterDate { get; set; }
        public string LastSeenDate { get; set; }

        /// <summary>توکن FCM ثبت شده و اجازه نمایش نوتیفیکیشن هر دو برقرار است</summary>
        public bool PushReady { get; set; }

        public bool Rooted { get; set; }
        public bool IsActive { get; set; }

        /// <summary>تعداد فاکتورهای ساخته شده با این دستگاه</summary>
        public int FactorCount { get; set; }
    }

    /// <summary>جزئیات یک دستگاه در مودال</summary>
    public class MobileUserDetailsModel
    {
        public int Id { get; set; }
        public string AndroidId { get; set; }
        public string Agent { get; set; }
        public string BusinessName { get; set; }

        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Device { get; set; }
        public string Product { get; set; }
        public string Hardware { get; set; }

        public string AndroidVersion { get; set; }
        public int? Sdk { get; set; }
        public string AppVersion { get; set; }
        public int? VersionCode { get; set; }
        public string PackageName { get; set; }

        public string Language { get; set; }
        public string Country { get; set; }
        public string Timezone { get; set; }

        public string Screen { get; set; }
        public int? Density { get; set; }

        public bool NotificationEnabled { get; set; }
        public bool HasFirebaseToken { get; set; }
        public bool Rooted { get; set; }
        public bool IsActive { get; set; }

        public string RegisterDate { get; set; }
        public string LastSeenDate { get; set; }
        public string LastIp { get; set; }

        public int FactorCount { get; set; }
        public int OrderCount { get; set; }
        public string TotalPaid { get; set; }
    }
}
