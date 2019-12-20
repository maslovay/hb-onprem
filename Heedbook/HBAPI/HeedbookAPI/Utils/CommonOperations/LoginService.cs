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
using System.Net.Mail;
using System.Net;
using HBLib.Utils;
using HBLib;
using Microsoft.AspNetCore.Http;

namespace UserOperations.Services
{
    public class LoginService
    {
        private readonly IGenericRepository _repository;
        private readonly IConfiguration _config;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const int PASSWORDS_TO_SAVE = 5;
        private const int ATTEMPT_TO_FAIL_LOG_IN = 5;

        public LoginService(IGenericRepository repository, 
            IConfiguration config, RecordsContext context, SftpClient sftpClient, 
            SftpSettings sftpSettings, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _config = config;
            _context = context;
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _httpContextAccessor = httpContextAccessor;
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
            if(password == null || login == null) return false;
            login = login.ToUpper();
            password = GeneratePasswordHash(password);
            return _context.ApplicationUsers.Count(p => p.NormalizedEmail == login && p.PasswordHash == password) == 1;
        }

        public string CreateTokenForUser(ApplicationUser user, bool remember)
        {
            try
            {
                // var roleInfo = _repository.GetWithIncludeOne<ApplicationUserRole>(p => p.UserId == user.Id, link => link.Role); 
                var roleInfo = _context.ApplicationUserRoles.Include(x => x.Role).Where(x => x.UserId == user.Id).FirstOrDefault();
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
                        new Claim("fullName", user.FullName),
                        new Claim("avatar", AvatarExist(user.Avatar) ? $"http://{_sftpSettings.Host}/useravatars/{user.Avatar}":""),
                    };

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

        // <summary>
        /// Parse JWT token 
        /// </summary>
        /// <param name="token">JWT token in request</param>
        /// <returns></returns>
        public bool GetDataFromToken(string token, out Dictionary<string, string> claims, string sign = null)
        {
            claims = null;
            if (sign == "" || sign == null)
                sign = _config["Tokens:Key"];
            try
            {

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
            catch (Exception e)
            {
                return false;
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

        
        public bool SavePasswordHistory(Guid userId, string passwordHash)
        {
            // PasswordHistory newPswd = null;
            // var passwords = _context.PasswordHistorys.Where(x => x.UserId == userId).OrderBy(x => x.CreationDate).ToList();
            // if (passwords.Any(x => x.PasswordHash == passwordHash))
            //     return false;
            // if (passwords.Count() < PASSWORDS_TO_SAVE)//---save five last used passwords
            // {
            //     newPswd = new PasswordHistory();
            //     newPswd.PasswordHistoryId = Guid.NewGuid();
            //     newPswd.UserId = userId;
            //     _context.Add(newPswd);
            // }
            // else newPswd = passwords.First();

            // newPswd.CreationDate = DateTime.UtcNow;
            // newPswd.PasswordHash = passwordHash;
            // _context.SaveChanges();
            return true;
        }
        // make error logins counter zero(if success) or create new line in error logins
        public bool SaveErrorLoginHistory(Guid userId, string type)
        {
           
            // var lastlogin = _context.LoginHistorys.Where(x => x.UserId == userId).OrderByDescending(x => x.LoginTime).FirstOrDefault(); 
            // //---if user make success login we need make counter of failed logins to zero
            // if( type == "success" )
            // {
            //     if ( lastlogin != null && lastlogin.Attempt != 0 )
            //     {
            //         lastlogin.Attempt = 0;
            //         _context.SaveChanges();
            //     }
            //     return true;
            // }
            // //---if failed login - save in magazine
            // LoginHistory newErrLogin = new LoginHistory();
            // newErrLogin.LoginHistoryId = Guid.NewGuid();
            // newErrLogin.UserId = userId;
            // newErrLogin.LoginTime = DateTime.UtcNow;         
            // _context.Add(newErrLogin);
            //  if( lastlogin == null || lastlogin.Attempt < ATTEMPT_TO_FAIL_LOG_IN - 1)//---5 attempt, if last was 3 - save as 4
            // {
            //     newErrLogin.Attempt = lastlogin!= null? lastlogin.Attempt + 1 : 1;
            //     newErrLogin.Message = "Wrong password";
            //     _context.SaveChanges();
            //     return true;//---user has attempts
            // }
            // else 
            // {
            //     newErrLogin.Attempt = ATTEMPT_TO_FAIL_LOG_IN;//---5 attempt, if last was 4 - save as 5 and block user
            //     newErrLogin.Message = "Wrong password. Blocked";
            //     _context.SaveChanges();
            //     return false;//---user has no any attempts
            // }               
           return true;
        }
      

        // //sendgrid account settings
        // private static string sendGridApiKey = "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";
        // private static string sendGridSenderEmail = "info@wantad.club";
        // private static string sendGridSenderName = "WantAd";
      
        public string GeneratePass(int x)
        {
            string pass = "";
            var r = new Random();
            while (pass.Length < x)
            {
                Char c = (char)r.Next(33, 125);
                if (Char.IsLetterOrDigit(c))
                    pass += c;
            }
            return pass;
        }
        public Guid GetCurrentCompanyId()
           => Guid.Parse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value);
        public Guid? GetCurrentCorporationId()
        { 
           Guid.TryParse(_httpContextAccessor.HttpContext.User.Claims
               .FirstOrDefault(c => c.Type == "corporationId")?.Value, out var corporationId);
           return corporationId;
        }


        public Guid GetCurrentUserId()
            => Guid.Parse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "applicationUserId")?.Value);

        public string GetCurrentRoleName()
        {
           return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
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
        private bool AvatarExist(string avatarPath)
        {
            if(String.IsNullOrEmpty(avatarPath))
                return false;
            var task = _sftpClient.IsFileExistsAsync($"useravatars/{avatarPath}");      
            task.Wait();
            bool exist = task.Result;      
            return exist;
        }
        public bool IsAdmin()
        {
            return GetCurrentRoleName().ToUpper() == "ADMIN" ? true : false;
        }
    }
}
