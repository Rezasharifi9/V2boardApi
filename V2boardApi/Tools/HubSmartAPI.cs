using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using static System.Net.WebRequestMethods;

namespace V2boardApi.Tools
{
    public class HubSmartAPI
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public HubSmartAPI(string api_key)
        {
            ApiKey = api_key;
            BaseUrl = "https://apipay24.top/api/v1/PaymentRequest/";
        }
        /// <summary>
        /// ساخت تراکنش
        /// </summary>
        /// <param name="requestNewPay"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ResponseNewPay> NewPay(RequestNewPay requestNewPay)
        {
            requestNewPay.apikey = ApiKey;
            requestNewPay.currency = "irt";
            requestNewPay.wallet = "TCYuvw16NGomffJN3yQdjuH5Rg7r7fPwx9";
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);
            var content = new FormUrlEncodedContent(Utility.ToDictionary(requestNewPay));
            
            var resp = await client.PostAsync(BaseUrl + "BuyTron/", content);
            if(resp.IsSuccessStatusCode)
            {
                var result = await resp.Content.ReadAsStringAsync();

                var model = JsonConvert.DeserializeObject<ResponseNewPay>(result);
                return model;

            }
            else
            {
                throw new Exception("درخواست از سمت HubSmart پذیرفته نشده کد خطا : " + resp.StatusCode );
            }

        }
        /// <summary>
        /// تائید تراکنش
        /// </summary>
        /// <param name="requestVerify"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ResponseVerifyTransaction> Verify(RequestVerifyTransaction requestVerify)
        {
            requestVerify.apikey = ApiKey;


            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);

            var content = new FormUrlEncodedContent(Utility.ToDictionary(requestVerify));
            var resp = await client.PostAsync(BaseUrl + "Verify/", content);
            if (resp.IsSuccessStatusCode)
            {
                var result = await resp.Content.ReadAsStringAsync();

                var model = JsonConvert.DeserializeObject<ResponseVerifyTransaction>(result);
                return model;

            }
            else
            {
                throw new Exception("درخواست از سمت HubSmart پذیرفته نشده کد خطا : " + resp.StatusCode);
            }

        }
        /// <summary>
        /// جزئیات تراکنش
        /// </summary>
        /// <param name="requestPayDetails"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ResponsePayDetails> GetDetails(RequestPayDetails requestPayDetails)
        {
            requestPayDetails.apikey = ApiKey;


            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);

            var content = new FormUrlEncodedContent(Utility.ToDictionary(requestPayDetails));
            var resp = await client.PostAsync(BaseUrl + "GetDetails", content);
            if (resp.IsSuccessStatusCode)
            {
                var result = await resp.Content.ReadAsStringAsync();

                var model = JsonConvert.DeserializeObject<ResponsePayDetails>(result);
                return model;

            }
            else
            {
                throw new Exception("درخواست از سمت HubSmart پذیرفته نشده کد خطا : " + resp.StatusCode);
            }

        }

    }


    #region مدل مربوط ساخت یک تراکنش

    public class RequestNewPay
    {
        public string apikey { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
        public string callback_url { get; set; }
        public string wallet { get; set; }
        public string order_id { get; set; }


    }

    public class ResponseNewPay
    {
        public bool status { get; set; }
        public string message { get; set; }
        public ResponseNewPayData data { get; set; }

        public class ResponseNewPayData
        {
            public string token { get; set; }
            public string payment_url { get; set; }
            public string total_pay { get; set; }
            public string trx_amount { get; set; }
        }
    }

    #endregion

    #region مدل مربوط به تائید پرداخت

    public class RequestVerifyTransaction
    {
        public string apikey { get; set; }
        public string token { get; set; }

    }
    public class ResponseVerifyTransaction
    {
        public bool status { get; set; }
        public string message { get; set; }
        public ResponseVerifyTransactionData data { get; set; }

        public class ResponseVerifyTransactionData
        {
            public string token { get; set; }
            public string payment_url { get; set; }
            public string total_pay { get; set; }
            public string trx_amount { get; set; }
        }
    }

    #endregion

    #region مدل مربوط به دریافت جزئیات تراکنش

    public class RequestPayDetails
    {
        public string apikey { get; set; }
        public string token { get; set; }
    }

    public class ResponsePayDetails
    {
        public bool status { get; set; }
        public string message { set; get; }
        public ResponsePayDetailsData data { get; set; }


        public class ResponsePayDetailsData
        {
            public string order_id { get; set; }
            public string total_pay { get; set; }
            public string trx_amount { get; set; }

            public string transfer_completed { get; set; }

            public string transfer_txid { get; set; }
            public DateTime transfer_date { get; set; }
        }
    }

    #endregion
}