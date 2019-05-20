using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HBData.Models;

namespace UserOperations.Services
{
    public interface IFileRefService : IDisposable
    {
        string GetFileUrl(string fileName, string containerName, DateTime expirationDate);
    }
}
