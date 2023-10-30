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
using V2boardApi.Tools;
using WebGrease;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace V2boardApi.Controllers
{
    [EnableCors(origins: "*", "*", "*")]
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

        /// <summary>
        /// تابع لاگین از سمت پنل ادمین
        /// </summary>
        /// <param name="loginModel"></param>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> Login(LoginModel loginModel)
        {
            try
            {
                tbUsers User = RepositoryUser.table.Where(p => p.Username == loginModel.username && p.Password == loginModel.password && p.Role == 1).FirstOrDefault();
                if (User != null)
                {

                    var Token = (User.Username + User.Password).ToSha256();
                    await Task.CompletedTask;
                    return Content(HttpStatusCode.OK, Token);

                }
                else
                {
                    await Task.CompletedTask;
                    return Content(HttpStatusCode.NotFound, "کاربری با این مشخصات یافت نشد");
                }
            }
            catch (Exception ex)
            {
                await Task.CompletedTask;
                return Content(HttpStatusCode.InternalServerError, "خطا در برقراری ارتباط با پایگاه داده");
            }
        }


        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> GetUsers()
        {
            try
            {
                var Token = Request.Headers.Authorization;
                if (Token != null)
                {
                    if (!string.IsNullOrEmpty(Token.Scheme))
                    {
                        tbUsers User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Role == 1).FirstOrDefault();
                        if (User != null)
                        {
                            var Users = RepositoryUser.table.Where(p => p.FK_Server_ID == User.FK_Server_ID).ToList();

                            List<UsersModel> UsersModel = new List<UsersModel>();
                            foreach (var item in Users)
                            {
                                UsersModel model = new UsersModel();
                                model.id = item.User_ID;
                                model.FirstName = item.FirstName;
                                model.LastName = item.LastName;
                                model.Username = item.Username;
                                model.Password = item.Password;
                                model.Email = item.Email;
                                model.CardNumber = item.Card_Number;
                                model.Status = item.Status;
                                model.Limit = item.Limit;
                                model.Debt = item.Wallet;
                                model.TelegramID = item.TelegramID;
                                UsersModel.Add(model);

                            }

                            await Task.CompletedTask;
                            return Ok(new { result= UsersModel });
                        }
                        else
                        {
                            await Task.CompletedTask;
                            return Content(HttpStatusCode.NotFound, "کاربری با این مشخصات یافت نشد");
                        }
                    }
                    else
                    {
                        await Task.CompletedTask;
                        return Content(HttpStatusCode.NotFound, "کاربری با این مشخصات یافت نشد");
                    }
                }
                else
                {
                    await Task.CompletedTask;
                    return Content(HttpStatusCode.NotFound, "کاربری با این مشخصات یافت نشد");
                }
            }
            catch (Exception ex)
            {
                await Task.CompletedTask;
                return Content(HttpStatusCode.InternalServerError, "خطا در برقراری ارتباط با پایگاه داده");
            }
        }
        [System.Web.Http.HttpGet]
        public IHttpActionResult ConnectSql()
        {
            List<UserMySqlModel> sql = new List<UserMySqlModel>();
            UserMySqlModel sql1 = new UserMySqlModel();
            var conn = ConfigurationManager.ConnectionStrings["mysql"].ConnectionString;

            if (MySqlEntities.Connect(conn))
            {
                var dic = MySqlEntities.GetData("SELECT * FROM v2_user ORDER BY password ASC");

                
            }
            MySqlEntities.Close();



            return Ok();


        }
    }
}
