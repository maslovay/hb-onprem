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

namespace UserOperations.Services
{
    public class FileRefService : IFileRefService
    {
        private readonly IGenericRepository _repository;
        private readonly IConfiguration _config;
        private readonly   SftpClient _sftpClient;
        
        public FileRefService(IGenericRepository repository, IConfiguration config, SftpClient sftpClient)
        {
            _repository = repository;
            _config = config;
            _sftpClient = sftpClient;
        }

       public string GetFileUrl(string fileName, string containerName, DateTime expirationDate)
       {
            if (string.IsNullOrEmpty(containerName))
                return "containerName is empty";
            if (string.IsNullOrEmpty(fileName))
                return "fileName is empty";
            if (expirationDate == default(DateTime))
                expirationDate = DateTime.Now.AddDays(2);
            

            var hash = MakeExpiryHash(expirationDate);
            var link = string.Format($"http://{_sftpClient.Host}/FileRef/GetFile?path={_sftpClient.DestinationPath}/{containerName}/" +
                                        $"{fileName}&expirationDate={expirationDate:s}&token={hash}");
            return link;
       }

        private string MakeExpiryHash(DateTime expiry)
        {
            const string salt = "Secret Phrase";                                                                  
            string result = "";                                                                                 
            byte[] bytes = Encoding.UTF8.GetBytes(salt + expiry.ToString("s"));                                     
            using (var sha = System.Security.Cryptography.SHA1.Create())                                            
            {
                IEnumerable<string> listString = sha.ComputeHash(bytes).Select(b => b.ToString("x2"));              
                result = string.Concat(listString).Substring(8);                                                      
            }
            return result;                                                                                            
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
