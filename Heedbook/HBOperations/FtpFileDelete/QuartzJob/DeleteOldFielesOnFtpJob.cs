using System;
using System.IO;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Quartz;

namespace DeleteScheduler.QuartzJob
{
    public class DeleteOldFilesOnFtpJob : IJob
    {
        private SftpClient _sftpclient;
        private ElasticClientFactory _elasticClientFactory;
        
        public DeleteOldFilesOnFtpJob(SftpClient sftpclient, 
            ElasticClientFactory elasticClientFactory)
        {
            _elasticClientFactory = elasticClientFactory;
            _sftpclient = sftpclient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dirs = new[] {"frames", "videos"};
            var _log = _elasticClientFactory.GetElasticClient();
            try
            {
                _log.Info($"Start function");
                foreach (var dir in dirs)
                {
                    var fileNames = await _sftpclient.ListDirectoryFiles(dir);
                    foreach (var fileName in fileNames)
                    {
                        var lastWriteTime = _sftpclient.GetLastWriteTime(fileName);
                        var diff = DateTime.UtcNow - lastWriteTime;
                        if (diff.Days >= 30)
                        {
                            var path = dir.Contains(Path.DirectorySeparatorChar)
                                ? dir + fileName
                                : dir + Path.DirectorySeparatorChar + fileName;
                            await _sftpclient.DeleteFileIfExistsAsync(path);
                            _log.Info($"Deleted {fileName}");
                        }
                    }
                }
                
            }
            catch(Exception e)
            {
                _log.Fatal($"{e}");
            }
            _log.Info("Function ended");
        }
    }
}