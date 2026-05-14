using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using V2boardApi.Areas.App.Data.PaymentLinksViewModel;
using V2boardApi.PaymentMethods;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [AuthorizeApp(Roles = "1")]
    [LogActionFilter]
    public class PaymentLinksController : Controller
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Repository<tbPaymentLinks> PaymentLinksRepo { get; set; }
        private Repository<tbSettings> SettingRepo { get; set; }
        private Repository<tbUsers> usersRepository { get; set; }
        public PaymentLinksController()
        {
            PaymentLinksRepo = new Repository<tbPaymentLinks>();
            SettingRepo = new Repository<tbSettings>();
            usersRepository = new Repository<tbUsers>();
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetLinks()
        {

            var lstPays = PaymentLinksRepo.GetAll();
            List<listPaymentLinksViewModel> lstPay = new List<listPaymentLinksViewModel>();
            foreach (var pay in lstPays)
            {
                listPaymentLinksViewModel payvm = new listPaymentLinksViewModel();
                payvm.Id = pay.py_id;
                payvm.Authority = pay.py_authority;
                payvm.Amount = pay.py_amount.ConvertToMony();
                payvm.Description = pay.py_desc;
                payvm.Hash = pay.py_hash;
                if (pay.py_status)
                {
                    payvm.Status = 1;
                }
                else
                {
                    payvm.Status = 0;
                }

                payvm.CreateDate = pay.py_createdate.ConvertDateTimeToShamsi4();
                payvm.PayWebLink = pay.py_link_web;
                payvm.PayTelLink = pay.py_link_bot;
                lstPay.Add(payvm);
            }

            return Json(new { data = lstPay }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateLink(string payAmount, string payDesc)
        {
            try
            {
                var user = usersRepository.Where(p => p.Username == User.Identity.Name).FirstOrDefault();

                var PayAPIKey = user.tbPaymentMethodUser.Where(a => a.tbPaymentMethods.tbpm_Key == "TetraPay").FirstOrDefault();

                var amount = int.Parse(payAmount, System.Globalization.NumberStyles.Currency);


                var BaseUrl = SettingRepo.Where(a => a.tbKey == "TetraPay_Addr").FirstOrDefault();

                TetraPay tetraPay = new TetraPay(BaseUrl.tbValue);

                var CallBackURL = SettingRepo.Where(a => a.tbKey == "CallbackTetraPay_Addr").FirstOrDefault();
                if (CallBackURL == null)
                {
                    logger.Warn("Not Found CallbackTetraPay_Addr Key");
                }

                TetraPay.RequestCreateOrderModel requestCreateOrder = new TetraPay.RequestCreateOrderModel();
                requestCreateOrder.ApiKey = PayAPIKey.tbpu_ApiKey;
                requestCreateOrder.Description = payDesc;
                requestCreateOrder.Amount = amount * 10;
                requestCreateOrder.Email = "sharifir545@gmail.com";
                requestCreateOrder.Mobile = "09155557495";
                requestCreateOrder.Hash_id = user.Username + "#" + Guid.NewGuid().ToString().Split('-')[1] + Guid.NewGuid().ToString().Split('-')[2];
                requestCreateOrder.CallbackURL = CallBackURL.tbValue;
                var res = await tetraPay.CreateOrder(requestCreateOrder);
                if (res.Status == "100")
                {
                    tbPaymentLinks tbPaymentLinks = new tbPaymentLinks();
                    tbPaymentLinks.py_amount = amount;
                    tbPaymentLinks.py_status = false;
                    tbPaymentLinks.py_desc = payDesc;
                    tbPaymentLinks.py_createdate = DateTime.Now;
                    tbPaymentLinks.py_hash = requestCreateOrder.Hash_id;
                    tbPaymentLinks.py_link_bot = res.payment_url_bot;
                    tbPaymentLinks.py_link_web = res.payment_url_web;
                    tbPaymentLinks.py_authority = res.Authority;
                    PaymentLinksRepo.Insert(tbPaymentLinks);
                    await PaymentLinksRepo.SaveChangesAsync();

                    return Toaster.Success("موفق", "لینک با موفقیت ثبت شد");
                }
                else
                {
                    return Toaster.Error("ناموفق", "خطا در ارتباط با سرویس تتراپی");
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex.StackTrace);
                return Toaster.Error("ناموفق", "خطا در ساخت لینک");
            }
        }

        [HttpPost]
        public ActionResult Accept(int pay_id)
        {
            var paylink = PaymentLinksRepo.GetById(pay_id);
            paylink.py_status = true;
            PaymentLinksRepo.Save();
            return Toaster.Success("موفق", "پرداخت با موفقیت تایید شد");
        }

    }
}