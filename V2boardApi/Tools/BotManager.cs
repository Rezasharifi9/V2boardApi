using DataLayer.DomainModel;
using DeviceDetectorNET.Class;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot;
using static System.Net.WebRequestMethods;

namespace V2boardApi.Tools
{
    public static class BotManager
    {
        public static Dictionary<string, BotInfo> Bots = new Dictionary<string, BotInfo>();

        public static tbServers Server;
        public static void AddBot(string name, string token)
        {
            if (!Bots.ContainsKey(name))
            {
                var bot = new TelegramBotClient(token);
                Bots.Add(name, new BotInfo { Name = name, Token = token, Client = bot, Started = false });
            }
        }

        public static BotInfo GetBot(string name)
        {
            if (Bots.ContainsKey(name))
            {
                return Bots[name];
            }
            return null;
        }

        public static IEnumerable<BotInfo> GetAllBots()
        {
            return Bots.Values;
        }

    }

    public class BotService
    {

        public async Task<bool> Register(string name)
        {

            var method = "http";
            if (HttpContext.Current.Request.Url.AbsoluteUri.Contains("https"))
            {
                method = "https";
            }

            var url = method + "://" + HttpContext.Current.Request.Url.Authority;
            //var url = "https://aa86-45-76-44-165.ngrok-free.app";

            if (!BotManager.Bots.ContainsKey(name))
            {
                throw new ArgumentException($"Bot with name {name} not found.");
            }

            var botClient = BotManager.Bots[name].Client;

            await botClient.DeleteWebhookAsync(false);

            var webhookUrl = $"{url}/Bot/Update/?botName={BotManager.Bots[name].Name}";
            await botClient.SetWebhookAsync(webhookUrl);

            return true;
        }
    }



    public class BotInfo
    {
        public string Token { get; set; }
        public string Name { get; set; }
        public TelegramBotClient Client { get; set; }
        public bool Started { get; set; }
    }
}