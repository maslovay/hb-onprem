using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Utils
{
    public class SftpClient : IDisposable
    {
        private readonly SftpSettings _sftpSettings;
        private readonly Renci.SshNet.SftpClient _client;

        public SftpClient(SftpSettings sftpSettings)
        {
            _client = new Renci.SshNet.SftpClient(sftpSettings.Host, sftpSettings.Port, sftpSettings.UserName, sftpSettings.Password);
            _sftpSettings = sftpSettings;
        }

        private async Task ConnectToSftpAsync()
        {
            if (!_client.IsConnected)
            {
                await Task.Run(() => _client.Connect()).ContinueWith((t) =>
                {
                    _client.ChangeDirectory(_sftpSettings.DestinationPath);
                });
            }
        }

        /// <summary>
        /// Upload file to remote sftp server. 
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="remotePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task UploadAsync(String localPath, String remotePath, String fileName)
        {
            await ConnectToSftpAsync();
            using (var fs = new FileStream(localPath, FileMode.Open))
            {
                _client.BufferSize = 4 * 1024;
                _client.ChangeDirectory(_sftpSettings.DestinationPath + remotePath);
                await Task.Run(() => _client.UploadFile(fs, fileName));
            }
        }

        /// <summary>
        /// Upload as memory stream to sftp server
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="path"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task UploadAsMemoryStreamAsync(Stream stream, String path, String filename)
        {
            await ConnectToSftpAsync();
            _client.BufferSize = 4 * 1024;
            _client.ChangeDirectory(_sftpSettings.DestinationPath + path);
            _client.UploadFile(stream, filename);
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
            var files = _client.ListDirectory(directory);

            var tasks = files.Select(file =>
            {
                var name = file.Name;
                return new Task(async () =>
                {
                    using (var ms = new MemoryStream())
                    {
                        _client.DownloadFile(directory + name, ms);
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
            _client.DownloadFile(path, stream);
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
            using (var fs = File.OpenWrite(localPath))
            {
                await Task.Run(() => _client.DownloadFile(remotePath, fs));
            }
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
            return await Task.Run(() => _client.Exists(path));
        }

        /// <summary>
        /// Delete file from server
        /// </summary>
        /// <param name="path">Specified directory + filename</param>
        /// <returns></returns>
        public async Task DeleteFileIfExistsAsync(String path)
        {
            await ConnectToSftpAsync();
            if (_client.Exists(path))
            {
                await Task.Run(() => _client.DeleteFile(path));
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}