using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.UsersViewModels
{
    public class UserViewModel
    {
        public int id { get; set; } 
        public string profile { get; set; }
        public string username { get; set; }
        public string fullName { get; set; }
        public int sellCount { get; set; }
        public string sumSellCount { get; set; }
        public short status { get; set; }
        public string limit { get; set; }
        public string used { get; set; }
        public double walletValue { get; set; }
        public short RobotStatus { get; set; }
        /// <summary>اولویت نمایش: ۰ = عبور از سقف، ۱ = بالای ۸۰٪، ۲ = عادی</summary>
        public int sortPriority { get; set; }
        public int role { get; set; }
        public int parentId { get; set; }
        public string parentUsername { get; set; }
        public bool telegramActive { get; set; }
        public bool isBlocked { get; set; }
        public string lastPaymentDate { get; set; }
        public long lastPaymentSort { get; set; }
        public int daysUnpaid { get; set; }
    }
}