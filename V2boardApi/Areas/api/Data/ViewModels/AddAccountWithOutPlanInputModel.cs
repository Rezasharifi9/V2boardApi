using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    public class AddAccountWithOutPlanInputModel
    {
        public string AccountName { get; set; }
        public int Traffic { get; set; }

        public int Month { get; set; }
    }
}