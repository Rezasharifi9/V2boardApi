using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.RequestModels
{
    public class UserLogResponseModel
    {
        public int id { get; set; }
        public string SubName { get; set; }
        public string CreateDate { get; set; }
        public string Event { get; set; }
        public string SellPrice { get; set; }
        public string Plan { get; set; }
    }
}