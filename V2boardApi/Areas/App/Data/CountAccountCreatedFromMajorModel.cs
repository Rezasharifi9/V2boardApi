using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using V2boardApi.Models.AdminModel;

namespace V2boardApi.Areas.App.Data
{
    public class CountAccountCreatedFromMajorModel
    {
        public List<string> Users { get; set; }
        public List<int> Prices { get; set;}
    }
}