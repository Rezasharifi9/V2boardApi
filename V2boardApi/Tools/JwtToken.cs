using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace V2boardApi.Tools
{
    public static class JwtToken
    {
        public static string GenerateToken(string username, string role, string secretKey, int expireMinutes = 30)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var claims = new[]
            {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role) // اضافه کردن نقش کاربر به توکن
             };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(expireMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static ClaimsPrincipal ValidateToken(string token, string secretKey)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal; // اگر توکن معتبر باشد، ClaimsPrincipal بازمی‌گردد
            }
            catch
            {
                return null; // اگر توکن نامعتبر باشد، null برمی‌گردد
            }
            
        }

        public static string GetUserRole(ClaimsPrincipal principal)
        {
            return principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        }
        public static string GetUserRole()
        {
            var Token = HttpContext.Current.Request.Cookies["Token"].Value;

            var principal = ValidateToken(Token, GetSecretKey());
            return principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        }

        public static string GetSecretKey()
        {
            string secretKey = ConfigurationManager.AppSettings["JwtSecretKey"];
            return secretKey;
        }
    }
}