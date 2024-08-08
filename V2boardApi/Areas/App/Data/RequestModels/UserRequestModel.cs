using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.RequestModels
{
    public class UserRequestModel
    {

        public int userId { get; set; } 
        [Display(Name ="نام کاربری")]
        [MaxLength(20,ErrorMessage ="نام کاربری نمی تواند بیشتر از 20 کاراکتر باشد")]
        public string userUsername { get; set; }
        [MaxLength(20, ErrorMessage = "رمز عبور نمی تواند بیشتر از 20 کاراکتر باشد")]
        [MinLength(4, ErrorMessage = "رمز عبور نمی تواند کمتر از 4 کاراکتر باشد")]
        public string userPassword { get; set; }
        public string userLimit { get; set; }

        [MaxLength(30, ErrorMessage = "نام و نام خانوادگی نمی تواند بیشتر از 30 کاراکتر باشد")]
        [MinLength(4, ErrorMessage = "نام و نام خانوادگی نمی تواند کمتر از 4 کاراکتر باشد")]
        public string userFullname { get; set; }
        [MaxLength(50, ErrorMessage = "ایمیل نمی تواند بیشتر از 50 کاراکتر باشد")]
        public string userEmail { get; set; }
        [MaxLength(12,ErrorMessage = "شماره همراه می تواند حداکثر 12 رقم باشد")]
        [MinLength(11,ErrorMessage = "شماره همراه می تواند حداقل 11 رقم باشد")]
        public string userContact { get; set; }

        [MinLength(4, ErrorMessage = "آیدی تلگرام نمی تواند کمتر از 4 کاراکتر باشد")]
        [MaxLength(50, ErrorMessage = "آیدی تلگرام نمی تواند بیشتر از 50 کاراکتر باشد")]
        public string userTelegramid { get; set; }
    }
}