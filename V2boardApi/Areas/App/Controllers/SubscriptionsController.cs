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
        private Repository<tbOrders> ordersRepository { get; set; }
        private Repository<tbLinks> linksRepository { get; set; }
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
            ordersRepository = new Repository<tbOrders>(db);
            linksRepository = new Repository<tbLinks>(db);
            //V2boardApiTools.init();
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
                var filterAgentIdStr = Request.Form.GetValues("filterAgentId")?.FirstOrDefault();
                var filterFromDate = Request.Form.GetValues("filterFromDate")?.FirstOrDefault();
                var filterToDate = Request.Form.GetValues("filterToDate")?.FirstOrDefault();
                var filterSortLowVolume = Request.Form.GetValues("filterSortLowVolume")?.FirstOrDefault() == "1";

                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;

                var user = await usersRepository.table.FirstOrDefaultAsync(p => p.Username == User.Identity.Name);
                if (user?.tbServers == null)
                {
                    return MessageBox.Error("خطا", "خطا در دریافت داده از سمت سرور");
                }

                var detail_online = await V2boardApiTools.GetSubOnlineList();

                var userRole = user.Role.Value;

                tbUsers filterAgent = null;
                if (!string.IsNullOrEmpty(filterAgentIdStr) && int.TryParse(filterAgentIdStr, out var filterAgentId))
                {
                    var candidate = await usersRepository.FirstOrDefaultAsync(u => u.User_ID == filterAgentId);
                    if (CanAccessFilterAgent(user, userRole, candidate))
                        filterAgent = candidate;
                }

                var lockedByAdmin = userRole != 1 &&
                    (SettlementService.IsAgentOrAncestorBlocked(user, db) ||
                     (filterAgent != null && SettlementService.IsAgentOrAncestorBlocked(filterAgent, db)));

                string baseQuery = "SELECT v2.id, v2.email, v2.t, v2.u, v2.d, v2.transfer_enable, v2.banned, v2.token, v2.expired_at, v2.plan_id, v2.created_at, pl.name " +
                                   "FROM `v2_user` AS v2 JOIN v2_plan AS pl ON v2.plan_id = pl.id WHERE 1=1 ";
                string filterQuery = BuildRoleScopeFilter(user, userRole, filterAgent);
                filterQuery += BuildDateFilter(filterFromDate, filterToDate);

                if (!string.IsNullOrEmpty(searchValue))
                {
                    if (searchValue.Contains("token="))
                    {
                        var tokenValue = searchValue.Split('=')[1];
                        filterQuery += $" AND v2.token='{tokenValue}'";
                    }
                    else
                    {
                        filterQuery += $" AND v2.email LIKE '%{searchValue}%'";
                    }
                }

                string query = baseQuery + filterQuery;
                query += BuildSubscriptionOrderBy(sortColumnIndex, sortColumnDir, filterSortLowVolume);
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
                                    IsBanned = Convert.ToBoolean(reader.GetSByte(reader.GetOrdinal("banned"))),
                                    IsAdminBlocked = lockedByAdmin,
                                    Name = reader.GetString(reader.GetOrdinal("email")),
                                    IsActive = 1,
                                    SubLink = $"https://{user.tbServers.SubAddress}/api/v1/client/subscribe?token={reader.GetString(reader.GetOrdinal("token"))}",
                                    

                                };
                                if (user.tbServers.BackupSubAddr != null)
                                {
                                    getuserData.BackupLink = $"https://{user.tbServers.BackupSubAddr}/api/v1/client/subscribe?token={reader.GetString(reader.GetOrdinal("token"))}";
                                }

                                var detail = detail_online.Where(s => s.user_id == getuserData.id).FirstOrDefault();
                                if (detail != null)
                                {
                                    getuserData.OnlineUsers = detail.online_count;
                                    getuserData.LimitUsers = detail.device_limit;
                                    getuserData.Exceeded = detail.exceeded;
                                }
                                else
                                {
                                    getuserData.OnlineUsers = 0;
                                    getuserData.LimitUsers = 0;
                                    getuserData.Exceeded = false;
                                }

                                var PlanId = reader.GetInt64(reader.GetOrdinal("plan_id"));
                                var Plan = await plansRepository.FirstOrDefaultAsync(s => s.Plan_ID_V2 == PlanId);
                                if (Plan == null)
                                {
                                    getuserData.PlanName = reader.GetString(reader.GetOrdinal("name"));
                                }
                                else
                                {
                                    getuserData.PlanName = Plan.Plan_Name;
                                }
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

                                long? createdAtUnix = null;
                                var createdAtRaw = reader["created_at"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(createdAtRaw))
                                    createdAtUnix = Convert.ToInt64(createdAtRaw);

                                var remainingVolume = reader.GetInt64(reader.GetOrdinal("transfer_enable")) - (u + d);
                                var remainingVolumeGB = Utility.ConvertByteToGB(remainingVolume);
                                if (remainingVolumeGB <= 2) getuserData.CanEdit = true;
                                if (remainingVolume <= 0) getuserData.IsActive = 3;
                                getuserData.RemainingVolume = $"{Math.Round(remainingVolumeGB, 2)}";

                                if (Convert.ToBoolean(reader.GetInt16(reader.GetOrdinal("banned"))))
                                {
                                    getuserData.IsActive = 4;
                                }

                                var transferEnable = reader.GetInt64(reader.GetOrdinal("transfer_enable"));
                                getuserData.CanEarlyDeleteRefund = SubscriptionPackageHelper.IsEligibleForEarlyDeleteWithRefund(
                                    createdAtUnix, u, d);
                                getuserData.CanDelete = SubscriptionPackageHelper.CanAgentDeleteSubscription(
                                    userRole, transferEnable, u, d, reader["expired_at"], createdAtUnix);

                                users.Add(getuserData);
                            }
                        }

                        // Ensure the reader is closed before proceeding
                        reader.Close();
                    }

                    var countQuery = "SELECT COUNT(*) AS Count FROM `v2_user` AS v2 WHERE 1=1" + filterQuery;

                    object summary = null;
                    var hasDateRange = !string.IsNullOrWhiteSpace(filterFromDate) && !string.IsNullOrWhiteSpace(filterToDate);
                    var summaryAgent = filterAgent;
                    if (summaryAgent == null && hasDateRange && (userRole == 2 || userRole == 3))
                        summaryAgent = user;
                    if (hasDateRange && summaryAgent != null)
                    {
                        var summaryFilter = BuildRoleScopeFilter(user, userRole, summaryAgent) + BuildDateFilter(filterFromDate, filterToDate);
                        summary = await BuildFilterSummaryAsync(mySqlEntities, summaryFilter, summaryAgent, user.FK_Server_ID.Value);
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
                            data = users,
                            summary
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

        private static bool CanAccessFilterAgent(tbUsers currentUser, int userRole, tbUsers targetAgent)
        {
            if (targetAgent == null || targetAgent.FK_Server_ID != currentUser.FK_Server_ID)
                return false;
            if (userRole == 1)
                return targetAgent.FK_Server_ID == currentUser.FK_Server_ID && targetAgent.Role != 1;
            if (userRole == 2)
                return targetAgent.User_ID == currentUser.User_ID;
            if (userRole == 3)
                return currentUser.tbUsers1.Any(a => a.User_ID == targetAgent.User_ID && a.Status == true);
            return false;
        }

        private static string BuildRoleScopeFilter(tbUsers currentUser, int userRole, tbUsers filterAgent)
        {
            if (filterAgent != null)
                return $" AND v2.email LIKE '%@{filterAgent.Username}'";

            if (userRole == 2 || userRole == 3)
                return $" AND v2.email LIKE '%@{currentUser.Username}'";

            if (userRole == 4)
            {
                var parts = new List<string> { $"v2.email LIKE '%@{currentUser.Username}'" };
                foreach (var item in currentUser.tbUsers1)
                    parts.Add($"v2.email LIKE '%@{item.Username}'");
                return " AND (" + string.Join(" OR ", parts) + ")";
            }

            return "";
        }

        private string BuildDateFilter(string filterFromDate, string filterToDate)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(filterFromDate))
            {
                var fromUnix = (long)ParsePersianDate(filterFromDate).Date.ConvertDatetimeToSecond();
                sb.Append($" AND v2.created_at >= {fromUnix}");
            }
            if (!string.IsNullOrWhiteSpace(filterToDate))
            {
                var toUnix = (long)ParsePersianDate(filterToDate).Date.AddDays(1).AddSeconds(-1).ConvertDatetimeToSecond();
                sb.Append($" AND v2.created_at <= {toUnix}");
            }
            return sb.ToString();
        }

        private static string BuildSubscriptionOrderBy(string sortColumnIndex, string sortColumnDir, bool filterSortLowVolume)
        {
            var dir = string.IsNullOrEmpty(sortColumnDir) ? "ASC" : sortColumnDir;
            if (filterSortLowVolume || sortColumnIndex == "4")
                return " ORDER BY (v2.transfer_enable - (v2.u + v2.d)) ASC";

            switch (sortColumnIndex)
            {
                case "1":
                    return $" ORDER BY v2.email {dir}";
                case "2":
                    return $" ORDER BY v2.transfer_enable {dir}";
                case "3":
                    return $" ORDER BY v2.t {dir}";
                case "5":
                case "8":
                    return $" ORDER BY v2.expired_at {dir}";
                default:
                    return " ORDER BY v2.id DESC";
            }
        }

        private async Task<object> BuildFilterSummaryAsync(MySqlEntities mySqlEntities, string filterClause, tbUsers filterAgent, int serverId)
        {
            var summaryQuery = "SELECT v2.plan_id FROM `v2_user` AS v2 WHERE 1=1" + filterClause;
            var planIds = new List<long>();
            using (var summaryReader = await mySqlEntities.GetDataAsync(summaryQuery))
            {
                while (await summaryReader.ReadAsync())
                    planIds.Add(summaryReader.GetInt64(summaryReader.GetOrdinal("plan_id")));
            }

            long totalAmount = 0;
            foreach (var planId in planIds)
                totalAmount += await ComputeSubscriptionAmountAsync(filterAgent, planId, serverId);

            var agentRole = filterAgent.Role ?? 2;
            return new
            {
                totalCount = planIds.Count,
                totalAmount,
                totalAmountFormatted = totalAmount.ToString("N0"),
                amountLabel = GetSubscriptionAmountLabel(agentRole),
                agentUsername = filterAgent.Username
            };
        }

        private async Task<long> ComputeSubscriptionAmountAsync(tbUsers agent, long planIdV2, int serverId)
        {
            var plan = await plansRepository.FirstOrDefaultAsync(p => p.Plan_ID_V2 == planIdV2 && p.FK_Server_ID == serverId);
            if (plan == null)
                return 0;

            if (agent.Role == 2)
                return plan.Price;

            var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == plan.Group_Id && s.FK_User_Id == agent.User_ID);
            if (linkGroupUser == null)
                return plan.Price;

            return (long)((plan.PlanVolume * linkGroupUser.PriceForGig)
                + (plan.PlanMonth * linkGroupUser.PriceForMonth)
                + ((plan.device_limit ?? 0) * linkGroupUser.PriceForUser));
        }

        private static string GetSubscriptionAmountLabel(int agentRole)
        {
            return agentRole == 2 ? "جمع قیمت فروش تعرفه" : "جمع هزینه (قیمت هر گیگ)";
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
                    if (userSubname.Contains('@'))
                    {
                        return MessageBox.Warning("هشدار", "نام اشتراک نمی تواند حاوی کاراکتر @ باشد");
                    }
                    userSubname = userSubname.ToLower();
                    if (userPlan != 0)
                    {

                        var userAdmin = usersRepository.table.Where(p => p.Role == 1 && p.Status == true && p.Username != User.Identity.Name && p.IsNotActiveSell).Any();
                        if (userAdmin)
                        {
                            return MessageBox.Warning("هشدار", "متاسفانه فروش موقتا غیرفعال شده است");
                        }

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

                                    var walletBefore = user.Wallet;
                                    double? parentWalletBefore = user.tbUsers2 != null ? user.tbUsers2.Wallet : (double?)null;

                                    string exp = "";
                                    if (plan.PlanMonth == 0)
                                    {
                                        exp = null;
                                    }
                                    else
                                    {
                                        exp = DateTime.Now.AddDays(plan.PlanMonth * 30).ConvertDatetimeToSecond().ToString();
                                    }



                                    if (user.Role == 3)
                                    {
                                        if (user.tbUsers2 != null)
                                        {
                                            var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == plan.Group_Id && s.FK_User_Id == user.User_ID);
                                            user.Wallet += (int)((plan.PlanVolume * linkGroupUser.PriceForGig) + (plan.PlanMonth * linkGroupUser.PriceForMonth) + (plan.device_limit * linkGroupUser.PriceForUser));
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

                                                user.tbUsers2.Wallet += (int)((plan.PlanVolume * linkGroupUser.PriceForGig) + (plan.PlanMonth * linkGroupUser.PriceForMonth) + (plan.device_limit * linkGroupUser.PriceForUser));
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
                                    var vol = Utility.ConvertGBToByte(plan.PlanVolume);
                                    Disc3.Add("@tran", vol);
                                    Disc3.Add("@grid", group.V2_Group_Id);
                                    Disc3.Add("@planid", planid);
                                    Disc3.Add("@token", token);
                                    var DeviceLimit = "";
                                    var DeviceLimitCol = "";

                                    var resolvedDeviceLimit = SubscriptionPackageHelper.ResolveDeviceLimitForV2(plan);
                                    if (resolvedDeviceLimit.HasValue)
                                    {
                                        Disc3.Add("@device_limit", resolvedDeviceLimit.Value);
                                        DeviceLimit = ",@device_limit";
                                        DeviceLimitCol = ",device_limit";
                                    }

                                    if (plan.Speed_limit != null)
                                    {
                                        Disc3.Add("@Speed_limit", plan.Speed_limit);
                                    }
                                    else
                                    {
                                        Disc3.Add("@Speed_limit", null);
                                    }

                                    string Query = "insert into v2_user (email,expired_at,created_at,uuid,t,u,d,transfer_enable,banned,group_id,plan_id,token,password,updated_at" + DeviceLimitCol + ",speed_limit) VALUES (@FullName,@expired,@create,@guid,0,0,0,@tran,0,@grid,@planid,@token,'" + Guid.NewGuid() + "',@create " + DeviceLimit + ",@Speed_limit)";

                                    var reader = await mySql.GetDataAsync(Query, Disc3);
                                    reader.Close();
                                    var link = linkUserAndPlansRepository.table.Where(p => p.L_FK_U_ID == user.User_ID && p.L_FK_P_ID == plan.Plan_ID && p.L_Status == true).FirstOrDefault();
                                    if (user.Role == 2)
                                    {
                                        user.Wallet += link.tbPlans.Price;
                                    }



                                    await mySql.CloseAsync();

                                    var newLink = new tbLinks();
                                    newLink.tbL_Email = emilprx;
                                    newLink.tbL_Token = token;
                                    newLink.FK_Server_ID = user.FK_Server_ID;
                                    newLink.FK_User_ID = user.User_ID;
                                    newLink.tb_AutoRenew = false;
                                    SubscriptionReserveWarnHelper.ResetReserveWarnState(newLink);
                                    linksRepository.Insert(newLink);
                                    linksRepository.Save();

                                    linkUserAndPlansRepository.Save();
                                    usersRepository.Save();
                                    NotifyAgentLimitAfterWalletChange(user, walletBefore, parentWalletBefore);
                                    AddLog(Resource.LogActions.U_Created, link.Link_PU_ID, userSubname, (int)plan.Price, plan.Plan_Name, plan.PlanVolume, plan.PlanMonth, token);
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

        private bool AddLog(string Action, int LinkUserID, string V2User, int price, string planName, double planVolume, double planMonth, string subToken = null)
        {
            try
            {
                if (!SubscriptionLogHelper.IsAllowedSubscriptionLogAction(Action))
                    return false;

                tbLogs tbLogs = new tbLogs();
                tbLogs.FK_Link_User_Plan_ID = LinkUserID;
                tbLogs.Action = Action;
                tbLogs.FK_NameUser_ID = V2User;
                tbLogs.CreateDatetime = DateTime.Now;
                tbLogs.SalePrice = price;
                tbLogs.PlanName = planName;
                tbLogs.PlanVolume = planVolume;
                tbLogs.PlanMonth = planMonth;
                tbLogs.SubToken = subToken;
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

        private static void NotifyAgentLimitAfterWalletChange(tbUsers user, double walletBefore, double? parentWalletBefore = null)
        {
            AgentLimitNotificationService.ScheduleCheckAfterWalletChange(user.User_ID, walletBefore);
            if (parentWalletBefore.HasValue && user.tbUsers2 != null)
                AgentLimitNotificationService.ScheduleCheckAfterWalletChange(user.tbUsers2.User_ID, parentWalletBefore.Value);
        }

        #endregion

        #region ویرایش اشتراک

        [AuthorizeApp(Roles = "1")]
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
                    var Traffic = Utility.ConvertByteToGB(read.GetInt64("transfer_enable"));
                    var Subname = read["email"]?.ToString().Split('@')[0];
                    var Date = read["expired_at"];
                    var SpeedLimit = read["speed_limit"];
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


        [AuthorizeApp(Roles = "1")]
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

                    DateTime? miladi = null;
                    if (!string.IsNullOrWhiteSpace(userExpire))
                    {
                        DateTime parsed;
                        if (!Utility.TryParseShamsiDate(userExpire, out parsed))
                        {
                            return MessageBox.Warning("هشدار", "لطفا تاریخ را صحیح وارد کنید");
                        }
                        miladi = parsed.AddHours(12);
                    }

                    string MiliSecoundTime = null;
                    if (miladi.HasValue)
                    {
                        MiliSecoundTime = Utility.ConvertDatetimeToSecond(miladi.Value).ToString();
                    }

                    string name = "";
                    string username = "";
                    var transfe_enable = Utility.ConvertGBToByte(userTraffic);
                    var read = await mysql.GetDataAsync("select v2_user.email FROM `v2_user` where id=" + user_id);
                    if (await read.ReadAsync())
                    {
                        userSubname += "@" + read["email"]?.ToString().Split('@')[1];
                        name = read["email"]?.ToString().Split('@')[0];
                        username = read["email"]?.ToString().Split('@')[1];
                        read.Close();
                    }

                    if ((name + "@" + username) != userSubname)
                    {
                        var log = await logsRepository.FirstOrDefaultAsync(s => s.FK_NameUser_ID == name && s.tbLinkUserAndPlans.tbUsers.Username == username);
                        if (log != null)
                        {
                            log.FK_NameUser_ID = userSubname.Split('@')[0];
                        }
                    }

                    var Disc1 = new Dictionary<string, object>();
                    Disc1.Add("@userSubname", userSubname);
                    Disc1.Add("@transfe_enable", transfe_enable);

                    var updateParts = new List<string>
                    {
                        "email=@userSubname",
                        "transfer_enable=@transfe_enable"
                    };

                    if (!string.IsNullOrWhiteSpace(userSpeed))
                    {
                        updateParts.Add("speed_limit=@Speed");
                        Disc1.Add("@Speed", userSpeed);
                    }

                    if (MiliSecoundTime != null)
                    {
                        updateParts.Add("expired_at=@MiliSecoundTime");
                        Disc1.Add("@MiliSecoundTime", MiliSecoundTime);
                    }

                    var Query = "update v2_user set " + string.Join(", ", updateParts) + " where id=" + user_id;

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
                if (user == null || user.tbServers == null)
                    return Toaster.Error("ناموفق", "خطایی در مسدود سازی اشتراک رخ داد");

                if (!status && user.Role != 1 && SettlementService.IsAgentOrAncestorBlocked(user, db))
                    return Toaster.Warning("ناموفق", "این اشتراک توسط مدیریت مسدود شده و امکان رفع مسدودی توسط نماینده وجود ندارد");

                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                await mySql.OpenAsync();

                if (!status && user.Role != 1)
                {
                    var remarksQuery = "SELECT remarks FROM v2_user WHERE id = @id AND email LIKE @pattern LIMIT 1";
                    var remarksParams = new Dictionary<string, object>
                    {
                        { "@id", user_id },
                        { "@pattern", "%" + user.Username + "%" }
                    };
                    var adminBlocked = false;
                    using (var remarksReader = await mySql.GetDataAsync(remarksQuery, remarksParams))
                    {
                        if (await remarksReader.ReadAsync())
                        {
                            var remarks = remarksReader["remarks"] == DBNull.Value ? "" : remarksReader["remarks"].ToString();
                            adminBlocked = SettlementService.HasAdminBlockFlag(remarks);
                        }
                    }
                    if (adminBlocked)
                    {
                        await mySql.CloseAsync();
                        return Toaster.Warning("ناموفق", "این اشتراک توسط مدیریت مسدود شده و امکان رفع مسدودی توسط نماینده وجود ندارد");
                    }
                }

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

        #region تمدید اکانت (رزرو بسته)
        [System.Web.Http.HttpPost]
        [AuthorizeApp(Roles = "1,2,3,4")]
        [ValidateAntiForgeryToken]
        public Task<ActionResult> Renew(int user_id, int userPlan, bool confirmDirectActivation = false)
        {
            return ReservePackage(user_id, userPlan, confirmDirectActivation);
        }

        #endregion

        #region رزرو بسته

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> GetSubscriptionPackages(int user_id)
        {
            try
            {
                var user = usersRepository.table.FirstOrDefault(p => p.Username == User.Identity.Name);
                if (user?.tbServers == null)
                    return Json(new { status = "error", message = "خطا در دریافت اطلاعات سرور" }, JsonRequestBehavior.AllowGet);

                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                await mySql.OpenAsync();

                var disc = new Dictionary<string, object> { { "@id", user_id } };
                var reader = await mySql.GetDataAsync(
                    "SELECT v2.email, v2.u, v2.d, v2.transfer_enable, v2.expired_at, pl.name AS plan_name " +
                    "FROM v2_user v2 JOIN v2_plan pl ON v2.plan_id = pl.id WHERE v2.id = @id", disc);

                if (!await reader.ReadAsync())
                {
                    reader.Close();
                    await mySql.CloseAsync();
                    return Json(new { status = "error", message = "اشتراک یافت نشد" }, JsonRequestBehavior.AllowGet);
                }

                var email = reader.GetString("email");
                var subName = email.Split('@')[0];
                var download = reader.GetInt64("d");
                var upload = reader.GetInt64("u");
                var transferEnable = reader.GetInt64("transfer_enable");
                var remainingGb = Math.Round(Utility.ConvertByteToGB(transferEnable - (download + upload)), 2);
                var totalGb = Math.Round(Utility.ConvertByteToGB(transferEnable), 2);
                var planName = reader.GetString("plan_name");
                var expiredAt = reader["expired_at"];
                reader.Close();
                await mySql.CloseAsync();

                string expireText = "بدون محدودیت";
                if (expiredAt != null && !string.IsNullOrWhiteSpace(expiredAt.ToString()))
                {
                    var expDate = Utility.ConvertSecondToDatetime(Convert.ToInt64(expiredAt));
                    expireText = expDate.ConvertDateTimeToShamsi4();
                }

                var isEnded = SubscriptionPackageHelper.IsPackageEnded(transferEnable, download, upload, expiredAt);
                var currentStatus = isEnded ? "در انتظار تعویض" : "فعال";

                var reserved = ordersRepository
                    .Where(o => o.AccountName == email && o.OrderStatus == "FOR_RESERVE")
                    .OrderBy(o => o.OrderDate)
                    .ToList()
                    .Select(o =>
                    {
                        var pName = o.tbLinkUserAndPlans?.tbPlans?.Plan_Name;
                        if (string.IsNullOrEmpty(pName) && o.V2_Plan_ID.HasValue)
                        {
                            var p = plansRepository.Where(pl => pl.Plan_ID_V2 == o.V2_Plan_ID).FirstOrDefault();
                            pName = p?.Plan_Name ?? "بسته رزرو";
                        }
                        return new
                        {
                            orderId = o.Order_ID,
                            planName = pName ?? "بسته رزرو",
                            volumeGb = o.Traffic,
                            months = o.Month,
                            reservedDate = o.OrderDate?.ConvertDateTimeToShamsi4() ?? "-",
                            status = "در انتظار فعال سازی"
                        };
                    }).ToList();

                return Json(new
                {
                    status = "success",
                    data = new
                    {
                        subscriptionName = subName,
                        current = new
                        {
                            planName,
                            totalVolumeGb = totalGb,
                            remainingVolumeGb = remainingGb,
                            expireDate = expireText,
                            status = currentStatus
                        },
                        reserved
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دریافت بسته‌های اشتراک");
                return Json(new { status = "error", message = "خطا در دریافت اطلاعات بسته‌ها" }, JsonRequestBehavior.AllowGet);
            }
        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> PreviewRenewPackage(int user_id, int userPlan)
        {
            try
            {
                var user = usersRepository.table.FirstOrDefault(p => p.Username == User.Identity.Name);
                if (user?.tbServers == null)
                    return Json(new { status = "error", message = "کاربر یافت نشد" }, JsonRequestBehavior.AllowGet);

                var plan = plansRepository.table
                    .FirstOrDefault(p => p.Plan_ID == userPlan && p.FK_Server_ID == user.tbServers.ServerID && p.Status == true);
                if (plan == null)
                    return Json(new { status = "error", message = "تعرفه انتخابی معتبر نیست" }, JsonRequestBehavior.AllowGet);

                using (var mySql = new MySqlEntities(user.tbServers.ConnectionString))
                {
                    await mySql.OpenAsync();
                    var disc = new Dictionary<string, object> { { "@id", user_id } };
                    using (var reader = await mySql.GetDataAsync(
                        "SELECT u, d, transfer_enable, expired_at FROM v2_user WHERE id=@id", disc))
                    {
                        if (!await reader.ReadAsync())
                            return Json(new { status = "error", message = "اشتراک یافت نشد" }, JsonRequestBehavior.AllowGet);

                        var packageEnded = SubscriptionPackageHelper.IsPackageEnded(
                            reader.GetInt64("transfer_enable"),
                            reader.GetInt64("d"),
                            reader.GetInt64("u"),
                            reader["expired_at"]);
                        reader.Close();

                        return Json(new
                        {
                            status = "success",
                            data = new
                            {
                                willActivateImmediately = packageEnded,
                                planName = plan.Plan_Name,
                                planPrice = plan.Price.ConvertToMony()
                            }
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در پیش‌نمایش تمدید اشتراک");
                return Json(new { status = "error", message = "خطا در بررسی وضعیت اشتراک" }, JsonRequestBehavior.AllowGet);
            }
        }

        [System.Web.Http.HttpPost]
        [AuthorizeApp(Roles = "1,2,3,4")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReservePackage(int user_id, int userPlan, bool confirmDirectActivation = false)
        {
            try
            {
                var user = usersRepository.table.FirstOrDefault(p => p.Username == User.Identity.Name);
                if (user == null)
                    return Toaster.Error("ناموفق", "کاربر یافت نشد");

                if ((user.Limit - user.Wallet) < 0)
                {
                    return Toaster.Success("موفق", "مبلغ تعرفه انتخابی بیشتر از موجودی حساب شما می باشد لطفا بدهی خود را پرداخت کنید");
                }

                var plan = plansRepository.table
                    .FirstOrDefault(p => p.Plan_ID == userPlan && p.FK_Server_ID == user.tbServers.ServerID && p.Status == true);

                if (plan == null)
                    return MessageBox.Warning("هشدار", "تعرفه انتخابی معتبر نیست");

                if ((plan.Price + user.Wallet) > user.Limit)
                    return Toaster.Success("موفق", "مبلغ تعرفه انتخابی بیشتر از موجودی حساب شما می باشد لطفا بدهی خود را پرداخت کنید");

                var linkPlan = linkUserAndPlansRepository.table
                    .FirstOrDefault(p => p.L_FK_U_ID == user.User_ID && p.L_FK_P_ID == plan.Plan_ID && p.L_Status == true);

                if (linkPlan == null)
                    return MessageBox.Warning("هشدار", "تعرفه برای حساب شما فعال نیست");

                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                await mySql.OpenAsync();

                var disc = new Dictionary<string, object> { { "@id", user_id } };
                var reader = await mySql.GetDataAsync(
                    "SELECT email,u,d,transfer_enable,expired_at,token FROM v2_user WHERE id=@id", disc);

                if (!await reader.ReadAsync())
                {
                    reader.Close();
                    await mySql.CloseAsync();
                    return MessageBox.Warning("هشدار", "اشتراک یافت نشد");
                }

                var email = reader.GetString("email");
                var download = reader.GetInt64("d");
                var upload = reader.GetInt64("u");
                var transferEnable = reader.GetInt64("transfer_enable");
                var expiredAt = reader["expired_at"];
                var subToken = reader["token"]?.ToString();
                var subName = email.Split('@')[0];
                reader.Close();

                var packageEnded = SubscriptionPackageHelper.IsPackageEnded(transferEnable, download, upload, expiredAt);
                var link = linksRepository.Where(l => l.tbL_Email == email).FirstOrDefault();

                if (packageEnded && !confirmDirectActivation)
                {
                    await mySql.CloseAsync();
                    return MessageBox.Warning("تأیید لازم است",
                        "بسته فعالی روی این اشتراک وجود ندارد و بسته انتخابی بلافاصله فعال می‌شود. هزینه بسته قابل برگشت نیست. لطفاً پس از تأیید مجدداً اقدام کنید.");
                }

                var walletBefore = user.Wallet;
                double? parentWalletBefore = user.tbUsers2 != null ? user.tbUsers2.Wallet : (double?)null;

                if (user.Role == 3)
                {
                    if (user.tbUsers2 == null)
                        return MessageBox.Warning("هشدار", "مدیر والدی برای شما تعریف نشده است لطفا با مدیر سامانه تماس بگیرید !!");

                    var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == plan.Group_Id && s.FK_User_Id == user.User_ID);
                    if (linkGroupUser == null)
                    {
                        await mySql.CloseAsync();
                        return MessageBox.Warning("هشدار", "تنظیمات قیمت‌گذاری گروه برای کاربر یافت نشد");
                    }
                    var deviceLimitForPrice = plan.device_limit ?? 0;
                    user.Wallet += (int)((plan.PlanVolume * linkGroupUser.PriceForGig) + (plan.PlanMonth * linkGroupUser.PriceForMonth) + (deviceLimitForPrice * linkGroupUser.PriceForUser));
                }

                if (user.Role == 2)
                {
                    if (user.tbUsers2 == null)
                        return MessageBox.Warning("هشدار", "مدیر والدی برای شما تعریف نشده است لطفا با مدیر سامانه تماس بگیرید !!");

                    if (user.tbUsers2.Wallet >= user.tbUsers2.Limit)
                        return MessageBox.Warning("هشدار", "فروش موقتا توسط ادمین متوقف شده است لطفا با پشتیبانی ارتباط بگیرید !!");

                    if (user.tbUsers2.Role != 1 && user.tbUsers2.Role == 3)
                    {
                        var linkGroupUser = await linkUserGroupRepository.FirstOrDefaultAsync(s => s.FK_Group_Id == plan.Group_Id && s.FK_User_Id == user.tbUsers2.User_ID);
                        if (linkGroupUser == null)
                        {
                            await mySql.CloseAsync();
                            return MessageBox.Warning("هشدار", "تنظیمات قیمت‌گذاری گروه مدیر والد یافت نشد");
                        }
                        var deviceLimitForPrice = plan.device_limit ?? 0;
                        user.tbUsers2.Wallet += (plan.PlanVolume * linkGroupUser.PriceForGig) + (plan.PlanMonth * linkGroupUser.PriceForMonth) + (deviceLimitForPrice * linkGroupUser.PriceForUser);
                    }
                }

                if (packageEnded)
                {
                    var t = Utility.ConvertGBToByte(Convert.ToInt64(plan.PlanVolume));
                    object expValue = null;
                    if (plan.PlanMonth != 0)
                        expValue = DateTime.Now.AddDays(plan.PlanMonth * 30).ConvertDatetimeToSecond().ToString();

                    var discUp = new Dictionary<string, object>
                    {
                        { "@Plan_ID_V2", plan.Plan_ID_V2 },
                        { "@transfer_enable", t },
                        { "@exp", expValue }
                    };

                    var group = await groupsRepository.FirstOrDefaultAsync(s => s.Group_Id == plan.Group_Id);
                    discUp.Add("@group", group.V2_Group_Id);

                    var deviceLimit = "";
                    var resolvedUpdateDeviceLimit = SubscriptionPackageHelper.ResolveDeviceLimitForV2(plan);
                    if (resolvedUpdateDeviceLimit.HasValue)
                    {
                        discUp.Add("@device_limit", resolvedUpdateDeviceLimit.Value);
                        deviceLimit = ",device_limit=@device_limit ";
                    }

                    var query = "update v2_user set u=0,d=0, plan_id=@Plan_ID_V2,group_id=@group, transfer_enable = @transfer_enable , expired_at = @exp " + deviceLimit + " where id =" + user_id;
                    var updateReader = await mySql.GetDataAsync(query, discUp);
                    updateReader.Read();
                    updateReader.Close();
                    await mySql.CloseAsync();

                    if (user.Role == 2)
                        user.Wallet += linkPlan.tbPlans.Price;

                    AddLog(Resource.LogActions.U_Edited, linkPlan.Link_PU_ID, subName, (int)plan.Price, plan.Plan_Name, plan.PlanVolume, plan.PlanMonth, subToken);
                    usersRepository.Save();
                    linkUserAndPlansRepository.Save();
                    NotifyAgentLimitAfterWalletChange(user, walletBefore, parentWalletBefore);

                    logger.Info("بسته اشتراک بلافاصله فعال شد (بسته قبلی تمام شده بود)");
                    return Toaster.Success("موفق", "بسته فعلی تمام شده بود و بسته جدید بلافاصله فعال شد");
                }

                var order = new tbOrders
                {
                    Order_Guid = Guid.NewGuid(),
                    AccountName = email,
                    OrderDate = DateTime.Now,
                    OrderType = "تمدید",
                    OrderStatus = "FOR_RESERVE",
                    Traffic = plan.PlanVolume,
                    Month = plan.PlanMonth,
                    V2_Plan_ID = plan.Plan_ID_V2,
                    FK_Link_Plan_ID = linkPlan.Link_PU_ID,
                    FK_Tel_UserID = link?.FK_TelegramUserID,
                    Order_Price = plan.Price,
                    PriceWithOutDiscount = plan.Price
                };

                ordersRepository.Insert(order);

                if (user.Role == 2)
                    user.Wallet += linkPlan.tbPlans.Price;

                ordersRepository.Save();
                usersRepository.Save();
                linkUserAndPlansRepository.Save();
                NotifyAgentLimitAfterWalletChange(user, walletBefore, parentWalletBefore);
                await mySql.CloseAsync();

                AddLog(ReservedPackageHelper.ReserveLogAction, linkPlan.Link_PU_ID, subName, (int)plan.Price, plan.Plan_Name, plan.PlanVolume, plan.PlanMonth, subToken);

                logger.Info("بسته برای اشتراک رزرو شد");
                return Toaster.Success("موفق", "بسته با موفقیت رزرو شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "رزرو بسته اشتراک با خطا مواجه شد");
                return Toaster.Error("ناموفق", "خطا در رزرو بسته");
            }
        }

        [System.Web.Http.HttpPost]
        [AuthorizeApp(Roles = "1,2,3,4")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActivateReservedPackage(int orderId)
        {
            try
            {
                var agent = usersRepository.table.FirstOrDefault(p => p.Username == User.Identity.Name);
                if (agent?.tbServers == null)
                    return Toaster.Error("ناموفق", "کاربر یافت نشد");

                var order = await GetValidatedReservedOrderAsync(orderId, agent);
                if (order == null)
                    return MessageBox.Warning("هشدار", "اشتراک رزرو یافت نشد");

                using (var mySql = new MySqlEntities(agent.tbServers.ConnectionString))
                {
                    await mySql.OpenAsync();
                    var applied = await SubscriptionPackageHelper.ApplyReservedOrderAsync(
                        order, mySql, ordersRepository, linksRepository);

                    if (!applied)
                        return Toaster.Error("ناموفق", "خطا در فعال‌سازی اشتراک");

                    logger.Info("بسته رزرو به‌صورت دستی فعال شد");
                    return Toaster.Success("موفق", "اشتراک با موفقیت فعال شد");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "فعال‌سازی بسته رزرو با خطا مواجه شد");
                return Toaster.Error("ناموفق", "خطا در فعال‌سازی اشتراک");
            }
        }

        [System.Web.Http.HttpPost]
        [AuthorizeApp(Roles = "1,2,3,4")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelReservedPackage(int orderId)
        {
            try
            {
                var agent = usersRepository.table.FirstOrDefault(p => p.Username == User.Identity.Name);
                if (agent == null)
                    return Toaster.Error("ناموفق", "کاربر یافت نشد");

                var order = await GetValidatedReservedOrderAsync(orderId, agent);
                if (order == null)
                    return MessageBox.Warning("هشدار", "اشتراک رزرو یافت نشد");

                int? refundAmount = 0;
                double? telRefund = null;

                // ادمین سیستم: فقط حذف رزرو، بدون برگشت کیف
                if ((agent.Role ?? 0) != 1)
                {
                    refundAmount = await ReservedPackageHelper.RefundReservedOrderWalletAsync(
                        order, usersRepository, plansRepository, linkUserAndPlansRepository, linkUserGroupRepository);
                    if (!refundAmount.HasValue)
                        return Toaster.Error("ناموفق", "خطا در بازگشت مبلغ به حساب شما");

                    telRefund = ReservedPackageHelper.RefundTelegramWalletForReservedOrder(order);
                }

                ReservedPackageHelper.RemoveReservePackageLogs(logsRepository, order);

                ordersRepository.Delete(order);
                ordersRepository.Save();

                logger.Info("بسته رزرو لغو شد");
                if ((refundAmount ?? 0) > 0 || telRefund.HasValue)
                    return Toaster.Success("موفق", "رزرو لغو شد و مبلغ بازگشت داده شد");
                return Toaster.Success("موفق", "رزرو با موفقیت لغو شد");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "لغو بسته رزرو با خطا مواجه شد");
                return Toaster.Error("ناموفق", "خطا در حذف اشتراک");
            }
        }

        private async Task<tbOrders> GetValidatedReservedOrderAsync(int orderId, tbUsers agent)
        {
            var order = await ordersRepository.table
                .Include(o => o.tbLinkUserAndPlans)
                .Include(o => o.tbLinkUserAndPlans.tbPlans)
                .Include(o => o.tbTelegramUsers)
                .Include(o => o.tbDepositWallet_Log)
                .FirstOrDefaultAsync(o => o.Order_ID == orderId && o.OrderStatus == "FOR_RESERVE");

            if (order == null || string.IsNullOrEmpty(order.AccountName) || !order.AccountName.Contains("@"))
                return null;

            if (agent.Role == 1)
                return order;

            var username = order.AccountName.Split('@')[1];
            return agent.Username == username ? order : null;
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

                var reader1 = await mySql.GetDataAsync("select token from v2_user where id=" + user_id);
                var OldToken = "";
                if (await reader1.ReadAsync())
                {
                    OldToken = reader1["token"]?.ToString();
                }
                reader1.Close();

                string token = Guid.NewGuid().ToString().Split('-')[0] + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                var query = "update v2_user set token = '" + token + "',uuid='" + Guid.NewGuid() + "' where id=" + user_id;
                var reader = await mySql.GetDataAsync(query);
                reader.Close();
                var Logs = await logsRepository.WhereAsync(s => s.SubToken == OldToken);
                foreach (var item in Logs)
                {
                    item.SubToken = token;
                }
                await logsRepository.SaveChangesAsync();
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

                //if (user.Role != 1)
                //{
                //    return Toaster.Warning("درخواست ناموفق", "این گزینه ویژگی موقتا غیرفعال شده است");
                //}

                var Server = user.tbServers;

                MySqlEntities mySql = new MySqlEntities(user.tbServers.ConnectionString);
                await mySql.OpenAsync();


                var Query = "select email,u,d,transfer_enable,expired_at,created_at from v2_user where id=" + user_id;
                var reader = await mySql.GetDataAsync(Query);
                await reader.ReadAsync();
                var name = reader.GetString("email").Split('@')[0];
                var username = reader.GetString("email").Split('@')[1];

                var download = reader.GetInt64("d");
                var upload = reader.GetInt64("u");
                var transferEnable = reader.GetInt64("transfer_enable");
                var expiredAtRaw = reader["expired_at"];
                long? createdAtUnix = null;
                var createdAtRaw = reader["created_at"]?.ToString();
                if (!string.IsNullOrWhiteSpace(createdAtRaw))
                    createdAtUnix = Convert.ToInt64(createdAtRaw);

                if (!SubscriptionPackageHelper.CanAgentDeleteSubscription(
                    user.Role ?? 0, transferEnable, download, upload, expiredAtRaw, createdAtUnix))
                {
                    reader.Close();
                    await mySql.CloseAsync();
                    return MessageBox.Warning("هشدار",
                        "تا پایان بسته فعال امکان حذف وجود ندارد؛ فقط اشتراک‌های تازه‌ساخته (کمتر از ۱ روز و کمتر از ۱ گیگ مصرف) قابل حذف با بازگشت هزینه هستند.");
                }

                reader.Close();

                var email = name + "@" + username;
                await TelegramSubscriptionHelper.CancelReservedOrdersForEmailAsync(
                    email, ordersRepository, logsRepository, usersRepository,
                    plansRepository, linkUserAndPlansRepository, linkUserGroupRepository);

                int? refundAmount = null;
                try
                {
                    refundAmount = await ReservedPackageHelper.ProcessSubscriptionDeleteLogsAsync(
                        name, username, createdAtUnix, download, upload,
                        logsRepository, usersRepository, linkUserGroupRepository);
                }
                catch (InvalidOperationException ex)
                {
                    await mySql.CloseAsync();
                    return Toaster.Error("ناموفق", ex.Message);
                }

                var Query1 = "delete from v2_user where id=" + user_id;
                var reader1 = await mySql.GetDataAsync(Query1);
                reader1.Close();
                await mySql.CloseAsync();

                await usersRepository.SaveChangesAsync();

                await logsRepository.SaveChangesAsync();

                logger.Info("اشتراک حذف شد");
                if (refundAmount.HasValue && refundAmount.Value > 0)
                    return Toaster.Success("موفق", "اشتراک حذف شد و مبلغ " + refundAmount.Value.ConvertToMony() + " تومان به حساب شما برگشت.");

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

        #region دریافت تاریخچه مصرف اشتراک

        [System.Web.Http.HttpGet]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> GetSubUseage(int user_id, string fromDate = null, string toDate = null)
        {
            try
            {
                var result = await BuildUsageHistoryAsync(user_id, fromDate, toDate);
                return Json(new { status = "success", data = result.Items, summary = result.Summary }, JsonRequestBehavior.AllowGet);
            }
            catch (ArgumentException ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "دریافت تاریخچه مصرف اشتراک با خطا مواجه شد");
                return Json(new { status = "error", message = "نمایش تاریخچه مصرف با خطا مواجه شد" }, JsonRequestBehavior.AllowGet);
            }
        }

        [System.Web.Http.HttpGet]
        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> ExportSubUsagePdf(int user_id, string fromDate = null, string toDate = null, string subName = null)
        {
            try
            {
                var result = await BuildUsageHistoryAsync(user_id, fromDate, toDate);
                result.SubName = subName;

                try
                {
                    var pdfBytes = UsageHistoryPdfHelper.Export(result);
                    var safeName = string.IsNullOrWhiteSpace(subName) ? "subscription" : subName.Replace("@", "_");
                    var fileName = "usage-history-" + safeName + "-" + DateTime.Now.ToString("yyyyMMddHHmm") + ".pdf";
                    return File(pdfBytes, "application/pdf", fileName);
                }
                catch (Exception pdfEx)
                {
                    logger.Warn(pdfEx, "خروجی Stimulsoft PDF ناموفق بود، نسخه چاپی HTML باز می‌شود");
                    return View("SubUsageHistoryExport", result);
                }
            }
            catch (ArgumentException ex)
            {
                return Content(ex.Message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خروجی PDF تاریخچه مصرف با خطا مواجه شد");
                return Content("خطا در تهیه خروجی PDF");
            }
        }

        private async Task<SubUsageHistoryResultViewModel> BuildUsageHistoryAsync(int user_id, string fromDate, string toDate)
        {
            var user = await usersRepository.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
            if (user == null)
                throw new InvalidOperationException("کاربر یافت نشد");

            var end = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
            var start = DateTime.Now.Date.AddDays(-30);

            if (!string.IsNullOrWhiteSpace(fromDate))
                start = ParsePersianDate(fromDate).Date;

            if (!string.IsNullOrWhiteSpace(toDate))
                end = ParsePersianDate(toDate).Date.AddDays(1).AddSeconds(-1);

            if (start > end)
                throw new ArgumentException("بازه تاریخ نامعتبر است");

            var startUnix = (long)Utility.ConvertDatetimeToSecond(start);
            var endUnix = (long)Utility.ConvertDatetimeToSecond(end);

            var parameters = new Dictionary<string, object>
            {
                { "@userId", user_id },
                { "@startUnix", startUnix },
                { "@endUnix", endUnix }
            };

            var query = "SELECT d, u, updated_at FROM v2_stat_user WHERE user_id=@userId AND updated_at >= @startUnix AND updated_at <= @endUnix ORDER BY updated_at DESC";

            using (MySqlEntities mysql = new MySqlEntities(user.tbServers.ConnectionString))
            {
                await mysql.OpenAsync();

                var reader = await mysql.GetDataAsync(query, parameters);
                var dayGroups = new Dictionary<DateTime, Tuple<long, long>>();

                while (await reader.ReadAsync())
                {
                    var d = reader.GetInt64("d");
                    var u = reader.GetInt64("u");
                    var unixDate = reader.GetInt64("updated_at");
                    var day = Utility.ConvertSecondToDatetime(unixDate).Date;

                    if (!dayGroups.ContainsKey(day))
                        dayGroups[day] = Tuple.Create(0L, 0L);

                    var current = dayGroups[day];
                    dayGroups[day] = Tuple.Create(current.Item1 + d, current.Item2 + u);
                }

                reader.Close();
                await mysql.CloseAsync();

                long totalDownloadBytes = 0;
                long totalUploadBytes = 0;

                foreach (var group in dayGroups.Values)
                {
                    totalDownloadBytes += group.Item1;
                    totalUploadBytes += group.Item2;
                }

                var totalDownloadGb = Math.Round(Utility.ConvertByteToGB(totalDownloadBytes), 2, MidpointRounding.AwayFromZero);
                var totalUploadGb = Math.Round(Utility.ConvertByteToGB(totalUploadBytes), 2, MidpointRounding.AwayFromZero);
                var totalGb = Math.Round(totalDownloadGb + totalUploadGb, 2, MidpointRounding.AwayFromZero);

                var items = dayGroups
                    .OrderByDescending(x => x.Key)
                    .Select(g =>
                    {
                        var downloadGb = Math.Round(Utility.ConvertByteToGB(g.Value.Item1), 2, MidpointRounding.AwayFromZero);
                        var uploadGb = Math.Round(Utility.ConvertByteToGB(g.Value.Item2), 2, MidpointRounding.AwayFromZero);
                        var rowTotalGb = Math.Round(downloadGb + uploadGb, 2, MidpointRounding.AwayFromZero);

                        return new SubUsageHistoryItemViewModel
                        {
                            Date = g.Key.ConvertDateTimeToShamsi5(),
                            DateSort = (long)Utility.ConvertDatetimeToSecond(g.Key),
                            Download = downloadGb.ConvertToMony() + " GB",
                            Upload = uploadGb.ConvertToMony() + " GB",
                            Total = rowTotalGb.ConvertToMony() + " GB"
                        };
                    })
                    .ToList();

                return new SubUsageHistoryResultViewModel
                {
                    Items = items,
                    Summary = new SubUsageHistorySummaryViewModel
                    {
                        FromDate = start.ConvertDateTimeToShamsi5(),
                        ToDate = end.Date.ConvertDateTimeToShamsi5(),
                        TotalDownload = totalDownloadGb.ConvertToMony() + " GB",
                        TotalUpload = totalUploadGb.ConvertToMony() + " GB",
                        Total = totalGb.ConvertToMony() + " GB"
                    }
                };
            }
        }

        private static DateTime ParsePersianDate(string date)
        {
            try
            {
                return DateTime.Parse(date.Trim(), CultureInfo.GetCultureInfo("fa-IR"));
            }
            catch
            {
                var parts = date.Split('/');
                if (parts.Length == 3)
                {
                    var pc = new System.Globalization.PersianCalendar();
                    return pc.ToDateTime(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), 0, 0, 0, 0);
                }

                throw;
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