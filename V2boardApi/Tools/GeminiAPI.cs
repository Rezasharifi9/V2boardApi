using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static System.Net.WebRequestMethods;
using System.Web.Mvc;

namespace V2boardApi.Tools
{
    public static class GeminiAPI
    {
        public class ResponseModel
        {
            public HttpStatusCode StatusCode { get; set; }
            public string Message { get; set; }
            public bool ok { get; set; }
        }

        public static async Task<ResponseModel> Generate(string prompt)
        {
            ResponseModel responseModel = new ResponseModel();

            if (string.IsNullOrWhiteSpace(prompt))
            {
                responseModel.ok = false;
                responseModel.Message = "Prompt خالی است";
                return responseModel;
            }

            HttpClient _http = new HttpClient();


            // اطمینان از TLS1.2 (روی بعضی سرورها لازم می‌شود)
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];

            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

            var payload = new
            {
                contents = new[]
                {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
                // می‌توانید generationConfig / safetySettings هم اضافه کنید
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                req.Headers.Add("x-goog-api-key", apiKey);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var res = await _http.SendAsync(req))
                {
                    var body = await res.Content.ReadAsStringAsync();

                    if (!res.IsSuccessStatusCode)
                    {
                        responseModel.ok = false;
                        responseModel.StatusCode = res.StatusCode;
                        responseModel.Message = body;
                        return responseModel;
                    }
                        

                    // پاسخ معمولاً در candidates[0].content.parts[0].text است
                    var jo = JObject.Parse(body);
                    var text = (string)jo["candidates"]?[0]?["content"]?["parts"]?[0]?["text"];

                    responseModel.ok = false;
                    responseModel.StatusCode = res.StatusCode;
                    responseModel.Message = text;
                    return responseModel;
                }
            }
        }
    }
}