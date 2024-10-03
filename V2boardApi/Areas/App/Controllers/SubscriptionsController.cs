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
using Telegram.Bot.Types;
using static Stimulsoft.Base.Drawing.StiFontReader;
using System.Numerics;
using V2boardApi.Models.V2boardModel;

namespace V2boardApi.Areas.App.Controllers
{
    [LogActionFilter]
    public class SubscriptionsController : Controller
    {

        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private Repository<tbLogs> logsRepository { get; set; }
        private Repository<tbUsers> usersRepository { get; set; }
        private Repository<tbPlans> plansRepository { get; set; }
        private Repository<tbServerGroups> groupsRepository { get; set; }
        private Repository<tbLinkUserAndPlans> linkUserAndPlansRepository { get; set; }
        private Repository<tbServers> serverRepository { get; set; }
        private Repository<tbLinkServerGroupWithUsers> linkUserGroupRepository { get; set; }
        private Entities db { get; set; }
        public SubscriptionsController()
        {
            db = new Entities();
            logsRepository = new Repository<tbLogs>(db);
            usersRepository = new Repository<tbUsers>(db);
            plansRepository = new Repository<tbPlans>(db);
            linkUserAndPlansRepository = new Repository<tbLinkUserAndPlans>(db);
            serverRepository = new Repository<tbServers>(db);
            linkUserGroupRepository = new Repository<tbLinkServerGroupWithUsers>(db);
            groupsRepository = new Repository<tbServerGroups>(db);
        }

        #region لیست اشتراک ها 

