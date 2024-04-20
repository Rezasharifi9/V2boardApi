using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.AdminModel
{
    public class AccountsViewModel
    {
        public string V2boardUsername { get; set; }
        public string UsedVolume { get; set; }
        public string RemainingVolume { get; set; }
        public double TotalVolume { get; set; }
        public string ExpireDate { get; set; }
        public string State { get; set; }
    }
}