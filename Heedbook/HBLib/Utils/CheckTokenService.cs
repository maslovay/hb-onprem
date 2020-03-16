using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using HBData;

namespace HBLib.Utils
{
    public class CheckTokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly RecordsContext _context;

        public CheckTokenService(
            IHttpContextAccessor httpContextAccessor,
            RecordsContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
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