        // GET: App/Subscriptions
        [AuthorizeApp(Roles = "1,2,3,4")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
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

                // دریافت نقش کاربر
                var userRole = user.Role.Value; // فرض می‌کنیم نقش کاربر در user.Role ذخیره شده است

                string baseQuery = "SELECT v2.id, v2.email, t, u, d, v2.transfer_enable, banned, token, expired_at, pl.name " +
                                   "FROM `v2_user` AS v2 JOIN v2_plan AS pl ON plan_id = pl.id WHERE 1=1 ";
                string searchQuery = "";

                if (!string.IsNullOrEmpty(searchValue))
                {
                    if (searchValue.Contains("token="))
                    {
                        var tokenValue = searchValue.Split('=')[1];
                        searchQuery += $" AND token='{tokenValue}'";
                        if (userRole == 2 || userRole == 3 || userRole == 4) // اگر نقش برابر 2 بود، فیلتر Username را اضافه می‌کنیم
                        {
                            if (userRole == 4)
                            {
                                searchQuery += $" AND (";
                                var Counter = 1;
                                foreach (var item in user.tbUsers1)
                                {


                                    if (Counter == user.tbUsers1.Count)
                                    {
                                        searchQuery += $"email LIKE '%@{item.Username}%')";
                                    }
                                    else
                                    {
                                        searchQuery += $"email LIKE '%@{item.Username}%' OR  ";
                                    }
                                    Counter++;

                                }


                            }
                            else
                            {
                                searchQuery += $" AND email LIKE '%@{user.Username}%'";
                            }

                        }
                    }
                    else
                    {
                        searchQuery += $" AND email LIKE '%{searchValue}%'";
                        if (userRole == 2 || userRole == 3 || userRole == 4) // اگر نقش برابر 2 بود، فیلتر Username را اضافه می‌کنیم
                        {
                            if (userRole == 4)
                            {
                                searchQuery += $" AND (";
                                var Counter = 1;
                                foreach (var item in user.tbUsers1)
                                {


                                    if (Counter == user.tbUsers1.Count)
                                    {
                                        searchQuery += $"email LIKE '%@{item.Username}%')";
                                    }
                                    else
                                    {
                                        searchQuery += $"email LIKE '%@{item.Username}%' OR  ";
                                    }
                                    Counter++;

                                }


                            }
                            else
                            {
                                searchQuery += $" AND email LIKE '%@{user.Username}%'";
                            }
                        }
                    }
                }
                else if (userRole == 2 || userRole == 3 || userRole == 4) // اگر نقش برابر 2 بود، فیلتر Username را اضافه می‌کنیم
                {
                    if (userRole == 4)
                    {
                        searchQuery += $" AND (";
                        var Counter = 1;
                        foreach (var item in user.tbUsers1)
                        {


                            if (Counter == user.tbUsers1.Count)
                            {
                                searchQuery += $"email LIKE '%@{item.Username}%')";
                            }
                            else
                            {
                                searchQuery += $"email LIKE '%@{item.Username}%' OR  ";
                            }
                            Counter++;

                        }


                    }
                    else
                    {
                        searchQuery += $" AND email LIKE '%@{user.Username}%'";
                    }
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
                List<GetUserDataModel> users = new List<GetUserDataModel>();
                using (var mySqlEntities = new MySqlEntities(user.tbServers.ConnectionString))
                {
                    await mySqlEntities.OpenAsync();
                    using (var reader = await mySqlEntities.GetDataAsync(query))
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.HasRows)
                            {
                                var getuserData = new GetUserDataModel
                                {
                                    id = reader.GetInt32(reader.GetOrdinal("id")),

                                    TotalVolume = Utility.ConvertByteToGB(reader.GetInt64(reader.GetOrdinal("transfer_enable"))).ToString(),
                                    PlanName = reader.GetString(reader.GetOrdinal("name")),
                                    IsBanned = Convert.ToBoolean(reader.GetSByte(reader.GetOrdinal("banned"))),
                                    Name = reader.GetString(reader.GetOrdinal("email")),
                                    IsActive = 1,
                                    SubLink = $"https://{user.tbServers.SubAddress}/api/v1/client/subscribe?token={reader.GetString(reader.GetOrdinal("token"))}"
                                };

                                var exp = reader["expired_at"].ToString();
                                if (!string.IsNullOrEmpty(exp))
                                {
                                    var e = Convert.ToInt64(exp);
                                    var ex = Utility.ConvertSecondToDatetime(e);
                                    getuserData.ExpireDate = Utility.ConvertDateTimeToShamsi2(ex);
                                    getuserData.DaysLeft = Utility.CalculateLeftDayes(ex);
                                    if (getuserData.DaysLeft <= 2) getuserData.IsActive = 5;
                                    if (ex <= DateTime.Now) getuserData.IsActive = 2;
                                }
                                else
                                {
                                    getuserData.ExpireDate = "بدون محدودیت";
                                    getuserData.DaysLeft = -1;
                                }
                                var onlineTime = reader["t"].ToString();
                                if (onlineTime != "0")
                                {
                                    var onlineTimeDt = Utility.ConvertSecondToDatetime(Convert.ToInt64(onlineTime));

                                    if (onlineTimeDt >= DateTime.Now.AddSeconds(-60))
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
                    }

                    var countQuery = "SELECT COUNT(*) AS Count FROM `v2_user` WHERE 1=1";
                    if (userRole == 2 || userRole == 3 || userRole == 4) // اگر نقش برابر 2 بود، فیلتر Username را اضافه می‌کنیم
                    {
                        if (userRole == 4)
                        {
                            countQuery += $" AND (";
                            var Counter = 1;
                            foreach (var item in user.tbUsers1)
                            {


                                if (Counter == user.tbUsers1.Count)
                                {
                                    countQuery += $"email LIKE '%@{item.Username}%' )";
                                }
                                else
                                {
                                    countQuery += $"email LIKE '%@{item.Username}%' OR  ";
                                }
                                Counter++;

                            }


                        }
                        else
                        {
                            countQuery += $" AND email LIKE '%@{user.Username}%'";
                        }
                    }

                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        if (searchValue.Contains("token="))
                        {
                            var tokenValue = searchValue.Split('=')[1];
                            countQuery += $" AND token='{tokenValue}'";
                        }
                        else
                        {
                            countQuery += $" AND email LIKE '%{searchValue}%'";
                        }
                    }

                    using (var reader = await mySqlEntities.GetDataAsync(countQuery))
                    {
                        await reader.ReadAsync();
                        var totalRecords = reader.GetInt32(reader.GetOrdinal("Count"));

                        await mySqlEntities.CloseAsync();
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
            catch (Exception ex)
            {
                logger.Error(ex, "نمایش لیست اشتراکات در پنل فروش با خطا مواجه شد");
                return MessageBox.Error("خطا", "خطا در دریافت داده از سمت سرور");
            }
        }











        #endregion

        #region افزودن اشتراک

        [AuthorizeApp(Roles = "1,2,3,4")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateUser(string userSubname, int userPlan)
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

                                var plan = plansRepository.table.Where(p => p.Plan_ID == userPlan && p.FK_Server_ID == user.FK_Server_ID).FirstOrDefault();
                                if (plan != null)
                                {
                                    if ((plan.Price + user.Wallet) > user.Limit)
                                    {
                                        return MessageBox.Warning("هشدار", "مبلغ تعرفه انتخابی بیشتر از موجودی حساب شما می باشد لطفا بدهی خود را پرداخت کنید");
                                    }
                                    string exp = "";
                                    if (plan.PlanMonth == 0)
                                    {
                                        exp = null;
                                    }
                                    else
                                    {
                                        exp = DateTime.Now.AddMonths((int)plan.PlanMonth).ConvertDatetimeToSecond().ToString();
                                    }



                                    if (user.Role == 3)
                                    {
                                        if (user.tbUsers2 != null)
                                        {
                                            var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == plan.Group_Id && s.FK_User_Id == user.User_ID);
                                            user.Wallet += (plan.PlanVolume * (linkGroupUser.PriceForGig)) + (plan.PlanMonth * linkGroupUser.PriceForMonth);
                                        }
                                        else
                                        {
                                            return MessageBox.Warning("هشدار", "مدیر والدی برای شما تعریف نشده است لطفا با مدیر سامانه تماس بگیرید !!");
                                        }
                                    }

                                    if (user.Role == 2)
                                    {
                                        if (user.tbUsers2 != null)
                                        {
                                            if (user.tbUsers2.Wallet >= user.tbUsers2.Limit)
                                            {
                                                return MessageBox.Warning("هشدار", "فروش موقتا توسط ادمین متوقف شده است لطفا با پشتیبانی ارتباط بگیرید !!");
                                            }
                                            if (user.tbUsers2.Role != 1 && user.tbUsers2.Role == 3)
                                            {
                                                var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == plan.Group_Id && s.FK_User_Id == user.tbUsers2.User_ID);

                                                user.tbUsers2.Wallet += (plan.PlanVolume * (linkGroupUser.PriceForGig)) + (plan.PlanMonth * linkGroupUser.PriceForMonth);
                                            }
                                        }
                                        else
                                        {
                                            return MessageBox.Warning("هشدار", "مدیر والدی برای شما تعریف نشده است لطفا با مدیر سامانه تماس بگیرید !!");
                                        }
                                    }

                                    var create = DateTime.Now.ConvertDatetimeToSecond().ToString();
                                    var planid = plan.Plan_ID_V2;
                                    var emilprx = userSubname + "@" + user.Username;

                                    MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                                    await mySql.OpenAsync();

                                    string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                                    var group = await groupsRepository.FirstOrDefaultAsync(s => s.Group_Id == plan.Group_Id);
                                    var Disc3 = new Dictionary<string, object>();
                                    Disc3.Add("@FullName", emilprx);
                                    Disc3.Add("@expired", exp);
                                    Disc3.Add("@create", create);
                                    Disc3.Add("@guid", Guid.NewGuid());
                                    var vol = Utility.ConvertGBToByte(plan.PlanVolume.Value);
                                    Disc3.Add("@tran", vol);
                                    Disc3.Add("@grid", group.V2_Group_Id);
                                    Disc3.Add("@planid", planid);
                                    Disc3.Add("@token", token);
                                    var DeviceLimit = "";
                                    var DeviceLimitCol = "";

                                    if (plan.device_limit == null || plan.device_limit.Value == 0)
                                    {

                                    }
                                    else
                                    {
                                        Disc3.Add("@device_limit", plan.device_limit);
                                        DeviceLimit = ",@device_limit";
                                        DeviceLimitCol = ",device_limit";
                                    }

                                    string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at" + DeviceLimitCol + ") VALUES (@FullName,@expired,@create,@guid,0,0,0,@tran,0,@grid,@planid,@token,'" + Guid.NewGuid() + "',@create " + DeviceLimit + " )";

                                    var reader = await mySql.GetDataAsync(Query, Disc3);
                                    reader.Close();
                                    var link = linkUserAndPlansRepository.table.Where(p => p.L_FK_U_ID == user.User_ID && p.L_FK_P_ID == plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                                    if (user.Role == 2)
                                    {
                                        user.Wallet += link.tbPlans.Price;
                                    }



                                    await mySql.CloseAsync();
                                    linkUserAndPlansRepository.Save();
                                    usersRepository.Save();
                                    AddLog(Resource.LogActions.U_Created, link.Link_PU_ID, userSubname, (int)plan.Price, plan.Plan_Name, plan.PlanVolume.Value, plan.PlanMonth.Value);
                                    logger.Info("اشتراک جدید توسط نماینده ایجاد گردید");
                                    return Toaster.Success("موفق", "اشتراک با موفقیت ایجاد گردید");
                                }
                                else
                                {
                                    logger.Warn("عدم پیدا کردن تعرفه " + userPlan);
                                    return MessageBox.Warning("هشدار", "تعرفه مورد نظر یافت نشد لطفا با پشتیبانی ارتباط بگیرید !!");
                                }
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
        private bool AddLog(string Action, int LinkUserID, string V2User, int price, string planName, int planVolume, int planMonth)
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
                tbLogs.PlanVolume = planVolume;
                tbLogs.PlanMonth = planMonth;
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

        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> Edit(int user_id)
        {
            var user = usersRepository.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (user.tbServers != null)
            {
                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                await mySql.OpenAsync();

                var read = await mySql.GetDataAsync("SELECT v2_user.email,v2_user.transfer_enable,v2_user.expired_at,v2_user.speed_limit FROM `v2_user` WHERE id =" + user_id);
                if (await read.ReadAsync())
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


                    await mySql.CloseAsync();
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


        [AuthorizeApp(Roles = "1,3,4")]
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int user_id, string userSubname, string userSpeed, string userExpire, double userTraffic)
        {

            try
            {
                if (userSubname.Length <= 50 && !userSubname.Contains('@'))
                {
                    var user = usersRepository.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

                    MySqlEntities mysql = new MySqlEntities(user.tbServers.ConnectionString);
                    await mysql.OpenAsync();

                    var Miladi = new DateTime();
                    if (userExpire != "")
                    {
                        try
                        {
                            Miladi = Convert.ToDateTime(userExpire, CultureInfo.GetCultureInfo("fa-IR"));
                            Miladi = Miladi.AddHours(12);
                        }
                        catch (Exception ex)
                        {
                            return MessageBox.Warning("هشدار", "لطفا تاریخ را صحیح وارد کنید");
                        }
                    }

                    string Speed = null;
                    string MiliSecoundTime = null;
                    if (Miladi != default(DateTime))
                    {
                        double MiliSecoundTime1 = Utility.ConvertDatetimeToSecond(Miladi);
                        MiliSecoundTime = MiliSecoundTime1.ToString();
                    }

                    if (userSpeed != "")
                    {
                        Speed = userSpeed;
                    }
                    string name = "";
                    string username = "";
                    var transfe_enable = Utility.ConvertGBToByte(userTraffic);
                    var read = await mysql.GetDataAsync("select v2_user.email FROM `v2_user` where id=" + user_id);
                    if (await read.ReadAsync())
                    {
                        userSubname += "@" + read.GetBodyDefinition("email").Split('@')[1];
                        name = read.GetBodyDefinition("email").Split('@')[0];
                        username = read.GetBodyDefinition("email").Split('@')[1];
                        read.Close();
                    }
                    if ((name + "@" + username) != userSubname)
                    {
                        var log = await logsRepository.FirstOrDefaultAsync(s => s.FK_NameUser_ID == name && s.tbLinkUserAndPlans.tbUsers.Username == username);
                        log.FK_NameUser_ID = userSubname.Split('@')[0];
                    }


                    var Disc1 = new Dictionary<string, object>();
                    Disc1.Add("@userSubname", userSubname);
                    Disc1.Add("@Speed", Speed);
                    Disc1.Add("@MiliSecoundTime", MiliSecoundTime);
                    Disc1.Add("@transfe_enable", transfe_enable);

                    var Query = "update v2_user set v2_user.email=@userSubname, v2_user.speed_limit=@Speed, v2_user.expired_at=@MiliSecoundTime, v2_user.transfer_enable=@transfe_enable where v2_user.id=" + user_id;

                    try
                    {
                        read = await mysql.GetDataAsync(Query, Disc1);
                    }
                    catch (Exception ex)
                    {
                        return MessageBox.Warning("هشدار", "کاربری با این نام وجود دارد لطفا نام دیگری وارد کنید");
                    }

                    await logsRepository.SaveChangesAsync();

                    return Toaster.Success("موفق", "اطلاعات اشتراک با موفقیت ویرایش شد");
                }
                else
                {
                    return MessageBox.Warning("هشدار", "نام اشتراک نمی تواند بزرگتر از 50 حرف یا شامل کاراکتر @ باشد");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ویرایش اطلاعات اشتراک با خطا مواجه شد");
                return MessageBox.Error("هشدار", "ویرایش اطلاعات اشتراک با خطا مواجه شد لطفا مجدد تست کنید");
            }
        }

        #endregion

        #region مسدودی کاربر

        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> BanUser(int user_id, bool status)
        {
            try
            {
                var user = usersRepository.table.Where(p => p.Username == User.Identity.Name && p.Status == true).FirstOrDefault();
                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                await mySql.OpenAsync();
                var Query = "update v2_user set banned = " + Convert.ToInt16(status) + " where email like '%" + user.Username + "%' and id =" + user_id;
                var reader = await mySql.GetDataAsync(Query);
                var res = await reader.ReadAsync();

                var state = "رفع مسدود";
                if (status)
                {
                    state = "مسدود";
                }

                var mess = " اشتراک با موفقیت " + state + " شد ";
                logger.Info(mess);
                await mySql.CloseAsync();
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
        [AuthorizeApp(Roles = "1,2,3,4")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Renew(int user_id, int userPlan)
        {

            try
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
                        if (Plan.PlanMonth == 0)
                        {
                            exp = null;
                        }
                        else
                        {
                            exp = DateTime.Now.AddMonths((int)Plan.PlanMonth).ConvertDatetimeToSecond().ToString();
                        }
                        //چک می کنیم اگر نماینده بود از بر اساس محاسبات نماینده کل از کیف پولش کسر میکنیم
                        if (user.Role == 3)
                        {
                            if (user.tbUsers2 != null)
                            {
                                var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == Plan.Group_Id && s.FK_User_Id == user.User_ID);

                                user.Wallet += (Plan.PlanVolume * (linkGroupUser.PriceForGig)) + (Plan.PlanMonth * linkGroupUser.PriceForMonth);
                            }
                            else
                            {
                                return MessageBox.Warning("هشدار", "مدیر والدی برای شما تعریف نشده است لطفا با مدیر سامانه تماس بگیرید !!");
                            }
                        }
                        //چک می کنیم اگر نماینده معمولی بود هزینه رو بر اساس محاسبات نماینده کل از کیف پول نماینده کل کسر میکنیم
                        if (user.Role == 2)
                        {
                            if (user.tbUsers2 != null)
                            {
                                if (user.tbUsers2.Wallet >= user.tbUsers2.Limit)
                                {
                                    return MessageBox.Warning("هشدار", "فروش موقتا توسط ادمین متوقف شده است لطفا با پشتیبانی ارتباط بگیرید !!");
                                }
                                if (user.tbUsers2.Role != 1 && user.tbUsers2.Role == 3)
                                {
                                    var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == Plan.Group_Id && s.FK_User_Id == user.tbUsers2.User_ID);
                                    user.tbUsers2.Wallet += (Plan.PlanVolume * (linkGroupUser.PriceForGig)) + (Plan.PlanMonth * linkGroupUser.PriceForMonth);
                                }
                            }
                            else
                            {
                                return MessageBox.Warning("هشدار", "مدیر والدی برای شما تعریف نشده است لطفا با مدیر سامانه تماس بگیرید !!");
                            }
                        }

                        var Disc1 = new Dictionary<string, object>();
                        Disc1.Add("@Plan_ID_V2", Plan.Plan_ID_V2);
                        Disc1.Add("@transfer_enable", t);
                        Disc1.Add("@exp", exp);


                        var DeviceLimit = "";

                        if (Plan.device_limit == null || Plan.device_limit.Value == 0)
                        {

                        }
                        else
                        {
                            Disc1.Add("@device_limit", Plan.device_limit);
                            DeviceLimit = ",device_limit=@device_limit ";
                        }



                        var Query = "update v2_user set u = 0 , d = 0 , t = 0 ,plan_id=@Plan_ID_V2, transfer_enable =@transfer_enable , expired_at =@exp " + DeviceLimit + " where id =" + user_id;

                        MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                        await mySql.OpenAsync();
                        var reader = await mySql.GetDataAsync(Query, Disc1);
                        reader.Read();
                        reader.Close();

                        var Query2 = "SELECT email FROM `v2_user` WHERE id=" + user_id;

                        var reader2 = await mySql.GetDataAsync(Query2);
                        if (await reader2.ReadAsync())
                        {
                            var link = linkUserAndPlansRepository.table.Where(p => p.L_FK_U_ID == user.User_ID && p.L_FK_P_ID == Plan.Plan_ID && p.L_Status == true).FirstOrDefault();

                            if (user.Role == 2)
                            {
                                user.Wallet += link.tbPlans.Price;
                            }

                            AddLog(Resource.LogActions.U_Edited, link.Link_PU_ID, reader2.GetString("email").Split('@')[0], (int)Plan.Price, Plan.Plan_Name, Plan.PlanVolume.Value, Plan.PlanMonth.Value);
                        }
                        reader2.Close();

                        await mySql.CloseAsync();

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
            catch (Exception ex)
            {
                logger.Error(ex, "تمدید اشتراک با خطا مواجه شد");
                return Toaster.Success("ناموفق", "خطا در تمدید اشتراک");
            }

        }

        #endregion

        #region ریست لینک اکانت
        [System.Web.Http.HttpPost]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> Reset(int user_id)
        {
            var user = usersRepository.table.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

            if (user != null)
            {
                var Server = user.tbServers;

                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                await mySql.OpenAsync();
                string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                var query = "update v2_user set token = '" + token + "',uuid='" + Guid.NewGuid() + "' where id=" + user_id;
                var reader = mySql.GetDataAsync(query);

                logger.Info("لینک اشتراک با موفقیت تغییر یافت");
                await mySql.CloseAsync();
                return Toaster.Success("موفق", "لینک با موفقیت تغییر کرد");
            }
            else
            {
                return Toaster.Success("هشدار", "کاربر یافت نشد");
            }


        }
        #endregion

        #region حذف لینک
        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> delete(int user_id)
        {
            try
            {
                var user = await usersRepository.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);

                var Server = user.tbServers;

                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                await mySql.OpenAsync();


                var Query = "select email,u,d from v2_user where id=" + user_id;
                var reader = await mySql.GetDataAsync(Query);
                await reader.ReadAsync();
                var name = reader.GetString("email").Split('@')[0];
                var username = reader.GetString("email").Split('@')[1];

                var totalUse = Utility.ConvertByteToGB(reader.GetInt64("u") + reader.GetInt64("d"));

                var Expire = reader.GetBodyDefinition("expired_at");
                var ExpireTime = new DateTime();
                if (Expire != "")
                {
                    ExpireTime = Utility.ConvertSecondToDatetime(Convert.ToDouble(Expire));
                }

                var log = await logsRepository.FirstOrDefaultAsync(s => s.FK_NameUser_ID == name && s.tbLinkUserAndPlans.tbUsers.Username == username);
                if (log != null)
                {

                    if (totalUse <= 1 && (ExpireTime != default(DateTime) && ExpireTime <= DateTime.Now))
                    {
                        var userAccount = await usersRepository.FirstOrDefaultAsync(s => s.Username == username);
                        if (userAccount != null)
                        {
                            //چک می کنیم اگر نماینده مبلغ تعرفه رو به کیف پولش برمیگردونیم
                            if (userAccount.Role == 2)
                            {
                                var price = log.SalePrice;

                                userAccount.Wallet -= price;
                            }
                            else
                            // گار نماینده کل بود محاسبات بر اساس قیمت مصوبه صورت میگرد
                            if (userAccount.Role == 3)
                            {
                                if (userAccount.tbUsers2 != null)
                                {
                                    var groupId = log.tbLinkUserAndPlans.tbPlans.Group_Id;
                                    var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == groupId && s.FK_User_Id == userAccount.User_ID);
                                    if (log.PlanVolume != null)
                                    {

                                        userAccount.Wallet -= (log.PlanVolume * (linkGroupUser.PriceForGig)) + (log.PlanMonth * linkGroupUser.PriceForMonth);
                                    }
                                    else
                                    {
                                        userAccount.Wallet -= (log.tbLinkUserAndPlans.tbPlans.PlanVolume * (linkGroupUser.PriceForGig)) + (log.tbLinkUserAndPlans.tbPlans.PlanMonth * linkGroupUser.PriceForMonth);
                                    }
                                }
                                else
                                {
                                    return MessageBox.Warning("هشدار", "مدیر والدی برای شما تعریف نشده است لطفا با مدیر سامانه تماس بگیرید !!");
                                }
                            }
                            //اگر نماینده معمولی بود این قسمت به حساب نماینده کل بر اساس قیمت تصویب شده اضافه میگردد
                            if (userAccount.Role == 2)
                            {
                                if (userAccount.tbUsers2 != null)
                                {
                                    if (userAccount.tbUsers2.Role == 3)
                                    {
                                        var groupId = log.tbLinkUserAndPlans.tbPlans.Group_Id;
                                        var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == groupId && s.FK_User_Id == userAccount.tbUsers2.User_ID);
                                        if (log.PlanVolume != null)
                                        {
                                            userAccount.tbUsers2.Wallet -= (log.PlanVolume * (linkGroupUser.PriceForGig)) + (log.PlanMonth * linkGroupUser.PriceForMonth);
                                        }
                                        else
                                        {
                                            userAccount.tbUsers2.Wallet -= (log.tbLinkUserAndPlans.tbPlans.PlanVolume * (linkGroupUser.PriceForGig)) + (log.tbLinkUserAndPlans.tbPlans.PlanMonth * linkGroupUser.PriceForMonth);
                                        }
                                    }
                                }
                                else
                                {
                                    return Toaster.Error("ناموفق", "عدم صحت نام کاربری لطفا با مدیر تماس بگیرید !!");
                                }
                            }
                        }
                        else
                        {
                            return Toaster.Error("ناموفق", "عدم صحت نام کاربری لطفا با مدیر تماس بگیرید !!");
                        }



                        var logs = await logsRepository.WhereAsync(s => s.FK_NameUser_ID == name && s.tbLinkUserAndPlans.tbUsers.Username == username);
                        await logsRepository.DeleteRangeAsync(logs);


                    }
                    else
                    {
                        var logs = await logsRepository.WhereAsync(s => s.FK_NameUser_ID == name && s.tbLinkUserAndPlans.tbUsers.Username == username);
                        foreach (var item in logs)
                        {
                            item.FK_NameUser_ID = "del_" + name;
                        }
                    }
                }


                reader.Close();
                var Query1 = "delete from v2_user where id=" + user_id;
                var reader1 = await mySql.GetDataAsync(Query1);
                reader1.Close();
                await mySql.CloseAsync();

                await usersRepository.SaveChangesAsync();

                await logsRepository.SaveChangesAsync();

                logger.Info("اشتراک حذف شد");
                return Toaster.Success("موفق", "اشتراک با موفقیت حذف شد");

            }
            catch (Exception ex)
            {
                logger.Error(ex, "حذف اشتراک با خطا مواجه شد");
                return Toaster.Error("ناموفق", "حذف اشتراک با خطا مواجه شد");
            }

        }


        #endregion

        #region ویرایش نام اشتراک
        [AuthorizeApp(Roles = "1,2,3,4")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> EditSubName(int user_id, string SubName, string OldName)
        {
            try
            {
                if (SubName.Length <= 50 && !SubName.Contains('@'))
                {
                    var user = await usersRepository.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);

                    using (MySqlEntities mysql = new MySqlEntities(user.tbServers.ConnectionString))
                    {
                        await mysql.OpenAsync();
                        var Username = OldName.Split('@')[1];
                        var subName = OldName.Split('@')[0];

                        SubName += "@" + Username;

                        var Disc1 = new Dictionary<string, object>();
                        Disc1.Add("@SubName", SubName);

                        var reader = await mysql.GetDataAsync("select email from v2_user where email=@SubName", Disc1);
                        if (await reader.ReadAsync())
                        {
                            reader.Close();
                            return MessageBox.Warning("هشدار", "این نام اشتراک از قبل وجود دارد");
                        }
                        reader.Close();


                        var reader1 = await mysql.GetDataAsync("update v2_user set email=@SubName where id=" + user_id, Disc1);
                        await reader1.ReadAsync();

                        var newName = OldName.Split('@')[0];
                        var log = await logsRepository.FirstOrDefaultAsync(s => s.FK_NameUser_ID == newName && s.tbLinkUserAndPlans.tbUsers.Username == Username);
                        if (log != null)
                        {
                            log.FK_NameUser_ID = SubName.Split('@')[0];

                            await logsRepository.SaveChangesAsync();
                        }

                        reader1.Close();
                        await mysql.CloseAsync();

                        return Toaster.Success("موفق", "نام اشتراک با موفقیت تغییر کرد");
                    }

                }
                else
                {
                    return MessageBox.Warning("هشدار", "نام اشتراک نمی تواند بزرگتر از 50 حرف یا شامل کاراکتر @ باشد");
                }


            }
            catch (Exception ex)
            {
                logger.Error(ex, "تغییر نام اشتراک با خطا مواجه شد");
                return Toaster.Success("موفق", "خطا در تغییر اشتراک لطفا مجدد تلاش کنید");
            }
        }

        #endregion

        #region اطلاعات فعالیت کاربران

        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> GetActivityUsers()
        {
            try
            {
                var user = await usersRepository.FirstOrDefaultAsync(p => p.Username == User.Identity.Name);
                ActivityStatusViewModel activity = new ActivityStatusViewModel();

                if (user != null)
                {

                    var Query = "";
                    if (user.Role == 1)
                    {
                        Query = "SELECT COUNT(*) AS total_users, SUM(CASE WHEN (UNIX_TIMESTAMP(NOW()) - t) < 60 THEN 1 ELSE 0 END) AS online_users, SUM(CASE WHEN banned = 1 THEN 1 ELSE 0 END) AS banned_users, SUM(CASE WHEN (UNIX_TIMESTAMP(NOW()) - t) >= 60 OR (d + u >= transfer_enable) OR expired_at <= UNIX_TIMESTAMP(NOW()) THEN 1 ELSE 0 END) AS inactive_users FROM v2_user WHERE (d + u < transfer_enable) AND (expired_at > UNIX_TIMESTAMP(NOW()))";

                    }
                    else
                    {
                        Query = "SELECT COUNT(*) AS total_users, SUM(CASE WHEN (UNIX_TIMESTAMP(NOW()) - t) < 60 THEN 1 ELSE 0 END) AS online_users, SUM(CASE WHEN banned = 1 THEN 1 ELSE 0 END) AS banned_users, SUM(CASE WHEN (UNIX_TIMESTAMP(NOW()) - t) >= 60 OR (d + u >= transfer_enable) OR expired_at <= UNIX_TIMESTAMP(NOW()) THEN 1 ELSE 0 END) AS inactive_users FROM v2_user WHERE (d + u < transfer_enable) AND (expired_at > UNIX_TIMESTAMP(NOW())) and email like '%@" + user.Username + "%'";
                    }
                    MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                    await mySql.OpenAsync();
                    activity.total_users = 0;
                    activity.online_users = 0;
                    activity.banned_users = 0;
                    activity.inactive_users = 0;
                    using (var reader = await mySql.GetDataAsync(Query))
                    {
                        if (await reader.ReadAsync())
                        {
                            activity.total_users = reader.GetInt32("total_users");
                            activity.online_users = reader.GetInt32("online_users");
                            activity.banned_users = reader.GetInt32("banned_users");
                            activity.inactive_users = reader.GetInt32("inactive_users");
                        }
                    }

                    await mySql.CloseAsync();
                }
                return Json(new { status = "success", data = activity }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "نمایش اطلاعات فعالیت کاربران با خطا مواجه شد");
                return Json(new { status = "error" }, JsonRequestBehavior.AllowGet);
            }

        }

        #endregion

        #region دریافت تاریخچه اشتراک

        [System.Web.Http.HttpGet]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> GetSubUseage(int user_id)
        {
            var user = await usersRepository.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);

            try
            {

                MySqlEntities mysql = new MySqlEntities(user.tbServers.ConnectionString);
                await mysql.OpenAsync();

                var reader = await mysql.GetDataAsync("select * from v2_stat_user where user_id=" + user_id);
                UsagesModel Useage = new UsagesModel();
                Useage.Date = new List<string>();
                Useage.Used = new List<float>();

                var OldDate = "";
                var Counter = -1;
                while (reader.Read())
                {
                    UsagesModel model = new UsagesModel();
                    var d = reader.GetInt64("d");
                    var u = reader.GetInt64("u");

                    var total = d + u;

                    var UnixDate = reader.GetInt64("updated_at");

                    var Datetime = Utility.ConvertSecondToDatetime(UnixDate);

                    var Date = Utility.ConvertDateTimeToMonthAndDay(Datetime);
                    var Used = Utility.ConvertByteToGB(total);
                    if (Date != OldDate)
                    {
                        Useage.Date.Add(Date);
                        var use = (float)Math.Round(Used, 2, MidpointRounding.AwayFromZero);
                        Useage.Used.Add(use);
                        Counter++;
                        OldDate = Date;
                    }
                    else
                    {
                        var use = (float)Math.Round(Used, 2, MidpointRounding.AwayFromZero);
                        Useage.Used[Counter] += use;
                    }

                }
                await mysql.CloseAsync();

                return Json(new { status = "success", data = Useage }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                logger.Error(ex, "دریافت تاریخچه مصرف اشتراک با خطا مواجه شد");
                return MessageBox.Success("خطا", "نمایش نمودار با خطا مواجه شد");
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                logsRepository.Dispose();
                usersRepository.Dispose();
                plansRepository.Dispose();
                linkUserAndPlansRepository.Dispose();
                serverRepository.Dispose();
                linkUserGroupRepository.Dispose();
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}