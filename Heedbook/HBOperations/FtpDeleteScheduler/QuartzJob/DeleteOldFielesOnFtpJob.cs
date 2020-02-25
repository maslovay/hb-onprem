using System;
using System.IO;
using System.Linq;
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
            var dirs = new[] {"videos", "frames"};
            var _log = _elasticClientFactory.GetElasticClient();
            try
            {
                _log.Info($"Start function");
                foreach (var dir in dirs)
                {
                    var files = await _sftpclient.ListDirectoryAsync(dir);
                    var now = DateTime.UtcNow;
                    var fileNames = files.Where(item =>
                        {
                            var diff = now - item.LastWriteTimeUtc;
                            return diff.Days >= 30;
                        })
                        .Where(item => !item.IsDirectory)
                        .Select(item => item.FullName);
                    foreach (var fileName in fileNames)
                    {
                        await _sftpclient.DeleteFileIfExistsAsync(fileName);
                        _log.Info($"Deleted {fileName}");
                    }
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"{e}");
            }

            _log.Info("Function ended");
        }
    }
}