using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.PlanViewModels
{
    public class RequestPlanViewModel
    {
        public int? id { get; set; }

        [Required(ErrorMessage = "نام تعرفه را وارد کنید")]
        [StringLength(100, ErrorMessage = "نام تعرفه حداکثر می تواند 100 کاراکتر باشد")]
        public string planName { get; set; }

        [Required(ErrorMessage = "ترافیک را وارد کنید")]
        [Range(0.1,1000,ErrorMessage ="مقدار ترافیک نمی تواند کمتر از 0.1 و بیشتر از 1000 گیگ باشد")]
        public int planTraffic { get; set; }

        [Range(0,360,ErrorMessage ="زمان تعرفه نمی تواند بزرگتر از 360 روز باشد")]
        public Nullable<int> planTime { get; set; }

        [Required(ErrorMessage ="قیمت تعرفه را وارد کنید")]
        public string planPrice { get; set; }

        [Range(1,10000,ErrorMessage ="محدودیت سرعت نمی تواند بزرگتر 1,000 مگابیت باشد")]
        public Nullable<short> planSpeed { get; set; }
        public Nullable<short> planDevicelimit { get; set; }
        [Required(ErrorMessage = "لطفا دسته بندی را انتخاب کنید")]
        public Nullable<int> planGroup { get; set; }
    }
}