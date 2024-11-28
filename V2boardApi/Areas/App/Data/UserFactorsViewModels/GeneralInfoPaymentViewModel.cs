using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.UserFactorsViewModels
{
    public class GeneralInfoPaymentViewModel
    {
        public int AgentUsers { get; set; }
        public int Factors { get; set; }
        public string Payments { get; set; }
        public string DebtAgents { get; set; }
    }
}