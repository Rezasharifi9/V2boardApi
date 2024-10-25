using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.api.Data.ApiModels
{
    public class AddFirebaseTokenModel
    {
        public string sub_Token { get; set; }
        public string firebase_token { get; set; }
    }
}