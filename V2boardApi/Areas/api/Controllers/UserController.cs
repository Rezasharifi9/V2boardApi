using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.EnterpriseServices;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Timers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;
using System.Web.Management;
using System.Web.Mvc;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;
using Antlr.Runtime;
using DataLayer.DomainModel;
using DataLayer.Repository;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.X509;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using V2boardApi.Models;
using V2boardApi.Models.V2boardModel;
using V2boardApi.Tools;
using V2boardBot.Models;
using System.Threading.Tasks;
using V2boardApi.Areas.api.Data.ViewModels;
using V2boardBot.Functions;
using System.Windows;
using System.Web.WebSockets;
using Org.BouncyCastle.Crypto.Generators;
using System.Web.Security;
using YamlDotNet.Core.Tokens;
using System.Windows.Controls;
using LiteDB;
using DeviceDetectorNET.Class;
using V2boardBotApp.Models;

namespace V2boardApi.Areas.api.Controllers
{
    [EnableCors(origins: "*", "*", "*")]
    public class UserController : ApiController
    {
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbPlans> RepositoryPlan { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbOrders> RepositoryOrder { get; set; }
        private Repository<tbLinkUserAndPlans> RepositoryLinkUserAndPlan { get; set; }
        private Repository<tbLinks> RepositoryLinks { get; set; }
        private Repository<tbDepositWallet_Log> RepositoryDepositWallet { get; set; }
        private Repository<tbTelegramUsers> RepositoryTelegramUser { get; set; }
        private Repository<tbUserFactors> RepositoryFactor { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public UserController()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryPlan = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
            RepositoryOrder = new Repository<tbOrders>();
            RepositoryLinks = new Repository<tbLinks>();
            RepositoryDepositWallet = new Repository<tbDepositWallet_Log>(db);
            RepositoryTelegramUser = new Repository<tbTelegramUsers>(db);
            RepositoryFactor = new Repository<tbUserFactors>(db);
            Timer = new System.Timers.Timer();
            Timer.Elapsed += Timer_Elapsed;

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #region لاگین مربوط به اپلیکیشن
        [System.Web.Http.HttpPost]
        public IHttpActionResult LoginAdmin(ReqLoginModel req)
        {
            try
            {
                var pass = req.password.ToSha256();
                var User = RepositoryUser.Where(p => p.Username == req.username && p.Password == pass && p.Status == true).FirstOrDefault();
                if (User != null)
                {
                    var Server = User.tbServers;

                    var ActiveBank = User.tbBankCardNumbers.Where(p=> p.Active == true).FirstOrDefault();

                    return Ok(new { phoneNumber = User.PhoneNumber, BankSmsNumbers = ActiveBank.BankSmsNumber.Split(',').ToList() });

                }
                else
                {
                    return Content(System.Net.HttpStatusCode.NotFound, "نام کاربری یا رمز عبور اشتباه است");
                }
            }
            catch(Exception ex)
            {
                return BadRequest("خطا در ارتباط با سرور");
            }
        }

        #endregion

        #region لاگین
        [System.Web.Http.HttpPost]
        public IHttpActionResult Login(ReqLoginModel req)
        {
            try
            {
                var Sha = req.password.ToSha256();
                var User = RepositoryUser.table.Where(p => p.Username == req.username && p.Password == Sha).FirstOrDefault();
                if (User != null)
                {
                    if (User.Status == false)
                    {
                        return Content(System.Net.HttpStatusCode.NotFound, "کاربر گرامی حساب شما قفل شده است و اجازه ورود ندارید");
                    }

                    User.Token = (User.Username + User.Password).ToSha256();
                    RepositoryUser.Save();
                    return Ok(new { FirstName = User.FirstName, LastName = User.LastName, Role = User.Role, Token = User.Token });
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

        #endregion

        #region لیست کاربران
        [System.Web.Http.HttpGet]
        [Authorize]
        public IHttpActionResult GetAll(int page = 1, string name = null, string KeySort = null, string type = null, string SortType = "DESC")
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
                            Query += "token='" + name.Split('=')[1] + "'and " + "email like '%" + User.Username + "%'";
                        }
                        else if (type == "uuid")
                        {
                            Query += "uuid='" + name + "'" + " and " + "email like '%" + User.Username + "%'";
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

                            var log = RepositoryLogs.Where(p => p.FK_NameUser_ID.Equals(getUserData.Name) && p.tbLinkUserAndPlans.L_FK_U_ID == User.User_ID).ToList().LastOrDefault();


                            getUserData.TotalVolume = Utility.ConvertByteToGB(reader.GetInt64("transfer_enable")).ToString();
                            getUserData.PlanName = reader.GetString("name");
                            if (log != null)
                            {
                                if (log.tbLinkUserAndPlans != null)
                                {
                                    getUserData.TotalVolume = log.tbLinkUserAndPlans.tbPlans.PlanVolume.Value.ToString();
                                    if (log.PlanName != null)
                                    {
                                        getUserData.PlanName = log.PlanName;
                                    }
                                    else
                                    {
                                        getUserData.PlanName = log.tbLinkUserAndPlans.tbPlans.Plan_Name;
                                    }
                                }
                            }

                            getUserData.IsBanned = Convert.ToBoolean(reader.GetSByte("banned"));
                            getUserData.IsActive = "فعال";
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
                                if (onlineTime >= DateTime.Now.AddMinutes(-1))
                                {
                                    getUserData.IsOnline = true;
                                }
                                else
                                {
                                    getUserData.IsOnline = false;
                                }
                                getUserData.LastTimeOnline = Utility.ConvertDateTimeToShamsi2(onlineTime);
                            }
                            else
                            {
                                getUserData.IsOnline = false;
                            }



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

                    if (reader.IsClosed)
                    {
                        mySqlEntities.Close();
                    }


                    return Ok(new { result = Users, total = count });

                }
                else
                {
                    return Content(System.Net.HttpStatusCode.NotFound, "این کاربر مختص سروری نیست");
                }
            }
            catch (Exception ex)
            {
                var User = RepositoryUser.Where(p => p.Token == Request.Headers.Authorization.Scheme).FirstOrDefault();
                ExceptionHandling.InsertLog(ex, User.User_ID);
                return Content(System.Net.HttpStatusCode.InternalServerError, "خطا در برقراری ارتباط");

            }
        }

        #endregion

        #region افزودن اکانت جدید

        #region افزودن اکانت با پلن
        [System.Web.Http.HttpPost]
        [Authorize]
        public IHttpActionResult CreateUser(CreateUserModel createUser)
        {
            try
            {
                var Token = Request.Headers.Authorization;

                if (!string.IsNullOrEmpty(createUser.name))
                {
                    createUser.name = createUser.name.ToLower();
                    if (createUser.plan_id != 0)
                    {
                        var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true).FirstOrDefault();
                        if (User != null)
                        {
                            var Log = RepositoryLogs.Where(p => p.FK_NameUser_ID == createUser.name && p.tbLinkUserAndPlans.L_FK_U_ID == User.User_ID).ToList().LastOrDefault();
                            if (Log != null)
                            {
                                return Content(System.Net.HttpStatusCode.BadRequest, "این کاربر از قبل وجود دارد");
                            }

                            if ((User.Limit - User.Wallet) >= 0)
                            {

                                var plan = RepositoryPlan.table.Where(p => p.Plan_ID == createUser.plan_id && p.FK_Server_ID == User.FK_Server_ID && p.Status == true).FirstOrDefault();
                                if ((plan.Price + User.Wallet) > User.Limit)
                                {
                                    return Content(System.Net.HttpStatusCode.BadRequest, "مبلغ تعرفه انتخابی بیشتر از موجودی حساب شما می باشد لطفا بدهی خود را پرداخت کنید");
                                }
                                string exp = "";
                                if (plan.CountDayes == 0)
                                {
                                    exp = "NULL";
                                }
                                else
                                {
                                    exp = DateTime.Now.AddDays((int)plan.CountDayes).ConvertDatetimeToSecond().ToString();
                                }
                                var create = DateTime.Now.ConvertDatetimeToSecond().ToString();
                                var planid = plan.Plan_ID_V2;
                                var emilprx = createUser.name + "@" + User.Username;

                                MySqlEntities mySql = new MySqlEntities(User.tbServers.ConnectionString);
                                mySql.Open();

                                var reader = mySql.GetData("select group_id,transfer_enable from v2_plan where id =" + plan.Plan_ID_V2);
                                long tran = 0;
                                int grid = 0;
                                while (reader.Read())
                                {
                                    tran = Utility.ConvertGBToByte(Convert.ToInt64(plan.PlanVolume.Value));
                                    grid = reader.GetInt32("group_id");
                                }
                                reader.Close();

                                string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];

                                string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at) VALUES ('" + emilprx + "'," + exp + "," + create + ",'" + Guid.NewGuid() + "',0,0,0," + tran + ",0," + grid + "," + planid + ",'" + token + "','" + Guid.NewGuid() + "'," + create + ")";

                                reader = mySql.GetData(Query);
                                reader.Close();
                                var link = RepositoryLinkUserAndPlan.table.Where(p => p.L_FK_U_ID == User.User_ID && p.L_FK_P_ID == plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                                User.Wallet += link.tbPlans.Price;


                                RepositoryLinks.Save();
                                RepositoryUser.Save();
                                AddLog(Resource.LogActions.U_Created, link.Link_PU_ID, createUser.name, (int)plan.Price, plan.Plan_Name);
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

        #endregion

        //#region افزودن اکانت با تایم و ترافیک دلخواه

        //[Authorize]
        //[System.Web.Http.HttpPost]
        //public IHttpActionResult CreateAccWithOutPlan(AddAccountWithOutPlanInputModel model)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(model.AccountName) && model.Traffic != 0 && model.Month != 0)
        //        {
        //            if (model.AccountName.Length <= 20)
        //            {
        //                var User = RepositoryUser.table.Where(p => p.Token == Request.Headers.Authorization.Scheme).First();

        //                if (User.tbUserSetting.Us_Status)
        //                {

        //                    if (model.Traffic > User.tbUserSetting.Us_LimitTraffic)
        //                    {
        //                        return Content(System.Net.HttpStatusCode.NotAcceptable, "ترافیک درخواستی بیشتر از ترافیک تنظیم شده برای شماست");
        //                    }
        //                    else if (model.Month > User.tbUserSetting.Us_LimitMonth)
        //                    {
        //                        return Content(System.Net.HttpStatusCode.NotAcceptable, "تعداد ماه درخواستی بیشتر از تعداد ماه تنظیم شده برای شماست");
        //                    }
        //                    else
        //                    {
        //                        var Price = model.Traffic * User.tbUserSetting.Us_CostTraffic;
        //                        Price += model.Month * User.tbUserSetting.Us_LimitMonth;

        //                        var CheckWallet = User.Wallet + Price;
        //                        if (CheckWallet > User.Limit)
        //                        {
        //                            var Count = User.Limit;

        //                            StringBuilder str = new StringBuilder();
        //                            str.Append(" شما اجازه ساخت بیشتر از مبلغ ");
        //                            str.Append(string.Format("{0:C0}", Count).Replace("$", ""));
        //                            str.Append(" تومان");
        //                            str.Append(" را ندارید");
        //                            str.Append(" لطفا بدهی خود را پرداخت کنید تا محدودیت شما 0 شود ");

        //                            return Content(System.Net.HttpStatusCode.NotAcceptable, str.ToString());
        //                        }

        //                        MySqlEntities mySql = new MySqlEntities(User.tbServers.ConnectionString);
        //                        mySql.Open();
        //                        var Reader = mySql.GetData("select email from v2_user where email like '" + model.AccountName + "@" + User.Username + "'");
        //                        if (!Reader.Read())
        //                        {
        //                            Reader.Close();
        //                            var reader = mySql.GetData("select group_id,transfer_enable from v2_plan where id =" + User.tbUserSetting.Us_PlanDefaultInV2board);
        //                            long tran = 0;
        //                            int grid = 0;
        //                            while (reader.Read())
        //                            {
        //                                tran = Utility.ConvertGBToByte(Convert.ToInt64(model.Traffic));
        //                                grid = reader.GetInt32("group_id");
        //                            }
        //                            reader.Close();

        //                            string exp = DateTime.Now.AddMonths(model.Month).ConvertDatetimeToSecond().ToString();

        //                            var create = DateTime.Now.ConvertDatetimeToSecond().ToString();
        //                            var planid = User.tbUserSetting.Us_PlanDefaultInV2board;
        //                            var emilprx = model.AccountName + "@" + User.Username;

        //                            string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];

        //                            string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at) VALUES ('" + emilprx + "'," + exp + "," + create + ",'" + Guid.NewGuid() + "',0,0,0," + tran + ",0," + grid + "," + planid + ",'" + token + "','" + Guid.NewGuid() + "'," + create + ")";

        //                            reader = mySql.GetData(Query);
        //                            reader.Close();

        //                            User.Wallet += Price;
        //                            RepositoryUser.Save();
        //                            UserLog.InsertLog(model.Traffic, model.Month, User.tbUserSetting.Us_CostTraffic.Value, User.tbUserSetting.Us_CostMonth.Value, User.User_ID, model.AccountName);
        //                            mySql.Close();
        //                            return Content(System.Net.HttpStatusCode.OK, "اکانت با موفقیت ساخته شد");

        //                        }
        //                        else
        //                        {
        //                            return Content(System.Net.HttpStatusCode.NotAcceptable, "این اکانت از قبل در سیستم وجود دارد");
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    return Content(System.Net.HttpStatusCode.NotAcceptable, "ویژگی شخصی سازی ماه و ترافیک توسط ادمین بسته شده است");
        //                }
        //            }
        //            else
        //            {
        //                return Content(System.Net.HttpStatusCode.NotAcceptable, "نام کاربر نمی تواند بیشتر از 20 کاراکتر باشد");
        //            }
        //        }
        //        else
        //        {
        //            return Content(System.Net.HttpStatusCode.NoContent, "اطلاعات ورودی ناقص : لطفا ورودی ها را چک کنید");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Content(System.Net.HttpStatusCode.InternalServerError, "خطای ناشناخته سرور");
        //    }
        //}

        //#endregion
        #endregion

        #region لیست تعرفه ها
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
                            foreach (var plan in plans.Where(p => p.L_Status == true && p.tbPlans.FK_Server_ID == User.FK_Server_ID).OrderBy(p => p.tbPlans.CountDayes).ToList())
                            {
                                var dic = new Dictionary<string, string>();
                                dic.Add("ID", plan.tbPlans.Plan_ID.ToString());
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

        #endregion

        #region افزودن لاگ تمدید یا ساخت کاربر
        private bool AddLog(string Action, int LinkUserID, string V2User, int price, string planName)
        {
            try
            {
                tbLogs tbLogs = new tbLogs();
                tbLogs.FK_Link_User_Plan_ID = LinkUserID;
                tbLogs.Action = Action;
                tbLogs.FK_NameUser_ID = V2User;
                tbLogs.CreateDatetime = DateTime.Now;
                tbLogs.SalePrice = price;
                tbLogs.PlanName = planName;
                RepositoryLogs.Insert(tbLogs);
                return RepositoryLogs.Save();
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion

        #region تمدید اکانت
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


                    var Plan = RepositoryPlan.table.Where(p => p.Plan_ID == model.Plan_ID && p.FK_Server_ID == Server.ServerID && p.Status == true).FirstOrDefault();
                    if ((Plan.Price + User.Wallet) > User.Limit)
                    {
                        return Content(System.Net.HttpStatusCode.BadRequest, "مبلغ تعرفه انتخابی بیشتر از موجودی حساب شما می باشد لطفا بدهی خود را پرداخت کنید");
                    }

                    var t = Utility.ConvertGBToByte(Convert.ToInt64(Plan.PlanVolume));
                    string exp = "";
                    if (Plan.CountDayes == 0)
                    {
                        exp = "NULL";
                    }
                    else
                    {
                        exp = DateTime.Now.AddDays((int)Plan.CountDayes).ConvertDatetimeToSecond().ToString();
                    }

                    var Query = "update v2_user set u = 0 , d = 0 , t = 0 ,plan_id=" + Plan.Plan_ID_V2 + ", transfer_enable = " + t + " , expired_at = " + exp + " where id =" + model.AccountID;

                    MySqlEntities mySql = new MySqlEntities(User.tbServers.ConnectionString);
                    mySql.Open();
                    var reader = mySql.GetData(Query);
                    reader.Read();
                    reader.Close();

                    var Query2 = "SELECT email FROM `v2_user` WHERE id=" + model.AccountID;

                    MySqlEntities mySql2 = new MySqlEntities(User.tbServers.ConnectionString);
                    mySql2.Open();
                    var reader2 = mySql2.GetData(Query2);
                    if (reader2.Read())
                    {
                        var link = RepositoryLinkUserAndPlan.table.Where(p => p.L_FK_U_ID == User.User_ID && p.L_FK_P_ID == Plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                        User.Wallet += link.tbPlans.Price;

                        AddLog(Resource.LogActions.U_Edited, link.Link_PU_ID, reader2.GetString("email").Split('@')[0], (int)Plan.Price, Plan.Plan_Name);
                    }
                    reader2.Close();
                    RepositoryLinks.Save();
                    RepositoryUser.Save();
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

        #endregion

        #region دریافت کیف پول
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

        #endregion

        #region ریست لینک اکانت
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
        #endregion

        #region بن کاربر

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

        #endregion

        #region تابع برای ربات که تراکنش هارو چک میکنه
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> CheckOrder(string SMSMessageText, string Mobile)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var Phone = JsonConvert.DeserializeObject(Mobile);
                    var User = RepositoryUser.Where(p => p.PhoneNumber == Phone.ToString()).FirstOrDefault();
                    if (User != null)
                    {
                        int pr = int.Parse(SMSMessageText, NumberStyles.Currency);


                        var tbUserFactor = RepositoryFactor.Where(p => p.tbUf_Value == pr && p.IsPayed == false).OrderByDescending(p => p.tbUf_CreateTime).FirstOrDefault();
                        if (tbUserFactor != null)
                        {
                            tbUserFactor.IsPayed = true;
                            transaction.Commit();
                            RepositoryFactor.Save();
                            return Ok();
                        }

                        var date2 = DateTime.Now.AddDays(-2);
                        var tbDepositLog = RepositoryDepositWallet.Where(p => p.dw_Price == pr && p.dw_Status == "FOR_PAY" && p.dw_CreateDatetime >= date2).ToList();

                        foreach (var item in tbDepositLog)
                        {
                            item.dw_Status = "FINISH";
                            item.tbTelegramUsers.Tel_Wallet += item.dw_Price / 10;
                            StringBuilder str = new StringBuilder();
                            str.AppendLine("✅ کیف پول شما با موفقیت شارژ شد");
                            str.AppendLine("");
                            str.AppendLine("💳 موجودی کیف پول شما : " + item.tbTelegramUsers.Tel_Wallet.Value.ConvertToMony() + " تومان");
                            str.AppendLine("");
                            str.AppendLine("❗️ الان می تونید برای خرید یا تمدید اقدام کنید");

                            var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[]
                            {

                                new KeyboardButton("💰 خرید سرویس"),
                                new KeyboardButton("💸 تمدید سرویس"),
                                new KeyboardButton("⚙️ سرویس ها")
                            },new[]
                            {
                                new KeyboardButton("👜 کیف پول"),
                                new KeyboardButton("📊 تعرفه ها"),
                                new KeyboardButton("♨️ اشتراک تست"),
                            },
                            new[]
                            {
                                new KeyboardButton("🔗 اضافه کردن لینک"),
                                new KeyboardButton("📚 راهنمای اتصال"),
                            },
                            new[]
                            {
                                new KeyboardButton("📞 ارتباط با پشتیبانی"),
                                new KeyboardButton("❔ سوالات متداول"),
                            }

                        });

                            keyboard.IsPersistent = true;
                            keyboard.ResizeKeyboard = true;
                            keyboard.OneTimeKeyboard = false;

                            RealUser.SetUserStep(item.tbTelegramUsers.Tel_UniqUserID, "Start", db);

                            var botID = item.tbTelegramUsers.Tel_RobotID;
                            if (botID != null)
                            {
                                var Server = RepositoryServer.Where(p => p.Robot_ID == botID).FirstOrDefault();
                                if (Server != null)
                                {
                                    TelegramBotClient botClient = new TelegramBotClient(Server.Robot_Token);
                                    RepositoryDepositWallet.Save();
                                    await botClient.SendTextMessageAsync(item.tbTelegramUsers.Tel_UniqUserID, str.ToString(), parseMode: ParseMode.Html, replyMarkup: keyboard);
                                    transaction.Commit();


                                    return Ok();
                                }
                            }

                        }


                        return BadRequest("NOT FOUND ORDER");

                    }
                    else
                    {
                        var Server = RepositoryServer.Where(p => p.Robot_ID == "darkbaz_bot").FirstOrDefault();
                        if (Server != null)
                        {
                            TelegramBotClient client = new TelegramBotClient(Server.Robot_Token);
                            await client.SendTextMessageAsync(Server.AdminTelegramUniqID, "Error api : " + "شماره تلفن یافت نشد");
                        }

                        return BadRequest("FINISHED");
                    }


                }
                catch (Exception ex)
                {

                    var Server = RepositoryServer.Where(p => p.Robot_ID == "darkbaz_bot").FirstOrDefault();
                    if (Server != null)
                    {
                        TelegramBotClient client = new TelegramBotClient(Server.Robot_Token);
                        await client.SendTextMessageAsync(Server.AdminTelegramUniqID, "Error api : " + "Data:" + SMSMessageText + " " + "Mobile :" + Mobile + "|" + ex.Message + " - Trace :" + ex.StackTrace);
                    }

                    transaction.Rollback();
                    return BadRequest("Error api:" + ex.Message + "Data:" + SMSMessageText + " " + "Mobile :" + Mobile);
                }
            }
        }

        #endregion

        #region تاریخچه مصرف کاربر

        [Authorize]
        public IHttpActionResult GetTrafficUsage(int userId)
        {
            var Token = Request.Headers.Authorization;
            var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true).First();

            try
            {

                MySqlEntities mysql = new MySqlEntities(User.tbServers.ConnectionString);
                mysql.Open();

                var reader = mysql.GetData("select * from v2_stat_user where user_id=" + userId);
                List<UsagesModel> Useages = new List<UsagesModel>();
                while (reader.Read())
                {
                    UsagesModel model = new UsagesModel();
                    var d = reader.GetInt64("d");
                    var u = reader.GetInt64("u");

                    var total = d + u;

                    var UnixDate = reader.GetInt64("updated_at");

                    var Date = Utility.ConvertSecondToDatetime(UnixDate);

                    model.Date = Utility.ConvertDateTimeToShamsi(Date);
                    model.Used = Utility.ConvertByteToMG(total);

                    Useages.Add(model);
                }

                var Useage = Useages.GroupBy(p => p.Date).ToList();
                var use = Useage.Select(p => new { Date = p.Key, Used = p.Sum(s => Math.Round(s.Used, 2)) }).ToList();


                return Ok(use);

            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, "دریافت اطلاعات با خطا مواجه شد");
            }

        }

        #endregion

        #region تاریخچه تمدید کاربران

        [Authorize]
        [System.Web.Http.HttpGet]
        public IHttpActionResult GetAccountHistory(string username)
        {
            var result = RepositoryLogs.Where(p => p.FK_NameUser_ID.Contains(username) && p.tbLinkUserAndPlans.tbUsers.Token == Request.Headers.Authorization.Scheme).ToList();

            List<AccountHistoryViewModel> accountHistory = new List<AccountHistoryViewModel>();
            foreach (var item in result)
            {
                AccountHistoryViewModel account = new AccountHistoryViewModel();
                account.CreateTime = Utility.ConvertDateTimeToShamsi2(item.CreateDatetime.Value);
                if (item.PlanName != null)
                {
                    account.PlanName = item.PlanName;
                }
                else
                {
                    account.PlanName = item.tbLinkUserAndPlans.tbPlans.Plan_Name;
                }


                account.SalePrice = (int)item.SalePrice;

                accountHistory.Add(account);
            }

            return Ok(accountHistory);
        }



        #endregion

    }

}

