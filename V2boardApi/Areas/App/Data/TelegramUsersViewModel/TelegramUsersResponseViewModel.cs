using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.TelegramUsersViewModel
{
    public class TelegramUsersResponseViewModel
    {
        public int id { get; set; }
        public string Profile { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string SumBuy { get; set; }
        public int Status { get; set; }
        public string Wallet { get; set; }
        public int Invited { get; set; }
        public string InviteUser { get; set; }
    }
}