using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models
{
    public class Plans
    {
        public int id { get; set; }
        public string transfer_enable { get; set; }
        public string name { get; set; }
        public int show { get; set; }
        public int Time { get; set; }
        public string content { get; set; }
        public string month_price { get; set; }
        public string quarter_price { get; set; }
    }
}