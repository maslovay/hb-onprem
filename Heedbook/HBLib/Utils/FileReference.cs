using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HBLib.Utils
{
    public class FileReference
    {
        private static IConfiguration _config;

        //private readonly SftpClient _client;
        private readonly SftpSettings _sftpSettings;

        public FileReference(SftpSettings sftpSettings)
        {
            //_client = new SftpClient(sftpSettings);
            _sftpSettings = sftpSettings;
        }

        private string MakeExpiryHash(DateTime expiry)
        {
            const string salt = "Secret Phrase";
            string str1 = "";
            byte[] bytes = Encoding.UTF8.GetBytes(salt + expiry.ToString("s"));
            using (var sha = System.Security.Cryptography.SHA1.Create())
            {
                IEnumerable<string> listString = sha.ComputeHash(bytes).Select(b => b.ToString("x2"));
                str1 = string.Concat(listString).Substring(8);
            }

            return str1;
        }

        private Stream ConvertMemoryStreamToStream(MemoryStream ms)
        {
            var newStream = new MemoryStream();
            var buffer = new byte[32 * 1024]; // 32K buffer for example
            int bytesRead;

            while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) > 0)
                newStream.Write(buffer, 0, bytesRead);
            newStream.Position = 0;
            return newStream;
        }

        public string GetReference(string containerName, string fileName, DateTime expirationDate)
        {
            if (string.IsNullOrEmpty(containerName))
                return null;
            if (string.IsNullOrEmpty(fileName))
                return null;
            if (expirationDate == default(DateTime))
                expirationDate = DateTime.Now.AddDays(2);

            var hash = MakeExpiryHash(expirationDate);
            var reference = string.Format(
                $"http://{_sftpSettings.Host}/FileRef/GetFile?path={_sftpSettings.DestinationPath}/{containerName}/" +
                $"{fileName}&expirationDate={expirationDate:s}&token={hash}");

            return reference;
        }
    }
}