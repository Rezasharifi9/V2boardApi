using Antlr.Runtime.Misc;
using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Telegram.Bot.Types;
using V2boardApi.Areas.App.Data.UserFactors;
using V2boardApi.Areas.App.Data.UserFactorsViewModels;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [LogActionFilter]
    public class UserFactorsController : Controller
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Repository<tbUserFactors> RepositoryFactors { get; set; }
        private Repository<tbUsers> RepositoryUsers { get; set; }
        private Entities db;
        public UserFactorsController()
        {
            db = new Entities();
            RepositoryFactors = new Repository<tbUserFactors>(db);
            RepositoryUsers = new Repository<tbUsers>(db);
        }
        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> GetInvoices()
        {
            try
            {
                var UserID = JwtToken.GetUser_ID();
                var Factores = await RepositoryFactors.WhereAsync(s => s.tbUsers.Parent_ID.ToString() == UserID && s.tbUf_Status != null);


                List<UserFactor_ListViewModel> List_Factor = new List<UserFactor_ListViewModel>();
                Factores = Factores.OrderByDescending(s => s.tbUf_CreateTime.Value).ToList();
                foreach (var item in Factores)
                {

                    UserFactor_ListViewModel factor = new UserFactor_ListViewModel();
                    factor.ID = item.tbUf_ID;
                    factor.Username = item.tbUsers.Username;
                    factor.Status = (int)item.tbUf_Status;
                    factor.Desc = item.tbUf_Description;
                    factor.CreateTime = item.tbUf_CreateTime.Value.ConvertDateTimeToShamsi4();
                    if (item.tbUf_FileName != null)
                    {
                        factor.IsExistsFile = true;
                    }
                    factor.Price = item.tbUf_Value.Value.ConvertToMony();
                    if ((int)item.tbUsers.Role == 2)
                    {
                        factor.Role = "نماینده";
                    }
                    if ((int)item.tbUsers.Role == 3 || (int)item.tbUsers.Role == 4)
                    {
                        factor.Role = "نماینده ارشد";
                    }

                    List_Factor.Add(factor);
                }


                return Json(new { data = List_Factor }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در لود لیست پرداخت ها");
                return Content("");
            }
        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult _GeneralInfoPayment()
        {
            try
            {
                var UserID = JwtToken.GetUser_ID();

                GeneralInfoPaymentViewModel payInfo = new GeneralInfoPaymentViewModel();
                var Pc = new PersianCalendar();
                var NowMonth = Pc.GetMonth(DateTime.Now);
                var DateFirstYear = Pc.ToDateTime(Pc.GetYear(DateTime.Now), NowMonth, 1, 0, 0, 0, 0);

                var Factores = RepositoryFactors.GetAll().Where(s => s.tbUf_CreateTime >= DateFirstYear && s.tbUsers?.Parent_ID?.ToString() == UserID && s.tbUf_Status != null).ToList();
                var Users = RepositoryUsers.Where(s => s.Parent_ID.ToString() == UserID && s.Status == true).ToList();


                payInfo.AgentUsers = Users.Count();
                payInfo.DebtAgents = (Users.Sum(s => s.Wallet)).ConvertToMony();
                payInfo.Factors = Factores.Count();
                payInfo.Payments = (Factores.Where(s => s.tbUf_Status == 2 || s.tbUf_Status == 3).Sum(s => s.tbUf_Value.Value)).ConvertToMony();

                return PartialView(payInfo);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در لود اطلاعات کلی پرداخت ها");
                return Content("");
            }
        }

        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> CreateOrEdit(int usersSelect, string factorDate, string factorPrice, HttpPostedFileBase factorFile = null, string factorDescription = null, bool factorDebt = false)
        {
            try
            {
                using (var tr = db.Database.BeginTransaction())
                {
                    var UserAgent = await RepositoryUsers.FirstOrDefaultAsync(s => s.User_ID == usersSelect);
                    tbUserFactors userFactor = new tbUserFactors();
                    userFactor.FK_User_ID = usersSelect;
                    userFactor.tbUf_Value = int.Parse(factorPrice.ToString(), NumberStyles.Currency);
                    userFactor.tbUf_Description = factorDescription;
                    userFactor.tbUf_Status = 2;
                    userFactor.tbUf_CreateTime = DateTime.Parse(factorDate, CultureInfo.GetCultureInfo("fa-IR"));

                    if (factorFile != null)
                    {
                        var FileExt = Path.GetExtension(factorFile.FileName);
                        var FileName = Guid.NewGuid() + FileExt;
                        factorFile.SaveAs(Server.MapPath("~/assets/img/UserAgentFactorImages/") + FileName);

                        userFactor.tbUf_FileName = FileName;
                    }
                    RepositoryFactors.Insert(userFactor);
                    await RepositoryFactors.SaveChangesAsync();

                    var PayedFactores = await RepositoryFactors.WhereAsync(s => s.tbUf_Status == 2 && s.FK_User_ID == UserAgent.User_ID);

                    var PaySum = PayedFactores.Sum(s => s.tbUf_Value.Value);

                    if (PaySum >= UserAgent.Wallet)
                    {
                        UserAgent.Wallet -= PaySum;

                        foreach (var item in PayedFactores)
                        {
                            item.tbUf_Status = 3;
                        }
                    }
                    else
                    {
                        if (factorDebt)
                        {
                            UserAgent.Wallet -= userFactor.tbUf_Value.Value;
                            userFactor.tbUf_Status = 3;
                        }
                    }

                    await RepositoryFactors.SaveChangesAsync();
                    await RepositoryUsers.SaveChangesAsync();
                    tr.Commit();
                    logger.Warn("پرداخت جدید اضافه گردید");
                    return Toaster.Success("موفق", "فاکتور با موفقیت اضافه گردید");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ثبت پرداخت جدید");
                return MessageBox.Error("خطا", "خطا در ثبت پرداخت");
            }


        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult download(int factor_id)
        {
            try
            {
                var UserID = JwtToken.GetUser_ID();
                var Factor = RepositoryFactors.Where(s => s.tbUf_ID == factor_id && s.tbUsers.Parent_ID.ToString() == UserID).FirstOrDefault();

                if (Factor.tbUf_FileName != null)
                {
                    // از param1 و param2 برای تولید فایل استفاده کنید
                    string filePath = Server.MapPath("/assets/img/UserAgentFactorImages/" + Factor.tbUf_FileName);


                    byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                    return File(fileBytes, "application/octet-stream", Factor.tbUf_FileName);
                }
                else
                {
                    return RedirectToAction("ErrorNotFoundFile", "Error");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دانلود فایل رسید");
                return RedirectToAction("Error500", "Error");
            }


        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "2,3,4")]
        public ActionResult download2(int factor_id)
        {
            try
            {
                var UserID = JwtToken.GetUser_ID();
                var Factor = RepositoryFactors.Where(s => s.tbUf_ID == factor_id && s.FK_User_ID.ToString() == UserID).FirstOrDefault();

                if (Factor.tbUf_FileName != null)
                {
                    // از param1 و param2 برای تولید فایل استفاده کنید
                    string filePath = Server.MapPath("/assets/img/UserAgentFactorImages/" + Factor.tbUf_FileName);


                    byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                    return File(fileBytes, "application/octet-stream", Factor.tbUf_FileName);
                }
                else
                {
                    return RedirectToAction("ErrorNotFoundFile", "Error");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در دانلود فایل رسید");
                return RedirectToAction("Error500", "Error");
            }


        }

        [System.Web.Mvc.HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult delete(int factor_id)
        {
            try
            {
                var UserID = JwtToken.GetUser_ID();
                var Factor = RepositoryFactors.Where(s => s.tbUf_ID == factor_id && s.tbUsers.Parent_ID.ToString() == UserID).FirstOrDefault();

                if (Factor != null)
                {
                    if (Factor.tbUf_Status == 3)
                    {
                        return MessageBox.Warning("ناموفق", "شما امکان حذف پرداخت در وضعیت کسر از بدهی را ندارید");

                    }
                    else
                    {
                        RepositoryFactors.Delete(Factor.tbUf_ID);
                        RepositoryFactors.Save();
                    }
                }
                logger.Warn("پرداخت حذف گردید");
                return Toaster.Success("موفق", "پرداخت ثبت شده با موفقیت حذف گردید");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در حذف پرداخت");
                return MessageBox.Error("خطا", "خطا در حذف پرداخت");
            }
        }


        [V2boardApi.Tools.AuthorizeApp(Roles = "2,3,4")]
        public ActionResult UserAgent()
        {
            return View();
        }

        //[V2boardApi.Tools.AuthorizeApp(Roles = "2,3,4")]
        public async Task<ActionResult> GetInvoicesUserAgent()
        {
            var UserID = JwtToken.GetUser_ID();
            var Factores = await RepositoryFactors.WhereAsync(s => s.FK_User_ID.ToString() == UserID && s.tbUf_Status != null);


            List<UserFactor_ListViewModel> List_Factor = new List<UserFactor_ListViewModel>();
            Factores = Factores.OrderByDescending(s => s.tbUf_CreateTime.Value).ToList();
            foreach (var item in Factores)
            {

                UserFactor_ListViewModel factor = new UserFactor_ListViewModel();
                factor.ID = item.tbUf_ID;
                factor.Username = item.tbUsers.Username;
                factor.Status = (int)item.tbUf_Status;
                factor.CreateTime = item.tbUf_CreateTime.Value.ConvertDateTimeToShamsi4();
                factor.Desc = item.tbUf_Description;
                if (item.tbUf_FileName != null)
                {
                    factor.IsExistsFile = true;
                }
                factor.Price = item.tbUf_Value.Value.ConvertToMony();
                if ((int)item.tbUsers.Role == 2)
                {
                    factor.Role = "نماینده";
                }
                if ((int)item.tbUsers.Role == 3 || (int)item.tbUsers.Role == 4)
                {
                    factor.Role = "نماینده ارشد";
                }

                List_Factor.Add(factor);
            }


            return Json(new { data = List_Factor }, JsonRequestBehavior.AllowGet);
        }
    }
}