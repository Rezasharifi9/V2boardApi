using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using V2boardApi.Tools;

namespace V2boardApi.Models.V2boardModel
{
    public class UsagesModel
    {
        public int user_id { get; set; }
        public string server_rate { get; set; }
        public long u { get; set; }
        public long d { get; set; }
        public long updated_at { get; set; }
        public double TotalUseage { get { return Utility.ConvertByteToGB(u + d); } }
    }
}