using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.AdminDashboard
{
    public class TopAgentsViewModel
    {
        public List<int> Sells { get; set; }
        public List<string> Agents { get; set; }
    }
}