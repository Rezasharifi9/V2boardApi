using Fido2NetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.Fido2Models
{
    public class RegisterCredentialModel
    {
        public string Id { get; set; } // شناسه Credential (Base64UrlEncoded)
        public string RawId { get; set; } // شناسه خام Credential (Base64UrlEncoded)
        public AuthenticatorAttestationRawResponse attestationObject { get; set; } // پاسخ WebAuthn
        public string clientDataJSON { get; set; } // داده‌های کلاینت
        public string username { get; set; } // نام کاربری (اختیاری، برای شناسایی کاربر)
    }

    public class WebAuthnCredential
    {
        public string Id { get; set; } // Credential ID
        public string RawId { get; set; } // Raw ID in Base64
        public string Type { get; set; } // Type of credential (e.g., public-key)
        public CredentialResponse Response { get; set; } // Response object
        public string AuthenticatorAttachment { get; set; } // Authenticator attachment
        public ClientExtensionResults ClientExtensionResults { get; set; } // Extensions (optional)
    }

    public class CredentialResponse
    {
        public string AttestationObject { get; set; } // Base64 of attestation object
        public string ClientDataJSON { get; set; } // Base64 of client data JSON
        public string AuthenticatorData { get; set; } // Base64 of authenticator data
    }

    public class ClientExtensionResults
    {
        // Add properties for any client extensions if needed; leave empty if not used
    }

}

