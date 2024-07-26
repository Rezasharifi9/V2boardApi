using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.UsersViewModels
{
    public class UserViewModel
    {
        public int id { get; set; } 
        public string profile { get; set; }
        public string username { get; set; }
        public int sellCount { get; set; }
        public string sumSellCount { get; set; }
        public short status { get; set; }
        public string limit { get; set; }
        public string used { get; set; }
        public short RobotStatus { get; set; }
    }
}