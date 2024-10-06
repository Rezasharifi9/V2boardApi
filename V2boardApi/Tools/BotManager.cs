using DataLayer.DomainModel;
using DeviceDetectorNET.Class;
using MihaZupan;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Services.Description;
using Telegram.Bot;
using static System.Net.WebRequestMethods;

namespace V2boardApi.Tools
{
    public static class BotManager
    {
        public static tbServers Server;
        public static void AddBot(string name, string token)
        {
            var Sock = new tbSocks5();
            using (Entities db = new Entities())
            {
                var Sok = db.tbSocks5.Where(s => s.Active == true).FirstOrDefault();
                if (Sok != null)
                {
                    Sock = Sok;
                }
                else
                {
                    Sock = null;
                }
            }

            TelegramBotClient botClient;

            if (Sock != null)
            {
                // آدرس پروکسی و پورت
                var proxy = new HttpToSocks5Proxy(Sock.HostName, Sock.Port, username: Sock.Username, password: Sock.Password);

                // تنظیمات TelegramBotClient با پروکسی

                HttpClient http = new HttpClient(new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true
                });
                botClient = new TelegramBotClient(token, http);
            }
            else
            {
                botClient = new TelegramBotClient(token);
            }
            


            
            var botInfo = new BotInfo();
            botInfo.Name = name;
            botInfo.Token = token;
            botInfo.Started = false;
            botInfo.Client = botClient;
            var res = HttpRuntime.Cache[name];
            if (res == null)
            {
                HttpRuntime.Cache.Insert(name, botInfo, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);
            }
            else
            {
                HttpRuntime.Cache.Remove(name);

                HttpRuntime.Cache.Insert(name, botInfo, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);
            }

        }

        public static BotInfo GetBot(string name)
        {
            BotInfo myObject = HttpRuntime.Cache[name] as BotInfo;

            if (myObject != null)
            {
                return myObject;
            }
            return null;
        }

        public static bool StartBot(string name)
        {
            BotInfo myObject = HttpRuntime.Cache[name] as BotInfo;

            if (myObject != null)
            {
                myObject.Started = true;

                HttpRuntime.Cache.Remove(name);

                HttpRuntime.Cache.Insert(name, myObject, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);
            }
            return true;
        }

        public static bool StopBot(string name)
        {
            BotInfo myObject = HttpRuntime.Cache[name] as BotInfo;

            if (myObject != null)
            {
                myObject.Started = false;

                HttpRuntime.Cache.Remove(name);

                HttpRuntime.Cache.Insert(name, myObject, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);
            }
            return true;
        }

        public static bool SetNewToken(string name, string Token)
        {
            BotInfo myObject = HttpRuntime.Cache[name] as BotInfo;

            if (myObject != null)
            {
                myObject.Token = Token;

                HttpRuntime.Cache.Remove(name);

                HttpRuntime.Cache.Insert(name, myObject, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);
            }
            return true;
        }
    }

    public class BotService
    {

        public async Task<bool> Register(string name)
        {

            //var method = "http";
            //if (HttpContext.Current.Request.Url.AbsoluteUri.Contains("https"))
            //{
            //    method = "https";
            //}

            //var url = method + "://" + HttpContext.Current.Request.Url.Authority;
            //var url = "https://79f1-2a09-bac1-1e40-60-00-1d6-152.ngrok-free.app";


            var url = "https://";
            var Server = HttpRuntime.Cache["Server"] as tbServers;
            if (Server != null)
            {
                url += Server.BotbaseAddress;
            }
            var bot = BotManager.GetBot(name);
            if (bot == null)
            {
                throw new ArgumentException($"Bot with name {name} not found.");
            }

            var botClient = bot.Client;

            await botClient.DeleteWebhookAsync(true).ConfigureAwait(false);

            var webhookUrl = $"{url}/Bot/Update/?botName={name}";
            await botClient.SetWebhookAsync(webhookUrl);

            return true;
        }
    }



    public class BotInfo
    {
        private static readonly object _lock = new object();
        public string Token { get; set; }
        public string Name { get; set; }
        public TelegramBotClient Client { get; set; }
        public bool Started { get; set; }
    }
}