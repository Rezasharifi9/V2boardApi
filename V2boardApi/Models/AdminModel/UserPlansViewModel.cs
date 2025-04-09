using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.AdminModel
{
    public class UserPlansViewModel
    {
        public int UserPlan_ID { get; set; }
        public int Plan_ID { get; set; }
        public string UserPlan_Name { get; set; }
        public string UserPlan_Price { get; set; }
    }
}