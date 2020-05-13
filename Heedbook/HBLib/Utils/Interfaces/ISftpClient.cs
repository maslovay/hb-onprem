using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Renci.SshNet.Messages.Transport;
using Renci.SshNet.Sftp;

namespace HBLib.Utils.Interfaces
{
    public interface ISftpClient
    {
        string DestinationPath { get; }
        string Host { get; }

        void ChangeDirectory(string path);
        void ChangeDirectoryToDefault();
        Task CreateIfDirNoExistsAsync(string path);
        Task DeleteFileIfExistsAsync(string path);
        Task<IEnumerable<Task>> DeleteFileIfExistsBulkAsync(string path, string pattern);
        Task<IEnumerable<Task>> DeleteFileIfExistsBulkAsync(IEnumerable<string> files);
        Task DisconnectAsync();
        void Dispose();
        Task<ConcurrentDictionary<string, MemoryStream>> DownloadAllAsMemoryStreamAsync(string directory);
        Task<MemoryStream> DownloadFromFtpAsMemoryStreamAsync(string path);
        Task<string> DownloadFromFtpToLocalDiskAsync(string remotePath, string localPath = null);
        Task<string> DownloadFromFtpToLocalDiskAsync(string remotePath, string pattern, string localPath = null);
        Task<IEnumerable<SftpClient.FileInfoModel>> GetAllFilesData(string directory, string subDir);
        Task<IEnumerable<string>> GetAllFilesUrl(string directory, string[] subDirs = null);
        Task<List<string>> GetFileNames(string directory);
        Task<string> GetFileUrl(string path);
        DateTime GetLastWriteTime(string path);
        Task<bool> IsFileExistsAsync(string path);
        Task<IEnumerable<SftpFile>> ListDirectoryAsync(string path);
        Task<ICollection<string>> ListDirectoryFiles(string path, string patternToFind = null);
        Task<List<string>> ListDirectoryFilesByConditionAsync(string path, Func<SftpFile, bool> predicate);
        void RenameFile(string oldPath, string newPath);
        Task UploadAsMemoryStreamAsync(Stream stream, string path, string filename, bool toDestionationPath = false);
        Task UploadAsync(string localPath, string remotePath, string fileName);
    }
}