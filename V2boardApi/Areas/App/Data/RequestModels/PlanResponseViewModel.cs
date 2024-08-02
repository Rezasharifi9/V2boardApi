using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.RequestModels
{
    public class PlanResponseViewModel
    {
        public int id { get; set; }
        public string PlanName { get; set; }
        public int DayesCount { get; set; }
        public int Traffic { get; set; }
        public string Price { get; set; }
        public int Status { get; set; }
        public string SpeedLimit { get; set; }
        public string Group_Name { get; set; }
    }
}