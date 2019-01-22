using Rebex.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Threading.Tasks;
using System.Linq;

namespace HBLib.Utils
{
    public class SftpClient : IDisposable
    {
        private readonly SftpSettings _sftpSettings;
        private readonly Sftp _client;

        public SftpClient(SftpSettings sftpSettings)
        {
            _client = new Sftp();
            _sftpSettings = sftpSettings;
        }

        public async void UploadAsync(String sourceFile, String container)
        {
            var destinationPath = _sftpSettings.DestinationPath + container;
            _client.Connect(_sftpSettings.Host, _sftpSettings.Port);
            await _client.LoginAsync(_sftpSettings.UserName, _sftpSettings.Password);
            await _client.ChangeDirectoryAsync(destinationPath);
            using (var fs = new FileStream(sourceFile, FileMode.Open))
            {
                await _client.PutFileAsync(fs, destinationPath);
            }
        }

        public async Task<ConcurrentBag<MemoryStream>> DownloadAllAsync(String remoteDirectory)
        {
            var fileStreams = new ConcurrentBag<MemoryStream>();
            _client.Connect(_sftpSettings.Host, _sftpSettings.Port);
            await _client.LoginAsync(_sftpSettings.UserName, _sftpSettings.Password);
            IEnumerable<SftpItem> files = await _client.GetListAsync(remoteDirectory);
            Parallel.ForEach(files, file =>
            {
                var fileName = file.Name;
                if ((file.Name.StartsWith(".")) || ((file.LastWriteTime.Value != DateTime.Today))) return;
                using (var f = new MemoryStream())
                {
                    _client.GetFileAsync(remoteDirectory + fileName, f);
                    fileStreams.Add(f);
                }
            });
            return fileStreams;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
