using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.AdminModel
{
    public class UsersModel
    {
        public int id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Status { get; set; }
        public string Email { get; set; }
        public int? Limit { get; set; }
        public int? Debt { get; set; }
        public string TelegramID { get; set; }
        public string BussinesTitle { get; set; }
        public long? CardNumber { get; set; }
    }
}