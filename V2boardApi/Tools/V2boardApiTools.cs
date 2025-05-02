using DataLayer.DomainModel;
using Newtonsoft.Json;
using NLog.LayoutRenderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace V2boardApi.Tools
{
    public static class V2boardApiTools
    {
        private static tbServers Server;
        private static string BaseUrl;
        private static string Token;
        private static bool IsInit;
        public static void init()
        {
            Server = HttpRuntime.Cache["Server"] as tbServers;

            BaseUrl = Server.ServerAddress + "/api/v1/server/";

            Token = Server.ApiToken_V2board;
            IsInit = true;
        }


        public static async Task<SubInfo> GetSubOnlineDetails(int user_id)
        {
            try
            {
                if (IsInit)
                {
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(BaseUrl);
                    var response = await client.GetAsync(client.BaseAddress + "info/alivesingle?token=" + Token + "&" + "user_id=" + user_id);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();

                        var model = JsonConvert.DeserializeObject<SubInfo>(result);

                        return model;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    throw new ArgumentException("Init Function is not run");
                }
            }
            catch
            {
                return null;
            }

        }


        public static async Task<List<SubInfo>> GetSubOnlineList()
        {
            if (IsInit)
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(BaseUrl);
                var response = await client.GetAsync(client.BaseAddress + "info/alivelist?token=" + Token);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    var model = JsonConvert.DeserializeObject<List<SubInfo>>(result);

                    return model;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                throw new ArgumentException("Init Function is not run");
            }

        }


    }

    public class SubInfo
    {
        public int user_id { get; set; }
        public int online_count { get; set; }
        public int device_limit { get; set; }
        public bool exceeded { get; set; }
    }

    public class ListSubInfo
    {
        List<SubInfo> users { get; set; }
    }
}