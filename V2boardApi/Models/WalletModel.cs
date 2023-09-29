using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models
{
    public class WalletModel
    {
        public int Payable_debt { get; set; }
        public int Wallet { get; set; }
        public int PayLimit { get; set; }
    }
}