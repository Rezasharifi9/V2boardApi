using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;

namespace V2boardApi.Controllers
{
    public class AdminController : ApiController
    {



        private V2boardSiteEntities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public AdminController()
        {
            db = new V2boardSiteEntities();
            RepositoryUser = new Repository<tbUsers>(db);

        }

        /// <summary>
        /// تابع قفل کردن اکانت عمده فروش یا باز کردن اکانت
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public IHttpActionResult BanOrUnBanUser(string username)
        {
            var Token = Request.Headers.Authorization;
            if (Token != null)
            {
                if (!string.IsNullOrEmpty(Token.Scheme))
                {
                    var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true && p.Role == 1).FirstOrDefault();
                    if (User != null)
                    {
                        var BanUser = RepositoryUser.table.Where(p => p.Username == username && p.FK_Server_ID == User.FK_Server_ID).FirstOrDefault();
                        if (BanUser != null)
                        {
                            if ((bool)BanUser.Status)
                            {
                                BanUser.Status = false;
                            }
                            else
                            {
                                BanUser.Status = true;
                            }
                            RepositoryUser.Save();
                            return Ok(new { status = true, result = "وضعیت کاربر با موفقیت تغییر کرد" });
                        }
                        else
                        {
                            return Ok(new { status = false, result = "این کاربر در حوزه اختیارات شما یافت نشد" });
                        }
                    }
                    else
                    {
                        return Ok(new { status = false, result = "کاربر گرامی شما ادمین نیستید" });
                    }
                }
                else
                {
                    return Ok(new { status = false, result = "توکن خالی است لطفا توکن را وارد کنید" });
                }
            }
            else
            {
                return Ok(new { status = false, result = "توکن خالی است لطفا توکن را وارد کنید" });
            }
        }


    }
}
