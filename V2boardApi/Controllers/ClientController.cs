using DataLayer.DomainModel;
using DataLayer.Repository;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;
using V2boardApi.Models.V2boardModel;
using V2boardApi.Models;
using V2boardApi.Tools;
using System.IO;
using System.Security.Policy;
using System.Net.Mail;
using System.Net;

namespace V2boardApi.Controllers
{
    public class ClientController : Controller
    {
        private Repository<tbServers> RepositoryServer { get; set; }
        private V2boardSiteEntities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbLinkUserAndPlans> RepositoryLinkUserAndPlan { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbOrders> RepositoryOrders { get; set; }
        public ClientController()
        {
            db = new V2boardSiteEntities();
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryOrders = new Repository<tbOrders>(db);
        }

        public ActionResult subscribe(string token)
        {
            var UserAgent = Request.UserAgent.ToLower();
            var server = RepositoryServer.table.Where(p => p.SubAddress.Contains(Request.Url.Host)).FirstOrDefault();
            if (UserAgent.Contains("nekoray") || UserAgent.Contains("surfboard") || UserAgent.Contains("nekobox") || UserAgent.Contains("v2rayng") || UserAgent.Contains("v2box") || UserAgent.Contains("foxray") || UserAgent.Contains("fair") || UserAgent.Contains("str") || UserAgent.Contains("shadow") || UserAgent.Contains("v2rayn"))
            {
                if (server != null)
                {
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(server.ServerAddress + "api/v1/");
                    client.DefaultRequestHeaders.UserAgent.TryParseAdd(Request.UserAgent);
                    var res = client.GetAsync(client.BaseAddress + "client/subscribe?token=" + token);
                    if (res.Result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var result = res.Result.Content.ReadAsStringAsync();
                        //var response = new HttpResponseMessage();
                        //response.Content = new StringContent(result.Result, Encoding.UTF8, "text/html");
                        if (string.IsNullOrEmpty(result.Result))
                        {
                            var ress = HttpUtility.UrlEncode("❌ پایان تاریخ اشتراک ❌");
                            var str = "vless://660e64cc-a610-48ed-88bf-1edba3c99a6b@test:443?encryption=none&flow=xtls-rprx-vision&security=reality&sni=www.speedtest.net&fp=random&pbk=ioE61VC3V30U7IdRmQ3bjhOq2ij9tPhVIgAD4JZ4YRY&sid=6ba85179e30d4fc2&type=tcp&headerType=none#" + ress;
                            var base64 = Utility.Base64Encode(str);
                            return Content(base64, "text/html", Encoding.UTF8);
                        }
                        return Content(result.Result, "text/html", Encoding.UTF8);
                    }
                }
            }
            else
            {
                var url = Response.Cookies["url"];
                if (url == null)
                {
                    HttpCookie cookie = new HttpCookie("url");
                    cookie.Value = Request.Url.ToString();
                    cookie.Expires = DateTime.Now.AddYears(100);
                    Response.Cookies.Add(cookie);
                }
                else
                {
                    url.Value = Request.Url.ToString();
                    url.Expires = DateTime.Now.AddYears(100);
                }
                
                if (server != null)
                {
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(server.ServerAddress);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("Authorization", server.Auth_Token);

                    var result = client.GetAsync(client.BaseAddress + "api/v1/" + server.AdminPath + "/user/fetch?filter[0][key]=token&filter[0][condition]=%3D&filter[0][value]=" + token + "&pageSize=10");
                    if (result.Result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var Content = result.Result.Content.ReadAsStringAsync();
                        var Con = Content.Result.ToString();

                        var res = JObject.Parse(Con);
                        var data = res["data"].ToString();
                        var Js = JsonConvert.DeserializeObject<List<GetUserModel>>(data);
                        if (Js != null)
                        {
                            if (Js.Count >= 1)
                            {
                                var item2 = Js[0];
                                GetUserDataModel getUserData = new GetUserDataModel();
                                getUserData.IsActive = "فعال";
                                getUserData.Name = item2.email.Split('@')[0];
                                getUserData.IsBanned = Convert.ToBoolean(item2.banned);
                                getUserData.TotalVolume = Utility.ConvertByteToGB(item2.transfer_enable).ToString() + " GB";
                                getUserData.id = item2.id;
                                if (item2.expired_at != null)
                                {
                                    var ex = Utility.ConvertSecondToDatetime((long)item2.expired_at);
                                    getUserData.ExpireDate = Utility.ConvertDateTimeToShamsi(ex);
                                    getUserData.DaysLeft = Utility.CalculateLeftDayes(ex);

                                    if (ex <= DateTime.Today)
                                    {
                                        getUserData.IsActive = "پایان تاریخ اشتراک";
                                    }
                                    if (getUserData.DaysLeft <= 2)
                                    {
                                        getUserData.CanEdit = true;
                                    }

                                }
                                if (getUserData.IsBanned)
                                {
                                    getUserData.IsActive = "مسدود";
                                }


                                getUserData.PlanName = item2.plan_name;
                                getUserData.SubLink = item2.subscribe_url;

                                var re = Utility.ConvertByteToGB(item2.u + item2.d);
                                getUserData.UsedVolume = Math.Round(re, 2) + " GB";

                                var vol = item2.transfer_enable - (item2.u + item2.d);

                                if (vol <= 0)
                                {
                                    getUserData.IsActive = "اتمام حجم";
                                }
                                var d = Utility.ConvertByteToGB(vol);
                                if (d <= 2)
                                {
                                    getUserData.CanEdit = true;
                                }
                                getUserData.RemainingVolume = Math.Round(d, 2) + " GB";
                                var name = item2.email.Split('@')[1];
                                var User = RepositoryUser.table.Where(p => p.Username == name).FirstOrDefault();
                                if (User != null)
                                {
                                    ViewBag.TelegramID = User.TelegramID;
                                    ViewBag.Title = User.BussinesTitle;
                                }


                                ViewBag.Url = server.ServerAddress + "api/v1/" + "client/subscribe?token=" + token;
                                if (item2.email.Split('@')[1].Contains("."))
                                {
                                    ViewBag.LinkCreator = item2.email.Split('@')[1].Split('.')[0];
                                }
                                else
                                {
                                    ViewBag.LinkCreator = item2.email.Split('@')[1];
                                }

                                ViewBag.FirstName = User.FirstName; ViewBag.LastName = User.LastName; ViewBag.Card_Number = User.Card_Number;

                                return View(getUserData);
                            }
                        }

                    }
                }


                return PartialView();

            }


            return null;

        }

        public ActionResult UpdateAccount(string username, string Name, int id,string FirstName=null, string LastName = null, long Card_Number = 0)
        {
            var Plans = db.tbLinkUserAndPlans.Where(p => p.tbUsers.Username == username).ToList();
            ViewBag.Name = Name + "@" + username;
            ViewBag.Id = id;
            ViewBag.FirstName = FirstName; ViewBag.LastName = LastName; ViewBag.CardNumber = Card_Number;

            return PartialView(Plans);
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult UpdateAccount(int PlanID, HttpPostedFileBase ResidFile, string Name, int id)
        {
            var url = Request.Cookies["url"];
            try
            {
                if (ResidFile.ContentLength > 0)
                {
                    var ext = Path.GetExtension(ResidFile.FileName);
                    if (ext == ".pdf" || ext == ".png" || ext == ".jpg")
                    {
                        var OrderGuid = Guid.NewGuid();
                        var filename = Name + "_" + OrderGuid;
                        var path = Server.MapPath("~/Content/Receipts/") + filename + Path.GetExtension(ResidFile.FileName);
                        var ServerPath = "https://" + Request.Url.Authority + "/Content/Receipts/" + filename + Path.GetExtension(ResidFile.FileName);
                        var AcceptPath = "https://" + Request.Url.Authority + "/api/v1/client/AcceptOrder?token=" + OrderGuid;
                        tbOrders order = new tbOrders();
                        order.AccountName = Name;
                        var username = Name.Split('@')[1];
                        var admin = db.tbUsers.Where(p => p.Username == username).FirstOrDefault();
                        order.FK_OrderAdmin_ID = admin.User_ID;
                        order.FK_Plan_ID = db.tbPlans.Where(p => p.Plan_ID == PlanID).FirstOrDefault().Plan_ID;
                        order.OrderDate = DateTime.Now;
                        order.OrderStatus = "در انتظار تائید";
                        order.OrderType = "تمدید";
                        order.Order_Guid = OrderGuid;
                        order.V2_User_ID = id;
                        RepositoryOrders.Insert(order);
                        RepositoryOrders.Save();
                        ResidFile.SaveAs(path);

                        var text = "<div dir='rtl' style='width: 100%;height: 100px;background: rgba(7, 136, 223, 0.562);'><span>  لطفا برای تائید رسید روی</span><a style='font-size: 24px;margin-right: 10px;' href='" + AcceptPath + "'>تائید</a><span style='margin-right: 10px;'>کلیک کنید </span>   <br><br><a style='font-size: 24px;margin-right: 10px;' href='" + ServerPath + "'>نمایش رسید</a></div>";


                        SendGmail("تمدید اکانت" + " " + Name.Split('@')[0], text, admin.Email, admin.FirstName + " " + admin.LastName);



                        if (url != null)
                        {
                            TempData["status"] = true;
                            TempData["message"] = "درخواست تمدید شما با موفقیت ثبت گردید لطفا منتظر تائید درخواست بمانید بعد از تائید اشتراکتون فعال میشه حتما این صفحه رو چک کنید";
                            return RedirectToAction("subscribe", new { token = url.Value.Split('=')[1] });
                        }

                        return PartialView();
                    }
                    else
                    {
                        TempData["status"] = false;
                        TempData["message"] = "پسوند فایل باید با نوع png,jpg,pdf باشد";
                        return RedirectToAction("subscribe", new { token = url.Value.Split('=')[1] });
                    }

                }
                else
                {
                    TempData["status"] = false;
                    TempData["message"] = "لطفا رسید واریزی را آپلود کنید";
                    return RedirectToAction("subscribe", new { token = url.Value.Split('=')[1] });
                }
            }
            catch (Exception ex)
            {
                TempData["status"] = false;
                TempData["message"] = "تمدید با خطا مواجه شد لطفا با پشتیبانی ارتباط بگیرید";
                return RedirectToAction("subscribe", new { token = url.Value.Split('=')[1] });
            }

        }

        [System.Web.Mvc.HttpGet]
        public ActionResult AcceptOrder(string token)
        {
            var ConvertGuid = Guid.Parse(token);
            var Order = RepositoryOrders.table.Where(p => p.Order_Guid == ConvertGuid).FirstOrDefault();
            if (Order != null)
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(Order.tbUsers.tbServers.ServerAddress + "api/v1/" + Order.tbUsers.tbServers.AdminPath);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", Order.tbUsers.tbServers.Auth_Token);

                Dictionary<string, string> exp = new Dictionary<string, string>
                {
                    { "id", Order.V2_User_ID.ToString() },
                    { "email", Order.AccountName },
                    { "u", "0" },
                    { "d", "0" },
                    { "is_staff", "0" },
                    { "is_admin", "0" },
                    { "banned", "0" }
                };



                var t = ((Convert.ToInt64(Order.tbPlans.PlanVolume) * 1024) * 1024) * 1024;
                exp.Add("expired_at", DateTime.Now.AddDays((int)Order.tbPlans.CountDayes).ConvertDatetimeToSecond().ToString());
                exp.Add("transfer_enable", t.ToString());


                var Form = new FormUrlEncodedContent(exp);

                var addr = client.BaseAddress + "/user/update";
                var request = client.PostAsync(addr, Form);
                if (request.Result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Order.OrderStatus = "تائید سفارش";
                    var link = RepositoryLinkUserAndPlan.table.Where(p => p.L_FK_U_ID == Order.tbUsers.User_ID && p.L_FK_P_ID == Order.tbPlans.Plan_ID && p.L_Status == true).FirstOrDefault();
                    Order.tbUsers.Wallet += link.tbPlans.Price;
                    AddLog(Resource.LogActions.U_Edited, link.Link_PU_ID, Order.AccountName);
                    RepositoryOrders.Save();
                    return Content("اکانت با موفقیت تمدید شد");
                }
                else
                {
                    var result = request.Result.Content.ReadAsStringAsync();
                    return Content("اکانت تمدید نشد");
                }

            }

            return Content("اکانت تمدید نشد");
        }


        public void SendGmail(string Subject, string BodyText, string ToEmail, string ToFullName)
        {
            var fromAddress = new MailAddress("darkbazsp@gmail.com", "پشتیبانی DARKBAZ");

            var toAddress = new MailAddress(ToEmail, ToFullName);
            string fromPassword = "cclabboynxuwqdoa";
            string subject = Subject;



            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword),

            };
            using (var messagee = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = BodyText,
                IsBodyHtml = true,
            })
            {
                smtp.Send(messagee);
            }
        }

        public bool AddLog(string Action, int LinkUserID, string V2User)
        {
            try
            {
                tbLogs tbLogs = new tbLogs();
                tbLogs.FK_Link_User_Plan_ID = LinkUserID;
                tbLogs.Action = Action;
                tbLogs.FK_NameUser_ID = V2User;
                tbLogs.CreateDatetime = DateTime.Now;
                RepositoryLogs.Insert(tbLogs);
                return RepositoryLogs.Save();
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}