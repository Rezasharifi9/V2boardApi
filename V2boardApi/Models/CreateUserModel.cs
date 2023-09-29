using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models
{
    public class CreateUserModel
    {
        public string name { get; set; }
        public int plan_id { get; set; }
    }
}