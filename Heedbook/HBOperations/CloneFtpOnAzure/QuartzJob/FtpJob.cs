using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBLib.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Quartz;
using Microsoft.Azure;

namespace CloneFtpOnAzure
{
    public class FtpJob : IJob
    {
        public SftpClient _sftpClient;
        public BlobController _blobController;

        public FtpJob(SftpClient sftpClient, BlobController blobController)
        {
            _blobController = blobController;
            _sftpClient = sftpClient;
        }
        

        public async Task Execute(IJobExecutionContext context)
        {
            
            string[] path ={"dialoguevideos","dialogueaudios","clientavatars","mediacontens"};
            var files = new Dictionary<string, ICollection<String>>();
            path.Select(async item => files[item] = await _sftpClient.ListDirectoryFiles(item)).ToList();
            var localPaths = new Dictionary<string, List<string>>();
            foreach (var file in files)
            {
                localPaths[file.Key] = new List<string>();
                foreach (var fileName in file.Value)
                {
                 var localPath = await  _sftpClient.DownloadFromFtpToLocalDiskAsync(Path.Combine(file.Key, fileName));
                 localPaths[file.Key].Add(localPath);
                }
            }

            foreach (var localPath in localPaths)
            {
                foreach (var localPathValue in localPath.Value)
                {
                   await _blobController.UploadBlob(localPathValue,Path.Combine(localPath.Key + Path.GetFileName(localPathValue)));
                   
                }
            }
            // /opt/download/1232312.jpg -> 1232312.jpg;
            // directory -> dialoguevideos;
            // Path.Combine(directory, 1232312.jpg) -> directory/1232312.jpg - linux | directory\1232312.jpg - windows
            
        }
        
    }
}