using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.OrdersViewModels
{
    public class OrderResponseViewModel
    {
        public string SubName { get; set; }
        public string UserCreator { get; set; }
        public string Plan { get; set; }
        public string CreateDate { get; set; }
        public string Price { get; set; }
        public string ActiveDate { get; set; }
        public int Status { get; set; }
    }
}