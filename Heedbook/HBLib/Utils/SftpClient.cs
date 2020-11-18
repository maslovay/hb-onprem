using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBLib.Utils.Interfaces;
using Microsoft.Extensions.Configuration;
using Renci.SshNet.Messages.Transport;
using Renci.SshNet.Sftp;

namespace HBLib.Utils
{
    public class SftpClient : IDisposable, ISftpClient
    {
        private readonly string httpFileUrl;
        private readonly Renci.SshNet.SftpClient _client;
        private readonly SftpSettings _sftpSettings;
        private readonly IConfiguration _config;

        public SftpClient(SftpSettings sftpSettings, IConfiguration config)
        {

            _client = new Renci.SshNet.SftpClient(sftpSettings.Host, sftpSettings.Port, sftpSettings.UserName,
                sftpSettings.Password);
            _sftpSettings = sftpSettings;
            _config = config;

            httpFileUrl = _config["FileRefPath:url"];
            var _retryCount = 5;
            while (true)
            {
                try
                {
                    ConnectToSftpAsync().Wait();
                    break;
                }
                catch (Exception e)
                {
                    _retryCount-- ;
                    if (_retryCount == 0) throw;
                    Thread.Sleep(100 * _retryCount);
                }
            }
        }

        public void RenameFile(String oldPath, String newPath)
        {
            ConnectToSftpAsync().Wait();
            _client.RenameFile(oldPath, newPath);
        }
        
