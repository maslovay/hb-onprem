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

        /// <summary>
        /// Upload file to remote sftp server. 
        /// </summary>
        /// <param name="sourceFile">name of source file</param>
        /// <param name="container">name of folder in remote server</param>
        /// <returns></returns>
        public async Task UploadAsync(String sourceFile, String container)
        {
            await ConnectToSftpAsync();
            using (var fs = new FileStream(sourceFile, FileMode.Open))
            {
                await _client.PutFileAsync(fs, container);
            }
        }

        /// <summary>
        /// Downloads all files from specified directory. It's downloaded files to memory.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>Returns ConcurrentDictionary String, MemoryStream where string is filename, Memory stream is file</returns>
        public async Task<ConcurrentDictionary<String, MemoryStream>> DownloadAllAsMemoryStreamAsync(String directory)
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
        /// <summary>
        /// Download one file from sftp as memory stream
        /// </summary>
        /// <param name="path">Specific remote path of file. {folder}/{file}</param>
        /// <returns></returns>
        public async Task<MemoryStream> DownloadFromFtpAsMemoryStreamAsync(String path)
        {
            await ConnectToSftpAsync();
            var stream = new MemoryStream();
            await _client.GetFileAsync(path, stream);
            return stream;
        }
        
        /// <summary>
        /// Download file to local disk.
        /// </summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public async Task<String> DownloadFromFtpToLocalDiskAsync(String remotePath)
        {
            await ConnectToSftpAsync();
            var filename = remotePath.Split('/').Last();
            var localPath = _sftpSettings.DownloadPath + filename;
            await _client.GetFileAsync(remotePath, localPath);
            return localPath;
        }
        
        /// <summary>
        /// Check file exists on server
        /// </summary>
        /// <param name="path">Specified directory + filename</param>
        /// <returns></returns>
        public async Task<Boolean> IsFileExistsAsync(String path)
        {
            await ConnectToSftpAsync();
            return await _client.FileExistsAsync(path);
        }

        /// <summary>
        /// Delete file from server
        /// </summary>
        /// <param name="path">Specified directory + filename</param>
        /// <returns></returns>
        public async Task DeleteFileIfExistsAsync(String path)
        {
            await ConnectToSftpAsync();
            if (await _client.FileExistsAsync(path))
            {
                await _client.DeleteFileAsync(path);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}