using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data.SubscriptionsViewModels;
using V2boardApi.Models;
using NLog;
using V2boardApi.Tools;
using System.Text;
using System.Globalization;
using MySql.Data.MySqlClient;
using System.Data.Entity;
using System.Threading.Tasks;

namespace V2boardApi.Areas.App.Controllers
{
    public class SubscriptionsController : Controller
    {

        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private Repository<tbLogs> logsRepository;
        private Repository<tbUsers> usersRepository;
        private Repository<tbPlans> plansRepository;
        private Repository<tbLinkUserAndPlans> linkUserAndPlansRepository;
        private Repository<tbServers> serverRepository;
        private Entities db;
        public SubscriptionsController()
        {
            db = new Entities();
            logsRepository = new Repository<tbLogs>(db);
            usersRepository = new Repository<tbUsers>(db);
            plansRepository = new Repository<tbPlans>(db);
            linkUserAndPlansRepository = new Repository<tbLinkUserAndPlans>(db);
            serverRepository = new Repository<tbServers>(db);
        }

        #region لیست کاربران

        // GET: App/Subscriptions
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1,2")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var draw = Request.Form.GetValues("draw").FirstOrDefault();
                var start = Request.Form.GetValues("start").FirstOrDefault();
                var length = Request.Form.GetValues("length").FirstOrDefault();
                var searchValue = Request.Form.GetValues("search[value]").FirstOrDefault();
                var sortColumnIndex = Request.Form.GetValues("order[0][column]").FirstOrDefault();
                var sortColumnDir = Request.Form.GetValues("order[0][dir]").FirstOrDefault();

                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;

                var user = await usersRepository.table.FirstOrDefaultAsync(p => p.Username == User.Identity.Name);
                if (user?.tbServers == null)
                {
                    return MessageBox.Error("خطا", "خطا در دریافت داده از سمت سرور");
                }

                string baseQuery = "SELECT v2.id, v2.email, t, u, d, v2.transfer_enable, banned, token, expired_at, pl.name FROM `v2_user` AS v2 JOIN v2_plan AS pl ON plan_id = pl.id WHERE ";
                string searchQuery;

                if (!string.IsNullOrEmpty(searchValue))
                {
                    if (searchValue.Contains("token="))
                    {
                        var tokenValue = searchValue.Split('=')[1];
                        searchQuery = $"token='{tokenValue}' AND email LIKE '%@{user.Username}%'";
                    }
                    else
                    {
                        searchQuery = $"email LIKE '%{searchValue}%' AND email LIKE '%@{user.Username}%'";
                    }
                }
                else
                {
                    searchQuery = $"email LIKE '%@{user.Username}%'";
                }

                string query = baseQuery + searchQuery;

                // Sorting
                switch (sortColumnIndex)
                {
                    case "0":
                        query += $" ORDER BY email {sortColumnDir}";
                        break;
                    case "1":
                        query += $" ORDER BY transfer_enable {sortColumnDir}";
                        break;
                    case "2":
                        query += $" ORDER BY t {sortColumnDir}";
                        break;
                    case "5":
                        query += $" ORDER BY expired_at {sortColumnDir}";
                        break;
                    default:
                        query += " ORDER BY v2.id DESC";
                        break;
                }

                query += pageSize > 0 ? $" LIMIT {skip}, {pageSize}" : "";

