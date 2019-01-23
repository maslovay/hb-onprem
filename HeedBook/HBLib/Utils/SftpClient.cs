using Rebex.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.CompilerServices;

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

        private async Task ConnectToSftpAsync()
        {
            await _client.ConnectAsync(_sftpSettings.Host, _sftpSettings.Port);
            await _client.LoginAsync(_sftpSettings.UserName, _sftpSettings.Password);
            await _client.ChangeDirectoryAsync(_sftpSettings.DestinationPath);
        }

        public async Task UploadAsync(String sourceFile, String container)
        {
            await ConnectToSftpAsync();
            using (var fs = new FileStream(sourceFile, FileMode.Open))
            {
                await _client.PutFileAsync(fs, container);
            }
        }

        public async Task<ConcurrentDictionary<String, MemoryStream>> DownloadAllAsync(String directory)
        {
            var fileStreams = new ConcurrentDictionary<String, MemoryStream>();
            await ConnectToSftpAsync();
            IEnumerable<SftpItem> files = await _client.GetListAsync(directory);

            var tasks = files.Select(file =>
            {
                var name = file.Name;
                return new Task(async () =>
                {
                    using (var ms = new MemoryStream())
                    {
                        await _client.GetFileAsync(directory + name, ms);
                        fileStreams.TryAdd(name, ms);
                    }
                });
            });
            await Task.WhenAll(tasks);
            return fileStreams;
        }

        public async Task<Boolean> IsFileExistsAsync(String path)
        {
            await ConnectToSftpAsync();
            return await _client.FileExistsAsync(path);
        }

        public async Task DeleteFileAsync(String path)
        {
            await ConnectToSftpAsync();
            await _client.DeleteFileAsync(path);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
