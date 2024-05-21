using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Tools
{
    public class UserLog
    {
        public static bool InsertLog(int Traffic,int Month,int Cost_Traffic,int Cost_Month,int FK_User_ID,string AccountName)
        {
            try
            {
                tbLogAccount lg = new tbLogAccount();
                lg.La_Traffic = Traffic;
                lg.La_Month = Month;
                lg.La_Cost = (Traffic * Cost_Traffic) + (Month * Cost_Month);
                lg.La_CreateDate = DateTime.Now;
                lg.FK_User_ID = FK_User_ID;
                lg.La_AccountName = AccountName;

                var Repo = new Repository<tbLogAccount>();
                Repo.Insert(lg);
                Repo.Save();
                return true;
            }
            catch(Exception ex)
            {
                ExceptionHandling.InsertLog(ex, FK_User_ID);
                return false;
            }
        }
    }
}