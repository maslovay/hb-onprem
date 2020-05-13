using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using HBData.Models;
using HBData.Repository;
using System.Security.Cryptography;
using HBLib.Utils;
using HBLib;
using Microsoft.AspNetCore.Http;
using UserOperations.Utils.CommonOperations;
using UserOperations.Services.Interfaces;
using UserOperations.Utils.Interfaces;

namespace UserOperations.Services
{
    public class LoginService : ILoginService
    {
        private readonly IConfiguration _config;
        private readonly IGenericRepository _repository;
        private readonly IFileRefUtils _fileRef;
        //  private readonly SftpSettings _sftpSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const int PASSWORDS_TO_SAVE = 5;
        private const int ATTEMPT_TO_FAIL_LOG_IN = 5;

        public LoginService(
            IConfiguration config,
            IGenericRepository repository,
            IFileRefUtils fileRef,
            //   SftpSettings sftpSettings, 
            IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _repository = repository;
            _fileRef = fileRef;
            //   _sftpSettings = sftpSettings;
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
            if (password == null || login == null) return false;
            login = login.ToUpper();
            password = GeneratePasswordHash(password);
            return _repository.GetAsQueryable<ApplicationUser>().Count(p => p.NormalizedEmail == login && p.PasswordHash == password) == 1;
        }

        public bool CheckDeviceLogin(string deviceName, string code)
        {
            if (code == null || deviceName == null) return false;
            code = GeneratePasswordHash(code);
            return _repository.GetAsQueryable<Device>().Count(p => p.Name.ToUpper() == deviceName.ToUpper() && p.Code == code) == 1;
        }

        public string CreateTokenForUser(ApplicationUser user)
        {
            try
            {
                // var roleInfo = _repository.GetWithIncludeOne<ApplicationUserRole>(p => p.UserId == user.Id, link => link.Role); 
                var roleInfo = _repository.GetAsQueryable<ApplicationUserRole>().Include(x => x.Role).Where(x => x.UserId == user.Id).FirstOrDefault();
                var role = roleInfo.Role.Name;
                Claim[] claims;
                if (role.ToLower() == "service")
                {
                    claims = ClaimsForWebsocket(user, role);
                }
                else if (user.StatusId == 3)
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
                        new Claim("fullName", user.FullName),
                        new Claim("avatar", GetAvatar(user.Avatar)),
                        new Claim("isExtended", user.Company.IsExtended.ToString())
                    };
            return claims;
        }

        private Claim[] ClaimsForWebsocket(ApplicationUser user, string role)
        {
            var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim("applicationUserId", user.Id.ToString()),
                        new Claim("applicationUserName", user.FullName),
                        new Claim("role", role),
                        new Claim("fullName", user.FullName),
                    };
            return claims;
        }

        public string CreateTokenForDevice(Device device)
        {
            try
            {
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, device.Name),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("deviceId", device.DeviceId.ToString()),
                    new Claim("deviceName", device.Name),
                    new Claim("companyId", device.CompanyId.ToString()),
                    new Claim("corporationId", device.Company.CorporationId.ToString()),
                    new Claim("languageCode", device.Company.LanguageId.ToString()),
                    new Claim("isExtended", device.Company.IsExtended.ToString())
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
            catch (Exception e)
            {
                return $"Device not exist or internal error {e}";
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
        public bool GetIsExtended()
        => Boolean.Parse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "isExtended")?.Value);
        public Guid GetCurrentCompanyId()
           => Guid.Parse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value);
        public Guid? GetCurrentCorporationId()
        {
            Guid.TryParse(_httpContextAccessor.HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == "corporationId")?.Value, out var corporationId);
            return corporationId == default ? null : (Guid?)corporationId;
        }


        public Guid GetCurrentUserId() =>
            Guid.Parse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "applicationUserId")?.Value);

        public Int32 GetCurrentLanguagueId()
           => Int32.Parse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "languageCode")?.Value);

        public Guid? GetCurrentDeviceId()
        {
            Guid.TryParse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "deviceId")?.Value, out var deviceId);
            return (deviceId == Guid.Empty || deviceId == null) ? null : (Guid?)deviceId;
        }
        public string GetCurrentRoleName()
        {
            return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        }

        private bool AvatarExist(string avatarPath)
        {
            if (String.IsNullOrEmpty(avatarPath))
                return false;
            return true;
        }

        public string GetAvatar(string avatarPath)
        {
            if (AvatarExist(avatarPath))
                return _fileRef.GetFileLink("useravatars", avatarPath, default);
            return "";
        }
        public bool IsAdmin()
        {
            return GetCurrentRoleName().ToUpper() == "ADMIN" ? true : false;
        }




        ///remove
        public string CreateTokenEmpty()
        {
            try
            {
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("name", "empty")
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
            catch (Exception e)
            {
                return $"Device not exist or internal error {e}";
            }
        }
    }
}
