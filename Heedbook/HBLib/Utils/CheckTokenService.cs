using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using HBData;
<<<<<<< HEAD
=======
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HBData.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
>>>>>>> devices

namespace HBLib.Utils
{
    public class CheckTokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly RecordsContext _context;
<<<<<<< HEAD

        public CheckTokenService(
            IHttpContextAccessor httpContextAccessor,
            RecordsContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
=======
        private readonly IConfiguration _config;

        public CheckTokenService(
            IHttpContextAccessor httpContextAccessor,
            RecordsContext context,
            IConfiguration config)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _config = config;
        }

        public bool CheckUserLogin(string login, string password)
        {
            if (password == null || login == null) return false;
            login = login.ToUpper();
            password = GeneratePasswordHash(password);
            return _context.ApplicationUsers.Count(p => p.NormalizedEmail == login && p.PasswordHash == password) == 1;
        }

        public string GeneratePasswordHash(string password)
        {
            var crypt = new SHA256Managed();
            var passwordHashSalt = _config["Tokens:Hash_salt"];
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(password + passwordHashSalt));
            return System.Convert.ToBase64String(crypto);
        }

        public string CreateTokenForUser(ApplicationUser user)
        {
            try
            {
                var roleInfo = _context.ApplicationUserRoles.Include(x => x.Role).Where(x => x.UserId == user.Id).FirstOrDefault();
                var role = roleInfo.Role.Name;
                Claim[] claims;
                if (user.StatusId == 3)
                {
                    claims = ClaimsForUser(user, role);
                }
                else return "User inactive";

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(_config["Tokens:Issuer"],
                    _config["Tokens:Issuer"],
                    claims,
                    expires: DateTime.Now.AddDays(31),// remember ? DateTime.Now.AddDays(31) : DateTime.Now.AddDays(1),
                    signingCredentials: creds);

                var tokenenc = new JwtSecurityTokenHandler().WriteToken(token);
                return tokenenc;

            }
            catch (Exception e)
            {
                return $"User not exist or internal error {e}";
            }
        }

        private Claim[] ClaimsForUser(ApplicationUser user, string role)
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
                        new Claim("isExtended", user.Company.IsExtended.ToString())
                    };
            return claims;
>>>>>>> devices
        }

        public bool CheckIsUserAdmin()
        {
            var userId = GetCurrentUserId();
            var roleName = _context.ApplicationUserRoles.Where(x => x.UserId == userId).Select(x => x.Role.Name).FirstOrDefault();
            if (roleName.ToUpper() == "ADMIN" && IsAdmin())
                return true;
            return false;
        }

        private Guid GetCurrentUserId() =>
         Guid.Parse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "applicationUserId")?.Value);

        private string GetCurrentRoleName()
        {
            return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        }

        private bool IsAdmin()
        {
            return GetCurrentRoleName().ToUpper() == "ADMIN" ? true : false;
        }
    }
}
