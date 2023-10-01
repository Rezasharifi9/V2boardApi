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

namespace V2boardApi.Controllers
{
    public class ClientController : Controller
    {
        private Repository<tbServers> RepositoryServer { get; set; }
        private V2boardSiteEntities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbLinkUserAndPlans> RepositoryLinkUserAndPlan { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        public ClientController()
        {
            db = new V2boardSiteEntities();
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryUser = new Repository<tbUsers>(db);
        }

        public ActionResult subscribe(string token = "26948e1cfb866fbd9b80ac3626f7f6f4")
        {
            var UserAgent = Request.UserAgent.ToLower();

            if (UserAgent.Contains("surfboard") || UserAgent.Contains("v2rayng") || UserAgent.Contains("v2box") || UserAgent.Contains("foxray") || UserAgent.Contains("fair") || UserAgent.Contains("str") || UserAgent.Contains("shadow") || UserAgent.Contains("v2rayn"))
            {
                var server = RepositoryServer.table.Where(p => p.SubAddress.Contains(Request.Url.Host)).FirstOrDefault();
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
                var server = RepositoryServer.table.Where(p => p.SubAddress.Contains(Request.Url.Host)).FirstOrDefault();


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
                                if (item2.expired_at != null)
                                {
                                    var ex = Utility.ConvertSecondToDatetime((long)item2.expired_at);
                                    getUserData.ExpireDate = Utility.ConvertDateTimeToShamsi(ex);
                                    getUserData.DaysLeft = Utility.CalculateLeftDayes(ex);

                                    if (ex <= DateTime.Today)
                                    {
                                        getUserData.IsActive = "پایان تاریخ اشتراک";
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
                                getUserData.RemainingVolume = Math.Round(d, 2) + " GB";
                                var name = item2.email.Split('@')[1];
                                var User = RepositoryUser.table.Where(p => p.Username == name).FirstOrDefault();
                                if (User != null)
                                {
                                    ViewBag.TelegramID = User.TelegramID;
                                    ViewBag.Title = User.BussinesTitle;
                                }


                                ViewBag.Url = server.ServerAddress + "api/v1/" + "client/subscribe?token=" + token;
                                return View(getUserData);
                            }
                        }

                    }
                }


                return PartialView();

            }


            return null;

        }
    }
}