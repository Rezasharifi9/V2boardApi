using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.MysqlModel
{
    public class UserMySqlModel
    {
        public int id { get; set; }
        public string email { get; set; }
        public string password { get; set; }
    }
}