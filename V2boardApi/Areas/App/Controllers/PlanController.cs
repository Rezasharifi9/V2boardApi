using DataLayer.DomainModel;
using DataLayer.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Telegram.Bot.Types;
using V2boardApi.Areas.App.Data.PlanViewModels;
using V2boardApi.Areas.App.Data.RequestModels;
using V2boardApi.Tools;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace V2boardApi.Areas.App.Controllers
{
    [AuthorizeApp(Roles = "1")]
    [LogActionFilter]
    public class PlanController : Controller
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public PlanController()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryPlans = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryServer = new Repository<tbServers>(db);
        }


        #region لیست تعرفه ها

        [AuthorizeApp(Roles = "1")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1")]
        public ActionResult _PartialGetAllPlans()
        {
            var result = RepositoryPlans.GetAll().OrderByDescending(s => s.Plan_ID).ToList();

            List<PlanResponseViewModel> Plans = new List<PlanResponseViewModel>();

            foreach (var item in result)
            {
                PlanResponseViewModel Plan = new PlanResponseViewModel();
                Plan.id = item.Plan_ID;
                Plan.PlanName = item.Plan_Name;
                if (item.CountDayes != null)
                {
                    Plan.DayesCount = item.CountDayes.Value;
                }
                else
                {
                    Plan.DayesCount = 0;
                }
                if (item.Speed_limit == null)
                {
                    Plan.SpeedLimit = "بدون محدودیت";
                }
                else
                {
                    Plan.SpeedLimit = item.Speed_limit.Value.ToString();
                }
                Plan.Group_Name = item.Group_Name;
                Plan.Traffic = item.PlanVolume.Value;
                Plan.Price = item.Price.Value.ConvertToMony();
                Plan.Status = Convert.ToInt32(item.Status.Value);
                Plans.Add(Plan);
            }


            return Json(new { data = Plans }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region افزودن و ویرایش تعرفه

        #region ثبت افزودن و ویرایش

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> CreateOrEdit(RequestPlanViewModel model)
        {

            try
            {
                if (ModelState.IsValid)
                {
                    var user = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                    if (user != null)
                    {
                        int OrgPrice = 0;
                        try
                        {
                            OrgPrice = int.Parse(model.planPrice, System.Globalization.NumberStyles.Currency);
                        }
                        catch
                        {
                            return MessageBox.Warning("هشدار", "لطفا مبلغ را صحیح وارد کنید", icon: icon.warning);
                        }
                        var time = "";
                        if (model.planTime == null)
                        {
                            time = "NULL";
                        }
                        else
                        {
                            var Date = DateTime.Now.AddDays(model.planTime.Value);

                            time = Utility.ConvertDatetimeToSecond(Date).ToString();
                        }
                        var Speed = "NULL";
                        if (model.planSpeed != null)
                        {
                            Speed = model.planSpeed.Value.ToString();
                        }
                        var TimeNow = Utility.ConvertDatetimeToSecond(DateTime.Now);

                        var Query = "";
                        tbPlans plan = new tbPlans();
                        if (model.id != null)
                        {
                            plan = await RepositoryPlans.FirstOrDefaultAsync(s => s.Plan_ID == model.id);
                            Query = "update v2_plan set group_id='" + model.planGroup + "',transfer_enable='" + model.planTraffic + "',name='" + model.planName + "',speed_limit=" + Speed + " where id=" + plan.Plan_ID_V2;
                        }
                        else
                        {
                            Query = "INSERT INTO `v2_plan`(`group_id`, `transfer_enable`, `name`, `speed_limit`, `show`,`created_at`, `updated_at`) VALUES ('" + model.planGroup + "','" + model.planTraffic + "','" + model.planName + "'," + Speed + ",'1','" + TimeNow + "','" + TimeNow + "')";
                        }

                        using (MySqlEntities mysql = new MySqlEntities(user.tbServers.ConnectionString))
                        {
                            await mysql.OpenAsync();
                            var Reader = await mysql.GetDataAsync(Query);
                            Reader.Close();

                            var GetPlanIdQuery2 = "select * from v2_server_group where id='" + model.planGroup + "'";
                            var NewReader2 = await mysql.GetDataAsync(GetPlanIdQuery2);
                            await NewReader2.ReadAsync();
                            plan.Group_Id = NewReader2.GetInt32("id");
                            plan.Group_Name = NewReader2.GetString("name");
                            NewReader2.Close();

                            if (model.id == null)
                            {
                                var GetPlanIdQuery = "select id from v2_plan where name='" + model.planName + "'";
                                var NewReader = await mysql.GetDataAsync(GetPlanIdQuery);
                                await NewReader.ReadAsync();
                                plan.Plan_ID_V2 = NewReader.GetInt32("id");
                                NewReader.Close();
                            }
                            plan.Plan_Name = model.planName;

                            plan.PlanVolume = model.planTraffic;
                            if (model.planTime == null)
                            {
                                plan.CountDayes = 0;
                            }
                            else
                            {
                                plan.CountDayes = model.planTime;
                            }

                            plan.Price = OrgPrice;
                            plan.FK_Server_ID = user.FK_Server_ID;
                            plan.Status = true;
                            plan.Speed_limit = model.planSpeed;
                            plan.Group_Id = model.planGroup;
                            if (model.id == null)
                            {
                                RepositoryPlans.Insert(plan);
                            }

                            await RepositoryPlans.SaveChangesAsync();
                            await mysql.CloseAsync();

                        }
                    }
                    if (model.id != null)
                    {
                        return Toaster.Success("موفق", "تعرفه با موفقیت ویرایش گردید");
                    }
                    else
                    {
                        return Toaster.Success("موفق", "تعرفه با موفقیت ایجاد گردید");
                    }
                }
                else
                {
                    var errors = ModelState.GetError();
                    return MessageBox.Warning("هشدار", errors, icon: icon.warning);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ذخیره سازی تعرفه با خطا مواجه شد");
                return MessageBox.Error("ناموفق", "ذخیره سازی تعرفه با خطا مواجه شد");
            }

        }

        #endregion

        #region نمایش اطلاعات برای ویرایش

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var plan = await RepositoryPlans.FirstOrDefaultAsync(s => s.Plan_ID == id);
                RequestPlanViewModel requestPlan = new RequestPlanViewModel();
                requestPlan.id = id;
                requestPlan.planName = plan.Plan_Name;
                requestPlan.planTraffic = plan.PlanVolume.Value;
                requestPlan.planGroup = plan.Group_Id.Value;
                requestPlan.planPrice = plan.Price.Value.ConvertToMony();
                requestPlan.planTime = plan.CountDayes;
                requestPlan.planSpeed = plan.Speed_limit;
                var data = requestPlan.ToDictionary();
                return Json(new { status = "success", data = data }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                return Json(new { status = "error", });
            }
        }

        #endregion

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> ChangeStatus(int id)
        {
            try
            {
                var Plan = await RepositoryPlans.FirstOrDefaultAsync(p => p.Plan_ID == id);
                if (Plan.Status.Value)
                {
                    Plan.Status = false;
                }
                else
                {
                    Plan.Status = true;
                }
                await RepositoryPlans.SaveChangesAsync();
                
                logger.Info("وضعیت تعرفه با نام " + Plan.Plan_Name + " تغییر کرد");
                return Toaster.Success("موفق", "وضعیت تعرفه با موفقیت تغییر کرد");

            }
            catch(Exception ex)
            {
                logger.Info(ex, "تغییر وضعیت تعرفه با خطا مواجه شد");
                return MessageBox.Error("ناموفق", "خطا در پردازش درخواست رخ داد لطفا با پشتیبانی ارتباط بگیرید");
            }
            


        }

        #endregion

        #region دریافت گروه مجوز 

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> GetSelectGroups()
        {
            try
            {
                var user = await RepositoryUser.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                List<PermissionsViewModel> permissions = new List<PermissionsViewModel>();

                if (user != null)
                {
                    using (MySqlEntities mysql = new MySqlEntities(user.tbServers.ConnectionString))
                    {
                        await mysql.OpenAsync();
                        var Reader = await mysql.GetDataAsync("SELECT id,name FROM `v2_server_group`");

                        while (await Reader.ReadAsync())
                        {
                            PermissionsViewModel permissionsView = new PermissionsViewModel();
                            permissionsView.id = Reader.GetInt32("id");
                            permissionsView.Name = Reader.GetString("name");
                            permissions.Add(permissionsView);
                        }
                        Reader.Close();
                        await mysql.CloseAsync();
                    }

                }
                return Json(new { status = "success", data = permissions }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { status = "error" }, JsonRequestBehavior.AllowGet);
            }

        }

        #endregion

        #region بروزرسانی تعرفه ها

        [HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> UpdatePlans()
        {
            try
            {
                var user = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();



                MySqlEntities mySqlEntities = new MySqlEntities(user.tbServers.ConnectionString);
                await mySqlEntities.OpenAsync();
                var reader = await mySqlEntities.GetDataAsync("SELECT *,v2_server_group.id as groupId,v2_server_group.name as groupName FROM `v2_plan` join v2_server_group on v2_plan.group_id = v2_server_group.id");

                while (await reader.ReadAsync())
                {
                    var show = reader.GetByte("show");
                    var id = reader.GetInt32("id");

                    var planD = RepositoryPlans.Where(p => p.Plan_ID_V2 == id).FirstOrDefault();
                    if (planD == null)
                    {
                        tbPlans plan = new tbPlans();
                        plan.Plan_ID_V2 = id;
                        plan.PlanVolume = reader.GetInt32("transfer_enable");
                        plan.Plan_Name = reader.GetString("name");
                        plan.Group_Id = reader.GetInt32("groupId");
                        plan.Group_Name = reader.GetString("groupName");
                        var speedLimit = reader.GetBodyDefinition("speed_limit");
                        if (speedLimit != "")
                        {
                            plan.Speed_limit = Convert.ToInt16(speedLimit);

                        }
                        var Month_Price = reader.GetBodyDefinition("month_price");
                        var quarter_price = reader.GetBodyDefinition("quarter_price");
                        var half_year_price = reader.GetBodyDefinition("half_year_price");
                        var year_price = reader.GetBodyDefinition("year_price");
                        if (Month_Price != "")
                        {
                            plan.Price = Convert.ToInt32(Month_Price) / 100;
                            plan.CountDayes = 30;

                        }
                        else if (quarter_price != "")
                        {
                            plan.Price = Convert.ToInt32(quarter_price) / 100;
                            plan.CountDayes = 90;
                        }
                        else if (half_year_price != "")
                        {
                            plan.Price = Convert.ToInt32(half_year_price) / 100;
                            plan.CountDayes = 180;
                        }
                        else if (year_price != "")
                        {
                            plan.Price = Convert.ToInt32(year_price) / 100;
                            plan.CountDayes = 360;
                        }
                        if (show == 1)
                        {
                            plan.Status = true;
                        }
                        else
                        {
                            plan.Status = false;
                        }
                        if (plan.Price == null)
                        {
                            plan.Price = 0;
                        }
                        plan.FK_Server_ID = user.FK_Server_ID;
                        RepositoryPlans.Insert(plan);
                    }
                    else
                    {
                        planD.Plan_ID_V2 = id;
                        planD.PlanVolume = reader.GetInt32("transfer_enable");
                        planD.Plan_Name = reader.GetString("name");
                        var Month_Price = reader.GetBodyDefinition("month_price");
                        var quarter_price = reader.GetBodyDefinition("quarter_price");
                        var half_year_price = reader.GetBodyDefinition("half_year_price");
                        var year_price = reader.GetBodyDefinition("year_price");
                        if (Month_Price != "")
                        {
                            planD.Price = Convert.ToInt32(Month_Price) / 100;
                            planD.CountDayes = 30;

                        }
                        else if (quarter_price != "")
                        {
                            planD.Price = Convert.ToInt32(quarter_price) / 100;
                            planD.CountDayes = 90;
                        }
                        else if (half_year_price != "")
                        {
                            planD.Price = Convert.ToInt32(half_year_price) / 100;
                            planD.CountDayes = 180;
                        }
                        else if (year_price != "")
                        {
                            planD.Price = Convert.ToInt32(year_price) / 100;
                            planD.CountDayes = 360;
                        }
                        if (show == 1)
                        {
                            planD.Status = true;
                        }
                        else
                        {
                            planD.Status = false;
                        }
                        if (planD.Price == null)
                        {
                            planD.Price = 0;
                        }
                        planD.Group_Id = reader.GetInt32("groupId");
                        planD.Group_Name = reader.GetString("groupName");
                        var speedLimit = reader.GetBodyDefinition("speed_limit");
                        if (speedLimit != "")
                        {
                            planD.Speed_limit = Convert.ToInt16(speedLimit);
                        }

                        planD.FK_Server_ID = user.FK_Server_ID;
                    }
                }

                await mySqlEntities.CloseAsync();
                RepositoryServer.Save();
                RepositoryPlans.Save();
                logger.Info("بروزرسانی تعرفه ها با موفقیت انجام شد");
                return Toaster.Success("موفق", "تعرفه ها با موفقیت بروزرسانی شدند");

            }
            catch (Exception ex)
            {
                logger.Error(ex, "بروزرسانی تعرفه ها با خطا مواجه شد");
                return Toaster.Error("ناموفق", "بروزرسانی تعرفه ها با خطا مواجه شده اند");
            }

        }

        #endregion

        public ActionResult DisablePlan(int id)
        {
            var Plan = RepositoryPlans.Where(p => p.Plan_ID == id).FirstOrDefault();
            if (Plan != null)
            {
                if (Plan.Status.Value)
                {
                    Plan.Status = false;
                }
                else
                {
                    Plan.Status = true;
                }
                RepositoryPlans.Save();
                return Content("1");
            }
            return PartialView("2");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                RepositoryUser.Dispose();
                RepositoryPlans.Dispose();
                RepositoryLogs.Dispose();
                RepositoryServer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}