using Fido2NetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.Fido2Models
{
    public class AuthenticatorAttestationRawResponse2
    {
        public class ResponseData
        {
            public string AttestationObject { get; set; }

            public string ClientDataJson { get; set; }
        }

        public string Id { get; set; }

        public string RawId { get; set; }

        public string Type { get; set; }

        public ResponseData Response { get; set; }
    }
}