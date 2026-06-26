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
using System.Web.Http.Cors;
using System.Runtime.Remoting.Messaging;
using V2boardApi.Areas.App.Data.SubscriptionsViewModels;
using V2boardApi.Areas.api.Data.ViewModels;
using System.Threading.Tasks;
using Mysqlx.Expr;
using System.Windows.Forms;
using System.Buffers.Text;
using static System.Windows.Forms.LinkLabel;
using DeviceDetectorNET.Class.Device;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Fluent;
using MySqlConnector;

namespace V2boardApi.Areas.api.Controllers
{
    [EnableCors(origins: "*", "*", "*")]
    [LogActionFilter]
    public class ClientController : Controller
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Repository<tbServers> RepositoryServer { get; set; }
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbLinkUserAndPlans> RepositoryLinkUserAndPlan { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbOrders> RepositoryOrders { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private Repository<tbLinks> RepositoryLinks { get; set; }
        private static tbServers server;
        public ClientController()
        {
            db = new Entities();
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryOrders = new Repository<tbOrders>(db);
            RepositoryPlans = new Repository<tbPlans>(db);
            RepositoryLinks = new Repository<tbLinks>(db);
            server = HttpRuntime.Cache["Server"] as tbServers;
        }

        public async Task<ActionResult> subscribe(string token)
        {
            try
            {
                var UserAgent = Request.UserAgent.ToLower();
                var host = Request.Url.Host;

                if (!(UserAgent.Contains("chrome") || UserAgent.Contains("firefox") || UserAgent.Contains("safari") || UserAgent.Contains("edg") || UserAgent.Contains("samsung")))
                {
                    if (server != null)
                    {
                        HttpClient client = new HttpClient();
                        client.BaseAddress = new Uri(server.ServerAddress + "/api/v1/");
                        client.DefaultRequestHeaders.UserAgent.TryParseAdd(Request.UserAgent);
                        var res = client.GetAsync(client.BaseAddress + "client/subscribe?token=" + token);
                        if (res.Result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var result = res.Result.Content.ReadAsStringAsync();

                            MySqlEntities sqlEntities = new MySqlEntities(server.ConnectionString);
                            await sqlEntities.OpenAsync();
                            if (UserAgent.StartsWith("hiddify"))
                            {

                                var query = "SELECT id,email,d,u,transfer_enable,banned,expired_at FROM v2_user where token='" + token + "'";
                                var reader = await sqlEntities.GetDataAsync(query);
                                while (await reader.ReadAsync())
                                {
                                    var str = "upload=" + reader["u"] + ";download=" + reader["d"] + ";total=" + reader["transfer_enable"] + ";expire=" + reader["expired_at"];
                                    var name = reader.GetString("email").Split('@')[0];
                                    var base64 = Utility.Base64Encode(name.Split('$')[0]);

                                    Response.Headers.Add("subscription-userinfo", str);
                                    Response.Headers.Add("profile-title", "base64:" + base64);

                                }
                                reader.Close();
                            }

                            if (UserAgent.StartsWith("safenet"))
                            {

                                var query = "SELECT id,email,d,u,transfer_enable,banned,expired_at FROM v2_user where token='" + token + "'";
                                var reader = await sqlEntities.GetDataAsync(query);
                                var IsGetSub = false;
                                while (await reader.ReadAsync())
                                {
                                    IsGetSub = true;
                                    var name = reader.GetString("email").Split('@')[0];
                                    var base64 = Utility.Base64Encode(name.Split('$')[0]);
                                   

                                    int Status = 1;
                                    var VolumeRemaning = reader.GetInt64("transfer_enable") - (reader.GetDouble("d") + reader.GetDouble("u"));
                                    if (VolumeRemaning <= 0)
                                    {
                                        VolumeRemaning = 0;
                                        Status = 2;
                                    }
                                    Response.Headers.Add("remaining-volume", Utility.ConvertByteToGB(VolumeRemaning).ToString());
                                    var n = reader.GetString("email");
                                    var Log = RepositoryLogs.Where(s => s.FK_NameUser_ID == n || s.SubToken == token).OrderByDescending(s => s.CreateDatetime).ToList().FirstOrDefault();
                                    if (Log != null)
                                    {
                                        if (Log.tbLinkUserAndPlans.tbUsers.BussinesTitle != null)
                                        {
                                            Response.Headers.Add("bussines-title", Utility.Base64Encode(Log.tbLinkUserAndPlans.tbUsers.BussinesTitle));
                                        }
                                        else
                                        {
                                            Response.Headers.Add("bussines-title", "");
                                        }

                                        if (Log.tbLinkUserAndPlans.tbUsers.TelegramID != null)
                                        {
                                            Response.Headers.Add("telegram-agent", Log.tbLinkUserAndPlans.tbUsers.TelegramID);
                                        }
                                        else
                                        {
                                            Response.Headers.Add("telegram-agent", "");
                                        }
                                    }
                                    else
                                    {
                                        var link = await RepositoryLinks.FirstOrDefaultAsync(s => s.tbL_Token == token);
                                        if(link != null)
                                        {
                                            if (link.tbTelegramUsers?.tbUsers?.BussinesTitle != null)
                                            {
                                                Response.Headers.Add("bussines-title", Utility.Base64Encode(link.tbTelegramUsers.tbUsers.BussinesTitle));
                                            }
                                            else
                                            {
                                                Response.Headers.Add("bussines-title", "");
                                            }

                                            if (link.tbTelegramUsers?.tbUsers?.TelegramID != null)
                                            {
                                                Response.Headers.Add("telegram-agent", link.tbTelegramUsers.tbUsers.TelegramID);
                                            }
                                            else
                                            {
                                                Response.Headers.Add("telegram-agent", "");
                                            }
                                        }
                                    }
                                    var exipre = reader["expired_at"];
                                    if (exipre != null)
                                    {
                                        var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(exipre));
                                        var day = Utility.CalculateLeftDayes(ex);
                                        if(day <= 0)
                                        {
                                            Status = 3;
                                        }
                                        if (day <= 0 && VolumeRemaning <= 0)
                                        {
                                            Status = 4;
                                        }
                                        Response.Headers.Add("remaining-day", day.ToString());
                                        Response.Headers.Add("expire-date", Utility.ConvertDateTimeToShamsi2(ex));
                                        
                                    }
                                    else
                                    {
                                        Response.Headers.Add("remaining-day", "-1");
                                        Response.Headers.Add("expire-date", "-1");
                                    }
                                    Response.Headers.Add("profile-title", Utility.Base64Encode(name.Split('$')[0]));

                                    var Banned = reader.GetBoolean("banned");
                                    if (Banned)
                                    {
                                        Status = 5;
                                    }

                                    Response.Headers.Add("sub-status", Status.ToString());
                                }

                                if(IsGetSub == false)
                                {
                                    return HttpNotFound();
                                }

                                reader.Close();
                            }

                            if (string.IsNullOrEmpty(result.Result))
                            {
                                var query1 = "SELECT uuid,email,u,d,transfer_enable FROM v2_user where token='" + token + "'";
                                var reader2 = await sqlEntities.GetDataAsync(query1);
                                await reader2.ReadAsync();
                                var Admin = server.tbUsers.Where(p => p.Role == 1).FirstOrDefault();

                                var ress = HttpUtility.UrlEncode("❌ کاربر گرامی زمان اشتراک شما به پایان رسیده است ❌");
                                var vless = "vless://" + reader2.GetString("uuid") + "@test:443?encryption=none&flow=xtls-rprx-vision&security=reality&sni=www.speedtest.net&fp=random&pbk=ioE61VC3V30U7IdRmQ3bjhOq2ij9tPhVIgAD4JZ4YRY&sid=6ba85179e30d4fc2&type=tcp&headerType=none#";
                                var link1 = vless + ress;

                                var passvand = reader2.GetString("email").Split('@')[1];


                                var link2 = "";
                                var link3 = "";

                                if (passvand == Admin.Username)
                                {
                                    var ress1 = HttpUtility.UrlEncode("❌ شما می توانید از طریق ربات به آیدی " + server.Robot_ID + " اکانت خود را مجدد شارژ کنید ❌");
                                    link2 = vless + ress1;
                                }
                                else
                                {
                                    var Admin2 = server.tbUsers.Where(p => p.Username == passvand).FirstOrDefault();
                                    if (Admin2 != null)
                                    {
                                        if (Admin2.TelegramID != null)
                                        {
                                            var ress1 = HttpUtility.UrlEncode("❌ شما می توانید در تلگرام به آیدی " + Admin2.TelegramID + " پیام دهید تا اکانتتان مجدد شارژ شود ❌");
                                            link3 = vless + ress1;
                                        }
                                    }
                                }

                                var base64 = link1 + "\n" + link2 + "\n" + link3;

                                reader2.Close();
                                await sqlEntities.CloseAsync();
                                return Content(base64, "text/html", Encoding.UTF8);
                            }
                            await sqlEntities.CloseAsync();
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
                        using (var sqlEntities = new MySqlEntities(server.ConnectionString))
                        {
                            await sqlEntities.OpenAsync();
                            var disc = new Dictionary<string, object> { { "@token", token } };
                            var reader = await sqlEntities.GetDataAsync(
                                "SELECT id,email,d,u,transfer_enable,banned,expired_at FROM v2_user WHERE token=@token", disc);

                            GetUserDataModel getUserData = null;
                            string email = null;
                            if (reader.Read())
                            {
                                getUserData = MapSubscribeUser(reader, token, server, out email);
                                SetSubscribeViewBag(email, token, server);
                            }
                            reader.Close();

                            if (getUserData == null)
                                return HttpNotFound();

                            ViewBag.ReservedPackages = GetReservedPackages(email);
                            return View(getUserData);
                        }
                    }


                    return HttpNotFound();

                }


                return null;
            }
            catch (Exception ex)
            {
                logger.Warn(ex.Message+"|" + ex.StackTrace, ex);
                return HttpNotFound();
            }

        }

        public ActionResult android(string token)
        {
            Dictionary<string, string> key = new Dictionary<string, string>();
            key.Add("link", token);
            return View(key);
        }
        public ActionResult ios(string token)
        {
            Dictionary<string, string> key = new Dictionary<string, string>();
            key.Add("link", token);
            return View(key);
        }
        public ActionResult windows(string token)
        {
            Dictionary<string, string> key = new Dictionary<string, string>();
            key.Add("link", token);
            return View(key);
        }
        public ActionResult linux(string token)
        {
            Dictionary<string, string> key = new Dictionary<string, string>();
            key.Add("link", token);
            return View(key);
        }

        private static GetUserDataModel MapSubscribeUser(MySqlDataReader reader, string token, tbServers server, out string email)
        {
            var ordEmail = reader.GetOrdinal("email");
            var ordId = reader.GetOrdinal("id");
            var ordBanned = reader.GetOrdinal("banned");
            var ordTransferEnable = reader.GetOrdinal("transfer_enable");
            var ordExpiredAt = reader.GetOrdinal("expired_at");
            var ordD = reader.GetOrdinal("d");
            var ordU = reader.GetOrdinal("u");

            email = reader.GetString(ordEmail);
            var model = new GetUserDataModel
            {
                id = reader.GetInt32(ordId),
                IsActive = 1,
                Name = email.Split('@')[0],
                IsBanned = reader.GetBoolean(ordBanned),
                TotalVolume = Utility.ConvertByteToGB(reader.GetInt64(ordTransferEnable)).ToString() + " GB",
                SubLink = "https://" + server.SubAddress + "/api/v1/client/subscribe?token=" + token
            };

            var exp = reader.IsDBNull(ordExpiredAt) ? null : reader.GetValue(ordExpiredAt)?.ToString();
            if (!string.IsNullOrEmpty(exp))
            {
                var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(exp));
                var onlineTime = Utility.ConvertSecondToDatetime(reader.GetInt64(ordExpiredAt));
                if (onlineTime <= DateTime.Now.AddMinutes(-2))
                    model.IsOnline = true;

                model.LastTimeOnline = Utility.ConvertDateTimeToShamsi(onlineTime);
                model.ExpireDate = Utility.ConvertDateTimeToShamsi5(ex);
                model.DaysLeft = Utility.CalculateLeftDayes(ex);

                if (ex <= DateTime.Now)
                    model.IsActive = 3;
                if (model.DaysLeft <= 2)
                    model.CanEdit = true;
            }

            if (model.IsBanned)
                model.IsActive = 5;

            var usedGb = Utility.ConvertByteToGB(reader.GetInt64(ordD) + reader.GetInt64(ordU));
            model.UsedVolume = Math.Round(usedGb, 2) + " GB";

            var remainingBytes = reader.GetInt64(ordTransferEnable) - (reader.GetInt64(ordD) + reader.GetInt64(ordU));
            if (remainingBytes <= 0)
                model.IsActive = 2;

            var remainingGb = Utility.ConvertByteToGB(remainingBytes);
            if (remainingGb <= 2)
                model.CanEdit = true;

            model.RemainingVolume = Math.Round(remainingGb, 2) + " GB";
            return model;
        }

        private void SetSubscribeViewBag(string email, string token, tbServers server)
        {
            ViewBag.Url = server.ServerAddress + "api/v1/client/subscribe?token=" + token;
            var username = email.Split('@')[1];
            ViewBag.LinkCreator = username.Contains(".") ? email.Split('.')[0] : username;

            var user = server.tbUsers.FirstOrDefault(p => p.Username == username);
            if (user != null)
                ViewBag.IsRenew = user.IsRenew;
        }

        private List<ReservedPackageViewModel> GetReservedPackages(string email)
        {
            return RepositoryOrders
                .Where(o => o.AccountName == email && o.OrderStatus == "FOR_RESERVE")
                .OrderBy(o => o.OrderDate)
                .ToList()
                .Select(o =>
                {
                    var planName = o.tbLinkUserAndPlans?.tbPlans?.Plan_Name;
                    if (string.IsNullOrEmpty(planName) && o.V2_Plan_ID.HasValue)
                    {
                        var plan = RepositoryPlans.Where(p => p.Plan_ID_V2 == o.V2_Plan_ID).FirstOrDefault();
                        planName = plan?.Plan_Name ?? "بسته رزرو";
                    }

                    return new ReservedPackageViewModel
                    {
                        PlanName = planName ?? "بسته رزرو",
                        VolumeGb = o.Traffic,
                        Months = o.Month,
                        ReservedDate = o.OrderDate?.ConvertDateTimeToShamsi4() ?? "-",
                        Status = "در انتظار فعال‌سازی"
                    };
                })
                .ToList();
        }
    }
}