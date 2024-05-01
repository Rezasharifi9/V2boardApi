using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardBot.Functions
{
    public static class RealUser
    {
        public static void SetUserStep(string UserUniq, string Step, Entities db)
        {
            db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq).First().Tel_Step = Step;
            db.SaveChanges();
        }
    }
}