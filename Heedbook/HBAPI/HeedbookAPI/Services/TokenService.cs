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

namespace UserOperations.Services
{
    public class TokenService : ITokenService
    {
        private readonly IGenericRepository _repository;
        private readonly IConfiguration _config;
        private readonly RecordsContext _context;
        
        public TokenService(IGenericRepository repository, IConfiguration config, RecordsContext context)
        {
            _repository = repository;
            _config = config;
            _context = context;
        }

        public string CreateTokenForUser(string userEmail, bool remember)
        {
            try
            {
                userEmail = userEmail.ToUpper();

                System.Console.WriteLine("CreateTokenForUser --------------- " + userEmail);
                var user = _context.ApplicationUsers.Include(p => p.Company).Where(p => p.NormalizedEmail == userEmail).FirstOrDefault();
                System.Console.WriteLine(user == null);
                var roleInfo = _repository.GetWithIncludeOne<ApplicationUserRole>(p => p.UserId == user.Id, link => link.Role); 
                System.Console.WriteLine(roleInfo == null);
                var role = roleInfo.Role.Name;            
                System.Console.WriteLine(role);
                System.Console.WriteLine(user.StatusId);

                if (user.StatusId == 3)
                {
                    Console.WriteLine("New claim");
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
                    System.Console.WriteLine("End");

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"]));
                    System.Console.WriteLine($"{key}");
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
