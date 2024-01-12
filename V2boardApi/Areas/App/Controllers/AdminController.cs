using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using V2boardApi.Models.AdminModel;
using V2boardApi.Models.MysqlModel;
using WebGrease;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Web.Security;
using Microsoft.Ajax.Utilities;

namespace V2boardApi.Areas.App.Controllers
{
    public class AdminController : Controller
    {


        private V2boardSiteEntities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public AdminController()
        {
            db = new V2boardSiteEntities();
            RepositoryUser = new Repository<tbUsers>(db);

        }

        [System.Web.Mvc.Authorize]
        public ActionResult Index()
        {
            var Use = db.tbUsers.Where(p => p.Username == User.Identity.Name).First();
            return View(RepositoryUser.table.Where(p=> p.FK_Server_ID == Use.FK_Server_ID).OrderByDescending(p=> p.User_ID).ToList());
        }
        [System.Web.Mvc.Authorize]
        public ActionResult Create()
        {
            var us = db.tbUsers.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if(us != null)
            {
                Models.UserAndPlans UserPlans = new Models.UserAndPlans();
                UserPlans.tbPlans = db.tbPlans.Where(p => p.FK_Server_ID == us.FK_Server_ID).ToList();
                return View(UserPlans);
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }
            
        }

        


        [System.Web.Mvc.Authorize]
        [System.Web.Mvc.HttpPost]
        public ActionResult Create(Models.UserAndPlans user,string hidden_select2)
        {

            var Us = db.tbUsers.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (Us != null)
            {
                user.tbUser.FK_Server_ID = Us.FK_Server_ID;
                user.tbUser.Wallet = 0;
                user.tbUser.Status = true;
                user.tbUser.Role = 2;
                foreach(var item in hidden_select2.Split(','))
                {
                    tbLinkUserAndPlans link = new tbLinkUserAndPlans();
                    link.L_FK_P_ID = Convert.ToInt32(item);
                    link.L_Status = true;
                    user.tbUser.tbLinkUserAndPlans.Add(link);
                }

                RepositoryUser.Insert(user.tbUser);
                RepositoryUser.Save();
                return RedirectToAction("Index", "Admin");
            }

            return View();
        }

        [System.Web.Mvc.Authorize]
        public ActionResult Edit(int id)
        {
            var us = db.tbUsers.Where(p => p.User_ID == id).FirstOrDefault();
            if (us != null)
            {
                Models.UserAndPlans UserPlans = new Models.UserAndPlans();
                UserPlans.tbUser = us;
                UserPlans.tbPlans = us.tbLinkUserAndPlans.Select(p => p.tbPlans).ToList();
                UserPlans.AllPlans = db.tbPlans.Where(p => p.FK_Server_ID == us.FK_Server_ID).ToList();
                return View(UserPlans);
            }
            else
            {
              return  RedirectToAction("Login", "Admin");
            }

        }


        [System.Web.Mvc.Authorize]
        [System.Web.Mvc.HttpPost]
        public ActionResult Edit(Models.UserAndPlans user, string hidden_select2)
        {

            var Us = db.tbUsers.Where(p => p.User_ID == user.tbUser.User_ID).FirstOrDefault();
            if (Us != null)
            {
                Us.Wallet = user.tbUser.Wallet;
                Us.Status = user.tbUser.Status;
                Us.FirstName = user.tbUser.FirstName;
                Us.Username = user.tbUser.Username;
                Us.LastName = user.tbUser.LastName;
                Us.Password = user.tbUser.Password;
                Us.Email = user.tbUser.Email;
                Us.Limit = user.tbUser.Limit;
                foreach(var item in Us.tbLinkUserAndPlans.ToList())
                {
                    item.L_Status = false;
                }

                foreach (var item in hidden_select2.Split(','))
                {
                    var first = Us.tbLinkUserAndPlans.Where(p => p.L_FK_P_ID == Convert.ToInt32(item)).FirstOrDefault();
                    if (first == null)
                    {
                        tbLinkUserAndPlans link = new tbLinkUserAndPlans();
                        link.L_FK_P_ID = Convert.ToInt32(item);
                        link.L_Status = true;
                        Us.tbLinkUserAndPlans.Add(link);
                    }
                    else
                    {
                        first.L_Status = true;
                    }
                }
                RepositoryUser.Save();
                return RedirectToAction("Index", "Admin");   
            }

            return View();
        }

        [System.Web.Mvc.HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// تابع لاگین از سمت پنل ادمین
        /// </summary>
        /// <param name="loginModel"></param>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        public ActionResult Login(tbUsers user)
        {
            try
            {
                tbUsers User = RepositoryUser.table.Where(p => p.Username == user.Username && p.Password == user.Password && p.Role == 1).FirstOrDefault();
                if (User != null)
                {

                    FormsAuthentication.SetAuthCookie(User.Username, false);
                    return RedirectToAction("Index", "Admin");


                }
                else
                {
                    return RedirectToAction("Login", "Admin");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Login", "Admin");
            }
        }


    }
}
