using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data.PlanViewModels;

namespace V2boardApi.Areas.App.Controllers
{
    [Authorize]
    public class PlanController : Controller
    {

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

        // GET: App/Plan
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult _PartialGetAllPlans()
        {
            var user = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                return PartialView(user.tbServers.tbPlans.ToList());
            }
            return PartialView();
        }

        public ActionResult UpdatePlans()
        {
            try
            {
                var user = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

                if (user != null)
                {

                    MySqlEntities mySqlEntities = new MySqlEntities(user.tbServers.ConnectionString);
                    mySqlEntities.Open();
                    var reader = mySqlEntities.GetData("select * from v2_plan");

                    while (reader.Read())
                    {
                        var show = reader.GetByte("show");
                        var id = reader.GetInt32("id");

                        var planD = RepositoryPlans.Where(p => p.Plan_ID_V2 == id && user.tbServers.ServerID == p.tbServers.ServerID).FirstOrDefault();
                        if (planD == null)
                        {
                            tbPlans plan = new tbPlans();
                            plan.Plan_ID_V2 = id;
                            plan.PlanVolume = reader.GetInt32("transfer_enable");
                            plan.Plan_Name = reader.GetString("name");
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
                            if(show == 1)
                            {
                                plan.Status = true;
                            }
                            else
                            {
                                plan.Status = false;
                            }
                            if(plan.Price == null)
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
                            planD.FK_Server_ID = user.FK_Server_ID;
                        }
                    }


                    RepositoryServer.Save();
                    RepositoryPlans.Save();
                    return Content("1");
                }
                else
                {
                    return Content("2");
                }
            }
            catch (Exception ex)
            {
                return Content("2");
            }

        }

        public ActionResult _PartialEdit(int id)
        {
            var user = RepositoryUser.Where(p => p.Username == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                return PartialView(user.tbServers.tbPlans.Where(p => p.Plan_ID == id).FirstOrDefault());
            }
            return PartialView();
        }

        [HttpPost]
        public ActionResult _PartialEdit(tbPlans plan)
        {
            var Plan = RepositoryPlans.Where(p => p.Plan_ID == plan.Plan_ID).FirstOrDefault();
            if (Plan != null)
            {
                Plan.PlanVolume = plan.PlanVolume;
                Plan.Plan_Des = plan.Plan_Des;
                Plan.CountDayes = plan.CountDayes;
                Plan.Plan_Name = plan.Plan_Name;
                Plan.Price = plan.Price;
                Plan.Price2 = plan.Price2;

                RepositoryPlans.Save();
                return Content("1");
            }
            return PartialView();
        }

        public ActionResult DisablePlan(int id)
        {
            var Plan = RepositoryPlans.Where(p => p.Plan_ID == id).FirstOrDefault();
            if(Plan != null)
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
    }
}