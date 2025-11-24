using Antlr.Runtime.Misc;
using DataLayer.DomainModel;
using DataLayer.Repository;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Results;
using System.Web.Mvc;
using System.Web.WebPages;
using Telegram.Bot.Types;
using V2boardApi.Models;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    public class UserNotificationsController : Controller
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private Repository<tbNotifications> notificationRepository;
        private Repository<tbNotificationUser> notificationUserRepository;
        private Repository<tbUsers> usersRepository;
        private Entities db;
        public UserNotificationsController()
        {
            db = new Entities();
            notificationRepository = new Repository<tbNotifications>(db);
            usersRepository = new Repository<tbUsers>(db);
            notificationUserRepository = new Repository<tbNotificationUser>(db);
        }
        [AuthorizeApp(Roles = "1,3,4")]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> List()
        {
            try
            {
                var user = await usersRepository.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);

                if (user != null)
                {
                    var NotificatonList = await notificationRepository.WhereAsync(s => s.tbNoti_FK_User_ID == user.User_ID);
                    List<tbUserNotificationList_ViewModel> notificationList = new List<tbUserNotificationList_ViewModel>();
                    foreach (var item in NotificatonList)
                    {
                        tbUserNotificationList_ViewModel noti = new tbUserNotificationList_ViewModel();
                        noti.tbNoti_ID = item.tbNoti_ID;
                        noti.tbNoti_User = string.Join(", ", item.tbNotificationUser.Select(s => s.tbUsers?.Username).ToList());
                        noti.tbNoti_RegisterDate = item.tbNoti_RegisterDate.ConvertDateTimeToShamsi2();
                        noti.tbNoti_Text = item.tbNoti_Text;
                        noti.tbNoti_EndDate = item.tbNoti_EndDate.ConvertDatetimeToShamsiDate();
                        noti.tbNoti_Status = item.tbNoti_Status;
                        noti.tbNoti_Title = item.tbNoti_Title;
                        noti.tbNoti_UserSeen = string.Join(", ", item.tbNotificationUser.Where(s => s.tbNotiUser_Seen == true).Select(s => s.tbUsers?.Username).ToList());

                        notificationList.Add(noti);
                    }


                    return Json(new { data = notificationList }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    Response.SetStatus(System.Net.HttpStatusCode.Unauthorized);
                    return Json(new { error = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex,"نمایش اعلانات پنل مدیریت با خطا مواجه شد");
                return null;
            }
            
        }
        [HttpGet]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> Edit(int Noti_ID)
        {
            var user = await usersRepository.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);

            var Notificaton = await notificationRepository.FirstOrDefaultAsync(s => s.tbNoti_FK_User_ID == user.User_ID && s.tbNoti_ID == Noti_ID);
            if (Notificaton != null)
            {
                var Date = Utility.ConvertDateTimeToShamsi3(Notificaton.tbNoti_EndDate);
                return Json(new { status = "success", data = new { Noti_Title = Notificaton.tbNoti_Title, endDate = Date, usersSelect = Notificaton.tbNotificationUser.Select(s => s.tbNotiUser_FK_User_ID).ToList(), Noti_Text = Notificaton.tbNoti_Text } },JsonRequestBehavior.AllowGet);
            }
            else
            {
                return null;
            }
        }

        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeApp(Roles = "1,3,4")]
        public async Task<ActionResult> CreateOrEdit(string Noti_Title, string endDate, List<int> usersSelect, string Noti_Text,int Noti_ID= 0)
        {
            try
            {
                var user = await usersRepository.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
                if (user == null)
                {
                    return MessageBox.Warning("هشدار", "کاربر پیدا نشد");
                }

                if (Noti_ID == 0)
                {
                    tbNotifications noti = new tbNotifications
                    {
                        tbNoti_Text = Noti_Text,
                        tbNoti_RegisterDate = DateTime.Now,
                        tbNoti_FK_User_ID = user.User_ID,
                        tbNoti_Status = 1,
                        tbNoti_Title = Noti_Title
                    };

                    // Convert date with proper validation
                    if (!DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.GetCultureInfo("fa-IR"), DateTimeStyles.None, out DateTime parsedEndDate))
                    {
                        return MessageBox.Warning("هشدار", "لطفا تاریخ را به صورت صحیح وارد کنید");
                    }
                    noti.tbNoti_EndDate = parsedEndDate;

                    // Add selected users to notification
                    foreach (var userId in usersSelect)
                    {
                        tbNotificationUser notificationUser = new tbNotificationUser
                        {
                            tbNotiUser_FK_User_ID = userId,
                            tbNotiUser_Seen = false
                        };
                        noti.tbNotificationUser.Add(notificationUser);
                    }

                    notificationRepository.Insert(noti);
                    await notificationRepository.SaveChangesAsync();
                    logger.Info("اطلاعیه جدید اضافه شد");
                    return Toaster.Success("موفق", "اطلاعیه با موفقیت ثبت شد");
                }
                else
                {
                    // Fetch the existing notification
                    var noti = await notificationRepository.FirstOrDefaultAsync(s => s.tbNoti_ID == Noti_ID && s.tbNoti_FK_User_ID == user.User_ID);
                    if (noti == null)
                    {
                        return MessageBox.Warning("هشدار", "اطلاعیه پیدا نشد");
                    }

                    noti.tbNoti_Text = Noti_Text;
                    noti.tbNoti_RegisterDate = DateTime.Now;
                    noti.tbNoti_Title = Noti_Title;

                    // Convert date with proper validation
                    if (!DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.GetCultureInfo("fa-IR"), DateTimeStyles.None, out DateTime parsedEndDate))
                    {
                        return MessageBox.Warning("هشدار", "لطفا تاریخ را به صورت صحیح وارد کنید");
                    }
                    noti.tbNoti_EndDate = parsedEndDate;

                    // Remove users not in the selected list
                    var existingNotiUsers = noti.tbNotificationUser.ToList();
                    foreach (var notUser in existingNotiUsers.Where(u => !usersSelect.Contains(u.tbNotiUser_FK_User_ID)).ToList())
                    {
                        notificationUserRepository.Delete(notUser); // Safely remove notification users
                    }

                    // Reset seen status for remaining users
                    foreach (var item in noti.tbNotificationUser)
                    {
                        item.tbNotiUser_Seen = false;
                        item.tbNotiUser_DateSeen = null;
                    }

                    // Add new users who are not already part of the notification
                    var newUsers = usersSelect.Where(s => !noti.tbNotificationUser.Select(a => a.tbNotiUser_FK_User_ID).Contains(s)).ToList();
                    foreach (var newUserId in newUsers)
                    {
                        var notificationUser = new tbNotificationUser
                        {
                            tbNotiUser_FK_User_ID = newUserId,
                            tbNoti_FK_Noti_ID = Noti_ID,
                            tbNotiUser_Seen = false
                        };
                        noti.tbNotificationUser.Add(notificationUser);
                    }

                    await notificationRepository.SaveChangesAsync();
                    logger.Info("اطلاعیه ویرایش گردید");
                    return Toaster.Success("موفق", "اطلاعیه با موفقیت ویرایش شد");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "خطا در ثبت اطلاعیه");
                return MessageBox.Error("خطا", "خطا در ثبت اطلاعات");
            }
        }


        [HttpGet]
        [AuthorizeApp(Roles ="1,3,4")]
        public async Task<ActionResult> delete(int noti_id)
        {
            var user = await usersRepository.FirstOrDefaultAsync(s => s.Username == User.Identity.Name);
            var Notification = await notificationRepository.FirstOrDefaultAsync(s => s.tbNoti_ID == noti_id && s.tbNoti_FK_User_ID == user.User_ID);
            if (Notification != null)
            {
                notificationRepository.Delete(Notification);
                await notificationRepository.SaveChangesAsync();
                logger.Info("اطلاعیه حذف گردید");
                return Toaster.Success("موفق", "اطلاعیه با موفقیت حذف گردید");
            }
            else
            {
                return Toaster.Error("خطا", "خطا در حذف اطلاعیه");
            }
            
        }
    }
}