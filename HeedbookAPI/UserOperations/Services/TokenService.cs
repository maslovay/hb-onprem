using UserOperations.Models;
using UserOperations.Repository;
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



namespace UserOperations.Services
{
    public class TokenService : ITokenService
    {
        private readonly IGenericRepository _repository;
        private readonly IConfiguration _config;

        public TokenService(IGenericRepository repository, IConfiguration config)
        {
            _repository = repository;
            _config = config;
        }

        public async Task<string> CreateTokenForUser(string userEmail, bool remember)
        {
            try
            {

                var user = await _repository.FindOneByConditionAsync<ApplicationUser>(p => p.NormalizedEmail == userEmail);
                var role = _repository.GetWithIncludeOne<ApplicationUserRole>(p => p.UserId == user.Id, q => q.Role).Role.Name; 
                var company = await _repository.FindOneByConditionAsync<Company>(p => p.CompanyId == user.CompanyId);
                Console.WriteLine($"{user.StatusId}");

                if (user.StatusId == 3)
                {
                    Console.WriteLine("New claim");
                    var claims = new[]
                    {       
                        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim("applicationUserId", user.Id.ToString()),
                        new Claim("applicationUserName", user.FullName),
                        new Claim("companyName", company.CompanyName),
                        new Claim("companyId", company.CompanyId.ToString()),
                        new Claim("corporationId", company.CorporationId.ToString()),
                        new Claim("languageCode", company.LanguageId.ToString()),
                        // new Claim("role", role),
                    };
                    Console.WriteLine("Create key");
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    Console.WriteLine($"{key}");
                    var token = new JwtSecurityToken(_config["Tokens:Issuer"],
                        _config["Tokens:Issuer"],
                        claims,
                        expires: remember ? DateTime.Now.AddDays(31) : DateTime.Now.AddDays(1),
                        signingCredentials: creds);

                    Console.WriteLine($"{token}");
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
