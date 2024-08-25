using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.ServerGroups
{
    public class UserGroupsViewModel
    {
        public int Id { get; set; }
        public int groupId { get; set; }    
        public string GroupName { get; set; }
        public int PriceForGig { get; set; }
        public int PriceForMonth { get; set; }
    }
}