using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data
{
    public class DashboardViewModel
    {
        /// <summary>
        /// فروش از عمده فروش
        /// </summary>
        public double SaleFromMajor { get; set; }

        /// <summary>
        /// فروش امروز از عمده  فروش
        /// </summary>
        public double SaleFromMajorToday { get; set; }
        /// <summary>
        /// فروش امروز از ربات
        /// </summary>
        public double SaleFromBotToday { get; set; }
        /// <summary>
        /// درصد سود یا ضرر از عمده فروش
        /// </summary>
        public double InterestRatesFromMajor { get; set; }

        /// <summary>
        /// مبلغ فروش از ربات
        /// </summary>
        public double SaleFromBot { get; set; }
        /// <summary>
        /// درصد سود یا ضرر از ربات
        /// </summary>
        public double InterestRatesFromBot { get; set; }

        /// <summary>
        /// تعداد کاربر افزایش یافته 
        /// </summary>
        public double CountUserFromMajor { get; set; }
        /// <summary>
        /// درصد تعداد کاربران افزایش یافته
        /// </summary>
        public double InterestUserRatesFromMajor { get; set; }

        /// <summary>
        /// تعداد کاربر افزایش یافته از ربات 
        /// </summary>
        public double CountUserFromBot { get; set; }
        /// <summary>
        /// درصد تعداد کاربران افزایش یافته از ربات
        /// </summary>
        public double InterestUserRatesBot { get; set; }

    }
}