                using (var mySqlEntities = new MySqlEntities2(user.tbServers.ConnectionString))
                {
                    await mySqlEntities.OpenAsync();
                    using (var reader = await mySqlEntities.GetDataAsync(query))
                    {
                        List<GetUserDataModel> users = new List<GetUserDataModel>();
                        while (await reader.ReadAsync())
                        {
                            if (reader.HasRows)
                            {
                                var getuserData = new GetUserDataModel
                                {
                                    id = reader.GetInt32(reader.GetOrdinal("id")),
                                    Name = reader.GetString(reader.GetOrdinal("email")).Split('@')[0],
                                    TotalVolume = Utility.ConvertByteToGB(reader.GetInt64(reader.GetOrdinal("transfer_enable"))).ToString(),
                                    PlanName = reader.GetString(reader.GetOrdinal("name")),
                                    IsBanned = Convert.ToBoolean(reader.GetSByte(reader.GetOrdinal("banned"))),
                                    IsActive = 1,
                                    SubLink = $"https://{user.tbServers.SubAddress}/api/v1/client/subscribe?token={reader.GetString(reader.GetOrdinal("token"))}"
                                };

                                var exp = reader["expired_at"].ToString();
                                if (!string.IsNullOrEmpty(exp))
                                {
                                    var e = Convert.ToInt64(exp);
                                    var ex = Utility.ConvertSecondToDatetime(e);
                                    getuserData.DaysLeft = Utility.CalculateLeftDayes(ex);
                                    if (getuserData.DaysLeft <= 2) getuserData.IsActive = 5;
                                    if (ex <= DateTime.Now) getuserData.IsActive = 2;
                                }
                                else
                                {
                                    getuserData.DaysLeft = -1;
                                }
                                var onlineTime = reader["t"].ToString();
                                if (onlineTime != "0")
                                {
                                    var onlineTimeDt = Utility.ConvertSecondToDatetime(Convert.ToInt64(onlineTime));

                                    if (onlineTimeDt >= DateTime.Now.AddMinutes(-1))
                                    {
                                        getuserData.IsOnline = true;
                                    }
                                    else
                                    {
                                        getuserData.LastTimeOnline = Utility.ConvertDatetimeToShamsiDate(onlineTimeDt);
                                    }
                                }
                                if (getuserData.LastTimeOnline == null)
                                {
                                    getuserData.LastTimeOnline = "آنلاین نشده";
                                }
                                var u = reader.GetInt64(reader.GetOrdinal("u"));
                                var d = reader.GetInt64(reader.GetOrdinal("d"));
                                var usedVolume = Utility.ConvertByteToGB(u + d);
                                getuserData.UsedVolume = $"{Math.Round(usedVolume, 2)}";

                                var remainingVolume = reader.GetInt64(reader.GetOrdinal("transfer_enable")) - (u + d);
                                var remainingVolumeGB = Utility.ConvertByteToGB(remainingVolume);
                                if (remainingVolumeGB <= 2) getuserData.CanEdit = true;
                                if (remainingVolume <= 0) getuserData.IsActive = 3;
                                getuserData.RemainingVolume = $"{Math.Round(remainingVolumeGB, 2)}";

                                if (Convert.ToBoolean(reader.GetInt16(reader.GetOrdinal("banned"))))
                                {
                                    getuserData.IsActive = 4;
                                }

                                users.Add(getuserData);
                            }
                        }

                        // Ensure the reader is closed before proceeding
                        reader.Close();

                        var countQuery = $"SELECT COUNT(id) AS Count FROM `v2_user` WHERE email LIKE '%@{user.Username}%'";
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            countQuery = searchValue.Contains("token=")
                                ? $"SELECT COUNT(id) AS Count FROM `v2_user` WHERE token='{searchValue.Split('=')[1]}' AND email LIKE '%@{user.Username}%'"
                                : $"SELECT COUNT(id) AS Count FROM `v2_user` WHERE email LIKE '%{searchValue}%' AND email LIKE '%@{user.Username}%'";
                        }


                        using (var countCommand = new MySqlCommand(countQuery, mySqlEntities.MySqlConnection))
                        {
                            using (var countReader = await countCommand.ExecuteReaderAsync())
                            {
                                await countReader.ReadAsync();
                                var totalRecords = countReader.GetInt32(countReader.GetOrdinal("Count"));


                                await mySqlEntities.CloseAysnc(mySqlEntities.MySqlConnection);
                                return Json(new
                                {
                                    draw,
                                    recordsTotal = totalRecords,
                                    recordsFiltered = totalRecords,
                                    data = users
                                }, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }

                   
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "نمایش لیست اشتراکات در پنل فروش با خطا مواجه شد");
                return MessageBox.Error("خطا", "خطا در دریافت داده از سمت سرور");
            }
        }









        #endregion

        #region افزودن اشتراک

