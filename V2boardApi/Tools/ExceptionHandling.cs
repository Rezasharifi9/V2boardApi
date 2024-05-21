using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace V2boardApi.Tools
{
    public static class ExceptionHandling
    {
        public static bool InsertLog(Exception ex,int FK_User_ID)
        {
            string Message = "";

            tbExpLog log = new tbExpLog();

            if (ex.Message != null)
            {
                Message += " | Message : " + ex.Message;
            }
            if (ex.InnerException != null)
            {
                Message += " | InnerException : " + ex.InnerException.Message;
            }
            Message += " | Trace : " + ex.StackTrace;
            log.exl_Description = Message;
            log.exl_MethodName = HttpContext.Current.Request.Url.ToString();
         
            log.FK_User_ID = FK_User_ID;
            log.exl_CreateDatetime = DateTime.Now;

            var Repository = new Repository<tbExpLog>();
            Repository.Insert(log);
            Repository.Save();
            return true;


        }
    }
}