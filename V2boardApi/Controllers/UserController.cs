using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
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
                        return Ok(new { status = false, result = "کاربر گرامی حساب شما قفل شده است و اجازه ورود ندارید" });
                    }
                    var Token = (req.username + req.password).ToSha256();
                    if (User.Token != Token)
                    {
                        User.Token = Token;

                        var res = RepositoryServer.Save();
                    }
                    return Ok(new { status = true, result = Token });
                }
                else
                {
                    return Ok(new { status = false, result = "نام کاربری یا رمز عبور اشتباه است" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { status = false, result = "خطا در برقراری ارتباط با سرور" });
            }
        }
        [System.Web.Http.HttpGet]
        public IHttpActionResult GetAll(int page = 1, string name = null, string link = null, string KeySort = null, string SortType = "DESC")
        {
            var counter = 0;
            try
            {
                var Token = Request.Headers.Authorization;
                if (Token != null)
                {
                    var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true).FirstOrDefault();
                    if (User != null)
                    {
                        HttpClient client = new HttpClient();
                        if (User.tbServers != null)
                        {
                            client.BaseAddress = new Uri(User.tbServers.ServerAddress);
                            client.DefaultRequestHeaders.Clear();
                            client.DefaultRequestHeaders.Add("Authorization", User.tbServers.Auth_Token);

                            StringBuilder str = new StringBuilder();
                            str.Append(client.BaseAddress + "api/v1/" + User.tbServers.AdminPath + "/user/fetch?");
                            if (name != null)
                            {
                                str.Append("filter[0][key]=email&filter[0][condition]=%E6%A8%A1%E7%B3%8A&filter[0][value]=" + name);
                                str.Append("&filter[1][key]=email&filter[1][condition]=%E6%A8%A1%E7%B3%8A&filter[1][value]=" + "@" + User.Username);
                            }
                            else
                            {
                                str.Append("filter[0][key]=email&filter[0][condition]=%E6%A8%A1%E7%B3%8A&filter[0][value]=" + "@" + User.Username.ToLower());
                            }
                            if (link != null)
                            {
                                str.Append("&filter[1][key]=token&filter[1][condition]=%3D&filter[1][value]=" + link.Split('=')[1]);
                            }

                            str.Append("&pageSize=10&current=" + page);

                            if (KeySort != null)
                            {
                                if (KeySort.ToLower() == "name")
                                {
                                    str.Append("&" + "sort_type=" + SortType + "&sort=id");
                                }
                                else if (KeySort.ToLower() == "date")
                                {
                                    str.Append("&" + "sort_type=" + SortType + "&sort=expired_at");
                                }
                                else if (KeySort.ToLower() == "totalvolume")
                                {
                                    str.Append("&" + "sort_type=" + SortType + "&sort=transfer_enable");
                                }
                                else if (KeySort.ToLower() == "usedvolume")
                                {
                                    str.Append("&" + "sort_type=" + SortType + "&sort=total_used");
                                }
                            }

                            var result = client.GetAsync(str.ToString());

                            if (result.Result.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                var Content = result.Result.Content.ReadAsStringAsync();
                                var Con = Content.Result.ToString();

                                var res = JObject.Parse(Con);
                                var data = res["data"].ToString();
                                var total1 = Convert.ToInt32(res["total"].ToString());

                                var Js = JsonConvert.DeserializeObject<List<GetUserModel>>(data);

                                var Users = new List<GetUserDataModel>();

                                foreach (var item in Js)
                                {
                                    GetUserDataModel getUserData = new GetUserDataModel();
                                    getUserData.Name = item.email.Split('@')[0];
                                    getUserData.id = item.id;
                                    getUserData.IsBanned = Convert.ToBoolean(item.banned);
                                    getUserData.TotalVolume = Utility.ConvertByteToGB(item.transfer_enable).ToString();
                                    if (item.expired_at != null)
                                    {
                                        var ex = Utility.ConvertSecondToDatetime((long)item.expired_at);
                                        getUserData.ExpireDate = Utility.ConvertDateTimeToShamsi(ex);
                                        getUserData.DaysLeft = Utility.CalculateLeftDayes(ex);
                                        if (getUserData.DaysLeft <= 2)
                                        {
                                            getUserData.CanEdit = true;
                                        }
                                    }
                                    var Plan = RepositoryLogs.table.Where(p => p.FK_NameUser_ID == getUserData.Name && p.tbLinkUserAndPlans.tbUsers.Token == Token.Scheme).FirstOrDefault();
                                    if (Plan != null)
                                    {
                                        getUserData.PlanName = Plan.tbLinkUserAndPlans.tbPlans.Plan_Name;
                                    }
                                    else
                                    {
                                        getUserData.PlanName = item.plan_name;
                                    }

                                    getUserData.SubLink = item.subscribe_url;

                                    var re = Utility.ConvertByteToGB(item.u + item.d);
                                    getUserData.UsedVolume = Math.Round(re, 2) + " GB";

                                    var vol = item.transfer_enable - (item.u + item.d);
                                    var d = Utility.ConvertByteToGB(vol);
                                    if (d <= 2)
                                    {
                                        getUserData.CanEdit = true;
                                    }

                                    getUserData.RemainingVolume = Math.Round(d, 2) + " GB";
                                    Users.Add(getUserData);
                                    counter++;
                                }

                                return Ok(new { status = true, result = Users, total = total1 });
                            }
                            else
                            {
                                return Ok(new { status = false, result = "ارتباط با سرور پنل برقرار نشد لطفا اطلاعات ورودی را چک کنید" });
                            }

                        }
                        else
                        {
                            return Ok(new { status = false, result = "این کاربر مختص سروری نیست" });
                        }

                    }
                    else
                    {
                        return Ok(new { status = false, result = "کاربر یافت نشد لطفا توکن را چک کنید" });
                    }
                }
                else
                {
                    return Ok(new { status = false, result = "توکن خالی است لطفا توکن را وارد کنید" });
                }


            }
            catch (Exception ex)
            {

                return Ok(new { status = false, result = "خطا در برقراری ارتباط با سرور" });

            }
        }

        [System.Web.Http.HttpPost]
        public IHttpActionResult CreateUser(CreateUserModel createUser)
        {
            try
            {
                var Token = Request.Headers.Authorization;

                if (Token != null)
                {
                    if (!string.IsNullOrEmpty(createUser.name))
                    {
                        if (createUser.plan_id != 0)
                        {
                            var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true).FirstOrDefault();
                            if (User != null)
                            {

                                if ((User.Limit - User.Wallet) >= 0)
                                {
                                    HttpClient client = new HttpClient();
                                    client.BaseAddress = new Uri(User.tbServers.ServerAddress);
                                    client.DefaultRequestHeaders.Clear();
                                    client.DefaultRequestHeaders.Add("Authorization", User.tbServers.Auth_Token);

                                    client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

                                    Dictionary<string, string> exp = new Dictionary<string, string>();

                                    var plan = RepositoryPlan.table.Where(p => p.Plan_ID_V2 == createUser.plan_id && p.FK_Server_ID == User.FK_Server_ID && p.Status == true).FirstOrDefault();

                                    exp.Add("expired_at", DateTime.Now.AddDays((int)plan.CountDayes).ConvertDatetimeToSecond().ToString());
                                    exp.Add("plan_id", createUser.plan_id.ToString());
                                    exp.Add("email_prefix", createUser.name);
                                    exp.Add("email_suffix", User.Username);

                                    var Form = new FormUrlEncodedContent(exp);

                                    var result = client.PostAsync(client.BaseAddress + "api/v1/" + User.tbServers.AdminPath + "/user/generate", Form);

                                    if (result.Result.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        var link = RepositoryLinkUserAndPlan.table.Where(p => p.L_FK_U_ID == User.User_ID && p.L_FK_P_ID == plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                                        RepositoryUser.Save();
                                        User.Wallet += link.tbPlans.Price;
                                        AddLog(Resource.LogActions.U_Created, link.Link_PU_ID, createUser.name);
                                        return Ok(new { status = true, result = "اکانت با موفقیت ساخته شد" });
                                    }
                                    else
                                    {
                                        return Ok(new { status = false, result = "این اکانت از قبل وجود دارد" });
                                    }
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

                                    return Ok(new { status = false, result = str.ToString() });
                                }
                            }
                            else
                            {
                                return Ok(new { status = false, result = "کاربر یافت نشد لطفا توکن را چک کنید" });
                            }
                        }
                        else
                        {
                            return Ok(new { status = false, result = "لطفا پلن را انتخاب کنید" });
                        }
                    }
                    else
                    {
                        return Ok(new { status = false, result = "لطفا نام اکانت را وارد کنید" });
                    }
                }
                else
                {
                    return Ok(new { status = false, result = "توکن خالی است لطفا توکن را وارد کنید" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { status = false, result = "خطا در برقراری ارتباط با سرور" });
            }

        }

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
                            foreach (var plan in plans.Where(p => p.L_Status == true).ToList())
                            {
                                var dic = new Dictionary<string, string>();
                                dic.Add("ID", plan.tbPlans.Plan_ID_V2.ToString());
                                dic.Add("Name", plan.tbPlans.Plan_Name);
                                key.Add(dic);
                            }

                            return Ok(new { status = true, result = key });
                        }
                        else
                        {
                            return Ok(new { status = false, result = "پلنی برای این سرور وجود ندارد" });
                        }

                    }
                    else
                    {
                        return Ok(new { status = false, result = "این کاربر مختص سروری نیست" });
                    }
                }
                else
                {
                    return Ok(new { status = false, result = "کاربر یافت نشد لطفا توکن را چک کنید" });
                }
            }
            else
            {
                return Ok(new { status = false, result = "توکن خالی است لطفا توکن را وارد کنید" });
            }
        }

        [System.Web.Http.HttpGet]
        public IHttpActionResult GetInfoForAccount(string SubLink)
        {
            try
            {
                if (!string.IsNullOrEmpty(SubLink))
                {
                    foreach (var item in RepositoryServer.GetAll(p => p.Status == true))
                    {
                        HttpClient client = new HttpClient();
                        client.BaseAddress = new Uri(item.ServerAddress);
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Authorization", item.Auth_Token);
                        var result = client.GetAsync(client.BaseAddress + "api/v1/" + item.AdminPath + "/user/fetch?filter[0][key]=token&filter[0][condition]=%3D&filter[0][value]=" + SubLink.Split('=')[1] + "&pageSize=10");
                        if (result.Result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var Content = result.Result.Content.ReadAsStringAsync();
                            var Con = Content.Result.ToString();

                            var res = JObject.Parse(Con);
                            var data = res["data"].ToString();
                            var Js = JsonConvert.DeserializeObject<List<GetUserModel>>(data);
                            if (Js.Count >= 1)
                            {
                                var item2 = Js[0];
                                GetUserDataModel getUserData = new GetUserDataModel();
                                getUserData.Name = item2.email.Split('@')[0];

                                if (item2.expired_at != 0)
                                {
                                    var ex = Utility.ConvertSecondToDatetime((long)item2.expired_at);
                                    getUserData.ExpireDate = Utility.ConvertDateTimeToShamsi(ex);
                                    getUserData.DaysLeft = Utility.CalculateLeftDayes(ex);
                                }



                                getUserData.PlanName = item2.plan_name;
                                getUserData.SubLink = item2.subscribe_url;

                                var re = Utility.ConvertByteToGB(item2.u + item2.d);
                                getUserData.UsedVolume = Math.Round(re, 2) + " GB";

                                var vol = item2.transfer_enable - (item2.u + item2.d);
                                var d = Utility.ConvertByteToGB(vol);
                                getUserData.RemainingVolume = Math.Round(d, 2) + " GB";

                                return Ok(new { status = true, result = getUserData });
                            }

                        }
                    }
                    return Ok(new { status = false, result = "دریافت اطلاعات با خطا مواجه شد" });
                }
                else
                {
                    return Ok(new { status = false, result = "لطفا لینک را وارد کنید" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { status = false, result = "دریافت اطلاعات با خطا مواجه شد" });

            }
        }

        public bool AddLog(string Action, int LinkUserID, string V2User)
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



        [System.Web.Http.HttpGet]
        public IHttpActionResult GetUserBySearch(string search)
        {
            var counter = 0;
            try
            {
                if (!string.IsNullOrEmpty(search))
                {
                    var Token = Request.Headers.Authorization;
                    if (Token != null)
                    {
                        var User = RepositoryUser.table.Where(p => p.Token == Token.Scheme && p.Status == true).FirstOrDefault();
                        if (User != null)
                        {
                            HttpClient client = new HttpClient();
                            if (User.tbServers != null)
                            {
                                client.BaseAddress = new Uri(User.tbServers.ServerAddress);
                                client.DefaultRequestHeaders.Clear();
                                client.DefaultRequestHeaders.Add("Authorization", User.tbServers.Auth_Token);
                                var result = client.GetAsync(client.BaseAddress + "api/v1/" + User.tbServers.AdminPath + "/user/fetch?filter[0][key]=email&filter[0][condition]=%E6%A8%A1%E7%B3%8A&filter[0][value]=" + search + "@" + User.Username + "&pageSize=10&current=1");
                                if (result.Result.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    var Content = result.Result.Content.ReadAsStringAsync();
                                    var Con = Content.Result.ToString();

                                    var res = JObject.Parse(Con);
                                    var data = res["data"].ToString();
                                    var total1 = Convert.ToInt32(res["total"].ToString());

                                    var Js = JsonConvert.DeserializeObject<List<GetUserModel>>(data);

                                    var Users = new List<GetUserDataModel>();

                                    foreach (var item in Js)
                                    {
                                        GetUserDataModel getUserData = new GetUserDataModel();
                                        getUserData.Name = item.email.Split('@')[0];
                                        getUserData.id = item.id;

                                        if (item.expired_at != null)
                                        {
                                            var ex = Utility.ConvertSecondToDatetime((long)item.expired_at);
                                            getUserData.ExpireDate = Utility.ConvertDateTimeToShamsi(ex);
                                            getUserData.DaysLeft = Utility.CalculateLeftDayes(ex);
                                        }
                                        getUserData.PlanName = item.plan_name;
                                        getUserData.SubLink = item.subscribe_url;

                                        var re = Utility.ConvertByteToGB(item.u + item.d);
                                        getUserData.UsedVolume = Math.Round(re, 2) + " GB";

                                        var vol = item.transfer_enable - (item.u + item.d);
                                        var d = Utility.ConvertByteToGB(vol);
                                        getUserData.RemainingVolume = Math.Round(d, 2) + " GB";
                                        Users.Add(getUserData);
                                        counter++;
                                    }


                                    return Ok(new { status = true, result = Users, total = total1 });
                                }
                                else
                                {
                                    return Ok(new { status = false, result = "ارتباط با سرور پنل برقرار نشد لطفا اطلاعات ورودی را چک کنید" });
                                }

                            }
                            else
                            {
                                return Ok(new { status = false, result = "این کاربر مختص سروری نیست" });
                            }

                        }
                        else
                        {
                            return Ok(new { status = false, result = "کاربر یافت نشد لطفا توکن را چک کنید" });
                        }
                    }
                    else
                    {
                        return Ok(new { status = false, result = "توکن خالی است لطفا توکن را وارد کنید" });
                    }
                }
                else
                {
                    return Ok(new { status = false, result = "لطفا متن سرچ را وارد کنید" });
                }

            }
            catch (Exception ex)
            {

                return Ok(new { status = false, result = "خطا در برقراری ارتباط با سرور" });

            }
        }



        [System.Web.Http.HttpPost]
        public IHttpActionResult Update(Models.UpdateUserModel model)
        {
            var auth = Request.Headers.Authorization;
            if (auth != null)
            {
                if (!string.IsNullOrEmpty(auth.Scheme))
                {
                    var User = RepositoryUser.table.Where(p => p.Token == auth.Scheme).FirstOrDefault();
                    if (User != null)
                    {
                        if ((User.Limit - User.Wallet) >= 0 || model.IsBanned == true)
                        {
                            var Server = User.tbServers;

                            HttpClient client = new HttpClient();
                            client.BaseAddress = new Uri(Server.ServerAddress + "api/v1/" + Server.AdminPath);
                            client.DefaultRequestHeaders.Clear();
                            client.DefaultRequestHeaders.Add("Authorization", Server.Auth_Token);

                            UpdateUserV2Model v2model = new UpdateUserV2Model();
                            v2model.email = model.Name + "@" + User.Username;

                            var Plan = RepositoryPlan.table.Where(p => p.Plan_ID_V2 == model.Plan_ID && p.FK_Server_ID == Server.ServerID && p.Status == true).FirstOrDefault();

                            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

                            Dictionary<string, string> exp = new Dictionary<string, string>();
                            int ban = 0;
                            if (model.IsBanned)
                            {
                                ban = 1;
                            }


                            exp.Add("id", model.AccountID.ToString());
                            exp.Add("email", model.Name + "@" + User.Username);
                            exp.Add("u", "0");
                            exp.Add("d", "0");
                            exp.Add("is_staff", "0");
                            exp.Add("is_admin", "0");
                            exp.Add("banned", ban.ToString());

                            if (model.Plan_ID != 0 && model.IsBanned == false)
                            {
                                var t = ((Convert.ToInt64(Plan.PlanVolume) * 1024) * 1024) * 1024;
                                exp.Add("expired_at", DateTime.Now.AddDays((int)Plan.CountDayes).ConvertDatetimeToSecond().ToString());
                                exp.Add("transfer_enable", t.ToString());
                            }

                            var Form = new FormUrlEncodedContent(exp);

                            var addr = client.BaseAddress + "/user/update";
                            var request = client.PostAsync(addr, Form);
                            if (request.Result.StatusCode == System.Net.HttpStatusCode.OK)
                            {

                                if (model.Plan_ID != 0)
                                {
                                    var link = RepositoryLinkUserAndPlan.table.Where(p => p.L_FK_U_ID == User.User_ID && p.L_FK_P_ID == Plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                                    User.Wallet += link.tbPlans.Price;
                                    AddLog(Resource.LogActions.U_Edited, link.Link_PU_ID, model.Name);
                                }


                                return Ok(new { status = true, result = "اکانت با موفقیت ویرایش شد" });
                            }
                            else
                            {
                                var result = request.Result.Content.ReadAsStringAsync();
                                return Ok(new { status = false, result = "اکانت ویرایش نشد" });
                            }

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

                            return Ok(new { status = false, result = str.ToString() });
                        }

                    }
                    else
                    {
                        return Ok(new { status = false, result = "کاربر یافت نشد لطفا توکن را چک کنید" });
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

        [System.Web.Http.HttpGet]
        public IHttpActionResult GetWallet()
        {
            var Auth = Request.Headers.Authorization;
            if (Auth != null)
            {
                if (!string.IsNullOrEmpty(Auth.Scheme))
                {
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
                        return Ok(new { status = false, result = "کاربر یافت نشد لطفا توکن را چک کنید" });
                    }
                }
                else
                {
                    return Ok(new { status = false, result = "کاربر یافت نشد لطفا توکن را چک کنید" });
                }
            }
            else
            {
                return Ok(new { status = false, result = "توکن خالی است لطفا توکن را وارد کنید" });
            }
        }

        [System.Web.Http.HttpPost]
        public IHttpActionResult Reset(BanUserModel model)
        {
            var auth = Request.Headers.Authorization;
            if (!string.IsNullOrEmpty(auth.Scheme))
            {
                var User = RepositoryUser.table.Where(p => p.Token == auth.Scheme).FirstOrDefault();
                if (User != null)
                {
                    var Server = User.tbServers;

                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(Server.ServerAddress + "api/v1/" + Server.AdminPath);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("Authorization", Server.Auth_Token);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

                    Dictionary<string, string> exp = new Dictionary<string, string>();
                    exp.Add("id", model.id.ToString());

                    var Form = new FormUrlEncodedContent(exp);

                    var addr = client.BaseAddress + "/user/resetSecret";
                    var request = client.PostAsync(addr, Form);
                    if (request.Result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Ok(new { status = true, result = "لینک اشتراک با موفقیت تغییر کرد لطفا مجدد لینک رو کپی کنید" });
                    }
                    else
                    {
                        var result = request.Result.Content.ReadAsStringAsync();
                        return Ok(new { status = false, result = "خطا در تغییر لینک" });
                    }
                }
                else
                {
                    return Ok(new { status = false, result = "کاربر یافت نشد لطفا توکن را چک کنید" });
                }
            }
            else
            {
                return Ok(new { status = false, result = "توکن خالی است لطفا توکن را وارد کنید" });
            }
        }





    }

}