        [AuthorizeApp(Roles = "1,2")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(string userSubname, int userPlan)
        {
            try
            {
                if (!string.IsNullOrEmpty(userSubname))
                {
                    userSubname = userSubname.ToLower();
                    if (userPlan != 0)
                    {
                        var user = usersRepository.table.Where(p => p.Username == User.Identity.Name && p.Status == true).FirstOrDefault();
                        if (user != null)
                        {
                            var Log = logsRepository.Where(p => p.FK_NameUser_ID == userSubname && p.tbLinkUserAndPlans.L_FK_U_ID == user.User_ID).ToList().LastOrDefault();
                            if (Log != null)
                            {
                                return MessageBox.Warning("هشدار", "این کاربر از قبل وجود دارد");
                            }

                            if ((user.Limit - user.Wallet) >= 0)
                            {

                                var plan = plansRepository.table.Where(p => p.Plan_ID == userPlan && p.FK_Server_ID == user.FK_Server_ID && p.Status == true).FirstOrDefault();
                                if ((plan.Price + user.Wallet) > user.Limit)
                                {
                                    return MessageBox.Warning("هشدار", "مبلغ تعرفه انتخابی بیشتر از موجودی حساب شما می باشد لطفا بدهی خود را پرداخت کنید");
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
                                var emilprx = userSubname + "@" + user.Username;

                                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
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
                                var link = linkUserAndPlansRepository.table.Where(p => p.L_FK_U_ID == user.User_ID && p.L_FK_P_ID == plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                                user.Wallet += link.tbPlans.Price;


                                linkUserAndPlansRepository.Save();
                                usersRepository.Save();
                                AddLog(Resource.LogActions.U_Created, link.Link_PU_ID, userSubname, (int)plan.Price, plan.Plan_Name);
                                logger.Info("اشتراک جدید توسط نماینده ایجاد گردید");
                                return Toaster.Success("موفق", "اشتراک با موفقیت ایجاد گردید");
                            }
                            else
                            {

                                var Count = user.Limit;

                                StringBuilder str = new StringBuilder();
                                str.Append(" شما اجازه ساخت بیشتر از مبلغ ");
                                str.Append(string.Format("{0:C0}", Count).Replace("$", ""));
                                str.Append(" تومان");
                                str.Append(" را ندارید");
                                str.Append(" لطفا بدهی خود را پرداخت کنید تا محدودیت 0 شود ");

                                return MessageBox.Warning("هشدار", str.ToString());
                            }
                        }
                        else
                        {
                            return MessageBox.Warning("هشدار", "حساب کاربری شما غیرفعال شده است لطفا با پشتیبانی ارتباط بگیرید");
                        }
                    }
                    else
                    {
                        return MessageBox.Warning("هشدار", "لطفا پلن را انتخاب کنید");
                    }
                }
                else
                {
                    return MessageBox.Warning("هشدار", "لطفا نام اشتراک را وارد کنید");
                }

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(userSubname))
                {
                    return MessageBox.Warning("هشدار", "این کاربر از قبل وجود دارد");
                }
                logger.Error(ex, "در ساخت اشتراک در پنل فروش با خطایی مواجه شدیم");
                return MessageBox.Warning("هشدار", "خطا در برقراری ارتباط با سرور");
            }

        }

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
                logsRepository.Insert(tbLogs);
                logger.Info("لاگ ساخت اشتراک اضافه شد");
                return logsRepository.Save();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "در لاگ ساخت اشتراک خطایی رخ داد");
                return false;
            }
        }

        #endregion


        #endregion

        #region ویرایش اشتراک

