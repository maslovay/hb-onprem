using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using HBData.Repository;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace HBLib.Utils
{
    public class CheckTokenService
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CheckTokenService(
            IConfiguration config,
            IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }
        public string GeneratePasswordHash(string password)
        {
            var crypt = new SHA256Managed();
            var passwordHashSalt = _config["Tokens:Hash_salt"];
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(password + passwordHashSalt));
            return System.Convert.ToBase64String(crypto);
        }

        public bool GetDataFromToken(string token, out Dictionary<string, string> claims, string sign = null)
        {
            claims = null;
            if (sign == "" || sign == null)
                sign = _config["Tokens:Key"];
            var pureToken = token.Split(' ')[1];
            if (CheckToken(pureToken, sign))
            {
                var jwt = new JwtSecurityToken(pureToken);
                claims = jwt.Payload.ToDictionary(key => key.Key.ToString(), value => value.Value.ToString());
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Validate token function
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <param name="sign"></param>
        /// <returns></returns>
        public bool CheckToken(string token, string sign = "")
        {
            if (sign == "" || sign == null)
                sign = _config["Tokens:Key"];
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principial = handler.ValidateToken(token, new TokenValidationParameters()
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sign)),
                    ValidIssuer = _config["Tokens:Issuer"],
                    ValidateAudience = false
                }, out SecurityToken tk);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public string GetCurrentRoleName()
        {
            return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        }

        public bool IsAdmin()
        {
            return GetCurrentRoleName().ToUpper() == "ADMIN" ? true : false;
        }
    }
}
