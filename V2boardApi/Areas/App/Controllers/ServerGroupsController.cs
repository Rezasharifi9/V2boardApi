using DataLayer.DomainModel;
using DataLayer.Repository;
using DeviceDetectorNET.Class.Device;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using V2boardApi.Models.ServerGroups;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    public class ServerGroupsController : Controller
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Repository<tbServerGroups> serverGroup_Repo { get; set; }
        private Repository<tbLinkServerGroupWithUsers> LinkGroupWithUser_Repo { get; set; }
        private Repository<tbUsers> User_Repo { get; set; }
        private Entities db;

        public ServerGroupsController()
        {
            db = new Entities();
            serverGroup_Repo = new Repository<tbServerGroups>(db);
            User_Repo = new Repository<tbUsers>(db);
            LinkGroupWithUser_Repo = new Repository<tbLinkServerGroupWithUsers>(db);
        }
        [AuthorizeApp(Roles = "1")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpGet]
        public async Task<ActionResult> _PartialGetAll()
        {
            try
            {
                var ServerGorups = await serverGroup_Repo.GetAllAsync();
                List<List_Model> List_Groups = new List<List_Model>();

                foreach (var item in ServerGorups)
                {
                    List_Model model = new List_Model();
                    model.GroupName = item.Group_Name;
                    model.Status = Convert.ToInt16(item.Status);
                    model.ID = item.Group_Id;
                    List_Groups.Add(model);
                }

                return Json(new { status = "success", data = List_Groups }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در لود مجوز ها");
                return Json(new { status = "error" });
            }
        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> _SelectGroups()
        {
            var ServerGorups = await serverGroup_Repo.GetAllAsync();
            List<List_Model> List_Groups = new List<List_Model>();

            foreach (var item in ServerGorups)
            {
                List_Model model = new List_Model();
                model.GroupName = item.Group_Name;
                model.ID = item.Group_Id;
                List_Groups.Add(model);
            }

            return Json(new { status = "success", data = List_Groups }, JsonRequestBehavior.AllowGet);
        }

        #region دریافت گروه های مجوز کاربر
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> GetSelectUserGroups()
        {
            try
            {
                var user = await User_Repo.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                List<List_Model> groups = new List<List_Model>();
                foreach (var item in user.tbLinkServerGroupWithUsers)
                {
                    groups.Add(new List_Model { GroupName = item.tbServerGroups.Group_Name, ID = item.FK_Group_Id.Value });
                }

                return Json(new { status = "success", data = groups }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "دریافت سلکت دسته بندی ها با خطا مواجه شد");
                return Json(new { status = "error" });
            }
        }

        #endregion


        [AuthorizeApp(Roles = "1")]
        [System.Web.Http.HttpPost]
        public async Task<ActionResult> CreateOrEdit(string groupName, int? ID = null)
        {
            try
            {

                var user = await User_Repo.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);

                if (ID == null)
                {
                    using (MySqlEntities entities = new MySqlEntities(user.tbServers.ConnectionString))
                    {
                        tbServerGroups tbServerGroup = new tbServerGroups();
                        tbServerGroup.Group_Name = groupName;
                        var Disc1 = new Dictionary<string, object>();
                        Disc1.Add("@groupName", groupName);
                        Disc1.Add("@created_at", Utility.ConvertDatetimeToSecond(DateTime.Now));
                        Disc1.Add("@updated_at", Utility.ConvertDatetimeToSecond(DateTime.Now));

                        var Query = "INSERT INTO `v2_server_group`(`name`, `created_at`, `updated_at`) VALUES (@groupName,@created_at,@updated_at)";
                        await entities.OpenAsync();
                        var Reader = await entities.GetDataAsync(Query, Disc1);
                        Reader.Close();

                        var Query2 = "select * from v2_server_group where name ='" + groupName + "'";
                        var Reader2 = await entities.GetDataAsync(Query2);
                        await Reader2.ReadAsync();
                        tbServerGroup.V2_Group_Id = Reader2.GetInt32("id");
                        Reader2.Close();
                        tbServerGroup.Status = true;

                        serverGroup_Repo.Insert(tbServerGroup);
                        await entities.CloseAsync();
                        await serverGroup_Repo.SaveChangesAsync();
                        logger.Info("کاربر با موفقیت دسته بندی را اضافه کرد");
                        return Toaster.Success("موفق", "دسته بندی اضافه شد");
                    }
                }
                else
                {
                    using (MySqlEntities entities = new MySqlEntities(user.tbServers.ConnectionString))
                    {
                        var group = await serverGroup_Repo.FirstOrDefaultAsync(s => s.Group_Id == ID);
                        var Disc1 = new Dictionary<string, object>();
                        Disc1.Add("@groupName", groupName);

                        var Query = "update `v2_server_group` set name=@groupName where id=" + group.V2_Group_Id;
                        await entities.OpenAsync();
                        var Reader = await entities.GetDataAsync(Query, Disc1);
                        Reader.Close();

                        group.Group_Name = groupName;

                        await entities.CloseAsync();
                        await serverGroup_Repo.SaveChangesAsync();
                        logger.Info("کاربر با موفقیت دسته بندی را ویرایش کرد");
                        return Toaster.Success("موفق", "دسته بندی ویرایش شد");
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex,"ویرایش دسته بندی با خطا مواجه شد");
                return Toaster.Success("موفق", "ویرایش دسته بندی با خطا مواجه شد لطفا مجدد تلاش کنید");
            }
        }


        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> EditGroup(int id)
        {
            try
            {
                var group = await serverGroup_Repo.FirstOrDefaultAsync(s => s.Group_Id == id);

                return Json(new { status = "success", data = new { groupName = group.Group_Name, ID = group.Group_Id } }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex,"نمایش دسته بندی با خطا مواجه شد");
                return Json(new { status = "error" }, JsonRequestBehavior.AllowGet);
            }


        }


        public async Task<ActionResult> UpdateGroups()
        {
            try
            {
                var user = await User_Repo.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);

                using (MySqlEntities entities = new MySqlEntities(user.tbServers.ConnectionString))
                {
                    var Query = "SELECT * FROM `v2_server_group`";
                    await entities.OpenAsync();
                    var reader = await entities.GetDataAsync(Query);

                    while (await reader.ReadAsync())
                    {

                        var name = reader.GetBodyDefinition("name");
                        var id = reader.GetInt32("id");
                        var group = await serverGroup_Repo.FirstOrDefaultAsync(s => s.V2_Group_Id == id);
                        if (group != null)
                        {
                            group.Group_Name = name;
                        }
                        else
                        {
                            tbServerGroups tbServerGroups = new tbServerGroups();
                            tbServerGroups.Group_Name = name;
                            tbServerGroups.V2_Group_Id = id;
                            tbServerGroups.Status = true;
                            serverGroup_Repo.Insert(tbServerGroups);
                        }
                        await serverGroup_Repo.SaveChangesAsync();
                    }
                    reader.Close();

                    await entities.CloseAsync();
                }


                return MessageBox.Success("موفق", "دسته بندی ها با موفقیت بروزرسانی شدند");
            }
            catch(Exception ex)
            {
                logger.Error(ex, "بروزرسانی دسته بندی ها با خطا مواجه شد");
                return MessageBox.Error("ناموفق", "بروزرسانی دسته بندی ها با خطا مواجه شد");
            }
        }


        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpGet]
        public async Task<ActionResult> GetUserGroups(int user_id)
        {
            var user = await User_Repo.FirstOrDefaultAsync(s => s.User_ID == user_id);

            List<UserGroupsViewModel> groups = new List<UserGroupsViewModel>();
            foreach (var item in user.tbLinkServerGroupWithUsers)
            {
                UserGroupsViewModel mgroups = new UserGroupsViewModel();
                mgroups.Id = item.GroupUserLink_ID;
                mgroups.groupId = item.FK_Group_Id.Value;
                mgroups.GroupName = item.tbServerGroups.Group_Name;
                mgroups.PriceForMonth = item.PriceForMonth;
                mgroups.PriceForGig = item.PriceForGig;
                groups.Add(mgroups);
            }

            return Json(new { status = "success", data = groups }, JsonRequestBehavior.AllowGet);

        }


        [AuthorizeApp(Roles = "1")]
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> SetGroupForUser(int user_id, int planGroup, int userPriceForGig, int userPriceForMonth, int id = 0)
        {
            try
            {
                var user = await User_Repo.FirstOrDefaultAsync(s => s.User_ID == user_id);

                if (id != 0)
                {
                    var LinkGroup = await LinkGroupWithUser_Repo.FirstOrDefaultAsync(s => s.GroupUserLink_ID == id);
                    LinkGroup.PriceForMonth = userPriceForMonth;
                    LinkGroup.PriceForGig = userPriceForGig;
                    LinkGroup.FK_Group_Id = planGroup;
                    await User_Repo.SaveChangesAsync();
                    logger.Info("دسته بندی برای کاربر ویرایش شد");
                    return Toaster.Success("موفق", "دسته بندی کاربر ویرایش شد");
                }
                else
                {


                    tbLinkServerGroupWithUsers userGroup = new tbLinkServerGroupWithUsers();
                    userGroup.PriceForMonth = userPriceForMonth;
                    userGroup.PriceForGig = userPriceForGig;
                    userGroup.FK_Group_Id = planGroup;

                    user.tbLinkServerGroupWithUsers.Add(userGroup);
                    await User_Repo.SaveChangesAsync();
                    logger.Info("دسته بندی برای کاربر ثبت شد");
                    return Toaster.Success("موفق", "دسته بندی برای کاربر مورد نظر ثبت شد");

                }
                
            }
            catch (Exception ex)
            {
                logger.Error(ex,"ثبت دسته بندی برای کاربر با خطا مواجه شد");
                return MessageBox.Success("ناموفق", "ثبت دسته بندی با خطا مواجه شد");
            }



        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1")]
        public async Task<ActionResult> DeleteUserGroup(int id, int user_id)
        {
            try
            {
                var user = await User_Repo.FirstOrDefaultAsync(s => s.User_ID == user_id);

                var LinkGroup = await LinkGroupWithUser_Repo.FirstOrDefaultAsync(s => s.GroupUserLink_ID == id);

                foreach(var item in user.tbPlans.Where(s=> s.Group_Id == LinkGroup.FK_Group_Id).ToList())
                {
                    item.Group_Id = null;
                }

                user.tbLinkServerGroupWithUsers.Remove(LinkGroup);

                await User_Repo.SaveChangesAsync();

                logger.Info("دسته بندی با موفقیت حذف شد");
                return Toaster.Success("موفق", "دسته بندی با موفقیت حذف شد");

            }
            catch(Exception ex)
            {
                logger.Error(ex, "حذف دسته بندی با خطا مواجه شد");
                return MessageBox.Error("ناموفق", "حذف دسته بندی با خطا مواجه شد");
            }
        }
    }
}