        [AuthorizeApp(Roles = "1")]
        public ActionResult Edit(int user_id)
        {
            var user = usersRepository.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (user.tbServers != null)
            {
                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                mySql.Open();

                var read = mySql.GetData("SELECT v2_user.email,v2_user.transfer_enable,v2_user.expired_at,v2_user.speed_limit FROM `v2_user` WHERE id =" + user_id);
                if (read.Read())
                {
                    var Traffic = Utility.ConvertByteToGB(read.GetDouble("transfer_enable"));
                    var Subname = read.GetBodyDefinition("email").Split('@')[0];
                    var Date = read.GetBodyDefinition("expired_at");
                    var SpeedLimit = read.GetBodyDefinition("speed_limit");
                    var ShamsiDate = "";
                    if (Date != "")
                    {
                        ShamsiDate = Utility.ConvertMillisecondToShamsiDate(Convert.ToInt64(Date));
                    }


                    return Json(new { data = new { userSubname = Subname, userTraffic = Traffic, userSpeed = SpeedLimit, userExpire = ShamsiDate }, status = "success" }, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    return MessageBox.Warning("موفق", "اطلاعات سرور یافت نشد");
                }
            }
            else
            {
                return MessageBox.Error("موفق", "اطلاعات سرور یافت نشد");
            }
        }


        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int user_id, string userSubname, string userSpeed, string userExpire, double userTraffic)
        {

            try
            {
                var user = usersRepository.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

                MySqlEntities mysql = new MySqlEntities(user.tbServers.ConnectionString);
                mysql.Open();

                var Miladi = new DateTime();
                try
                {
                    Miladi = Convert.ToDateTime(userExpire, CultureInfo.GetCultureInfo("fa-IR"));
                    Miladi = Miladi.AddDays(1);
                }
                catch (Exception ex)
                {
                    return MessageBox.Warning("هشدار", "لطفا تاریخ را صحیح وارد کنید");
                }

                string Speed = "NULL";
                double MiliSecoundTime = Utility.ConvertDatetimeToSecond(Miladi);
                if (userSpeed != "")
                {
                    Speed = userSpeed;
                }
                var transfe_enable = Utility.ConvertGBToByte(userTraffic);
                var read = mysql.GetData("select v2_user.email FROM `v2_user` where id=" + user_id);
                if (read.Read())
                {
                    userSubname += "@" + read.GetBodyDefinition("email").Split('@')[1];
                }
                read.Close();


                var Query = "update v2_user set v2_user.email='" + userSubname + "', v2_user.speed_limit=" + Speed + ", v2_user.expired_at=" + MiliSecoundTime + ", v2_user.transfer_enable=" + transfe_enable + " where v2_user.id=" + user_id;

                try
                {
                    read = mysql.GetData(Query);
                }
                catch (Exception ex)
                {
                    return MessageBox.Warning("هشدار", "کاربری با این نام وجود دارد لطفا نام دیگری وارد کنید");
                }

                return Toaster.Success("موفق", "اطلاعات اشتراک با موفقیت ویرایش شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ویرایش اطلاعات اشتراک با خطا مواجه شد");
                return MessageBox.Error("هشدار", "ویرایش اطلاعات اشتراک با خطا مواجه شد لطفا مجدد تست کنید");
            }
        }

        #endregion

        #region مسدودی کاربر

        [AuthorizeApp(Roles = "1,2")]
        public ActionResult BanUser(int user_id, bool status)
        {
            try
            {
                var user = usersRepository.table.Where(p => p.Username == User.Identity.Name && p.Status == true).FirstOrDefault();
                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                mySql.Open();
                var Query = "update v2_user set banned = " + Convert.ToInt16(status) + " where email like '%" + user.Username + "%' and id =" + user_id;
                var reader = mySql.GetData(Query);
                var res = reader.Read();

                var state = "رفع مسدود";
                if (status)
                {
                    state = "مسدود";
                }

                var mess = " اشتراک با موفقیت " + state + " شد ";
                logger.Info(mess);

                return Toaster.Success("موفق", mess);

            }
            catch (Exception ex)
            {
                logger.Error(ex, "در مسدود سازی اشتراک خطایی رخ داد");
                return Toaster.Error("ناموفق", "خطایی در مسدود سازی اشتراک رخ داد");
            }
        }

        #endregion

