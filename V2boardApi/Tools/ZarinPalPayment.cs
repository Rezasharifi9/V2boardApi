using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace V2boardApi.Tools
{
    public class ZarinPalPayment
    {
        public string ApiKey { get; set; }
        public string KeyZarin { get; set; }
        public string BaseUrl { get; set; }
        public ZarinPalPayment(string api_key,string Key)
        {
            ApiKey = api_key;
            KeyZarin = Key;
            BaseUrl = "https://payment.zarinpal.com/pg/v4/payment/";
        }

        public async Task<PaymentResponseModel> CreatePayment(PaymentRequestModel requestNewPay)
        {
            requestNewPay.merchant_id = ApiKey;
            requestNewPay.description = KeyZarin;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);
            var content = new StringContent(JsonConvert.SerializeObject(requestNewPay),System.Text.Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(BaseUrl + "request.json", content);
            if (resp.IsSuccessStatusCode)
            {
                var result = await resp.Content.ReadAsStringAsync();

                var model = JsonConvert.DeserializeObject<PaymentResponseModel>(result);
                
                return model;

            }
            else
            {
                throw new Exception("درخواست از سمت HubSmart پذیرفته نشده کد خطا : " + resp.StatusCode);
            }

        }


        public class PaymentRequestModel
        {
            public string merchant_id { get; set; }
            public int amount { get; set; }
            public string callback_url { get; set; }
            public string description { get; set; }

        }
        public class PaymentResponseModel
        {
            public Data data { get; set; }  
            public class Data
            {
                public string authority { get; set; }
                public string fee { get; set; }
            }

        }
    }
}