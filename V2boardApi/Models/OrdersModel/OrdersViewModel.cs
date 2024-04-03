using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.OrdersModel
{
    public class OrdersViewModel
    {
        public int CountReserve { get; set; }
        public int SalePrice { get; set;}
        public int CountNewAcc { get; set; }
        public int CountRenewAcc { get; set; }
        public List<tbOrders> Orders { get; set; }
    }
}