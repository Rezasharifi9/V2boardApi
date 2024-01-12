using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models
{
    public class UserAndPlans
    {
        public tbUsers tbUser { get; set; }
        public List<tbPlans> tbPlans { get; set; }
        public List<tbPlans> AllPlans { get; set; }
    }
}