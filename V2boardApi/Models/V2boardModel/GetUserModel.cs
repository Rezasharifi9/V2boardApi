using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.V2boardModel
{
    public class GetUserModel
    {
        public int id { get; set; }
        public string email { get; set; }
        public long t { get; set; }
        public long u { get; set; }
        public long d { get; set; }
        public long transfer_enable { get; set; }
        public int banned { get; set; }
        public string plan_name { get; set; }
        public string subscribe_url { get; set; }
        public Nullable<long> expired_at { get; set; }

    }
}