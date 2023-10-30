using DataLayer.DomainModel;
using DataLayer.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Timers;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Cors;
using V2boardApi.Models;
using V2boardApi.Tools;

namespace V2boardApi.Controllers
{
    [EnableCors(origins: "*", "*", "*")]
    public class ServerController : ApiController
    {
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbPlans> RepositoryPlans { get; set; }
        private System.Timers.Timer Timer { get; set; }
        public ServerController()
        {
            RepositoryServer = new Repository<tbServers>();
            RepositoryPlans = new Repository<tbPlans>();
        }

        //[System.Web.Http.HttpPost]
        //public IHttpActionResult Login(string Email, string Password, string ServerAddress)
        //{
        //    try
        //    {

        //        var Server = RepositoryServer.table.Where(p => p.Status == true && p.Email == Email && p.Password == Password && p.ServerAddress == ServerAddress).FirstOrDefault();
        //        if (Server != null)
        //        {


        //            var formContent = new FormUrlEncodedContent(new[] {

        //                        new KeyValuePair<string, string>("email",Email),
        //                        new KeyValuePair<string, string>("password",Password)

        //                         });

        //            HttpClient httpClient = new HttpClient();
        //            httpClient.DefaultRequestHeaders.Clear();
        //            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

        //            httpClient.BaseAddress = new Uri(ServerAddress);
        //            var res = httpClient.PostAsync(httpClient.BaseAddress + "api/v1/passport/auth/login", formContent);
        //            if (res.Result.StatusCode == HttpStatusCode.OK)
        //            {
        //                var content = res.Result.Content.ReadAsStringAsync();
        //                var js = JObject.Parse(content.Result.ToString());

        //                var data = js["data"];

        //                Server.Auth_Token = data["auth_data"].ToString();

        //                RepositoryServer.Save();

        //                return Ok(new { status = true, result = "لاگین به سرور با موفقیت انجام شد" });
        //            }
        //            else
        //            {
        //                return Ok(new { status = false, result = "ارتباط با سرور پنل برقرار نشد لطفا اطلاعات ورودی را چک کنید" });
        //            }
        //        }
        //        else
        //        {
        //            return Ok(new { status = false, result = "ایمیل یا رمز عبور اشتباه است" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Ok(new { status = false, result = "خطا در برقراری ارتباط با سرور" });
        //    }
        //}
        ////[System.Web.Http.HttpPost]
        ////public IHttpActionResult Register(string Email, string Password, string ServerAddress)
        ////{
        ////    try
        ////    {
        ////        var pass = Password.ToSha256();
        ////        var Server = RepositoryServer.table.Where(p => p.Email == Email && p.Password == pass && p.ServerAddress == ServerAddress).FirstOrDefault();
        ////        if (Server == null)
        ////        {

        ////            var formContent = new FormUrlEncodedContent(new[] {

        ////                        new KeyValuePair<string, string>("email",Email),
        ////                        new KeyValuePair<string, string>("password",Password)

        ////                         });

        ////            HttpClient httpClient = new HttpClient();
        ////            httpClient.DefaultRequestHeaders.Clear();
        ////            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

        ////            httpClient.BaseAddress = new Uri(ServerAddress);
        ////            var res = httpClient.PostAsync(httpClient.BaseAddress + "api/v1/passport/auth/login", formContent);
        ////            if (res.Result.StatusCode == HttpStatusCode.OK)
        ////            {
        ////                var content = res.Result.Content.ReadAsStringAsync();
        ////                var js = JObject.Parse(content.Result.ToString());

        ////                var data = js["data"];

        ////                if (data["is_admin"].ToString() == "1")
        ////                {
        ////                    tbServers tbServers = new tbServers();
        ////                    tbServers.Email = Email;
        ////                    tbServers.Password = Password.ToSha256();
        ////                    tbServers.ServerAddress = ServerAddress;
        ////                    RepositoryServer.Insert(tbServers);
        ////                    RepositoryServer.Save();
        ////                    return Ok(new { status = "true", result = "سرور با موفقیت اضافه شد" });
        ////                }
        ////                else
        ////                {
        ////                    return Ok(new { status = "notaccess", result = "شما اجازه ثبت کردن سرور را ندارید" });
        ////                }

