using DataLayer.DomainModel;
using System;
using System.Security.Claims;
using System.Web;
using System.Web.Security;

namespace V2boardApi.Tools
{
    public static class AuthSession
    {
        public const int ExpireDays = 7;

        public static int ExpireMinutes => ExpireDays * 24 * 60;

        public static void SetLoginCookies(HttpResponseBase response, tbUsers user, string jwtToken)
        {
            var expire = DateTime.Now.AddDays(ExpireDays);
            var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;

            WriteCookie(response, "Role", user.Role.ToString(), expire, httpOnly: true);
            WriteCookie(response, "Token", jwtToken, expire, httpOnly: true);
            WriteCookie(response, "LoginUser", displayName, expire, httpOnly: false);

            FormsAuthentication.SetAuthCookie(user.Username, true);
        }

        public static void ClearLoginCookies(HttpResponseBase response)
        {
            ExpireCookie(response, "Token");
            ExpireCookie(response, "Role");
            ExpireCookie(response, "LoginUser");
            FormsAuthentication.SignOut();
        }

        public static ClaimsPrincipal GetValidPrincipal(HttpRequestBase request)
        {
            var tokenCookie = request?.Cookies["Token"];
            if (tokenCookie == null || string.IsNullOrEmpty(tokenCookie.Value))
                return null;

            return JwtToken.ValidateToken(tokenCookie.Value, JwtToken.GetSecretKey());
        }

        private static void WriteCookie(HttpResponseBase response, string name, string value, DateTime expire, bool httpOnly)
        {
            var cookie = new HttpCookie(name)
            {
                Value = value,
                Expires = expire,
                HttpOnly = httpOnly,
                Secure = false,
                SameSite = SameSiteMode.Lax
            };
            response.Cookies.Add(cookie);
        }

        private static void ExpireCookie(HttpResponseBase response, string name)
        {
            var cookie = new HttpCookie(name)
            {
                Expires = DateTime.Now.AddDays(-1),
                Value = string.Empty
            };
            response.Cookies.Add(cookie);
        }
    }
}
