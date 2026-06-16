using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models
{
    public class UserInvoiceViewModel
    {
        public int invoice_id { get; set; }
        public string FullName { get; set; }
        public string Amount { get; set; }
        public string Date { get; set; }
        public string Desc { get; set; }

        public string Card_FullName { get; set; }
        public string Card_Number { get; set; }
        public string PayAmount { get; set; }
        public bool PayStatus { get; set; }
    }
}