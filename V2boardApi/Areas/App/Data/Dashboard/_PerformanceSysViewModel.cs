using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.Dashboard
{
    public class _PerformanceSysViewModel
    {
        public int id { get; set; }
        public List<double> chart_data { get; set; }
        public int active_option { get; set; }
        public List<string> chart_xData { get; set; }
    }
}