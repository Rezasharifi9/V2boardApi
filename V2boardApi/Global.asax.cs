using DataLayer.DomainModel;
using DataLayer.Repository;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Timers;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Http;

namespace V2boardApi
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private Repository<tbServers> RepositoryServer { get; set; }
        private System.Timers.Timer Timer { get; set; }
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            GlobalConfiguration.Configure(WebApiConfig.Register);

            V2boardSiteEntities db = new V2boardSiteEntities();

            RepositoryServer = new Repository<tbServers>(db);
            Timer = new System.Timers.Timer();
            Timer.Elapsed += Timer_Elapsed;
            Timer.Interval = 604800000;
            Timer.Start();
            Timer_Elapsed(null, null);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                foreach (var Server in RepositoryServer.table.ToList())
                {

                    if (Server != null)
                    {
                        var formContent = new FormUrlEncodedContent(new[] {

                                new KeyValuePair<string, string>("email",Server.Email),
                                new KeyValuePair<string, string>("password",Server.Password)

                                 });

                        HttpClient httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Clear();
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

                        httpClient.BaseAddress = new Uri(Server.ServerAddress);
                        var res = httpClient.PostAsync(httpClient.BaseAddress + "api/v1/passport/auth/login", formContent);
                        if (res.Result.StatusCode == HttpStatusCode.OK)
                        {
                            var content = res.Result.Content.ReadAsStringAsync();
                            var js = JObject.Parse(content.Result.ToString());

                            var data = js["data"];

                            Server.Auth_Token = data["auth_data"].ToString();

                            RepositoryServer.Save();
                        }

                    }
                }

            }
            catch (Exception ex)
            {
            }
        }
    }

    
}
