using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models
{
    public class UpdateUserModel
    {
        public int AccountID { get; set; }
        public int Plan_ID { get; set; }
    }
}