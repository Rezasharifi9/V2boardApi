using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace V2boardApi.PaymentMethods
{
    public class PlisioPay
    {
        private readonly string _apiKey;
        private readonly string _callbackurl;
        private readonly HttpClient _httpClient;

        public PlisioPay(string baseUrl,string apiKey, string callbackurl)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("apiKey is required");

            _apiKey = apiKey;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
            _callbackurl = callbackurl;
        }

        public async Task<PlisioInvoiceResponse> CreateInvoiceAsync(
            decimal amount,
            string currency, // EUR, USD
            string orderName,
            string orderNumber
        )
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            query["api_key"] = _apiKey;
            query["source_amount"] = amount.ToString(CultureInfo.InvariantCulture);
            query["source_currency"] = currency;
            query["order_name"] = orderName;
            query["order_number"] = orderNumber;
            query["callback_url"] = _callbackurl;
            query["success_callback_url "] = _callbackurl;
            query["email"] = "darkbazsp@gmail.com";

            var url = "api/v1/invoices/new?" + query.ToString();

            var response = await _httpClient.GetAsync(url);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Plisio error: " + json);

            var result = JsonConvert.DeserializeObject<PlisioInvoiceResponse>(json);

            if (result.status != "success")
                throw new Exception("Plisio API error");

            return result;
        }

    }
    public class PlisioInvoiceResponse
    {
        public string status { get; set; }
        public PlisioInvoiceData data { get; set; }
    }

    public class PlisioInvoiceData
    {
        public string txn_id { get; set; }

        public string invoice_url { get; set; }

        public string source_amount { get; set; }

        public string source_currency { get; set; }

        public string amount { get; set; }

        public string currency { get; set; }
        public string invoice_total_sum { get; set; }
        public string verify_hash { get; set; }
    }
}


