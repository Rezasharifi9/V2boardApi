using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models
{
    public class tbUserNotificationList_ViewModel
    {
        public int tbNoti_ID { get; set; }
        public string tbNoti_Title { get; set; }
        public string tbNoti_Text { get; set; }
        public string tbNoti_RegisterDate { get; set; }
        public string tbNoti_User { get; set; }
        public string tbNoti_UserSeen { get; set; }
        public int tbNoti_Status { get; set; }
        public string tbNoti_EndDate { get; set; }
    }
}