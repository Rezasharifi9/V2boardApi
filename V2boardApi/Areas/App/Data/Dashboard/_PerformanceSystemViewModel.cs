using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.Dashboard
{
    public class _PerformanceSystemViewModel
    {
        public double Precent_MasterAgent { get; set; }
        public double Precent_Agent { get; set; }
        public double Precent_CEO { get; set; }
        public double Precent_bot { get; set; }

        public int Active_Sub { get; set; }
        public int New_Sub { get; set; }
        public string Traffic_down { get; set; }
        public string Traffic_up { get; set; }
    }
}