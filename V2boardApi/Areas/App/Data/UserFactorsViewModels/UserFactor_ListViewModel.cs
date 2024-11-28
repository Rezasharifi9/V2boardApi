using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.UserFactors
{
    public class UserFactor_ListViewModel
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string CreateTime { get; set; }
        public string Role { get; set; }
        public string Price { get; set; }
        public string Desc { get; set; }
        public int Status { get; set; }
        public bool IsExistsFile { get; set; }
        
    }
}