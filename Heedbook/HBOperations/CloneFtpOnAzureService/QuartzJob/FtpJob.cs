using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using HBLib.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Quartz;
using Microsoft.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace CloneFtpOnAzureService
{
    public class FtpJob : IJob
    {
        private SftpClient _sftpClient;
        private BlobSettings _blobSettings;
        private BlobClient _blobClient;
        private readonly ElasticClientFactory _elasticClientFactory;        
        private RecordsContext _context;
        private readonly IServiceScopeFactory _scopeFactory;

        public FtpJob(IServiceScopeFactory scopeFactory,
            SftpClient sftpClient,
            BlobSettings blobSettings,
            BlobClient blobClient,
            ElasticClientFactory elasticClientFactory)
        {
            _scopeFactory = scopeFactory;
            _elasticClientFactory = elasticClientFactory;
            _blobSettings = blobSettings;
            _blobClient = blobClient;
            _sftpClient = sftpClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _log = _elasticClientFactory.GetElasticClient();
                try
                {
                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();

                    var dialogues = _context.Dialogues
                        .Where(d => d.Status.StatusId == 3 &&
                                    //d.CreationTime >= DateTime.UtcNow.AddHours(-24)
                                    d.CreationTime.Date >= new DateTime(2020, 02, 27).Date
                                    )
                        .Select(s => s.DialogueId)
                        .ToList();
                    var tasks = new List<Task>();
                    var dict = new Dictionary<String, String>()
                    {
                        {_blobSettings.AvatarName, ".jpg"},
                        {_blobSettings.VideoName, ".mkv"},
                        {_blobSettings.AudioName, ".wav"}
                    };
                    System.Console.WriteLine("Try to download and upload");
                    System.Console.WriteLine($"dialogues count: {dialogues.Count}");
                    
                    foreach (var dialogue in dialogues)
                    {
                        System.Console.WriteLine(dialogue);
                        foreach (var (key, value) in dict)
                        {
                            var fileName = dialogue + value;
                            var filePath = key + "/" + fileName;
                            var stream =  await _sftpClient.DownloadFromFtpAsMemoryStreamAsync(filePath);
                            tasks.Add(_blobClient.UploadFileStreamToBlob(key, fileName, stream));
                        }
                    }
                    
                    await Task.WhenAll(tasks);
                    System.Console.WriteLine("Download and Upload finished");
                }
                catch (Exception e)
                {
                    _log.Fatal($"{e}");
                }
            }
        }
    }
}