        public void Dispose()
        {
            _client.Dispose();
        }
        public bool ClientIsConnected()
        {
            return _client.IsConnected;            
        }
        private async Task ConnectToSftpAsync()
        {
            if (!_client.IsConnected)
            {
                await Task.Run(() => _client.Connect()).ContinueWith(t => { ChangeDirectoryToDefault(); });
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
     
        public async Task<List<string>> GetFileNames(String directory)
        {
            await ConnectToSftpAsync();
            return _client.ListDirectory(directory).Where(f => !f.IsDirectory).Select(f => f.Name).ToList();
        }

        public void ChangeDirectory(String path)
        {
            ConnectToSftpAsync().Wait();
            _client.ChangeDirectory(path);
        }

        public void ChangeDirectoryToDefault()
        {
            ConnectToSftpAsync().Wait();
            _client.ChangeDirectory(_sftpSettings.DestinationPath);
        }


        /// <summary>
        /// Get urls to files. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetAllFilesUrl(String directory, string[] subDirs = null)
        {
            await ConnectToSftpAsync();
            List<Renci.SshNet.Sftp.SftpFile> files = new List<Renci.SshNet.Sftp.SftpFile>();
            if (subDirs != null && subDirs.Count() != 0)
            {
                foreach (var dir in subDirs)
                {
                    files.AddRange(_client.ListDirectory($"{directory}/{dir}"));
                }
            }
            else
                files = _client.ListDirectory(directory).ToList();
            return await Task.Run(() => files
                .Where(f => !f.IsDirectory)
                .Select(f => $"http://{_sftpSettings.Host}/{f.FullName.Replace("/home/nkrokhmal/storage/", "")}"));
        }

        /// <summary>
        /// Get urls to files. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IEnumerable<FileInfoModel>> GetAllFilesData(String directory, string subDir)
        {
            await ConnectToSftpAsync();
            List<Renci.SshNet.Sftp.SftpFile> files = new List<Renci.SshNet.Sftp.SftpFile>();
            files = _client.ListDirectory($"{directory}/{subDir}").ToList();
            return await Task.Run(() => files
                .Where(f => !f.IsDirectory)
                .Select(f =>
                    new FileInfoModel
                    {
                        url = $"{httpFileUrl}{f.FullName.Replace("/home/nkrokhmal/storage/", "")}",
                        name = f.Name,
                        date = f.Attributes.LastWriteTime
                    }));
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
        ///     Upload as memory stream to sftp server
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="path"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task UploadAsMemoryStreamAsync(Stream stream, String path, String filename, Boolean toDestionationPath = false)
        {
            await ConnectToSftpAsync();
            _client.BufferSize = 4 * 1024;
            //_client.BufferSize = (uint)stream.Length;
            await CreateIfDirNoExistsAsync(_sftpSettings.DestinationPath + path);
            _client.ChangeDirectory(_sftpSettings.DestinationPath + path);
            _client.UploadFile(stream, filename);
            if (toDestionationPath) _client.ChangeDirectory(_sftpSettings.DestinationPath);
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
            var filename = Path.GetFileName(remotePath);

            localPath = localPath == null ? Path.Combine(_sftpSettings.DownloadPath, filename) : Path.Combine(localPath, filename);
            using (var fs = File.OpenWrite(localPath))
            {
                try
                { 
                _client.DownloadFile(remotePath, fs);
                    //await Task.Run(() => _client.DownloadFile(remotePath, fs));
                }
                catch
                {
                    _client.Disconnect();
                    await ConnectToSftpAsync();
                    _client.DownloadFile(remotePath, fs);
                }
            }
            return localPath;
        }

        public DateTime GetLastWriteTime(string path)
        {
            return _client.GetLastWriteTime(path);
        }

        /// <summary>
        ///     Download files to local disk.
        /// </summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public async Task<String> DownloadFromFtpToLocalDiskAsync(String remotePath, String pattern,
            String localPath = null)
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
        ///     Check file exists on server and create new if no exist
        /// </summary>
        /// <param name="path">Specified directory + filename</param>
        /// <returns></returns>
        public async Task CreateIfDirNoExistsAsync(String path)
        {
            await ConnectToSftpAsync();
            if (!await Task.Run(() => _client.Exists(path)))
                _client.CreateDirectory(path);
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
                await Task.Run(() => _client.DeleteFile(path));
        }

        /// <summary>
        ///    Deletes files from a server using a pattern
        /// </summary>
        /// <param name="path">Specified directory</param>
        /// <param name="pattern">Filename pattern</param>
        /// <returns></returns>
        public async Task<IEnumerable<Task>> DeleteFileIfExistsBulkAsync(string path, string pattern)
        {
            var files = await ListDirectoryFiles(path, pattern);
            return await DeleteFileIfExistsBulkAsync(files);
        }

        /// <summary>
        ///    Deletes files from a server
        /// </summary>
        /// <param name="files">File paths</param>
        /// <returns></returns>
        public async Task<IEnumerable<Task>> DeleteFileIfExistsBulkAsync(IEnumerable<string> files)
        {
            await ConnectToSftpAsync();

            var taskList = new List<Task>(files.Count());

            foreach (var path in files)
            {
                if (_client.Exists(path))
                    taskList.Add(Task.Run(() => _client.DeleteFile(path)));
            }

            return taskList.ToArray();
        }

        public async Task<List<string>> ListDirectoryFilesByConditionAsync(String path, Func<SftpFile, bool> predicate)
        {
            var result = new List<String>();
            await ConnectToSftpAsync();

            path = _sftpSettings.DestinationPath + path;

            if (!_client.Exists(path))
                return result;
            return _client.ListDirectory(path).Where(predicate).Select(f => f.Name).ToList();
        }
        /// <summary>
        ///     Lists all files in a directory using a pattern
        /// </summary>
        /// <param name="path">dir path in FTP</param>
        /// <param name="patternToFind">pattern for filename</param>
        /// <returns></returns>
        public async Task<ICollection<String>> ListDirectoryFiles(String path, String patternToFind = null)
        {
            var result = new List<String>();
            await ConnectToSftpAsync();

            path = _sftpSettings.DestinationPath + path;

            if (!_client.Exists(path))
                return result;

            if (patternToFind != null)
                return _client.ListDirectory(path).Where(f => !f.IsDirectory && f.Name.Contains(patternToFind))
                              .Select(f => f.Name).ToList();
            return _client.ListDirectory(path).Where(f => !f.IsDirectory).Select(f => f.Name).ToList();
        }

        public async Task<IEnumerable<Renci.SshNet.Sftp.SftpFile>> GetAllFilesDataRecursively(String directory)
        {
            await ConnectToSftpAsync();
            return GetAllFilesRecursively(directory);
            
        }
        private IEnumerable<Renci.SshNet.Sftp.SftpFile> GetAllFilesRecursively(String directory)
        {
            List<Renci.SshNet.Sftp.SftpFile> files = new List<Renci.SshNet.Sftp.SftpFile>();
            var listOfItems = _client.ListDirectory($"{directory}").ToList();
            foreach(var item in listOfItems)
            {
                if(item.IsDirectory)
                {
                    if(item.FullName.Split("/").Last() != "." && item.FullName.Split("/").Last() != "..")
                    {
                        var fileList = GetAllFilesRecursively($"{item.FullName}");
                        files.AddRange(fileList);
                        // System.Console.WriteLine($"{item.FullName} folder");
                    }
                }
                else
                {
                    files.Add(item);
                    // System.Console.WriteLine($"{item.FullName} file");
                }
            }
            return files;
        }
        public async Task<IEnumerable<SftpFile>> ListDirectoryAsync(string path)
        {
            await ConnectToSftpAsync();
            path = _sftpSettings.DestinationPath + path;
            return _client.ListDirectory(path);
        }
        /// <summary>
        /// Disconnects from a FTP server
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_client.IsConnected)
                await Task.Run(() => _client.Disconnect());
        }
    
        public class FileInfoModel
        {
            public string url;
            public string name;
            public DateTime date;
        }

        public string DestinationPath =>
            _sftpSettings.DestinationPath;

        public string Host =>
            _sftpSettings.Host;
    }
}