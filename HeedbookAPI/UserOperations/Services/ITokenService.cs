using System;
using System.Threading.Tasks;



namespace UserOperations.Services
{
    public interface ITokenService : IDisposable
    {
        Task<string> CreateTokenForUser(string userEmail, bool remember);
    }
}
