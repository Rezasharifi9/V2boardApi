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

namespace V2boardApi.Areas.api.Controllers
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

        [HttpGet]
        public IHttpActionResult UpdatePlans()
        {
            try
            {
                foreach (tbServers s in RepositoryServer.GetAll(p => p.Status == true).ToList())
                {


                    MySqlEntities mySqlEntities = new MySqlEntities(s.ConnectionString);
                    mySqlEntities.Open();
                    var reader = mySqlEntities.GetData("select * from v2_plan");

                    while (reader.Read())
                    {
                        var show = reader.GetByte("show");
                        var id = reader.GetInt32("id");

                        var planD = RepositoryPlans.table.Where(p => p.Plan_ID_V2 == id).FirstOrDefault();
                        if (planD != null)
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

                            planD.Status = Convert.ToBoolean(reader.GetByte("show"));
                            planD.FK_Server_ID = s.ServerID;
                        }
                        else
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

                            plan.Status = true;
                            plan.FK_Server_ID = s.ServerID;
                            RepositoryPlans.Insert(plan);
                        }
                    }

                }
                RepositoryServer.Save();
                RepositoryPlans.Save();
                return Ok(new { status = true, result = "پلن ها با موفقیت آپدیت شد" });
            }
            catch (Exception ex)
            {
                return Ok(new { status = false, result = "آپدیت با خطا مواجه شد" });
            }

        }
    }
}
