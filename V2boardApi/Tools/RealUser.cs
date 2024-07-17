using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using V2boardBotApp;
using V2boardBotApp.Models;

namespace V2boardBot.Functions
{
    public static class RealUser
    {
        public static void SetUserStep(string UserUniq, string Step, Entities db, string botName ,string Data = null)
        {
            db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).First().Tel_Step = Step;
            if (Data != null)
            {
                db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).First().Tel_Data += Data;
            }
            db.SaveChanges();
        }
        public static void SetEmptyState(string UserUniq, Entities db, string botName)
        {
            db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).First().Tel_Step = null;
            db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).First().Tel_Data = null;
            db.SaveChanges();
        }


        public static void SetTraffic(string UserUniq, Entities db, int? Traffic, string botName)
        {
            db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).First().Tel_Traffic = Traffic;
            db.SaveChanges();
        }


        public static void SetMonth(string UserUniq, Entities db, int? Month, string botName)
        {
            db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).First().Tel_Monthes = Month;
            db.SaveChanges();
        }

        public static void SetUpdateMessageTime(string UserUniq, Entities db, DateTime time, string botName)
        {
            db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).First().Tel_UpdateMessage = time;
            db.SaveChanges();
        }

        public static void SetGetedAccountTest(string UserUniq, Entities db, string botName)
        {
            db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).First().Tel_GetedTestAccount = true;
            db.SaveChanges();
        }

    }
}