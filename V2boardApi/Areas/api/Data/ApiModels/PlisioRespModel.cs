using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.api.Data.ApiModels
{
    public class PlisioRespModel
    {
        public string status { get; set; }
        public string txn_id { get; set; }
        public string order_name { get; set; }
        public string order_number { get; set; }
    }
}