        #region تمدید اکانت
        [System.Web.Http.HttpPost]
        [AuthorizeApp(Roles = "1,2")]
        [ValidateAntiForgeryToken]
        public ActionResult Renew(int user_id, int userPlan)
        {

            var user = usersRepository.table.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                if ((user.Limit - user.Wallet) >= 0)
                {
                    var Server = user.tbServers;


                    var Plan = plansRepository.table.Where(p => p.Plan_ID == userPlan && p.FK_Server_ID == Server.ServerID && p.Status == true).FirstOrDefault();
                    if ((Plan.Price + user.Wallet) > user.Limit)
                    {
                        return Toaster.Success("موفق", "مبلغ تعرفه انتخابی بیشتر از موجودی حساب شما می باشد لطفا بدهی خود را پرداخت کنید");
                    }

                    var t = Utility.ConvertGBToByte(System.Convert.ToInt64(Plan.PlanVolume));
                    string exp = "";
                    if (Plan.CountDayes == 0)
                    {
                        exp = "NULL";
                    }
                    else
                    {
                        exp = DateTime.Now.AddDays((int)Plan.CountDayes).ConvertDatetimeToSecond().ToString();
                    }

                    var Query = "update v2_user set u = 0 , d = 0 , t = 0 ,plan_id=" + Plan.Plan_ID_V2 + ", transfer_enable = " + t + " , expired_at = " + exp + " where id =" + user_id;

                    MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                    mySql.Open();
                    var reader = mySql.GetData(Query);
                    reader.Read();
                    reader.Close();

                    var Query2 = "SELECT email FROM `v2_user` WHERE id=" + user_id;

                    MySqlEntities mySql2 = new MySqlEntities(user.tbServers.ConnectionString);
                    mySql2.Open();
                    var reader2 = mySql2.GetData(Query2);
                    if (reader2.Read())
                    {
                        var link = linkUserAndPlansRepository.table.Where(p => p.L_FK_U_ID == user.User_ID && p.L_FK_P_ID == Plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                        user.Wallet += link.tbPlans.Price;

                        AddLog(Resource.LogActions.U_Edited, link.Link_PU_ID, reader2.GetString("email").Split('@')[0], (int)Plan.Price, Plan.Plan_Name);
                    }
                    reader2.Close();
                    linkUserAndPlansRepository.Save();
                    usersRepository.Save();
                    logger.Info("اشتراک با موفقیت تمدید شد");
                    return Toaster.Success("موفق", "اشتراک با موفقیت تمدید شد");


                }
                else
                {
                    var Count = user.Limit;

                    StringBuilder str = new StringBuilder();
                    str.Append(" شما اجازه ساخت بیشتر از مبلغ ");
                    str.Append(string.Format("{0:C0}", Count).Replace("$", ""));
                    str.Append(" تومان");
                    str.Append(" را ندارید");
                    str.Append(" لطفا بدهی خود را پرداخت کنید تا محدودیت 0 شود ");

                    return Toaster.Success("موفق", str.ToString());
                }

            }
            else
            {
                return Toaster.Success("ناموفق", "خطا در تمدید اشتراک");
            }


        }

        #endregion

        #region ریست لینک اکانت
        [System.Web.Http.HttpPost]
        [AuthorizeApp(Roles = "1,2")]
        public ActionResult Reset(int user_id)
        {
            var user = usersRepository.table.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

            if (user != null)
            {
                var Server = user.tbServers;

                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                mySql.Open();
                string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                var query = "update v2_user set token = '" + token + "',uuid='" + Guid.NewGuid() + "' where id=" + user_id;
                var reader = mySql.GetData(query);

                logger.Info("لینک اشتراک با موفقیت تغییر یافت");

                return Toaster.Success("موفق", "لینک با موفقیت تغییر کرد");
            }
            else
            {
                return Toaster.Success("هشدار", "کاربر یافت نشد");
            }


        }
        #endregion

        #region حذف لینک

        public ActionResult delete(int user_id)
        {
            try
            {
                var user = usersRepository.table.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

                var Server = user.tbServers;

                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                mySql.Open();

                var Query = "delete from v2_user where id=" + user_id;
                var reader = mySql.GetData(Query);


                logger.Info("اشتراک حذف شد");
                return Toaster.Success("موفق", "اشتراک با موفقیت حذف شد");

            }
            catch (Exception ex)
            {
                return Toaster.Error("ناموفق", "حذف اشتراک با خطا مواجه شد");
            }

        }


        #endregion

        #region اطلاعات کیف پول

        [AuthorizeApp(Roles ="1,2")]
        public ActionResult _Wallet()
        {
            var user = usersRepository.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

            return PartialView(user);

        }

        #endregion
    }
}