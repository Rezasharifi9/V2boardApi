using DataLayer.DomainModel;
using DataLayer.Repository;
using MihaZupan;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot.Types;

namespace V2boardApi.PaymentMethods
{
    public class TetraPay
    {
        private Repository<tbSocks5> Socks5Repository { get; set; }
        public HttpClient HttpClient { get; set; }
        public TetraPay(string BaseUrl)
        {
            
            Socks5Repository = new Repository<tbSocks5>();

            var AcitveProxy = Socks5Repository.Where(a=> a.IsActive == true).FirstOrDefault();
            if(AcitveProxy!=null)
            {
                var proxy = new HttpToSocks5Proxy(AcitveProxy.tbsck_ip, AcitveProxy.tbsck_port.Value, AcitveProxy.tbsck_username, AcitveProxy.tbsck_password);

                var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true
                };

                HttpClient = new HttpClient(handler);
            }
            else
            {
                HttpClient = new HttpClient();
            }

            HttpClient.BaseAddress = new Uri(BaseUrl);

        }

        public async Task<ResponseCreateOrderModel> CreateOrder(RequestCreateOrderModel request)
        {
            var serial = JsonConvert.SerializeObject(request);
            var Content = new StringContent(serial, System.Text.Encoding.UTF8);

            var res = await HttpClient.PostAsync(HttpClient.BaseAddress + "api/create_order", Content);
            if (res.IsSuccessStatusCode)
            {
                var result = res.Content.ReadAsStringAsync();
                var payment = JsonConvert.DeserializeObject<ResponseCreateOrderModel>(result.Result.ToString());

                return payment;
            }
            else
            {
                var resp = new ResponseCreateOrderModel();
                resp.Status = "500";
                return resp;
            }

        }

        public async Task<ResponseVerifyModel> VerifyRequest(RequestVerifyModel request)
        {
            
            var serial = JsonConvert.SerializeObject(request);
            var Content = new StringContent(serial, System.Text.Encoding.UTF8);

            var res = await HttpClient.PostAsync(HttpClient.BaseAddress + "/api/verify", Content);
            if (res.IsSuccessStatusCode)
            {
                var result = res.Content.ReadAsStringAsync();
                var payment = JsonConvert.DeserializeObject<ResponseVerifyModel>(result.Result.ToString());

                return payment;
            }
            else
            {
                var result = res.Content.ReadAsStringAsync();
                var resp = JsonConvert.DeserializeObject<ResponseVerifyModel>(result.Result.ToString());
                return resp;
            }

        }

        




        public class RequestCreateOrderModel
        {
            public string ApiKey { get; set; }
            public string Hash_id { get; set; }
            public int Amount { get; set; }
            public string Description { get; set; }
            public string Email { get; set; }
            public string Mobile { get; set; }
            public string CallbackURL { get; set; }
        }
        public class ResponseCreateOrderModel
        {
            public string Status { get; set; }
            public string Authority { get; set; }
            public string payment_url_bot { get; set; }
            public string payment_url_web { get; set; }
            public string tracking_id { get; set; }

        }

        public class RequestVerifyModel
        {
            public string ApiKey { get; set;}
            public string authority { get; set;}
        }

        public class ResponseVerifyModel
        {
            public string status { get; set; }
            public string hash_id { get; set; }
            public string authority { get; set; }
            public string message { get; set; }
        }
    }
}