        ////            }
        ////            else
        ////            {
        ////                return Ok(new { status = "notconnect", result = "ارتباط با سرور پنل برقرار نشد لطفا اطلاعات ورودی را چک کنید" });
        ////            }
        ////        }
        ////        else
        ////        {
        ////            return Ok(new { status = "found", result = "این سرور از قبل در سیستم وجود دارد" });
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        return Ok(new { status = "error", result = "خطا در برقراری ارتباط با سرور" });
        ////    }
        ////}

        //[HttpGet]
        //public IHttpActionResult UpdatePlans()
        //{
        //    try
        //    {
        //        foreach (tbServers s in RepositoryServer.GetAll(p => p.Status == true).ToList())
        //        {
        //            HttpClient client = new HttpClient();
        //            client.BaseAddress = new Uri(s.ServerAddress);
        //            client.DefaultRequestHeaders.Clear();
        //            client.DefaultRequestHeaders.Add("Authorization", s.Auth_Token);
        //            var req = client.GetAsync(client.BaseAddress + "api/v1/" + s.AdminPath + "/plan/fetch");
        //            if (req.Result.StatusCode == System.Net.HttpStatusCode.OK)
        //            {
        //                var result = req.Result.Content.ReadAsStringAsync().Result;
        //                var js = JObject.Parse(result);
        //                var res = js["data"].ToString();
        //                res = res.Replace("\r\n", "");
        //                res = res.Replace(@"\", "");

        //                var plans = JsonConvert.DeserializeObject<List<Plans>>(res);

        //                foreach (var plan in plans)
        //                {
        //                    var pl = s.tbPlans.Where(p => p.Plan_ID_V2 == plan.id).FirstOrDefault();
        //                    if (pl == null)
        //                    {
        //                        tbPlans tbPlans = new tbPlans();
        //                        if (plan.quarter_price != null)
        //                        {
        //                            tbPlans.Price = Convert.ToInt32(plan.quarter_price);
        //                            tbPlans.CountDayes = 90;
        //                        }
        //                        else if (plan.month_price != null)
        //                        {
        //                            tbPlans.Price = Convert.ToInt32(plan.month_price);
        //                            tbPlans.CountDayes = 30;
        //                        }
        //                        else
        //                        {
        //                            tbPlans.CountDayes = 0;
        //                        }

        //                        tbPlans.Plan_ID_V2 = plan.id;
        //                        tbPlans.Plan_Name = plan.name;
        //                        tbPlans.FK_Server_ID = s.ServerID;
        //                        if (plan.show == 1)
        //                        {
        //                            tbPlans.Status = true;
        //                        }
        //                        else
        //                        {
        //                            tbPlans.Status = false;
        //                        }
        //                        tbPlans.Plan_Des = plan.content;
        //                        tbPlans.PlanVolume = Convert.ToInt32(plan.transfer_enable);
        //                        RepositoryPlans.Insert(tbPlans);
        //                    }
        //                    else
        //                    {

        //                        if (plan.quarter_price != null)
        //                        {
        //                            pl.Price = Convert.ToInt32(plan.quarter_price) / 100;

        //                            pl.CountDayes = 90;
        //                        }
        //                        else if (plan.month_price != null)
        //                        {
        //                            pl.Price = Convert.ToInt32(plan.month_price) / 100;
        //                            pl.CountDayes = 30;
        //                        }
        //                        else
        //                        {
        //                            pl.CountDayes = 0;
        //                        }

        //                        if (plan.show == 1)
        //                        {
        //                            pl.Status = true;
        //                        }
        //                        else
        //                        {
        //                            pl.Status = false;
        //                        }

        //                        pl.Plan_ID_V2 = plan.id;
        //                        pl.Plan_Name = plan.name;
        //                        pl.FK_Server_ID = s.ServerID;
        //                        if (plan.show == 1)
        //                        {
        //                            pl.Status = true;
        //                        }
        //                        pl.Plan_Des = plan.content;
        //                        pl.PlanVolume = Convert.ToInt32(plan.transfer_enable);
        //                    }
        //                }


        //            }
        //            RepositoryPlans.Save();
        //            RepositoryServer.Save();

        //        }
        //        return Ok(new { status = true, result = "پلن ها با موفقیت آپدیت شد" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Ok(new { status = false, result = "آپدیت با خطا مواجه شد" });
        //    }

        //}
    }
}
