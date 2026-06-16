using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.RequestModels
{
    public class UserFactorResponseModel
    {
        public int factor_id { get; set; }
        public string PayDate { get; set; }
        public int PayStatus { get; set; }
        public string Price { get; set; }
    }
}