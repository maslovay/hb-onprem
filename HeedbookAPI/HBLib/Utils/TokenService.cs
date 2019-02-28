using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace HBLib.Utils
{
    public class TokenService
    {
        /// <summary>
        ///     Validate token function
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <param name="sign"></param>
        /// <returns></returns>
        public static bool CheckToken(string token, string sign = "")
        {
            if (sign == "" || sign == null)
                sign = EnvVar.Get("TOKEN_SIGN_KEY");
            try
            {
                var handler = new JwtSecurityTokenHandler();
                SecurityToken tk;
                var principial = handler.ValidateToken(token, new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sign)),
                    ValidIssuer = EnvVar.Get("TOKEN_ISSUER"),
                    ValidateAudience = false
                }, out tk);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Validates JWT token
        /// </summary>
        /// <param name="token">JWT token in request</param>
        /// <returns></returns>
        public static Dictionary<string, string> GetDataFromToken(string token, string sign = null)
        {
            try
            {
                var pureToken = token.Split(' ')[1];
                if (CheckToken(pureToken, sign))
                {
                    var jwt = new JwtSecurityToken(pureToken);
                    Dictionary<string, string> claims;
                    claims = jwt.Payload.ToDictionary(key => key.Key.ToString(), value => value.Value.ToString());
                    return claims;
                }

                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        ///     Validates JWT access to companies data
        /// </summary>
        /* 
        public static bool CheckAccess(Dictionary<string, string> claims, List<string> companyIds)
        {
            //manager with one company in request
            var managerRoles = EnvVar.Get("ManagerRoles").Split(',');
            if (companyIds.Contains(claims["companyId"]) && companyIds.Count() == 1 &&
                managerRoles.Contains(claims["role"]))
                return true;
            //Supervisor or Admin with one company in request
            var supervisorRoles = EnvVar.Get("SupervisorRoles").Split(',');
            if (companyIds.Contains(claims["companyId"]) && companyIds.Count() == 1 &&
                supervisorRoles.Contains(claims["role"]) && HeedbookMessengerStatic.Context().Companys
                    .Where(p => companyIds.Contains(p.CompanyId.ToString()))
                    .All(p => p.CorporationId.ToString() == claims["corporationId"]))
                return true;
            //reject if non succeded request
            return false;
        }
        */
    }
}