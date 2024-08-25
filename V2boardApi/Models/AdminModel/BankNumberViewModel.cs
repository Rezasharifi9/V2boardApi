using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.AdminModel
{
    public class BankNumberViewModel
    {
        public int Card_ID { get; set; }
        public string CardNumber { get; set; }
        public string SmsNumberOfCard { get; set;}
        public string NameOfCard { get; set;}
        public string phoneNumber { get; set;}
        public int Status { get; set; }
    }
}