using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.PlanViewModels
{
    public class RequestPlanViewModel
    {
        [Required(ErrorMessage = "نام تعرفه را وارد کنید")]
        [StringLength(20, ErrorMessage = "نام تعرفه حداکثر می تواند 20 کاراکتر باشد")]
        public string planName { get; set; }

        [Required(ErrorMessage = "ترافیک را وارد کنید")]
        [Range(0.1,1000,ErrorMessage ="مقدار ترافیک نمی تواند کمتر از 0.1 و بیشتر از 1000 گیگ باشد")]
        public int planTraffic { get; set; }

        [Range(0,360,ErrorMessage ="زمان تعرفه نمی تواند بزرگتر از 360 روز باشد")]
        public Nullable<int> planTime { get; set; }

        [Required(ErrorMessage ="قیمت تعرفه را وارد کنید")]
        public string planPrice { get; set; }

        [Required(ErrorMessage ="لطفا گروه مجوز را انتخاب کنید")]
        public int planGroup { get; set; }

        [Range(1,10000,ErrorMessage ="محدودیت سرعت نمی تواند بزرگتر 10,000 مگابیت باشد")]
        public Nullable<short> planSpeed { get; set; }
    }
}