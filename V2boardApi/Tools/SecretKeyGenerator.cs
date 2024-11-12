using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace V2boardApi.Tools
{
	public static class SecretKeyGenerator
	{
		public static string GenerateSecretKey(int size = 32)
		{
			using (var rng = new RNGCryptoServiceProvider())
			{
				var key = new byte[size];
				rng.GetBytes(key);
				return Convert.ToBase64String(key);
			}
		}
	}
}