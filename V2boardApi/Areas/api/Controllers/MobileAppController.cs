using DataLayer.DomainModel;
using DataLayer.Repository;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using V2boardApi.Areas.api.Data.AppModels;
using V2boardApi.Tools;

namespace V2boardApi.Areas.api.Controllers
{
    public class MobileAppController : ApiController
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Entities db;
        private Repository<tbUsers> RepositoryUser { get; set; }
        private Repository<tbServers> RepositoryServer { get; set; }
        private Repository<tbPlans> RepositoryPlan { get; set; }
        private Repository<tbLogs> RepositoryLogs { get; set; }
        private Repository<tbOrders> RepositoryOrder { get; set; }
        private Repository<tbLinkUserAndPlans> RepositoryLinkUserAndPlan { get; set; }
        private Repository<tbLinks> RepositoryLinks { get; set; }
        private Repository<tbDepositWallet_Log> RepositoryDepositWallet { get; set; }
        private Repository<tbTelegramUsers> RepositoryTelegramUser { get; set; }
        private Repository<tbUserFactors> RepositoryFactor { get; set; }
        private Repository<tbServerGroups> RepositoryServerGroups { get; set; }
        public static tbServers Server { get; set; }
        public MobileAppController()
        {
            db = new Entities();
            RepositoryUser = new Repository<tbUsers>(db);
            RepositoryServer = new Repository<tbServers>(db);
            RepositoryPlan = new Repository<tbPlans>(db);
            RepositoryLogs = new Repository<tbLogs>(db);
            RepositoryLinkUserAndPlan = new Repository<tbLinkUserAndPlans>(db);
            RepositoryOrder = new Repository<tbOrders>();
            RepositoryLinks = new Repository<tbLinks>();
            RepositoryDepositWallet = new Repository<tbDepositWallet_Log>(db);
            RepositoryTelegramUser = new Repository<tbTelegramUsers>(db);
            RepositoryFactor = new Repository<tbUserFactors>(db);
            RepositoryServerGroups = new Repository<tbServerGroups>(db);

            Server = HttpRuntime.Cache["Server"] as tbServers;

        }
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> GetSubscriptionInfo()
        {
            var Token = Request.Headers.Authorization.ToString();
            if (Token == null)
            {
                return BadRequest("پارامتر توکن در هدر Authorization ارسال نشده است");
            }
            //var businessUserName = Request.Headers.Where(a => a.Key == "BusinessUserName").FirstOrDefault();
            //if (businessUserName.Value == null)
            //{
            //    return BadRequest("پارامتر توکن در هدر BusinessUserName ارسال نشده است");
            //}


            var Link = RepositoryLinks.Where(a => a.tbL_Token == Token).FirstOrDefault();
            if (Link == null)
            {
                return BadRequest("لینک اشتراک VPN یافت نشد؛");

            }

            SubscriptionInfo model = new SubscriptionInfo();

            using (MySqlEntities mySql = new MySqlEntities(Server.ConnectionString))
            {
                await mySql.OpenAsync();
                var Disc1 = new Dictionary<string, object>();
                Disc1.Add("@Email", Link.tbL_Email);
                var reader = await mySql.GetDataAsync("select * from v2_user where email=@Email", Disc1);

                while (await reader.ReadAsync())
                {
                    var ExpireTime = reader.GetBodyDefinition("expired_at");
                    var ex = Utility.ConvertSecondToDatetime(Convert.ToInt64(ExpireTime));
                    var UsedVolume = Utility.ConvertByteToGB(reader.GetDouble("d") + reader.GetDouble("u"));
                    var TotalVolume = reader.GetInt64("transfer_enable");


                    var TotalDays = (ex - DateTime.Now).TotalDays;


                    var AccountName = Link.tbL_Email.Split('@')[0];
                    if (AccountName.Contains('$'))
                    {
                        AccountName = AccountName.Split('$')[0];
                    }


                    model.TotalVolume = Utility.ConvertByteToGB(TotalVolume);
                    model.UsedVolume = Math.Round(Utility.ConvertByteToGB(UsedVolume),2);
                    model.BusinessName = Link.tbTelegramUsers.tbUsers.BussinesTitle;
                    model.BusinessUserName = Link.tbTelegramUsers.tbUsers.Username;
                    model.RemainingDays = (int)TotalDays;
                    model.SubscriptionName = AccountName;


                }

            }

            return Ok(model);

        }
    }
}
