using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Timers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;
using System.Web.Management;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using Antlr.Runtime;
using DataLayer.DomainModel;
using DataLayer.Repository;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using V2boardApi.Models;
using V2boardApi.Models.V2boardModel;
using V2boardApi.Tools;


namespace V2boardApi.Controllers
{
    [EnableCors(origins: "*", "*", "*")]
    public class UserController : ApiController
    {
        private V2boardSiteEntities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbPlans> RepositoryPlan { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbLinkUserAndPlans> RepositoryLinkUserAndPlan { get; set; }

        private System.Timers.Timer Timer { get; set; }
        public UserController()
        {
            db = new V2boardSiteEntities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryPlan = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);

            Timer = new System.Timers.Timer();
            Timer.Elapsed += Timer_Elapsed;

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        [System.Web.Http.HttpPost]
        public IHttpActionResult Login(ReqLoginModel req)
        {
            try
            {
                var User = RepositoryUser.table.Where(p => p.Username == req.username && p.Password == req.password).FirstOrDefault();
                if (User != null)
                {
                    if (User.Status == false)
                    {
                        return Content(System.Net.HttpStatusCode.NotFound, "کاربر گرامی حساب شما قفل شده است و اجازه ورود ندارید");
                    }
                    var Token = (req.username + req.password).ToSha256();
                    User.Token = Token;

                    var res = RepositoryServer.Save();

                    return Ok(new { Role = User.Role, Token = User.Token });
                }
                else
                {
                    return Content(System.Net.HttpStatusCode.NotFound, "نام کاربری یا رمز عبور اشتباه است");
                }
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, "خطا در برقراری ارتباط با سرور");
            }
        }
        [System.Web.Http.HttpGet]
        [Authorize]
        public IHttpActionResult GetAll(int page = 1, string name = null, string KeySort = null, string SortType = "DESC")
        {
            try
            {
                var Token = Request.Headers.Authorization;
                var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true).FirstOrDefault();
                if (User.tbServers != null)
                {
                    string Query = "SELECT v2.id,v2.email,t,u,d,v2.transfer_enable,banned,token,expired_at,pl.name FROM `v2_user` as v2 join v2_plan as pl on plan_id = pl.id where ";
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (name.Contains("token="))
                        {
                            Query += "token='" + name.Split('=')[1] + "'";
                        }
                        else
                        {
                            Query += "email like'%" + name + "%'" + " and " + "email like '%" + User.Username + "%'";
                        }
                    }
                    else
                    {
                        Query += "email like '%@" + User.Username + "%'";
                    }



                    if (KeySort != null)
                    {
                        if (KeySort.ToLower() == "name")
                        {
                            Query += " order by email " + SortType;
                        }
                        else if (KeySort.ToLower() == "date")
                        {
                            Query += " order by expired_at " + SortType;
                        }
                        else if (KeySort.ToLower() == "totalvolume")
                        {
                            Query += " order by transfer_enable " + SortType;
                        }
                        else if (KeySort.ToLower() == "usedvolume")
                        {
                            Query += " order by t " + SortType;
                        }
                    }
                    else
                    {
                        Query += " Order by v2.id DESC";
                    }

                    if (page == 1)
                    {
                        Query += " LIMIT 10";
                    }
                    else if (page >= 2)
                    {

                        Query += " LIMIT " + (page - 1) * 10 + ",10";

                    }
                    MySqlEntities mySqlEntities = new MySqlEntities(User.tbServers.ConnectionString);

                    mySqlEntities.Open();
                    var reader = mySqlEntities.GetData(Query);

                    List<GetUserDataModel> Users = new List<GetUserDataModel>();
                    var Counter = 0;
                    while (reader.Read())
                    {
                        GetUserDataModel getUserData = new GetUserDataModel();
                        if (reader.HasRows)
                        {
                            getUserData.id = reader.GetInt32("id");
                            getUserData.Name = reader.GetString("email").Split('@')[0];
                            getUserData.IsBanned = Convert.ToBoolean(reader.GetSByte("banned"));
                            getUserData.IsActive = "فعال";
                            getUserData.TotalVolume = Utility.ConvertByteToGB(reader.GetInt64("transfer_enable")).ToString();
                            var exp = reader.GetBodyDefinition("expired_at");
                            var OnlineTime = reader.GetBodyDefinition("t");
                            if (exp != "")
                            {
                                var e = Convert.ToInt64(exp);
                                var ex = Utility.ConvertSecondToDatetime(e);
                                getUserData.ExpireDate = Utility.ConvertDateTimeToShamsi(ex);

                                getUserData.DaysLeft = Utility.CalculateLeftDayes(ex);
                                if (getUserData.DaysLeft <= 2)
                                {
                                    getUserData.CanEdit = true;
                                }
                                if (ex <= DateTime.Now)
                                {
                                    getUserData.IsActive = "پایان تاریخ اشتراک";
                                }
                            }
                            if (OnlineTime != "0")
                            {
                                var onlineTime = Utility.ConvertSecondToDatetime(Convert.ToInt64(OnlineTime));
                                if (onlineTime >= DateTime.Now.AddMinutes(-2))
                                {
                                    getUserData.IsOnline = true;
                                }
                                else
                                {
                                    getUserData.IsOnline = false;
                                }
                                getUserData.LastTimeOnline = Utility.ConvertDateTimeToShamsi(onlineTime);
                            }
                            else
                            {
                                getUserData.IsOnline = false;
                            }

                            getUserData.PlanName = reader.GetString("name");


                            getUserData.SubLink = "https://" + User.tbServers.SubAddress + "/api/v1/client/subscribe?token=" + reader.GetString("token");
                            var u = reader.GetInt64("u");
                            var d = reader.GetInt64("d");
                            var re = Utility.ConvertByteToGB(u + d);
                            getUserData.UsedVolume = Math.Round(re, 2) + " GB";

                            var vol = reader.GetInt64("transfer_enable") - (u + d);
                            var dd = Utility.ConvertByteToGB(vol);
                            if (dd <= 2)
                            {
                                getUserData.CanEdit = true;
                            }

                            if (vol <= 0)
                            {
                                getUserData.IsActive = "اتمام حجم";
                            }
                            else
                                if (getUserData.IsBanned && getUserData.IsActive == "فعال")
                            {
                                getUserData.IsActive = "مسدود";
                            }
                            getUserData.RemainingVolume = Math.Round(dd, 2) + " GB";
                            Users.Add(getUserData);
                            Counter++;
                        }


                    }
                    reader.Close();
                    mySqlEntities.Close();

                    mySqlEntities.Open();
                    if (name != null)
                    {
                        reader = mySqlEntities.GetData("SELECT COUNT(id) as Count FROM `v2_user` where email like '%" + User.Username + "' and email like '" + name + "%'");
                    }
                    else
                    {
                        reader = mySqlEntities.GetData("SELECT COUNT(id) as Count FROM `v2_user` where email like '%" + User.Username + "%'");
                    }
                    reader.Read();
                    var count = reader.GetInt32("Count");
                    reader.Close();
                    mySqlEntities.Close();
                    return Ok(new { result = Users, total = count });

                }
                else
                {
                    return Content(System.Net.HttpStatusCode.NotFound, "این کاربر مختص سروری نیست");
                }
            }
            catch (Exception ex)
            {
                Utility.InsertLog(ex);
                return Content(System.Net.HttpStatusCode.InternalServerError, "خطا در برقراری ارتباط با پایگاه داده");

            }
        }

        [System.Web.Http.HttpPost]
        [Authorize]
        public IHttpActionResult CreateUser(CreateUserModel createUser)
        {
            try
            {
                var Token = Request.Headers.Authorization;

                if (!string.IsNullOrEmpty(createUser.name))
                {
                    if (createUser.plan_id != 0)
                    {
                        var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true).FirstOrDefault();
                        if (User != null)
                        {

                            if ((User.Limit - User.Wallet) >= 0)
                            {

                                var plan = RepositoryPlan.table.Where(p => p.Plan_ID_V2 == createUser.plan_id && p.FK_Server_ID == User.FK_Server_ID && p.Status == true).FirstOrDefault();
                                var exp = DateTime.Now.AddDays((int)plan.CountDayes).ConvertDatetimeToSecond().ToString();
                                var create = DateTime.Now.ConvertDatetimeToSecond().ToString();
                                var planid = createUser.plan_id;
                                var emilprx = createUser.name + "@" + User.Username;

                                MySqlEntities mySql = new MySqlEntities(User.tbServers.ConnectionString);
                                mySql.Open();

                                var reader = mySql.GetData("select group_id,transfer_enable from v2_plan where id =" + planid);
                                long tran = 0;
                                int grid = 0;
                                while (reader.Read())
                                {
                                    tran = Utility.ConvertGBToByte(reader.GetInt64("transfer_enable"));
                                    grid = reader.GetInt32("group_id");
                                }
                                reader.Close();

                                string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];

                                string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at) VALUES ('" + emilprx + "'," + exp + "," + create + ",'" + Guid.NewGuid() + "',0,0,0," + tran + ",0," + grid + "," + planid + ",'" + token + "','" + Guid.NewGuid() + "'," + create + ")";



                                reader = mySql.GetData(Query);
                                reader.Close();
                                var link = RepositoryLinkUserAndPlan.table.Where(p => p.L_FK_U_ID == User.User_ID && p.L_FK_P_ID == plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                                RepositoryUser.Save();
                                User.Wallet += link.tbPlans.Price;
                                AddLog(Resource.LogActions.U_Created, link.Link_PU_ID, createUser.name);
                                return Content(System.Net.HttpStatusCode.OK, "اکانت با موفقیت ساخته شد");
                            }
                            else
                            {

                                var Count = User.Limit;

                                StringBuilder str = new StringBuilder();
                                str.Append(" شما اجازه ساخت بیشتر از مبلغ ");
                                str.Append(string.Format("{0:C0}", Count).Replace("$", ""));
                                str.Append(" تومان");
                                str.Append(" را ندارید");
                                str.Append(" لطفا بدهی خود را پرداخت کنید تا محدودیت 0 شود ");

                                return Content(System.Net.HttpStatusCode.BadRequest, str.ToString());
                            }
                        }
                        else
                        {
                            return Content(System.Net.HttpStatusCode.NotFound, "کاربر یافت نشد لطفا توکن را چک کنید");
                        }
                    }
                    else
                    {
                        return Content(System.Net.HttpStatusCode.NotFound, "لطفا پلن را انتخاب کنید");
                    }
                }
                else
                {
                    return Content(System.Net.HttpStatusCode.NotFound, "لطفا نام اکانت را وارد کنید");
                }

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(createUser.name))
                {
                    return Content(System.Net.HttpStatusCode.BadRequest, "این کاربر از قبل وجود دارد");
                }
                return Content(System.Net.HttpStatusCode.InternalServerError, "خطا در برقراری ارتباط با سرور");
            }

        }

        [Authorize]
        public IHttpActionResult GetPlans()
        {
            var Token = Request.Headers.Authorization;
            if (Token != null)
            {
                var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true).FirstOrDefault();
                if (User != null)
                {
                    if (User.tbServers != null)
                    {
                        var plans = User.tbLinkUserAndPlans;
                        if (plans != null)
                        {
                            List<Dictionary<string, string>> key = new List<Dictionary<string, string>>();
                            foreach (var plan in plans.Where(p => p.L_Status == true && p.tbPlans.FK_Server_ID == User.FK_Server_ID).OrderBy(p=> p.tbPlans.Price).ToList())
                            {
                                var dic = new Dictionary<string, string>();
                                dic.Add("ID", plan.tbPlans.Plan_ID_V2.ToString());
                                dic.Add("Name", plan.tbPlans.Plan_Name);
                                key.Add(dic);
                            }

                            return Ok(key);
                        }
                        else
                        {
                            return Content(System.Net.HttpStatusCode.NotFound, "پلنی برای این سرور وجود ندارد");
                        }

                    }
                    else
                    {
                        return Content(System.Net.HttpStatusCode.NotFound, "این کاربر مختص سروری نیست");
                    }
                }
                else
                {
                    return Content(System.Net.HttpStatusCode.NotFound, "کاربر یافت نشد لطفا توکن را چک کنید");
                }
            }
            else
            {
                return Content(System.Net.HttpStatusCode.NotFound, "توکن خالی است لطفا توکن را وارد کنید");
            }
        }
        private bool AddLog(string Action, int LinkUserID, string V2User)
        {
            try
            {
                tbLogs tbLogs = new tbLogs();
                tbLogs.FK_Link_User_Plan_ID = LinkUserID;
                tbLogs.Action = Action;
                tbLogs.FK_NameUser_ID = V2User;
                tbLogs.CreateDatetime = DateTime.Now;
                RepositoryLogs.Insert(tbLogs);
                return RepositoryLogs.Save();
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        [System.Web.Http.HttpPost]
        [Authorize]
        public IHttpActionResult Update(Models.UpdateUserModel model)
        {
            var auth = Request.Headers.Authorization;

            var User = RepositoryUser.table.Where(p => p.Token == auth.Scheme).FirstOrDefault();
            if (User != null)
            {
                if ((User.Limit - User.Wallet) >= 0)
                {
                    var Server = User.tbServers;


                    var Plan = RepositoryPlan.table.Where(p => p.Plan_ID_V2 == model.Plan_ID && p.FK_Server_ID == Server.ServerID && p.Status == true).FirstOrDefault();


                    var t = Utility.ConvertGBToByte(Convert.ToInt64(Plan.PlanVolume));
                    var pl = DateTime.Now.AddDays((int)Plan.CountDayes).ConvertDatetimeToSecond().ToString();


                    var Query = "update v2_user set u = 0 , d = 0 , t = 0 ,plan_id=" + model.Plan_ID + ", transfer_enable = " + t + " , expired_at = " + pl + " where id =" + model.AccountID;

                    MySqlEntities mySql = new MySqlEntities(User.tbServers.ConnectionString);
                    mySql.Open();
                    var reader = mySql.GetData(Query);
                    reader.Read();
                    reader.Close();

                    var link = RepositoryLinkUserAndPlan.table.Where(p => p.L_FK_U_ID == User.User_ID && p.L_FK_P_ID == Plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                    User.Wallet += link.tbPlans.Price;
                    AddLog(Resource.LogActions.U_Edited, link.Link_PU_ID, model.AccountID.ToString());



                    return Ok("اکانت با موفقیت تمدید شد");


                }
                else
                {
                    var Count = User.Limit;

                    StringBuilder str = new StringBuilder();
                    str.Append(" شما اجازه ساخت بیشتر از مبلغ ");
                    str.Append(string.Format("{0:C0}", Count).Replace("$", ""));
                    str.Append(" تومان");
                    str.Append(" را ندارید");
                    str.Append(" لطفا بدهی خود را پرداخت کنید تا محدودیت 0 شود ");

                    return Content(System.Net.HttpStatusCode.BadRequest, str.ToString());
                }

            }
            else
            {
                return Content(System.Net.HttpStatusCode.NotFound, "کاربر یافت نشد لطفا توکن را چک کنید");
            }


        }

        [System.Web.Http.HttpGet]
        [Authorize]
        public IHttpActionResult GetWallet()
        {
            var Auth = Request.Headers.Authorization;

            var User = RepositoryUser.table.Where(p => p.Token == Auth.Scheme).FirstOrDefault();
            if (User != null)
            {
                WalletModel model = new WalletModel();
                if (User.Wallet != null)
                {
                    model.Wallet = (int)User.Limit - (int)User.Wallet;
                    model.Payable_debt = (int)User.Wallet;
                }
                model.PayLimit = (int)User.Limit;

                return Ok(model);
            }
            else
            {
                return Content(System.Net.HttpStatusCode.NotFound, "کاربر یافت نشد لطفا توکن را چک کنید");
            }
        }


        [System.Web.Http.HttpPost]
        [Authorize]
        public IHttpActionResult Reset(BanUserModel model)
        {
            var auth = Request.Headers.Authorization;

            var User = RepositoryUser.table.Where(p => p.Token == auth.Scheme).FirstOrDefault();

            if (User != null)
            {
                var Server = User.tbServers;

                MySqlEntities mySql = new MySqlEntities(User.tbServers.ConnectionString);
                mySql.Open();
                string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                var query = "update v2_user set token = '" + token + "',uuid='" + Guid.NewGuid() + "' where id=" + model.AccountID;
                var reader = mySql.GetData(query);
                return Ok("لینک اشتراک با موفقیت تغییر یافت");
            }
            else
            {
                return Content(System.Net.HttpStatusCode.NotFound, "کاربری یافت نشد لطفا توکن را چک کنید");
            }


        }


        [System.Web.Http.HttpPost]
        [Authorize]
        public IHttpActionResult BanUser(BanUserModel model)
        {
            try
            {
                var auth = Request.Headers.Authorization;
                var User = RepositoryUser.table.Where(p => p.Token == auth.Scheme && p.Status == true).FirstOrDefault();
                MySqlEntities mySql = new MySqlEntities(User.tbServers.ConnectionString);
                mySql.Open();

                var reader = mySql.GetData("update v2_user set banned = " + Convert.ToInt16(!model.Status) + " where email like '%" + User.Username + "%' and id =" + model.AccountID);
                reader.Read();

                var state = "غیرفعال";
                if (model.Status)
                {
                    state = "فعال";
                }

                var mess = " کاربر با موفقیت " + state + " شد ";
                return Ok(mess);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, "خطا در برقراری ارتباط با سرور");
            }
        }

    }

}

