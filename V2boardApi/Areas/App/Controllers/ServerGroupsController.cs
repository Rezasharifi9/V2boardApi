using DataLayer.DomainModel;
using DataLayer.Repository;
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
        private Entities db;

        public ServerGroupsController()
        {
            db = new Entities();
            serverGroup_Repo = new Repository<tbServerGroups>(db);
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

                return Json(new { status = "success", data = List_Groups },JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
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
    }
}