using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using DataLayer.DomainModel;
using NLog;
using V2boardApi.Areas.api.Data.ApiModels;
using V2boardApi.Areas.api.Data.ViewModels;

namespace V2boardApi.Areas.api.Controllers
{
    /// <summary>
    /// ثبت دستگاه های اپلیکیشن موبایل و تخصیص آن ها به نماینده.
    ///
    /// عمدا LogActionFilter ندارد چون بدنه درخواست شامل توکن FCM و شناسه دستگاه است
    /// و نباید در جدول لاگ ذخیره شود.
    /// </summary>
    [EnableCors(origins: "*", "*", "*")]
    public class MobileDeviceController : ApiController
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Entities db;

        public MobileDeviceController()
        {
            db = new Entities();
        }

        /// <summary>
        /// ثبت یا به روزرسانی یک دستگاه. کلاینت این را در اولین اجرای برنامه صدا می زند
        /// و در صورت خطا تا موفق شدن دوباره تلاش می کند ، بنابراین این متد idempotent است :
        /// فراخوانی دوباره با همان deviceId رکورد قبلی را به روز می کند نه رکورد جدید.
        /// </summary>
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> Register(RegisterMobileDeviceModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("اطلاعات دستگاه ارسال نشده است");
                }

                var DeviceId = model.ResolveDeviceId();
                if (DeviceId.Length == 0)
                {
                    return BadRequest("شناسه دستگاه (DeviceId) ارسال نشده است");
                }
                if (DeviceId.Length > 64)
                {
                    DeviceId = DeviceId.Substring(0, 64);
                }

                var Token = GetAgentToken(model);
                if (Token == null)
                {
                    return BadRequest("توکن نماینده در هدر Authorization ارسال نشده است");
                }

                var Agent = await db.tbUsers.FirstOrDefaultAsync(p => p.Token == Token);
                if (Agent == null)
                {
                    return Content(HttpStatusCode.NotFound, "کاربری با این توکن یافت نشد");
                }

                var Device = await db.tbMobileUsers.FirstOrDefaultAsync(p => p.tbMu_AndroidId == DeviceId);
                var IsExisting = Device != null;


                if (!IsExisting)
                {
                    Device = new tbMobileUsers();
                    Device.tbMu_AndroidId = DeviceId;
                    Device.tbMu_RegisterDate = DateTime.Now;
                    Device.tbMu_IsActive = true;
                    db.tbMobileUsers.Add(Device);
                }
                else if (Device.FK_User_ID != null && Device.FK_User_ID != Agent.User_ID)
                {
                    // نصب دوباره با بیلد یک نماینده دیگر — توکن بیلد ملاک است
                    logger.Info("دستگاه " + DeviceId + " از نماینده " + Device.FK_User_ID + " به نماینده " + Agent.User_ID + " منتقل شد");
                }

                Device.FK_User_ID = Agent.User_ID;
                Device.tbMu_LastSeenDate = DateTime.Now;
                Device.tbMu_LastIp = Cut(GetClientIp(), 64);

                // توکن قبلی فقط با یک توکن واقعی جایگزین می شود. اگر کلاینت این بار
                // توکنی نداشت (مثلا Firebase هنوز آماده نشده) توکن ذخیره شده پاک نمی شود ،
                // وگرنه یک ثبت مجدد ساده دستگاه را از دسترس Push خارج می کرد.
                var NewFirebaseToken = Cut(model.FirebaseToken, 500);
                if (NewFirebaseToken != null)
                {
                    Device.tbMu_FirebaseToken = NewFirebaseToken;
                }

                Device.tbMu_NotificationEnabled = model.NotificationEnabled ?? false;
                Device.tbMu_Rooted = model.Rooted ?? false;

                Device.tbMu_Manufacturer = Cut(model.Manufacturer, 100);
                Device.tbMu_Model = Cut(model.Model, 100);
                Device.tbMu_Device = Cut(model.Device, 100);
                Device.tbMu_Product = Cut(model.Product, 100);
                Device.tbMu_Hardware = Cut(model.Hardware, 100);

                Device.tbMu_AndroidVersion = Cut(model.ResolveOsVersion(), 20);
                Device.tbMu_Sdk = model.Sdk;
                Device.tbMu_AppVersion = Cut(model.AppVersion, 30);
                Device.tbMu_VersionCode = model.VersionCode;
                Device.tbMu_PackageName = Cut(model.PackageName, 150);

                Device.tbMu_Language = Cut(model.Language, 10);
                Device.tbMu_Country = Cut(model.Country, 10);
                Device.tbMu_Timezone = Cut(model.Timezone, 60);

