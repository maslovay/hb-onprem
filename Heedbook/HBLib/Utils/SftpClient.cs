using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Utils
{
    public class SftpClient : IDisposable
    {
        private readonly Renci.SshNet.SftpClient _client;
        private readonly SftpSettings _sftpSettings;

        public SftpClient(SftpSettings sftpSettings)
        {
            _client = new Renci.SshNet.SftpClient(sftpSettings.Host, sftpSettings.Port, sftpSettings.UserName,
                sftpSettings.Password);
            _sftpSettings = sftpSettings;
        }

        private async Task ConnectToSftpAsync()
        {
            if (!_client.IsConnected)
                await Task.Run(() => _client.Connect()).ContinueWith(t =>
                {
                    _client.ChangeDirectory(_sftpSettings.DestinationPath);
                });
        }

        /// <summary>
        ///     Upload file to remote sftp server.
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="remotePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task UploadAsync(String localPath, String remotePath, String fileName)
        {
            await ConnectToSftpAsync();
            using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read))
            {
                _client.BufferSize = 4 * 1024;
                _client.ChangeDirectory(_sftpSettings.DestinationPath + remotePath);
                await Task.Run(() => _client.UploadFile(fs, fileName));
            }
        }
        /// <summary>
        /// Get url to file. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<string> GetFileUrl(String path)
        {
            await ConnectToSftpAsync();
            if (await IsFileExistsAsync(_sftpSettings.DestinationPath + "/" + path))
                return $"http://{_sftpSettings.Host}/{path}";
            return null;
        }

        public async Task<IEnumerable<string>> GetAllFilesUrl(String directory, string[] subDirs = null)
        {
            await ConnectToSftpAsync();
            List<Renci.SshNet.Sftp.SftpFile> files = new List<Renci.SshNet.Sftp.SftpFile>();
            if (subDirs != null && subDirs.Count() != 0)
            {
                foreach (var dir in subDirs)
                {
                    files.AddRange(_client.ListDirectory(directory + "/" + dir));
                }
            }
            else
                files = _client.ListDirectory(directory).ToList();
            return await Task.Run(() => files.Where(f => !f.IsDirectory).Select(f => $"http://{_sftpSettings.Host}/{f.FullName.Replace("/home/nkrokhmal/storage/","")}"));
        }

        /// <summary>
        ///     Upload as memory stream to sftp server
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="path"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task UploadAsMemoryStreamAsync(Stream stream, String path, String filename)
        {
            await ConnectToSftpAsync();
            _client.BufferSize = 4 * 1024;
             if (! await IsFileExistsAsync(_sftpSettings.DestinationPath + path))
                _client.CreateDirectory(_sftpSettings.DestinationPath + path);
            _client.ChangeDirectory(_sftpSettings.DestinationPath + path);
            _client.UploadFile(stream, filename);
        }

        /// <summary>
        ///     Downloads all files from specified directory. It's downloaded files to memory.
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
                return new Task(() =>
                {
                    var ms = new MemoryStream();
                    _client.DownloadFile(directory + name, ms);
                    fileStreams.TryAdd(name, ms);
                });
            });
            await Task.WhenAll(tasks);
            return fileStreams;
        }

        /// <summary>
        ///     Download one file from sftp as memory stream
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
        ///     Download file to local disk.
        /// </summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public async Task<String> DownloadFromFtpToLocalDiskAsync(String remotePath, String localPath = null)
        {
            await ConnectToSftpAsync();
            //Console.WriteLine("Successfully connected");
            var filename = remotePath.Split('/').Last();

            //Console.WriteLine(localPath == null);
            localPath = localPath == null ? localPath = _sftpSettings.DownloadPath + filename : localPath + filename;
            //Console.WriteLine($"{localPath}, {remotePath}");
            using (var fs = File.OpenWrite(localPath))
            {
                await Task.Run(() => _client.DownloadFile(remotePath, fs));
            }

            return localPath;
        }

        /// <summary>
        ///     Download files to local disk.
        /// </summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public async Task<String> DownloadFromFtpToLocalDiskAsync(String remotePath, string pattern, String localPath = null)
        {
            await ConnectToSftpAsync();
            Console.WriteLine("Successfully connected");
            var filename = remotePath.Split('/').Last();

            Console.WriteLine(localPath == null);
            localPath = localPath == null ? localPath = _sftpSettings.DownloadPath + filename : localPath + filename;
            Console.WriteLine($"{localPath}, {remotePath}");
            using (var fs = File.OpenWrite(localPath))
            {
                await Task.Run(() => _client.DownloadFile(remotePath, fs));
            }

            return localPath;
        }
        
        /// <summary>
        ///     Check file exists on server
        /// </summary>
        /// <param name="path">Specified directory + filename</param>
        /// <returns></returns>
        public async Task<Boolean> IsFileExistsAsync(String path)
        {
            await ConnectToSftpAsync();
            return await Task.Run(() => _client.Exists(path));
        }

        /// <summary>
        ///     Delete file from server
        /// </summary>
        /// <param name="path">Specified directory + filename</param>
        /// <returns></returns>
        public async Task DeleteFileIfExistsAsync(String path)
        {
            await ConnectToSftpAsync();
            if (_client.Exists(path)) 
                await Task.Run(() => _client.DeleteFile(path));conflict?name=Heedbook%252FHBOperations%252FHeedbook.sln&ancestor_oid=71a486bf139b886d6ad0fd7a39b03b29fc09da9f&base_oid=d6a55cb7d861f54f5e249d439ac87fdb245fb382&head_oid=29cccf1b9acab127d6b5a35d2533d65fca438522
        }

        /// <summary>
        /// Lists all files in a directory using a pattern
        /// </summary>
        /// <param name="path">dir path in FTP</param>
        /// <param name="patternToFind">pattern for filename</param>
        /// <returns></returns>
        public async Task<ICollection<string>> ListDirectoryFiles(string path, string patternToFind = null)
        {
            var result = new List<string>();
            await ConnectToSftpAsync();

            path = _sftpSettings.DestinationPath + path;

            if (!_client.Exists(path))
                return result;

            if (patternToFind != null)
                return _client.ListDirectory(path).Where(f => !f.IsDirectory && f.Name.Contains(patternToFind)).Select(f => f.Name).ToList();
            else
                return _client.ListDirectory(path).Where(f => !f.IsDirectory).Select(f => f.Name).ToList();          

            if (_client.Exists(_sftpSettings.DestinationPath + "/" + path))
            {
                await Task.Run(() => _client.DeleteFile(_sftpSettings.DestinationPath + "/" + path));
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}