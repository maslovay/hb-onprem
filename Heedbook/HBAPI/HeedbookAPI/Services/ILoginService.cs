using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HBData.Models;

namespace UserOperations.Services
{
    public interface ILoginService : IDisposable
    {
        string CreateTokenForUser(ApplicationUser user, bool remember);
        Dictionary<string, string> GetDataFromToken(string token, string sign = null);
        bool GetDataFromToken(string token, out Dictionary<string, string> claims, string sign = null);
        bool CheckToken(string token, string sign = "");
        string GeneratePasswordHash(string password);
        bool CheckUserLogin(string login, string password);
        string GeneratePass(int x);
        bool SavePasswordHistory(Guid userId, string passwordHash);
        bool SaveErrorLoginHistory(Guid userId, string type);
    }
}
