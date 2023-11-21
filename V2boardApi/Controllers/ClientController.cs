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
using Newtonsoft.Json;

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
        private Repository<tbUpdateLogs> RepositoryUpdateLogs { get; set; }
        public ClientController()
        {
            db = new V2boardSiteEntities();
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryOrders = new Repository<tbOrders>(db);
            RepositoryUpdateLogs = new Repository<tbUpdateLogs>(db);
        }



        public ActionResult subscribe(string token)
        {
            var UserAgent = Request.UserAgent.ToLower();
            var server = RepositoryServer.table.Where(p => p.SubAddress.Contains(Request.Url.Host)).FirstOrDefault();
            if (UserAgent.Contains("wing") || UserAgent.Contains("nekoray") || UserAgent.Contains("surfboard") || UserAgent.Contains("nekobox") || UserAgent.Contains("v2rayng") || UserAgent.Contains("v2box") || UserAgent.Contains("foxray") || UserAgent.Contains("fair") || UserAgent.Contains("str") || UserAgent.Contains("shadow") || UserAgent.Contains("v2rayn"))
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
                var url = Request.Cookies["url"];
                if (url == null)
                {
                    HttpCookie cookie = new HttpCookie("url");
                    cookie.Value = (Server.HtmlEncode(Request.Url.ToString())).Replace(";", "");
                    cookie.Expires = DateTime.Now.AddYears(100);
                    Response.Cookies.Add(cookie);
                }
                else
                {
                    var url1 = Response.Cookies["url"];
                    url1.Value = (Server.HtmlEncode(Request.Url.ToString())).Replace(";", "");
                    url1.Expires = DateTime.Now.AddYears(100);
                }

                if (server != null)
                {
                    MySqlEntities sqlEntities = new MySqlEntities(server.ConnectionString);
                    sqlEntities.Open();
                    var query = "SELECT email,d,u,transfer_enable,banned,expired_at FROM v2_user where token='" + token + "'";
                    var reader = sqlEntities.GetData(query);
                    while (reader.Read())
                    {
                        GetUserDataModel getUserData = new GetUserDataModel();
                        getUserData.IsActive = "فعال";
                        getUserData.Name = reader.GetString("email").Split('@')[0];
                        getUserData.IsBanned = reader.GetBoolean("banned");
                        getUserData.TotalVolume = Utility.ConvertByteToGB(reader.GetDouble("transfer_enable")).ToString() + " GB";
                        var exp = reader.GetBodyDefinition("expired_at");
                        if (exp != "")
                        {
                            var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp));
                            var onlineTime = Utility.ConvertSecondToDatetime(reader.GetDouble("expired_at"));
                            if (onlineTime <= DateTime.Now.AddMinutes(-2))
                            {
                                getUserData.IsOnline = true;
                            }
                            getUserData.LastTimeOnline = Utility.ConvertDateTimeToShamsi(onlineTime);
                            getUserData.ExpireDate = Utility.ConvertDateTimeToShamsi(ex);
                            getUserData.DaysLeft = Utility.CalculateLeftDayes(ex);

                            if (ex <= DateTime.Now)
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

                        getUserData.SubLink = getUserData.SubLink = "https://" + server.SubAddress + "/api/v1/client/subscribe?token=" + token;

                        var re = Utility.ConvertByteToGB(reader.GetDouble("d")+ reader.GetDouble("u"));
                        getUserData.UsedVolume = Math.Round(re, 2) + " GB";

                        var vol = reader.GetInt64("transfer_enable") - (reader.GetDouble("d") + reader.GetDouble("u"));

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
                        var name = reader.GetString("email").Split('@')[1];

                        ViewBag.Url = server.ServerAddress + "api/v1/" + "client/subscribe?token=" + token;
                        if (reader.GetString("email").Split('@')[1].Contains("."))
                        {
                            ViewBag.LinkCreator = reader.GetString("email").Split('.')[0];
                        }
                        else
                        {
                            ViewBag.LinkCreator = reader.GetString("email").Split('@')[1];
                        }




                        var ManiText = Server.MapPath("/Content/Manifests/") + reader.GetString("email") + ".json";
                        if (!System.IO.File.Exists(ManiText))
                        {
                            var ManiPath = Server.MapPath("/") + "manifest.json";
                            System.IO.File.Copy(ManiPath, ManiText);
                        }
                        var Text = System.IO.File.ReadAllText(ManiText);

                        var mani = JsonConvert.DeserializeObject<ManifestModel>(Text);
                        mani.name = reader.GetString("email").Split('@')[0];
                        mani.short_name = reader.GetString("email").Split('@')[0];


                        var User = RepositoryUser.table.Where(p => p.Username == name && p.Status == true).FirstOrDefault();
                        if (User != null)
                        {
                            ViewBag.IsRenew = User.IsRenew;
                            ViewBag.TelegramID = User.TelegramID;
                            ViewBag.Title = User.BussinesTitle;
                            ViewBag.FirstName = User.FirstName; ViewBag.LastName = User.LastName; ViewBag.Card_Number = User.Card_Number;

                            var PwaIcon = "images/" + User.Username + "192.png";
                            var PwaIcon1 = "images/" + User.Username + "512.png";
                            var SrcIcon = Server.MapPath("/Content/Manifests/images/") + User.Username + "192.png";
                            if (System.IO.File.Exists(SrcIcon))
                            {
                                mani.icons[0].src = PwaIcon;
                                mani.icons[1].src = PwaIcon1;
                            }
                        }

                        var maniJs = JsonConvert.SerializeObject(mani);
                        System.IO.File.WriteAllText(ManiText, maniJs);

                        var httpPath = "https://" + Request.Url.Authority + "/Content/Manifests/" + reader.GetString("email") + ".json";
                        ViewBag.manifest = httpPath;
                        return View(getUserData);
                    }

                }


                return PartialView();

            }


            return null;

        }

        //public ActionResult UpdateAccount(string username, string Name, int id, string FirstName = null, string LastName = null, long Card_Number = 0)
        //{
        //    var Plans = db.tbLinkUserAndPlans.Where(p => p.tbUsers.Username == username && p.L_Status == true).ToList();
        //    ViewBag.Name = Name + "@" + username;
        //    ViewBag.Id = id;
        //    ViewBag.FirstName = FirstName; ViewBag.LastName = LastName; ViewBag.CardNumber = Card_Number;

        //    return PartialView(Plans);
        //}

        //[System.Web.Mvc.HttpPost]
        //public ActionResult UpdateAccount(int PlanID, HttpPostedFileBase ResidFile, string Name, int id)
        //{
        //    var url = Request.Cookies["url"];
        //    try
        //    {
        //        if (ResidFile.ContentLength > 0)
        //        {
        //            var ext = Path.GetExtension(ResidFile.FileName);
        //            if (ext == ".pdf" || ext == ".png" || ext == ".jpg")
        //            {
        //                var OrderGuid = Guid.NewGuid();
        //                var filename = Name + "_" + OrderGuid;
        //                var path = Server.MapPath("~/Content/Receipts/") + filename + Path.GetExtension(ResidFile.FileName);
        //                var ServerPath = "https://" + Request.Url.Authority + "/Content/Receipts/" + filename + Path.GetExtension(ResidFile.FileName);
        //                var AcceptPath = "https://" + Request.Url.Authority + "/api/v1/client/AcceptOrder?token=" + OrderGuid;
        //                tbOrders order = new tbOrders();
        //                order.AccountName = Name;
        //                var username = Name.Split('@')[1];
        //                var admin = db.tbUsers.Where(p => p.Username == username).FirstOrDefault();
        //                order.FK_OrderAdmin_ID = admin.User_ID;
        //                order.FK_Plan_ID = db.tbPlans.Where(p => p.Plan_ID == PlanID && p.Status == true).FirstOrDefault().Plan_ID;
        //                order.OrderDate = DateTime.Now;
        //                order.OrderStatus = "در انتظار تائید";
        //                order.OrderType = "تمدید";
        //                order.Order_Guid = OrderGuid;
        //                order.V2_User_ID = id;
        //                RepositoryOrders.Insert(order);
        //                RepositoryOrders.Save();
        //                ResidFile.SaveAs(path);

        //                var text = "<div dir='rtl' style='width: 100%;height: 100px;background: rgba(7, 136, 223, 0.562);'><span>  لطفا برای تائید رسید روی</span><a style='font-size: 24px;margin-right: 10px;' href='" + AcceptPath + "'>تائید</a><span style='margin-right: 10px;'>کلیک کنید </span>   <br><br><a style='font-size: 24px;margin-right: 10px;' href='" + ServerPath + "'>نمایش رسید</a></div>";


        //                SendGmail("تمدید اکانت" + " " + Name.Split('@')[0], text, admin.Email, admin.FirstName + " " + admin.LastName);



        //                if (url != null)
        //                {
        //                    TempData["status"] = true;
        //                    TempData["message"] = "درخواست تمدید شما با موفقیت ثبت گردید لطفا منتظر تائید درخواست بمانید بعد از تائید اشتراکتون فعال میشه حتما این صفحه رو چک کنید";
        //                    return RedirectToAction("subscribe", new { token = url.Value.Split('=')[1] });
        //                }

        //                return PartialView();
        //            }
        //            else
        //            {
        //                TempData["status"] = false;
        //                TempData["message"] = "پسوند فایل باید با نوع png,jpg,pdf باشد";
        //                return RedirectToAction("subscribe", new { token = url.Value.Split('=')[1] });
        //            }

        //        }
        //        else
        //        {
        //            TempData["status"] = false;
        //            TempData["message"] = "لطفا رسید واریزی را آپلود کنید";
        //            return RedirectToAction("subscribe", new { token = url.Value.Split('=')[1] });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["status"] = false;
        //        TempData["message"] = "تمدید با خطا مواجه شد لطفا با پشتیبانی ارتباط بگیرید";
        //        return RedirectToAction("subscribe", new { token = url.Value.Split('=')[1] });
        //    }

        //}

        //[System.Web.Mvc.HttpGet]
        //public ActionResult AcceptOrder(string token)
        //{
        //    var ConvertGuid = Guid.Parse(token);
        //    var Order = RepositoryOrders.table.Where(p => p.Order_Guid == ConvertGuid).FirstOrDefault();
        //    if (Order != null)
        //    {
        //        HttpClient client = new HttpClient();
        //        client.BaseAddress = new Uri(Order.tbUsers.tbServers.ServerAddress + "api/v1/" + Order.tbUsers.tbServers.AdminPath);
        //        client.DefaultRequestHeaders.Clear();
        //        client.DefaultRequestHeaders.Add("Authorization", Order.tbUsers.tbServers.Auth_Token);

        //        Dictionary<string, string> exp = new Dictionary<string, string>
        //        {
        //            { "id", Order.V2_User_ID.ToString() },
        //            { "email", Order.AccountName },
        //            { "u", "0" },
        //            { "d", "0" },
        //            { "is_staff", "0" },
        //            { "is_admin", "0" },
        //            { "banned", "0" }
        //        };



        //        var t = ((Convert.ToInt64(Order.tbPlans.PlanVolume) * 1024) * 1024) * 1024;
        //        exp.Add("expired_at", DateTime.Now.AddDays((int)Order.tbPlans.CountDayes).ConvertDatetimeToSecond().ToString());
        //        exp.Add("transfer_enable", t.ToString());


        //        var Form = new FormUrlEncodedContent(exp);

        //        var addr = client.BaseAddress + "/user/update";
        //        var request = client.PostAsync(addr, Form);
        //        if (request.Result.StatusCode == System.Net.HttpStatusCode.OK)
        //        {
        //            Order.OrderStatus = "تائید سفارش";
        //            var link = RepositoryLinkUserAndPlan.table.Where(p => p.L_FK_U_ID == Order.tbUsers.User_ID && p.L_FK_P_ID == Order.tbPlans.Plan_ID && p.L_Status == true).FirstOrDefault();
        //            Order.tbUsers.Wallet += link.tbPlans.Price;
        //            AddLog(Resource.LogActions.U_Edited, link.Link_PU_ID, Order.AccountName);
        //            RepositoryOrders.Save();
        //            return Content("اکانت با موفقیت تمدید شد");
        //        }
        //        else
        //        {
        //            var result = request.Result.Content.ReadAsStringAsync();
        //            return Content("اکانت تمدید نشد");
        //        }

        //    }

        //    return Content("اکانت تمدید نشد");
        //}


        //public void SendGmail(string Subject, string BodyText, string ToEmail, string ToFullName)
        //{
        //    var fromAddress = new MailAddress("darkbazsp@gmail.com", "پشتیبانی DARKBAZ");

        //    var toAddress = new MailAddress(ToEmail, ToFullName);
        //    string fromPassword = "cclabboynxuwqdoa";
        //    string subject = Subject;



        //    var smtp = new SmtpClient
        //    {
        //        Host = "smtp.gmail.com",
        //        Port = 587,
        //        EnableSsl = true,
        //        DeliveryMethod = SmtpDeliveryMethod.Network,
        //        UseDefaultCredentials = true,
        //        Credentials = new NetworkCredential(fromAddress.Address, fromPassword),

        //    };
        //    using (var messagee = new MailMessage(fromAddress, toAddress)
        //    {
        //        Subject = subject,
        //        Body = BodyText,
        //        IsBodyHtml = true,
        //    })
        //    {
        //        smtp.Send(messagee);
        //    }
        //}

        //public bool AddLog(string Action, int LinkUserID, string V2User)
        //{
        //    try
        //    {
        //        tbLogs tbLogs = new tbLogs();
        //        tbLogs.FK_Link_User_Plan_ID = LinkUserID;
        //        tbLogs.Action = Action;
        //        tbLogs.FK_NameUser_ID = V2User;
        //        tbLogs.CreateDatetime = DateTime.Now;
        //        RepositoryLogs.Insert(tbLogs);
        //        return RepositoryLogs.Save();
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
    }
}