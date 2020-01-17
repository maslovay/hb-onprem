using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeleteScheduler.Models;
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
                var notations = new List<RemoveReport>();
                var counter = 0;
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
                        .OrderByDescending(item => item.LastWriteTimeUtc)
                        .Select(item => item.FullName)
                        .ToList();

                    counter = fileNames.Count;
                    notations.Add(new RemoveReport(dir, counter));
                    var number = 0;
                    foreach (var fileName in fileNames)
                    {
                        await _sftpclient.DeleteFileIfExistsAsync(fileName);
                        System.Console.WriteLine($"{number++}/{counter}: {fileName} deleted");
                    }
                }
                var report = "Deleted ";
                foreach(var n in notations)
                    report += $"{n.RemovedFileCount} files from {n.FolderName} ";
                _log.Info(report);
            }
            catch (Exception e)
            {
                _log.Fatal($"{e}");
            }            
            _log.Info("Function ended");
        }
    }
}