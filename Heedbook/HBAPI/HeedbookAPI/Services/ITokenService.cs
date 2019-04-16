using System;
using System.Collections.Generic;
using System.Threading.Tasks;



namespace UserOperations.Services
{
    public interface ITokenService : IDisposable
    {
        string CreateTokenForUser(string userEmail, bool remember);
        Dictionary<string, string> GetDataFromToken(string token, string sign = null);
        bool CheckToken(string token, string sign = "");
    }
}