                Device.tbMu_ScreenWidth = model.ScreenWidth;
                Device.tbMu_ScreenHeight = model.ScreenHeight;
                Device.tbMu_Density = model.Density;

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException) when (!IsExisting)
                {
                    // ایندکس یکتای tbMu_AndroidId خورده است : یک درخواست همزمان زودتر
                    // همین دستگاه را ثبت کرده. رکورد آن یکی ملاک است و این درخواست هم
                    // موفق حساب می شود ، وگرنه کلاینت بی دلیل دوباره تلاش می کرد.
                    db.Entry(Device).State = EntityState.Detached;

                    var Existing = await db.tbMobileUsers.FirstOrDefaultAsync(p => p.tbMu_AndroidId == DeviceId);
                    if (Existing == null)
                    {
                        throw;
                    }

                    logger.Info("ثبت همزمان دستگاه " + DeviceId + " تشخیص داده شد ، رکورد موجود استفاده شد");
                    Device = Existing;
                    IsExisting = true;
                }

                MobileDeviceViewModel data = new MobileDeviceViewModel();
                data.DeviceId = Device.tbMu_ID;
                data.AgentUsername = Agent.Username;
                data.BusinessName = Agent.BussinesTitle;
                data.IsExisting = IsExisting;
                data.Message = IsExisting ? "اطلاعات دستگاه به روز شد" : "دستگاه با موفقیت ثبت شد";

                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ثبت دستگاه موبایل");
                return Content(HttpStatusCode.InternalServerError, "خطا در ثبت دستگاه");
            }
        }

        /// <summary>
        /// به روزرسانی توکن FCM بدون فرستادن دوباره کل مشخصات دستگاه.
        /// توکن FCM بعد از پاک کردن داده برنامه یا بازیابی بکاپ عوض می شود.
        /// </summary>
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> UpdateToken(RegisterMobileDeviceModel model)
        {
            try
            {
                if (model == null || model.ResolveDeviceId().Length == 0)
                {
                    return BadRequest("شناسه دستگاه (DeviceId) ارسال نشده است");
                }

                var Token = GetAgentToken(model);
                if (Token == null)
                {
                    return BadRequest("توکن نماینده در هدر Authorization ارسال نشده است");
                }

                var Agent = await db.tbUsers.FirstOrDefaultAsync(p => p.Token == Token);
                if (Agent == null)
                {
                    return Content(HttpStatusCode.NotFound, "کاربری با این توکن یافت نشد");
                }

                var NewFirebaseToken = Cut(model.FirebaseToken, 500);
                if (NewFirebaseToken == null)
                {
                    return BadRequest("توکن نوتیفیکیشن ارسال نشده است");
                }

                var DeviceId = model.ResolveDeviceId();
                var Device = await db.tbMobileUsers.FirstOrDefaultAsync(p => p.tbMu_AndroidId == DeviceId);

                // دستگاه باید متعلق به همان نماینده ای باشد که توکنش ارسال شده ، وگرنه
                // هرکس با دانستن deviceId یک دستگاه می توانست توکن Push آن را عوض کند.
                // پاسخ در هر دو حالت یکسان است تا نشود وجود یک deviceId را حدس زد.
                if (Device == null || Device.FK_User_ID != Agent.User_ID)
                {
                    return Content(HttpStatusCode.NotFound, "دستگاهی با این شناسه ثبت نشده است");
                }

                Device.tbMu_FirebaseToken = NewFirebaseToken;
                Device.tbMu_NotificationEnabled = model.NotificationEnabled ?? Device.tbMu_NotificationEnabled;
                Device.tbMu_LastSeenDate = DateTime.Now;
                Device.tbMu_LastIp = Cut(GetClientIp(), 64);

                await db.SaveChangesAsync();

                MobileDeviceViewModel data = new MobileDeviceViewModel();
                data.DeviceId = Device.tbMu_ID;
                data.AgentUsername = Device.tbUsers == null ? null : Device.tbUsers.Username;
                data.BusinessName = Device.tbUsers == null ? null : Device.tbUsers.BussinesTitle;
                data.IsExisting = true;
                data.Message = "توکن نوتیفیکیشن به روز شد";

                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در به روزرسانی توکن نوتیفیکیشن دستگاه");
                return Content(HttpStatusCode.InternalServerError, "خطا در به روزرسانی توکن");
            }
        }

        /// <summary>
        /// توکن نماینده : اول هدر Authorization (با یا بدون Bearer) و بعد فیلد AgentToken بدنه
        /// </summary>
        private string GetAgentToken(RegisterMobileDeviceModel model)
        {
            IEnumerable<string> AuthValues;
            string token = null;
            if (Request.Headers.TryGetValues("Authorization", out AuthValues))
            {
                token = AuthValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(token) && token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Substring("Bearer ".Length);
                }
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                token = model.AgentToken;
            }

            token = token == null ? null : token.Trim();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        /// <summary>
        /// آی پی کلاینت با در نظر گرفتن reverse proxy
        /// </summary>
        private static string GetClientIp()
        {
            try
            {
                var context = HttpContext.Current;
                if (context == null)
                {
                    return null;
                }

                var forwarded = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (!string.IsNullOrWhiteSpace(forwarded))
                {
                    return forwarded.Split(',')[0].Trim();
                }

                return context.Request.UserHostAddress;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// کوتاه کردن رشته تا طول ستون ، تا ورودی طولانی کلاینت باعث خطای ذخیره نشود
        /// </summary>
        private static string Cut(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            return value.Length <= max ? value : value.Substring(0, max);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
