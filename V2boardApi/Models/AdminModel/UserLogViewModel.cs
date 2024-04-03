using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.AdminModel
{
    public class UserLogViewModel
    {
        public int CountCreated { get; set; }
        public int SumSale { get; set; }
        public int CountCreatedFormLastPay { get; set; }
        public int SumSaleFromLastPay { get; set; }
        public List<tbLogs> Logs { get; set; }
    }
}