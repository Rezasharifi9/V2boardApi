using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.PaymentLinksViewModel
{
    public class listPaymentLinksViewModel
    {
        public int Id { get; set; }
        public string Hash { get; set; }
        public string Authority { get; set; }
        public string Amount { get; set; }
        public string Description { get; set; }
        public string CreateDate { get; set; }
        public short Status { get; set; }
        public string PayWebLink { get; set; }
        public string PayTelLink { get; set; }
    }
}