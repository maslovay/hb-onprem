using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UserOperations.Services;
using Newtonsoft.Json;
using HBData;
using HBData.Models;
using HBData.Repository;
using System.Security.Cryptography;

namespace UserOperations.Services
{
    public class LoginService : ILoginService
    {
        private readonly IGenericRepository _repository;
        private readonly IConfiguration _config;
        private readonly RecordsContext _context;
        
        public LoginService(IGenericRepository repository, IConfiguration config, RecordsContext context)
        {
            _repository = repository;
            _config = config;
            _context = context;
        }

        public string GeneratePasswordHash(string password)
        {
            var crypt = new SHA256Managed();
            var passwordHashSalt = _config["Tokens:Hash_salt"];
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(password + passwordHashSalt));
            return System.Convert.ToBase64String(crypto);
        }

        public bool CheckUserLogin(string login, string password)
        {
            login = login.ToUpper();
            password = GeneratePasswordHash(password);
            return _context.ApplicationUsers.Count(p => p.NormalizedEmail == login && p.PasswordHash == password) == 1;
        }

        public string CreateTokenForUser(ApplicationUser user, bool remember)
        {
            try
            {
                var roleInfo = _repository.GetWithIncludeOne<ApplicationUserRole>(p => p.UserId == user.Id, link => link.Role); 
                var role = roleInfo.Role.Name;            

                if (user.StatusId == 3)
                {
                    var claims = new[]
                    {       
                        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim("applicationUserId", user.Id.ToString()),
                        new Claim("applicationUserName", user.FullName),
                        new Claim("companyName", user.Company.CompanyName),
                        new Claim("companyId", user.CompanyId.ToString()),
                        new Claim("corporationId", user.Company.CorporationId.ToString()),
                        new Claim("languageCode", user.Company.LanguageId.ToString()),
                        new Claim("role", role),
                    };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken(_config["Tokens:Issuer"],
                        _config["Tokens:Issuer"],
                        claims,
                        expires: remember ? DateTime.Now.AddDays(31) : DateTime.Now.AddDays(1),
                        signingCredentials: creds);

                    var tokenenc = new JwtSecurityTokenHandler().WriteToken(token);

                    return tokenenc;
                }
                else
                {
                    return "User inactive";
                }

            }
            catch (Exception e)
            {
                return $"User not exist or internal error {e}";
            }
        }

        // <summary>
        /// Parse JWT token 
        /// </summary>
        /// <param name="token">JWT token in request</param>
        /// <returns></returns>
        public Dictionary<string, string> GetDataFromToken(string token, string sign = null)
        {
            if (sign == "" || sign == null)
                sign = _config["Tokens:Key"];
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
                else
                    return null;
            }
            catch (Exception e)
            {
                return null;
            }
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
                SecurityToken tk;
                var principial = handler.ValidateToken(token, new TokenValidationParameters()
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sign)),
                    ValidIssuer = _config["Tokens:Issuer"],
                    ValidateAudience = false
                }, out tk);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public bool CheckAccess(Dictionary<string, string> claims, List<string> companyIds)
        {
            //manager with one company in request
            var managerRoles = _config["Roles:ManagerRoles"].Split(',');
            if (companyIds.Contains(claims["companyId"]) 
                && companyIds.Count() == 1 
                && managerRoles.Contains(claims["role"]))
                return true;
            //Supervisor or Admin with one company in request
            var supervisorRoles = _config["Roles:SupervisorRoles"].Split(',');
            if (companyIds.Contains(claims["companyId"]) 
                && companyIds.Count() == 1 && supervisorRoles.Contains(claims["role"]) 
                && _context.Companys.Where(p => companyIds.Contains(p.CompanyId.ToString())).All(p => p.CorporationId.ToString()  == claims["corporationId"]))
                return true;
            //reject if non succeded request
            return false;
        }

        public bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _repository.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
