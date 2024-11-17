using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using V2boardBotApp;
using V2boardBotApp.Models;

namespace V2boardBot.Functions
{
    public static class RealUser
    {
        public static async Task SetUserStep(string UserUniq, string Step, Entities db, string botName ,string Data = null)
        {
            var res = await db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).FirstAsync();
            if (Data != null)
            {
                res.Tel_Data += Data;
            }
            res.Tel_Step = Step;
            await db.SaveChangesAsync();
        }
        public static bool SetUserStepWithoutAsync(string UserUniq, string Step, Entities db, string botName, string Data = null)
        {
            var res = db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).FirstOrDefault();
            if (Data != null)
            {
                res.Tel_Data += Data;
            }
            res.Tel_Step = Step;
            db.SaveChanges();
            return true;
        }
        public static async Task  SetEmptyState(string UserUniq, Entities db, string botName)
        {
            var res = await db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).FirstAsync();
            res.Tel_Step = null;
            res.Tel_Data = null;
            await db.SaveChangesAsync();
        }


        public static async Task SetTraffic(string UserUniq, Entities db, int? Traffic, string botName)
        {
            var res = await db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).FirstAsync();
            res.Tel_Traffic = Traffic;
            await db.SaveChangesAsync();
        }


        public static async Task SetMonth(string UserUniq, Entities db, int? Month, string botName)
        {
            var res = await db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).FirstAsync();
            res.Tel_Monthes = Month;
            await db.SaveChangesAsync();
        }

        public static async Task SetUpdateMessageTime(string UserUniq, Entities db, DateTime time, string botName)
        {
            var res  = await db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).FirstAsync();

            res.Tel_UpdateMessage = time;

            await db.SaveChangesAsync();
        }

        public static async Task SetGetedAccountTest(string UserUniq, Entities db, string botName)
        {
            var res = await db.tbTelegramUsers.Where(p => p.Tel_UniqUserID == UserUniq && p.tbUsers.Username == botName).FirstAsync();
            res.Tel_GetedTestAccount = true;
            await db.SaveChangesAsync();
        }

    }
}