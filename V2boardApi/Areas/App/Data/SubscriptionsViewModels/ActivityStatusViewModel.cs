using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.SubscriptionsViewModels
{
    public class ActivityStatusViewModel
    {
        public int total_users { get; set; }
        public int online_users { get; set; }
        public int banned_users { get; set; }
        public int inactive_users { get; set; }
    }
}