using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models
{
    public class BanUserModel
    {
        public int AccountID { get; set; }
        public bool Status { get; set; }
    }
}